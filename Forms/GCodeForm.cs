using System;
using System.Windows.Forms;
using HavenCNCServer.Components;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Forms
{
    /// <summary>
    /// Dedicated form for displaying G-Code
    /// </summary>
    public partial class GCodeForm : Form
    {
        private GCodeViewerComponent? _gCodeViewerComponent;

        /// <summary>
        /// Initializes a new instance of the GCodeForm
        /// </summary>
        public GCodeForm()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 
            // GCodeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Name = "GCodeForm";
            this.Text = "G-Code Viewer";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
        }

        private void InitializeComponents()
        {
            try
            {
                // Create the G-code viewer component
                _gCodeViewerComponent = new GCodeViewerComponent();
                _gCodeViewerComponent.Dock = DockStyle.Fill;
                this.Controls.Add(_gCodeViewerComponent);

                LogInfo("G-Code form initialized", "GCodeForm");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize G-Code component: {ex.Message}", "GCodeForm");
            }
        }

        /// <summary>
        /// Load G-code into the display
        /// </summary>
        public void LoadGCodeForDisplay(string[] gcode)
        {
            try
            {
                _gCodeViewerComponent?.LoadGCodeForDisplay(gcode);
                LogInfo($"Loaded {gcode?.Length ?? 0} lines of G-code for display", "GCodeForm");
            }
            catch (Exception ex)
            {
                LogError($"Error loading G-code for display: {ex.Message}", "GCodeForm");
            }
        }

        /// <summary>
        /// Clear the G-code display
        /// </summary>
        public void ClearGCodeDisplay()
        {
            try
            {
                _gCodeViewerComponent?.ClearGCode();
                LogInfo("G-code display cleared", "GCodeForm");
            }
            catch (Exception ex)
            {
                LogError($"Error clearing G-code display: {ex.Message}", "GCodeForm");
            }
        }

        /// <summary>
        /// Handles form closing event to hide instead of disposing
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Hide instead of close to keep the component active
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
