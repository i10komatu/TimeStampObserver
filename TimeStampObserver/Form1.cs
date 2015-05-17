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

namespace TimeStampObserver
{
    public partial class Form1 : Form
    {
        OpenFileDialog ofd1, ofd2, ofd3;
        FolderBrowserDialog fbd;
        bool working;

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

            working = false;
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
                this.textBox2.Text = ofd1.FileName;
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
            string[] s = parse(textBox3.Text);
            for (int i = 0; i < s.Length; i++)
            {
                MessageBox.Show(s[i]);
            }
            return;
            if (working)
            {
                this.button5.Text = "実行(&R)";
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.button3.Enabled = true;
                this.textBox1.Enabled = true;
                this.textBox2.Enabled = true;
                this.textBox3.Enabled = true;
                working = false;
            }
            else
            {
                string file = textBox1.Text;
                string cmdln = textBox2.Text;
                cmdln = cmdln.Replace("$F", file);
                cmdln = cmdln.Replace("$$", "$");
                string[] commands = new List<string>(cmdln.Split(' ')).ToArray();

                MessageBox.Show(file);
                foreach (string c in commands)
                {
                    MessageBox.Show(c);
                }

                this.button5.Text = "停止(&S)";
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.button3.Enabled = false;
                this.textBox1.Enabled = false;
                this.textBox2.Enabled = false;
                this.textBox3.Enabled = false;
                working = true;
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
    }
}
