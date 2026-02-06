# HavenCNCServer Thread Architecture & Shutdown Flow

## Overview
This document explains the thread architecture, naming conventions, and shutdown sequence to help diagnose and fix application closure issues.

---

## Thread Categories

### 1. **CNCEventBus Worker Threads** (Managed, Named, Background)
**Location:** `Centroid/Events/CNCEventBus.cs`

**Thread Count:** 3 threads
- `EventBus-Position` (1 thread) - Priority: AboveNormal
- `EventBus-Messages-0` (1 thread) - Priority: Normal  
- `EventBus-Messages-1` (1 thread) - Priority: Normal

**Lifecycle:**
- Created in: `StartWorkerThreads()`
- Properties: `IsBackground = true`, `Name = "EventBus-..."`, `Priority` set explicitly
- Stopped via: `_cancellation.Cancel()` → `Dispose()` with 5-second join timeout
- **Status:** ✅ Properly named, background threads, cancellation token support

**Shutdown:**
```csharp
_cancellation.Cancel();
_positionChannel.Writer.Complete();
_logChannel.Writer.Complete();
_messageChannel.Writer.Complete();
foreach (var thread in _workerThreads) {
    if (!thread.Join(TimeSpan.FromSeconds(5))) {
        LogWarning($"Worker thread {thread.Name} did not exit gracefully");
    }
}
```

---

### 2. **SignalR Message Processor Thread** (Managed, Named, Background)
**Location:** `Services/SignalRManager.cs` → `SignalREventListener` class

**Thread Count:** 1 thread
- `SignalR-MessageProcessor` - Priority: AboveNormal

**Lifecycle:**
- Created in: Constructor with `new Thread(ProcessMessageQueue)`
- Properties: `IsBackground = true`, `Name = "SignalR-MessageProcessor"`, `Priority = AboveNormal`
- Stopped via: `_cancellationTokenSource.Cancel()` → `Dispose()` with 5-second join timeout
- **Status:** ✅ Properly named, background thread, cancellation token support

**Shutdown:**
```csharp
_isRunning = false;
_cancellationTokenSource.Cancel();
_messageQueue.CompleteAdding();
if (!_processingThread.Join(TimeSpan.FromSeconds(5))) {
    LogWarning("SignalR message processor thread did not stop within timeout");
}
```

---

### 3. **ASP.NET Core Thread Pool** (Unmanaged, Unnamed, NOT Background)
**Location:** `Services/ApiManager.cs` → Web Host created via `Host.CreateDefaultBuilder()`

**Thread Count:** ~50-60 threads (dynamic thread pool)
- Thread pool worker threads (unnamed)
- I/O completion threads (unnamed)
- Timer threads (unnamed)
- SignalR hub connection threads (unnamed)

**Lifecycle:**
- Created in: `ApiManager.StartAsync()` → `_webHost.RunAsync(_cancellationTokenSource.Token)`
- Properties: **NONE** - ASP.NET Core creates unmanaged thread pool
- Stopped via: `_cancellationTokenSource.Cancel()` → `_webHost.StopAsync()`
- **Status:** ⚠️ **CRITICAL ISSUE** - Not background threads, cannot be named, relies on StopAsync

**Problem:**
- ASP.NET Core thread pool threads are **NOT background threads**
- When WPF application tries to exit, these threads keep the process alive
- `_webHost.StopAsync()` must be called explicitly to shut down thread pool
- Even with cancellation, shutdown can take 3+ seconds

**Fix Applied:**
1. Added `ApiManager.StopAsync()` call to `CleanupBeforeShutdown()` (was missing!)
2. Increased force exit timer from 3 → 5 seconds to allow proper cleanup
3. Added console logging to track shutdown progress

---

### 4. **WPF Dispatcher Thread** (Managed, Primary UI Thread)
**Location:** `WPF/MainWindow.xaml.cs`, `ProgramWPF.cs`

**Thread Count:** 1 thread (main UI thread)

**Lifecycle:**
- Created by: WPF Application framework
- Stopped via: `Application.Shutdown()` after window closes
- **Status:** ✅ Properly managed by WPF

---

### 5. **Background Task.Run() Operations** (Thread Pool)
**Locations:** Multiple files using `Task.Run()`

Examples:
- `ProgramWPF.cs`: MongoDB initialization, CNC connection
- `ApiManager.cs`: Web host runner, initialization tasks
- `CNCConnectionManager.cs`: Connection monitoring
- `WPF/ViewModels/MainViewModel.cs`: Button command handlers

**Thread Count:** Variable (uses .NET thread pool)
- All use `CancellationToken` from `_cancellationTokenSource`
- **Status:** ✅ Properly use cancellation tokens

---

## Current Shutdown Sequence

### MainWindow.OnClosing → CleanupBeforeShutdown (Fixed!)

```
1. User closes window
   ↓
2. MainWindow.OnClosing triggered
   ├─ Set _isShuttingDown flag
   ├─ Start 5-second force exit timer
   └─ Trigger CleanupBeforeShutdown() asynchronously

3. CleanupBeforeShutdown executes:
   ├─ [Before] DumpActiveThreads() - show initial thread state
   ├─ [1/5] ApiManager.StopAsync() ← **CRITICAL: NOW INCLUDED**
   │   ├─ Cancel _cancellationTokenSource
   │   ├─ Call _webHost.StopAsync() with 3-second timeout
   │   └─ Dispose _webHost
   ├─ [2/5] Unsubscribe CNC event handlers
   ├─ [3/5] CentroidEventBridge.Stop()
   ├─ [4/5] CNCEventBus.Instance.Dispose()
   │   ├─ Cancel worker threads
   │   ├─ Complete channels
   │   └─ Join threads (5-second timeout each)
   ├─ [5/5] CNCConnectionManager.Disconnect()
   └─ [After] DumpActiveThreads() - show final thread state

4. If cleanup doesn't complete in 5 seconds:
   └─ Force exit timer fires → Environment.Exit(0)
```

---

## What Was Broken Before

### Missing ApiManager.StopAsync Call
**Problem:** The ASP.NET Core Web API was never being stopped during the main cleanup sequence.

**Evidence:**
- `CleanupBeforeShutdown()` did NOT call `ApiManager.StopAsync()`
- `ApiManager.StopAsync()` was only in `App_Exit()` which never ran (force exit timer fired first)
- 50-60 ASP.NET Core thread pool threads remained active, preventing shutdown

**Fix:**
```csharp
// Added to CleanupBeforeShutdown():
Console.WriteLine("\n[1/5] Stopping API Manager...");
try {
    var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    ApiManager.StopAsync(timeoutSource.Token).Wait(3500);
    Console.WriteLine("[1/5] ✓ API Manager stopped");
}
catch (Exception ex) {
    Console.WriteLine($"[1/5] ✗ API Manager stop error: {ex.Message}");
}
```

---

## Thread Naming Best Practices

### ✅ Good Examples (Named Background Threads)
```csharp
// CNCEventBus
var positionWorker = new Thread(() => PositionWorker(_cancellation.Token)) {
    Name = "EventBus-Position",
    IsBackground = true,
    Priority = ThreadPriority.AboveNormal
};

// SignalRManager
_processingThread = new Thread(ProcessMessageQueue) {
    Name = "SignalR-MessageProcessor",
    IsBackground = true,
    Priority = ThreadPriority.AboveNormal
};
```

### ⚠️ Limitations (ASP.NET Core Thread Pool)
```csharp
// Cannot name or mark as background - managed by ASP.NET Core
_webHost = builder.Build();
_ = Task.Run(async () => {
    await _webHost.RunAsync(_cancellationTokenSource.Token);
});

// MUST explicitly stop via StopAsync() and dispose
await _webHost.StopAsync(cancellationToken);
_webHost.Dispose();
```

---

## How to Verify Shutdown

### Console Output During Shutdown
```
*** SHUTDOWN STARTED - Dumping active threads ***
========== ACTIVE THREADS DUMP ==========
Process: VirtualControlPanel (PID: 12345)
Total Threads: 64
...

[1/5] Stopping API Manager...
[ApiManager] StopAsync called
[ApiManager] Cancellation token cancelled
[ApiManager] Calling StopAsync on web host...
[ApiManager] Web host StopAsync completed
[ApiManager] Disposing web host...
[ApiManager] Web host disposed
[1/5] ✓ API Manager stopped

[2/5] Unsubscribing from CNC events...
[2/5] ✓ CNC events unsubscribed

[3/5] Stopping Centroid Event Bridge...
[3/5] ✓ Centroid Event Bridge stopped

[4/5] Disposing CNC Event Bus...
[4/5] ✓ CNC Event Bus disposed

[5/5] Disconnecting from CNC...
[5/5] ✓ CNC disconnected

*** CLEANUP COMPLETE - Dumping final thread state ***
========== ACTIVE THREADS DUMP ==========
Total Threads: 5  ← Should be much lower now
...
```

### Expected Timeline
- **T+0.0s** - Window closes, cleanup starts
- **T+0.5s** - ApiManager stopping
- **T+2.0s** - All managed threads stopped
- **T+2.5s** - Cleanup complete, process exits naturally
- **T+5.0s** - Force exit timer fires (only if cleanup hangs)

---

## Debugging Commands

### View Thread Dump
The `DumpActiveThreads()` method shows:
- Process name and PID
- Total thread count
- Per-thread details: ID, state, priority, start time, CPU time, wait reason
- Thread pool statistics

### Monitor Shutdown
1. Run application with console visible
2. Close main window
3. Watch for shutdown sequence console output
4. Check if "CLEANUP COMPLETE" appears before "FORCE EXIT"

### If Still Hangs
- Check which step fails (`[1/5]` through `[5/5]`)
- Look for threads still in "Wait" state after cleanup
- Verify ApiManager console messages appear
- Check if `_webHost.StopAsync()` times out (3-second timeout)

---

## Future Improvements

1. **Add Thread Names to DumpActiveThreads Output**
   - Currently shows process threads (native), not managed thread names
   - Consider using `System.Diagnostics.StackTrace` to capture managed thread info

2. **Reduce Force Exit Timeout**
   - Currently 5 seconds - may be reduced to 3 seconds if cleanup is reliable

3. **Add Shutdown Performance Metrics**
   - Track how long each shutdown step takes
   - Log slowest components

4. **Consider Background Thread Pool for API**
   - Investigate if Kestrel can be configured with background threads
   - May require custom thread pool configuration

---

## Summary

The critical fix was **adding `ApiManager.StopAsync()` to `CleanupBeforeShutdown()`**. Without this call, the ASP.NET Core Web API's 50-60 thread pool threads remained active, preventing the application from exiting gracefully. The force exit timer ensures the process terminates even if cleanup hangs, but proper cleanup should complete within 2-3 seconds.

All custom worker threads (CNCEventBus, SignalRManager) are properly named, marked as background threads, and use cancellation tokens for graceful shutdown. The ASP.NET Core thread pool requires explicit StopAsync() calls and cannot be configured as background threads.
