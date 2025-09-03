using Microsoft.Web.WebView2.WinForms;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HavenCNCServer
{
    public partial class BrowserForm : Form
    {
        private WebView2 webView;
        private string _url;
        private bool _isFullScreen = false;

        public BrowserForm(string url)
        {
            _url = url;
            InitializeComponent();
            SetupForm();
        }

        private void SetupForm()
        {
            // Form properties
            this.Text = "HavenCNC Control Interface";
            this.BackColor = Color.Black;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            this.ShowInTaskbar = true;
            this.KeyPreview = true;

            // Create WebView2
            webView = new WebView2()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };

            this.Controls.Add(webView);

            // Handle key events for ESC to exit full screen
            this.KeyDown += BrowserForm_KeyDown;
            
            // Handle form events
            this.Load += BrowserForm_Load;
            this.FormClosing += BrowserForm_FormClosing;
        }

        private async void BrowserForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Initialize WebView2
                await webView.EnsureCoreWebView2Async(null);
                
                // Navigate to the URL
                webView.CoreWebView2.Navigate(_url);
                
                // Start in full screen mode
                EnterFullScreen();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize browser: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BrowserForm_KeyDown(object sender, KeyEventArgs e)
        {
            // ESC key to toggle full screen
            if (e.KeyCode == Keys.Escape)
            {
                if (_isFullScreen)
                    ExitFullScreen();
                else
                    EnterFullScreen();
            }
        }

        private void BrowserForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cleanup WebView2
            webView?.Dispose();
        }

        public void EnterFullScreen()
        {
            if (_isFullScreen) return;

            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            
            _isFullScreen = true;
        }

        public void ExitFullScreen()
        {
            if (!_isFullScreen) return;

            this.TopMost = false;
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.WindowState = FormWindowState.Maximized;
            
            _isFullScreen = false;
        }

        public bool IsFullScreen => _isFullScreen;

        public void NavigateToUrl(string url)
        {
            if (webView?.CoreWebView2 != null)
            {
                webView.CoreWebView2.Navigate(url);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // BrowserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.Name = "BrowserForm";
            this.Text = "HavenCNC Control Interface";
            this.ResumeLayout(false);
        }
    }
}
