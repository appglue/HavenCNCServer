# ICNCJobStorage Interface Specification

## Overview

The `ICNCJobStorage` interface defines the contract for managing CNC jobs and G-code files in the HavenCNC application. This interface provides 14 methods for CRUD operations on jobs and G-code files, with support for paging, categories, and metadata. It supports two different implementations:

1. **Centroid Implementation**: File system-based storage with optional MongoDB sync
2. **Simulator Implementation**: In-memory storage for testing and simulation

## Core Concepts

### Unique Identifiers

- **Jobs**: Identified by unique `id` (GUID/UUID)
  - Jobs can have duplicate `name` values
  - All operations use `id` for lookup
  
- **G-Code Files**: Identified by composite key `directory + fileName`
  - Must be unique within a directory
  - Same filename can exist in different directories

### Paging

All list operations support server-side paging to handle large datasets efficiently.

## Data Structures

### Job Class

```typescript
export class Job implements IStorable {
  public id: string;              // UUID v4 - auto-generated on creation
  public name: string;            // Display name (can be non-unique)
  
  // Material configuration
  private _material: Material;
  private _materialOffset: CNCOffset;
  private _materialPositionReference: MaterialPositionReference;
  
  // G-Code
  private _gcode: string;
  
  // Clamps
  public clamps: Clamp[];
  
  // Work zero
  private _workZeroPoint: CNCOffset;
  private _workZeroConfirmed: boolean;
  
  // Runtime statistics
  private _runStatistics: JobRunStatistic[];
  private _executionCount: number;
  
  // Stage-based execution
  private _stages: JobStage[];
  private _browsedStageIndex: number;
  
  // Run configuration
  private _useCurrentTool: boolean;
  private _runDustCollector: boolean;
  private _turnVacuumOffAtEnd: boolean;
  private _noToolChanges: boolean;
  private _skipOptionalStops: boolean;
  private _startVacuumTable: boolean;
  private _startDustCollector: boolean;
  private _lowerDustBoot: boolean;
  private _useCurrentFixture: boolean;
  
  // Dry run settings
  private _dryRunToolpathMode: DryRunToolpathMode;
  private _dryRunToolMode: DryRunToolMode;
  private _dryRunZ: number;
  private _dryRunChangeSpeed: boolean;
  private _dryRunSpeed: number;
}

export interface JobRunStatistic {
  startTime: Date;
  endTime: Date;
  durationMs: number;
}
```

**Notes:**
- `id` is auto-generated using `crypto.randomUUID()` in constructor
- Job implements `IStorable` for serialization via `toJSON()` and `fromJSON()`
- All private fields have getters/setters with StateManager notifications

### JobMetadata

Lightweight metadata returned by list operations (without full Job object):

```typescript
export interface JobMetadata {
  id: string;                    // Unique identifier (UUID)
  name: string;                  // Display name (can be non-unique)
  lastModified: Date;
  createdAt: Date;
  size: number;                  // Serialized size in bytes
  executionCount: number;        // Number of times job was run
  lastRunDate?: Date;            // Date of most recent execution
  category?: string;             // Optional category/tag
  description?: string;
  materialType?: MaterialType;
  estimatedTime?: string;
}
```

### GCodeFileMetadata

Lightweight metadata for G-code files (without content):

```typescript
export interface GCodeFileMetadata {
  name: string;                  // Filename (e.g., "cabinet_panel.nc")
  directory: string;             // Full directory path
  category?: string;             // Optional category/tag
  description?: string;
  materialType?: MaterialType;
  estimatedTime?: string;
  lastModified: Date;
  size: number;                  // File size in bytes
}
```

**Unique Identifier**: `directory + name` (composite key)

### GCodeFileData

Full file data including content:

```typescript
export interface GCodeFileData extends GCodeFileMetadata {
  content: string;               // Full G-code content
}
```

### Paging Types

```typescript
export interface PageRequest {
  page: number;                  // 0-indexed page number
  pageSize: number;              // Items per page
  sortBy?: 'name' | 'lastModified' | 'size' | 'executionCount';
  sortDirection?: 'asc' | 'desc';
}

export interface PagedResult<T> {
  items: T[];                    // Items for current page
  totalCount: number;            // Total items matching criteria
  page: number;                  // Current page (0-indexed)
  pageSize: number;              // Items per page
  totalPages: number;            // Total number of pages
}
```

## Interface Methods

### G-Code File Operations

#### listGCodeFiles
```typescript
listGCodeFiles(
  directories: string[],
  pageRequest: PageRequest
): Promise<PagedResult<GCodeFileMetadata>>;
```

**Purpose**: List G-code files across multiple directories with paging

**Parameters**:
- `directories`: Array of full directory paths to search (e.g., `["/ncfiles/recent", "/ncfiles/templates"]`)
- `pageRequest`: Paging and sorting configuration

**Returns**: Paged result with file metadata (no content)

**Implementation Notes**:
- Scan all specified directories
- Combine results and apply sorting
- Return only requested page
- Metadata only - content not loaded

---

#### fetchGCodeFile
```typescript
fetchGCodeFile(
  directory: string,
  fileName: string
): Promise<GCodeFileData | null>;
```

**Purpose**: Fetch complete G-code file with content

**Returns**: Full file data or null if not found

---

#### storeGCodeFile
```typescript
storeGCodeFile(
  directory: string,
  fileData: GCodeFileData
): Promise<boolean>;
```

**Purpose**: Save/update a G-code file

**Implementation Notes**:
- Create directory if it doesn't exist
- Overwrite if file already exists
- Update metadata (lastModified, size)
- Return success status

---

#### deleteGCodeFile
```typescript
deleteGCodeFile(
  directory: string,
  fileName: string
): Promise<boolean>;
```

**Purpose**: Delete a G-code file

---

#### setGCodeFileCategory
```typescript
setGCodeFileCategory(
  directory: string,
  fileName: string,
  category: string | undefined
): Promise<boolean>;
```

**Purpose**: Set/update/remove category for a file

**Notes**: Pass `undefined` to remove category

---

#### moveGCodeFileToCategory
```typescript
moveGCodeFileToCategory(
  directory: string,
  fileName: string,
  newCategory: string
): Promise<boolean>;
```

**Purpose**: Move file to different category (alias for setGCodeFileCategory)

### Job Operations

#### listJobs
```typescript
listJobs(
  pageRequest: PageRequest
): Promise<PagedResult<JobMetadata>>;
```

**Purpose**: List all saved jobs with paging

**Returns**: Job metadata only (no full Job objects)

**Implementation Notes**:
- Load metadata from database/index
- Don't deserialize full Job objects
- Support sorting by executionCount

---

#### fetchJob
```typescript
fetchJob(jobId: string): Promise<Job | null>;
```

**Purpose**: Load complete Job object by ID

**Implementation Notes**:
- Deserialize full Job with all nested objects
- Reconstruct Material, Clamps, Stages, etc.
- Return null if not found

---

#### getLastJob
```typescript
getLastJob(): Promise<Job | null>;
```

**Purpose**: Get the most recently executed job

**Implementation Notes**:
- Track in database or separate "last job" pointer
- Eventually replaces `MachineStateData.lastJob`
- Return null if no jobs have been run

---

#### saveLastJob
```typescript
saveLastJob(job: Job): Promise<boolean>;
```

**Purpose**: Mark a job as the last one executed

**Implementation Notes**:
- Update "last job" tracking
- Store job ID and timestamp
- Don't necessarily save full job (that's `storeJob`)

---

#### storeJob
```typescript
storeJob(job: Job): Promise<boolean>;
```

**Purpose**: Save/update a complete Job

**Implementation Notes**:
- Serialize full Job object (via `toJSON()`)
- Update metadata (lastModified, size)
- Create if new, update if exists (based on `job.id`)
- Update index/database

---

#### deleteJob
```typescript
deleteJob(jobId: string): Promise<boolean>;
```

**Purpose**: Delete a job by ID

---

#### setJobCategory
```typescript
setJobCategory(
  jobId: string,
  category: string | undefined
): Promise<boolean>;
```

**Purpose**: Set/update/remove category for a job

**Notes**: Pass `undefined` to remove category

---

#### moveJobToCategory
```typescript
moveJobToCategory(
  jobId: string,
  newCategory: string
): Promise<boolean>;
```

**Purpose**: Move job to different category (alias for setJobCategory)

## Implementation Guidelines

### Storage Architecture

HavenCNC uses a **hybrid local + cloud storage strategy** similar to the configuration file system:

**Local Storage (Always)**:
- Primary storage in `/data/jobs/` and `/data/gcode/` directories
- Files stored with unique identifiers as filenames
- Metadata can be extracted from file contents
- Always available, works offline

**MongoDB Cloud Storage (Optional)**:
- Secondary storage when MongoDB is available
- Stores same data with additional metadata:
  - Version number (for tracking changes)
  - Machine name (for multi-machine setups)
  - Sync timestamp
- Provides backup and multi-machine access

**Retrieval Priority**:
1. **If MongoDB available**: Check MongoDB first (most recent version)
2. **If MongoDB unavailable**: Use local files
3. **Sync Strategy**: Keep last 20 jobs and last 20 files synced to MongoDB

### File Organization

**Job Files** (`/data/jobs/`):
```
/data/jobs/
  550e8400-e29b-41d4-a716-446655440000.json
  7c9e6679-7425-40de-944b-e07fc1f90ae7.json
  ...
```
- Filename: `{jobId}.json` (UUID from Job.id)
- Content: Full Job serialized via `toJSON()`
- Metadata embedded in JSON (name, category, executionCount, etc.)

**G-Code Files** (`/data/gcode/`):
```
/data/gcode/
  {directory}/
    cabinet_panel.nc
    simple_test.nc
    ...
```
- Organized by directory path (preserves user organization)
- Filename: Original name (unique within directory)
- Sidecar metadata files: `{filename}.meta.json` (optional, for extended metadata)

**Metadata Extraction**:
- Job metadata extracted from JSON file during indexing
- G-Code metadata from `.meta.json` or by parsing G-code comments
- Build in-memory index on startup for fast listing

### MongoDB Schema

**Jobs Collection** (`jobs`):
```javascript
{
  _id: ObjectId,
  jobId: "550e8400-e29b-41d4-a716-446655440000",  // UUID from Job.id
  machineName: "Machine001",
  version: 5,                                       // Incremented on each save
  lastModified: ISODate("2026-02-07T10:30:00Z"),
  syncedAt: ISODate("2026-02-07T10:30:05Z"),
  data: { /* Full Job JSON */ },
  metadata: {
    name: "Cabinet Panel",
    category: "Woodworking",
    executionCount: 3,
    size: 45632,
    // ... other JobMetadata fields
  }
}
```

**G-Code Files Collection** (`gcodeFiles`):
```javascript
{
  _id: ObjectId,
  directory: "/data/gcode/templates",
  fileName: "simple_test.nc",
  machineName: "Machine001",
  version: 2,
  lastModified: ISODate("2026-02-07T09:15:00Z"),
  syncedAt: ISODate("2026-02-07T09:15:03Z"),
  content: "G21\nG90\n...",                         // Full G-code content
  metadata: {
    name: "simple_test.nc",
    category: "Testing",
    size: 3250,
    // ... other GCodeFileMetadata fields
  }
}
```

### Sync Strategy (Similar to Configuration Files)

**Automatic Sync Events**:
1. **On Save**: When `storeJob()` or `storeGCodeFile()` called
   - Save to local file immediately
   - If MongoDB available, sync to cloud (increment version)
   - Background operation, doesn't block UI

2. **On Fetch**: When `fetchJob()` or `fetchGCodeFile()` called
   - Check MongoDB first (if available)
   - If found and newer than local, use MongoDB version
   - If MongoDB unavailable or older, use local file

3. **On Startup**: Background sync process
   - Check MongoDB for newer versions of last 20 jobs/files
   - Download and update local copies
   - Build in-memory index

4. **Periodic Sync** (Optional):
   - Every N minutes, check for updates from MongoDB
   - Download newer versions
   - Upload local changes not yet synced

**Last 20 Rule**:
- Only sync most recent 20 jobs (by lastModified)
- Only sync most recent 20 G-code files across all directories (by lastModified)
- Older items remain local-only unless explicitly synced
- Configurable threshold in settings

**Conflict Resolution**:
- Version number determines latest
- If local and cloud both modified offline:
  - Higher version wins
  - Store conflict copy with timestamp if needed
- Machine name helps identify source of changes

### Centroid Implementation Details

**File Operations**:
```typescript
class CentroidJobStorage implements ICNCJobStorage {
  private localJobsPath = '/data/jobs/';
  private localGCodePath = '/data/gcode/';
  private mongoClient?: MongoClient;
  private jobIndex: Map<string, JobMetadata> = new Map();
  private fileIndex: Map<string, GCodeFileMetadata> = new Map();
  
  async storeJob(job: Job): Promise<boolean> {
    // 1. Save to local file
    const localPath = `${this.localJobsPath}${job.id}.json`;
    const json = JSON.stringify(job.toJSON(), null, 2);
    await fs.writeFile(localPath, json);
    
    // 2. Update local index
    this.jobIndex.set(job.id, this.extractJobMetadata(job));
    
    // 3. Sync to MongoDB if available
    if (this.mongoClient) {
      await this.syncJobToMongo(job);
    }
    
    return true;
  }
  
  async fetchJob(jobId: string): Promise<Job | null> {
    // 1. Check MongoDB first (if available)
    if (this.mongoClient) {
      const mongoJob = await this.fetchJobFromMongo(jobId);
      if (mongoJob) {
        // Save to local cache
        await this.saveJobToLocal(mongoJob);
        return mongoJob;
      }
    }
    
    // 2. Fall back to local file
    const localPath = `${this.localJobsPath}${jobId}.json`;
    if (await fs.exists(localPath)) {
      const json = await fs.readFile(localPath, 'utf-8');
      const job = new Job();
      job.fromJSON(JSON.parse(json));
      return job;
    }
    
    return null;
  }
  
  private async syncJobToMongo(job: Job): Promise<void> {
    const collection = this.mongoClient.db().collection('jobs');
    
    // Get current version from MongoDB
    const existing = await collection.findOne({ jobId: job.id });
    const newVersion = (existing?.version || 0) + 1;
    
    await collection.updateOne(
      { jobId: job.id },
      {
        $set: {
          jobId: job.id,
          machineName: this.getMachineName(),
          version: newVersion,
          lastModified: new Date(),
          syncedAt: new Date(),
          data: job.toJSON(),
          metadata: this.extractJobMetadata(job)
        }
      },
      { upsert: true }
    );
  }
  
  private async syncLast20JobsOnStartup(): Promise<void> {
    if (!this.mongoClient) return;
    
    const collection = this.mongoClient.db().collection('jobs');
    
    // Get last 20 jobs from MongoDB
    const cloudJobs = await collection
      .find({ machineName: this.getMachineName() })
      .sort({ lastModified: -1 })
      .limit(20)
      .toArray();
    
    for (const cloudJob of cloudJobs) {
      const localPath = `${this.localJobsPath}${cloudJob.jobId}.json`;
      
      // Check if local version is older or missing
      if (await this.isCloudNewerThanLocal(localPath, cloudJob)) {
        // Download and save locally
        await fs.writeFile(localPath, JSON.stringify(cloudJob.data, null, 2));
        this.jobIndex.set(cloudJob.jobId, cloudJob.metadata);
      }
    }
  }
}
```

**Index Building**:
- On startup, scan `/data/jobs/` and `/data/gcode/` directories
- Parse each file to extract metadata
- Build in-memory `Map<jobId, JobMetadata>` for fast listing
- No database needed for local operations

**Performance Optimizations**:
- Async file operations (don't block UI)
- MongoDB operations in background
- Cache frequently accessed jobs in memory
- Index provides O(1) lookup for metadata

### Simulator Implementation

**Storage Strategy**:
- In-memory maps/dictionaries
- `Map<string, Job>` for jobs (keyed by ID)
- `Map<string, Map<string, GCodeFileData>>` for files (directory → filename → file)
- No persistence (resets on restart)
- No MongoDB integration (test data only)

**Test Data**:
- Pre-populate with sample jobs and files
- Use same test data currently in `JobStorage.ts`
- Simulate version numbers and timestamps

## Migration Path

### Current State
- `JobStorage.ts` provides hardcoded test data
- Returns full Job objects and GCodeFile objects
- No paging support

### Migration Steps

1. **Phase 1**: Create simulator implementation
   - Implement `ICNCJobStorage` with in-memory storage
   - Migrate current test data from `JobStorage.ts`
   - Keep existing UI working

2. **Phase 2**: Add Job.id field and update serialization
   - ✅ **COMPLETED**: Added `id` field to Job class
   - ✅ **COMPLETED**: Auto-generate UUID in constructor
   - Update `toJSON()` / `fromJSON()` to include id

3. **Phase 3**: Create local file storage
   - Implement file-based Centroid storage
   - Save to `/data/jobs/` and `/data/gcode/`
   - Build in-memory index for fast listing
   - No MongoDB yet (local-only)

4. **Phase 4**: Add MongoDB sync
   - Implement MongoDB connection and sync logic
   - Follow configuration file sync pattern
   - Sync last 20 jobs and files
   - Version tracking and conflict resolution

5. **Phase 5**: Update UI to use new interface
   - Update `GCodeStage.tsx` to use paging
   - Show metadata in lists, fetch full data on demand
   - Use job IDs instead of names

6. **Phase 6**: Remove `MachineStateData.lastJob`
   - Replace with `getLastJob()` / `saveLastJob()`
   - Clean up state management

## Example Usage

### Listing Jobs
```typescript
const pageRequest: PageRequest = {
  page: 0,
  pageSize: 20,
  sortBy: 'lastModified',
  sortDirection: 'desc'
};

const result = await jobStorage.listJobs(pageRequest);
console.log(`Total jobs: ${result.totalCount}`);
console.log(`Showing page ${result.page + 1} of ${result.totalPages}`);

for (const jobMeta of result.items) {
  console.log(`${jobMeta.name} - Run ${jobMeta.executionCount} times`);
}
```

### Fetching and Running a Job
```typescript
// Get job ID from list
const jobId = "550e8400-e29b-41d4-a716-446655440000";

// Fetch full job
const job = await jobStorage.fetchJob(jobId);
if (job) {
  // Run the job
  await runJob(job);
  
  // Mark as last job
  await jobStorage.saveLastJob(job);
  
  // Update execution count and save
  job.incrementExecutionCount();
  await jobStorage.storeJob(job);
}
```

### Listing Files Across Directories
```typescript
const pageRequest: PageRequest = {
  page: 0,
  pageSize: 50,
  sortBy: 'lastModified',
  sortDirection: 'desc'
};

const directories = [
  '/ncfiles/recent',
  '/ncfiles/templates',
  '/ncfiles/examples'
];

const result = await jobStorage.listGCodeFiles(directories, pageRequest);

for (const fileMeta of result.items) {
  console.log(`${fileMeta.name} (${fileMeta.directory})`);
  console.log(`  Size: ${fileMeta.size} bytes`);
  console.log(`  Modified: ${fileMeta.lastModified}`);
}
```

### Loading File Content
```typescript
const fileData = await jobStorage.fetchGCodeFile(
  '/ncfiles/templates',
  'simple_test.nc'
);

if (fileData) {
  console.log(`Content length: ${fileData.content.length} chars`);
  // Use fileData.content for preview or execution
}
```

## Key Differences from Current Implementation

| Aspect | Current (JobStorage) | New (ICNCJobStorage) |
|--------|---------------------|---------------------|
| Job Identification | By name (st (includes `id: string` field with UUID generation)
- **Current Implementation**: `src/data/JobStorage.ts` (to be replaced)
- **UI**: `src/components/Views/Milling/GCodeStage.tsx`
- **Configuration Sync Reference**: See configuration file sync implementation for similar pattern

## Configuration File Sync Pattern Reference

The job/file sync should follow the same pattern as configuration files:

1. **Local First**: Always save to local file immediately (no blocking)
2. **Background Sync**: Upload to MongoDB asynchronously
3. **Version Tracking**: Increment version number on each save
4. **Machine Name**: Tag with machine identifier for multi-machine setups
5. **Conflict Resolution**: Higher version wins, store conflicts if needed
6. **Startup Sync**: Download last N items from MongoDB on startup
7. **Offline Support**: Full functionality without MongoDB connection

This ensures consistent behavior across all data types in HavenCNC.y |
| Paging | No support | Full paging support |
| Categories | No support | Category tagging |
| Last Job | Stored in MachineStateData | Managed by interface |
| Directories | Enum | String paths |
| Implementation | Single hardcoded class | Interface with multiple implementations |

## Related Files

- **Interface**: `src/machine/ICNCJobStorage.ts`
- **Job Class**: `src/data/Job.ts`
- **Current Implementation**: `src/data/JobStorage.ts` (to be replaced)
- **UI**: `src/components/Views/Milling/GCodeStage.tsx`
