using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HavenCNCServer.Components
{
    /// <summary>
    /// A flicker-free log viewer control optimized for real-time log display
    /// Uses double-buffering and efficient text append operations
    /// </summary>
    public class FlickerFreeLogViewer : RichTextBox
    {
        private int _lastDisplayedCount = 0;
        private const int MaxLines = 10000;

        /// <summary>
        /// Initializes a new instance of the FlickerFreeLogViewer class
        /// </summary>
        public FlickerFreeLogViewer()
        {
            // Don't use UserPaint for RichTextBox - it breaks rendering
            // Just use double-buffering through standard Windows Forms
            DoubleBuffered = true;

            // Configure the control
            ReadOnly = true;
            WordWrap = true;
            ScrollBars = RichTextBoxScrollBars.Vertical;
            BorderStyle = BorderStyle.Fixed3D;
            BackColor = Color.White;
            Font = new Font("Consolas", 9F);
            Dock = DockStyle.Fill;
        }

        /// <summary>
        /// Appends a log entry with color without clearing the entire control
        /// </summary>
        /// <param name="text">The text to append</param>
        /// <param name="color">The color for the text</param>
        public void AppendLogEntry(string text, Color color)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => AppendLogEntry(text, color));
                return;
            }

            try
            {
                // Suspend layout during update
                SuspendLayout();

                // Remember if we were at the bottom before update
                bool wasAtBottom = IsScrolledToBottom();

                // Append the colored text
                SelectionStart = TextLength;
                SelectionLength = 0;
                SelectionColor = color;
                AppendText(text + Environment.NewLine);
                SelectionColor = ForeColor;

                // Trim old lines if we exceed max
                TrimOldLines();

                // Auto-scroll only if we were at the bottom
                if (wasAtBottom)
                {
                    ScrollToBottom();
                }
            }
            finally
            {
                ResumeLayout();
            }
        }

        /// <summary>
        /// Updates the log viewer with new entries (append-only)
        /// </summary>
        /// <param name="entries">All log entries</param>
        /// <param name="colorSelector">Function to get color for log level</param>
        /// <param name="textSelector">Function to get text from entry</param>
        public void UpdateLogEntries<T>(IEnumerable<T> entries, Func<T, Color> colorSelector, Func<T, string> textSelector)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => UpdateLogEntries(entries, colorSelector, textSelector));
                return;
            }

            try
            {
                var entryList = entries.ToList();
                int newCount = entryList.Count;

                // Only append new entries since last update
                if (newCount > _lastDisplayedCount)
                {
                    // Suspend to reduce flicker
                    SuspendLayout();

                    bool wasAtBottom = IsScrolledToBottom();

                    // Append only new entries
                    var newEntries = entryList.Skip(_lastDisplayedCount);
                    foreach (var entry in newEntries)
                    {
                        var text = textSelector(entry);
                        var color = colorSelector(entry);

                        SelectionStart = TextLength;
                        SelectionLength = 0;
                        SelectionColor = color;
                        AppendText(text + Environment.NewLine);
                    }

                    _lastDisplayedCount = newCount;

                    // Trim if needed
                    TrimOldLines();

                    // Auto-scroll if was at bottom
                    if (wasAtBottom)
                    {
                        ScrollToBottom();
                    }

                    ResumeLayout();
                }
                else if (newCount < _lastDisplayedCount)
                {
                    // Logs were cleared, rebuild
                    RebuildFromScratch(entryList, colorSelector, textSelector);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating log viewer: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears and rebuilds the log from scratch
        /// </summary>
        private void RebuildFromScratch<T>(List<T> entries, Func<T, Color> colorSelector, Func<T, string> textSelector)
        {
            SuspendLayout();

            Clear();
            _lastDisplayedCount = 0;

            // Display last MaxLines entries
            var displayEntries = entries.TakeLast(MaxLines);

            foreach (var entry in displayEntries)
            {
                var text = textSelector(entry);
                var color = colorSelector(entry);

                SelectionStart = TextLength;
                SelectionLength = 0;
                SelectionColor = color;
                AppendText(text + Environment.NewLine);
            }

            _lastDisplayedCount = entries.Count;
            ScrollToBottom();

            ResumeLayout();
        }

        /// <summary>
        /// Removes old lines if we exceed the maximum
        /// </summary>
        private void TrimOldLines()
        {
            if (Lines.Length > MaxLines)
            {
                int linesToRemove = Lines.Length - MaxLines;
                int charCount = 0;

                // Count characters to remove
                for (int i = 0; i < linesToRemove; i++)
                {
                    charCount += Lines[i].Length + Environment.NewLine.Length;
                }

                // Remove from beginning
                Select(0, charCount);
                SelectedText = string.Empty;
                Select(TextLength, 0);
            }
        }

        /// <summary>
        /// Checks if the scroll position is at the bottom
        /// </summary>
        private bool IsScrolledToBottom()
        {
            // Get the position of the first visible character
            int firstVisibleChar = GetCharIndexFromPosition(new Point(1, 1));
            int lastVisibleChar = GetCharIndexFromPosition(new Point(Width - 1, Height - 1));

            // Check if we can see the last character
            return lastVisibleChar >= TextLength - 1 || TextLength == 0;
        }

        /// <summary>
        /// Scrolls to the bottom of the control
        /// </summary>
        private void ScrollToBottom()
        {
            SelectionStart = TextLength;
            ScrollToCaret();
        }

        /// <summary>
        /// Reset the displayed count (call when logs are cleared)
        /// </summary>
        public void Reset()
        {
            _lastDisplayedCount = 0;
            Clear();
        }
    }
}
