# HavenCNCServer - C# ASP.NET Core Web API Application

This is a C# ASP.NET Core Web API project for HavenCNCServer with REST services and Swagger/OpenAPI documentation.

## Project Structure
- `Program.cs` - Application entry point and configuration
- `Controllers/` - API controllers with REST endpoints
- `Models/` - Data models and DTOs
- `HavenCNCServer.csproj` - Project configuration file

## Development Guidelines
- Use ASP.NET Core Web API best practices
- Implement attribute-based routing with [Route], [HttpGet], [HttpPost], etc.
- Use [ProducesResponseType] for better Swagger documentation
- Follow RESTful API conventions
- Use proper dependency injection patterns

## Important Scope Note
- **This is BACKEND ONLY**: Do not make changes when user asks about React, frontend, or UI modifications
- If user mentions frontend/React changes, acknowledge but explain no backend changes are needed
- Only modify backend C# code for API endpoints, services, and data models

## Build and Run
- Build: `dotnet build`
- Run: `dotnet run`
- Access Swagger UI: `https://localhost:5001/swagger`
- Debug: Use F5 in Visual Studio Code with C# extension
