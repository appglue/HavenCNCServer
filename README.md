# HavenCNCServer

A C# Windows Forms application that hosts an ASP.NET Core Web API internally for CNC server management.

## Features
- **WinForms Desktop UI**: Native Windows interface for server management
- **Self-Hosted Web API**: REST services hosted internally within the WinForms app
- **Automatic Startup**: API services start automatically when the application launches
- **Live Monitoring**: Real-time server logs and status monitoring in the desktop UI
- **Swagger Integration**: Built-in API documentation accessible via web browser
- **Auto-Generated OpenAPI**: Automatically generates `openapi.json` on startup if it doesn't exist
- **Manual OpenAPI Generation**: "Generate OpenAPI" button to regenerate the specification file
- **Embedded React App**: Chromium-based WebView2 control to display your React frontend at localhost:3000
- **Maximized Window**: Application starts maximized for optimal viewing experience
- **CORS Enabled**: Ready for React frontend integration

## Requirements
- .NET 8.0 or later
- Windows operating system

## Getting Started

### Build and Run
```bash
dotnet build
dotnet run
```

### Application Features
When you run the application:
1. **Desktop Interface Opens**: A maximized WinForms window provides server management
2. **API Auto-Starts**: Web API services automatically start hosting on `http://localhost:5000`
3. **Auto-Generate OpenAPI**: If `openapi.json` doesn't exist, it's automatically generated on startup
4. **Live Status**: Real-time server status and logs displayed in the UI
5. **Browser Integration**: Click "Open Swagger UI" to access API documentation
6. **React App Integration**: Click "Open React App" to display your React frontend in an embedded Chromium browser pointing to `http://localhost:3000`
7. **Manual OpenAPI Generation**: Click "Generate OpenAPI" to manually regenerate the specification file

### API Endpoints
The hosted API provides these REST endpoints:
- `GET /api/person/GetAllPersons` - Get all persons
- `GET /api/person/GetPerson/{id}` - Get person by ID
- `POST /api/person/SavePerson` - Save/create a person

### Access Points
- **Desktop UI**: Native Windows Forms interface
- **API Base URL**: `http://localhost:5000`
- **Swagger Documentation**: `http://localhost:5000/swagger`
- **OpenAPI Spec**: `http://localhost:5000/swagger/v1/swagger.json`

### Generate OpenAPI Specification
The OpenAPI specification is automatically generated on startup if the `openapi.json` file doesn't exist.

**Manual Generation:**
While the application is running, you can regenerate the OpenAPI specification by:
1. Clicking the "Generate OpenAPI" button in the application
2. Or downloading directly:
```bash
curl -o openapi.json http://localhost:5000/swagger/v1/swagger.json
```

Or using PowerShell:
```powershell
Invoke-WebRequest -Uri "http://localhost:5000/swagger/v1/swagger.json" -OutFile "openapi.json"
```

The generated file will be saved to:
- `openapi.json` (project root)
- `bin/Debug/net8.0-windows/openapi.json` (build output)

## Architecture
This hybrid application combines:
- **WinForms Frontend**: Desktop interface for local management
- **ASP.NET Core Web API**: Self-hosted REST services for remote/web access
- **Shared Business Logic**: Controllers and models used by both interfaces
- **Integrated Logging**: Server logs displayed in both console and desktop UI

## Project Structure
- `Program.cs` - WinForms application entry point
- `MainForm.cs/.Designer.cs` - Main desktop interface with API hosting logic
- `ApiStartup.cs` - ASP.NET Core Web API configuration
- `Controllers/` - API controllers with REST endpoints
- `Models/` - Data models/DTOs
- `WinFormsLogger.cs` - Custom logger for desktop UI integration
