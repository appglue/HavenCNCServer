namespace HavenCNCServer
{
    partial class GCodeTestDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtGCode = new System.Windows.Forms.TextBox();
            this.lblGCode = new System.Windows.Forms.Label();
            this.btnRunGCode = new System.Windows.Forms.Button();
            this.btnRunSingleCommand = new System.Windows.Forms.Button();
            this.grpGCodeEditor = new System.Windows.Forms.GroupBox();
            this.grpControls = new System.Windows.Forms.GroupBox();
            this.grpGCodeEditor.SuspendLayout();
            this.grpControls.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtGCode
            // 
            this.txtGCode.AcceptsReturn = true;
            this.txtGCode.AcceptsTab = true;
            this.txtGCode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtGCode.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.txtGCode.Location = new System.Drawing.Point(15, 45);
            this.txtGCode.Multiline = true;
            this.txtGCode.Name = "txtGCode";
            this.txtGCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtGCode.Size = new System.Drawing.Size(750, 365);
            this.txtGCode.TabIndex = 0;
            this.txtGCode.Text = "G00 X0 \r\nG01 Z-0.1 F100\r\nG01 X1 Y1 F500\r\nG01 X0 Y0\r\nG01 X1 Y1\r\nG00 Z-1\r\nM30";
            this.txtGCode.WordWrap = false;
            // 
            // lblGCode
            // 
            this.lblGCode.AutoSize = true;
            this.lblGCode.Location = new System.Drawing.Point(15, 20);
            this.lblGCode.Name = "lblGCode";
            this.lblGCode.Size = new System.Drawing.Size(157, 15);
            this.lblGCode.TabIndex = 1;
            this.lblGCode.Text = "G-Code Editor (Line by Line):";

            // 
            // btnRunGCode
            // 
            this.btnRunGCode.BackColor = System.Drawing.Color.LightGreen;
            this.btnRunGCode.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnRunGCode.Location = new System.Drawing.Point(15, 25);
            this.btnRunGCode.Name = "btnRunGCode";
            this.btnRunGCode.Size = new System.Drawing.Size(100, 40);
            this.btnRunGCode.TabIndex = 4;
            this.btnRunGCode.Text = "Run G-Code";
            this.btnRunGCode.UseVisualStyleBackColor = false;
            this.btnRunGCode.Click += new System.EventHandler(this.btnRunGCode_Click);
            // 
            // btnRunSingleCommand
            // 
            this.btnRunSingleCommand.BackColor = System.Drawing.Color.LightGreen;
            this.btnRunSingleCommand.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.btnRunSingleCommand.Location = new System.Drawing.Point(405, 25);
            this.btnRunSingleCommand.Name = "btnRunSingleCommand";
            this.btnRunSingleCommand.Size = new System.Drawing.Size(140, 40);
            this.btnRunSingleCommand.TabIndex = 10;
            this.btnRunSingleCommand.Text = "Run Single Command";
            this.btnRunSingleCommand.UseVisualStyleBackColor = false;
            this.btnRunSingleCommand.Click += new System.EventHandler(this.btnRunSingleCommand_Click);


            // 
            // grpGCodeEditor
            // 
            this.grpGCodeEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpGCodeEditor.Controls.Add(this.txtGCode);
            this.grpGCodeEditor.Controls.Add(this.lblGCode);
            this.grpGCodeEditor.Location = new System.Drawing.Point(15, 15);
            this.grpGCodeEditor.Name = "grpGCodeEditor";
            this.grpGCodeEditor.Size = new System.Drawing.Size(780, 425);
            this.grpGCodeEditor.TabIndex = 12;
            this.grpGCodeEditor.TabStop = false;
            this.grpGCodeEditor.Text = "G-Code Editor";
            // 
            // grpControls
            // 
            this.grpControls.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpControls.Controls.Add(this.btnRunGCode);
            this.grpControls.Controls.Add(this.btnRunSingleCommand);
            this.grpControls.Location = new System.Drawing.Point(15, 450);
            this.grpControls.Name = "grpControls";
            this.grpControls.Size = new System.Drawing.Size(780, 80);
            this.grpControls.TabIndex = 13;
            this.grpControls.TabStop = false;
            this.grpControls.Text = "G-Code Execution Controls";

            // 
            // GCodeTestDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(810, 540);
            this.Controls.Add(this.grpControls);
            this.Controls.Add(this.grpGCodeEditor);
            this.Name = "GCodeTestDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "G-Code Test Dialog - Centroid API Integration";
            this.grpGCodeEditor.ResumeLayout(false);
            this.grpGCodeEditor.PerformLayout();
            this.grpControls.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtGCode;
        private System.Windows.Forms.Label lblGCode;
        private System.Windows.Forms.Button btnRunGCode;
        private System.Windows.Forms.Button btnRunSingleCommand;
        private System.Windows.Forms.GroupBox grpGCodeEditor;
        private System.Windows.Forms.GroupBox grpControls;
    }
}