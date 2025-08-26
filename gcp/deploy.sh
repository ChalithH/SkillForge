#!/bin/bash
# Deployment script for Google Cloud Run

set -e

# Configuration
PROJECT_ID="skillforge-app" 
REGION="australia-southeast1" 
SERVICE_ACCOUNT="skillforge-service@${PROJECT_ID}.iam.gserviceaccount.com"

echo "Deploying SkillForge to Google Cloud Run"

# Set the project
gcloud config set project $PROJECT_ID

# Enable required APIs
echo "Enabling required APIs..."
gcloud services enable run.googleapis.com
gcloud services enable cloudbuild.googleapis.com
gcloud services enable secretmanager.googleapis.com
gcloud services enable sqladmin.googleapis.com
gcloud services enable artifactregistry.googleapis.com

# Create Artifact Registry repository for container images
echo "Creating Artifact Registry repository..."
gcloud artifacts repositories create skillforge-repo \
  --repository-format=docker \
  --location=$REGION \
  --description="SkillForge container images" \
  --quiet || echo "Artifact Registry repository already exists"

# Generate and create secure JWT secret for production
echo "Generating secure JWT secret for production..."
if [ -z "$JWT_SECRET_KEY" ]; then
    JWT_SECRET_KEY=$(openssl rand -base64 64)
    echo "Generated new JWT secret for production deployment"
else
    echo "Using provided JWT secret (WARNING: Ensure this is not a development secret!)"
fi

echo "Creating JWT secret in Google Secret Manager..."
echo -n "$JWT_SECRET_KEY" | gcloud secrets create jwt-secret --data-file=- --quiet || echo "JWT secret already exists"

# Create Cloud SQL PostgreSQL instance (cost-optimised)
echo "Creating Cloud SQL PostgreSQL instance..."
gcloud sql instances create skillforge-db \
  --database-version=POSTGRES_15 \
  --tier=db-f1-micro \
  --region=$REGION \
  --storage-type=SSD \
  --storage-size=10GB \
  --storage-auto-increase \
  --backup-start-time=03:00 \
  --maintenance-window-day=SUN \
  --maintenance-window-hour=04 \
  --deletion-protection \
  --quiet || echo "Database instance already exists"

# Create database
echo "Creating database..."
gcloud sql databases create skillforge --instance=skillforge-db --quiet || echo "Database already exists"

# Create database user
echo "Creating database user..."
DB_PASSWORD=$(openssl rand -base64 32)
gcloud sql users create skillforge-user --instance=skillforge-db --password=$DB_PASSWORD --quiet || echo "User already exists"

# Store database password in Secret Manager
echo "Storing database credentials..."
echo -n "$DB_PASSWORD" | gcloud secrets create db-password --data-file=- --quiet || echo "DB password secret already exists"

# Get the connection name for Cloud SQL
CONNECTION_NAME="$PROJECT_ID:$REGION:skillforge-db"
DB_CONNECTION_STRING="Host=/cloudsql/$CONNECTION_NAME;Database=skillforge;Username=skillforge-user;Password=$DB_PASSWORD"

# Store connection string in Secret Manager
echo -n "$DB_CONNECTION_STRING" | gcloud secrets create db-connection-string --data-file=- --quiet || echo "Connection string secret already exists"

# Deploy Backend using source-based deployment
echo "Deploying backend to Cloud Run from source..."
gcloud run deploy skillforge-backend \
  --source ../backend \
  --platform managed \
  --region $REGION \
  --allow-unauthenticated \
  --port 5000 \
  --memory 1Gi \
  --cpu 1 \
  --min-instances 0 \
  --max-instances 10 \
  --concurrency 1000 \
  --timeout 300 \
  --cpu-throttling \
  --execution-environment gen2 \
  --add-cloudsql-instances $CONNECTION_NAME \
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Production,ASPNETCORE_URLS=http://+:5000,JwtSettings__ExpirationInHours=24,ExchangeBackgroundService__Enabled=true,ExchangeBackgroundService__CheckIntervalMinutes=15" \
  --set-secrets "ConnectionStrings__DefaultConnection=db-connection-string:latest,JwtSettings__SecretKey=jwt-secret:latest"

# Get backend URL
BACKEND_URL=$(gcloud run services describe skillforge-backend --platform managed --region $REGION --format 'value(status.url)')
echo "Backend deployed at: $BACKEND_URL"

# Deploy Frontend using Cloud Build with build arguments
echo "Building frontend with Cloud Build (with environment variables as build args)..."
cd ../frontend
gcloud builds submit --config ../gcp/cloudbuild-frontend.yaml \
  --substitutions=_BACKEND_URL="$BACKEND_URL" \
  --region=$REGION

echo "Deploying pre-built frontend to Cloud Run..."
cd ../gcp
gcloud run deploy skillforge-frontend \
  --image australia-southeast1-docker.pkg.dev/$PROJECT_ID/skillforge-repo/skillforge-frontend:latest \
  --platform managed \
  --region $REGION \
  --allow-unauthenticated \
  --port 80 \
  --memory 512Mi \
  --cpu 1 \
  --min-instances 0 \
  --max-instances 5 \
  --concurrency 1000 \
  --timeout 60 \
  --cpu-throttling \
  --execution-environment gen2

# Get frontend URL
FRONTEND_URL=$(gcloud run services describe skillforge-frontend --platform managed --region $REGION --format 'value(status.url)')

echo "Deployment complete!"
echo "Frontend URL: $FRONTEND_URL"
echo "Backend URL: $BACKEND_URL"
# echo "Next steps:"
# echo "1. Update CORS settings in backend to allow $FRONTEND_URL"
# echo "2. Set up custom domains if needed"
# echo "3. Configure Cloud CDN for additional performance and cost savings"