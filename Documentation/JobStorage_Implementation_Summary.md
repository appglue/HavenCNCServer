# Job Storage Implementation Summary

## Overview
Successfully implemented complete job and G-code file storage system following the configuration storage pattern. The implementation includes local file storage, MongoDB cloud sync, filesystem navigation for folder selection, and a RESTful API.

## Implementation Date
February 7, 2026

## Architecture

### Core Components

1. **JobFileManager** (`Services/JobFileManager.cs`)
   - Manages job files in `C:\havencncdata\jobs\{jobId}.json`
   - Version tracking with separate `.version` files
   - CRUD operations for job data
   - Lists all local job IDs

2. **GCodeFileManager** (`Services/GCodeFileManager.cs`)
   - Manages three sources of G-code files:
     - **Managed files**: `C:\havencncdata\gcode\{fileId}.nc` (synced with MongoDB)
     - **External directories**: Read-only scanning of user-specified CAD output folders
     - **MongoDB**: Cloud storage for managed files only
   - Version tracking for managed files
   - Directory scanning for external files

3. **MongoDbService Extensions** (`Services/MongoDbService.cs`)
   - Added `jobs` collection support
   - Added `gcodeFiles` collection support
   - Methods for save, load, delete, and list operations
   - Recent items sync (last 20 jobs and files)

4. **JobStorageController** (`Controllers/JobStorageController.cs`)
   - RESTful API with 13 endpoints
   - Base route: `/api/JobStorage/`
   - Static initialization pattern (follows MachineConfigurationController)
   - Initialized via ApiManager on startup
   - Includes filesystem navigation for folder picker UI

### Data Models (`Models/JobStorageModels.cs`)

- **JobDocument**: MongoDB document with jobId, machineName, version, data, timestamp, metadata
- **JobMetadata**: Lightweight metadata for list operations
- **GCodeFileDocument**: MongoDB document for managed G-code files
- **GCodeFileMetadata**: Metadata including source indicator (managed vs external)
- **PageRequest**: Paging and sorting parameters
- **PagedResult<T>**: Generic paged result wrapper
- **ListGCodeFilesRequest**: Request with directories array, fileExtensions array (defaults: .nc, .txt, .tap), and paging
- **StoreJobRequest**: Job storage request with optional metadata
- **StoreGCodeFileRequest**: G-code storage with optional fileId and metadata
- **SaveLastJobRequest**: Simple last job ID tracker
- **StoreResponse**: Generic store operation response
- **DriveInfoResponse**: Drive information for filesystem navigation (name, label, type, sizes)
- **DirectoryInfoResponse**: Directory information for browsing (name, path, hasSubdirectories)

## API Endpoints

### Job Endpoints

1. **POST** `/api/JobStorage/jobs/list`
   - List all jobs with paging and sorting
   - Body: `PageRequest` (page, pageSize, sortBy, sortDirection)
   - Returns: `PagedResult<JobMetadata>`

2. **GET** `/api/JobStorage/jobs/{id}`
   - Fetch specific job by ID
   - Returns: Job data (JSON string)
   - MongoDB first, local fallback

3. **POST** `/api/JobStorage/jobs`
   - Store a job
   - Body: `StoreJobRequest` (jobId, data, optional metadata)
   - Returns: `StoreResponse` with success status and version

4. **DELETE** `/api/JobStorage/jobs/{id}`
   - Delete job by ID
   - Deletes from local and MongoDB

5. **GET** `/api/JobStorage/jobs/last`
   - Get last executed job ID
   - Returns: Job ID string
   - Stored in `C:\havencncdata\jobs\lastJob.txt`

6. **POST** `/api/JobStorage/jobs/last`
   - Save last executed job ID
   - Body: `SaveLastJobRequest` (jobId)

### G-Code File Endpoints

7. **POST** `/api/JobStorage/gcode/list`
   - List G-code files from all sources
   - Body: `ListGCodeFilesRequest` (directories array, fileExtensions array, paging)
   - FileExtensions defaults to: `[".nc", ".txt", ".tap"]` (case-insensitive)
   - Combines: External directories + Managed files + MongoDB
   - Returns: `PagedResult<GCodeFileMetadata>`

8. **GET** `/api/JobStorage/gcode?fileId={id}`
   - Fetch managed G-code file by ID
   - Returns: G-code content (string)
   - MongoDB first, local fallback

9. **GET** `/api/JobStorage/gcode?directory={dir}&fileName={name}`
   - Fetch external G-code file
   - Returns: G-code content (string)
   - Read-only from external directory

10. **POST** `/api/JobStorage/gcode`
    - Store managed G-code file
    - Body: `StoreGCodeFileRequest`
    - Generates GUID if fileId not provided
    - Returns: `StoreResponse` with fileId and version

11. **DELETE** `/api/JobStorage/gcode?fileId={id}`
    - Delete managed G-code file
    - Deletes from local and MongoDB
    - External files cannot be deleted

### Filesystem Navigation Endpoints

12. **GET** `/api/JobStorage/filesystem/drives`
    - Get all available drives on the system
    - Returns: Array of `DriveInfoResponse` with name, label, type, size, available space
    - Used by frontend to start directory browsing
    - Drive names returned with trailing backslash: `"C:\"`, `"D:\"`

13. **GET** `/api/JobStorage/filesystem/directories?path={path}`
    - Get subdirectories for a given path
    - Query parameter: `path` (required, URL-encoded)
    - Returns: Array of `DirectoryInfoResponse` with name, fullPath, hasSubdirectories, lastModified
    - Sorted alphabetically by name
    - Returns 403 for access denied, 404 if path doesn't exist
    - **Path Format**: Standard Windows paths (`"C:\Users"`, `"D:\CAD_Output"`)
      - Accepts backslashes `\` or forward slashes `/` (.NET handles both)
      - Must be absolute paths, not relative
      - URL-encode spaces and special characters (`C:\Program%20Files`)
      - Use `fullPath` from response for next navigation call

## Storage Strategy

### Local Storage Structure
```
C:\havencncdata\
  jobs\
    {jobId}.json          # Job data
    {jobId}.json.version  # Version number
    lastJob.txt          # Last executed job ID
  gcode\
    {fileId}.nc          # Managed G-code files (named by GUID)
    {fileId}.nc.version.json  # Version tracking
```

### MongoDB Collections

**jobs** collection:
- jobId (required, indexed with machineName)
- machineName (required)
- version (long)
- data (JSON string of full job)
- timestamp (DateTime)
- metadata (JobMetadata object)

**gcodeFiles** collection:
- fileId (required, indexed with machineName)
- fileName (original filename)
- machineName (required)
- version (long)
- data (G-code content string)
- timestamp (DateTime)
- size (long)
- category, description, materialType, estimatedTime (optional)

### Sync Strategy

1. **Startup**: 
   - Download last 20 jobs from MongoDB
   - Download last 20 G-code files from MongoDB
   - Only update local if MongoDB version > local version

2. **Write Operations**:
   - Write to local file first (immediate)
   - Sync to MongoDB in background (fire-and-forget)
   - Increment version on each write

3. **Read Operations**:
   - Check MongoDB first (if connected)
   - Fallback to local storage
   - External files read directly from source

4. **Delete Operations**:
   - Delete local immediately
   - Delete from MongoDB in background

### G-Code File Sources

1. **External Directories** (Read-Only)
   - Frontend specifies directory paths
   - Backend scans for specified file extensions (default: .nc, .txt, .tap)
   - Files never moved or modified
   - CAD tool output locations
   - Case-insensitive extension matching

2. **Managed Directory**
   - Files stored by GUID: `{fileId}.nc`
   - Synced with MongoDB
   - Version tracked
   - Written when frontend explicitly saves

3. **MongoDB**
   - Cloud storage for managed files only
   - Includes metadata (category, description, etc.)
   - Machine-specific collections

## Initialization

Controller initialized in `Services/ApiManager.cs`:
```csharp
await Controllers.JobStorageController.InitializeAsync();
```

Initialization steps:
1. Create JobFileManager and GCodeFileManager
2. Load MongoDB settings from SettingsManager
3. Load machine name from machineDataStorageSettings.json
4. Sync recent items (last 20 jobs and files)

## Version Management

- Local `.version` files contain: `{"Version": N}`
- Version incremented on each write
- MongoDB stores version number with data
- Conflict resolution: Higher version wins during sync

## Paging and Sorting

- Server-side paging for all list operations
- Configurable page size (default 20)
- Sort by any metadata property
- Sort direction: "asc" or "desc"

## Error Handling

- All endpoints return 500 with error message on exceptions
- 404 for not found resources
- 400 for bad requests (missing parameters)
- Null safety checks on static services
- Logger optional (nullable) for file managers

## Machine Name Tagging

- All MongoDB documents tagged with machine name
- Supports multi-machine deployments
- Machine name from `machineDataStorageSettings.json` or Environment.MachineName

## Future Enhancements

Identified but not implemented (separate work):
- Upload utility for design machine monitoring
- Automatic upload of CAD output files
- File change detection and auto-sync
- Bulk import operations
- Search and filter by metadata
- Thumbnail/preview generation

## Testing Recommendations

1. **Test Endpoints**:
   - Use Swagger UI at `https://localhost:5001/swagger`
   - Test all CRUD operations for jobs
   - Test all CRUD operations for G-code files
   - Test paging and sorting
   - Test with and without MongoDB connection

2. **Test File Sources**:
   - Create external directory with .nc files
   - Verify read-only access (no modifications)
   - Store managed files and verify GUID naming
   - Verify MongoDB sync in background

3. **Test Sync**:
   - Restart application with MongoDB connected
   - Verify last 20 items downloaded
   - Test version conflict resolution
   - Test offline mode (MongoDB disabled)

## Configuration

MongoDB settings in `settings.json`:
```json
{
  "MongoDB": {
    "Enabled": true,
    "ConnectionString": "mongodb://...",
    "DatabaseName": "havenCNC"
  }
}
```

Machine name in `C:\havencncdata\machineDataStorageSettings.json`:
```json
{
  "CurrentMachineName": "CNC-Machine-01"
}
```

## Files Created

1. `Models/JobStorageModels.cs` - All data models
2. `Services/JobFileManager.cs` - Job file management
3. `Services/GCodeFileManager.cs` - G-code file management
4. `Controllers/JobStorageController.cs` - REST API controller

## Files Modified

1. `Services/MongoDbService.cs` - Added job and gcode collections
2. `Services/ApiManager.cs` - Added JobStorageController initialization

## Build Status

✅ Build succeeded with no errors (only pre-existing XML comment warnings)

## Next Steps for Frontend Integration

1. Import `ICNCJobStorage` TypeScript interface
2. Implement API client calls to `/api/JobStorage/*` endpoints
3. **Use filesystem navigation endpoints to build folder picker**:
   - Call `GET /api/JobStorage/filesystem/drives` to show available drives
   - Call `GET /api/JobStorage/filesystem/directories?path={path}` to browse folders
   - Build tree navigation UI for selecting external G-code directories
   - **Path handling**: URL-encode paths when making requests, use `fullPath` from responses
   - **Example flow**: 
     - Get drives → `[{"name": "C:\\", ...}, {"name": "D:\\", ...}]`
     - Browse C:\ → `GET /directories?path=C:\` → Returns subdirectories
     - Navigate deeper → `GET /directories?path=C:\Users` → Returns subdirectories
     - Select folder → Use selected path in `ListGCodeFilesRequest.directories` array
4. Provide selected external directory paths in `ListGCodeFilesRequest`
5. Handle paging for large lists
6. Use `fileId` for managed files, `directory + fileName` for external
7. Store new G-code files with `POST /api/JobStorage/gcode`
8. Track last executed job with `/api/JobStorage/jobs/last`

## Dependencies

- MongoDB.Driver (already installed)
- Microsoft.AspNetCore.Mvc (already installed)
- System.Text.Json (already installed)

## Compliance

✅ Follows configuration storage pattern from MachineConfigurationController
✅ Matches TypeScript interface requirements from documentation
✅ Supports multi-source file reading (external + managed + MongoDB)
✅ Backend ONLY - no frontend changes per project scope
✅ Uses static controller pattern consistent with codebase
✅ Implements version tracking and conflict resolution
✅ Provides server-side paging for performance
