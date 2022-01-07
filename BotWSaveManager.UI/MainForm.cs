using BotWSaveManager.Conversion;
using BotWSaveManager.Conversion.IO;
using BotWSaveManager.UI.Logging;
using BotWSaveManager.UI.Properties;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BotWSaveManager.UI
{
    public partial class MainForm : Form
    {
        public Save SelectedSave;
        public Dictionary<string, byte[]> SaveFilesDictionary;

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public MainForm()
        {
            InitializeComponent();

            Directory.CreateDirectory(Path.Combine(Application.StartupPath, "Logs"));

            string logFileName = $"{DateTime.Now:dd-MM-yy}-1.log";

            int index = 2;
            while (File.Exists(Path.Combine(Application.StartupPath, "Logs", logFileName)))
            {
                logFileName = $"{DateTime.Now:dd-MM-yy}-{index}.log";
                index++;
            }

            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget logfile = new FileTarget("logfile")
            {
                FileName = Path.Combine(Application.StartupPath, "Logs", logFileName),
                Layout = "${time} ${level:uppercase=true} ${logger} ${message}"
            };
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);

            LogManager.Configuration = config;
        }

        private void OpenSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.Info("Initiating save folder selection.");

            FolderBrowserDialog dia = new FolderBrowserDialog();

            if (dia.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Logger.Info($"Save folder {dia.SelectedPath} selected.");
                    Logger.Trace($"Logging directory structure...\n{LogHelper.VisualizeDirectoryAsString(dia.SelectedPath)}");

                    SelectedSave = new Save(dia.SelectedPath);
                }
                catch (UnsupportedSaveException exception)
                {
                    Logger.Error(exception, "Selected save folder failed to initialize.");

                    if (exception.IsSwitch)
                    {
                        Logger.Error("The version of a numbered Switch save folder selected could not be retrieved. Prompting user to ignore error.");

                        if (MessageBox.Show("The version of a numbered save folder you selected cannot be retrieved. If you would like to attempt to use this file anyways, select Yes.", "Possibly unsupported save.", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            Logger.Info("User chose to ignore error. Initializing save with Switch version checking disabled.");
                            SelectedSave = new Save(dia.SelectedPath, true);
                        }
                        else
                        {
                            Logger.Fatal(exception, "Aborting save loading as per user request.");
                            MessageBox.Show(exception.Message);
                            ResetControls();
                            return;
                        }
                    }
                    else
                    {
                        Logger.Error("The version of a numbered Wii U save folder selected could not be retrieved. Prompting user to ignore error.");

                        if (MessageBox.Show("The version of a numbered save folder you selected cannot be retrieved. If you would like to attempt to use this file anyways, select Yes.", "Possibly unsupported save.", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            Logger.Info("User chose to ignore error. Initializing save with Wii U version checking disabled.");
                            SelectedSave = new Save(dia.SelectedPath, true);
                        }
                        else
                        {
                            Logger.Fatal(exception, "Aborting save loading as per user request.");
                            MessageBox.Show(exception.Message);
                            ResetControls();
                            return;
                        }
                    }
                }

                if (SelectedSave?.SaveFolder == null)
                {
                    Logger.Fatal("The save folder selected by the user is null. Resetting controls and notifying user.");
                    MessageBox.Show("The selected folder is null or invalid. Try running this app as administrator.");
                    ResetControls();
                    return;
                }

                SaveFilesDictionary = new Dictionary<string, byte[]>();

                foreach (string file in Directory.GetFiles(SelectedSave.SaveFolder, "*.sav", SearchOption.AllDirectories))
                {
                    Logger.Info($"Loading save file {file}.");
                    SaveFilesDictionary.Add(file, File.ReadAllBytes(file));
                }

                lblFileWarning.Hide();

                pbConsole.Image = SelectedSave.SaveConsoleType == Save.SaveType.Switch ? Resources.nintendo_switch_logo : Resources.wii_u_logo;
                lblConsoleName.Text = string.Empty;

                for (int index = 0; index < SelectedSave.SaveVersionList.Count; index++)
                {
                    string s = SelectedSave.SaveVersionList[index];
                    Logger.Info($"Save {index} is of version \"{s}\".");
                    lblConsoleName.Text += $"Save {index}: {s}\n";
                }

                btnConvert.Show();
                tbSaveLocation.Show();
                btnBrowse.Show();
                btnSaveToFiles.Show();
            }
            else
            {
                Logger.Info("User did not select a file.");
                MessageBox.Show("Please select your save folder before continuing.");
            }
        }

        private void ResetControls()
        {
            btnConvert.Hide();
            tbSaveLocation.Hide();
            btnBrowse.Hide();
            btnSaveToFiles.Hide();
            pbConsole.Image = null;
            lblConsoleName.Text = "";
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.Trace("User opened about dialog.");
            new About().Show();
        }

        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Logger.Trace("User opened help dialog.");
            new Help().ShowDialog();
        }

        private void BtnConvert_Click(object sender, EventArgs e)
        {
            Enabled = false;
            try
            {
                Logger.Info("Save conversion initiated.");
                SaveFilesDictionary = SelectedSave.ConvertSave();
            }
            catch (Exception exception)
            {
                Logger.Fatal("Generic error caught during save conversion.");
                Logger.Fatal(exception);

                MessageBox.Show("Error converting save file! Create an issue at https://github.com/JordanZeotni/BotW-Save-Manager with the error log and describe your game progress to the best of your ability.", exception.Message);

                Enabled = true;
                return;
            }

            Logger.Info("Save seems to have converted correctly. Notifying user and updating UI to reflect conversion.");
            MessageBox.Show("Save converted successfully! Just save the folder and copy it to your Nintendo console.");

            Enabled = true;

            pbConsole.Image = SelectedSave.SaveConsoleType == Save.SaveType.Switch ? Resources.nintendo_switch_logo : Resources.wii_u_logo;
            lblConsoleName.Text = string.Empty;

            for (int index = 0; index < SelectedSave.SaveVersionList.Count; index++)
            {
                string s = SelectedSave.SaveVersionList[index];
                lblConsoleName.Text += $"Save {index}: {s}\n";
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            Logger.Info("User selecting location to write resulting save folder.");

            FolderBrowserDialog dia = new FolderBrowserDialog();

            if (dia.ShowDialog() == DialogResult.OK)
            {
                Logger.Info($"Selected write location {dia.SelectedPath}.");
                tbSaveLocation.Text = dia.SelectedPath;
            }
        }

        private void BtnSaveToFile_Click(object sender, EventArgs eventArgs)
        {
            try
            {
                Logger.Info($"Initiating write process for converted save with projected folder {tbSaveLocation.Text}.");

                Logger.Info("Making duplicate of original folder to preserve non-save files.");
                CopyDir.Copy(SelectedSave.SaveFolder, tbSaveLocation.Text);
            }
            catch (Exception exception)
            {
                Logger.Error("Generic exception caught while duplicating save files.");
                Logger.Error(exception);

                MessageBox.Show("Error writing save files to disk! Create an issue at https://github.com/JordanZeotni/BotW-Save-Manager with the error log **IF** you think its **NOT** a permission error.", exception.Message);

                return;
            }

            foreach (KeyValuePair<string, byte[]> convertSaveByte in SaveFilesDictionary)
            {
                Logger.Info($"Generating output path from original file {convertSaveByte.Key}");

                string saveTo;
                try
                {
                    saveTo = Directory.GetFiles(tbSaveLocation.Text, "*.sav", SearchOption.AllDirectories)
                        .First(e => Path.GetFileName(convertSaveByte.Key) == "option.sav" ||
                                    Path.GetFileName(e) == Path.GetFileName(convertSaveByte.Key) &&
                                    Directory.GetParent(e).Name == Directory.GetParent(convertSaveByte.Key).Name);
                }
                catch (InvalidOperationException e)
                {
                    Logger.Error("Unexpected .sav file in the save directory. Full exception:");
                    Logger.Error(e);

                    MessageBox.Show($"Unexpected file \"{convertSaveByte.Key}\" was unable to be converted. " +
                                    $"Try first deleting it from the original save folder, then restarting this application, and finally retrying the conversion. " +
                                    $"If that doesn't work, make an issue with your log at https://github.com/JordanZeotni/BotW-Save-Manager.", e.Message);

                    return;
                }
                catch (Exception e)
                {

                    Logger.Error("Generic exception caught while finding the output for a .sav file.");
                    Logger.Error(e);

                    MessageBox.Show("Error writing save files to disk! Create an issue at https://github.com/JordanZeotni/BotW-Save-Manager with the error log **IF** you think its **NOT** a permission error.", e.Message);

                    return;
                }

                Logger.Info($"Writing save file {saveTo}.");

                try
                {
                    File.WriteAllBytes(saveTo, convertSaveByte.Value);
                }
                catch (Exception e)
                {
                    Logger.Error($"Generic exception caught while writing .sav file {convertSaveByte.Key}.");
                    Logger.Error(e);

                    MessageBox.Show("Error writing a save file to disk! Create an issue at https://github.com/JordanZeotni/BotW-Save-Manager with the error log **IF** you think its **NOT** a permission error.", e.Message);

                    return;
                }
            }

            Logger.Info("Loading converted save as the selected save.");

            try
            {
                SelectedSave = new Save(tbSaveLocation.Text, true);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to load converted save. Alerting user that the save may be corrupt but could be potentially still usable.");
                Logger.Error(e);

                MessageBox.Show("The converted save was unable to be verified. The save may still work on the intended system, " +
                                "but if it doesn't please make an issue at https://github.com/JordanZeotni/BotW-Save-Manager with your log.");

                Logger.Info("Process complete. Application entering standby.\nInkling is the best Smash Ultimate character :)\n" +
                            Encoding.UTF8.GetString(Convert.FromBase64String(Program.SS)));

                return;
            }

            Logger.Info("Process complete. Application entering standby.\nInkling is the best Smash Ultimate character :)\n" +
                        Encoding.UTF8.GetString(Convert.FromBase64String(Program.SS)));

            MessageBox.Show("Files written successfully!");
        }
    }
}
