# HavenCNCServer - CNC Server Management Application

HavenCNCServer is a C# ASP.NET Core Web API application that provides REST services for CNC machine management with an integrated WinForms GUI and React frontend.

## Features

- **REST API Server** - ASP.NET Core Web API with comprehensive CNC management endpoints
- **Swagger/OpenAPI Documentation** - Interactive API documentation and testing
- **Windows Forms GUI** - Native Windows interface for local management
- **React Frontend** - Modern web interface served at the root URL
- **Command Line Interface** - CLI tools for automation and CI/CD integration

## Quick Start

### Running the Application

**GUI Mode (Default):**
```bash
HavenCNCServer.exe
```

**Generate OpenAPI Documentation:**
```bash
HavenCNCServer.exe --generate-openapi
# or
HavenCNCServer.exe -g
```

**Show Help:**
```bash
HavenCNCServer.exe --help
# or
HavenCNCServer.exe -h
```

### Accessing the API

When running, the application provides:
- **Main Application**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger  
- **OpenAPI JSON**: http://localhost:5000/swagger/v1/swagger.json

## Command Line Interface

### OpenAPI Generation

The `--generate-openapi` argument allows you to generate OpenAPI specifications without running the GUI:

```bash
# Generate OpenAPI specification
HavenCNCServer.exe --generate-openapi

# Output:
# HavenCNCServer OpenAPI Generator
# ================================
# 
# Starting API server...
# Downloading OpenAPI specification...
# ✓ OpenAPI specification saved to: openapi.json
# ✓ File size: 125,432 bytes
# 
# You can also access the live documentation at:
#   - Swagger UI: http://localhost:5000/swagger
#   - OpenAPI JSON: http://localhost:5000/swagger/v1/swagger.json
# 
# Stopping server...
# ✓ Done!
```

This is particularly useful for:
- **CI/CD Pipelines** - Automated API documentation generation
- **API Client Generation** - Creating clients from OpenAPI specs
- **Documentation Updates** - Ensuring API docs stay current
- **Integration Testing** - Validating API contract changes

### Available Commands

| Command | Short | Description |
|---------|-------|-------------|
| `--generate-openapi` | `-g` | Generate OpenAPI specification and exit |
| `--help` | `-h` | Show help message with usage information |

## Development

### Building the Project

```bash
# Build for release
dotnet build --configuration Release

# Build for specific framework
dotnet build --configuration Release --framework net8.0-windows
```

### Alternative OpenAPI Generation Methods

1. **PowerShell Script:**
   ```powershell
   .\generate-openapi.ps1
   ```

2. **Manual Access:**
   - Start the application normally
   - Visit http://localhost:5000/swagger/v1/swagger.json
   - Save the JSON response

## Project Structure

### Core Files
- `Program.cs` - Application entry point with CLI support
- `ApiStartup.cs` - Web API configuration and middleware
- `MainForm.cs` - Windows Forms GUI implementation

### API Controllers
- `Controllers/` - REST API endpoints
  - `CNCConfigurationController.cs` - Configuration management
  - `CNCMovementController.cs` - Machine movement control
  - `CNCProgramController.cs` - Program execution
  - `CNCSpindleController.cs` - Spindle operations
  - And more...

### Models and DTOs
- `Models/` - Data models and utilities
  - `CNCConfiguration.cs` - Configuration data structures
  - `CNCEnums.cs` - Enumeration definitions
  - `CNCRequests.cs` - Request/response models
  - `MachineConfigurationDTOs.cs` - Data transfer objects

### Documentation
- `Documentation/` - Technical documentation
  - `CentriodSetupAPI.md` - API setup guide
  - `InputOutputPorts.md` - I/O configuration

## API Documentation and Integration

This directory contains documentation and API wrappers for the HavenCNCServer project.

### API Classes (/CentriodAPI/)
- CNCUtils_Final.cs - Clean CNC12 API wrapper with no dependencies
  - Replaces GeneralUtils from Centroid Wizard project
  - Provides parameter access, workpiece reference points, and bit manipulation
  - Requires only CentroidAPI reference

### Quick Start Integration

1. Initialize CNCUtils:
   ```csharp
   using HavenCNCServer.CentriodAPI;
   CNCUtils.Initialize(yourCentroidApiInstance);
   ```

2. Use in PLC code:
   ```csharp
   double value = CNCUtils.GetParameterValue(CNC12Parameters.SPINDLE_COUNTS_REV_PARM);
   CNCUtils.SetWorkpieceReferencePoint(ReferencePoints.G30, 1, newXPos);
   ```

3. Read the guides:
   - Start with CNCUtils_Integration_Guide.md
   - Reference PLC_File_Format_Guide.md for PLC programming
   - Use PLC_IO_Writing_Documentation.md for I/O configuration

## Migration from GeneralUtils

All documentation has been updated to use CNCUtils instead of GeneralUtils:
- 124 method calls updated across documentation
- Zero external dependencies
- Same API surface as GeneralUtils
- Real CNC12 parameter values included

---

**Last Updated:** October 5, 2025  
**Version:** 1.0  
**Framework:** .NET 8.0 (Windows)
