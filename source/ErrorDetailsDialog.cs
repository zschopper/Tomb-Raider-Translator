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
using Microsoft.Win32;
using System.Management;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;

using System.Web;

namespace TRTR
{
    public partial class ErrorDetailsDialog : Form
    {

        public Exception error = null;
        int fullHeight;
        internal static void ShowError(Exception e)
        {
            Assembly thisExe = Assembly.GetExecutingAssembly();

            ErrorDetailsDialog inst = new ErrorDetailsDialog();
            inst.fullHeight = inst.ClientSize.Height;
            inst.ClientSize = new Size(inst.ClientSize.Width, inst.textBoxDetails.Top - 1);

            inst.pictureBox1.Image = System.Drawing.SystemIcons.Error.ToBitmap();
            inst.error = e;
            inst.labelErrorMessage.Text = e.Message;

            inst.textBoxDetails.Text = Write(e);

            //page.Position = 0;
            //inst.webBrowserDetails.DocumentStream = page;
            if (Application.OpenForms.Count > 0)
            {
                inst.ShowDialog(Application.OpenForms[0]);
            }
            else
                inst.ShowDialog(null);

        }




        public ErrorDetailsDialog()
        {
            InitializeComponent();
        }

        static string Write(Exception ex)
        {
            StringBuilder ret = new StringBuilder();
            ret.AppendFormat("Message: {0}\r\n", ex.Message);
            ret.AppendFormat("Source: {0}\r\n", ex.Source);
            ret.AppendFormat("TargetSite: {0}\r\n", ex.TargetSite);
            if (ex.Data.Count == 0)
            {
                ret.Append("Data: [none]\r\n");
            }
            else
            {
                ret.Append("Data:\r\n");
                foreach (object ob in ex.Data.Keys)
                {
                    object Data = ex.Data[ob];
                    ret.AppendFormat("\"{0}\": \"{1}\"", ob, ex.Data[ob]);
                    if (Data is string)
                    {
                        ret.Append("   Hex Values:");
                        foreach (char c in (string)(ex.Data[ob]))
                            ret.Append(" " + ((Int32)c).ToString("x2"));
                    }
                    ret.Append("\r\n");
                }
                ret.Append("\r\n");
            }
            ret.AppendFormat("StackTrace:\r\n{0}\r\n", ex.StackTrace);
            if (ex.InnerException == null)
                ret.Append("InnerException: [none]\r\n");
            else
                ret.Append("InnerException: [details below]\r\n");

            if (ex.InnerException != null)
                ret.Append(Write(ex.InnerException));
            ret.Append("\r\n");
            ret.Append(Log.GetLogContents());
            //HttpServerUtility u;

            return ret.ToString();
        }

        private void wmiInstalledAppList()
        {
            ManagementScope scope = null;
            ConnectionOptions options = null;
            ManagementPath path = null;
            ManagementBaseObject inParams = null;
            ManagementBaseObject outParams = null;
            ManagementClass wmiRegistry = null;
            try
            {
                try
                {
                    const uint HKEY_LOCAL_MACHINE = unchecked((uint)0x80000002);
                    string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

                    options = new ConnectionOptions();
                    options.Impersonation = ImpersonationLevel.Impersonate;
                    options.EnablePrivileges = true;

                    scope = new ManagementScope(@"\\.\root\default", options);

                    path = new ManagementPath("StdRegProv");

                    wmiRegistry = new ManagementClass(scope, path, null);

                    inParams = wmiRegistry.GetMethodParameters("EnumKey");
                    inParams["hDefKey"] = HKEY_LOCAL_MACHINE;
                    inParams["sSubKeyName"] = keyPath;

                    //wmiRegistry = new ManagementClass("root/default", "StdRegProv", null);

                    outParams = wmiRegistry.InvokeMethod("EnumKey", inParams, null);

                    //Console.WriteLine("Executing EnumKey() returns: {0}", returnValue);
                    if ((Int32)outParams["returnValue"] == 0)
                    {
                        string[] subKeys = null; // methodArgs[2] as String[];
                        if (subKeys == null)
                            return;
                        string keyName = string.Empty;

                        foreach (string subKey in subKeys)
                        {
                            //Display application name
                            keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\" + subKey;
                            keyName = "DisplayName";
                            inParams["sSubKeyName"] = keyPath;
                            inParams["sValueName"] = keyName;
                            ManagementBaseObject outParam =
                            wmiRegistry.InvokeMethod("GetStringValue", inParams, null);

                            //if ((uint)outParam["ReturnValue"] == 0)
                            //Console.WriteLine(outParam["sValue"]);
                            ;
                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            finally
            {
                // disposal
                if (outParams != null)
                    outParams.Dispose();
                if (inParams != null)
                    inParams.Dispose();
                if (wmiRegistry != null)
                    wmiRegistry.Dispose();
            }
        }

        private string regInstalledAppList()
        {
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine;
            Microsoft.Win32.RegistryKey subKey1 = regKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            string[] subKeyNames = subKey1.GetSubKeyNames();
            string ret = string.Empty;

            foreach (string subKeyName in subKeyNames)
            {
                Microsoft.Win32.RegistryKey subKey2 = subKey1.OpenSubKey(subKeyName);

                if (ValueNameExists(subKey2.GetValueNames(), "DisplayName") &&
                    ValueNameExists(subKey2.GetValueNames(), "DisplayVersion"))
                {
                    if (subKey2.GetValue("DisplayName").ToString().IndexOf(".NET Framework") >= 0 ||
                        subKey2.GetValue("DisplayName").ToString().IndexOf("MSXML") >= 0)
                        ret += subKey2.GetValue("DisplayName").ToString() + " (" + subKey2.GetValue("DisplayVersion").ToString() + ")\r\n";
                }

                subKey2.Close();
            }
            subKey1.Close();
            return ret;
        }

        private bool ValueNameExists(string[] valueNames, string valueName)
        {
            foreach (string s in valueNames)
                if (s.ToLower() == valueName.ToLower())
                    return true;
            return false;
        }

        /*
         * System.Diagnostics.FileVersionInfo fileVersionInfo =
        System.Diagnostics.FileVersionInfo.GetVersionInfo( fileName);
        string fileDescription = fileVersionInfo.FileDescription;
        */

        private string getSysInfo()
        {
            OperatingSystem os = Environment.OSVersion;
            string ret = os.ToString() + "\r\n" + os.VersionString.ToString() + "\r\n";

            //            string friendlyName = string.Empty;
            //string version = string.Empty;
            string componentsKeyName = @"SOFTWARE\Microsoft\Active Setup\Installed Components",
               friendlyName,
               version;
            // Find out in the registry anything under:
            //    HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components
            // that has ".NET Framework" in the name
            RegistryKey componentsKey = Registry.LocalMachine.OpenSubKey(componentsKeyName);
            string[] instComps = componentsKey.GetSubKeyNames();
            foreach (string instComp in instComps)
            {
                RegistryKey key = componentsKey.OpenSubKey(instComp);
                friendlyName = (string)key.GetValue(null); // Gets the (Default) value from this key
                if (friendlyName != null && friendlyName.IndexOf(".NET Framework") >= 0) //.NET Framework
                {
                    // Let's try to get any version information that's available
                    version = (string)key.GetValue("Version");
                    // If you want only the framework info with its SP level and not the
                    // other hotfix and service pack detail, uncomment this if:
                    //    if(version!=null && version.Split(',').Length>=4)
                    ret += friendlyName + (version != null ? (" (" + version + ")") : "") + "\r\n";
                }
            }
            return ret;
        }

        private void webBrowserDetails_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //HtmlDocument doc = webBrowserDetails.Document;
            //doc.GetElementById("errorLabel").InnerText = StaticTexts.error;
            //doc.GetElementById("error").InnerText = error.Message;
            ////doc.GetElementById("more").InnerText = StaticTexts.details;
            //string details = string.Empty;
            //doc.GetElementById("detailed").InnerHtml = Write(error);
            //doc.GetElementById("sysInfo").InnerHtml = regInstalledAppList().Replace("\r\n", "<br/>");
            //doc.GetElementById("copy").InnerHtml = StaticTexts.copyDetails;
        }

        private void webBrowserDetails_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            Noop.DoIt();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ClientSize.Height == fullHeight)
                ClientSize = new Size(ClientSize.Width, textBoxDetails.Top - 1);
            else
                ClientSize = new Size(ClientSize.Width, fullHeight);
            

        }

        private void button3_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(labelErrorMessage.Text + "\r\n\r\nDetails:\r\n" + textBoxDetails.Text);
            MessageBox.Show("Details copied to clipboard!", "Information");
        }
    }
}
