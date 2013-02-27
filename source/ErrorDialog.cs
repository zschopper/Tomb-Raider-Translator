using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Resources;
using System.Reflection;

namespace TRTR
{
    public partial class ErrorDialog : Form
    {
        internal static void ShowError(Exception e)
        {
            if (MessageBox.Show(e.Message + "\r\n\r\n" + StaticTexts.dialogDetails, StaticTexts.error, MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.OK)
                ErrorDetailsDialog.ShowError(e);
        }

        public ErrorDialog()
        {
            InitializeComponent();
        }
    }
}
