# Billing Extractor

AI-powered invoice processing system for grocery stores that automatically extracts structured billing information from invoice documents using LLM technology.

## 🚀 Features

- **AI-Powered Extraction**: Uses Google Gemini to extract structured data from invoice images and PDFs
- **Multi-Format Support**: Accepts PNG, JPG, JPEG, and PDF invoice documents
- **Batch Processing**: Process multiple invoices simultaneously
- **Validation & Anomaly Detection**: 
  - Duplicate invoice detection
  - Amount verification (line items vs totals)
  - Data validation and error reporting
- **Web Interface**: Interactive HTML UI for uploading and viewing results
- **API Access**: RESTful API for programmatic access
- **Data Persistence**: Stores processed invoices in SQLite database

## 🛠️ Tech Stack

- **Backend**: .NET 8, C#
- **Architecture**: Clean Architecture with Domain, Application, Infrastructure, and API layers
- **Patterns**: CQRS, MediatR, Repository Pattern, Dependency Injection
- **Database**: Entity Framework Core with SQLite (default)
- **AI/ML**: Google Gemini API for document understanding
- **Frontend**: HTML/CSS/JavaScript with responsive design
- **Logging**: Serilog with structured logging
- **API Documentation**: Swagger/OpenAPI

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Google Gemini API Key (free tier available)

## 🔧 Setup and Configuration

### 1. Clone the Repository

```bash
git clone <your-repository-url>
cd BillingExtractor
```

### 2. Configure API Keys

Create or update `appsettings.json` or `appsettings.Development.json`:

```json
{
  "LLM": {
    "Provider": "Gemini",
    "Gemini": {
      "ProjectId": "your-project-id",
      "Location": "us-central1",
      "ModelId": "gemini-1.5-pro",
      "ApiKey": "your-api-key-here"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=billing_extractor_dev.db"
  }
}
```

Alternatively, set environment variables:

```bash
export LLM__GEMINI__APIKEY="your-api-key-here"
export LLM__GEMINI__PROJECTID="your-project-id"
```

### 3. Install Dependencies

```bash
dotnet restore
```

### 4. Run Database Migrations

```bash
# From the BillingExtractor root directory
dotnet ef database update --project BillingExtractor.Infrastructure --startup-project BillingExtractor.Api
```

### 5. Build the Solution

```bash
dotnet build
```

## ▶️ Running the Application

### Development Mode

```bash
dotnet run --project BillingExtractor.Api
```

The application will start at:
- **API**: `http://localhost:5001`
- **Web UI**: `http://localhost:5001`
- **API Docs**: `http://localhost:5001/swagger`

### Production Mode

```bash
dotnet publish -c Release
cd BillingExtractor.Api/bin/Release/net8.0/publish
dotnet BillingExtractor.Api.dll
```

## 🐳 Docker Deployment

The application can be deployed using Docker containers:

### Prerequisites
- Docker Desktop or Docker Engine
- Docker Compose

### Quick Start with Docker Compose

1. Create a `.env` file with your API keys:
```bash
GEMINI_API_KEY=your-gemini-api-key
GEMINI_PROJECT_ID=your-project-id
GEMINI_LOCATION=us-central1
```

2. Build and run the containers:
```bash
docker-compose up --build
```

The application will be available at `http://localhost:8080`

### Building Docker Image Manually

```bash
# Build the image
docker build -t billing-extractor .

# Run the container (requires database connection)
docker run -p 8080:80 -e ConnectionStrings__DefaultConnection="..." billing-extractor
```

### Docker Compose Services
- `billingextractor`: Main API application
- `mssql`: SQL Server database for persistence
- File storage volume for uploaded documents

## 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test BillingExtractor.Tests/
```

## 🌐 API Endpoints

### Invoice Processing
- `POST /api/invoices/upload` - Upload and process invoice files
- `GET /api/invoices` - Get all processed invoices with filtering
- `GET /api/invoices/{id}` - Get specific invoice by ID
- `DELETE /api/invoices/{id}` - Delete an invoice
- `GET /health` - Health check endpoint

### Query Parameters for GET /api/invoices
- `page` - Page number (default: 1)
- `pageSize` - Items per page (default: 20)
- `vendorName` - Filter by vendor name
- `fromDate` - Filter from date
- `toDate` - Filter to date

## 🖥️ Web Interface

The application includes a comprehensive web interface accessible at the root URL (`http://localhost:5001`):

- Drag-and-drop file upload
- Processing options (validation, duplicate detection)
- Real-time progress tracking
- Results display with validation warnings/errors
- Invoice history and search
- Detailed invoice view modal

## 🏗️ Project Structure

```
BillingExtractor/
├── BillingExtractor.Api/          # API layer, controllers, middleware
├── BillingExtractor.Application/  # Business logic, commands, queries, DTOs
├── BillingExtractor.Domain/       # Entities, value objects, domain events
├── BillingExtractor.Infrastructure/ # Data access, LLM services, file storage
```

## 🔐 Security Considerations

- File type validation for uploads
- File size limits (50MB max)
- API key stored securely in configuration
- Input validation and sanitization
- SQL injection protection via Entity Framework

## 📊 Configuration Options

### LLM Providers
- **Google Gemini** (default): Recommended for image/PDF processing
- **OpenAI**: Alternative option
- **Mock**: For testing without API calls

### File Storage
- Local file system storage (default)
- Configurable storage location in `appsettings.json`

## 🤖 AI Integration

The system uses Google Gemini for document understanding with:
- Multimodal processing (images and PDFs)
- Structured output formatting
- Confidence scoring
- Error handling for API failures

## 🧩 Extensibility

The architecture supports:
- Multiple LLM providers (easily swappable)
- Different database providers (SQLite, SQL Server)
- Custom validation rules
- Additional file storage backends
- Extended business logic

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

The MIT License is appropriate for this assessment project as it allows others to freely use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the software, while maintaining that the original copyright notice and disclaimer appear in all copies or substantial portions of the software.