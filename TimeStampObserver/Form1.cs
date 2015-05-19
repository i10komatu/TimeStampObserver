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
        #region フィールド
        /// <summary>
        /// 監視ファイル選択用ダイアログ
        /// </summary>
        private OpenFileDialog ofd1;

        /// <summary>
        /// 実行ファイル選択用ダイアログ
        /// </summary>
        private OpenFileDialog ofd2;

        /// <summary>
        /// コマンドライン引数追加用ダイアログ
        /// </summary>
        private OpenFileDialog ofd3;

        /// <summary>
        /// ディレクトリ選択ダイアログ
        /// </summary>
        private FolderBrowserDialog fbd;

        /// <summary>
        /// ファイルの変更を監視する
        /// </summary>
        private FileSystemWatcher watcher = null;

        /// <summary>
        /// イベントが実行中かどうか
        /// </summary>
        private bool processing = false;

        /// <summary>
        /// 長さが1または2の文字列配列
        /// 要素0には実行するプログラムを、要素1にはコマンドライン引数が入っている
        /// </summary>
        private string[] commands;

        /// <summary>
        /// 起動直後に監視を開始するかどうか
        /// </summary>
        private bool run;
        #endregion フィールド

        #region コンストラクタ
        /// <summary>
        /// 通常のコンストラクタ。フィールドの初期化のみを行う。
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            // ダイアログの初期化
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

        /// <summary>
        /// フィールドの初期化に加え、渡された環境変数を用いて初期化を行う。
        /// </summary>
        /// <param name="args">環境変数</param>
        public Form1(Dictionary<string, string> args)
            : this()
        {
            // ワーキングディレクトリの追加
            if (args.ContainsKey("wd"))
            {
                textBox1.Text = args["wd"];
                if (Directory.Exists(args["wd"]))
                {
                    Directory.SetCurrentDirectory(args["wd"]);
                }
            }

            // 監視ファイルの追加
            if (args.ContainsKey("file"))
            {
                // 絶対パス指定じゃなかった場合は、カレントディレクトリを設定
                if (Path.IsPathRooted(args["file"]))
                {
                    textBox1.Text = Environment.CurrentDirectory;
                }

                // 相対パス指定にできるか検証、ファイルが存在するかどうかは検証しない
                if (string.IsNullOrWhiteSpace(textBox1.Text))
                {
                    textBox2.Text = args["file"];
                }
                else
                {
                    textBox2.Text = args["file"].Replace(textBox1.Text, "");
                }
            }

            // イベントハンドラで実行するコマンドを設定
            if (args.ContainsKey("cmd"))
            {
                textBox3.Text = args["cmd"];
            }

            // Form_Loadで監視を開始するようにする
            run = args.ContainsKey("run");
        }
        #endregion コンストラクタ

        #region イベントハンドラー
        private void Form1_Load(object sender, EventArgs e)
        {
            // 起動
            if (run)
            {
                // 開始判定
                if (power() && watcher != null)
                {
                    this.WindowState = FormWindowState.Minimized;
                    showBalloonTip("監視を開始しました", ToolTipIcon.Info);
                }
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            // 最小化したときにタスクバーに非表示に設定
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
                this.ShowInTaskbar = false;
                this.notifyIcon1.Visible = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /* ワーキングディレクトリの設定を行う */
            // あらかじめディレクトリが入力されていた場合は初めから選択状態にしておく
            if (Directory.Exists(textBox1.Text))
            {
                fbd.SelectedPath = textBox1.Text;
            }
            // 選択されたフォルダを表示し、ほかのOpenFileDialogの初期位置に設定する
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = fbd.SelectedPath;
                this.ofd1.InitialDirectory = fbd.SelectedPath;
                this.ofd2.InitialDirectory = fbd.SelectedPath;
                this.ofd3.InitialDirectory = fbd.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            /* 監視するファイルの設定を行う */
            if (ofd1.ShowDialog() == DialogResult.OK)
            {
                // ワーキングディレクトリが設定されている場合は簡易的に相対パスに変更する
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
            /* 実行するプログラムを選択する */
            if (ofd2.ShowDialog() == DialogResult.OK)
            {
                // すでに入力されているコマンドラインの、プログラムに相当する部分のみ置き換える
                string str;

                // 構文解析を行い、何か入力されている場合とされていない場合に分ける
                string[] args = parse(textBox3.Text);
                if (args.Length != 0)
                {
                    // 選択されたファイルのパスに空白が含まれている場合は""で囲む
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

                // 結果を表示
                textBox3.Text = str;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // 選択されたファイルをテキストボックスの末尾に追加
            if (ofd3.ShowDialog() == DialogResult.OK)
            {
                // ファイルパスに空白が含まれる場合は""で囲む
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
            // 開始判定
            if (power() && watcher != null)
            {
                this.WindowState = FormWindowState.Minimized;
                showBalloonTip("監視を開始しました", ToolTipIcon.Info);
            }
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            // 手で入力した際にも設定を行う
            if (Directory.Exists(textBox1.Text))
            {
                this.ofd1.InitialDirectory = textBox1.Text;
                this.ofd2.InitialDirectory = textBox1.Text;
                this.ofd3.InitialDirectory = textBox1.Text;
            }
        }

        private void textBox1_DragDrop(object sender, DragEventArgs e)
        {
            // ドロップされたファイル一覧を取得
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files.Length <= 0)
            {
                return;
            }

            // ドロップ先がTextBoxであることを確認
            TextBox target = sender as TextBox;
            if (target == null)
            {
                return;
            }

            // 内容をフォルダ名に変更
            if (Directory.Exists(files[0]))
            {
                target.Text = files[0];
            }
            // ファイルだった場合
            else
            {
                int tmp = files[0].LastIndexOf('\\', files[0].Length - 1);
                string file = files[0].Substring(tmp + 1);
                string filePath = files[0].Substring(0, tmp);

                textBox1.Text = filePath;
                textBox2.Text = file;
            }
        }

        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            // ドロップされたファイル一覧を取得
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files.Length <= 0)
            {
                return;
            }

            // ドロップ先がTextBoxであることを確認
            TextBox target = sender as TextBox;
            if (target == null)
            {
                return;
            }

            if (File.Exists(files[0]))
            {
                target.Text = files[0].Replace(textBox1.Text, "").Trim('\\');
            }
        }

        private void textBox3_DragDrop(object sender, DragEventArgs e)
        {
            // ドロップされたファイル一覧を取得
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files.Length <= 0)
            {
                return;
            }

            // ドロップ先がTextBoxであることを確認
            TextBox target = sender as TextBox;
            if (target == null)
            {
                return;
            }

            // ファイル名に空白が含まれていた場合は"で囲む
            string file = files[0].Replace(textBox1.Text, "").Trim('\\');
            if (file.IndexOf(' ') == -1)
            {
                target.Text = target.Text.Trim() + " " + file;
            }
            else
            {
                target.Text += target.Text.Trim() + " \"" + file + "\"";
            }
        }

        private void textBox_DragEnter(object sender, DragEventArgs e)
        {
            // カーソルの下にドラッグ可能を示すエフェクトをつける
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void watcher_Changed(object source, FileSystemEventArgs e)
        {
            // 同時起動を防ぐ目的
            if (processing) { return; }

            processing = true;
            switch (e.ChangeType)
            {
                // 変更に関してのみ行う
                case WatcherChangeTypes.Changed:
                    switch (commands[0])
                    {
                        case "copy":
                        case "cp":
                            // コマンドライン引数を二つに分割
                            string[] args = parseAll(commands[1]);
                            if (args.Length != 2)
                            {
                                ReturnFromTray();
                                power();
                                MessageBox.Show("プロセスの起動に失敗しました。コマンドの構文が間違っています。\n監視を中断します。", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.textBox3.Focus();
                            }
                            // コピー元のファイルがなければエラー
                            if (File.Exists(args[0]))
                            {
                                try
                                {
                                    File.Copy(args[0], args[1], true);
                                }
                                catch (DirectoryNotFoundException)
                                {
                                    ReturnFromTray();
                                    power();
                                    MessageBox.Show("ファイルコピーに失敗しました。フォルダが見つかりません。\n監視を中断します。", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                catch (FileNotFoundException)
                                {
                                    ReturnFromTray();
                                    power();
                                    MessageBox.Show("ファイルコピーに失敗しました。ファイルが見つかりません。\n監視を中断します。", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                                catch
                                {
                                    ReturnFromTray();
                                    power();
                                    MessageBox.Show("ファイルコピーに失敗しました。\n監視を中断します。", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            }
                            else
                            {
                                ReturnFromTray();
                                power();
                                MessageBox.Show("プロセスの起動に失敗しました。" + args[0] + "が見つかりません。\n監視を中断します。", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.textBox3.Focus();
                            }
                            break;
                        default:
                            // 外部プログラムを実行する
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
                                    MessageBox.Show("プロセスの起動に失敗しました。", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    this.textBox3.Focus();
                                }
                            }
                            catch
                            {
                                ReturnFromTray();
                                power();
                                MessageBox.Show("プロセスの起動に失敗しました。処理を確認してください。\n監視を中断します。", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                this.textBox3.Focus();
                            }
                            break;
                    }
                    break;
            }
            processing = false;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
            this.ShowInTaskbar = true;
            this.notifyIcon1.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        private void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            // 停止時のバルーンチップのみ
            if (watcher == null)
            {
                ReturnFromTray();
            }
        }

        private void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            int idx;

            for (idx = 0; e.ClickedItem.Text != this.contextMenuStrip1.Items[idx].ToString(); idx++) ;

            switch (idx)
            {
                // ウィンドウを表示
                case 0:
                    ReturnFromTray();
                    break;
                // 開始 | 停止
                case 1:
                    bool flag = power();
                    if (watcher != null)
                    {
                        showBalloonTip("監視を開始しました", ToolTipIcon.Info);
                    }
                    else if (flag)
                    {
                        showBalloonTip("監視を停止しました。ウィンドウを表示する場合はクリックしてください。", ToolTipIcon.Info);
                    }
                    else
                    {
                        ReturnFromTray();
                    }
                    break;
                // 終了
                case 2:
                    this.Close();
                    break;
            }
        }

        #endregion イベントハンドラー

        #region メソッド
        /// <summary>
        /// 引数をコマンドラインとして構文解析し、プログラム名とコマンドライン引数を区切ります
        /// </summary>
        /// <param name="s">解析する文章</param>
        /// <returns>区切った引数の配列</returns>
        private string[] parse(string s)
        {
            // 引数リスト
            List<string> args = new List<string>();

            if (!string.IsNullOrWhiteSpace(s))
            {
                // 実行するファイルが""で囲まれているかどうかを判断
                if (s[0] == '"')
                {
                    // 2つ目の"の位置を探す
                    int t = s.IndexOf('"', 1);
                    // なければ全てを実行ファイル名とみなす
                    if (t == -1)
                    {
                        args.Add(s);
                    }
                    // あれば実行ファイル名とコマンドライン引数を分割
                    else
                    {
                        args.Add(s.Substring(0, t + 1));
                        args.Add(s.Substring(t + 2));
                    }
                }
                else
                {
                    // 実行ファイル名とコマンドライン引数の分割位置を探す
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

        /// <summary>
        /// 引数をコマンドラインとして構文解析し、プログラム名とコマンドライン引数を区切ります。
        /// コマンドライン引数も一つ一つ別要素に区切ります。
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string[] parseAll(string s)
        {
            // 引数リスト
            List<string> args = new List<string>();

            if (!string.IsNullOrWhiteSpace(s))
            {
                while (s.Length > 0)
                {
                    // 先頭が"ならば、次の引数は"で囲まれたものと判断する
                    if (s[0] == '"')
                    {
                        // 次の"の位置を探す
                        int t = s.IndexOf('"', 1);
                        // なければ残りを1つの引数とみなす
                        if (t == -1)
                        {
                            args.Add(s);
                            s = "";
                        }
                        // あればそこまでを1引数とし、切り取る
                        else
                        {
                            args.Add(s.Substring(0, t + 1));
                            s = s.Substring(t + 2).Trim();
                        }
                    }
                    else
                    {
                        // 次の引数の分割位置を探す
                        int t = s.IndexOf(' ');

                        // なければ残りを1つの引数とみなす
                        if (t == -1)
                        {
                            args.Add(s);
                            s = "";
                        }
                        // あればそこまでを1引数とし、切り取る
                        else
                        {
                            args.Add(s.Substring(0, t));
                            s = s.Substring(t + 1);
                        }
                    }
                }
            }

            return args.ToArray();
        }

        /// <summary>
        /// トレイからフォームを取り出し表示する
        /// </summary>
        public void ReturnFromTray()
        {
            this.Visible = true;
            this.ShowInTaskbar = true;
            this.notifyIcon1.Visible = false;
            this.WindowState = FormWindowState.Normal;
        }

        /// <summary>
        /// 監視の状態を切り替える
        /// </summary>
        /// <returns>正常に切り替えられた場合はtrue、失敗した場合はfalse</returns>
        private bool power()
        {
            // 動作中
            if (watcher != null)
            {
                // FileSystemWatcherを停止
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;

                // GUIを元に戻す
                this.button5.Text = "開始(&S)";
                this.button1.Enabled = true;
                this.button2.Enabled = true;
                this.button3.Enabled = true;
                this.button4.Enabled = true;
                this.textBox1.Enabled = true;
                this.textBox2.Enabled = true;
                this.textBox3.Enabled = true;
                this.notifyIcon1.Text = "Time Stamp Observer\n待機中";
                this.contextMenuStrip1.Items[1].Text = "開始(&S)";
            }
            // 停止中
            else
            {
                // 引数用意
                string wd = string.IsNullOrWhiteSpace(textBox1.Text) ? Directory.GetCurrentDirectory() : textBox1.Text;
                string filePath;
                string file;
                string cmdln = textBox3.Text;
                cmdln = cmdln.Replace("$F", textBox2.Text);
                cmdln = cmdln.Replace("$$", "$");
                commands = parse(cmdln);

                // ファイル名とファイルパスを分離する
                int tmp = textBox2.Text.LastIndexOf('\\', textBox2.Text.Length - 1);
                file = textBox2.Text.Substring(tmp + 1);

                // ファイルパスは絶対パスにする
                if (Path.IsPathRooted(textBox2.Text))
                {
                    filePath = textBox2.Text.Substring(0, tmp);
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

                // 引数の確認
                if (!string.IsNullOrWhiteSpace(wd))
                {
                    if (Directory.Exists(wd))
                    {
                        Directory.SetCurrentDirectory(wd);
                    }
                    else
                    {
                        MessageBox.Show("ワーキングディレクトリ(" + wd + ")が正しくありません", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.textBox1.Focus();
                        return false;
                    }
                }
                if (string.IsNullOrWhiteSpace(textBox2.Text) || !File.Exists(textBox2.Text))
                {
                    MessageBox.Show((string.IsNullOrWhiteSpace(textBox2.Text) ? "" : "ファイル名: " + (Path.IsPathRooted(textBox2.Text) ? "" : filePath.TrimEnd('\\') + "\\" + textBox2.Text+ "\n")) +"ファイルが見つかりません", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.textBox2.Focus();
                    return false;
                }
                if (string.IsNullOrWhiteSpace(filePath) || !Directory.Exists(filePath))
                {
                    MessageBox.Show("ファイルの場所: " + filePath + "\nファイルの場所を確認してください", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.textBox2.Focus();
                    return false;
                }
                if (commands.Length == 0)
                {
                    MessageBox.Show((string.IsNullOrWhiteSpace(cmdln) ? "" : "設定中の処理: " + cmdln + "\n") + "処理を確認してください", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.textBox3.Focus();
                    return false;
                }

                // FileSystemWatcherの設定
                watcher = new FileSystemWatcher();
                watcher.Path = filePath;
                watcher.Filter = file;
                watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastAccess;
                watcher.SynchronizingObject = this;

                watcher.Changed += new FileSystemEventHandler(watcher_Changed);

                // FileSystemWatcherを有効に
                watcher.EnableRaisingEvents = true;

                this.button5.Text = "停止(&S)";
                this.button1.Enabled = false;
                this.button2.Enabled = false;
                this.button3.Enabled = false;
                this.button4.Enabled = false;
                this.textBox1.Enabled = false;
                this.textBox2.Enabled = false;
                this.textBox3.Enabled = false;
                this.notifyIcon1.Text = "Time Stamp Observer\n監視中";
                this.contextMenuStrip1.Items[1].Text = "停止(&S)";
            }
            return true;
        }

        private void showBalloonTip(string text, ToolTipIcon icon)
        {
            this.notifyIcon1.ShowBalloonTip(500, "Time Stamp Observer", text, icon);
        }
        #endregion メソッド
    }
}
