using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TimeStampObserver
{
    static class Program
    {
        static string message = null;

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Dictionary<string, string> args = parseArgs();
            if (args == null) {
                if (message != null)
                {
                    MessageBox.Show("Failed to Launch : " + message != null ? message : "", "Time Stamp Observer", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Count == 0)
            {
                Application.Run(new Form1());
            }
            else
            {
                Application.Run(new Form1(args));
            }
        }

        /// <summary>
        /// コマンドライン引数を解析します
        /// </summary>
        /// <returns>解析結果を返します。それぞれ、指定されていた場合のみwdにワーキングディレクトリが、fileにファイル名が、cmdに処理が入っています。</returns>
        private static Dictionary<string, string> parseArgs()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            string[] args = Environment.GetCommandLineArgs();
            string s = "";
            try
            {
                for (int i = 1; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "/w":
                            result["wd"] = args[++i];
                            break;
                        case "/f":
                            result["file"] = args[++i];
                            break;
                        case "/c":
                            result["cmd"] = args[++i];
                            break;
                        case "/r":
                            result["run"] = "";
                            break;
                        case "/h":
                            MessageBox.Show(
                                "/h … ヘルプを表示\n" +
                                "/r … 可能であればすぐに監視を開始する\n" +
                                "/w [Directory] … ワーキングディレクトリを指定\n" +
                                "/f [File] … 監視ファイルを指定\n" +
                                "/c [String] … 実行する処理を指定。オプションを使わず引数を列挙するだけでも指定可能\n",
                                "Time Stamp Observer Help", MessageBoxButtons.OK, MessageBoxIcon.Question);
                            return null;
                        default:
                            if (args[i][0] != '/') {
                                s += args[i] + " ";
                                break;
                            }
                            else
                            {
                                message = "無効なオプションです。";
                                return null;
                            }
                    }
                }
            }
            catch
            {
                message = "オプションの書式が間違っています。";
                return null;
            }
            // ファイル指定なしかつオプション指定なしが一つでもあった場合
            if (!result.ContainsKey("file") && s != "")
            {
                int tmp = s.IndexOf(' ');
                result["file"] = s.Substring(0, tmp);
                s = s.Substring(tmp).Trim();
            }
            if (!result.ContainsKey("cmd") && s != "")
            {
                result["cmd"] = s.Trim();
            }
            return result;
        }
    }
}
