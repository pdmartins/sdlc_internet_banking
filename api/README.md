# Contoso Bank Backend API

## Overview
This is the backend API for the Contoso Bank Internet Banking system, built using ASP.NET Core 8.0 with Clean Architecture principles.

## Architecture

The solution follows Clean Architecture with the following layers:

### 1. Domain Layer (`ContosoBank.Domain`)
- **Entities**: Core business entities (User, Account, Transaction, SecurityEvent)
- **Interfaces**: Repository and domain service interfaces
- **Business Logic**: Domain-specific logic and validations

### 2. Application Layer (`ContosoBank.Application`)
- **DTOs**: Data Transfer Objects for API communication
- **Services**: Business logic services (RegistrationService)
- **Interfaces**: Service interfaces

### 3. Infrastructure Layer (`ContosoBank.Infrastructure`)
- **Data**: Entity Framework DbContext and configurations
- **Repositories**: Data access implementations
- **External Services**: Third-party integrations

### 4. Web Layer (`ContosoBank.Web`)
- **Controllers**: API endpoints
- **Middleware**: Cross-cutting concerns
- **Configuration**: Dependency injection and startup configuration

## Features Implemented

### User Registration
- **Personal Information**: Full name, email, phone, CPF, date of birth
- **Validation**: Age verification (18+), CPF check digits, uniqueness checks
- **Security Setup**: Password, security questions, MFA options
- **Account Creation**: Automatic account generation with random account number

### Security Features
- **Password Hashing**: SHA-256 with salt
- **Security Events**: Audit logging for security-related activities
- **Data Validation**: Input validation and business rule enforcement
- **Error Handling**: Comprehensive error handling and logging

### Database
- **Entity Framework Core**: ORM for data access
- **SQL Server**: Database engine (LocalDB for development, Azure SQL for production)
- **Migrations**: Database schema versioning

## API Endpoints

### Registration Controller (`/api/registration`)

#### POST `/api/registration/register`
Register a new user with personal information.

**Request Body:**
```json
{
  "fullName": "string",
  "email": "string",
  "phone": "string",
  "cpf": "string",
  "dateOfBirth": "2000-01-01"
}
```

**Response:**
```json
{
  "userId": "guid",
  "message": "string",
  "account": {
    "accountNumber": "string",
    "branchCode": "string",
    "accountType": "string"
  }
}
```

#### POST `/api/registration/{userId}/security`
Set up security information for a registered user.

**Request Body:**
```json
{
  "password": "string",
  "securityQuestion": "string",
  "securityAnswer": "string",
  "mfaOption": "string",
  "passwordStrength": "string"
}
```

#### Validation Endpoints
- GET `/api/registration/validate/email?email={email}` - Check email availability
- GET `/api/registration/validate/cpf?cpf={cpf}` - Check CPF availability  
- GET `/api/registration/validate/phone?phone={phone}` - Check phone availability

## Configuration

### Development
- **Database**: SQL Server LocalDB
- **Connection String**: `Server=(localdb)\\mssqllocaldb;Database=ContosoBankDB;Trusted_Connection=true`
- **CORS**: Allows requests from React development server (localhost:3000)

### Production
- **Database**: Azure SQL Database
- **Security**: HTTPS enforcement, security headers
- **Monitoring**: Application Insights (to be configured)

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server LocalDB (for development)
- Entity Framework Core tools

### Running the Application

1. **Restore packages:**
   ```bash
   dotnet restore
   ```

2. **Build the solution:**
   ```bash
   dotnet build
   ```

3. **Create database migration:**
   ```bash
   cd src/ContosoBank.Web
   dotnet ef migrations add InitialCreate --project ../ContosoBank.Infrastructure
   ```

4. **Update database:**
   ```bash
   dotnet ef database update --project ../ContosoBank.Infrastructure
   ```

5. **Run the application:**
   ```bash
   dotnet run
   ```

The API will be available at:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7000` (development only)

## Database Schema

### Users Table
- Id (GUID, Primary Key)
- FullName (nvarchar(100))
- Email (nvarchar(255), Unique)
- Phone (nvarchar(20), Unique)
- CPF (nvarchar(14), Unique)
- DateOfBirth (datetime2)
- PasswordHash (nvarchar(255))
- SecurityQuestion (nvarchar(255))
- SecurityAnswerHash (nvarchar(500))
- MfaOption (nvarchar(20))
- PasswordStrength (nvarchar(20))
- CreatedAt/UpdatedAt (datetime2)

### Accounts Table
- Id (GUID, Primary Key)
- UserId (GUID, Foreign Key)
- AccountNumber (nvarchar(20), Unique)
- BranchCode (nvarchar(10))
- AccountType (nvarchar(20))
- Balance (decimal(18,2))
- DailyLimit/MonthlyLimit (decimal(18,2))
- IsActive (bit)
- CreatedAt (datetime2)

### Transactions Table
- Id (GUID, Primary Key)
- AccountId (GUID, Foreign Key)
- Type/Category (nvarchar)
- Amount (decimal(18,2))
- Description, Reference, RecipientAccount, RecipientName
- Status (nvarchar(20))
- Fee (decimal(18,2))
- BalanceAfter (decimal(18,2))
- CreatedAt (datetime2)

### SecurityEvents Table
- Id (GUID, Primary Key)
- UserId (GUID, Foreign Key)
- EventType (nvarchar(50))
- Severity (nvarchar(20))
- Description (nvarchar(500))
- IpAddress, UserAgent, Location
- IsSuccessful (bit)
- CreatedAt (datetime2)

## Integration with Frontend

The API is designed to work with the React frontend registration flow:

1. **Personal Info Form** → POST `/api/registration/register`
2. **Security Setup Form** → POST `/api/registration/{userId}/security`
3. **Real-time Validation** → GET validation endpoints
4. **Confirmation Display** → Response data from registration endpoints

## Security Considerations

- **Input Validation**: All inputs are validated at multiple layers
- **Password Security**: SHA-256 hashing with salt
- **Audit Logging**: Security events are logged for monitoring
- **CORS Policy**: Restricted to frontend application origins
- **HTTPS**: Enforced in production
- **Security Headers**: Added to prevent common attacks

## Monitoring and Logging

- **Structured Logging**: Using Microsoft.Extensions.Logging
- **Security Events**: Audit trail for security-related activities
- **Health Checks**: Database connectivity monitoring
- **Error Handling**: Comprehensive error responses with appropriate HTTP status codes

## Next Steps

1. **Authentication**: Implement JWT-based authentication
2. **Authorization**: Role-based access control
3. **Rate Limiting**: API throttling for security
4. **Caching**: Performance optimization
5. **Testing**: Unit and integration tests
6. **Monitoring**: Application Insights integration
7. **Deployment**: Azure App Service configuration
