namespace TRTR
{
    partial class form_Main
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
            this.components = new System.ComponentModel.Container();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonExtra = new System.Windows.Forms.Button();
            this.labelProgressText = new System.Windows.Forms.Label();
            this.labelVersion = new System.Windows.Forms.Label();
            this.linkTombRaiderHU = new System.Windows.Forms.LinkLabel();
            this.labelLang = new System.Windows.Forms.Label();
            this.contextLang = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.buttonTranslate = new System.Windows.Forms.Button();
            this.buttonRestore = new System.Windows.Forms.Button();
            this.labelGame = new System.Windows.Forms.Label();
            this.comboGame = new System.Windows.Forms.ComboBox();
            this.groupBoxGameInfo = new System.Windows.Forms.GroupBox();
            this.buttonTest1 = new System.Windows.Forms.Button();
            this.labelAvailableTranslationVersion = new System.Windows.Forms.Label();
            this.labelSelectedSubtitleLanguage = new System.Windows.Forms.Label();
            this.labelSelectedLanguage = new System.Windows.Forms.Label();
            this.labelLastUsedProfile = new System.Windows.Forms.Label();
            this.labelInstalledVersion = new System.Windows.Forms.Label();
            this.labelInstallPath = new System.Windows.Forms.Label();
            this.labelSelectedSubtitleLanguageText = new System.Windows.Forms.Label();
            this.labelAvailableTranslationVersionText = new System.Windows.Forms.Label();
            this.labelSelectedLanguageText = new System.Windows.Forms.Label();
            this.labelLastUsedProfileText = new System.Windows.Forms.Label();
            this.labelInstalledVersionText = new System.Windows.Forms.Label();
            this.labelInstallPathText = new System.Windows.Forms.Label();
            this.progressTask = new System.Windows.Forms.ProgressBar();
            this.contextExtra = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.menuItemExtractTexts = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemCompileTexts = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemCompileTextsHU = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemCompileTextsBR = new System.Windows.Forms.ToolStripMenuItem();
            this.bulgarianToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemCreateRestoration = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemRunGame = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemRunGameWithConfiguration = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemSimulateTranslation = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemSimulateRestoration = new System.Windows.Forms.ToolStripMenuItem();
            this.generateFilestxtToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemConvertOldTranslationsHU = new System.Windows.Forms.ToolStripMenuItem();
            this.menuItemConvertOldTranslationsPT = new System.Windows.Forms.ToolStripMenuItem();
            this.bulgarianBGToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.panel1.SuspendLayout();
            this.groupBoxGameInfo.SuspendLayout();
            this.contextExtra.SuspendLayout();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBox1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(553, 267);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 7;
            this.pictureBox1.TabStop = false;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.Controls.Add(this.buttonExtra);
            this.panel1.Controls.Add(this.labelProgressText);
            this.panel1.Controls.Add(this.labelVersion);
            this.panel1.Controls.Add(this.linkTombRaiderHU);
            this.panel1.Controls.Add(this.labelLang);
            this.panel1.Controls.Add(this.buttonTranslate);
            this.panel1.Controls.Add(this.buttonRestore);
            this.panel1.Controls.Add(this.labelGame);
            this.panel1.Controls.Add(this.comboGame);
            this.panel1.Controls.Add(this.groupBoxGameInfo);
            this.panel1.Controls.Add(this.progressTask);
            this.panel1.Location = new System.Drawing.Point(12, 15);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(530, 240);
            this.panel1.TabIndex = 24;
            // 
            // buttonExtra
            // 
            this.buttonExtra.Enabled = false;
            this.buttonExtra.FlatAppearance.BorderSize = 0;
            this.buttonExtra.Location = new System.Drawing.Point(447, 15);
            this.buttonExtra.Name = "buttonExtra";
            this.buttonExtra.Size = new System.Drawing.Size(75, 23);
            this.buttonExtra.TabIndex = 39;
            this.buttonExtra.Text = "E&xtra";
            this.buttonExtra.UseVisualStyleBackColor = true;
            this.buttonExtra.Click += new System.EventHandler(this.buttonExtra_Click);
            // 
            // labelProgressText
            // 
            this.labelProgressText.AutoSize = true;
            this.labelProgressText.Location = new System.Drawing.Point(166, 192);
            this.labelProgressText.Name = "labelProgressText";
            this.labelProgressText.Size = new System.Drawing.Size(48, 13);
            this.labelProgressText.TabIndex = 38;
            this.labelProgressText.Text = "Progress";
            this.labelProgressText.Visible = false;
            // 
            // labelVersion
            // 
            this.labelVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.labelVersion.AutoSize = true;
            this.labelVersion.BackColor = System.Drawing.Color.Transparent;
            this.labelVersion.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelVersion.Location = new System.Drawing.Point(265, 227);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(53, 13);
            this.labelVersion.TabIndex = 37;
            this.labelVersion.Text = "<version>";
            // 
            // linkTombRaiderHU
            // 
            this.linkTombRaiderHU.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.linkTombRaiderHU.AutoSize = true;
            this.linkTombRaiderHU.Cursor = System.Windows.Forms.Cursors.Hand;
            this.linkTombRaiderHU.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.linkTombRaiderHU.Location = new System.Drawing.Point(398, 227);
            this.linkTombRaiderHU.Name = "linkTombRaiderHU";
            this.linkTombRaiderHU.Size = new System.Drawing.Size(129, 13);
            this.linkTombRaiderHU.TabIndex = 36;
            this.linkTombRaiderHU.TabStop = true;
            this.linkTombRaiderHU.Text = "http://www.tombraider.hu";
            this.linkTombRaiderHU.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkTombRaiderHU_LinkClicked);
            // 
            // labelLang
            // 
            this.labelLang.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelLang.AutoSize = true;
            this.labelLang.BackColor = System.Drawing.SystemColors.Highlight;
            this.labelLang.ContextMenuStrip = this.contextLang;
            this.labelLang.ForeColor = System.Drawing.SystemColors.HighlightText;
            this.labelLang.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelLang.Location = new System.Drawing.Point(499, 192);
            this.labelLang.Name = "labelLang";
            this.labelLang.Size = new System.Drawing.Size(25, 13);
            this.labelLang.TabIndex = 35;
            this.labelLang.Text = "MM";
            this.labelLang.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.labelLang.Click += new System.EventHandler(this.labelLang_Click);
            // 
            // contextLang
            // 
            this.contextLang.Name = "contextLang";
            this.contextLang.ShowCheckMargin = true;
            this.contextLang.ShowImageMargin = false;
            this.contextLang.ShowItemToolTips = false;
            this.contextLang.Size = new System.Drawing.Size(61, 4);
            // 
            // buttonTranslate
            // 
            this.buttonTranslate.Enabled = false;
            this.buttonTranslate.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.buttonTranslate.Location = new System.Drawing.Point(6, 187);
            this.buttonTranslate.Name = "buttonTranslate";
            this.buttonTranslate.Size = new System.Drawing.Size(75, 23);
            this.buttonTranslate.TabIndex = 30;
            this.buttonTranslate.Text = "&Translate";
            this.buttonTranslate.UseVisualStyleBackColor = true;
            this.buttonTranslate.Click += new System.EventHandler(this.buttonTranslate_Click);
            // 
            // buttonRestore
            // 
            this.buttonRestore.Enabled = false;
            this.buttonRestore.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.buttonRestore.Location = new System.Drawing.Point(85, 187);
            this.buttonRestore.Name = "buttonRestore";
            this.buttonRestore.Size = new System.Drawing.Size(75, 23);
            this.buttonRestore.TabIndex = 29;
            this.buttonRestore.Text = "&Restore";
            this.buttonRestore.Click += new System.EventHandler(this.buttonRestore_Click);
            // 
            // labelGame
            // 
            this.labelGame.AutoSize = true;
            this.labelGame.BackColor = System.Drawing.Color.Transparent;
            this.labelGame.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelGame.Location = new System.Drawing.Point(3, 0);
            this.labelGame.Name = "labelGame";
            this.labelGame.Size = new System.Drawing.Size(81, 13);
            this.labelGame.TabIndex = 23;
            this.labelGame.Text = "Choose a &game";
            // 
            // comboGame
            // 
            this.comboGame.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboGame.FormattingEnabled = true;
            this.comboGame.Location = new System.Drawing.Point(5, 16);
            this.comboGame.Name = "comboGame";
            this.comboGame.Size = new System.Drawing.Size(245, 21);
            this.comboGame.TabIndex = 24;
            this.comboGame.SelectionChangeCommitted += new System.EventHandler(this.comboGame_SelectionChangeCommitted);
            // 
            // groupBoxGameInfo
            // 
            this.groupBoxGameInfo.BackColor = System.Drawing.Color.Transparent;
            this.groupBoxGameInfo.Controls.Add(this.buttonTest1);
            this.groupBoxGameInfo.Controls.Add(this.labelAvailableTranslationVersion);
            this.groupBoxGameInfo.Controls.Add(this.labelSelectedSubtitleLanguage);
            this.groupBoxGameInfo.Controls.Add(this.labelSelectedLanguage);
            this.groupBoxGameInfo.Controls.Add(this.labelLastUsedProfile);
            this.groupBoxGameInfo.Controls.Add(this.labelInstalledVersion);
            this.groupBoxGameInfo.Controls.Add(this.labelInstallPath);
            this.groupBoxGameInfo.Controls.Add(this.labelSelectedSubtitleLanguageText);
            this.groupBoxGameInfo.Controls.Add(this.labelAvailableTranslationVersionText);
            this.groupBoxGameInfo.Controls.Add(this.labelSelectedLanguageText);
            this.groupBoxGameInfo.Controls.Add(this.labelLastUsedProfileText);
            this.groupBoxGameInfo.Controls.Add(this.labelInstalledVersionText);
            this.groupBoxGameInfo.Controls.Add(this.labelInstallPathText);
            this.groupBoxGameInfo.Location = new System.Drawing.Point(5, 43);
            this.groupBoxGameInfo.Name = "groupBoxGameInfo";
            this.groupBoxGameInfo.Size = new System.Drawing.Size(517, 138);
            this.groupBoxGameInfo.TabIndex = 26;
            this.groupBoxGameInfo.TabStop = false;
            this.groupBoxGameInfo.Text = "Game Informations";
            // 
            // buttonTest1
            // 
            this.buttonTest1.Location = new System.Drawing.Point(436, 109);
            this.buttonTest1.Name = "buttonTest1";
            this.buttonTest1.Size = new System.Drawing.Size(75, 23);
            this.buttonTest1.TabIndex = 18;
            this.buttonTest1.Text = "button1";
            this.buttonTest1.UseVisualStyleBackColor = true;
            this.buttonTest1.Click += new System.EventHandler(this.buttonTest1_Click);
            // 
            // labelAvailableTranslationVersion
            // 
            this.labelAvailableTranslationVersion.AutoSize = true;
            this.labelAvailableTranslationVersion.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelAvailableTranslationVersion.Location = new System.Drawing.Point(176, 81);
            this.labelAvailableTranslationVersion.Name = "labelAvailableTranslationVersion";
            this.labelAvailableTranslationVersion.Size = new System.Drawing.Size(0, 13);
            this.labelAvailableTranslationVersion.TabIndex = 17;
            // 
            // labelSelectedSubtitleLanguage
            // 
            this.labelSelectedSubtitleLanguage.AutoSize = true;
            this.labelSelectedSubtitleLanguage.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelSelectedSubtitleLanguage.Location = new System.Drawing.Point(176, 68);
            this.labelSelectedSubtitleLanguage.Name = "labelSelectedSubtitleLanguage";
            this.labelSelectedSubtitleLanguage.Size = new System.Drawing.Size(0, 13);
            this.labelSelectedSubtitleLanguage.TabIndex = 14;
            // 
            // labelSelectedLanguage
            // 
            this.labelSelectedLanguage.AutoSize = true;
            this.labelSelectedLanguage.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelSelectedLanguage.Location = new System.Drawing.Point(176, 55);
            this.labelSelectedLanguage.Name = "labelSelectedLanguage";
            this.labelSelectedLanguage.Size = new System.Drawing.Size(0, 13);
            this.labelSelectedLanguage.TabIndex = 13;
            // 
            // labelLastUsedProfile
            // 
            this.labelLastUsedProfile.AutoSize = true;
            this.labelLastUsedProfile.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelLastUsedProfile.Location = new System.Drawing.Point(176, 42);
            this.labelLastUsedProfile.Name = "labelLastUsedProfile";
            this.labelLastUsedProfile.Size = new System.Drawing.Size(0, 13);
            this.labelLastUsedProfile.TabIndex = 12;
            // 
            // labelInstalledVersion
            // 
            this.labelInstalledVersion.AutoSize = true;
            this.labelInstalledVersion.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelInstalledVersion.Location = new System.Drawing.Point(176, 29);
            this.labelInstalledVersion.Name = "labelInstalledVersion";
            this.labelInstalledVersion.Size = new System.Drawing.Size(0, 13);
            this.labelInstalledVersion.TabIndex = 11;
            // 
            // labelInstallPath
            // 
            this.labelInstallPath.AutoSize = true;
            this.labelInstallPath.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.labelInstallPath.Location = new System.Drawing.Point(176, 16);
            this.labelInstallPath.Name = "labelInstallPath";
            this.labelInstallPath.Size = new System.Drawing.Size(0, 13);
            this.labelInstallPath.TabIndex = 10;
            this.labelInstallPath.MouseHover += new System.EventHandler(this.labelInstallPath_MouseHover);
            // 
            // labelSelectedSubtitleLanguageText
            // 
            this.labelSelectedSubtitleLanguageText.AutoSize = true;
            this.labelSelectedSubtitleLanguageText.BackColor = System.Drawing.Color.Transparent;
            this.labelSelectedSubtitleLanguageText.Location = new System.Drawing.Point(6, 68);
            this.labelSelectedSubtitleLanguageText.Name = "labelSelectedSubtitleLanguageText";
            this.labelSelectedSubtitleLanguageText.Size = new System.Drawing.Size(138, 13);
            this.labelSelectedSubtitleLanguageText.TabIndex = 9;
            this.labelSelectedSubtitleLanguageText.Text = "Selected Subtitle Language";
            // 
            // labelAvailableTranslationVersionText
            // 
            this.labelAvailableTranslationVersionText.AutoSize = true;
            this.labelAvailableTranslationVersionText.BackColor = System.Drawing.Color.Transparent;
            this.labelAvailableTranslationVersionText.Location = new System.Drawing.Point(6, 81);
            this.labelAvailableTranslationVersionText.Name = "labelAvailableTranslationVersionText";
            this.labelAvailableTranslationVersionText.Size = new System.Drawing.Size(143, 13);
            this.labelAvailableTranslationVersionText.TabIndex = 8;
            this.labelAvailableTranslationVersionText.Text = "Available Translation Version";
            // 
            // labelSelectedLanguageText
            // 
            this.labelSelectedLanguageText.BackColor = System.Drawing.Color.Transparent;
            this.labelSelectedLanguageText.Location = new System.Drawing.Point(6, 55);
            this.labelSelectedLanguageText.Name = "labelSelectedLanguageText";
            this.labelSelectedLanguageText.Size = new System.Drawing.Size(100, 23);
            this.labelSelectedLanguageText.TabIndex = 3;
            this.labelSelectedLanguageText.Text = "Selected Language";
            // 
            // labelLastUsedProfileText
            // 
            this.labelLastUsedProfileText.AutoSize = true;
            this.labelLastUsedProfileText.BackColor = System.Drawing.Color.Transparent;
            this.labelLastUsedProfileText.Location = new System.Drawing.Point(6, 42);
            this.labelLastUsedProfileText.Name = "labelLastUsedProfileText";
            this.labelLastUsedProfileText.Size = new System.Drawing.Size(87, 13);
            this.labelLastUsedProfileText.TabIndex = 2;
            this.labelLastUsedProfileText.Text = "Last Used Profile";
            // 
            // labelInstalledVersionText
            // 
            this.labelInstalledVersionText.AutoSize = true;
            this.labelInstalledVersionText.BackColor = System.Drawing.Color.Transparent;
            this.labelInstalledVersionText.Location = new System.Drawing.Point(6, 29);
            this.labelInstalledVersionText.Name = "labelInstalledVersionText";
            this.labelInstalledVersionText.Size = new System.Drawing.Size(84, 13);
            this.labelInstalledVersionText.TabIndex = 1;
            this.labelInstalledVersionText.Text = "Installed Version";
            // 
            // labelInstallPathText
            // 
            this.labelInstallPathText.AutoSize = true;
            this.labelInstallPathText.BackColor = System.Drawing.Color.Transparent;
            this.labelInstallPathText.Location = new System.Drawing.Point(6, 16);
            this.labelInstallPathText.Name = "labelInstallPathText";
            this.labelInstallPathText.Size = new System.Drawing.Size(59, 13);
            this.labelInstallPathText.TabIndex = 0;
            this.labelInstallPathText.Text = "Install Path";
            // 
            // progressTask
            // 
            this.progressTask.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.progressTask.Location = new System.Drawing.Point(169, 208);
            this.progressTask.Name = "progressTask";
            this.progressTask.Size = new System.Drawing.Size(323, 13);
            this.progressTask.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressTask.TabIndex = 28;
            this.progressTask.Value = 20;
            this.progressTask.Visible = false;
            // 
            // contextExtra
            // 
            this.contextExtra.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuItemExtractTexts,
            this.menuItemCompileTexts,
            this.menuItemCreateRestoration,
            this.menuItemRunGame,
            this.menuItemRunGameWithConfiguration,
            this.menuItemSimulateTranslation,
            this.menuItemSimulateRestoration,
            this.generateFilestxtToolStripMenuItem});
            this.contextExtra.Name = "contextMenuStrip1";
            this.contextExtra.Size = new System.Drawing.Size(257, 202);
            // 
            // menuItemExtractTexts
            // 
            this.menuItemExtractTexts.Name = "menuItemExtractTexts";
            this.menuItemExtractTexts.Size = new System.Drawing.Size(256, 22);
            this.menuItemExtractTexts.Text = "&Extract texts from datafiles";
            this.menuItemExtractTexts.Click += new System.EventHandler(this.menuItemExtractTexts_Click);
            // 
            // menuItemCompileTexts
            // 
            this.menuItemCompileTexts.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuItemCompileTextsHU,
            this.menuItemCompileTextsBR,
            this.bulgarianToolStripMenuItem});
            this.menuItemCompileTexts.Name = "menuItemCompileTexts";
            this.menuItemCompileTexts.Size = new System.Drawing.Size(256, 22);
            this.menuItemCompileTexts.Text = "Compile &text translations into XML";
            // 
            // menuItemCompileTextsHU
            // 
            this.menuItemCompileTextsHU.Name = "menuItemCompileTextsHU";
            this.menuItemCompileTextsHU.Size = new System.Drawing.Size(181, 22);
            this.menuItemCompileTextsHU.Tag = "hu";
            this.menuItemCompileTextsHU.Text = "Hungarian";
            this.menuItemCompileTextsHU.Click += new System.EventHandler(this.menuItemCompileTexts_Click);
            // 
            // menuItemCompileTextsBR
            // 
            this.menuItemCompileTextsBR.Name = "menuItemCompileTextsBR";
            this.menuItemCompileTextsBR.Size = new System.Drawing.Size(181, 22);
            this.menuItemCompileTextsBR.Tag = "pt";
            this.menuItemCompileTextsBR.Text = "Brazilian Portuguese";
            this.menuItemCompileTextsBR.Click += new System.EventHandler(this.menuItemCompileTexts_Click);
            // 
            // bulgarianToolStripMenuItem
            // 
            this.bulgarianToolStripMenuItem.Name = "bulgarianToolStripMenuItem";
            this.bulgarianToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.bulgarianToolStripMenuItem.Tag = "bg";
            this.bulgarianToolStripMenuItem.Text = "Bulgarian";
            this.bulgarianToolStripMenuItem.Click += new System.EventHandler(this.menuItemCompileTexts_Click);
            // 
            // menuItemCreateRestoration
            // 
            this.menuItemCreateRestoration.Name = "menuItemCreateRestoration";
            this.menuItemCreateRestoration.Size = new System.Drawing.Size(256, 22);
            this.menuItemCreateRestoration.Text = "Create &restoration XML";
            this.menuItemCreateRestoration.Click += new System.EventHandler(this.menuItemCreateRestoration_Click);
            // 
            // menuItemRunGame
            // 
            this.menuItemRunGame.Name = "menuItemRunGame";
            this.menuItemRunGame.Size = new System.Drawing.Size(256, 22);
            this.menuItemRunGame.Text = "Run &game";
            this.menuItemRunGame.Click += new System.EventHandler(this.menuItemRunGame_Click);
            // 
            // menuItemRunGameWithConfiguration
            // 
            this.menuItemRunGameWithConfiguration.Name = "menuItemRunGameWithConfiguration";
            this.menuItemRunGameWithConfiguration.Size = new System.Drawing.Size(256, 22);
            this.menuItemRunGameWithConfiguration.Text = "Run game &with configuration";
            this.menuItemRunGameWithConfiguration.Click += new System.EventHandler(this.menuItemRunGameWithConfiguration_Click);
            // 
            // menuItemSimulateTranslation
            // 
            this.menuItemSimulateTranslation.Name = "menuItemSimulateTranslation";
            this.menuItemSimulateTranslation.Size = new System.Drawing.Size(256, 22);
            this.menuItemSimulateTranslation.Text = "Simulate translation";
            this.menuItemSimulateTranslation.Click += new System.EventHandler(this.menuItemSimulateTranslation_Click);
            // 
            // menuItemSimulateRestoration
            // 
            this.menuItemSimulateRestoration.Name = "menuItemSimulateRestoration";
            this.menuItemSimulateRestoration.Size = new System.Drawing.Size(256, 22);
            this.menuItemSimulateRestoration.Text = "Simulate restoration";
            this.menuItemSimulateRestoration.Click += new System.EventHandler(this.menuItemSimulateRestore_Click);
            // 
            // generateFilestxtToolStripMenuItem
            // 
            this.generateFilestxtToolStripMenuItem.Name = "generateFilestxtToolStripMenuItem";
            this.generateFilestxtToolStripMenuItem.Size = new System.Drawing.Size(256, 22);
            this.generateFilestxtToolStripMenuItem.Text = "Generate files.txt";
            this.generateFilestxtToolStripMenuItem.Click += new System.EventHandler(this.generateFilestxtToolStripMenuItem_Click);
            // 
            // menuItemConvertOldTranslationsHU
            // 
            this.menuItemConvertOldTranslationsHU.Name = "menuItemConvertOldTranslationsHU";
            this.menuItemConvertOldTranslationsHU.Size = new System.Drawing.Size(32, 19);
            // 
            // menuItemConvertOldTranslationsPT
            // 
            this.menuItemConvertOldTranslationsPT.Name = "menuItemConvertOldTranslationsPT";
            this.menuItemConvertOldTranslationsPT.Size = new System.Drawing.Size(32, 19);
            // 
            // bulgarianBGToolStripMenuItem
            // 
            this.bulgarianBGToolStripMenuItem.Name = "bulgarianBGToolStripMenuItem";
            this.bulgarianBGToolStripMenuItem.Size = new System.Drawing.Size(181, 22);
            this.bulgarianBGToolStripMenuItem.Tag = "bg";
            this.bulgarianBGToolStripMenuItem.Text = "Bulgarian";
            // 
            // form_Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(553, 267);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.pictureBox1);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Location = new System.Drawing.Point(100, 100);
            this.MaximizeBox = false;
            this.Name = "form_Main";
            this.Text = "TRTR";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.form_Main_FormClosing);
            this.Load += new System.EventHandler(this.form_Main_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.groupBoxGameInfo.ResumeLayout(false);
            this.groupBoxGameInfo.PerformLayout();
            this.contextExtra.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonTranslate;
        private System.Windows.Forms.Button buttonRestore;
        private System.Windows.Forms.Label labelGame;
        private System.Windows.Forms.ComboBox comboGame;
        private System.Windows.Forms.ProgressBar progressTask;
        private System.Windows.Forms.ContextMenuStrip contextExtra;
        private System.Windows.Forms.ToolStripMenuItem menuItemExtractTexts;
        private System.Windows.Forms.ToolStripMenuItem menuItemCompileTexts;
        private System.Windows.Forms.ToolStripMenuItem menuItemCreateRestoration;
        private System.Windows.Forms.Label labelLang;
        private System.Windows.Forms.ContextMenuStrip contextLang;
        private System.Windows.Forms.ToolStripMenuItem menuItemRunGame;
        private System.Windows.Forms.ToolStripMenuItem menuItemRunGameWithConfiguration;
        private System.Windows.Forms.ToolStripMenuItem menuItemSimulateTranslation;
        private System.Windows.Forms.ToolStripMenuItem menuItemSimulateRestoration;
        private System.Windows.Forms.LinkLabel linkTombRaiderHU;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.GroupBox groupBoxGameInfo;
        private System.Windows.Forms.Label labelAvailableTranslationVersion;
        private System.Windows.Forms.Label labelSelectedSubtitleLanguage;
        private System.Windows.Forms.Label labelSelectedLanguage;
        private System.Windows.Forms.Label labelLastUsedProfile;
        private System.Windows.Forms.Label labelInstalledVersion;
        private System.Windows.Forms.Label labelInstallPath;
        private System.Windows.Forms.Label labelSelectedSubtitleLanguageText;
        private System.Windows.Forms.Label labelAvailableTranslationVersionText;
        private System.Windows.Forms.Label labelSelectedLanguageText;
        private System.Windows.Forms.Label labelLastUsedProfileText;
        private System.Windows.Forms.Label labelInstalledVersionText;
        private System.Windows.Forms.Label labelInstallPathText;
        private System.Windows.Forms.ToolStripMenuItem menuItemCompileTextsHU;
        private System.Windows.Forms.ToolStripMenuItem menuItemCompileTextsBR;
        private System.Windows.Forms.Label labelProgressText;
        private System.Windows.Forms.Button buttonExtra;
        private System.Windows.Forms.ToolStripMenuItem menuItemConvertOldTranslationsHU;
        private System.Windows.Forms.ToolStripMenuItem menuItemConvertOldTranslationsPT;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolStripMenuItem bulgarianBGToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bulgarianToolStripMenuItem;
        private System.Windows.Forms.Button buttonTest1;
        private System.Windows.Forms.ToolStripMenuItem generateFilestxtToolStripMenuItem;

    }
}

