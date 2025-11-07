# HavenCNCServer Installer Setup

This guide explains how to create an installer for the HavenCNCServer application using Inno Setup.

## Prerequisites

### 1. Install Inno Setup
Download and install Inno Setup from: https://jrsoftware.org/isdl.php
- Recommended: Inno Setup 6.x (latest version)
- Install with default settings

### 2. Verify .NET 8 SDK
Ensure you have .NET 8 SDK installed:
```bash
dotnet --version
```

### 3. Install Yarn (for React Frontend)
Install Yarn package manager for building the React frontend:
```bash
# Install via npm
npm install -g yarn

# Or download from https://yarnpkg.com/getting-started/install
```

**Frontend Structure:**
- React frontend located at: `../HavenCNC/`
- Build command: `yarn build`
- Deploy command: `yarn deploy` (copies to `wwwroot/`)

**Note:** If Yarn is not available, the scripts will skip frontend building and use existing `wwwroot/` content.

## Building the Installer

### PowerShell Script (Recommended)
```powershell
.\build-installer.ps1
```

**Additional Parameters:**
- `-SelfContained`: Create self-contained deployment (includes .NET runtime)
- `-SkipBuild`: Skip the build step (use existing build)
- `-SkipFrontend`: Skip the React frontend build and deployment
- `-OpenInstaller`: Open installer directory when complete
- `-Configuration Debug`: Build debug version

**Examples:**
```powershell
# Standard build (includes frontend with clean deployment)
.\build-installer.ps1

# Self-contained deployment (larger but no .NET requirement)
.\build-installer.ps1 -SelfContained

# Quick rebuild of installer only (skip both backend and frontend)
.\build-installer.ps1 -SkipBuild -SkipFrontend

# Backend only build (skip frontend)
.\build-installer.ps1 -SkipFrontend
```

### Manual Process
1. Build the React frontend:
   ```bash
   cd ..\HavenCNC
   yarn install
   yarn build
   yarn deploy:clean
   yarn deploy:copy
   cd ..\HavenCNCServer
   ```

2. Build the application:
   ```bash
   dotnet build --configuration Release --framework net8.0-windows
   ```

3. Compile installer:
   ```bash
   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" setup.iss
   ```

## What Gets Included

The installer includes:

### Application Files
- ✅ **Main executable** (`HavenCNCServer.exe`)
- ✅ **All DLLs** (dependencies, including CentroidAPI.dll)
- ✅ **Configuration files** (`appsettings.json`, `settings.json`)
- ✅ **Debug symbols** (`.pdb` files)

### Web Content
- ✅ **wwwroot folder** (complete web interface)
  - Static files (HTML, CSS, JS)
  - API documentation
  - Test pages
  - Functions.xml

### CNC Integration
- ✅ **Centriod folder** (PLC scripts, configuration)
- ✅ **Documentation** (API docs, setup guides)

### Runtime Support
- ✅ **Runtime files** (if self-contained)
- ✅ **.NET dependency check** (framework-dependent builds)

## Installation Options

The installer provides these options:

### Standard Installation
- Installs to `Program Files\HavenCNCServer`
- Creates desktop shortcut (optional)
- Creates Start Menu entries
- Configures firewall rules for ports 5000/5001

### Windows Service Installation
- **Optional**: Install as Windows Service
- **Service Name**: `HavenCNCService`
- **Display Name**: "Haven CNC Server"
- **Startup**: Automatic
- **Optional**: Start service immediately

### Firewall Configuration
- Automatically adds firewall rules:
  - **HTTP**: Port 5000
  - **HTTPS**: Port 5001
- Rules are removed during uninstallation

## Testing the Installer

### Before Distribution
1. **Test on clean VM**: Verify installation on machine without development tools
2. **Check all files**: Ensure wwwroot and Centriod folders are included
3. **Test both modes**: Desktop application and Windows service
4. **Verify web interface**: Check http://localhost:5000 after installation
5. **Test uninstall**: Ensure clean removal

### Installation Verification
After installation, verify:
- Application starts correctly
- Web interface accessible at http://localhost:5000
- Swagger UI available at http://localhost:5000/swagger
- React frontend loads properly with all components
- CNC functionality works (if CNC hardware available)
- Service runs properly (if service option selected)

## Troubleshooting

### Common Issues

**Build Fails:**
- Ensure .NET 8 SDK is installed
- Check project builds successfully with `dotnet build`
- Verify all NuGet packages are restored

**Frontend Build Fails:**
- Ensure Yarn is installed: `yarn --version`
- Check frontend directory exists: `../HavenCNC/`
- Verify package.json has build and deploy scripts
- Try manual build: `cd ../HavenCNC && yarn build && yarn deploy`

**Inno Setup Not Found:**
- Install Inno Setup from official website
- Verify installation path in script
- Try running as Administrator

**Missing Files in Installer:**
- Check `[Files]` section in `setup.iss`
- Ensure source paths are correct
- Build application before creating installer

**Service Installation Fails:**
- Run installer as Administrator
- Check Windows Event Log for service errors
- Verify .NET runtime is available

### Log Files
- **Build logs**: Check console output during build
- **Installer logs**: Check Windows installer logs
- **Service logs**: Check Windows Event Viewer (Application logs)

## Customization

### Modify Installer
Edit `setup.iss` to:
- Change application version
- Modify installation paths
- Add/remove included files
- Customize installation options

### Application Settings
Users can modify after installation:
- `appsettings.json`: API configuration
- `settings.json`: Application settings
- Service configuration via Services.msc

## Distribution

The created installer:
- **Location**: `installer\HavenCNCServer-Setup-1.0.0.exe`
- **Size**: Varies (smaller for framework-dependent, larger for self-contained)
- **Requirements**: Windows x64, .NET 8 Runtime (if framework-dependent)
- **Privileges**: Requires Administrator for service installation

### Recommended Distribution Method
1. Test installer thoroughly
2. Create release notes
3. Provide both self-contained and framework-dependent versions
4. Include this README with distribution
5. Consider code signing for production releases