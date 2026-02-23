using System;
using System.Windows;

namespace HavenCNCServer.WPF.Views
{
    public partial class BrowserWindow : Window
    {
        public BrowserWindow()
        {
            InitializeComponent();
            Loaded += BrowserWindow_Loaded;
        }

        private void BrowserWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get screen dimensions
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            // Calculate screen aspect ratio
            double screenAspectRatio = screenWidth / screenHeight;
            const double targetAspectRatio = 16.0 / 9.0;

            // If screen is wider than 16:9, constrain to 16:9 at full height
            if (screenAspectRatio > targetAspectRatio)
            {
                // Use full height
                this.Height = screenHeight;
                this.Width = screenHeight * targetAspectRatio;

                // Center horizontally
                this.Left = (screenWidth - this.Width) / 2;
                this.Top = 0;

                this.WindowState = WindowState.Normal;
            }
            else
            {
                // Screen is 16:9 or narrower, go full screen
                this.WindowState = WindowState.Maximized;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            webView.Reload();
        }
    }
}
