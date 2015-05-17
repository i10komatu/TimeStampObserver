using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TimeStampObserver
{
    public partial class Form1 : Form
    {
        OpenFileDialog ofd1, ofd2;
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
            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = ofd1.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (ofd2.ShowDialog() == DialogResult.OK)
            {
                this.textBox2.Text = ofd2.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (working)
            {
                this.button3.Text = "実行(&R)";
                this.textBox1.Enabled = true;
                this.textBox2.Enabled = true;
                working = false;
            }
            else
            {
                string file = textBox1.Text;
                string cmdln = textBox2.Text;
                cmdln = cmdln.Replace("$F",file);
                cmdln = cmdln.Replace("$$", "$");
                string[] commands = new List<string>(cmdln.Split(' ')).ToArray();

                MessageBox.Show(file);
                foreach (string c in commands)
                {
                    MessageBox.Show(c);
                }

                this.button3.Text = "停止(&S)";
                this.textBox1.Enabled = false;
                this.textBox2.Enabled = false;
                working = true;
            }
        }
    }
}
