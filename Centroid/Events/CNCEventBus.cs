using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Centroid.Events
{
    /// <summary>
    /// Centralized event bus for distributing CNC events to subscribers using channels
    /// Eliminates lock contention by using lock-free channels and background worker threads
    /// </summary>
    public sealed class CNCEventBus : IDisposable
    {
        private static readonly Lazy<CNCEventBus> _instance =
            new Lazy<CNCEventBus>(() => new CNCEventBus());

        public static CNCEventBus Instance => _instance.Value;

        // Channels for different event types
        private readonly Channel<DROEvent> _positionChannel;
        private readonly Channel<LogEvent> _logChannel;
        private readonly Channel<ICentroidEvent> _messageChannel;

        // Worker threads
        private readonly List<Thread> _workerThreads;
        private readonly CancellationTokenSource _cancellation;

        // Subscribers (thread-safe with unsubscribe support using ConcurrentDictionary)
        // Value is unused (byte) - we only care about the key set
        private readonly ConcurrentDictionary<IEventSubscriber, byte> _subscribers;

        // Metrics
        private long _positionEventsPublished;
        private long _logEventsPublished;
        private long _messageEventsPublished;
        private long _positionEventsDropped;
        private readonly DateTime _startTime;

        private CNCEventBus()
        {
            // Position: Drop oldest, keep latest (capacity 1)
            // High-frequency updates where only the latest value matters
            _positionChannel = Channel.CreateBounded<DROEvent>(
                new BoundedChannelOptions(1)
                {
                    FullMode = BoundedChannelFullMode.DropOldest
                });

            // Logs: Bounded with backpressure (capacity 1000)
            // Medium frequency, all messages are important for audit trail
            _logChannel = Channel.CreateBounded<LogEvent>(
                new BoundedChannelOptions(1000)
                {
                    FullMode = BoundedChannelFullMode.Wait
                });

            // Messages: Bounded with backpressure (capacity 500)
            // Variable frequency, all CNC messages are important
            _messageChannel = Channel.CreateBounded<ICentroidEvent>(
                new BoundedChannelOptions(500)
                {
                    FullMode = BoundedChannelFullMode.Wait
                });

            _subscribers = new ConcurrentDictionary<IEventSubscriber, byte>();
            _cancellation = new CancellationTokenSource();
            _workerThreads = new List<Thread>();
            _startTime = DateTime.Now;

            StartWorkerThreads();

            LogSuccess("CNCEventBus initialized with channel-based architecture", "EventBus");
        }

        #region Producer API (called by CentroidEventBridge)

        /// <summary>
        /// Publish a position/DRO update (non-blocking, drops old if channel full)
        /// </summary>
        public void PublishPosition(DROEvent position)
        {
            try
            {
                var written = _positionChannel.Writer.TryWrite(position);
                Interlocked.Increment(ref _positionEventsPublished);

                if (!written)
                {
                    // Channel was full, old value was dropped (expected behavior)
                    Interlocked.Increment(ref _positionEventsDropped);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error publishing position event: {ex.Message}", "EventBus");
            }
        }

        /// <summary>
        /// Publish a log message (async with backpressure)
        /// </summary>
        public async Task PublishLogAsync(LogEvent log)
        {
            try
            {
                await _logChannel.Writer.WriteAsync(log);
                Interlocked.Increment(ref _logEventsPublished);
            }
            catch (Exception ex)
            {
                LogError($"Error publishing log event: {ex.Message}", "EventBus");
            }
        }

        /// <summary>
        /// Publish a log message (synchronous version for backwards compatibility)
        /// </summary>
        public void PublishLog(LogEvent log)
        {
            try
            {
                // Use TryWrite for synchronous publishing
                if (!_logChannel.Writer.TryWrite(log))
                {
                    // Channel full, fall back to async
                    _ = PublishLogAsync(log);
                }
                else
                {
                    Interlocked.Increment(ref _logEventsPublished);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error publishing log event: {ex.Message}", "EventBus");
            }
        }

        /// <summary>
        /// Publish a CNC message (async with backpressure)
        /// </summary>
        public async Task PublishMessageAsync(ICentroidEvent message)
        {
            try
            {
                await _messageChannel.Writer.WriteAsync(message);
                Interlocked.Increment(ref _messageEventsPublished);
            }
            catch (Exception ex)
            {
                LogError($"Error publishing message event: {ex.Message}", "EventBus");
            }
        }

        /// <summary>
        /// Publish a CNC message (synchronous version for backwards compatibility)
        /// </summary>
        public void PublishMessage(ICentroidEvent message)
        {
            try
            {
                // Use TryWrite for synchronous publishing
                if (!_messageChannel.Writer.TryWrite(message))
                {
                    // Channel full, fall back to async
                    _ = PublishMessageAsync(message);
                }
                else
                {
                    Interlocked.Increment(ref _messageEventsPublished);
                }
            }
            catch (Exception ex)
            {
                LogError($"Error publishing message event: {ex.Message}", "EventBus");
            }
        }

        #endregion

        #region Consumer API (called by WPF components, SignalR, etc.)

        /// <summary>
        /// Subscribe to receive CNC events based on EventTypeFlags
        /// </summary>
        public void Subscribe(IEventSubscriber subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (_subscribers.TryAdd(subscriber, 0))
            {
                LogInfo($"Subscriber registered: {subscriber.GetType().Name} (wants: {subscriber.GetSubscribedEvents()})", "EventBus");
            }
            else
            {
                LogWarning($"Subscriber already registered: {subscriber.GetType().Name}", "EventBus");
            }
        }

        /// <summary>
        /// Unsubscribe from receiving CNC events
        /// </summary>
        public void Unsubscribe(IEventSubscriber subscriber)
        {
            if (subscriber == null)
                return;

            if (_subscribers.TryRemove(subscriber, out _))
            {
                LogInfo($"Subscriber removed: {subscriber.GetType().Name}", "EventBus");
            }
            else
            {
                LogWarning($"Subscriber not found for removal: {subscriber.GetType().Name}", "EventBus");
            }
        }

        /// <summary>
        /// Get the number of active subscribers
        /// </summary>
        public int GetSubscriberCount() => _subscribers.Count;

        #endregion

        #region Worker Threads

        private void StartWorkerThreads()
        {
            // Position worker (high priority) - dedicated thread for DRO updates
            var positionWorker = new Thread(() => PositionWorker(_cancellation.Token))
            {
                Name = "EventBus-Position",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            positionWorker.Start();
            _workerThreads.Add(positionWorker);

            // Log/Message workers (normal priority) - 2 threads to handle both channels
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

            LogInfo($"Started {_workerThreads.Count} worker threads", "EventBus");
        }

        /// <summary>
        /// Worker thread for position/DRO updates (high priority)
        /// </summary>
        private async void PositionWorker(CancellationToken ct)
        {
            LogInfo("Position worker started", "EventBus");

            try
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
                            LogError($"Subscriber {subscriber.GetType().Name} error in OnPositionUpdate: {ex.Message}", "EventBus");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogInfo("Position worker cancelled", "EventBus");
            }
            catch (Exception ex)
            {
                LogError($"Position worker error: {ex.Message}", "EventBus");
            }
        }

        /// <summary>
        /// Worker thread for log and message events (normal priority)
        /// Interleaves reading from both channels
        /// </summary>
        private async void MessageWorker(CancellationToken ct)
        {
            LogInfo("Message worker started", "EventBus");

            try
            {
                // Use Task.WhenAny to interleave reading from both channels
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
                            catch (Exception ex)
                            {
                                LogError($"Subscriber {subscriber.GetType().Name} error in OnLogMessage: {ex.Message}", "EventBus");
                            }
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
                            catch (Exception ex)
                            {
                                LogError($"Subscriber {subscriber.GetType().Name} error in OnCNCMessage: {ex.Message}", "EventBus");
                            }
                        }
                        messageTask = _messageChannel.Reader.ReadAsync(ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogInfo("Message worker cancelled", "EventBus");
            }
            catch (Exception ex)
            {
                LogError($"Message worker error: {ex.Message}", "EventBus");
            }
        }

        #endregion

        #region Metrics

        /// <summary>
        /// Get event bus metrics
        /// </summary>
        public EventBusMetrics GetMetrics()
        {
            var uptime = DateTime.Now - _startTime;

            return new EventBusMetrics
            {
                PositionEventsPublished = Interlocked.Read(ref _positionEventsPublished),
                LogEventsPublished = Interlocked.Read(ref _logEventsPublished),
                MessageEventsPublished = Interlocked.Read(ref _messageEventsPublished),
                PositionEventsDropped = Interlocked.Read(ref _positionEventsDropped),
                SubscriberCount = _subscribers.Count,
                PositionChannelCount = _positionChannel.Reader.Count,
                LogChannelCount = _logChannel.Reader.Count,
                MessageChannelCount = _messageChannel.Reader.Count,
                UptimeSeconds = uptime.TotalSeconds
            };
        }

        /// <summary>
        /// Log metrics summary
        /// </summary>
        public void LogMetrics()
        {
            var metrics = GetMetrics();
            LogInfo($"EventBus Metrics: Pos={metrics.PositionEventsPublished} (dropped={metrics.PositionEventsDropped}), " +
                   $"Log={metrics.LogEventsPublished}, Msg={metrics.MessageEventsPublished}, " +
                   $"Subscribers={metrics.SubscriberCount}, Uptime={metrics.UptimeSeconds:F1}s", "EventBus");
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            LogInfo("Shutting down CNCEventBus...", "EventBus");

            // Cancel worker threads
            _cancellation.Cancel();

            // Complete channels (no more writes)
            _positionChannel.Writer.Complete();
            _logChannel.Writer.Complete();
            _messageChannel.Writer.Complete();

            // Wait for workers to finish
            foreach (var thread in _workerThreads)
            {
                if (!thread.Join(TimeSpan.FromSeconds(5)))
                {
                    LogWarning($"Worker thread {thread.Name} did not exit gracefully", "EventBus");
                }
            }

            // Log final metrics
            LogMetrics();

            _cancellation.Dispose();

            LogSuccess("CNCEventBus disposed", "EventBus");
        }

        #endregion
    }

    /// <summary>
    /// Metrics for the event bus
    /// </summary>
    public class EventBusMetrics
    {
        public long PositionEventsPublished { get; set; }
        public long LogEventsPublished { get; set; }
        public long MessageEventsPublished { get; set; }
        public long PositionEventsDropped { get; set; }
        public int SubscriberCount { get; set; }
        public int PositionChannelCount { get; set; }
        public int LogChannelCount { get; set; }
        public int MessageChannelCount { get; set; }
        public double UptimeSeconds { get; set; }
    }
}
