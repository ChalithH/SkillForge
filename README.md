# SkillForge - Skill Exchange Platform

SkillForge is a skill exchange platform with a time-credit economy, enabling users to teach and learn skills through balanced knowledge exchange. Users earn credits by teaching skills and spend them to learn from others, creating a sustainable ecosystem of knowledge sharing.

### Core Features

The platform connects people based on complementary skills and learning interests:

- **Intelligent Skill Matching**: Advanced compatibility algorithms analyse user skills, learning preferences, and proficiency levels
- **Real-time User Presence**: Live status indicators and user activity tracking via SignalR WebSockets
- **Credit-based Economy Framework**: Creates sustainable relationships through balanced give-and-take (1 hour teaching = 1 hour learning)
- **Skill Exchange Requests**: Users can request exchanges with comprehensive skill and user matching
- **Community Building**: User profiles, activity tracking, and skill discovery system

## Technology Stack

### Frontend
- **React 18** with **TypeScript**
- **Redux Toolkit** + RTK Query for predictable state management
- **React Router** for client-side navigation
- **Tailwind CSS** for responsive styling
- **SignalR** for real-time WebSocket features
- **Vite** for modern build tooling and fast development

### Backend
- **.NET 8** Web API
- **Entity Framework Core** with comprehensive data modeling
- **Multi-database support** (PostgreSQL, SQL Server, SQLite) with provider-agnostic architecture
- **JWT Authentication** for secure user management
- **SignalR** for real-time presence and activity tracking
- **RESTful API design** with comprehensive CRUD operations

### Infrastructure
- **Docker** containerisation with multi-stage builds
- **Docker Compose** with flexible database profiles
- **Environment-based configuration** management
- **PostgreSQL** (default) and **SQL Server** (enterprise) support

## Key Features

### Authentication & User Management
- Secure JWT-based authentication system
- User profile management with image uploads
- Protected routes and API endpoints
- Time credit tracking and management

### Intelligent Matching System
- Sophisticated compatibility scoring based on skill overlap and user preferences
- Recommendation engine analysing complementary skills and mutual learning opportunities
- Advanced filtering by category, rating, and online status
- Detailed compatibility analysis with skill-by-skill breakdown

### Real-time Features
- User presence tracking and online/offline status indicators
- Live activity feed showing recent user sign-ins and engagement
- Real-time presence updates via SignalR WebSocket connections
- Dynamic user interface updates without page refresh

### Skill Exchange Framework
- Comprehensive skill catalog with categorisation and proficiency tracking
- Exchange request system with detailed skill and user matching
- Credit economy data modeling supporting balanced transactions
- Meeting integration and scheduling framework
- Dual-mode proficiency system enabling users to set different skill levels for teaching vs learning


### Community & Discovery
- Skill-based user discovery and browsing
- User profile system with biographical information and skill showcases
- Activity tracking and engagement metrics
- Search and filtering capabilities across skills and users

## Database Architecture

- **Users**: Authentication credentials, profiles, time credits, biographical information
- **Skills**: Categorised skill catalog with descriptions and metadata
- **UserSkills**: User-skill relationships with proficiency levels and offering status
- **SkillExchanges**: Complete exchange lifecycle management with status tracking
- **Reviews**: Post-exchange rating and feedback system
- **CreditTransactions**: Comprehensive audit trail for all credit movements

## Getting Started

### Prerequisites
- Docker Desktop
- Node.js 18+ (for frontend development)
- VS Code with Dev Containers extension (recommended for backend C# development)

### Quick Start with Docker
```bash
# Clone and start with PostgreSQL (recommended)
git clone https://github.com/ChalithH/SkillForge.git
cd SkillForge
cp .env.postgresql .env
docker-compose up

# Access the application
# Frontend: http://localhost:3000  
# Backend API: http://localhost:5000
# Database: localhost:5432 (PostgreSQL)
```

### Database Profiles
Choose your development database based on your needs:

```bash
# PostgreSQL (recommended - production parity)
cp .env.postgresql .env
docker-compose up

# SQL Server (enterprise features)
cp .env.sqlserver .env  
docker-compose --profile sqlserver up
```

### Development Workflow

#### Hybrid Development Setup (Recommended)
```bash
# Start all services
docker-compose up

# Backend Development:
# 1. Open project in VS Code
# 2. Click "Reopen in Container" when prompted  
# 3. Open any .cs file from the project (via VS Code Explorer), then hover over the loading circle next to C# icon in status bar and click "Open Solution", then select SkillForge.sln
# 4. Develop with full C# IntelliSense and debugging in container

# Frontend Development (separate terminal):
cd frontend
npm install  # Required for local TypeScript/React IntelliSense
npm run dev  # Or edit files directly - hot reload works automatically
```

#### Alternative: Fully Containerised Development
- Use VS Code Dev Containers for both frontend and backend
- Complete environment isolation with all tooling in containers
- Slower performance but maximum consistency

### Testing

The project uses a multi-tier testing strategy:

```bash
# Unit tests only (fast - in-memory database, no external dependencies)
dotnet test --filter "FullyQualifiedName~CreditServiceTests"

# Integration tests (PostgreSQL - production parity)
docker-compose -f docker-compose.test.yml up -d postgres-test
dotnet test --filter "FullyQualifiedName~SmokeTests"

# All tests (requires PostgreSQL container)
docker-compose -f docker-compose.test.yml up -d postgres-test
dotnet test
```

**Testing Architecture:**
- **Unit Tests**: In-memory Entity Framework for speed and isolation
- **Integration Tests**: PostgreSQL container for production parity
- **Smoke Tests**: Critical path validation (health checks, auth, database)

## Technical Implementation Highlights

**Redux State Management**
- Centralised application state with Redux Toolkit
- RTK Query for efficient API caching and data synchronisation
- Optimistic updates for seamless user experience

**Real-time WebSocket Integration**  
- SignalR implementation for user presence tracking
- Live user presence and status updates
- Real-time activity feed and user engagement tracking

**Docker Containerisation**
- Multi-stage builds for optimised production images
- Development environment with hot-reload support
- Comprehensive docker-compose configuration for all services

## Development Practices

- **TypeScript** throughout for enhanced type safety and developer experience
- **Responsive Design** with mobile-first approach using Tailwind CSS
- **Error Boundaries** and comprehensive error handling for graceful failures
- **Environment Configuration** management for different deployment stages
- **Clean Architecture** with separation of concerns and dependency injection

---

**A skill exchange platform exploring time-credit economies and peer-to-peer learning**

![React](https://img.shields.io/badge/React-18-blue?logo=react)
![.NET](https://img.shields.io/badge/.NET-8-purple?logo=dotnet)
![TypeScript](https://img.shields.io/badge/TypeScript-5-blue?logo=typescript)

![Docker](https://img.shields.io/badge/Docker-Ready-blue?logo=docker)
