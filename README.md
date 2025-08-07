# Travel and Accommodation Booking Platform

## 📋 Project Description

A comprehensive backend API for a travel and accommodation booking platform built with ASP.NET Core. This system provides a complete solution for managing hotels, rooms, bookings, user authentication, and reviews. The platform supports multiple user roles including customers, hotel owners, and administrators, with features like real-time room availability, booking management, and user review systems.

### Key Features

- **User Management**: Registration, authentication, and role-based authorization (Customer, Hotel Owner, Admin)
- **Hotel Management**: CRUD operations for hotels with amenities, images, and location data
- **Room Management**: Room types, pricing, availability tracking, and inventory management
- **Booking System**: Shopping cart functionality, booking creation, and status management
- **Review System**: Hotel reviews and ratings with validation ensuring only past guests can review
- **Search & Filtering**: Advanced hotel search with filters for dates, location, price, amenities, and ratings
- **Recently Viewed**: Tracks user browsing history for personalized experience
- **Caching**: Redis integration for improved performance
- **Logging**: Comprehensive logging with Serilog and SQL Server sink

## 🛠️ Tech Stack & Requirements

### Core Technologies
- **.NET 9.0** - Latest .NET framework
- **ASP.NET Core Web API** - RESTful API framework
- **Entity Framework Core 9.0** - ORM for database operations
- **SQL Server** - Primary database
- **Redis** - Caching layer
- **JWT Authentication** - Secure token-based authentication

### Dependencies & Libraries
- **Microsoft.EntityFrameworkCore.SqlServer** - SQL Server provider
- **Microsoft.AspNetCore.Authentication.JwtBearer** - JWT authentication
- **Microsoft.Extensions.Caching.StackExchangeRedis** - Redis caching
- **Serilog.AspNetCore** - Structured logging
- **Serilog.Sinks.MSSqlServer** - SQL Server logging sink
- **Swashbuckle.AspNetCore** - API documentation (Swagger)
- **xUnit & Moq** - Testing frameworks

### System Requirements
- **.NET 9.0 SDK** or later
- **SQL Server** 2019 or later / SQL Server Express
- **Redis Server** (optional for caching)
- **Visual Studio 2022** or **VS Code** (recommended)

## 🚀 Installation and Setup

### 1. Clone the Repository
```bash
git clone <repository-url>
cd "Travel and Accommodation Booking Platform"
```

### 2. Configure Database Connection
Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TravelBookingDB;Trusted_Connection=true;MultipleActiveResultSets=true",
    "RedisConnection": "localhost:6379"
  }
}
```

### 3. Configure JWT Settings
Set up JWT configuration in `appsettings.json`:
```json
{
  "Jwt": {
    "Key": "your-super-secret-key-here-minimum-32-characters",
    "Issuer": "TravelBookingAPI",
    "Audience": "TravelBookingClient",
    "ExpiryMinutes": 60
  }
}
```

### 4. Install Dependencies
```bash
dotnet restore
```

### 5. Run Database Migrations
```bash
dotnet ef database update
```

### 6. Run the Application
```bash
dotnet run
```

The API will be available at:
- **HTTPS**: `https://localhost:7000`
- **HTTP**: `http://localhost:5000`
- **Swagger UI**: `https://localhost:7000/swagger`

## 🏗️ Architecture Overview

### Project Structure
```
├── Controllers/          # API endpoints
├── Services/            # Business logic layer
├── Data/               # Entity Framework DbContext
├── Models/             # Entity models
├── DTOs/               # Data Transfer Objects
├── Interfaces/         # Service contracts
├── Enums/              # Enumeration types
├── Constants/          # Application constants
├── Middlewares/        # Custom middleware
├── Migrations/         # EF Core migrations
└── Tests/              # Unit and integration tests
```

### Key Components

- **Authentication Layer**: JWT-based authentication with role-based authorization
- **Business Logic**: Service layer implementing core business rules
- **Data Access**: Entity Framework Core with SQL Server
- **Caching**: Redis for performance optimization
- **Logging**: Structured logging with Serilog
- **Validation**: Data annotations and custom validation logic

## 📡 API Endpoints

### Authentication Endpoints
| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| POST | `/api/auth/register` | User registration | None |
| POST | `/api/auth/login` | User login | None |
| POST | `/api/auth/forgot-password` | Request password reset | None |
| POST | `/api/auth/reset-password` | Reset password with token | None |
| POST | `/api/auth/logout` | Logout user | Bearer Token |

### Hotel Endpoints
| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| GET | `/api/hotels` | Get hotels (paginated) | Bearer Token |
| GET | `/api/hotels/{id}` | Get hotel by ID | Bearer Token |
| POST | `/api/hotels` | Create new hotel | Admin/Owner |
| PUT | `/api/hotels/{id}` | Update hotel | Admin/Owner |
| DELETE | `/api/hotels/{id}` | Delete hotel | Admin/Owner |
| POST | `/api/hotels/search` | Search hotels with filters | Bearer Token |

### Booking Endpoints
| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| GET | `/api/bookings/cart` | Get user's cart | Bearer Token |
| POST | `/api/bookings/cart/add` | Add item to cart | Bearer Token |
| DELETE | `/api/bookings/cart/remove/{itemId}` | Remove from cart | Bearer Token |
| POST | `/api/bookings/checkout` | Create booking | Bearer Token |
| GET | `/api/bookings/{id}` | Get booking details | Bearer Token |
| GET | `/api/bookings/user` | Get user bookings | Bearer Token |

### Review Endpoints
| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| POST | `/api/reviews` | Create review | Bearer Token |
| PUT | `/api/reviews/{id}` | Update review | Bearer Token |
| DELETE | `/api/reviews/{id}` | Delete review | Bearer Token |
| GET | `/api/reviews/{id}` | Get review by ID | None |
| GET | `/api/reviews/hotel/{hotelId}` | Get hotel reviews | None |
| GET | `/api/reviews/hotel/{hotelId}/summary` | Get review summary | None |

### Room Endpoints
| Method | Endpoint | Description | Authentication |
|--------|----------|-------------|----------------|
| GET | `/api/rooms/hotel/{hotelId}` | Get hotel rooms | Bearer Token |
| POST | `/api/rooms` | Create room | Admin/Owner |
| PUT | `/api/rooms/{id}` | Update room | Admin/Owner |
| DELETE | `/api/rooms/{id}` | Delete room | Admin/Owner |


## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run specific test project
dotnet test Tests/
```

### Testing Frameworks
- **xUnit**: Primary testing framework
- **Moq**: Mocking framework for unit tests
- **Entity Framework InMemory**: For integration tests

## 📊 Logging and Monitoring

### Logging Configuration
The application uses **Serilog** for structured logging with the following sinks:
- **Console**: Development environment
- **SQL Server**: Production logging to database
- **File**: Rotating file logs in `/Logs` directory

### Log Levels
- **Information**: General application flow
- **Warning**: Unexpected situations that don't stop execution
- **Error**: Error events that might still allow application to continue
- **Critical**: Very serious error events

### Viewing Logs
Logs are stored in:
1. **Database**: `Logs` table in SQL Server
2. **Files**: `/Logs/log-YYYYMMDD.txt`
3. **Console**: During development

## ⚠️ Known Issues and Limitations

### Current Limitations
- **Payment Integration**: Mock payment system (no real payment processing)
- **Email Service**: Basic email functionality (needs SMTP configuration)
- **File Upload**: Limited to URL-based image management
- **Real-time Updates**: No WebSocket integration for real-time booking updates

### Performance Considerations
- **Pagination**: Implemented for large datasets
- **Caching**: Redis caching for blacklist unvalid JWT tokens after logout or deleting a user 
- **Database Optimization**: Proper indexing and query optimization needed for production
---

**Note**: This is an educational project demonstrating modern web API development practices with .NET Core. For production use, additional security hardening, performance optimization, and monitoring should be implemented.
