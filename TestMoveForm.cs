using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using HavenCNCServer.Controllers;
using HavenCNCServer.Models;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer
{
    public partial class TestMoveForm : Form
    {
        public TestMoveForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.txtX = new System.Windows.Forms.TextBox();
            this.txtY = new System.Windows.Forms.TextBox();
            this.txtZ = new System.Windows.Forms.TextBox();
            this.lblX = new System.Windows.Forms.Label();
            this.lblY = new System.Windows.Forms.Label();
            this.lblZ = new System.Windows.Forms.Label();
            this.btnMove = new System.Windows.Forms.Button();
            this.btnMoveAsync = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblX
            // 
            this.lblX.AutoSize = true;
            this.lblX.Location = new System.Drawing.Point(20, 30);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(20, 15);
            this.lblX.TabIndex = 0;
            this.lblX.Text = "X:";
            // 
            // txtX
            // 
            this.txtX.Location = new System.Drawing.Point(50, 27);
            this.txtX.Name = "txtX";
            this.txtX.Size = new System.Drawing.Size(100, 23);
            this.txtX.TabIndex = 1;
            this.txtX.Text = "0";
            // 
            // lblY
            // 
            this.lblY.AutoSize = true;
            this.lblY.Location = new System.Drawing.Point(20, 60);
            this.lblY.Name = "lblY";
            this.lblY.Size = new System.Drawing.Size(20, 15);
            this.lblY.TabIndex = 2;
            this.lblY.Text = "Y:";
            // 
            // txtY
            // 
            this.txtY.Location = new System.Drawing.Point(50, 57);
            this.txtY.Name = "txtY";
            this.txtY.Size = new System.Drawing.Size(100, 23);
            this.txtY.TabIndex = 3;
            this.txtY.Text = "0";
            // 
            // lblZ
            // 
            this.lblZ.AutoSize = true;
            this.lblZ.Location = new System.Drawing.Point(20, 90);
            this.lblZ.Name = "lblZ";
            this.lblZ.Size = new System.Drawing.Size(20, 15);
            this.lblZ.TabIndex = 4;
            this.lblZ.Text = "Z:";
            // 
            // txtZ
            // 
            this.txtZ.Location = new System.Drawing.Point(50, 87);
            this.txtZ.Name = "txtZ";
            this.txtZ.Size = new System.Drawing.Size(100, 23);
            this.txtZ.TabIndex = 5;
            this.txtZ.Text = "0";
            // 
            // btnMove
            // 
            this.btnMove.Location = new System.Drawing.Point(20, 130);
            this.btnMove.Name = "btnMove";
            this.btnMove.Size = new System.Drawing.Size(130, 30);
            this.btnMove.TabIndex = 6;
            this.btnMove.Text = "Move (Sync)";
            this.btnMove.UseVisualStyleBackColor = true;
            this.btnMove.Click += new System.EventHandler(this.btnMove_Click);
            // 
            // btnMoveAsync
            // 
            this.btnMoveAsync.Location = new System.Drawing.Point(20, 170);
            this.btnMoveAsync.Name = "btnMoveAsync";
            this.btnMoveAsync.Size = new System.Drawing.Size(130, 30);
            this.btnMoveAsync.TabIndex = 7;
            this.btnMoveAsync.Text = "Move (Async)";
            this.btnMoveAsync.UseVisualStyleBackColor = true;
            this.btnMoveAsync.Click += new System.EventHandler(this.btnMoveAsync_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(20, 210);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(330, 60);
            this.lblStatus.TabIndex = 8;
            this.lblStatus.Text = "Ready";
            // 
            // TestMoveForm
            // 
            this.ClientSize = new System.Drawing.Size(370, 280);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnMoveAsync);
            this.Controls.Add(this.btnMove);
            this.Controls.Add(this.txtZ);
            this.Controls.Add(this.lblZ);
            this.Controls.Add(this.txtY);
            this.Controls.Add(this.lblY);
            this.Controls.Add(this.txtX);
            this.Controls.Add(this.lblX);
            this.Name = "TestMoveForm";
            this.Text = "Test Move Command";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox txtX;
        private System.Windows.Forms.TextBox txtY;
        private System.Windows.Forms.TextBox txtZ;
        private System.Windows.Forms.Label lblX;
        private System.Windows.Forms.Label lblY;
        private System.Windows.Forms.Label lblZ;
        private System.Windows.Forms.Button btnMove;
        private System.Windows.Forms.Button btnMoveAsync;
        private System.Windows.Forms.Label lblStatus;

        private void btnMove_Click(object sender, EventArgs e)
        {
            try
            {
                LogInfo("🔘 Sync Move button clicked", "TestMove");
                lblStatus.Text = "Moving (Sync)...";
                lblStatus.Refresh();

                double x = double.Parse(txtX.Text);
                double y = double.Parse(txtY.Text);
                double z = double.Parse(txtZ.Text);

                var controller = new CNCMovementController();
                var request = new MoveToRequest
                {
                    Point = new MachinePoint { X = x, Y = y, Z = z },
                    Strategy = MoveStrategy.ZSeparate,
                    XYSpeed = 100,
                    ZSpeed = 50
                };

                controller.MoveTo(request);

                lblStatus.Text = $"✅ Move completed: X={x}, Y={y}, Z={z}";
                LogInfo($"✅ Sync move completed", "TestMove");
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Error: {ex.Message}";
                LogError($"Sync move error: {ex.Message}", "TestMove");
                MessageBox.Show($"Error: {ex.Message}", "Move Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnMoveAsync_Click(object sender, EventArgs e)
        {
            try
            {
                LogInfo("🔘 Async Move button clicked", "TestMove");
                lblStatus.Text = "Moving (Async)...";
                btnMoveAsync.Enabled = false;
                btnMove.Enabled = false;

                double x = double.Parse(txtX.Text);
                double y = double.Parse(txtY.Text);
                double z = double.Parse(txtZ.Text);

                await Task.Run(() =>
                {
                    var controller = new CNCMovementController();
                    var request = new MoveToRequest
                    {
                        Point = new MachinePoint { X = x, Y = y, Z = z },
                        Strategy = MoveStrategy.ZSeparate,
                        XYSpeed = 100,
                        ZSpeed = 50
                    };

                    controller.MoveTo(request);
                });

                lblStatus.Text = $"✅ Move completed: X={x}, Y={y}, Z={z}";
                LogInfo($"✅ Async move completed", "TestMove");
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Error: {ex.Message}";
                LogError($"Async move error: {ex.Message}", "TestMove");
                MessageBox.Show($"Error: {ex.Message}", "Move Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnMoveAsync.Enabled = true;
                btnMove.Enabled = true;
            }
        }
    }
}
