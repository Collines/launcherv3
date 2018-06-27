﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace Launcherv3
{
    public partial class Form1 : Form
    {
        bool drag;
        int x, y;

        // menu bar links
        string link_website = "http://archewow.com";
        string link_register = "http://archewow.com";
        string link_forum = "http://archewow.com";
        string link_donate = "http://archewow.com";
        string link_discord = "http://archewow.com";

        // the webhost launcher folder location example "http://yoursite.com/FOLDER_NAME";
        string launcherv3_location = "http://80.235.132.198/launcherv3";

        private WebClient webClient;
        Stopwatch sw = new Stopwatch();    // The stopwatch which we will be using to calculate the download speed
        string realmlist = string.Empty;
        int realmPort = 0;

        public Form1()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.DimGray;
            this.TransparencyKey = Color.DimGray;

            InitializeComponent();
        }

        // Move & drag the form
        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                drag = true;
                x = e.X;
                y = e.Y;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            textBox1.Text = Properties.Settings.Default.custom_wow_username;

            pb1.Image = Properties.Resources.launcherv3_03;
            pb2.Image = Properties.Resources.launcherv3_04;

            pictureBox1.Enabled = false;
            pictureBox1.Image = Properties.Resources.launcherv3_play_c;

            timer1.Interval = 2500; // specify interval time as you want
            timer1.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            PlayerHasAllRequiredFiles();

            pictureBox1.Enabled = true;
            pictureBox1.Image = Properties.Resources.launcherv3_play_a;

            timer1.Stop();
        }

        public void RetrieveServerStatusAndRealmlistinfo()
        {
            string reply = string.Empty;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(launcherv3_location + "/launcherv3.xml");

            string versionX = xmlDoc.SelectSingleNode("launcher").Attributes["patchVersion"].Value;

            realmlist = xmlDoc.SelectSingleNode("launcher").Attributes["realmlist"].Value;
            realmPort = int.Parse(xmlDoc.SelectSingleNode("launcher").Attributes["realmPort"].Value);

            label2.Text = string.Format("Realmlist: {0}", realmlist);
            label2.ForeColor = Color.Lime;

            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect(realmlist, realmPort);
                label4.Text = "Realm is Online";
                label4.ForeColor = Color.Lime;
            }
            catch (Exception)
            {
                label4.Text = "Realm is offline";
                label4.ForeColor = Color.Red;
            }
            finally
            {
                tcpClient.Close();
            }

            xmlDoc = null;
        }

        private bool PlayerHasAllRequiredFiles()
        {
            RetrieveServerStatusAndRealmlistinfo();

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(launcherv3_location + "/launcherv3.xml");
            try
            {

                foreach (XmlNode nodes in xmlDoc.SelectNodes("//file"))
                {
                    foreach (XmlAttribute attribute in nodes.Attributes)
                    {
                        if (attribute.Name == "folderTarget")
                        {
                            if (Directory.Exists(attribute.Value))
                            {
                                if (File.Exists(attribute.Value/*target folder name*/ + "\\" + nodes.InnerText/*filename*/))
                                {
                                    System.Net.WebRequest req = System.Net.HttpWebRequest.Create(launcherv3_location + "/files/" + nodes.InnerText);

                                    req.Method = "HEAD";

                                    using (System.Net.WebResponse resp = req.GetResponse())
                                    {
                                        int ContentLength;

                                        if (int.TryParse(resp.Headers.Get("Content-Length"), out ContentLength))
                                        {
                                            FileInfo f1 = new FileInfo(attribute.Value/*target folder name*/ + "\\" + nodes.InnerText/*filename*/);

                                            if (ContentLength != f1.Length)
                                            {
                                                DownloadFile(launcherv3_location  + "/files/" + nodes.InnerText, attribute.Value + "\\" + nodes.InnerText);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    DownloadFile(launcherv3_location + "/files / " + nodes.InnerText, attribute.Value + "\\" + nodes.InnerText);
                                }
                            }
                            else
                                MessageBox.Show("There is an error selecting the destination folder for the downloaded file!");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(string.Format("Please Report the following error to our server:\n \n {0}", ex.ToString()));
                return false;
            }
            finally
            {
                xmlDoc = null;
            }

            return true;
        }

        public void DownloadFile(string urlAddress, string location)
        {
            using (webClient = new WebClient())
            {
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);

                // The variable that will be holding the url address (making sure it starts with http://)
                Uri URL = urlAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? new Uri(urlAddress) : new Uri("http://" + urlAddress);

                // Start the stopwatch which we will be using to calculate the download speed
                sw.Start();

                try
                {
                    // Start downloading the file
                    webClient.DownloadFileAsync(URL, location);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        // The event that will fire whenever the progress of the WebClient is changed
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // Calculate download speed and output it to labelSpeed.
            labelSpeed.Text = string.Format("{0} kb/s", (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"));

            // Update the progressbar percentage only when the value is not the same.
            newProgressBar1.Value = e.ProgressPercentage;

            // Show the percentage on our label.
            labelPerc.Text = e.ProgressPercentage.ToString() + "%";

            // Update the label with how much data have been downloaded so far and the total size of the file we are currently downloading
            labelDownloaded.Text = string.Format("{0} MB's / {1} MB's",
                (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
                (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));

            pictureBox1.Enabled = false;
            pictureBox1.Image = Properties.Resources.launcherv3_play_c;
        }

        // The event that will trigger when the WebClient is completed
        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            // Reset the stopwatch.
            sw.Reset();

            if (e.Cancelled == true)
            {
                labelDownloaded.Text = "Download has been canceled.";
            }
            else
            {
                labelDownloaded.Text = "Download completed.";

                pictureBox1.Enabled = true;
                pictureBox1.Image = Properties.Resources.launcherv3_play_a;
            }
        }


        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            drag = false;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
            {
                this.Location = new Point(e.X + this.Left - x, e.Y + this.Top - y);
            }
        }

        private void pb1_MouseEnter(object sender, EventArgs e)
        {
            pb1.Image = Properties.Resources.launcherv3_03b;
        }

        private void pb1_MouseLeave(object sender, EventArgs e)
        {
            pb1.Image = Properties.Resources.launcherv3_03;
        }

        private void pb1_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void pb2_MouseEnter(object sender, EventArgs e)
        {
            pb2.Image = Properties.Resources.launcherv3_04b;
        }

        private void pb2_MouseLeave(object sender, EventArgs e)
        {
            pb2.Image = Properties.Resources.launcherv3_04;
        }

        private void pb2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void panel2_MouseEnter(object sender, EventArgs e)
        {
            panel2.BackgroundImage = Properties.Resources.target_ssmall;
            panel2.BackgroundImageLayout = ImageLayout.Zoom;
        }

        private void panel2_MouseLeave(object sender, EventArgs e)
        {
            panel2.BackgroundImage = null;
        }

        private void panel3_MouseEnter(object sender, EventArgs e)
        {
            panel3.BackgroundImage = Properties.Resources.target_ssmall;
            panel3.BackgroundImageLayout = ImageLayout.Zoom;
        }

        private void panel3_MouseLeave(object sender, EventArgs e)
        {
            panel3.BackgroundImage = null;
        }

        private void panel4_MouseEnter(object sender, EventArgs e)
        {
            panel4.BackgroundImage = Properties.Resources.target_ssmall;
            panel4.BackgroundImageLayout = ImageLayout.Zoom;
        }

        private void panel4_MouseLeave(object sender, EventArgs e)
        {
            panel4.BackgroundImage = null;
        }

        private void panel5_MouseEnter(object sender, EventArgs e)
        {
            panel5.BackgroundImage = Properties.Resources.target_ssmall;
            panel5.BackgroundImageLayout = ImageLayout.Zoom;
        }

        private void panel5_MouseLeave(object sender, EventArgs e)
        {
            panel5.BackgroundImage = null;
        }

        private void panel6_MouseEnter(object sender, EventArgs e)
        {
            panel6.BackgroundImage = Properties.Resources.target_ssmall;
            panel6.BackgroundImageLayout = ImageLayout.Zoom;
        }

        private void panel6_MouseLeave(object sender, EventArgs e)
        {
            panel6.BackgroundImage = null;
        }

        private void panel2_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(link_website);
        }

        private void panel3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(link_register);
        }

        private void panel4_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(link_forum);
        }

        private void panel5_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(link_donate);
        }

        private void panel6_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(link_discord);
        }

        private void pictureBox1_MouseEnter(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.launcherv3_play_b;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            using (var outputFile = new StreamWriter(@"WTF\Config.WTF", true))
                outputFile.WriteLine("set realmlist " + realmlist);

            //SET accountName
            if (checkBox2.Checked)
            {
                using (var outputFile = new StreamWriter(@"WTF\Config.WTF", true))
                    outputFile.WriteLine("set accountName \"" + textBox1.Text + "\"");
            }
            else
            {
                using (var outputFile = new StreamWriter(@"WTF\Config.WTF", true))
                    outputFile.WriteLine("set accountName \"\"");
            }

            Properties.Settings.Default.custom_wow_username = textBox1.Text;
            Properties.Settings.Default.Save();

            if (checkBox1.Checked)
            {
                if (Directory.Exists("Cache"))
                {
                    var dir = new DirectoryInfo("Cache");
                    dir.Delete(true); // true => recursive delete
                }
            }

            try
            {
                if (!File.Exists("Wow.exe"))
                {
                    MessageBox.Show("The WoW.exe file could not be located!", "Something went wrong!");
                    return;
                }
                else
                {
                    string programPath = Directory.GetCurrentDirectory() + "\\Wow.exe";
                    Process proc = new Process();
                    proc.StartInfo.FileName = programPath;
                    proc.Start();
                    this.WindowState = FormWindowState.Minimized;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.custom_wow_username = textBox1.Text;
            Properties.Settings.Default.Save();
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            pictureBox1.Image = Properties.Resources.launcherv3_play_a;
        }
    }
}
