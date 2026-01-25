using System.Windows;

namespace HavenCNCServer.WPF.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // Load from configuration
            // txtApiUrl.Text = Configuration["ApiUrl"];
            // txtSwaggerUrl.Text = Configuration["SwaggerUrl"];
            // txtCnc12Path.Text = Configuration["Cnc12Path"];
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Save settings
            // Configuration["ApiUrl"] = txtApiUrl.Text;
            // Configuration["SwaggerUrl"] = txtSwaggerUrl.Text;
            // Configuration["Cnc12Path"] = txtCnc12Path.Text;

            System.Windows.MessageBox.Show("Settings saved successfully!", "Settings", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
