using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using HavenCNCServer.Centroid.Events;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.WPF.Controls
{
    /// <summary>
    /// WPF control for displaying G-code with line highlighting
    /// </summary>
    public partial class GCodeViewerControl : System.Windows.Controls.UserControl, ICNCEventListener
    {
        private string[] _currentGCode = Array.Empty<string>();
        private int _currentLineNumber = 0;

        public GCodeViewerControl()
        {
            InitializeComponent();
            DataContext = new GCodeViewerViewModel();

            // Register as event listener
            CNCJobInfoListener.AddListener(this);
        }

        public void EventReceived(ICentroidEvent centroidEvent)
        {
            if (centroidEvent is StepExecutionEvent stepEvent)
            {
                Dispatcher.Invoke(() => HandleStepExecution(stepEvent));
            }
            else if (centroidEvent is JobStartedEvent jobStartedEvent)
            {
                Dispatcher.Invoke(() => HandleJobStarted(jobStartedEvent));
            }
            else if (centroidEvent is JobCompletedEvent jobCompletedEvent)
            {
                Dispatcher.Invoke(() => HandleJobCompleted(jobCompletedEvent));
            }
            else if (centroidEvent is JobInfoEvent jobEvent)
            {
                Dispatcher.Invoke(() => UpdateGCodeDisplayFallback(jobEvent));
            }
        }

        private void HandleStepExecution(StepExecutionEvent stepEvent)
        {
            try
            {
                _currentLineNumber = stepEvent.LineNumber;

                var statusText = stepEvent.Status switch
                {
                    StepExecutionStatus.AboutToExecute => "About to execute",
                    StepExecutionStatus.Executing => "Executing",
                    StepExecutionStatus.Completed => "Completed",
                    StepExecutionStatus.Failed => "Failed",
                    StepExecutionStatus.Skipped => "Skipped",
                    _ => "Unknown"
                };

                if (DataContext is GCodeViewerViewModel vm)
                {
                    vm.CurrentJobInfo = $"Step Run: {stepEvent.JobId} - Line {stepEvent.LineNumber}/{stepEvent.TotalLines} ({statusText})";
                }

                if (_currentGCode.Length > 0)
                {
                    DisplayGCodeWithHighlight();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error handling step execution event: {ex.Message}", "GCodeDisplay");
            }
        }

        private void HandleJobStarted(JobStartedEvent jobStartedEvent)
        {
            try
            {
                LoadGCodeForDisplay(jobStartedEvent.GCodeLines);

                if (DataContext is GCodeViewerViewModel vm)
                {
                    vm.CurrentJobInfo = $"Job Started: {jobStartedEvent.JobId} ({jobStartedEvent.TotalLines} lines)";
                }

                _currentLineNumber = 1;
                DisplayGCodeWithHighlight();

                LogInfo($"Job started event handled: {jobStartedEvent.JobId}", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Error handling job started event: {ex.Message}", "GCodeDisplay");
            }
        }

        private void HandleJobCompleted(JobCompletedEvent jobCompletedEvent)
        {
            try
            {
                var status = jobCompletedEvent.Success ? "COMPLETED" : "FAILED";
                var duration = jobCompletedEvent.Duration.TotalSeconds.ToString("F1");

                if (DataContext is GCodeViewerViewModel vm)
                {
                    vm.CurrentJobInfo = $"Job {status}: {jobCompletedEvent.JobId} ({duration}s, {jobCompletedEvent.LinesExecuted} lines)";

                    if (!jobCompletedEvent.Success && !string.IsNullOrEmpty(jobCompletedEvent.ErrorMessage))
                    {
                        vm.CurrentJobInfo += $" - Error: {jobCompletedEvent.ErrorMessage}";
                    }
                }

                LogInfo($"Job completed event handled: {jobCompletedEvent.JobId} - Success: {jobCompletedEvent.Success}", "GCodeDisplay");
            }
            catch (Exception ex)
            {
                LogError($"Error handling job completed event: {ex.Message}", "GCodeDisplay");
            }
        }

        private void UpdateGCodeDisplayFallback(JobInfoEvent jobEvent)
        {
            try
            {
                _currentLineNumber = jobEvent.LineNumber;

                if (DataContext is GCodeViewerViewModel vm)
                {
                    vm.CurrentJobInfo = $"Current Job: Line {_currentLineNumber} - {jobEvent.JobName ?? "Unknown"}";
                }

                if (_currentGCode.Length > 0)
                {
                    DisplayGCodeWithHighlight();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error updating G-code display (fallback): {ex.Message}", "GCodeDisplay");
            }
        }

        public void LoadGCodeForDisplay(string[] gCodeLines)
        {
            try
            {
                _currentGCode = gCodeLines ?? Array.Empty<string>();
                _currentLineNumber = 0;
                DisplayGCodeWithHighlight();
            }
            catch (Exception ex)
            {
                LogError($"Error loading G-code: {ex.Message}", "GCodeDisplay");
            }
        }

        public void ClearGCode()
        {
            try
            {
                _currentGCode = Array.Empty<string>();
                _currentLineNumber = 0;
                txtGCode.Document.Blocks.Clear();

                if (DataContext is GCodeViewerViewModel vm)
                {
                    vm.CurrentJobInfo = "No active job";
                }
            }
            catch (Exception ex)
            {
                LogError($"Error clearing G-code: {ex.Message}", "GCodeDisplay");
            }
        }

        private void DisplayGCodeWithHighlight()
        {
            try
            {
                txtGCode.Document.Blocks.Clear();

                if (_currentGCode.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < _currentGCode.Length; i++)
                {
                    var lineNumber = i + 1;
                    var line = _currentGCode[i];
                    var displayLine = $"{lineNumber:D4}: {line}";

                    var paragraph = new Paragraph(new Run(displayLine));
                    paragraph.Margin = new Thickness(0);

                    if (lineNumber == _currentLineNumber)
                    {
                        paragraph.Foreground = System.Windows.Media.Brushes.Red;
                        paragraph.Background = System.Windows.Media.Brushes.Yellow;
                    }
                    else
                    {
                        paragraph.Foreground = System.Windows.Media.Brushes.Black;
                    }

                    txtGCode.Document.Blocks.Add(paragraph);
                }

                ScrollToCurrentLine();
            }
            catch (Exception ex)
            {
                LogError($"Error displaying G-code with highlight: {ex.Message}", "GCodeDisplay");
            }
        }

        private void ScrollToCurrentLine()
        {
            try
            {
                if (_currentLineNumber > 0 && _currentGCode.Length > 0)
                {
                    var targetBlock = txtGCode.Document.Blocks.ElementAtOrDefault(_currentLineNumber - 1);
                    if (targetBlock != null)
                    {
                        targetBlock.BringIntoView();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error scrolling to current line: {ex.Message}", "GCodeDisplay");
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CNCJobInfoListener.RemoveListener(this);
        }
    }

    public partial class GCodeViewerViewModel : ObservableObject
    {
        [ObservableProperty]
        private string currentJobInfo = "No active job";
    }
}
