using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.IO;
using System.Globalization;
using Vlc.DotNet.Core;
using System.Windows.Forms.DataVisualization.Charting;

namespace VideoAnnotationProject
{
    public partial class MainForm : Form
    {
        private string fileInfo;
        private Thread plotUpdater;
        private System.Timers.Timer myTimer;

        private static double KEYBOARD_STEP = 1;
        private static double MAX_VALUE = 1;
        private static double MIN_VALUE = -1;

        private double currentAnnotationValue;
        private double currentVidTime;

        private bool FLAG_VIDEO_IS_PLAYING = false;

        // LOGGING OPTIONS
        private string logFolder;
        private string logFile;

        private Axis yAxis;
        private double currentMinVal;
        private double currentMaxVal;

        public MainForm()
        {
            InitializeComponent();
            this.currentAnnotationValue = 0;
            this.currentVidTime = 0;

            this.KeyPreview = true;
            this.KeyDown += new KeyEventHandler(Keyboard_KeyPress);
            this.FormClosing += new FormClosingEventHandler(exitForm);

            // Create Logging
            logFolder = Application.StartupPath + "\\LogFolder";
            Directory.CreateDirectory(logFolder);

            logFile = null;

            yAxis = plot1.ChartAreas["realtimeChart"].AxisY;
            yAxis.Maximum = 1;
            yAxis.Minimum = -1;

            currentMaxVal = 0;
            currentMinVal = 0;

            vlcPlayer.MediaChanged += changeMedia;
            vlcPlayer.EndReached += mediaStopped;
        }

        private void vlcControl1_Click(object sender, EventArgs e)
        {
            checkIfPlaying();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            startChartThread();
        }

        private void loadVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadVideoFile();
        }

        private void loadVideoFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // Set filter options and filter index.
            openFileDialog.Filter = "Video Files (.avi, .mp4)|*.avi;*.mp4";
            openFileDialog.FilterIndex = 1;

            openFileDialog.Multiselect = false;

            if (vlcPlayer.IsPlaying)
            {
                MessageBox.Show("Please Pause the Video Before Proceeding.",
                    "Error Loading new video",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            else
            {
                // Call the ShowDialog method to show the dialog box.
                DialogResult userClickedOK = openFileDialog.ShowDialog();

                // Process input if the user clicked OK.
                if (userClickedOK == DialogResult.OK)
                {
                    // Open the selected file to read.
                    fileInfo = openFileDialog.FileName;
                    vlcPlayer.SetMedia(new System.IO.FileInfo(fileInfo));
                    MessageBox.Show("Video is now ready to be played.",
                    "Video Loaded Successfully.",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                    // Create a new Log File
                    string logName = openFileDialog.SafeFileName.Split('.')[0];
                    logFile = logFolder + "\\" + logName + "_" + GetTimestamp(System.DateTime.Now) + ".csv";
                }
                else if (userClickedOK == DialogResult.Cancel)
                {
                    // Do Nothing
                }
                else
                {
                    MessageBox.Show("The System was unable to load the selected video.",
                    "Error Loading the Video",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult exit = MessageBox.Show("Are you sure you want to exit?",
                "Exit Application.",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (exit == DialogResult.Yes)
            {
                myTimer.Stop();
                Environment.Exit(0);
            }
            else
            {
                // DO Nothing
            }
        }

        private void exitForm(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult result = MessageBox.Show("Are you sure you want to exit?",
                "Exit Application.",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    myTimer.Stop();
                    Environment.Exit(0);
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void plot1_Click(object sender, EventArgs e)
        {

        }

        private void initializePlotting()
        {
            // Create a timer
            myTimer = new System.Timers.Timer();
            // Tell the timer what to do when it elapses
            myTimer.Elapsed += new ElapsedEventHandler(plotEvent);
            // Set it to go off every x milliseconds (Each 33.3 Seconds = 1 Frame).
            //myTimer.Interval = 33.3;
            myTimer.Interval = 10;
            // And start it        
            myTimer.Enabled = true;
        }

        private void plotEvent(object sender, EventArgs e)
        {
            if (plot1.IsHandleCreated)
            {
                this.Invoke((MethodInvoker)delegate { updateChart(); });
            }
        }

        private void updateChart()
        {
            if (FLAG_VIDEO_IS_PLAYING)
            {
                // Show On Chart
                double yVal = currentAnnotationValue;
                TimeSpan tSpan = TimeSpan.FromMilliseconds(vlcPlayer.Time);
                double xVal = tSpan.TotalMilliseconds;

                if (xVal > currentVidTime)
                {

                    if (yVal != 0)
                    {
                        yAxis.Minimum = Double.NaN;
                        yAxis.Maximum = Double.NaN;
                    }

                    plot1.Series["timeLine"].Points.AddXY(xVal, yVal);

                    currentVidTime = xVal;

                    // LOG IT IN FILE
                    logCurrentPoint(xVal, yVal);
                    
                }

                // Reference Point (This is what the player "controls"
                plot1.Series["refPoint"].Points.Clear();
                plot1.Series["refPoint"].Points.AddXY(xVal, yVal);

            }
            else
            {
                // Show On Chart
                double yVal = currentAnnotationValue;

                if(yVal != 0)
                {
                    yAxis.Minimum = Double.NaN;
                    yAxis.Maximum = Double.NaN;
                }

                // Reference Point (This is what the player "controls"
                plot1.Series["refPoint"].Points.Clear();
                plot1.Series["refPoint"].Points.AddXY(currentVidTime, yVal);

            }
        }

        void Keyboard_KeyPress(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.A)
            {
                //if(currentAnnotationValue < 1)
                //{
                currentAnnotationValue += KEYBOARD_STEP;
                //}
                //else
                //{
                //    currentAnnotationValue = MAX_VALUE;
                //}
            }
            else if (e.KeyCode == Keys.Z)
            {
                //if (currentAnnotationValue > -1)
                //{
                    currentAnnotationValue -= KEYBOARD_STEP;
                //}
                //else
                //{
                //    currentAnnotationValue = MIN_VALUE;
                //}
            }
            else if (e.KeyCode == Keys.Enter)
            {
                checkIfPlaying();
            }
        }

        private void startChartThread()
        {
            if (plotUpdater == null)
            {
                // This is what needs to be done on Start-Up!
                plotUpdater = new Thread(new ThreadStart(this.initializePlotting));
                plotUpdater.IsBackground = true;
                plotUpdater.Start();
            }
        }

        private void checkIfPlaying()
        {
            if (vlcPlayer.GetCurrentMedia() == null)
            {
                MessageBox.Show("Please load a video before play.",
                "Error: No Video Loaded",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
                loadVideoFile();
            }
            else
            {
                if (!vlcPlayer.IsPlaying)
                {
                    FLAG_VIDEO_IS_PLAYING = true;
                    vlcPlayer.Play();
                }
                else
                {
                    FLAG_VIDEO_IS_PLAYING = false;
                    vlcPlayer.Pause();
                }

            }
        }

        private void logCurrentPoint(double timeStamp, double tensionValue)
        {
            if(logFile != null) // Safety Procedure -- Make sure logFile is not null.
            {
                //double tValue = Convert.ToDouble(tensionValue, new CultureInfo("en-US"));
                using (StreamWriter sw = File.AppendText(logFile))
                {
                    sw.WriteLine(timeStamp + "," + Convert.ToString(tensionValue, new CultureInfo("en-US")));
                }
            }
        }

        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssffff");
        }

        public void resetChart()
        {
            myTimer.Stop();

            plot1.Series["timeLine"].Points.Clear();
            plot1.Series["refPoint"].Points.Clear();
            plot1.Series["refPoint"].Points.AddXY(1,0);

            currentAnnotationValue = 0;
            currentVidTime = 0;

            myTimer.Start();
        }

        private void changeMedia(object sender, VlcMediaPlayerMediaChangedEventArgs e)
        {
            resetChart();
        }

        private void mediaStopped(object sender, VlcMediaPlayerEndReachedEventArgs e)
        {
            DialogResult result = MessageBox.Show("Please exit the video annotation tool and return to the game.",
                "Video Annotation Complete.",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            if(result == DialogResult.OK)
            {
                Environment.Exit(0);
            }
        }
    }
}
