using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HavenCNCServer.Centroid.Events;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;
using Color = System.Windows.Media.Color;

namespace HavenCNCServer.WPF.Controls
{
    /// <summary>
    /// Message severity levels for color coding
    /// </summary>
    public enum MessageSeverity
    {
        Normal,
        Info,
        Success,
        Warning,
        Error
    }

    /// <summary>
    /// WPF control for displaying CNC messages
    /// </summary>
    public partial class MessageDisplayControl : System.Windows.Controls.UserControl, IEventSubscriber
    {
        private const int MaxMessages = 5000;
        private int _currentMessageCount = 0;

        public MessageDisplayControl()
        {
            InitializeComponent();
            DataContext = new MessageDisplayViewModel(this);

            // Initialize with welcome message
            AppendColoredMessage("=== CNC Message Monitor ===", Colors.DarkBlue);
            AppendColoredMessage("Waiting for CNC messages...", Colors.Gray);
            AppendColoredMessage("", Colors.Black);

            // Subscribe to CNCEventBus for messages and job info
            CNCEventBus.Instance.Subscribe(this);
        }

        public EventTypeFlags GetSubscribedEvents()
        {
            return EventTypeFlags.Messages; // Only subscribe to CNC messages and job info
        }

        public void OnPositionUpdate(DROEvent position)
        {
            // Not interested in position updates
        }

        public void OnLogMessage(LogEvent log)
        {
            // Not interested in log messages
        }

        public void OnCNCMessage(ICentroidEvent message)
        {
            if (message is MessageEvent messageEvent)
            {
                Dispatcher.Invoke(() => AddMessage(messageEvent));
            }
            else if (message is JobInfoEvent jobEvent)
            {
                Dispatcher.Invoke(() => AddJobInfoMessage(jobEvent));
            }
            else if (message is StepExecutionEvent stepEvent)
            {
                if (stepEvent.Status == StepExecutionStatus.Failed ||
                    stepEvent.Status == StepExecutionStatus.Completed ||
                    stepEvent.IsLastStep)
                {
                    Dispatcher.Invoke(() => AddStepExecutionMessage(stepEvent));
                }
            }
        }

        private void AddMessage(MessageEvent messageEvent)
        {
            try
            {
                var severity = GetMessageSeverity(messageEvent.EventType);
                var timestamp = messageEvent.Timestamp.ToString("HH:mm:ss.fff");
                var eventCode = messageEvent.EventCode > 0 ? $"[{messageEvent.EventCode}]" : "";
                var messageText = $"[{timestamp}] {eventCode} ({messageEvent.EventType}) {messageEvent.Message}";

                AppendColoredMessage(messageText, GetColorForSeverity(severity));
                _currentMessageCount++;

                if (_currentMessageCount > MaxMessages)
                {
                    TrimOldMessages();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error adding CNC message to display: {ex.Message}", "MessageDisplay");
            }
        }

        private void AddJobInfoMessage(JobInfoEvent jobEvent)
        {
            try
            {
                var timestamp = jobEvent.Timestamp.ToString("HH:mm:ss.fff");
                var levelInfo = $" (Level {jobEvent.StackLevel})";
                var messageText = $"[{timestamp}] JOB: Line {jobEvent.LineNumber}{levelInfo} - {jobEvent.Message}";

                AppendColoredMessage(messageText, Colors.Blue);
                _currentMessageCount++;

                if (_currentMessageCount > MaxMessages)
                {
                    TrimOldMessages();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error adding job info message to display: {ex.Message}", "MessageDisplay");
            }
        }

        private void AddStepExecutionMessage(StepExecutionEvent stepEvent)
        {
            try
            {
                var timestamp = stepEvent.Timestamp.ToString("HH:mm:ss.fff");
                var progressInfo = stepEvent.TotalLines > 0 ? $" ({stepEvent.LineNumber}/{stepEvent.TotalLines})" : "";
                var statusInfo = $" [{stepEvent.Status}]";
                var messageText = $"[{timestamp}] STEP{progressInfo}{statusInfo}: {stepEvent.CurrentLine}";

                AppendColoredMessage(messageText, Colors.DarkGreen);
                _currentMessageCount++;

                if (_currentMessageCount > MaxMessages)
                {
                    TrimOldMessages();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error adding step execution message to display: {ex.Message}", "MessageDisplay");
            }
        }

        public void AppendColoredMessage(string message, Color color)
        {
            var paragraph = new Paragraph(new Run(message)
            {
                Foreground = new SolidColorBrush(color)
            });
            paragraph.Margin = new Thickness(0);
            txtMessages.Document.Blocks.Add(paragraph);
            txtMessages.ScrollToEnd();
        }

        private Color GetColorForSeverity(MessageSeverity severity)
        {
            return severity switch
            {
                MessageSeverity.Error => Colors.Red,
                MessageSeverity.Warning => Colors.Orange,
                MessageSeverity.Success => Colors.Green,
                MessageSeverity.Info => Colors.Blue,
                MessageSeverity.Normal => Colors.Black,
                _ => Colors.Black
            };
        }

        private MessageSeverity GetMessageSeverity(MessageEventType eventType)
        {
            return eventType switch
            {
                MessageEventType.SystemFault or
                MessageEventType.AxisFault or
                MessageEventType.LimitError or
                MessageEventType.ProbeError or
                MessageEventType.CommunicationError or
                MessageEventType.StartupError or
                MessageEventType.MiscellaneousError => MessageSeverity.Error,

                MessageEventType.SyntaxError or
                MessageEventType.GCodeError or
                MessageEventType.ParameterError or
                MessageEventType.CutterCompensationError or
                MessageEventType.ParameterSettingError or
                MessageEventType.CannedCycleError or
                MessageEventType.ScalingError => MessageSeverity.Warning,

                MessageEventType.JobCompleted => MessageSeverity.Success,
                MessageEventType.JobStarted or
                MessageEventType.StatusMessage => MessageSeverity.Info,

                _ => MessageSeverity.Normal
            };
        }

        private void TrimOldMessages()
        {
            while (txtMessages.Document.Blocks.Count > MaxMessages)
            {
                txtMessages.Document.Blocks.Remove(txtMessages.Document.Blocks.FirstBlock);
            }
            _currentMessageCount = txtMessages.Document.Blocks.Count;
        }

        public void ClearMessages()
        {
            txtMessages.Document.Blocks.Clear();
            _currentMessageCount = 0;
            AppendColoredMessage("=== CNC Message Monitor ===", Colors.DarkBlue);
            AppendColoredMessage("[Messages cleared]", Colors.Gray);
            AppendColoredMessage("", Colors.Black);
            LogInfo("🗑️ Message display cleared", "MessageDisplay");
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CNCEventBus.Instance.Unsubscribe(this);
        }
    }

    public partial class MessageDisplayViewModel : ObservableObject
    {
        private readonly MessageDisplayControl _control;

        public MessageDisplayViewModel(MessageDisplayControl control)
        {
            _control = control;
        }

        [RelayCommand]
        private void Clear()
        {
            _control.ClearMessages();
        }
    }
}
