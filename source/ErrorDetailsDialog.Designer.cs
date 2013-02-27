namespace TRTR
{
    partial class ErrorDetailsDialog
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
            this.webBrowserDetails = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowserDetails
            // 
            this.webBrowserDetails.AllowNavigation = false;
            this.webBrowserDetails.AllowWebBrowserDrop = false;
            this.webBrowserDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.webBrowserDetails.IsWebBrowserContextMenuEnabled = false;
            this.webBrowserDetails.Location = new System.Drawing.Point(0, 0);
            this.webBrowserDetails.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowserDetails.Name = "webBrowserDetails";
            this.webBrowserDetails.ScriptErrorsSuppressed = true;
            this.webBrowserDetails.Size = new System.Drawing.Size(688, 696);
            this.webBrowserDetails.TabIndex = 0;
            this.webBrowserDetails.WebBrowserShortcutsEnabled = false;
            this.webBrowserDetails.Navigating += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.webBrowserDetails_Navigating);
            this.webBrowserDetails.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowserDetails_DocumentCompleted);
            // 
            // ErrorDetailsDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(686, 693);
            this.Controls.Add(this.webBrowserDetails);
            this.MinimizeBox = false;
            this.Name = "ErrorDetailsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Tomb Raider Translator";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowserDetails;

    }
}