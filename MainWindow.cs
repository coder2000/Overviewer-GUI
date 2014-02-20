using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using OverviewerGUI.Properties;

namespace OverviewerGUI
{
    public partial class MainWindow : Form
    {
        public delegate void SetProgressBarDelegate();

        public delegate void SetProgressBarPercentDelegate(int per);

        public delegate void SetStatusDelegate(string info);

        private readonly String[] _splashes =
            {
                "Can't track the killers IP!",
                "CLOOOOOOOUD",
                "Uses the minecraft Overviewer!",
                "Coded in C#!",
                "Open source!",
                "Now with title bar splashes!",
                "Splashes are stupid!",
                "MIIIIIINECRAFT",
                "It was trivial!",
                "At the weekend!",
                "Some people want it in python!"
            };

        private string _configFile;
        private Boolean _haltedRender;
        private string _mapDir = Settings.Default.mapDir;
        private string _outDir = Settings.Default.outputDir;
        private Process _proc = new Process();
        private Boolean _windowExpanded;
        private TextWriter _writer;
        private readonly string _processName = IsMono() ? "overviewer.py" : "overviewer.exe";

        public MainWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string info)
        {
            if (statusLabel.InvokeRequired)
            {
                Invoke(new SetStatusDelegate(SetStatus), info);
            }
            else
            {
                statusLabel.Text = info;
            }
        }

        public void SetProgressBarPercent(int per)
        {
            if (renderProgress.InvokeRequired)
            {
                Invoke(new SetProgressBarPercentDelegate(SetProgressBarPercent), per);
            }
            else
            {
                renderProgress.Value = per;
            }
        }


        public void SetProgressBarToContinuous()
        {
            if (renderProgress.InvokeRequired)
            {
                Invoke(new SetProgressBarDelegate(SetProgressBarToContinuous), null);
            }
            else
            {
                renderProgress.Style = ProgressBarStyle.Continuous;
            }
        }

        public void SetProgressBarToMarquee()
        {
            if (renderProgress.InvokeRequired)
            {
                Invoke(new SetProgressBarDelegate(SetProgressBarToMarquee), null);
            }
            else
            {
                renderProgress.Style = ProgressBarStyle.Marquee;
            }
        }

/*
        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        { 
            _proc.Kill();
        }
*/

        private void MainWindow_Load(object sender, EventArgs e)
        {
            // Instantiate the writer
            _writer = new ConsoleRedirect(OVOutput);
            // Redirect the out Console stream
            Console.SetOut(_writer);
            Console.WriteLine(@"Now redirecting output to the text box");
            Text = @"Overviewer GUI - " + GetSplash();

            if (!string.IsNullOrEmpty(_mapDir))
            {
                worldFolder.Text = _mapDir;
            }

            if (string.IsNullOrEmpty(_outDir)) return;

            outputFolder.Text = _outDir;
        }

        private void buttonLevelBrowse_Click(object sender, EventArgs e)
        {
            // Show the dialog and get result.
            var result = LevelDialog.ShowDialog();
            if (result != DialogResult.OK) return;
            worldFolder.Text = LevelDialog.SelectedPath;
            _mapDir = LevelDialog.SelectedPath;
            _mapDir = LevelDialog.SelectedPath;
        }

        private void buttonDirBrowse_Click(object sender, EventArgs e)
        {
            // Show the dialog and get result.
            var result = outputDir.ShowDialog();
            if (result != DialogResult.OK) return;
            outputFolder.Text = outputDir.SelectedPath;
            _outDir = outputDir.SelectedPath;
        }

        private void advancedModeHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(
                @"In advanced mode, instead of specifying a world dir and an output directory, you specify a configuration file for the Overviewer. If using the config file, you do not need to specify world dir or output dir with the GUI - you can specify them in the config file :). More details on th config file are avaliable at docs.overviewer.org",
                @"What is advanced mode?", MessageBoxButtons.OK);
        }

        private void startRender_Click_1(object sender, EventArgs e)
        {
            if (_configFile != null)
            {
                ConfigRender(_configFile);
            }
            else
            {
                SimpleRender(_mapDir, _outDir);
            }
            startRender.Enabled = false;
        }

        private void SimpleRender(string worldDir, string outDir)
        {
            _proc = new Process
                {
                    StartInfo =
                        {
                            FileName = @"cmd",
                            Arguments =
                                "/c " + _processName + " --rendermodes=" + GetRenderModes() + " \"" + worldDir + "\" \"" +
                                outDir +
                                "\" ",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        },
                    EnableRaisingEvents = true
                };
            // set up output redirection
            _proc.StartInfo.CreateNoWindow = true;
            _proc.StartInfo.UseShellExecute = false;
            // see below for output handler
            _proc.ErrorDataReceived += proc_DataReceived;
            _proc.OutputDataReceived += proc_DataReceived;
            _proc.Start();
            _proc.BeginErrorReadLine();
            _proc.BeginOutputReadLine();
            _proc.Exited += ProcessExited;
        }

        private void ConfigRender(String config)
        {
            _proc = new Process
                {
                    StartInfo =
                        {
                            FileName = @"cmd",
                            Arguments = "/c " + _processName + " --config=\"" + config + "\" ",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        },
                    EnableRaisingEvents = true
                };
            // set up output redirection
            _proc.StartInfo.CreateNoWindow = true;
            _proc.StartInfo.UseShellExecute = false;
            // see below for output handler
            _proc.ErrorDataReceived += proc_DataReceived;
            _proc.OutputDataReceived += proc_DataReceived;
            _proc.Start();
            _outDir = "the directory specified in the config";
            _proc.BeginErrorReadLine();
            _proc.BeginOutputReadLine();
            _proc.Exited += ProcessExited;
        }

        private void configButton_Click_1(object sender, EventArgs e)
        {
            var result = configDialog.ShowDialog();
            if (result != DialogResult.OK) return;
            configTextBox.Text = configDialog.FileName;
            _configFile = configDialog.FileName;
        }

        private void proc_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            if (e.Data.Contains("Welcome to Minecraft Overviewer!"))
            {
                SetProgressBarToMarquee();
            }

            if (e.Data.Contains("You won't get percentage progress"))
            {
                _haltedRender = true;
                SetStatus("Last render was interrupted.  You won't get progress for this render.");
            }

            if (!_haltedRender)
            {
                //This is a 'Hack' to work with an inconsistency with overviewer
                var stripTiles = e.Data.Replace(" tiles", "");


                //This could probably be done so much better, but I'm a noob with regular expressions so...
                const string startPattern =
                    "[0-9]+[-][0-9]+[-][0-9]+ [0-9]+[:][0-9]+[:][0-9]+  Rendered [0-9]+ of [0-9]+.";
                var startExpression = new Regex(startPattern);
                const string perPattern = "% complete";
                var perExpression = new Regex(perPattern);

                if (Regex.IsMatch(stripTiles, perPattern, RegexOptions.IgnoreCase))
                {
                    foreach (var per in startExpression.Split(stripTiles).SelectMany(sub => perExpression.Split(sub))
                        )
                    {
                        SetProgressBarToContinuous();
                        if (string.IsNullOrEmpty(per)) continue;
                        SetStatus(per.Trim() + "% complete");
                        SetProgressBarPercent(Convert.ToInt16(per.Trim()));
                    }
                }
            }
            Console.WriteLine(e.Data);
        }

        private void ProcessExited(Object sender, EventArgs e)
        {
            startRender.Enabled = true;
            SetProgressBarToContinuous();
            renderProgress.Value = 100;

            if (OVOutput.Text.ToLower().Contains("error"))
            {
                SetStatus("Render finished with error");
                MessageBox.Show(@"Looks like an error occured! This means the render failed! Better report the error!");
            }
            else
            {
                SetStatus("Render complete!");
                MessageBox.Show(@"The render is complete! Go to " + _outDir + @" and click index.html to view it! :)");
            }
        }

        private String GetRenderModes()
        {
            //WALL OF IF STATEMENTS FTW
            var rendermodes = new List<string>();
            if (normalCheck.Checked)
            {
                rendermodes.Add("normal");
            }
            if (lightingCheck.Checked)
            {
                rendermodes.Add("lighting");
            }
            if (smoothLighingCheck.Checked)
            {
                rendermodes.Add("smooth-lighting");
            }
            if (caveCheck.Checked)
            {
                rendermodes.Add("cave");
            }
            if (nightCheck.Checked)
            {
                rendermodes.Add("night");
            }
            if (smoothNightCheck.Checked)
            {
                rendermodes.Add("smooth-night");
            }


            if (rendermodes.Count == 0)
            {
                Console.WriteLine(@"You need to specify a rendermode! Automatically rendering normal");
                rendermodes.Add("normal");
            }
            else
            {
                Console.WriteLine(@"Ok, I'll be rendering " + string.Join(",", rendermodes.ToArray()));
            }

            return string.Join(",", rendermodes.ToArray());
        }

        private void reportError_Click(object sender, EventArgs e)
        {
            var data = new NameValueCollection();
            var header = "###########################################" + Environment.NewLine +
                            "#                                         #" + Environment.NewLine +
                            "#   This pastebin was generated by the    #" + Environment.NewLine +
                            "#            Overviewer GUI               #" + Environment.NewLine +
                            "#                                         #" + Environment.NewLine +
                            "#   Paste the link for this in the IRC    #" + Environment.NewLine +
                            "#  channel at http://overviewer.org/irc   #" + Environment.NewLine +
                            "#  And we'll help you with the error :)   #" + Environment.NewLine +
                            "#                                         #" + Environment.NewLine +
                            "###########################################" + Environment.NewLine;
            data["api_paste_name"] = "[OV-GUI] Log file upload via the GUI";
            data["api_paste_expire_date"] = "N";
            data["api_paste_code"] = header + OVOutput.Text;
            data["api_dev_key"] = "8aaa33c046fd8faf1d495718d2414165";
            data["api_option"] = "paste";
            var wb = new WebClient();
            var bytes = wb.UploadValues("http://pastebin.com/api/api_post.php", data);
            string response;
            using (var ms = new MemoryStream(bytes))
            using (var reader = new StreamReader(ms))
                response = reader.ReadToEnd();
            if (response.StartsWith("Bad API request"))
            {
                Console.WriteLine(@"Something went wrong. How ironic, the error report returned an error");
                Console.WriteLine(@"Look, just go to http://overviewer.org/irc. We'll help there :)");
            }
            else
            {
                Process.Start(response);
            }
        }

        public String GetSplash()
        {
            var random = new Random();
            var n = random.Next(0, _splashes.Length);
            return _splashes[n];
        }

        private void expandCollapseButton_Click(object sender, EventArgs e)
        {
            if (_windowExpanded)
            {
                if (ActiveForm != null) ActiveForm.Height = 359;
                expandCollapseButton.Text = @"Expand";
            }
            else
            {
                if (ActiveForm != null) ActiveForm.Height = 543;
                expandCollapseButton.Text = @"Collapse";
            }

            _windowExpanded = !_windowExpanded;
        }

        public static bool IsMono()
        {
            return Type.GetType("Mono.Runtime") != null;
        }
    }
}