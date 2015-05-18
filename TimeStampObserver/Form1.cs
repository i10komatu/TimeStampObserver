using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;
using System.Diagnostics;

namespace TimeStampObserver
{
    public partial class Form1 : Form
    {
        private OpenFileDialog ofd1, ofd2, ofd3;
        private FolderBrowserDialog fbd;
        private FileSystemWatcher watcher = null;
        private bool processing = false;
        private string[] commands;

        public Form1()
        {
            InitializeComponent();

            ofd1 = new OpenFileDialog();
            ofd1.InitialDirectory = @"C:\";
            ofd1.Title = "タイムスタンプを監視するファイルを選択してください";
            ofd1.RestoreDirectory = true;

            ofd2 = new OpenFileDialog();
            ofd2.InitialDirectory = @"C:\";
            ofd2.Title = "実行するプログラムを選択してください";
            ofd2.RestoreDirectory = true;
            ofd2.Filter = "exe|*.exe";

            ofd3 = new OpenFileDialog();
            ofd3.InitialDirectory = @"C:\";
            ofd3.Title = "追加するファイルを選択してください";
            ofd3.RestoreDirectory = true;
            
            fbd = new FolderBrowserDialog();
            fbd.SelectedPath = @"C:\";
            fbd.Description = "ワーキングディレクトリを設定してください";
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(textBox1.Text)) {
                fbd.SelectedPath = textBox1.Text;
            }
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = fbd.SelectedPath;
                this.ofd1.InitialDirectory = fbd.SelectedPath;
                this.ofd2.InitialDirectory = fbd.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrWhiteSpace(textBox1.Text))
                {
                    this.textBox2.Text = ofd1.FileName.Replace(textBox1.Text, "").Trim('\\');
                }
                else
                {
                    this.textBox2.Text = ofd1.FileName.Trim('\\');
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (ofd2.ShowDialog() == DialogResult.OK)
            {
                string str;
                string[] args = parse(textBox3.Text);
                if (args.Length != 0)
                {
                    if (ofd2.FileName.IndexOf(' ') == -1)
                    {
                        args[0] = ofd2.FileName.Replace("$", "$$");
                    }
                    else
                    {
                        args[0] = "\"" + ofd2.FileName.Replace("$", "$$") + "\"";
                    }
                    str = args[0] + " " + args[1];
                }
                else
                {
                    if (ofd2.FileName.IndexOf(' ') == -1)
                    {
                        str = ofd2.FileName.Replace("$", "$$");
                    }
                    else
                    {
                        str = "\"" + ofd2.FileName.Replace("$", "$$") + "\"";
                    }
                }
                textBox3.Text = str;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (ofd3.ShowDialog() == DialogResult.OK)
            {
                if (ofd3.FileName.IndexOf(' ') == -1)
                {
                    textBox3.Text = textBox3.Text.Trim() + " " + ofd3.FileName.Replace("$", "$$");
                }
                else
                {
                    textBox3.Text = textBox3.Text.Trim() + " \"" + ofd3.FileName.Replace("$", "$$") + "\"";
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            // 動作中
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;

                this.button5.Text = "実行(&R)";
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.button3.Enabled = true;
                this.button4.Enabled = true;
                this.textBox1.Enabled = true;
                this.textBox2.Enabled = true;
                this.textBox3.Enabled = true;
            }
            // 停止中
            else
            {
                // 引数用意
                string wd = textBox1.Text;
                string filePath;
                string file;
                string cmdln = textBox3.Text;
                cmdln = cmdln.Replace("$F", textBox2.Text);
                cmdln = cmdln.Replace("$$", "$");
                commands = parse(cmdln);

                int tmp = textBox2.Text.LastIndexOf('\\', textBox2.Text.Length - 1);
                file = textBox2.Text.Substring(tmp + 1);
                if (Path.IsPathRooted(textBox2.Text))
                {
                    filePath = textBox2.Text.Substring(0,tmp);
                }
                else
                {
                    if (tmp != -1)
                    {
                        filePath = wd.TrimEnd('\\') + "\\" + textBox2.Text.Substring(0, tmp);
                    }
                    else
                    {
                        filePath = wd;
                    }
                }

                MessageBox.Show(file);
                MessageBox.Show(filePath);

                foreach (string c in commands)
                {
                    MessageBox.Show(c);
                }

                // 引数の確認
                if (!string.IsNullOrWhiteSpace(wd))
                {
                    if (Directory.Exists(wd))
                    {
                        Directory.SetCurrentDirectory(wd);
                    }
                    else
                    {
                        MessageBox.Show("ワーキングディレクトリが正しくありません");
                        return;
                    }
                }
                if (string.IsNullOrWhiteSpace(textBox2.Text) || !File.Exists(textBox2.Text))
                {
                    MessageBox.Show("ファイル名が正しくありません");
                    return;
                }
                if (string.IsNullOrWhiteSpace(filePath) || !Directory.Exists(filePath)) {
                    MessageBox.Show("ファイルの場所を確認してください");
                    return;
                }
                if(commands.Length == 0)
                {
                    MessageBox.Show("実行する処理を設定してください");
                    return;
                }

                // FileSystemWatcherの設定
                watcher = new FileSystemWatcher();
                watcher.Path = filePath;
                watcher.Filter = file;
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess;
                watcher.SynchronizingObject = this;

                watcher.Changed += new FileSystemEventHandler(watcherChanged);
                watcher.Renamed += new RenamedEventHandler(watcherRenamed);

                watcher.EnableRaisingEvents = true;

                this.button5.Text = "停止(&S)";
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.button3.Enabled = false;
                this.button4.Enabled = false;
                this.textBox1.Enabled = false;
                this.textBox2.Enabled = false;
                this.textBox3.Enabled = false;
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            if (Directory.Exists(textBox1.Text))
            {
                this.ofd1.InitialDirectory = textBox1.Text;
                this.ofd2.InitialDirectory = textBox1.Text;
            }
        }

        /// <summary>
        /// 引数をコマンドラインとして構文解析し、プログラム名とコマンドライン引数を区切ります
        /// </summary>
        /// <param name="s">解析する文章</param>
        /// <returns>区切った引数の配列</returns>
        private string[] parse(string s)
        {
            List<string> args = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(s))
            {
                if (s[0] == '"')
                {
                    int t = s.IndexOf('"', 1);
                    if (t == -1)
                    {
                        args.Add(s);
                    }
                    else
                    {
                        args.Add(s.Substring(0, t + 1));
                        args.Add(s.Substring(t + 2));
                    }
                }
                else
                {
                    int t = s.IndexOf(' ');
                    if (t == -1)
                    {
                        args.Add(s);
                    }
                    else
                    {
                        args.Add(s.Substring(0, t));
                        args.Add(s.Substring(t + 1));
                    }
                }
            }

            return args.ToArray();
        }

        private void watcherChanged(object source, FileSystemEventArgs e)
        {
            if (processing) { return; }

            processing = true;
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    Process p = new Process();
                    p.StartInfo.FileName = commands[0];
                    if (commands.Length == 2)
                    {
                        p.StartInfo.Arguments = commands[1];
                    }
                    try
                    {
                        if (!p.Start())
                        {
                            MessageBox.Show("Failed!!");
                        }
                    }
                    catch
                    {
                        this.Visible = true;
                        this.WindowState = FormWindowState.Normal;
                        this.button5.PerformClick();
                        MessageBox.Show("Command Failed!! Observation Aborted.", "Time Stamp Observer");
                    }
                    break;
            }
            processing = false;
        }

        private void watcherRenamed(object source, RenamedEventArgs e)
        {
            MessageBox.Show("Renamed!!");
        }
    }
}
