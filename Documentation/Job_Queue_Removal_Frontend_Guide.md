# Job Queue Removal - Frontend Integration Guide

## Summary of Backend Changes

The CNC job queue mechanism has been **removed** to simplify job execution and prevent stuck states. The backend now supports **single job execution only**.

## API Behavior Changes

### Before (Queued Jobs)
When calling `RunGCode` or `RunGCodeCommand` while a job was running:
- Job was added to queue
- Response: `{ "success": true, "message": "Job created and queued" }`
- Job would run automatically when previous job completed

### After (Single Job Only)
When calling `RunGCode` or `RunGCodeCommand` while a job is running:
- **Immediate rejection** with error
- Response: `{ "success": false, "error": "A job is already running. Please wait or stop the current job." }`
- Frontend must handle retry logic

## API Response Format (Unchanged)

```typescript
interface RunGCodeResponse {
  success: boolean;
  error?: string;
  message: string;
  jobId: string;
  job: JobDetails;
  filePath?: string;
}
```

## Error Response Example

```json
{
  "success": false,
  "error": "A job is already running. Please wait or stop the current job.",
  "message": "Job already running",
  "jobId": "",
  "job": {}
}
```

## Recommended Frontend Retry Pattern

### Option 1: Automatic Retry with Backoff

```typescript
async function runGCodeWithRetry(
  gCodeLines: string[],
  maxRetries: number = 3,
  retryDelayMs: number = 500
): Promise<RunGCodeResponse> {
  
  for (let attempt = 0; attempt < maxRetries; attempt++) {
    const response = await fetch('/api/CNCProgram/RunGCode', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ 
        gCodeLines, 
        startImmediately: true 
      })
    }).then(r => r.json());

    if (response.success) {
      return response; // Success!
    }

    // Check if error is "job already running"
    if (response.error?.includes('already running')) {
      if (attempt < maxRetries - 1) {
        // Wait before retry (exponential backoff)
        const delay = retryDelayMs * Math.pow(2, attempt);
        console.log(`Job already running, retrying in ${delay}ms...`);
        await new Promise(resolve => setTimeout(resolve, delay));
        continue;
      }
    }

    // Other error or max retries exceeded
    return response;
  }

  throw new Error('Failed to start job after maximum retries');
}
```

### Option 2: User-Initiated Retry with Polling

```typescript
async function runGCodeWithUserPrompt(gCodeLines: string[]): Promise<void> {
  const response = await fetch('/api/CNCProgram/RunGCode', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ gCodeLines, startImmediately: true })
  }).then(r => r.json());

  if (response.success) {
    showSuccess('Job started successfully');
    return;
  }

  if (response.error?.includes('already running')) {
    // Show user prompt with options
    const action = await showDialog({
      title: 'Job Already Running',
      message: 'Another job is currently executing. What would you like to do?',
      buttons: ['Wait and Retry', 'Stop Current Job', 'Cancel']
    });

    if (action === 'Wait and Retry') {
      // Poll IsJobRunning and retry when clear
      await waitForJobCompletion();
      return runGCodeWithUserPrompt(gCodeLines); // Recursive retry
    } else if (action === 'Stop Current Job') {
      await fetch('/api/CNCProgram/Stop', { method: 'POST' });
      await new Promise(resolve => setTimeout(resolve, 500)); // Wait for stop
      return runGCodeWithUserPrompt(gCodeLines); // Retry
    }
  } else {
    showError(response.error || 'Failed to start job');
  }
}

async function waitForJobCompletion(): Promise<void> {
  while (true) {
    const isRunning = await fetch('/api/CNCProgram/IsJobRunning')
      .then(r => r.json());
    
    if (!isRunning) {
      break; // Job completed
    }
    
    await new Promise(resolve => setTimeout(resolve, 500)); // Poll every 500ms
  }
}
```

### Option 3: Check Before Execution

```typescript
async function runGCodeSafe(gCodeLines: string[]): Promise<void> {
  // Check if job is already running BEFORE attempting
  const isRunning = await fetch('/api/CNCProgram/IsJobRunning')
    .then(r => r.json());

  if (isRunning) {
    showWarning('A job is already running. Please wait for it to complete.');
    return;
  }

  // Proceed with job execution
  const response = await fetch('/api/CNCProgram/RunGCode', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ gCodeLines, startImmediately: true })
  }).then(r => r.json());

  if (response.success) {
    showSuccess('Job started successfully');
  } else {
    showError(response.error || 'Failed to start job');
  }
}
```

## SignalR Events (Unchanged)

All real-time events continue to work exactly as before:
- `JobInfoEvent` - Line progress updates
- `JobStartedEvent` - Job execution begins
- `JobCompletedEvent` - Job finishes
- `StepExecutionEvent` - Step run progress
- `MessageEvent` - CNC messages, errors, warnings

## API Endpoints (Unchanged)

### Start Job
- `POST /api/CNCProgram/RunGCode`
- `POST /api/CNCProgram/RunGCodeCommand`

### Control Job
- `POST /api/CNCProgram/Stop`
- `POST /api/CNCProgram/Pause`
- `POST /api/CNCProgram/Resume`

### Query Status
- `GET /api/CNCProgram/IsJobRunning` ✅ Use this for retry logic
- `GET /api/CNCProgram/GetCurrentJobStatus`

## Migration Checklist

- [ ] Remove any code expecting jobs to queue
- [ ] Handle `success: false` responses with "already running" error
- [ ] Implement retry pattern (automatic or user-prompted)
- [ ] Update UI to show clearer messages when job is busy
- [ ] Test job rejection and retry flow
- [ ] Consider disabling "Run" button while job is active

## Benefits of This Change

✅ **Simpler** - No hidden queue state to manage  
✅ **More reliable** - Can't get stuck with queued jobs  
✅ **Frontend control** - UI decides when to retry or prompt user  
✅ **Clearer errors** - Explicit "job already running" message  
✅ **Self-healing** - If backend gets stuck, next API check recovers automatically
