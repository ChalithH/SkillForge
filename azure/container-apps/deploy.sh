#!/bin/bash
# Deployment script for Azure Container Apps
# Run this script to deploy SkillForge with cost-optimised settings

set -e

# Configuration
RESOURCE_GROUP="skillforge-rg"
LOCATION="australiaeast"
ENVIRONMENT_NAME="skillforge-env"
FRONTEND_APP_NAME="skillforge-frontend"
BACKEND_APP_NAME="skillforge-backend"

echo "Deploying SkillForge to Azure Container Apps"

# Create Container Apps Environment if it doesn't exist
echo "Checking Container Apps Environment..."
if ! az containerapp env show --name $ENVIRONMENT_NAME --resource-group $RESOURCE_GROUP > /dev/null 2>&1; then
    echo "Creating Container Apps Environment..."
    az containerapp env create \
      --name $ENVIRONMENT_NAME \
      --resource-group $RESOURCE_GROUP \
      --location $LOCATION
else
    echo "Container Apps Environment already exists."
fi

# Create Azure File Share for SQLite database persistence
echo "Checking Azure Storage Account..."
STORAGE_ACCOUNT=$(az storage account list --resource-group $RESOURCE_GROUP --query "[?starts_with(name, 'skillforge')].name" -o tsv | head -1)

if [ -z "$STORAGE_ACCOUNT" ]; then
    echo "Creating Azure File Share for database..."
    STORAGE_ACCOUNT="skillforge$(date +%s)"
    az storage account create \
      --name $STORAGE_ACCOUNT \
      --resource-group $RESOURCE_GROUP \
      --location $LOCATION \
      --sku Standard_LRS
    
    az storage share create \
      --name sqlite-data \
      --account-name $STORAGE_ACCOUNT
    
    # Add storage to Container Apps Environment
    STORAGE_KEY=$(az storage account keys list -g $RESOURCE_GROUP -n $STORAGE_ACCOUNT --query "[0].value" -o tsv)
    
    az containerapp env storage set \
      --name $ENVIRONMENT_NAME \
      --resource-group $RESOURCE_GROUP \
      --storage-name sqlite-storage \
      --azure-file-account-name $STORAGE_ACCOUNT \
      --azure-file-account-key $STORAGE_KEY \
      --azure-file-share-name sqlite-data \
      --access-mode ReadWrite
else
    echo "Storage account $STORAGE_ACCOUNT already exists."
fi

# Build and push Docker images first
echo "Building Docker images..."
cd ../..

# Build backend image with SQLite fix
echo "Building backend image..."
docker build -f backend/Dockerfile.azure -t skillforge-backend:local ./backend

# Build frontend image
echo "Building frontend image..."
docker build -f frontend/Dockerfile.azure -t skillforge-frontend:local ./frontend

# Push images to Azure Container Registry
echo "Creating temporary Azure Container Registry..."
ACR_NAME="skillforge$(date +%s)"
az acr create --name $ACR_NAME --resource-group $RESOURCE_GROUP --sku Basic --location $LOCATION

# Enable admin user for ACR (needed for Container Apps)
az acr update --name $ACR_NAME --admin-enabled true

# Get ACR credentials
ACR_USERNAME=$(az acr credential show --name $ACR_NAME --query username -o tsv)
ACR_PASSWORD=$(az acr credential show --name $ACR_NAME --query passwords[0].value -o tsv)

# Login to ACR
az acr login --name $ACR_NAME

# Tag and push images
docker tag skillforge-backend:local ${ACR_NAME}.azurecr.io/skillforge-backend:latest
docker tag skillforge-frontend:local ${ACR_NAME}.azurecr.io/skillforge-frontend:latest

docker push ${ACR_NAME}.azurecr.io/skillforge-backend:latest
docker push ${ACR_NAME}.azurecr.io/skillforge-frontend:latest

cd azure/container-apps

# Deploy Backend Container App
echo "Deploying backend container app..."
az containerapp create \
  --name $BACKEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT_NAME \
  --image ${ACR_NAME}.azurecr.io/skillforge-backend:latest \
  --registry-server ${ACR_NAME}.azurecr.io \
  --registry-username $ACR_USERNAME \
  --registry-password $ACR_PASSWORD \
  --target-port 5000 \
  --ingress external \
  --cpu 0.5 \
  --memory 1Gi \
  --min-replicas 1 \
  --max-replicas 5 \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://+:5000 "ConnectionStrings__DefaultConnection=Data Source=/app/skillforge.db" "JwtSettings__SecretKey=\$JWT_SECRET_KEY" JwtSettings__ExpirationInHours=24 ExchangeBackgroundService__Enabled=true ExchangeBackgroundService__CheckIntervalMinutes=10

# Deploy Frontend Container App with proper build-time environment variables
echo "Deploying frontend container app..."
BACKEND_FQDN=$(az containerapp show --name $BACKEND_APP_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)

# Build frontend with correct environment variables
echo "Building frontend with correct API URLs..."
docker build -f frontend/Dockerfile.azure \
  --build-arg VITE_API_URL="https://$BACKEND_FQDN/api" \
  --build-arg VITE_SIGNALR_URL="https://$BACKEND_FQDN" \
  -t skillforge-frontend:final ./frontend

# Tag and push the properly configured frontend
docker tag skillforge-frontend:final ${ACR_NAME}.azurecr.io/skillforge-frontend:final
docker push ${ACR_NAME}.azurecr.io/skillforge-frontend:final

az containerapp create \
  --name $FRONTEND_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --environment $ENVIRONMENT_NAME \
  --image ${ACR_NAME}.azurecr.io/skillforge-frontend:final \
  --registry-server ${ACR_NAME}.azurecr.io \
  --registry-username $ACR_USERNAME \
  --registry-password $ACR_PASSWORD \
  --target-port 80 \
  --ingress external \
  --cpu 0.25 \
  --memory 0.5Gi \
  --min-replicas 0 \
  --max-replicas 3

# Get URLs
FRONTEND_FQDN=$(az containerapp show --name $FRONTEND_APP_NAME --resource-group $RESOURCE_GROUP --query properties.configuration.ingress.fqdn -o tsv)

echo "Deployment complete!"
echo "Frontend URL: https://$FRONTEND_FQDN"
echo "Backend URL: https://$BACKEND_FQDN"