using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace SBT
{

    public partial class Form1 : Form
    {

        private static string resourcePath = Environment.CurrentDirectory + @"\";
        private static string logPath = Environment.CurrentDirectory + @"\";
        private static string path = System.Environment.GetEnvironmentVariable("USERPROFILE") + @"\AppData\LocalLow\Amistech\My Summer Car";
        private string dir = path;
        private bool hasMscExited;
        private bool isBackupCreated;
        private string timeNow = DateTime.Now.ToString("dd/MM/yyyy | HH:mm:ss    ");
        private string logFilename = "log.txt";
        private string pathFilename = @"path_settings.txt";
        private ContextMenu tray_context;
        private static readonly string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private static readonly string StartupValue = "SavegameBackupTool";
        private RegistryKey startup_regkey;
        private ToolStripMenuItem startup_item;

        public Form1()
        {
            Run1();
            tray_context = new ContextMenu();
            tray_context.MenuItems.Add(0, new MenuItem("Show", new System.EventHandler(Show_Click)));
            tray_context.MenuItems.Add(1, new MenuItem("Hide", new System.EventHandler(Hide_Click)));
            tray_context.MenuItems.Add(2, new MenuItem("Exit", new System.EventHandler(Exit_Click)));
            notifyIcon1.ContextMenu = tray_context;
            contextMenuStrip1.ShowCheckMargin = true;
            startup_item = ((ToolStripMenuItem)contextMenuStrip1.Items[0]);
            if (CheckRegKey() == true)
            {
                startup_item.Checked = true;
            }
            else
            {
                startup_item.Checked = false;
            }
            try
            {
                if (!File.Exists(logPath + @"log.txt"))
                {
                    File.Create(logPath + @"log.txt").Close();
                    //File.AppendAllText(logPath + logFilename, timeNow + "Success!!! Log file created!" + Environment.NewLine);
                }
                else
                {
                    File.WriteAllText(logPath + @"log.txt", String.Empty);
                }
                try
                {
                    if (!File.Exists(resourcePath + pathFilename))
                    {
                        richTextBox1.Text = path;
                        File.AppendAllText(logPath + logFilename, timeNow + "Reading File From variable" + Environment.NewLine);
                    }
                    else
                    {
                        richTextBox1.Text = File.ReadLines(resourcePath + pathFilename).First().ToString();
                        File.AppendAllText(logPath + logFilename, timeNow + "Reading File From path_settings.txt" + Environment.NewLine);
                    }
                    checkProcessTimer.Enabled = true;
                    checkProcessTimer.Start();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Couldn't Find settings.txt File", "Error!" + " " + e.Message, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    File.AppendAllText(logPath + logFilename, timeNow + "Couldn't Find settings.txt File" + e.Message + Environment.NewLine);
                }
            }
            catch
            {

            }
        }

        private void Run1()
        {
            if (!Program.IsAdministrator())
            {
                // Restart and run as admin
                var exeName = Application.ExecutablePath;
                ProcessStartInfo startInfo = new ProcessStartInfo(exeName);
                startInfo.Verb = "runas";
                startInfo.Arguments = "restart";
                Process.Start(startInfo);
                Application.Exit();
                InitializeComponent();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                folderBrowserDialog1.SelectedPath = richTextBox1.Text;
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    richTextBox1.Text = folderBrowserDialog1.SelectedPath;
                    path = richTextBox1.Text.ToString();
                    dir = path;
                    File.WriteAllText(resourcePath + pathFilename, richTextBox1.Text);
                }
            }
            catch (Exception a)
            {
                MessageBox.Show("Error while browsing for files:" + "\n" + a.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.AppendAllText(logPath + logFilename, timeNow + "Error while browsing for files:" + a.Message + Environment.NewLine);
            }
        }

        private void ThreadStart()
        {
            try
            {
                Thread th = new Thread(() =>
                {
                    CheckForMSCProcess();
                });
                th.IsBackground = true;
                th.Priority = ThreadPriority.Lowest;
                th.Start();
            }
            catch (Exception b)
            {
                MessageBox.Show("Error while starting a thread:" + "\n" + b.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.AppendAllText(logPath + logFilename, timeNow + "Error while starting a thread:" + b.Message + Environment.NewLine);
            }
        }

        private void CheckForMSCProcess()
        {

            try
            {
                Process[] AllProcesses = Process.GetProcessesByName("mysummercar");

                if (AllProcesses.Length <= 0)
                {
                    hasMscExited = true;
                    this.InvokeEx(f => f.label2.Text = "My Summer Not Car Running!");
                    this.InvokeEx(f => f.label2.ForeColor = Color.Red);
                    if (hasMscExited && !isBackupCreated)
                    {
                        Copy(path, dir);
                        this.InvokeEx(f => f.label2.Text = "Backup Save Created!");
                        this.InvokeEx(f => f.label2.ForeColor = Color.Blue);
                        isBackupCreated = true;
                    }

                }
                else
                {
                    hasMscExited = false;
                    isBackupCreated = false;
                    this.InvokeEx(f => f.label2.Text = "My Summer Car Running!");
                    this.InvokeEx(f => f.label2.ForeColor = Color.Green);
                }
            }
            catch (Exception c)
            {
                MessageBox.Show("Error while cechking for msc process:" + "\n" + c.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.AppendAllText(logPath + logFilename, timeNow + "Error while cechking for msc process:" + c.Message + Environment.NewLine);
            }
        }

        private void checkProcessTimer_Tick(object sender, EventArgs e)
        {
            ThreadStart();
        }

        public void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            try
            {
                Directory.CreateDirectory(target.FullName + ".bak");

                // Copy each file into the new directory.
                foreach (FileInfo fi in source.GetFiles())
                {
                    //Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                    fi.CopyTo(Path.Combine(target.FullName + ".bak", fi.Name), true);
                }

                // Copy each subdirectory using recursion.
                foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
                {
                    DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyAll(diSourceSubDir, nextTargetSubDir);
                }
            }
            catch (Exception d)
            {
                MessageBox.Show("Error while creating backup directory:" + "\n" + d.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.AppendAllText(logPath + logFilename, timeNow + "Error while creating backup directory:" + d.Message + Environment.NewLine);
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.Visible = true;
                this.ShowInTaskbar = false;
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = true;
                this.ShowInTaskbar = true;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            notifyIcon1.Visible = true;
            WindowState = FormWindowState.Normal;
        }

        protected void Exit_Click(Object sender, System.EventArgs e)
        {
            Close();
        }
        protected void Hide_Click(Object sender, System.EventArgs e)
        {
            Hide();
        }
        protected void Show_Click(Object sender, System.EventArgs e)
        {
            this.Show();
            WindowState = FormWindowState.Normal;
        }

        private void label1_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show(label1, 0, label1.Height);
        }
        private void SetStartup()
        {
            try
            {
                startup_regkey = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                startup_regkey.SetValue(StartupValue, Application.ExecutablePath.ToString());
                File.AppendAllText(logPath + logFilename, timeNow + "Application set to run on startup." + Environment.NewLine);
            }
            catch (Exception f)
            {
                MessageBox.Show("Error while adding startup registry key:" + "\n" + f.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.AppendAllText(logPath + logFilename, timeNow + "Error while adding startup registry key:" + f.Message + Environment.NewLine);
            }
        }

        private void RemoveStartup()
        {
            try
            {
                RegistryKey winLogonKey = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                winLogonKey.DeleteValue(StartupValue);
                File.AppendAllText(logPath + logFilename, timeNow + "Application removed from startup." + Environment.NewLine);
            }
            catch (Exception g)
            {
                MessageBox.Show("Error while removing startup registry key:" + "\n" + g.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.AppendAllText(logPath + logFilename, timeNow + "Error while removing startup registry key:" + g.Message + Environment.NewLine);
            }
        }
        public bool CheckRegKey()
        {
            try
            {
                RegistryKey winLogonKey = Registry.CurrentUser.OpenSubKey(StartupKey, true);
                return (winLogonKey.GetValueNames().Contains(StartupValue));
            }
            catch (Exception h)
            {
                MessageBox.Show("Error while checking startup registry key:" + "\n" + h.Message, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                File.AppendAllText(logPath + logFilename, timeNow + "Error while checking startup registry key:" + h.Message + Environment.NewLine);
                throw new Exception(h.Message);
            }
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            startup_item.CheckOnClick = true;
            if (startup_item.CheckState == CheckState.Unchecked)
            {
                SetStartup();
            }
            else
            {
                RemoveStartup();
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //File.Delete(logPath + @"log.txt");
            Application.Exit();
        }

    }
    public static class ISynchronizeInvokeExtensions
    {
        public static void InvokeEx<T>(this T @this, Action<T> action) where T : ISynchronizeInvoke
        {
            if (@this.InvokeRequired)
            {
                @this.Invoke(action, new object[] { @this });
            }
            else
            {
                action(@this);
            }
        }
    }
}
