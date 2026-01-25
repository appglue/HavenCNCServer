using System.Windows;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.WPF.Views
{
    public partial class GCodeTestWindow : Window
    {
        public GCodeTestWindow()
        {
            InitializeComponent();
        }

        private void LoadFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "G-Code Files (*.nc;*.gcode;*.txt)|*.nc;*.gcode;*.txt|All Files (*.*)|*.*",
                Title = "Select G-Code File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    txtGCode.Text = System.IO.File.ReadAllText(openFileDialog.FileName);
                    LogInfo($"Loaded G-Code file: {openFileDialog.FileName}", "GCodeTest");
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error loading file: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    LogError($"Failed to load G-Code file: {ex.Message}", "GCodeTest");
                }
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtGCode.Text))
            {
                System.Windows.MessageBox.Show("Please enter G-Code or load a file first.", "No G-Code", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var lines = txtGCode.Text.Split('\n').Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToArray();
            var stepRunService = new StepRunService();
            var result = stepRunService.StartStepRun(lines);

            if (result.Success)
            {
                System.Windows.MessageBox.Show($"G-Code execution started!\n{result.Message}", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show($"Failed to start execution:\n{result.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Step_Click(object sender, RoutedEventArgs e)
        {
            var stepRunService = new StepRunService();
            var result = stepRunService.ExecuteNextStep();

            if (!result.Success)
            {
                System.Windows.MessageBox.Show($"Step execution issue:\n{result.Message}", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            var stepRunService = new StepRunService();
            stepRunService.Reset();
            System.Windows.MessageBox.Show("Step run stopped and reset.", "Stopped", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
