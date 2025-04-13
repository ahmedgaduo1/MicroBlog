# MicroBlog

A modern microblogging platform built with .NET 9 and Clean Architecture principles, featuring real-time posts, image processing, and JWT authentication.

## Features

- Create and manage microblog posts (140 characters limit)
- Upload and process images with responsive variations
- Secure authentication using JWT
- Geographic location tracking (randomly generated)
- Real-time post processing and image optimization
- Responsive image delivery for different devices

## Tech Stack and System Component Rationale

### Core Technology Choices

- **Backend Framework**: .NET 9
  - *Rationale*: Latest .NET version providing improved performance, better cross-platform support, and access to modern C# features
  - *Alternatives Considered*: Node.js, Java Spring Boot, Python Django

- **Architecture**: Clean Architecture
  - *Rationale*: Separation of concerns, independence of frameworks, testability, and maintainability
  - *Alternatives Considered*: Vertical Slice Architecture, Traditional N-tier, Microservices

- **Database**: SQL Server with Entity Framework Core
  - *Rationale*: Strong ACID compliance, excellent tooling, and integration with .NET ecosystem
  - *Alternatives Considered*: PostgreSQL, MongoDB, Cosmos DB
  
- **ORM**: Entity Framework Core
  - *Rationale*: Native integration with .NET, LINQ support, migrations, and performance improvements
  - *Alternatives Considered*: Dapper, NHibernate, RepoDb

- **Authentication**: JWT (JSON Web Tokens)
  - *Rationale*: Stateless authentication, reduced database lookups, suitable for APIs
  - *Alternatives Considered*: OAuth 2.0, OpenID Connect, Cookie-based authentication

- **Image Processing**: SixLabors.ImageSharp
  - *Rationale*: Modern, cross-platform image processing library with .NET Core compatibility
  - *Alternatives Considered*: SkiaSharp, Magick.NET

- **File Storage**: Azure Blob Storage
  - *Rationale*: Scalable cloud storage, CDN integration capabilities, strong SLAs
  - *Alternatives Considered*: AWS S3, Google Cloud Storage, local file system

- **Async Processing**: Background Services and Hangfire
  - *Rationale*: Reliable background job processing with persistence, monitoring, and scheduling
  - *Alternatives Considered*: Azure Functions, RabbitMQ with workers

### Development Tools and Libraries

- **Mediator Pattern**: MediatR
  - *Rationale*: Decouples request/response handling, simplifies CQRS implementation
  - *Alternatives Considered*: Custom mediator implementation, direct service calls

- **Object Mapping**: AutoMapper
  - *Rationale*: Reduces boilerplate code for DTO mappings, convention-based configuration
  - *Alternatives Considered*: Manual mapping, Mapster

- **Validation**: FluentValidation
  - *Rationale*: Fluent API for building validation rules, clean separation of validation logic
  - *Alternatives Considered*: Data Annotations, custom validation approaches

- **Documentation**: Swagger/OpenAPI
  - *Rationale*: Interactive API documentation, client generation, testing capabilities
  - *Alternatives Considered*: Custom documentation, Postman collections

## Prerequisites

- .NET 9 SDK
- Azure Storage Account (for image storage)
- SQL Server (for database)

## Project Structure

```
src/
├── Domain/         # Core domain models and business logic
├── Application/    # Application services and CQRS implementation
├── Infrastructure/ # Infrastructure implementations (database, services)
└── Web/           # ASP.NET Core Web API
```

## How to Build and Run the Application Locally

### Prerequisites

Ensure you have the following installed on your development machine:

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (Local DB or full installation)
- [Node.js](https://nodejs.org/) (for the frontend components)
- [Git](https://git-scm.com/downloads)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [Visual Studio Code](https://code.visualstudio.com/)

### Getting the Code

```bash
# Clone the repository
git clone https://github.com/yourusername/MicroBlog.git
cd MicroBlog
```

### Building the Application

```bash
# Restore dependencies and build the solution
dotnet restore
dotnet build -tl
```

### Running the Application

From the command line:

```bash
# Navigate to the Web project directory
cd .\src\Web\

# Run the application in watch mode (auto-reloads on changes)
dotnet watch run
```

Alternatively, in Visual Studio:
1. Open the solution file `MicroBlog.sln`
2. Set the `Web` project as the startup project
3. Press F5 to start debugging

The application will be available at:
- API: https://localhost:5001/api
- Swagger Documentation: https://localhost:5001/swagger
- Web Interface: https://localhost:5001

## Database Setup

The application uses Entity Framework Core with SQL Server. Here's how to set up the database:

### Using Local Development Database

1. **Configure Connection String**:
   
   Update the connection string in `appsettings.Development.json` in the Web project:
   
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MicroBlogDb;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```
   
   Alternatively, you can use a full SQL Server instance:
   
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=MicroBlogDb;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```

2. **Run Database Migrations**:

   ```bash
   # Navigate to the Web project directory
   cd .\src\Web\
   
   # Apply migrations to create/update the database schema
   dotnet ef database update
   ```

3. **Seed Initial Data** (Optional):

   The application will automatically seed initial data (admin user, sample posts) on first run. 
   
   Default admin credentials:
   - Username: `admin@microblog.local`
   - Password: `Admin123!`

### For Production Environments

For production deployments, we recommend:

1. Using a managed SQL Server instance
2. Storing connection strings in environment variables or a secure configuration service
3. Running migrations as part of your deployment pipeline

## Key Components

### 1. Post Management

- Create posts with text (140 character limit)
- Optional image uploads with automatic processing
- Geographic location tracking
- User association and tracking

### 2. Image Processing

- Automatic image validation (size and format)
- Responsive image generation (320x240, 640x480, 1024x768, 1920x1080)
- WebP format optimization
- Asynchronous processing pipeline

### 3. Authentication

- JWT-based authentication
- Role-based access control
- Secure password hashing
- Token validation and refresh

## Configuration

### Application Settings

The application uses a combination of `appsettings.json` files and environment variables for configuration.

#### Development Environment

For local development, modify the `appsettings.Development.json` file in the Web project:

```json
{
  "UseInMemoryDatabase": false,
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MicroBlogDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true",
    "ContainerName": "uploads"
  },
  "JwtSettings": {
    "SecurityKey": "your_development_jwt_key_should_be_at_least_32_chars",
    "Issuer": "MicroBlog",
    "Audience": "MicroBlogUsers",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

#### Environment Variables

For production or when using containers, you can use environment variables that override the settings in the configuration files:

```
# Database
ConnectionStrings__DefaultConnection=your_connection_string

# Azure Storage
AzureStorage__ConnectionString=your_storage_connection_string
AzureStorage__ContainerName=uploads

# JWT Settings
JwtSettings__SecurityKey=your_jwt_key
JwtSettings__Issuer=your_issuer
JwtSettings__Audience=your_audience
JwtSettings__ExpiryMinutes=60
```

### Azure Storage Emulator

For local development without Azure Storage, you can:

1. Use the [Azurite storage emulator](https://docs.microsoft.com/azure/storage/common/storage-use-azurite)
2. Change the configuration to use local file storage by setting `UseLocalStorage` to `true` in your appsettings

## Testing

The solution contains unit, integration, functional, and acceptance tests.

To run all tests except acceptance tests:
```bash
dotnet test --filter "FullyQualifiedName!~AcceptanceTests"
```

To run acceptance tests:
1. Start the application:
```bash
cd .\src\Web\
dotnet run
```
2. In a new console:
```bash
dotnet test .\src\AcceptanceTests\
```

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Code Styles & Formatting

The template includes [EditorConfig](https://editorconfig.org/) support to help maintain consistent coding styles for multiple developers working on the same project across various editors and IDEs. The **.editorconfig** file defines the coding styles applicable to this solution.

## Code Scaffolding

The template includes support to scaffold new commands and queries.

Start in the `.\src\Application\` folder.

Create a new command:

```
dotnet new ca-usecase --name CreateTodoList --feature-name TodoLists --usecase-type command --return-type int
```

Create a new query:

```
dotnet new ca-usecase -n GetTodos -fn TodoLists -ut query -rt TodosVm
```

If you encounter the error *"No templates or subcommands found matching: 'ca-usecase'."*, install the template and try again:

```bash
dotnet new install Clean.Architecture.Solution.Template::9.0.8
```

## Help
To learn more about the template go to the [project website](https://github.com/jasontaylordev/CleanArchitecture). Here you can find additional guidance, request new features, report a bug, and discuss the template with other users.

## Build Status
![.NET CI](https://github.com/yourusername/MicroBlog/workflows/dotnet-ci/badge.svg)
[![codecov](https://codecov.io/gh/yourusername/MicroBlog/branch/main/graph/badge.svg)](https://codecov.io/gh/yourusername/MicroBlog)

## Continuous Integration
Automated CI/CD pipeline configured with GitHub Actions:
- Build validation
- Automated testing
- Code coverage reporting

## Running Tests
Run tests with coverage:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```