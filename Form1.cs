﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace OverviewerGUI
{
    public partial class Form1 : Form
    {
        private string worldDir;
        private string outDir;
        private string configFile;
        TextWriter _writer = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Instantiate the writer
            _writer = new ConsoleRedirect(OVOutput);
            // Redirect the out Console stream
            Console.SetOut(_writer);

            Console.WriteLine("Now redirecting output to the text box");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Show the dialog and get result.
            DialogResult result = levelDialog.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                textBox1.Text = levelDialog.FileName;
                worldDir = levelDialog.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Show the dialog and get result.
            DialogResult result = outputDir.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                textBox2.Text = outputDir.SelectedPath;
                outDir = outputDir.SelectedPath;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("In advanced mode, instead of specifying a world dir and an output directory, you specify a configuration file for the Overviewer. If using the config file, you do not need to specify world dir or output dir with the GUI - you can specify them in the config file :). More details on th config file are avaliable at docs.overviewer.org", "What is advanced mode?", MessageBoxButtons.OK);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (configFile != null)
            {
                configRender(configFile);
            }
            else
            {
                simpleRender(worldDir, outDir);
            }
        }

        private void simpleRender(string worldDir, string outDir)
        {
            Process proc = new Process();

            proc.StartInfo.FileName = @"cmd";
            proc.StartInfo.Arguments = "/c overviewer.exe \"" + worldDir + "\" \"" + outDir + "\" ";
            // set up output redirection
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.EnableRaisingEvents = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            // see below for output handler
            proc.ErrorDataReceived += proc_DataReceived;
            proc.OutputDataReceived += proc_DataReceived;
            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            proc.WaitForExit();

        }

        private void configRender(String config)
        {
            Process proc = new Process();

            proc.StartInfo.FileName = @"cmd";
            proc.StartInfo.Arguments = "/c overviewer.exe --config=\"" + config + "\" ";
            // set up output redirection
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.EnableRaisingEvents = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            // see below for output handler
            proc.ErrorDataReceived += proc_DataReceived;
            proc.OutputDataReceived += proc_DataReceived;
            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();

            //proc.WaitForExit();

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            DialogResult result = configDialog.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                textBox4.Text = configDialog.FileName;
                configFile = configDialog.FileName;
            }
        }

        void proc_DataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

    }
}