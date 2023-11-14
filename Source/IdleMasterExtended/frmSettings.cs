﻿using System;
using System.Drawing;
using System.Windows.Forms;
using IdleMasterExtended.Properties;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace IdleMasterExtended
{
    public partial class frmSettings : Form
    {

        private const string SortingMostCards = "mostcards";
        private const string SortingLeastCards = "leastcards";
        private const string SortingDefault = "default";

        public frmSettings()
        {
            InitializeComponent();
        }
        private void frmSettings_Load(object sender, EventArgs e)
        {
            LoadCurrentLanguage();
            LoadTranslation();
            LoadSortingMethod();
            LoadIdlingMethod();
            LoadMiscSettings();

            LoadCustomThemeSettings();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            UpdateSortingMethod();
            UpdateIdlingMethod();
            UpdateMiscSettings();

            CheckIfLanguageChanged();

            Settings.Default.Save();
            Close();
        }

        private void btnAdvanced_Click(object sender, EventArgs e)
        {
            var frm = new frmSettingsAdvanced();
            frm.ShowDialog();
        }

        private void darkThemeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Settings.Default.customTheme = darkThemeCheckBox.Checked;
            Settings.Default.whiteIcons = darkThemeCheckBox.Checked;
            LoadCustomThemeSettings();
        }

        private void chkShutdown_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShutdown.Checked)
            {
                if (MessageBox.Show("Are you sure you want Idle Master Extended to shutdown Windows when idling is done?\n\nNote: This setting will only be active once.",
                                    "Shutdown Windows", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    Settings.Default.ShutdownWindowsOnDone = chkShutdown.Checked;
                }
                else
                {
                    chkShutdown.Checked = false;
                }
            }
        }

        private void linkLabelSettings_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\IdleMasterExtended");
        }

        private void lnkGitHubWiki_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/JonasNilson/idle_master_extended/wiki");
        }


        private void LoadMiscSettings()
        {
            if (Settings.Default.minToTray)
            {
                chkMinToTray.Checked = true;
            }

            if (Settings.Default.ignoreclient)
            {
                chkIgnoreClientStatus.Checked = true;
            }

            if (Settings.Default.showUsername)
            {
                chkShowUsername.Checked = true;
            }

            if (Settings.Default.NoSleep)
            {
                chkPreventSleep.Checked = true;
            }

            if (Settings.Default.ShutdownWindowsOnDone)
            {
                chkShutdown.Checked = true;
            }

            if (Settings.Default.IdleOnlyPlayed)
            {
                chkIdleOnlyPlayed.Checked = true;
            }
        }

        private void LoadIdlingMethod()
        {
            if (Settings.Default.fastMode)
            {
                radFastMode.Checked = true;
            }
            else if (Settings.Default.IdlingModeWhitelist)
            {
                radWhitelistMode.Checked = true;
            }
            else if (Settings.Default.OneThenMany)
            {
                radOneThenMany.Checked = true;
            }
            else
            {
                radOneGameOnly.Checked = Settings.Default.OnlyOneGameIdle;
                radManyThenOne.Checked = !Settings.Default.OnlyOneGameIdle;
            }
        }

        private void LoadTranslation()
        {
            // Load translation
            this.Text = localization.strings.idle_master_settings;

            grpGeneral.Text = localization.strings.general;
            grpIdlingQuantity.Text = localization.strings.idling_behavior;
            grpPriority.Text = localization.strings.idling_order;
            btnOK.Text = localization.strings.accept;
            btnCancel.Text = localization.strings.cancel;

            ttHints.SetToolTip(btnAdvanced, localization.strings.advanced_auth);

            chkMinToTray.Text = localization.strings.minimize_to_tray;
            ttHints.SetToolTip(chkMinToTray, localization.strings.minimize_to_tray);

            chkIgnoreClientStatus.Text = localization.strings.ignore_client_status;
            ttHints.SetToolTip(chkIgnoreClientStatus, localization.strings.ignore_client_status);

            chkShowUsername.Text = localization.strings.show_username;
            ttHints.SetToolTip(chkShowUsername, localization.strings.show_username);

            radOneGameOnly.Text = localization.strings.idle_individual;
            ttHints.SetToolTip(radOneGameOnly, localization.strings.idle_individual);

            radManyThenOne.Text = localization.strings.idle_simultaneous;
            ttHints.SetToolTip(radManyThenOne, localization.strings.idle_simultaneous);

            radOneThenMany.Text = localization.strings.idle_onethenmany;
            ttHints.SetToolTip(radOneThenMany, localization.strings.idle_onethenmany);

            radIdleDefault.Text = localization.strings.order_default;
            ttHints.SetToolTip(radIdleDefault, localization.strings.order_default);

            radIdleMostDrops.Text = localization.strings.order_most;
            ttHints.SetToolTip(radIdleMostDrops, localization.strings.order_most);

            radIdleLeastDrops.Text = localization.strings.order_least;
            ttHints.SetToolTip(radIdleLeastDrops, localization.strings.order_least);

            lblLanguage.Text = localization.strings.interface_language;
        }

        private void LoadSortingMethod()
        {
            switch (Settings.Default.sort)
            {
                case SortingLeastCards:
                    radIdleLeastDrops.Checked = true;
                    break;
                case SortingMostCards:
                    radIdleMostDrops.Checked = true;
                    break;
                default:
                    break;
            }
        }

        private void LoadCurrentLanguage()
        {
            if (Settings.Default.language != "")
            {
                cboLanguage.SelectedItem = Settings.Default.language;
            }
            else
            {
                switch (Thread.CurrentThread.CurrentUICulture.EnglishName)
                {
                    case "Chinese (Simplified, China)":
                    case "Chinese (Traditional, China)":
                    case "Portuguese (Brazil)":
                        cboLanguage.SelectedItem = Thread.CurrentThread.CurrentUICulture.EnglishName;
                        break;
                    default:
                        cboLanguage.SelectedItem = Regex.Replace(Thread.CurrentThread.CurrentUICulture.EnglishName, @"\(.+\)", "").Trim();
                        break;
                }
            }
        }

        private void CheckIfLanguageChanged()
        {
            if (cboLanguage.Text != "")
            {
                if (cboLanguage.Text != Settings.Default.language)
                {
                    MessageBox.Show(localization.strings.please_restart);
                }
                Settings.Default.language = cboLanguage.Text;
            }
        }

        private void UpdateMiscSettings()
        {
            Settings.Default.minToTray = chkMinToTray.Checked;
            Settings.Default.ignoreclient = chkIgnoreClientStatus.Checked;
            Settings.Default.showUsername = chkShowUsername.Checked;
            Settings.Default.NoSleep = chkPreventSleep.Checked;
            Settings.Default.ShutdownWindowsOnDone = chkShutdown.Checked;
            Settings.Default.IdleOnlyPlayed = chkIdleOnlyPlayed.Checked;
        }

        private void UpdateIdlingMethod()
        {
            Settings.Default.OneThenMany = Settings.Default.OnlyOneGameIdle
                = Settings.Default.fastMode = Settings.Default.IdlingModeWhitelist = false;

            if (radFastMode.Checked)
            {
                Settings.Default.fastMode = true;
            }
            else if (radWhitelistMode.Checked)
            {
                Settings.Default.IdlingModeWhitelist = true;
            }
            else if (radOneThenMany.Checked)
            {
                Settings.Default.OneThenMany = true;
            }
            else
            {
                Settings.Default.OnlyOneGameIdle = !radManyThenOne.Checked;
            }
        }

        private void UpdateSortingMethod()
        {
            if (radIdleDefault.Checked)
            {
                Settings.Default.sort = SortingDefault;
            }
            else if (radIdleLeastDrops.Checked)
            {
                Settings.Default.sort = SortingLeastCards;
            }
            else if (radIdleMostDrops.Checked)
            {
                Settings.Default.sort = SortingMostCards;
            }
        }


        private void LoadCustomThemeSettings()
        {
            ThemeHandler.SetTheme(this, Settings.Default.customTheme);
            Settings.Default.Save();
        }
    }
}
