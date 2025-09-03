using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HavenCNCServer.Services
{
    /// <summary>
    /// Service to manage the browser UI state from API controllers
    /// </summary>
    public static class UIControlService
    {
        private static MainForm? _mainForm;
        private static BrowserForm? _browserForm;
        private static bool _isBrowserOpen = false;

        /// <summary>
        /// Register the main form instance
        /// </summary>
        public static void RegisterMainForm(MainForm mainForm)
        {
            _mainForm = mainForm;
        }

        /// <summary>
        /// Enter full screen mode - opens browser in full screen
        /// </summary>
        public static async Task<bool> EnterFullScreenAsync()
        {
            if (_mainForm == null) return false;

            try
            {
                if (_mainForm.InvokeRequired)
                {
                    var result = await Task.Run(() =>
                    {
                        return _mainForm.Invoke(new Func<bool>(() =>
                        {
                            return OpenBrowserFullScreen();
                        }));
                    });
                    return (bool)result;
                }
                else
                {
                    return OpenBrowserFullScreen();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Exit full screen mode - closes or minimizes browser
        /// </summary>
        public static async Task<bool> ExitFullScreenAsync()
        {
            if (_mainForm == null) return false;

            try
            {
                if (_mainForm.InvokeRequired)
                {
                    var result = await Task.Run(() =>
                    {
                        return _mainForm.Invoke(new Func<bool>(() =>
                        {
                            return CloseBrowser();
                        }));
                    });
                    return (bool)result;
                }
                else
                {
                    return CloseBrowser();
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Get current browser open state (not using full screen for now)
        /// </summary>
        public static bool IsFullScreen => _isBrowserOpen;

        private static bool OpenBrowserFullScreen()
        {
            try
            {
                if (_browserForm == null || _browserForm.IsDisposed)
                {
                    _browserForm = new BrowserForm("http://localhost:5000");
                    _browserForm.FormClosed += (s, e) => { _isBrowserOpen = false; _browserForm = null; };
                }

                if (!_isBrowserOpen)
                {
                    _browserForm.Show();
                    _isBrowserOpen = true;
                }

                // Open in normal windowed mode instead of full screen for now
                _browserForm.ExitFullScreen(); // Ensure it's not in full screen
                _browserForm.BringToFront();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool CloseBrowser()
        {
            try
            {
                if (_browserForm != null && !_browserForm.IsDisposed)
                {
                    // Just hide the browser 
                    _browserForm.Hide();
                    _isBrowserOpen = false;
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
