using System;
using System.Drawing;
using System.Windows.Forms;
using CentroidAPI;
using HavenCNCServer.Services;
using HavenCNCServer.Centroid.Events;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Components
{
    /// <summary>
    /// Component for displaying machine coordinates (DRO - Digital Readout)
    /// </summary>
    public class CoordinateDisplayComponent : UserControl, ICNCEventListener
    {
        private GroupBox grpCoordinates = null!;
        private Label lblXPos = null!;
        private Label lblYPos = null!;
        private Label lblZPos = null!;
        private Label lblXValue = null!;
        private Label lblYValue = null!;
        private Label lblZValue = null!;

        /// <summary>
        /// Initializes a new instance of the CoordinateDisplayComponent
        /// </summary>
        public CoordinateDisplayComponent()
        {
            InitializeComponent();
            SetupCoordinateDisplay();
        }

        private void InitializeComponent()
        {
            this.grpCoordinates = new GroupBox();
            this.lblZValue = new Label();
            this.lblYValue = new Label();
            this.lblXValue = new Label();
            this.lblZPos = new Label();
            this.lblYPos = new Label();
            this.lblXPos = new Label();
            this.grpCoordinates.SuspendLayout();
            this.SuspendLayout();

            // grpCoordinates
            this.grpCoordinates.Anchor = ((AnchorStyles)(((AnchorStyles.Top | AnchorStyles.Left) | AnchorStyles.Right)));
            this.grpCoordinates.Controls.Add(this.lblZValue);
            this.grpCoordinates.Controls.Add(this.lblYValue);
            this.grpCoordinates.Controls.Add(this.lblXValue);
            this.grpCoordinates.Controls.Add(this.lblZPos);
            this.grpCoordinates.Controls.Add(this.lblYPos);
            this.grpCoordinates.Controls.Add(this.lblXPos);
            this.grpCoordinates.Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold, GraphicsUnit.Point);
            this.grpCoordinates.ForeColor = Color.DarkBlue;
            this.grpCoordinates.Location = new Point(0, 0);
            this.grpCoordinates.Name = "grpCoordinates";
            this.grpCoordinates.Size = new Size(150, 110);
            this.grpCoordinates.TabIndex = 0;
            this.grpCoordinates.TabStop = false;
            this.grpCoordinates.Text = "Coordinates";

            // lblXPos
            this.lblXPos.AutoSize = true;
            this.lblXPos.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblXPos.Location = new Point(8, 22);
            this.lblXPos.Name = "lblXPos";
            this.lblXPos.Size = new Size(19, 17);
            this.lblXPos.TabIndex = 0;
            this.lblXPos.Text = "X:";

            // lblYPos
            this.lblYPos.AutoSize = true;
            this.lblYPos.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblYPos.Location = new Point(8, 48);
            this.lblYPos.Name = "lblYPos";
            this.lblYPos.Size = new Size(19, 17);
            this.lblYPos.TabIndex = 1;
            this.lblYPos.Text = "Y:";

            // lblZPos
            this.lblZPos.AutoSize = true;
            this.lblZPos.Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblZPos.Location = new Point(8, 74);
            this.lblZPos.Name = "lblZPos";
            this.lblZPos.Size = new Size(19, 17);
            this.lblZPos.TabIndex = 2;
            this.lblZPos.Text = "Z:";

            // lblXValue
            this.lblXValue.AutoSize = true;
            this.lblXValue.Font = new Font("Courier New", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblXValue.ForeColor = Color.Blue;
            this.lblXValue.Location = new Point(35, 22);
            this.lblXValue.Name = "lblXValue";
            this.lblXValue.Size = new Size(78, 18);
            this.lblXValue.TabIndex = 3;
            this.lblXValue.Text = "0.0000";

            // lblYValue
            this.lblYValue.AutoSize = true;
            this.lblYValue.Font = new Font("Courier New", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblYValue.ForeColor = Color.Blue;
            this.lblYValue.Location = new Point(35, 48);
            this.lblYValue.Name = "lblYValue";
            this.lblYValue.Size = new Size(78, 18);
            this.lblYValue.TabIndex = 4;
            this.lblYValue.Text = "0.0000";

            // lblZValue
            this.lblZValue.AutoSize = true;
            this.lblZValue.Font = new Font("Courier New", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblZValue.ForeColor = Color.Blue;
            this.lblZValue.Location = new Point(35, 74);
            this.lblZValue.Name = "lblZValue";
            this.lblZValue.Size = new Size(78, 18);
            this.lblZValue.TabIndex = 5;
            this.lblZValue.Text = "0.0000";

            // CoordinateDisplayComponent
            this.Controls.Add(this.grpCoordinates);
            this.Name = "CoordinateDisplayComponent";
            this.Size = new Size(150, 110);
            this.grpCoordinates.ResumeLayout(false);
            this.grpCoordinates.PerformLayout();
            this.ResumeLayout(false);
        }

        private void SetupCoordinateDisplay()
        {
            // Initialize coordinate values
            lblXValue.Text = "0.0000";
            lblYValue.Text = "0.0000";
            lblZValue.Text = "0.0000";

            // Register as event listener with CNCJobInfoListener
            CNCJobInfoListener.AddListener(this);
        }

        /// <summary>
        /// Receives and processes CNC events for coordinate updates
        /// </summary>
        /// <param name="centroidEvent">The CNC event to process</param>
        public void EventReceived(ICentroidEvent centroidEvent)
        {
            // Only process DRO events for coordinate updates
            if (centroidEvent is DROEvent droEvent)
            {
                // Update UI on the main thread using Invoke
                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateCoordinateDisplay(droEvent)));
                }
                else
                {
                    UpdateCoordinateDisplay(droEvent);
                }
            }
        }

        private void UpdateCoordinateDisplay(DROEvent droEvent)
        {
            try
            {
                // Update X, Y, Z coordinate displays with 4 decimal places
                lblXValue.Text = droEvent.Axis1.ToString("F4");
                lblYValue.Text = droEvent.Axis2.ToString("F4");
                lblZValue.Text = droEvent.Axis3.ToString("F4");
            }
            catch (Exception ex)
            {
                LogError($"Error updating coordinate display: {ex.Message}", "CoordinateDisplay");
            }
        }

        /// <summary>
        /// Updates the coordinate display with new X, Y, Z values
        /// </summary>
        /// <param name="x">X coordinate value</param>
        /// <param name="y">Y coordinate value</param>
        /// <param name="z">Z coordinate value</param>
        public void UpdateCoordinates(double x, double y, double z)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateCoordinates(x, y, z)));
                return;
            }

            lblXValue.Text = x.ToString("F4");
            lblYValue.Text = y.ToString("F4");
            lblZValue.Text = z.ToString("F4");
        }

        /// <summary>
        /// Handles cleanup when the component handle is destroyed
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            // Unregister from event listener when component is destroyed
            CNCJobInfoListener.RemoveListener(this);
            base.OnHandleDestroyed(e);
        }

        /// <summary>
        /// Disposes of the component resources
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Unregister from event listener
                CNCJobInfoListener.RemoveListener(this);
            }
            base.Dispose(disposing);
        }
    }
}