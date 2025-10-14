using System;
using System.Drawing;
using System.Windows.Forms;
using CentroidAPI;
using HavenCNCServer.Services;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Components
{
    /// <summary>
    /// Component for displaying machine coordinates (DRO - Digital Readout)
    /// </summary>
    public class CoordinateDisplayComponent : UserControl, ICNCEventListener
    {
        private GroupBox grpCoordinates;
        private Label lblXPos;
        private Label lblYPos;
        private Label lblZPos;
        private Label lblXValue;
        private Label lblYValue;
        private Label lblZValue;

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
            this.grpCoordinates.Anchor = ((AnchorStyles)((((AnchorStyles.Top | AnchorStyles.Bottom)
                        | AnchorStyles.Left)
                        | AnchorStyles.Right)));
            this.grpCoordinates.Controls.Add(this.lblZValue);
            this.grpCoordinates.Controls.Add(this.lblYValue);
            this.grpCoordinates.Controls.Add(this.lblXValue);
            this.grpCoordinates.Controls.Add(this.lblZPos);
            this.grpCoordinates.Controls.Add(this.lblYPos);
            this.grpCoordinates.Controls.Add(this.lblXPos);
            this.grpCoordinates.Location = new Point(3, 3);
            this.grpCoordinates.Name = "grpCoordinates";
            this.grpCoordinates.Size = new Size(194, 150);
            this.grpCoordinates.TabIndex = 0;
            this.grpCoordinates.TabStop = false;
            this.grpCoordinates.Text = "Machine Coordinates";

            // lblXPos
            this.lblXPos.AutoSize = true;
            this.lblXPos.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblXPos.Location = new Point(15, 30);
            this.lblXPos.Name = "lblXPos";
            this.lblXPos.Size = new Size(24, 20);
            this.lblXPos.TabIndex = 0;
            this.lblXPos.Text = "X:";

            // lblYPos
            this.lblYPos.AutoSize = true;
            this.lblYPos.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblYPos.Location = new Point(15, 60);
            this.lblYPos.Name = "lblYPos";
            this.lblYPos.Size = new Size(24, 20);
            this.lblYPos.TabIndex = 1;
            this.lblYPos.Text = "Y:";

            // lblZPos
            this.lblZPos.AutoSize = true;
            this.lblZPos.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.lblZPos.Location = new Point(15, 90);
            this.lblZPos.Name = "lblZPos";
            this.lblZPos.Size = new Size(23, 20);
            this.lblZPos.TabIndex = 2;
            this.lblZPos.Text = "Z:";

            // lblXValue
            this.lblXValue.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lblXValue.Location = new Point(45, 30);
            this.lblXValue.Name = "lblXValue";
            this.lblXValue.Size = new Size(140, 20);
            this.lblXValue.TabIndex = 3;
            this.lblXValue.Text = "0.0000";
            this.lblXValue.TextAlign = ContentAlignment.MiddleRight;

            // lblYValue
            this.lblYValue.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lblYValue.Location = new Point(45, 60);
            this.lblYValue.Name = "lblYValue";
            this.lblYValue.Size = new Size(140, 20);
            this.lblYValue.TabIndex = 4;
            this.lblYValue.Text = "0.0000";
            this.lblYValue.TextAlign = ContentAlignment.MiddleRight;

            // lblZValue
            this.lblZValue.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lblZValue.Location = new Point(45, 90);
            this.lblZValue.Name = "lblZValue";
            this.lblZValue.Size = new Size(140, 20);
            this.lblZValue.TabIndex = 5;
            this.lblZValue.Text = "0.0000";
            this.lblZValue.TextAlign = ContentAlignment.MiddleRight;

            // CoordinateDisplayComponent
            this.Controls.Add(this.grpCoordinates);
            this.Name = "CoordinateDisplayComponent";
            this.Size = new Size(200, 156);
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

        protected override void OnHandleDestroyed(EventArgs e)
        {
            // Unregister from event listener when component is destroyed
            CNCJobInfoListener.RemoveListener(this);
            base.OnHandleDestroyed(e);
        }

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