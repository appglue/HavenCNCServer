using System.Windows;

namespace HavenCNCServer.WPF.Views
{
    public partial class BrowserWindow : Window
    {
        public BrowserWindow()
        {
            InitializeComponent();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            webView.Reload();
        }
    }
}
