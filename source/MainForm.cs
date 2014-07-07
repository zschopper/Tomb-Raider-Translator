using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Xml;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

namespace TRTR
{

    internal partial class form_Main : Form, ILogListener
    {
        private delegate void SetControlPropertyThreadSafeDelegate(Control control, string propertyName, object propertyValue);

        public static void SetControlPropertyThreadSafe(Control control, string propertyName, object propertyValue)
        {
            if (control.InvokeRequired)
            {
                control.Invoke(new SetControlPropertyThreadSafeDelegate(SetControlPropertyThreadSafe), new object[] { control, propertyName, propertyValue });
            }
            else
            {
                control.GetType().InvokeMember(propertyName, BindingFlags.SetProperty, null, control, new object[] { propertyValue });
            }
        }

        public void InitializeListener()
        {
        }

        public void LogMsg(LogMessage msg)
        {
            if (msg.LogType == LogEntryType.Progress)
            {
                SetControlPropertyThreadSafe(labelProgressText, "Text", msg.Message);
                SetControlPropertyThreadSafe(progressTask, "Value", msg.Progress);
            }
        }

        public void FinalizeListener()
        {
        }

        internal class GamesComboBoxItem
        {
            public int Value { get; set; }
            public string Text { get; set; }
            public bool Selectable { get; set; }
            public GameInstance Game { get; set; }
        }

        /*[DllImport("user32.dll")]
        internal static extern void
            SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter,
                         Int32 X, Int32 Y, Int32 width, Int32 height, uint Flags);
        */

        internal form_Main()
        {
            Log.AddListener("form", this);
            Settings.Load();
            //            Application.CurrentCulture = new CultureInfo(Settings.LastLocale);
            //            Thread.CurrentThread.CurrentCulture = new CultureInfo(Settings.LastLocale);
            ChangeCulture(new CultureInfo(Settings.LastLocale));
        }

        private void InitializeLocalizations()
        {
            contextLang.Items.Clear();
            //            foreach(string key in Settings.Cultures.Keys)
            foreach (string Key in Settings.Cultures.Keys)
            {
                CultureInfo ci = Settings.Cultures[Key];
                ToolStripMenuItem item = new ToolStripMenuItem(ci.DisplayName);
                item.ToolTipText = Key;
                item.AutoToolTip = false;
                contextLang.Items.Add(item);
                //item.ToolTipText = Settings.Cultures[key].DisplayName;
                item.Click += new EventHandler(menuItemLangSelect_Click);
            }
        }

        private void Initialize()
        {
            try
            {
                // CultureInfo cinfo = Thread.CurrentThread.CurrentCulture;
                // Thread.CurrentThread.CurrentUICulture = new CultureInfo("hu-HU");
                // Thread.CurrentThread.CurrentCulture = new CultureInfo("hu-HU");
                InitializeComponent();
                this.SetStyle(
                  ControlStyles.AllPaintingInWmPaint |
                  ControlStyles.UserPaint |
                  ControlStyles.DoubleBuffer, true);

                Int32 top;
                Int32 left;
                foreach (Control c in Controls)
                {
                    top = c.Top;
                    left = c.Left;
                    pictureBox1.Controls.Add(c);
                    if (!(c is PictureBox) && !(c is ComboBox) && !(c is ProgressBar))
                        c.BackColor = Color.FromArgb(0, c.BackColor);
                    c.Top = top;
                    c.Left = left;
                }

                // search for installed games in registry and fill game selector combo

                //ezvanelbaszva:

                comboGame.Items.Clear();

                comboGame.ValueMember = "Game";
                comboGame.DisplayMember = "Text";
                InstallTypeEnum categ = InstallTypeEnum.Unknown;

                foreach (GameInstance game in InstalledGames.Items)
                {
                    if (categ != game.InstallType)
                    {
                        categ = game.InstallType;
                        comboGame.Items.Add(new GamesComboBoxItem()
                        {
                            Selectable = false,
                            Text = categ.ToString(),
                            Game = null
                        });

                    }
                    string typeString = string.Empty;
                    switch (game.InstallType)
                    {
                        case InstallTypeEnum.Steam:
                            typeString = "steam";
                            break;

                        case InstallTypeEnum.Custom:
                            typeString = "custom";
                            break;

                        default:

                            break;
                    }

                    comboGame.Items.Add(
                        new GamesComboBoxItem()
                        {
                            Selectable = true,
                            Text = string.Format("  {0} ({1})", game.Name, typeString),
                            Game = game
                        }
                    );
                }

                // update version and language label label
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);
                labelVersion.Text = String.Format(GeneralTexts.Version, Settings.version.ToString()) + (info.IsDebug ? " (dev)" : string.Empty);
                labelLang.Text = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToUpper();

                //menuItemCompileTexts.Visible = false;
                //menuItemExtractTexts.Visible = false;
                //menuItemSimulateRestoration.Visible = false;
                //menuItemSimulateTranslation.Visible = false;

                // show debug menu items in debug mode

                ShowExtraDebugMenuItems();
                #region FullScreen settings
                /*
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.TopMost = true;
                */

                #endregion


                TRGameInfo.Worker.WorkerStart += new DoWorkEventHandler(workerStart);
                //TRGameInfo2.Worker.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
                TRGameInfo.Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerRefreshGameInfo_RunWorkerCompleted);
                TRGameInfo.Worker.CurrentCulture = Application.CurrentCulture;

                TranslationHandler.Worker.WorkerStart += new DoWorkEventHandler(workerStart);
                TranslationHandler.Worker.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
                TranslationHandler.Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(generalWorker_RunWorkerCompleted);
                TranslationHandler.Worker.CurrentCulture = Application.CurrentCulture;
            }
            catch (Exception ex)
            {
                throw new Exception(Errors.InitializationError, ex);
            }
        }

        private void workerStart(object sender, DoWorkEventArgs e)
        {
            LockThreadControls();
        }

        private void generalWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateGameInfo();
            UnlockThreadControls2();
            if (e.Error != null)
            {
                Log.Write(e.Error);
                ErrorDetailsDialog.ShowError(e.Error);
                //MessageBox.Show(e.Error.Message, StaticTexts.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void generalWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            labelProgressText.Text = (e.UserState is string) ? (string)e.UserState : string.Empty;
            if (e.ProgressPercentage > 0)
            {
                progressTask.Style = ProgressBarStyle.Continuous;
                progressTask.Value = e.ProgressPercentage;
            }
        }

        private void form_Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings.FormLocation = this.Location;
            Settings.LastGame = comboGame.Text;
            Settings.LastLocale = Thread.CurrentThread.CurrentCulture.Name;
            Settings.Save();
        }

        private void form_Main_Load(object sender, EventArgs e)
        {
            Location = Settings.FormLocation;
            // restore MRU game
            GamesComboBoxItem game = null;
            int i = 0;
            while (i < comboGame.Items.Count && game == null)
            {
                if (((GamesComboBoxItem)(comboGame.Items[i])).Text == Settings.LastGame)
                    game = (GamesComboBoxItem)(comboGame.Items[i]);
                i++;
            }

            if (game != null)
            {
                comboGame.SelectedIndex = comboGame.Items.IndexOf(game);
                comboGame_SelectionChangeCommitted(comboGame, null);
            }
        }

        private void ShowExtraDebugMenuItems()
        {
            //menuItemCompileTexts.Visible = true;
            //menuItemExtractTexts.Visible = true;
            //menuItemSimulateRestoration.Visible = true;
            //menuItemSimulateTranslation.Visible = true;
        }

        // change language of application texts
        private void ChangeCulture(CultureInfo culture)
        {
            bool initControls = comboGame != null;
            string comboGameText = string.Empty;
            Point loc = new Point(0, 0);
            if (initControls)
            {
                comboGameText = comboGame.Text;
                loc = Location;
                Application.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }
            else
            {
                Application.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                Initialize();
                InitializeLocalizations();
            }

            ResTexts.Refresh();
            ChangeTexts(this);
            foreach (ToolStripItem item in contextExtra.Items)
                ChangeTexts(item);
            labelLang.Text = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName.ToUpper();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);

            labelVersion.Text = String.Format(GeneralTexts.Version, Settings.version.ToString()) + (info.IsDebug ? " (dev)" : string.Empty);
            if (initControls)
            {
                Location = loc;
                comboGame.SelectedIndex = comboGame.Items.IndexOf(comboGameText);
                if (comboGame.SelectedIndex >= 0)
                    comboGame_SelectionChangeCommitted(comboGame, null);
            }
        }

        private void ChangeTexts(ToolStripItem item)
        {
            string text = ResTexts.GetValue(item.Name);
            if (text.Length > 0)
                item.Text = text;
            if (item is ToolStripDropDownItem)
                foreach (ToolStripItem sub in ((ToolStripDropDownItem)item).DropDownItems)
                    ChangeTexts(sub);
        }

        // change text of a control
        // if it has child controls, call this recursively
        private void ChangeTexts(Control ctrl)
        {
            string text = ResTexts.GetValue(ctrl.Name);
            if (text.Length > 0)
                ctrl.Text = text;

            foreach (Control child in ctrl.Controls)
                ChangeTexts(child);
        }

        // initiate current game change
        private void comboGame_SelectionChangeCommitted(object sender, EventArgs e)
        {
            GameInstance game = ((GamesComboBoxItem)(comboGame.SelectedItem)).Game;
            if (game == null)
            {
                int idx = comboGame.SelectedIndex;
                if (comboGame.Items.Count > idx)
                {
                    game = ((GamesComboBoxItem)(comboGame.Items[idx + 1])).Game;
                    if (game != null)
                    {
                        comboGame.SelectedIndex = idx + 1;
                    }
                }
            }

            if (game != null)
            {
                LockThreadControls();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(workerRefreshGameInfo_DoWork);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerRefreshGameInfo_RunWorkerCompleted);
                bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);

                bw.RunWorkerAsync(game);

                //BackgroundWorker bw = new BackgroundWorker();
                //bw.DoWork += new DoWorkEventHandler(workerRefreshGameInfo_DoWork);
                //bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(workerRefreshGameInfo_RunWorkerCompleted);
                //bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
                //bw.RunWorkerAsync(comboGame.Text);
            }
            else
            {
                comboGame.DroppedDown = true;
            }
        }

        // initiate translation
        private void buttonTranslate_Click(object sender, EventArgs e)
        {
            LockThreadControls();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(workerTranslate_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(generalWorker_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
            bw.RunWorkerAsync(0);
        }

        // initiate restoration
        private void buttonRestore_Click(object sender, EventArgs e)
        {
            LockThreadControls();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(workerRestore_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(generalWorker_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
            bw.RunWorkerAsync(0);
        }

        private void menuItemSimulateTranslation_Click(object sender, EventArgs e)
        {
            LockThreadControls();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(workerTranslate_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(generalWorker_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
            bw.RunWorkerAsync(1);

        }

        // initiate restore simulation
        private void menuItemSimulateRestore_Click(object sender, EventArgs e)
        {
            LockThreadControls();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(workerRestore_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(generalWorker_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
            bw.RunWorkerAsync(1);
        }

        // initiate text export to file
        private void menuItemExtractTexts_Click(object sender, EventArgs e)
        {
            LockThreadControls();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(workerExtract_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(generalWorker_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
            bw.RunWorkerAsync();
        }

        // initiate converting translation texts to xml
        private void menuItemCompileTexts_Click(object sender, EventArgs e)
        {
            LockThreadControls();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(workerCompileTexts_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(generalWorker_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
            string lang = string.Empty;
            lang = (string)((ToolStripMenuItem)sender).Tag;
            bw.RunWorkerAsync(lang);
        }

        // initiate creating restoration point
        private void menuItemCreateRestoration_Click(object sender, EventArgs e)
        {
            LockThreadControls();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(workerCreateRestoration_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(generalWorker_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
            bw.RunWorkerAsync();
        }

        // generate files.txt
        private void generateFilestxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LockThreadControls();
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(workerGenerateFilesTxt_DoWork);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(generalWorker_RunWorkerCompleted);
            bw.ProgressChanged += new ProgressChangedEventHandler(generalWorker_ProgressChanged);
            bw.RunWorkerAsync();
        }

        // show language selector popup
        private void labelLang_Click(object sender, EventArgs e)
        {
            Point p = labelLang.Parent.PointToScreen(labelLang.Location);
            p.Y += labelLang.Height;
            p.X += labelLang.Width - contextLang.Width;
            contextLang.Show(p);
        }

        // update application text translations
        private void menuItemLangSelect_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem)
            {
                ToolStripMenuItem item = (ToolStripMenuItem)sender;
                if (item.Checked)
                    return;
                foreach (ToolStripItem sibling in item.Owner.Items)
                    if (sibling is ToolStripMenuItem)
                        ((ToolStripMenuItem)sibling).Checked = false;
                item.Checked = true;
                ChangeCulture(Settings.Cultures[item.ToolTipText]);
            }
        }

        // start game
        private void menuItemRunGame_Click(object sender, EventArgs e)
        {
            Process pr = new Process();
            //if (TRGameInfo.Game.InstallType == InstallTypeEnum.Steam)
            pr.StartInfo.FileName = "TombRaider.exe";
            pr.StartInfo.WorkingDirectory = TRGameInfo.Game.InstallFolder;
            pr.Start();
        }

        // start game with configuration
        private void menuItemRunGameWithConfiguration_Click(object sender, EventArgs e)
        {
            Process pr = new Process();

            pr.StartInfo.FileName = Path.Combine(TRGameInfo.Game.InstallFolder, TRGameInfo.Game.GameDefaults.ExeName);
            pr.StartInfo.Arguments = "-configure";
            pr.StartInfo.WorkingDirectory = TRGameInfo.Game.InstallFolder;
            pr.Start();
        }

        // navigate to our site from browser
        private void linkTombRaiderHU_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process pr = new Process();
            pr.StartInfo.FileName = "http://tombraiders.hu";
            pr.Start();
        }

        // show Extra popup
        private void buttonExtra_Click(object sender, EventArgs e)
        {
            Point p = buttonExtra.Parent.PointToScreen(buttonExtra.Location);
            p.Y += buttonExtra.Height;
            p.X += buttonExtra.Width - contextExtra.Width;
            contextExtra.Show(p);
        }

        // show full install path when mouse is over label
        private void labelInstallPath_MouseHover(object sender, EventArgs e)
        {
            ToolTip t = new ToolTip();
            if (sender is Label)
                t.SetToolTip((Label)sender, ((Label)sender).Text);
        }

        // it need to be called from main thread.
        // called after game selection change and application language change
        private void refreshGameInfoLabels(bool cancelled)
        {
            if (cancelled)
            {
                labelInstallPath.Text = GeneralTexts.Unknown;
                labelInstalledVersion.Text = GeneralTexts.Unknown;
                labelLastUsedProfile.Text = GeneralTexts.Unknown;
                labelSelectedLanguage.Text = GeneralTexts.Unknown;
                labelSelectedSubtitleLanguage.Text = GeneralTexts.Unknown;
                labelAvailableTranslationVersion.Text = GeneralTexts.Unknown;
            }
            else
            {
                TRGameStatus gameStatus = TRGameInfo.GameStatus;

                //if ((gameStatus & TRGameStatus.TranslationDataFileNotFound) != TRGameStatus.None)
                //    labelAvailableTranslationVersion.Text = Errors.TranslationDataFileNotFound;
                //else
                if (TRGameInfo.Game.TransFiles.Count > 0)
                    labelAvailableTranslationVersion.Text = TRGameInfo.Game.TransFiles[0].Description;
                else
                    labelAvailableTranslationVersion.Text = Errors.TranslationDataFileNotFound;

                if ((gameStatus & TRGameStatus.InstallDirectoryNotExist) != TRGameStatus.None)
                    labelInstallPath.Text = Errors.InstallDirectoryNotExist;
                else
                    labelInstallPath.Text = TRGameInfo.Game.InstallFolder;

                if ((gameStatus & TRGameStatus.DataFilesNotFound) != TRGameStatus.None)
                    labelInstallPath.Text = TRGameInfo.Game.InstallFolder + " (" + Errors.DataFilesNotFound + ")";

                labelInstalledVersion.Text = TRGameInfo.Game.VersionString;
                labelLastUsedProfile.Text = TRGameInfo.Game.UserSettings.ProfileName.Length > 0 ? TRGameInfo.Game.UserSettings.ProfileName : GeneralTexts.None;
                labelSelectedLanguage.Text = (TRGameInfo.Game.UserSettings.LangCode >= 0) ? LangNames.Localized((FileLanguage)TRGameInfo.Game.UserSettings.LangCode) : GeneralTexts.None;
                labelSelectedSubtitleLanguage.Text = (TRGameInfo.Game.UserSettings.SubLangCode >= 0) ? LangNames.Localized((FileLanguage)TRGameInfo.Game.UserSettings.SubLangCode) : GeneralTexts.None;
            }
            labelProgressText.Visible = false;
            labelProgressText.Text = string.Empty;
        }

        internal void LockThreadControls2()
        {
            activeBeforeLocked = this.ActiveControl;
            progressTask.Visible = true;
            labelProgressText.Visible = true;
            buttonExtra.Enabled = false;
            buttonTranslate.Enabled = false;
            buttonRestore.Enabled = false;
            comboGame.Enabled = false;
            labelLang.Enabled = false;

        }

        internal void UnlockThreadControls2()
        {
            if (progressTask.Value != 100)
                labelProgressText.Visible = false;
            progressTask.Visible = false;
            buttonExtra.Enabled = true;
            buttonTranslate.Enabled = true;
            buttonRestore.Enabled = true;
            comboGame.Enabled = true;
            labelLang.Enabled = true;
            if (activeBeforeLocked != null)
                activeBeforeLocked.Focus();
        }

    }
}
