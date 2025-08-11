**You are an expert .NET backend developer. Generate a production-ready RESTful API backend for a task management application using .NET 8+, ASP.NET Core, and C# 12+ with modern best practices.**

---

## 1. Core Requirements

- **Runtime**: .NET 8 (LTS)
    
- **Framework**: ASP.NET Core Web API
    
- **Language**: C# 12+ with nullable reference types and strict analysis enabled
    
- **Functionality**:
    
    - User Registration, Login (JWT)
        
    - Role-based authorization (Admin, Manager, Member)
        
    - Task creation, update, delete, assignment
        
    - Project/Team Management
        
    - Task status tracking (To Do, In Progress, Done)
        
    - Commenting on tasks
        
    - Due dates, reminders & priority settings
        
    - Activity history and audit trails
        
- **Architecture**: Clean Architecture with Domain, Application, Infrastructure, and Web layers
    
- **Pattern**: CQRS + Mediator (MediatR) with Repository & Unit of Work pattern
    
- **Performance**: Support 5,000+ concurrent users via Kestrel + reverse proxy (e.g. Nginx/IIS)
    

---

## 2. Internationalization (Zero Hardcoded Strings)

- **Library**: `Microsoft.Extensions.Localization` + `i18next.net` or `i18next` compatible resource management
    
- **Locales**:
    
    - `Resources/Localization/en/`
        
    - `Resources/Localization/fr/`
        
    - `Resources/Localization/es/`
        
    - `Resources/Localization/de/`
        
    - `Resources/Localization/pt/`
        
- **Detection Sources**:
    
    - User preference
        
    - Query string: `?lang=fr`
        
    - Header: `Accept-Language` and `X-Language`
        
- **Fallback**: Default to `en` with missing key warnings via logging
    
- **Key Format**: Dot notation e.g. `task.create.success`, `validation.task.title.required`
    

---

## 3. API Standards & Design

- **RESTful**: Proper HTTP verbs and status codes
    
- **API Versioning**: URL-based (`/api/v1/tasks`)
    
- **Response Format**:
    

json

CopyEdit

`{   "data": {},   "message": "task.create.success",   "localizedMessage": "Task created successfully",   "errors": [],   "success": true,   "timestamp": "2025-01-01T12:00:00Z",   "requestId": "uuid",   "pagination": {} }`

- **Validation**: FluentValidation with localized error messages
    
- **Error Handling**: Global middleware for structured, localized error responses
    
- **Logging**: Serilog with Correlation IDs
    

---

## 4. Database & Data Management

- **Database**: PostgreSQL with Dapper or EF Core
    
- **ORM**: EF Core + EF Core Migrations
    
- **Schema**:
    
    - GUID Primary Keys
        
    - Audit fields: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `DeletedAt`, `IsDeleted`
        
    - Optimistic concurrency with row versioning
        
    - Soft deletes via global filters
        
    - Active status tracking (`IsActive`)
        
- **Seeding**: JSON/YAML-based seeders for dev/testing
    
- **Caching**: Redis for:
    
    - Session & refresh tokens
        
    - Query result caching
        
    - Rate limit counters
        
- **Connection Resilience**: Retry policy with Polly
    

---

## 5. Security Implementation

- **Authentication**: JWT with refresh tokens
    
- **Authorization**: Role-based and resource-level policies
    
- **Security Headers**: CSP, HSTS, XSS, etc. via middleware
    
- **Rate Limiting**: ASP.NET Core Rate Limiting + Redis
    
- **Input Security**:
    
    - XSS/SQL injection protections
        
    - Payload size limits
        
- **CORS**: Configured per environment
    
- **Environment Security**:
    
    - Secrets managed via Azure Key Vault or .NET Secret Manager
        

---

## 6. Observability & Monitoring

- **Logging**: Serilog (JSON structured logs)
    
- **Distributed Tracing**: OpenTelemetry (.NET SDK)
    
- **Health Checks**:
    
    - `/health`, `/health/live`, `/health/ready`
        
    - PostgreSQL, Redis, external services
        
- **Metrics**: Prometheus exporter + Grafana dashboards
    
- **Error Tracking**: Application Insights, Sentry or ELK stack
    

---

## 7. Testing Strategy

- **Unit Tests**: xUnit with 90%+ coverage
    
- **Integration Tests**: WebApplicationFactory + Testcontainers
    
- **E2E Tests**: Playwright or Selenium (if UI involved)
    
- **Localization Tests**: Ensure all translations present
    
- **Load Testing**: k6 or Artillery
    
- **Contract Testing**: Pact.NET
    
- **Data Generation**: Bogus or AutoFixture
    

---

## 8. DevOps & Deployment

- **Containerization**: Multi-stage `Dockerfile` for production
    
- **Process Management**: Systemd or Kestrel via reverse proxy
    
- **CI/CD**: GitHub Actions with:
    
    - Linting, testing, security checks
        
    - Docker build & push
        
    - Secrets scanning (GitLeaks)
        
- **Environments**:
    
    - `.env.development`, `.env.staging`, `.env.production`
        
    - Environment validation with strongly typed options
        
- **Kubernetes**:
    
    - Helm charts or raw YAML
        
    - Liveness/readiness probes
        
    - ConfigMaps + Secrets
        

---

## 9. Documentation

- **OpenAPI**: Swashbuckle (Swagger) with:
    
    - Bearer authentication
        
    - Versioning
        
    - Localized descriptions
        
- **Postman Collection**: Pre-exported with examples
    
- **README**: Full setup, dev guide, i18n config
    
- **Changelog**: Semantic Versioning (CHANGELOG.md)
    

---

## 10. Code Quality & Standards

- **Static Analysis**: Roslyn analyzers + SonarQube
    
- **Style**: EditorConfig, dotnet format
    
- **Pre-commit Hooks**: Husky (with `lefthook`) or manually via Git
    
- **DI Container**: Built-in Microsoft.Extensions.DependencyInjection
    
- **Architecture Enforcement**: Ardalis.GuardClauses, Vertical slice pattern, SOLID
    
- **Asynchronous**: `async/await` non-blocking by default
    
- **Error Handling**: ProblemDetails with rich error metadata
    

---

## Project Structure

bash

CopyEdit

`src/ ├── WebApi/                 # ASP.NET Core Startup │   ├── Controllers/ │   ├── Middleware/ │   ├── Filters/ │   ├── Localization/ │   └── Program.cs ├── Application/            # Application Layer (Use Cases) │   ├── Commands/ │   ├── Queries/ │   ├── DTOs/ │   ├── Validators/ │   └── Interfaces/ ├── Domain/                 # Business Entities │   ├── Entities/ │   ├── ValueObjects/ │   └── Enums/ ├── Infrastructure/         # Data & External Services │   ├── Persistence/ │   │   ├── Context/ │   │   ├── Migrations/ │   │   └── Repositories/ │   ├── Services/           # External services (email, file, etc.) │   └── Cache/ ├── Tests/ │   ├── Unit/ │   ├── Integration/ │   ├── E2E/ │   └── Fixtures/ ├── Shared/                 # Common logic/utilities │   ├── Localization/ │   ├── Constants/ │   ├── Extensions/ │   └── Errors/ config/ ├── appsettings.json ├── appsettings.Development.json docker/ ├── Dockerfile ├── docker-compose.yml k8s/ ├── deployment.yaml .github/ ├── workflows/ │   ├── ci.yml │   └── cd.yml .env README.md CHANGELOG.md`

---

## Technology Stack

|Area|Technology|
|---|---|
|Core Framework|.NET 8, ASP.NET Core Web API|
|Language|C# 12 with nullable annotations|
|ORM|EF Core + Dapper (optional)|
|Auth|JWT, ASP.NET Core Identity|
|DB|PostgreSQL|
|Caching|Redis (StackExchange.Redis)|
|Validation|FluentValidation|
|i18n|IStringLocalizer, i18next.net|
|Logging|Serilog|
|Tracing|OpenTelemetry + Jaeger|
|Testing|xUnit, FluentAssertions, Testcontainers|
|Docs|Swashbuckle (Swagger)|
|DevOps|GitHub Actions, Docker, Kubernetes|

---

## Additional Context

- **Target Deployment**: Azure AKS or AWS ECS (with PostgreSQL and Redis)
    
- **Expected Load**: 5,000+ concurrent users, 1M+ tasks
    
- **Compliance**: GDPR (Europe), SOC2
    
- **External Integrations**: Azure AD B2C (optional), email (SendGrid), notifications (SignalR)
    
- **Supported Languages**: English (default), French, Spanish, German, Portuguese
    
- **Database Scale**: Millions of tasks with read replicas and optimized indexing## ✅ Ultimate ASP.NET Core Production-Ready API Prompt Template

**You are an expert .NET backend developer. Generate a production-ready RESTful API backend for a task management application using .NET 8+, ASP.NET Core, and C# 12+ with modern best practices.**

---

## 1. Core Requirements

- **Runtime**: .NET 8 (LTS)
    
- **Framework**: ASP.NET Core Web API
    
- **Language**: C# 12+ with nullable reference types and strict analysis enabled
    
- **Functionality**:
    
    - User Registration, Login (JWT)
        
    - Role-based authorization (Admin, Manager, Member)
        
    - Task creation, update, delete, assignment
        
    - Project/Team Management
        
    - Task status tracking (To Do, In Progress, Done)
        
    - Commenting on tasks
        
    - Due dates, reminders & priority settings
        
    - Activity history and audit trails
        
- **Architecture**: Clean Architecture with Domain, Application, Infrastructure, and Web layers
    
- **Pattern**: CQRS + Mediator (MediatR) with Repository & Unit of Work pattern
    
- **Performance**: Support 5,000+ concurrent users via Kestrel + reverse proxy (e.g. Nginx/IIS)
    

---

## 2. Internationalization (Zero Hardcoded Strings)

- **Library**: `Microsoft.Extensions.Localization` + `i18next.net` or `i18next` compatible resource management
    
- **Locales**:
    
    - `Resources/Localization/en/`
        
    - `Resources/Localization/fr/`
        
    - `Resources/Localization/es/`
        
    - `Resources/Localization/de/`
        
    - `Resources/Localization/pt/`
        
- **Detection Sources**:
    
    - User preference
        
    - Query string: `?lang=fr`
        
    - Header: `Accept-Language` and `X-Language`
        
- **Fallback**: Default to `en` with missing key warnings via logging
    
- **Key Format**: Dot notation e.g. `task.create.success`, `validation.task.title.required`
    

---

## 3. API Standards & Design

- **RESTful**: Proper HTTP verbs and status codes
    
- **API Versioning**: URL-based (`/api/v1/tasks`)
    
- **Response Format**:
    

json

CopyEdit

`{   "data": {},   "message": "task.create.success",   "localizedMessage": "Task created successfully",   "errors": [],   "success": true,   "timestamp": "2025-01-01T12:00:00Z",   "requestId": "uuid",   "pagination": {} }`

- **Validation**: FluentValidation with localized error messages
    
- **Error Handling**: Global middleware for structured, localized error responses
    
- **Logging**: Serilog with Correlation IDs
    

---

## 4. Database & Data Management

- **Database**: PostgreSQL with Dapper or EF Core
    
- **ORM**: EF Core + EF Core Migrations
    
- **Schema**:
    
    - GUID Primary Keys
        
    - Audit fields: `CreatedAt`, `UpdatedAt`, `CreatedBy`, `DeletedAt`, `IsDeleted`
        
    - Optimistic concurrency with row versioning
        
    - Soft deletes via global filters
        
    - Active status tracking (`IsActive`)
        
- **Seeding**: JSON/YAML-based seeders for dev/testing
    
- **Caching**: Redis for:
    
    - Session & refresh tokens
        
    - Query result caching
        
    - Rate limit counters
        
- **Connection Resilience**: Retry policy with Polly
    

---

## 5. Security Implementation

- **Authentication**: JWT with refresh tokens
    
- **Authorization**: Role-based and resource-level policies
    
- **Security Headers**: CSP, HSTS, XSS, etc. via middleware
    
- **Rate Limiting**: ASP.NET Core Rate Limiting + Redis
    
- **Input Security**:
    
    - XSS/SQL injection protections
        
    - Payload size limits
        
- **CORS**: Configured per environment
    
- **Environment Security**:
    
    - Secrets managed via Azure Key Vault or .NET Secret Manager
        

---

## 6. Observability & Monitoring

- **Logging**: Serilog (JSON structured logs)
    
- **Distributed Tracing**: OpenTelemetry (.NET SDK)
    
- **Health Checks**:
    
    - `/health`, `/health/live`, `/health/ready`
        
    - PostgreSQL, Redis, external services
        
- **Metrics**: Prometheus exporter + Grafana dashboards
    
- **Error Tracking**: Application Insights, Sentry or ELK stack
    

---

## 7. Testing Strategy

- **Unit Tests**: xUnit with 90%+ coverage
    
- **Integration Tests**: WebApplicationFactory + Testcontainers
    
- **E2E Tests**: Playwright or Selenium (if UI involved)
    
- **Localization Tests**: Ensure all translations present
    
- **Load Testing**: k6 or Artillery
    
- **Contract Testing**: Pact.NET
    
- **Data Generation**: Bogus or AutoFixture
    

---

## 8. DevOps & Deployment

- **Containerization**: Multi-stage `Dockerfile` for production
    
- **Process Management**: Systemd or Kestrel via reverse proxy
    
- **CI/CD**: GitHub Actions with:
    
    - Linting, testing, security checks
        
    - Docker build & push
        
    - Secrets scanning (GitLeaks)
        
- **Environments**:
    
    - `.env.development`, `.env.staging`, `.env.production`
        
    - Environment validation with strongly typed options
        
- **Kubernetes**:
    
    - Helm charts or raw YAML
        
    - Liveness/readiness probes
        
    - ConfigMaps + Secrets
        

---

## 9. Documentation

- **OpenAPI**: Swashbuckle (Swagger) with:
    
    - Bearer authentication
        
    - Versioning
        
    - Localized descriptions
        
- **Postman Collection**: Pre-exported with examples
    
- **README**: Full setup, dev guide, i18n config
    
- **Changelog**: Semantic Versioning (CHANGELOG.md)
    

---

## 10. Code Quality & Standards

- **Static Analysis**: Roslyn analyzers + SonarQube
    
- **Style**: EditorConfig, dotnet format
    
- **Pre-commit Hooks**: Husky (with `lefthook`) or manually via Git
    
- **DI Container**: Built-in Microsoft.Extensions.DependencyInjection
    
- **Architecture Enforcement**: Ardalis.GuardClauses, Vertical slice pattern, SOLID
    
- **Asynchronous**: `async/await` non-blocking by default
    
- **Error Handling**: ProblemDetails with rich error metadata
    

---

## Project Structure

bash

CopyEdit

`src/ ├── WebApi/                 # ASP.NET Core Startup │   ├── Controllers/ │   ├── Middleware/ │   ├── Filters/ │   ├── Localization/ │   └── Program.cs ├── Application/            # Application Layer (Use Cases) │   ├── Commands/ │   ├── Queries/ │   ├── DTOs/ │   ├── Validators/ │   └── Interfaces/ ├── Domain/                 # Business Entities │   ├── Entities/ │   ├── ValueObjects/ │   └── Enums/ ├── Infrastructure/         # Data & External Services │   ├── Persistence/ │   │   ├── Context/ │   │   ├── Migrations/ │   │   └── Repositories/ │   ├── Services/           # External services (email, file, etc.) │   └── Cache/ ├── Tests/ │   ├── Unit/ │   ├── Integration/ │   ├── E2E/ │   └── Fixtures/ ├── Shared/                 # Common logic/utilities │   ├── Localization/ │   ├── Constants/ │   ├── Extensions/ │   └── Errors/ config/ ├── appsettings.json ├── appsettings.Development.json docker/ ├── Dockerfile ├── docker-compose.yml k8s/ ├── deployment.yaml .github/ ├── workflows/ │   ├── ci.yml │   └── cd.yml .env README.md CHANGELOG.md`

---

## Technology Stack

|Area|Technology|
|---|---|
|Core Framework|.NET 8, ASP.NET Core Web API|
|Language|C# 12 with nullable annotations|
|ORM|EF Core + Dapper (optional)|
|Auth|JWT, ASP.NET Core Identity|
|DB|PostgreSQL|
|Caching|Redis (StackExchange.Redis)|
|Validation|FluentValidation|
|i18n|IStringLocalizer, i18next.net|
|Logging|Serilog|
|Tracing|OpenTelemetry + Jaeger|
|Testing|xUnit, FluentAssertions, Testcontainers|
|Docs|Swashbuckle (Swagger)|
|DevOps|GitHub Actions, Docker, Kubernetes|

---

## Additional Context

- **Target Deployment**: Azure AKS or AWS ECS (with PostgreSQL and Redis)
    
- **Expected Load**: 5,000+ concurrent users, 1M+ tasks
    
- **Compliance**: GDPR (Europe), SOC2
    
- **External Integrations**: Azure AD B2C (optional), email (SendGrid), notifications (SignalR)
    
- **Supported Languages**: English (default), French, Spanish, German, Portuguese
    
- **Database Scale**: Millions of tasks with read replicas and optimized indexing