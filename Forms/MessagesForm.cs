using System;
using System.Windows.Forms;
using HavenCNCServer.Components;
using static HavenCNCServer.Services.LoggingService;

namespace HavenCNCServer.Forms
{
    /// <summary>
    /// Dedicated form for displaying CNC messages
    /// </summary>
    public partial class MessagesForm : Form
    {
        private MessageDisplayComponent? _messageDisplayComponent;
        private Button btnClearMessages = null!;

        /// <summary>
        /// Initializes a new instance of the MessagesForm
        /// </summary>
        public MessagesForm()
        {
            InitializeComponent();
            InitializeComponents();
        }

        private void InitializeComponent()
        {
            this.btnClearMessages = new Button();
            this.SuspendLayout();

            // 
            // btnClearMessages
            // 
            this.btnClearMessages.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnClearMessages.Location = new System.Drawing.Point(880, 5);
            this.btnClearMessages.Name = "btnClearMessages";
            this.btnClearMessages.Size = new System.Drawing.Size(110, 30);
            this.btnClearMessages.TabIndex = 1;
            this.btnClearMessages.Text = "Clear Messages";
            this.btnClearMessages.UseVisualStyleBackColor = true;
            this.btnClearMessages.Click += new EventHandler(this.btnClearMessages_Click);

            // 
            // MessagesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.btnClearMessages);
            this.Name = "MessagesForm";
            this.Text = "CNC Messages";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ResumeLayout(false);
        }

        private void InitializeComponents()
        {
            try
            {
                // Create the message display component
                _messageDisplayComponent = new MessageDisplayComponent();
                _messageDisplayComponent.Dock = DockStyle.Fill;
                _messageDisplayComponent.Location = new System.Drawing.Point(0, 40);
                this.Controls.Add(_messageDisplayComponent);
                _messageDisplayComponent.BringToFront();

                LogInfo("Messages form initialized", "MessagesForm");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize messages component: {ex.Message}", "MessagesForm");
            }
        }

        private void btnClearMessages_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Clear all messages?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _messageDisplayComponent?.ClearMessages();
                LogInfo("Messages cleared by user", "MessagesForm");
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
