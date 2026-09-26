using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Danganronpa V3 Thai Mod Installer")]
[assembly: System.Reflection.AssemblyDescription("Standard WinForms installer for the Danganronpa V3 Thai translation mod")]
[assembly: System.Reflection.AssemblyProduct("Danganronpa V3 Thai Mod Installer")]
[assembly: System.Reflection.AssemblyVersion("2.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("2.0.0.0")]

namespace Drv3ThaiModStandard
{
    public sealed class InstallerForm : Form
    {
        readonly TextBox gamePath = new TextBox();
        readonly Button browseButton = new Button();
        readonly Button installButton = new Button();
        readonly Button uninstallButton = new Button();
        readonly Label statusText = new Label();
        readonly Label phaseText = new Label();
        readonly Label percentText = new Label();
        readonly ProgressBar progress = new ProgressBar();
        readonly TextBox log = new TextBox();

        Process worker;

        public InstallerForm()
        {
            Text = "Danganronpa V3 Thai Mod Installer";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            ClientSize = new Size(760, 560);
            MinimumSize = new Size(776, 599);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            BuildUi();
            gamePath.Text = FindDefaultGame();
            gamePath.TextChanged += delegate { RefreshState(); };
            RefreshState();
            FormClosing += OnFormClosing;
        }

        void BuildUi()
        {
            var title = new Label
            {
                Text = "Danganronpa V3 Thai Mod Installer",
                AutoSize = true,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(22, 18)
            };
            Controls.Add(title);

            var subtitle = new Label
            {
                Text = "ติดตั้งหรือถอนการติดตั้งม็อดภาษาไทย",
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Location = new Point(25, 58)
            };
            Controls.Add(subtitle);

            var gameLabel = new Label
            {
                Text = "ตำแหน่งเกม",
                AutoSize = true,
                Location = new Point(24, 98)
            };
            Controls.Add(gameLabel);

            gamePath.Location = new Point(27, 122);
            gamePath.Size = new Size(600, 29);
            Controls.Add(gamePath);

            browseButton.Text = "เลือกโฟลเดอร์...";
            browseButton.Location = new Point(635, 120);
            browseButton.Size = new Size(104, 32);
            browseButton.Click += Browse;
            Controls.Add(browseButton);

            statusText.Location = new Point(27, 164);
            statusText.Size = new Size(712, 28);
            statusText.BorderStyle = BorderStyle.FixedSingle;
            statusText.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(statusText);

            phaseText.Text = "พร้อมทำงาน";
            phaseText.Location = new Point(27, 210);
            phaseText.Size = new Size(570, 26);
            phaseText.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            Controls.Add(phaseText);

            percentText.Text = "0%";
            percentText.Location = new Point(630, 210);
            percentText.Size = new Size(109, 26);
            percentText.TextAlign = ContentAlignment.MiddleRight;
            Controls.Add(percentText);

            progress.Location = new Point(27, 240);
            progress.Size = new Size(712, 24);
            progress.Style = ProgressBarStyle.Continuous;
            Controls.Add(progress);

            log.Location = new Point(27, 282);
            log.Size = new Size(712, 190);
            log.Multiline = true;
            log.ReadOnly = true;
            log.ScrollBars = ScrollBars.Vertical;
            log.Font = new Font("Consolas", 9.5F);
            log.Text = "รายละเอียดการทำงานจะแสดงที่นี่" + Environment.NewLine;
            Controls.Add(log);

            uninstallButton.Text = "ถอนการติดตั้ง";
            uninstallButton.Location = new Point(27, 496);
            uninstallButton.Size = new Size(150, 40);
            uninstallButton.Click += ConfirmUninstall;
            Controls.Add(uninstallButton);

            installButton.Text = "ติดตั้งภาษาไทย";
            installButton.Location = new Point(589, 496);
            installButton.Size = new Size(150, 40);
            installButton.Click += ConfirmInstall;
            Controls.Add(installButton);
        }

        string FindDefaultGame()
        {
            string[] candidates =
            {
                @"C:\Program Files (x86)\Steam\steamapps\common\Danganronpa V3 Killing Harmony",
                @"C:\Program Files\Steam\steamapps\common\Danganronpa V3 Killing Harmony"
            };
            foreach (string path in candidates)
                if (Directory.Exists(Path.Combine(path, "data", "win"))) return path;
            return "";
        }

        string ValidGamePath()
        {
            string path = gamePath.Text.Trim().Trim('"');
            return Directory.Exists(Path.Combine(path, "data", "win")) ? path : null;
        }

        void RefreshState()
        {
            if (worker != null && !worker.HasExited) return;
            string game = ValidGamePath();
            if (game == null)
            {
                SetStatus("ยังไม่พบโฟลเดอร์เกม", Color.DarkRed);
                installButton.Enabled = uninstallButton.Enabled = false;
                return;
            }

            string backup = Path.Combine(game, "thai_mod_original_cpk_backup");
            if (File.Exists(Path.Combine(backup, "thai_mod_install_manifest.json")))
            {
                SetStatus("ติดตั้งม็อดภาษาไทยแล้ว", Color.DarkGreen);
                installButton.Enabled = false;
                uninstallButton.Enabled = true;
                return;
            }

            string win = Path.Combine(game, "data", "win");
            bool clean = File.Exists(Path.Combine(win, "partition_data_win_us.cpk")) &&
                         File.Exists(Path.Combine(win, "partition_resident_win.cpk"));
            if (clean)
            {
                SetStatus("พบเกม พร้อมติดตั้ง", Color.DarkGreen);
                installButton.Enabled = true;
                uninstallButton.Enabled = false;
            }
            else
            {
                SetStatus("ไฟล์เกมไม่ครบหรือมีม็อดแบบ manual อยู่", Color.DarkOrange);
                installButton.Enabled = uninstallButton.Enabled = false;
            }
        }

        void SetStatus(string text, Color color)
        {
            statusText.Text = "  " + text;
            statusText.ForeColor = color;
        }

        void Browse(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "เลือกโฟลเดอร์ Danganronpa V3 Killing Harmony",
                ShowNewFolderButton = false
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    gamePath.Text = dialog.SelectedPath;
            }
        }

        void ConfirmInstall(object sender, EventArgs e)
        {
            if (MessageBox.Show(this,
                    "ปิดเกมแล้วหรือยัง?\r\n\r\nตัวติดตั้งต้องใช้พื้นที่ว่างประมาณ 12 GB และจะสำรอง CPK ต้นฉบับให้โดยอัตโนมัติ",
                    "ยืนยันการติดตั้ง", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                StartWorker(false);
        }

        void ConfirmUninstall(object sender, EventArgs e)
        {
            if (MessageBox.Show(this,
                    "ต้องการถอนม็อดภาษาไทยและคืนไฟล์เกมต้นฉบับใช่ไหม?",
                    "ยืนยันการถอนการติดตั้ง", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                StartWorker(true);
        }

        void StartWorker(bool uninstall)
        {
            string game = ValidGamePath();
            if (game == null) return;

            string backend = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "DRV3ThaiBackend.exe");
            if (!File.Exists(backend))
            {
                MessageBox.Show(this, "ไม่พบไฟล์ bin\\DRV3ThaiBackend.exe", "แพ็กไม่สมบูรณ์",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            log.Clear();
            AppendLog("กรุณาอย่าปิดหน้าต่างระหว่างทำงาน");
            progress.Value = 0;
            percentText.Text = "0%";
            phaseText.Text = uninstall ? "กำลังคืนไฟล์เดิม..." : "กำลังเตรียมติดตั้ง...";
            SetControls(false);

            string arguments = (uninstall ? "--uninstall " : "") +
                "--no-pause --game \"" + game + "\"";
            var info = new ProcessStartInfo(backend, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            };
            info.EnvironmentVariables["PYTHONUTF8"] = "1";
            info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            worker = new Process { StartInfo = info, EnableRaisingEvents = true };
            worker.OutputDataReceived += OnBackendOutput;
            worker.ErrorDataReceived += OnBackendOutput;
            worker.Exited += delegate { BeginInvoke(new Action(FinishWorker)); };
            try
            {
                worker.Start();
                worker.BeginOutputReadLine();
                worker.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                if (worker != null)
                {
                    worker.Dispose();
                    worker = null;
                }
                SetControls(true);
                string message = ex.Message;
                MessageBox.Show(this, message, "เปิดตัวติดตั้งไม่ได้", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void OnBackendOutput(object sender, DataReceivedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(e.Data) || IsDisposed || Disposing) return;
            try
            {
                BeginInvoke((MethodInvoker)delegate { HandleLine(e.Data); });
            }
            catch (InvalidOperationException) { }
        }

        void HandleLine(string line)
        {
            AppendLog(line);
            Match match = Regex.Match(line, @"(\d+)\s*/\s*(\d+)");
            if (match.Success)
            {
                int done = Int32.Parse(match.Groups[1].Value);
                int total = Int32.Parse(match.Groups[2].Value);
                int value = Math.Min(99, done * 100 / Math.Max(1, total));
                progress.Value = value;
                percentText.Text = value + "%";
                phaseText.Text = String.Format("กำลังแตกไฟล์เกม — {0:N0}/{1:N0}", done, total);
            }
            else if (line.Contains("ตรวจสอบ")) phaseText.Text = "กำลังตรวจสอบไฟล์ Steam...";
            else if (line.Contains("ใส่ไฟล์ภาษาไทย"))
            {
                progress.Value = 99;
                percentText.Text = "99%";
                phaseText.Text = "กำลังใส่คำแปลภาษาไทย...";
            }
        }

        void FinishWorker()
        {
            worker.WaitForExit();
            int code = worker.ExitCode;
            worker.Dispose();
            worker = null;
            progress.Value = code == 0 ? 100 : progress.Value;
            percentText.Text = code == 0 ? "100%" : percentText.Text;
            SetControls(true);
            RefreshState();

            if (code == 0)
            {
                phaseText.Text = "ดำเนินการสำเร็จ";
                MessageBox.Show(this, "ดำเนินการเรียบร้อยแล้ว", "สำเร็จ",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                phaseText.Text = "การทำงานไม่สำเร็จ";
                MessageBox.Show(this, "เกิดข้อผิดพลาด กรุณาดูรายละเอียดในช่องสถานะ", "ไม่สำเร็จ",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        void SetControls(bool enabled)
        {
            gamePath.Enabled = browseButton.Enabled = enabled;
            installButton.Enabled = uninstallButton.Enabled = enabled;
            if (enabled) RefreshState();
        }

        void AppendLog(string line)
        {
            log.AppendText(line + Environment.NewLine);
            log.SelectionStart = log.TextLength;
            log.ScrollToCaret();
        }

        void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (worker != null && !worker.HasExited)
            {
                e.Cancel = true;
                MessageBox.Show(this, "กรุณารอให้การทำงานเสร็จก่อน", "กำลังทำงาน",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new InstallerForm());
        }
    }
}
