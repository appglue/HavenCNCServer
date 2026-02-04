# Event Distribution Refactoring Plan

## Problem Analysis

### Current Issues
1. **Thread Contention**: Multiple threads competing for locks when accessing shared event data
2. **Blocking Operations**: Event distribution can block producers (CNC pipe listener)
3. **Inconsistent Patterns**: Different handling for different event types
4. **Lock Hierarchies**: Complex locking in `CNCJobInfoListener` with multiple listeners

### Current Architecture
```
CentroidAPI (CNCPipe)
    │
    ├─> CNCJobInfoListener (static singleton)
    │       ├─> OnMessageReceived (locks)
    │       ├─> List<ICNCEventListener> (locks for iteration)
    │       │       ├─> SignalREventListener (has own queue + thread)
    │       │       ├─> WPF Controls (MessageDisplayControl, GCodeViewerControl, CoordinateDisplayControl)
    │       │       ├─> WinForms Components (MessageDisplayComponent - TO BE REMOVED)
    │       │       └─> MachinePositionService
    │       └─> Stored messages List (locks)
    │
    └─> Direct calls in controllers
```

### Three Data Types Identified

1. **Position Updates (DRO)**
   - High frequency (~100ms intervals)
   - Only latest value matters
   - Should use ring buffer or "latest value" pattern

2. **Log Messages**
   - Medium frequency
   - All messages important (audit trail)
   - Should use queue with persistence

3. **CNC12/Internal Messages**
   - Variable frequency
   - All messages important
   - Should use queue with event-driven delivery

## Proposed Solution: Centralized Event Bus

### Architecture Overview
```
CentroidAPI (CNCPipe)
    │
    └─> CNCEventBus (singleton)
            ├─> Position Channel (latest-value buffer)
            ├─> Log Channel (FIFO queue)
            └─> Message Channel (FIFO queue)
                    │
                    ├─> Background Worker Threads (3-5)
                    │       └─> Pull from channels, distribute to subscribers
                    │
                    └─> Subscribers
                            ├─> SignalREventListener
                            ├─> UI Components
                            └─> Logging Services
```

### Key Benefits
✅ **No Lock Contention**: Producers push to lock-free channels  
✅ **Backpressure Control**: Bounded channels prevent memory issues  
✅ **Type-Specific Optimization**: Each data type gets optimal handling  
✅ **Background Processing**: Worker threads handle distribution async  
✅ **Simplified Code**: Single point of entry, clear separation

## Design Details

### 1. CNCEventBus (Singleton)

**Responsibilities:**
- Receive events from CNC pipe
- Distribute to appropriate channel based on type
- Manage worker threads for distribution
- Handle subscriber registration/unregistration

**Channels:**

```csharp
// Position channel - only latest value matters
System.Threading.Channels.Channel<DROEvent> _positionChannel
    - BoundedCapacity: 1 (drop old, keep latest)
    - Full mode: DropOldest

// Log channel - preserve all messages
System.Threading.Channels.Channel<LogEvent> _logChannel
    - BoundedCapacity: 1000
    - Full mode: Wait (backpressure)

// Message channel - preserve all CNC messages
System.Threading.Channels.Channel<MessageEvent> _messageChannel
    - BoundedCapacity: 500
    - Full mode: Wait (backpressure)
```

### 2. Worker Thread Pattern

**Count:** 3-5 background threads
- 1 thread for position updates (high priority)
- 2-4 threads for log/message distribution (normal priority)

**Responsibilities:**
- Read from channels (non-blocking)
- Distribute to registered subscribers
- Handle subscriber exceptions (don't crash thread)
- Report metrics (events/sec, queue depth)

### 3. Subscriber Interface

```csharp
public interface IEventSubscriber
{
    // Receive position update (latest value only)
    void OnPositionUpdate(DROEvent position);
    
    // Receive log message (all messages)
    void OnLogMessage(LogEvent log);
    
    // Receive CNC message (all messages)
    void OnCNCMessage(ICentroidEvent message);
    
    // Optional: Allow subscribers to specify which events they want
    EventTypeFlags GetSubscribedEvents();
}

[Flags]
public enum EventTypeFlags
{
    None = 0,
    Position = 1,
    Logs = 2,
    Messages = 4,
    All = Position | Logs | Messages
}
```

### 4. Migration Strategy

#### Phase 1: Create Event Bus Infrastructure
- [ ] Create `CNCEventBus` singleton class
- [ ] Implement channel-based architecture
- [ ] Create worker thread pool
- [ ] Implement subscriber interface
- [ ] Add metrics/monitoring

#### Phase 2: Migrate Producers
- [ ] Modify `CNCJobInfoListener.OnMessageReceived` to push to event bus
- [ ] Remove direct listener iteration
- [ ] Keep stored message list for now (legacy support)

#### Phase 3: Migrate Consumers
- [ ] **Remove WinForms components first** (MainForm.cs, BrowserForm.cs, Components/MessageDisplayComponent.cs, etc.)
- [ ] Update `SignalREventListener` to subscribe to event bus
- [ ] Update WPF controls (WPF/Controls/MessageDisplayControl.xaml.cs, GCodeViewerControl, CoordinateDisplayControl)
- [ ] Update `MachinePositionService`
- [ ] Remove old listener registration code from remaining components

#### Phase 4: Cleanup
- [x] Remove `List<ICNCEventListener>` from CNCJobInfoListener
- [x] Remove `AddListener/RemoveListener` methods
- [x] Remove stored messages system entirely (unused)
- [x] Simplify locking (no more message storage or listener list)
- [x] Rename CNCJobInfoListener → CentroidEventBridge
- [ ] Performance testing

## Implementation Code Sketch

### CNCEventBus.cs

```csharp
public sealed class CNCEventBus : IDisposable
{
    private static readonly Lazy<CNCEventBus> _instance = 
        new Lazy<CNCEventBus>(() => new CNCEventBus());
    
    public static CNCEventBus Instance => _instance.Value;
    
    // Channels
    private readonly Channel<DROEvent> _positionChannel;
    private readonly Channel<LogEvent> _logChannel;
    private readonly Channel<MessageEvent> _messageChannel;
    
    // Worker threads
    private readonly List<Thread> _workerThreads;
    private readonly CancellationTokenSource _cancellation;
    
    // Subscribers (thread-safe with unsubscribe support)
    private readonly ConcurrentDictionary<IEventSubscriber, byte> _subscribers;
    
    private CNCEventBus()
    {
        // Position: Drop oldest, keep latest (capacity 1)
        _positionChannel = Channel.CreateBounded<DROEvent>(
            new BoundedChannelOptions(1) 
            { 
                FullMode = BoundedChannelFullMode.DropOldest 
            });
        
        // Logs: Bounded with backpressure
        _logChannel = Channel.CreateBounded<LogEvent>(
            new BoundedChannelOptions(1000) 
            { 
                FullMode = BoundedChannelFullMode.Wait 
            });
        
        // Messages: Bounded with backpressure
        _messageChannel = Channel.CreateBounded<MessageEvent>(
            new BoundedChannelOptions(500) 
            { 
                FullMode = BoundedChannelFullMode.Wait 
            });
        
        _subscribers = new ConcurrentDictionary<IEventSubscriber, byte>();
        _cancellation = new CancellationTokenSource();
        _workerThreads = new List<Thread>();
        
        StartWorkerThreads();
    }
    
    // Producer API (called by CNCJobInfoListener)
    public void PublishPosition(DROEvent position)
    {
        _positionChannel.Writer.TryWrite(position); // Non-blocking
    }
    
    public async Task PublishLogAsync(LogEvent log)
    {
        await _logChannel.Writer.WriteAsync(log); // With backpressure
    }
    
    public async Task PublishMessageAsync(MessageEvent message)
    {
        await _messageChannel.Writer.WriteAsync(message); // With backpressure
    }
    
    // Consumer API (called by WPF components, SignalR, etc.)
    public void Subscribe(IEventSubscriber subscriber)
    {
        _subscribers.TryAdd(subscriber, 0);
    }
    
    public void Unsubscribe(IEventSubscriber subscriber)
    {
        // Use ConcurrentDictionary for proper unsubscribe support
        _subscribers.TryRemove(subscriber, out _);
    }
    
    private void StartWorkerThreads()
    {
        // Position worker (high priority)
        var positionWorker = new Thread(() => PositionWorker(_cancellation.Token))
        {
            Name = "EventBus-Position",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal
        };
        positionWorker.Start();
        _workerThreads.Add(positionWorker);
        
        // Log/Message workers (normal priority)
        for (int i = 0; i < 2; i++)
        {
            var messageWorker = new Thread(() => MessageWorker(_cancellation.Token))
            {
                Name = $"EventBus-Messages-{i}",
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };
            messageWorker.Start();
            _workerThreads.Add(messageWorker);
        }
    }
    
    private async void PositionWorker(CancellationToken ct)
    {
        await foreach (var position in _positionChannel.Reader.ReadAllAsync(ct))
        {
            foreach (var subscriber in _subscribers.Keys)
            {
                try
                {
                    if (subscriber.GetSubscribedEvents().HasFlag(EventTypeFlags.Position))
                    {
                        subscriber.OnPositionUpdate(position);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Subscriber error: {ex.Message}", "EventBus");
                }
            }
        }
    }
    
    private async void MessageWorker(CancellationToken ct)
    {
        // Interleave reading from both channels
        var logTask = _logChannel.Reader.ReadAsync(ct);
        var messageTask = _messageChannel.Reader.ReadAsync(ct);
        
        while (!ct.IsCancellationRequested)
        {
            var completed = await Task.WhenAny(logTask.AsTask(), messageTask.AsTask());
            
            if (completed == logTask.AsTask() && logTask.IsCompleted)
            {
                var log = await logTask;
                foreach (var subscriber in _subscribers.Keys)
                {
                    try
                    {
                        if (subscriber.GetSubscribedEvents().HasFlag(EventTypeFlags.Logs))
                        {
                            subscriber.OnLogMessage(log);
                        }
                    }
                    catch { }
                }
                logTask = _logChannel.Reader.ReadAsync(ct);
            }
            else if (completed == messageTask.AsTask() && messageTask.IsCompleted)
            {
                var message = await messageTask;
                foreach (var subscriber in _subscribers.Keys)
                {
                    try
                    {
                        if (subscriber.GetSubscribedEvents().HasFlag(EventTypeFlags.Messages))
                        {
                            subscriber.OnCNCMessage(message);
                        }
                    }
                    catch { }
                }
                messageTask = _messageChannel.Reader.ReadAsync(ct);
            }
        }
    }
    
    public void Dispose()
    {
        _cancellation.Cancel();
        
        // Complete channels
        _positionChannel.Writer.Complete();
        _logChannel.Writer.Complete();
        _messageChannel.Writer.Complete();
        
        // Wait for workers
        foreach (var thread in _workerThreads)
        {
            thread.Join(TimeSpan.FromSeconds(5));
        }
        
        _cancellation.Dispose();
    }
}
```

## Performance Considerations

### Metrics to Monitor
- Queue depths (per channel)
- Events per second (per type)
- Subscriber processing time
- Dropped events (position channel)
- Worker thread CPU usage

### Expected Improvements
- **Reduced Lock Contention**: 90%+ reduction in lock time
- **Improved Throughput**: Higher event processing rate
- **Lower Latency**: Position updates delivered faster
- **Better UI Responsiveness**: No blocking on UI thread

## Testing Strategy

### Unit Tests
- [ ] Channel overflow behavior (position drops oldest)
- [ ] Backpressure handling (log/message channels)
- [ ] Subscriber registration/unregistration
- [ ] Worker thread lifecycle

### Integration Tests
- [ ] High-frequency position updates (1000/sec)
- [ ] Mixed event types
- [ ] Subscriber exceptions don't crash workers
- [ ] Graceful shutdown under load

### Load Tests
- [ ] 10,000 events/sec sustained
- [ ] 100 concurrent subscribers
- [ ] Memory leak detection (24hr run)

## Rollback Plan

If issues arise:
1. Feature flag to switch back to old listener pattern
2. Keep old code commented out initially
3. Gradual rollout (position only → all events)

## Timeline Estimate

- **Phase 1**: 4-6 hours (infrastructure)
- **Phase 2**: 2-3 hours (producer migration)
- **Phase 3**: 4-6 hours (consumer migration)
- **Phase 4**: 2-3 hours (cleanup)
- **Testing**: 3-4 hours

**Total**: 15-22 hours

## Questions to Resolve

1. ✅ **Subscriber Storage**: Use ConcurrentDictionary<IEventSubscriber, byte> for unsubscribe support
2. ✅ **Event History**: REMOVE - stored messages are unused (no API consumers)
3. ✅ **SignalR Integration**: KEEP existing BlockingCollection in SignalREventListener (already working well)
4. ✅ **WPF UI Threading**: Subscribers handle Dispatcher.Invoke (standard WPF pattern)
5. ✅ **WinForms**: REMOVE - only using WPF for server monitoring
6. ⏳ **Metrics/Monitoring**: Built-in or separate service?

## Deployment Architecture

**Server Machine:**
- ASP.NET Core Web API (SignalR hub)
- WPF Application (local server monitoring UI)
- Both subscribe to CNCEventBus
- WPF controls marshal to UI thread via Dispatcher.Invoke

**Client Machines:**
- Web browser (React/etc.)
- Connects to SignalR
- No direct CNC connection

## Next Steps

1. ✅ Review and approve architecture
2. ✅ Answer open questions
3. Create feature branch
4. Implement Phase 1 (infrastructure)
5. Test with position events only
6. Proceed with remaining phases
