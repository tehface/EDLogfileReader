﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EDLogfileReader
{
    public partial class EDLogFileReader : Form
    {
        //private EDLogFileWatcher watcher = null;
        private EDLogFilePoll watcher = null;
        delegate void SetTextCallback(String text);
        CultureInfo culture = CultureInfo.CreateSpecificCulture("en-GB");
        private Position lastPosition;
        private Position solPosition = new Position(0, 0, 0);
        private SettingsForm settingsForm = null;
        private About aboutForm = null;
        public double distanceTraveled = 0;
        public String edLogPath = null;
        private String outputFileName = null;
        public bool showSolDistance = false;
        public bool showDistanceTraveled = true;


        public EDLogFileReader()
        {
            string distanceAsString = getAppSetting("DistanceTraveled");
            string showSolDistanceAsString = getAppSetting("ShowSolDistance");
            this.distanceTraveled = distanceAsString == null ? 0 : double.Parse(distanceAsString);

            this.edLogPath = getAppSetting("EDLogsPath");
            this.outputFileName = getAppSetting("OutputFileName");
            this.showSolDistance = String.IsNullOrEmpty(showSolDistanceAsString) ? false : bool.Parse(showSolDistanceAsString);

            //this.edLogPath = "C:\\Users\\<home>\\AppData\\Local\\Frontier_Developments\\Products\\elite-dangerous-64\\Logs";
            watcher = new EDLogFilePoll(edLogPath, new EDLogFilePoll.SystemChangeNotificationDelegate(systemChangeNotification));

            InitializeComponent();

            this.StartButton.Click += onStartButtonClick;
            this.StopButton.Click += onStopButtonClick;
            this.QuitButton.Click += onQuitButtonClick;

            start();
        }


        public double getDistanceTraveled()
        {
            return this.distanceTraveled;
        }



        public void setDistanceTraveled(double newValue)
        {
            this.distanceTraveled = newValue;
            setAppSetting("DistanceTraveled", newValue.ToString());
            saveAppSettings();
        }


        public String getLogsPath()
        {
            return this.edLogPath;
        }


        public void setLogsPath(string newPath)
        {
            this.edLogPath = newPath;
            setAppSetting("EDLogsPath", newPath);
            watcher.setLogFilePath(newPath);
            saveAppSettings();
        }


        public String getOutputFileName()
        {
            return this.outputFileName;
        }


        public void setOutputFileName(String newName)
        {
            this.outputFileName = newName;
            setAppSetting("OutputFileName", newName);
            saveAppSettings();
        }


        public void onStartButtonClick(object sender, EventArgs e)
        {
            start();
        }


        public void onStopButtonClick(object sender, EventArgs e)
        {
            stop();
        }


        public void onQuitButtonClick(object sender, EventArgs e)
        {
            quit();
        }


        private void quit()
        {
            stop();
            Application.Exit();
        }


        private void start()
        {
            if (String.IsNullOrEmpty(this.edLogPath))
            {
                appendLogBoxText("You'll need to set your E:D log directory in \"Settings\" first." + Environment.NewLine);
                this.StartButton.Enabled = true;
                this.StopButton.Enabled = false;
            }
            else
            {
                appendLogBoxText("Starting to watch logfiles." + Environment.NewLine);
                if (watcher.startWatching())
                {
                    this.StartButton.Enabled = false;
                    this.StopButton.Enabled = true;
                }
                else
                {
                    this.StartButton.Enabled = true;
                    this.StopButton.Enabled = false;
                }
            }

        }


        private void stop()
        {
            appendLogBoxText("Stopping logfile watching." + Environment.NewLine);
            watcher.stopWatching();
            this.StartButton.Enabled = true;
            this.StopButton.Enabled = false;
        }


        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((this.settingsForm == null) || (this.settingsForm.Visible == false))
            {
                this.settingsForm = new SettingsForm(this);
                this.settingsForm.Show();
            }
            else if (this.settingsForm.Visible == true)
            {
                this.settingsForm.Focus();
            }
        }


        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if ((this.aboutForm == null) || (this.aboutForm.Visible == false))
            {
                this.aboutForm = new About();
                this.aboutForm.Show();
            }
            else if (this.aboutForm.Visible == true)
            {
                this.aboutForm.Focus();
            }
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            quit();
        }



        private String getTimestampString()
        {
            return "[" + DateTime.Now.ToString("hh:mm:ss") + "] ";
        }


        public void systemChangeNotification(String newSystemName, Position newPosition)
        {
            double diff = -1;
            if (this.lastPosition != null)
            {
                diff = this.lastPosition.distanceTo(newPosition);
                this.distanceTraveled += diff;
                setAppSetting("DistanceTraveled", this.distanceTraveled.ToString());
            }

            double d = 0;

            if (this.showDistanceTraveled)
            {
                d = Math.Round(newPosition.distanceTo(solPosition), 2, MidpointRounding.AwayFromZero);
            }

            if (!(String.IsNullOrEmpty(this.outputFileName)))
            {
                String spacer = (newPosition.X < 0) ? " " : "";
                string output = spacer + newSystemName + "\n" + newPosition.toString() + "\n";
                if (this.showSolDistance)
                {
                    output += spacer + d.ToString(culture) + " light years from Sol\n";
                }

                if (this.showDistanceTraveled)
                {
                    output += spacer + Math.Round(distanceTraveled, 2, MidpointRounding.AwayFromZero).ToString(culture) + " ly traveled so far.";
                }

                System.IO.File.WriteAllText(this.outputFileName, output);
            }

            String boxOutput = newSystemName + ", (" + newPosition.toString() + ")";
            if (this.showSolDistance) { boxOutput += ", " + d.ToString(culture) + " LY from Sol"; }
            if (this.showDistanceTraveled)
            {
                boxOutput += ", " + Math.Round(distanceTraveled, 2, MidpointRounding.AwayFromZero).ToString(culture) + " traveled";
            }
            boxOutput += "." + Environment.NewLine;
            this.appendLogBoxText(boxOutput);

            this.lastPosition = newPosition;
        }


        //private void ThreadProcText()
        //{
        //    this.appendLogBoxText(text);
        //}


        private void appendLogBoxText(String text)
        {
            if (this.LogTextbox.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(appendLogBoxText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.LogTextbox.AppendText(getTimestampString() + text);
            }
        }


        public string getAppSetting(string key)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(
                                    System.Reflection.Assembly.GetExecutingAssembly().Location);
            KeyValueConfigurationElement e = config.AppSettings.Settings[key];
            return e != null ? e.Value : null;
        }


        public void setAppSetting(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(
                                    System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (config.AppSettings.Settings[key] != null)
            {
                config.AppSettings.Settings.Remove(key);
            }
            config.AppSettings.Settings.Add(key, value);
            config.Save(ConfigurationSaveMode.Modified);
        }


        public void saveAppSettings()
        {
            ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location).Save(ConfigurationSaveMode.Modified);
        }


        public void setShowSolDistance(Boolean newValue)
        {
            this.showSolDistance = newValue;
            setAppSetting("ShowSolDistance", this.showSolDistance.ToString());
            saveAppSettings();
        }
    }
}
