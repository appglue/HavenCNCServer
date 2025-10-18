# Indexed Storage Pattern

## Overview

The CNC Program Storage system uses an **indexed storage pattern** to optimize list operations while maintaining full data integrity. This pattern separates metadata (for fast queries) from full data (loaded on demand).

## Implementation

### Core Concept

- **Index File** (`_index.json`) - Contains lightweight metadata entries
- **Individual Storage Files** (`{name}.json`) - Contains complete program data

### Index Structure

```csharp
public class StorageIndexEntry
{
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public StorageType StorageType { get; set; }  // Flow or MDI
    public bool ExposeAsAction { get; set; }
    public string ActionName { get; set; }
}
```

### Key Operations

**List Operations (Fast - Read Index Only)**
```csharp
public List<string> GetAllStorageNames()
{
    var index = LoadIndex();  // Read _index.json once
    return index.Select(e => e.Name).OrderBy(n => n).ToList();
}

public List<string> GetMDIStorageNames()
{
    var index = LoadIndex();
    return index
        .Where(e => e.StorageType == StorageType.MDI)
        .Select(e => e.Name)
        .ToList();
}
```

**Individual Read (Load Full Data)**
```csharp
public ProgramStorage? GetStorageData(string name)
{
    var filePath = GetStorageFilePath(name);
    var json = IOFile.ReadAllText(filePath);
    return JsonSerializer.Deserialize<ProgramStorage>(json);
}
```

**Write Operation (Update Both)**
```csharp
public void SaveStorageData(ProgramStorage storage)
{
    // 1. Save full data to individual file
    var json = JsonSerializer.Serialize(storage);
    IOFile.WriteAllText(filePath, json);
    
    // 2. Update index entry
    UpdateIndexEntry(storage);
}

private void UpdateIndexEntry(ProgramStorage storage)
{
    var index = LoadIndex();
    var existing = index.FirstOrDefault(e => e.Name == storage.Name);
    
    if (existing != null)
    {
        // Update existing entry
        existing.UpdatedAt = storage.UpdatedAt;
        existing.StorageType = storage.StorageType;
        // ... other metadata
    }
    else
    {
        // Add new entry
        index.Add(new StorageIndexEntry { /* ... */ });
    }
    
    SaveIndex(index);
}
```

**Delete Operation (Remove Both)**
```csharp
public void DeleteStorageData(string name)
{
    // 1. Delete individual file
    IOFile.Delete(filePath);
    
    // 2. Remove from index
    RemoveIndexEntry(name);
}

private void RemoveIndexEntry(string name)
{
    var index = LoadIndex();
    index.RemoveAll(e => e.Name == name);
    SaveIndex(index);
}
```

## React/LocalStorage Implementation

Apply the same pattern to React applications using localStorage:

### 1. Define Index Key and Entry Structure

```typescript
const STORAGE_INDEX_KEY = 'program_storage_index';

interface StorageIndexEntry {
    name: string;
    createdAt: string;  // ISO date string
    updatedAt: string;
    storageType: 'Flow' | 'MDI';
    exposeAsAction: boolean;
    actionName: string;
}

interface ProgramStorage extends StorageIndexEntry {
    data: string;  // Full program data
}
```

### 2. Index Management Functions

```typescript
// Load index from localStorage
function loadIndex(): StorageIndexEntry[] {
    try {
        const json = localStorage.getItem(STORAGE_INDEX_KEY);
        return json ? JSON.parse(json) : [];
    } catch {
        return [];
    }
}

// Save index to localStorage
function saveIndex(index: StorageIndexEntry[]): void {
    localStorage.setItem(STORAGE_INDEX_KEY, JSON.stringify(index));
}

// Update single index entry
function updateIndexEntry(storage: ProgramStorage): void {
    const index = loadIndex();
    const existingIndex = index.findIndex(e => e.name === storage.name);
    
    const entry: StorageIndexEntry = {
        name: storage.name,
        createdAt: storage.createdAt,
        updatedAt: storage.updatedAt,
        storageType: storage.storageType,
        exposeAsAction: storage.exposeAsAction,
        actionName: storage.actionName
    };
    
    if (existingIndex >= 0) {
        index[existingIndex] = entry;
    } else {
        index.push(entry);
    }
    
    saveIndex(index);
}

// Remove index entry
function removeIndexEntry(name: string): void {
    const index = loadIndex();
    const filtered = index.filter(e => e.name !== name);
    saveIndex(filtered);
}
```

### 3. Storage Operations

```typescript
// Get individual storage key
function getStorageKey(name: string): string {
    return `program_storage_${name}`;
}

// List operations (fast - use index)
export function getAllStorageNames(): string[] {
    const index = loadIndex();
    return index.map(e => e.name).sort();
}

export function getMDIStorageNames(): string[] {
    const index = loadIndex();
    return index
        .filter(e => e.storageType === 'MDI')
        .map(e => e.name)
        .sort();
}

export function getFlowStorageNames(): string[] {
    const index = loadIndex();
    return index
        .filter(e => e.storageType === 'Flow')
        .map(e => e.name)
        .sort();
}

export function getActionStorageNames(): string[] {
    const index = loadIndex();
    return index
        .filter(e => e.exposeAsAction)
        .map(e => e.actionName)
        .sort();
}

// Get full storage data (read individual item)
export function getStorageData(name: string): ProgramStorage | null {
    try {
        const key = getStorageKey(name);
        const json = localStorage.getItem(key);
        return json ? JSON.parse(json) : null;
    } catch {
        return null;
    }
}

// Save storage (update both)
export function saveStorageData(storage: ProgramStorage): void {
    storage.updatedAt = new Date().toISOString();
    
    // Save full data
    const key = getStorageKey(storage.name);
    localStorage.setItem(key, JSON.stringify(storage));
    
    // Update index
    updateIndexEntry(storage);
}

// Delete storage (remove both)
export function deleteStorageData(name: string): void {
    // Remove full data
    const key = getStorageKey(name);
    localStorage.removeItem(key);
    
    // Remove from index
    removeIndexEntry(name);
}
```

### 4. React Hook Example

```typescript
import { useEffect, useState } from 'react';

export function useProgramStorageList(type?: 'Flow' | 'MDI') {
    const [names, setNames] = useState<string[]>([]);
    
    const refresh = () => {
        if (type === 'Flow') {
            setNames(getFlowStorageNames());
        } else if (type === 'MDI') {
            setNames(getMDIStorageNames());
        } else {
            setNames(getAllStorageNames());
        }
    };
    
    useEffect(() => {
        refresh();
        
        // Listen for storage changes from other tabs
        const handleStorageChange = (e: StorageEvent) => {
            if (e.key === STORAGE_INDEX_KEY) {
                refresh();
            }
        };
        
        window.addEventListener('storage', handleStorageChange);
        return () => window.removeEventListener('storage', handleStorageChange);
    }, [type]);
    
    return { names, refresh };
}

export function useProgramStorage(name: string | null) {
    const [storage, setStorage] = useState<ProgramStorage | null>(null);
    
    useEffect(() => {
        if (name) {
            setStorage(getStorageData(name));
        } else {
            setStorage(null);
        }
    }, [name]);
    
    return storage;
}
```

## Benefits

1. **Performance** - List operations only read small index file
2. **Scalability** - Index size grows linearly with item count, not data size
3. **Flexibility** - Can filter/sort by metadata without loading full data
4. **Simple** - No database required, pure JSON files
5. **Reliability** - Each storage item is independent file

## Trade-offs

- **Write overhead** - Must update both index and data file
- **Consistency** - Index can become out of sync if not carefully maintained
- **Startup cost** - Index must be initialized on first use
- **LocalStorage limits** - React implementation limited to ~5-10MB total

## Best Practices

1. Always update index when modifying storage data
2. Initialize index on application startup if missing
3. Keep index entries minimal (metadata only)
4. Use consistent naming conventions for storage keys
5. Handle JSON parse errors gracefully
6. Consider index rebuild function for corruption recovery
