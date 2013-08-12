using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Diagnostics;

namespace TRTR
{
    internal partial class form_Main : Form
    {
        Control activeBeforeLocked = null;
        internal void LockThreadControls()
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

        internal void UnlockThreadControls()
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

        // it need to be called from worker's thread.
        private void InitializeWorker(BackgroundWorker worker)
        {
            InitializeWorker(worker, false);
        }

        private void InitializeWorker(BackgroundWorker worker, bool ignoreGame)
        {
            Thread.CurrentThread.CurrentCulture = Application.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = Application.CurrentCulture;
            if (!ignoreGame && string.IsNullOrEmpty(TRGameInfo.Game.Name))
                throw new Exception(Errors.GameIsNotSelected);
        }

        // initiate game info update
        private void UpdateGameInfo()
        {
            string gameName = TRGameInfo.Game.Name;
            GameInstance selGame = null;
            foreach (GameInstance game in InstalledGames.Items)
            {
                if (game.Name == TRGameInfo.Game.Name)
                    selGame = game;
            }

            if (selGame != null)
                TRGameInfo.Load(selGame);
        }

        /**/
        private void workerRefreshGameInfo_DoWork(object sender, DoWorkEventArgs e)
        {
            InitializeWorker((BackgroundWorker)sender, true);
            if (!(e.Argument is GameInstance))
                throw new Exception(Errors.InvalidParameter);
            TRGameInfo.Load(/*(BackgroundWorker)sender, */(GameInstance)(e.Argument));
        }
        /**/
        private void workerRefreshGameInfo_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //UpdateGameInfo();
            UnlockThreadControls();
            if (e.Error != null)
            {
                Log.Write(e.Error);
                ErrorDialog.ShowError(e.Error);
                //MessageBox.Show(e.Error.Message, StaticTexts.error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            refreshGameInfoLabels(e.Error != null || e.Cancelled);
        }

        private void workerExtract_DoWork(object sender, DoWorkEventArgs e)
        {
            TRGameInfo.Extract();
            //InitializeWorker((BackgroundWorker)sender);
            //FileEntryList entryList = new FileEntryList((BackgroundWorker)sender);
            //entryList.Extract(TRGameInfo.Game.WorkFolder);
            //MovieSubtitles sub = new MovieSubtitles();
            //sub.Extract(TRGameInfo.Game.WorkFolder);
        }

        private void workerTranslate_DoWork(object sender, DoWorkEventArgs e)
        {
            InitializeWorker((BackgroundWorker)sender);
            TRGameInfo.Translate((Int32)(e.Argument) != 0);
        }

        private void workerCreateRestoration_DoWork(object sender, DoWorkEventArgs e)
        {
            //InitializeWorker((BackgroundWorker)sender);
            //FileEntryList entryList = new FileEntryList((BackgroundWorker)sender);
            //entryList.CreateRestoration();
        }

        private void workerGenerateFilesTxt_DoWork(object sender, DoWorkEventArgs e)
        {
            //InitializeWorker((BackgroundWorker)sender);
            //FileEntryList entryList = new FileEntryList((BackgroundWorker)sender);
            //entryList.GenerateFilesTxt();
        }

        private void workerRestore_DoWork(object sender, DoWorkEventArgs e)
        {
            //InitializeWorker((BackgroundWorker)sender);
            //FileEntryList entryList = new FileEntryList((BackgroundWorker)sender);
            //entryList.simulateWrite = (Int32)(e.Argument) != 0;
            //entryList.Restore();
        }

        private void workerCompileTexts_DoWork(object sender, DoWorkEventArgs e)
        {
            InitializeWorker((BackgroundWorker)sender);
            TranslationParser parser = new TranslationParser((string)e.Argument,
                (BackgroundWorker)sender);
        }
    }
}
