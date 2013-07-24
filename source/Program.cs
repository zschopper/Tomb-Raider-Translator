using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

namespace TRTR
{
    static class Program
    {
        [DllImport("User32.dll")]
        public static extern int ShowWindowAsync(IntPtr hWnd, int swCommand);
        [STAThread]

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            Settings.args = args;
            Log.AddListener("debug", new DebugLogListener());
            Log.AddListener("applog", new FileLogListener(@".\trtr.log"));
            Log.Write(LogEntryType.Info, "Starting...");
            Log.Write(LogEntryType.Info, "Command Line: " + Environment.CommandLine);
            //try
            {
                Process[] RunningProcesses = Process.GetProcessesByName("TRTR2");
                if (RunningProcesses.Length < 2)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new form_Main());
                }
                else
                {
                    ShowWindowAsync(RunningProcesses[0].MainWindowHandle, (int)ShowWindowConstants.SW_SHOWMINIMIZED);
                    ShowWindowAsync(RunningProcesses[0].MainWindowHandle, (int)ShowWindowConstants.SW_RESTORE);
                }

                //Application.CurrentCulture = new CultureInfo("hu-HU");
            }
            //catch (Exception ex)
            //{
            //    Log.Write(ex);
            //    //ErrorDialog.ShowError(ex);
            //    //MessageBox.Show(ex.Message, StaticTexts.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            //    throw;
            //}
        }
    }

    public enum ShowWindowConstants
    {
        SW_HIDE = 0,
        SW_SHOWNORMAL = 1,
        SW_NORMAL = 1,
        SW_SHOWMINIMIZED = 2,
        SW_SHOWMAXIMIZED = 3,
        SW_MAXIMIZE = 3,
        SW_SHOWNOACTIVATE = 4,
        SW_SHOW = 5,
        SW_MINIMIZE = 6,
        SW_SHOWMINNOACTIVE = 7,
        SW_SHOWNA = 8,
        SW_RESTORE = 9,
        SW_SHOWDEFAULT = 10,
        SW_FORCEMINIMIZE = 11,
        SW_MAX = 11
    };
}
