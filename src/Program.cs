using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ChronoRuler
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    Ui.Scale = g.DpiX / 96f;
                    if (Ui.Scale < 1f) Ui.Scale = 1f;
                    if (Ui.Scale > 3f) Ui.Scale = 3f;
                }
            }
            catch
            {
                Ui.Scale = 1f;
            }

            string screenshot = null;
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "--screenshot" && i + 1 < args.Length)
                    {
                        screenshot = args[++i];
                    }
                    else if (args[i] == "--data" && i + 1 < args.Length)
                    {
                        Store.UsePath(args[++i]);
                    }
                }

                Application.Run(new MainForm(screenshot));
            }
            catch (Exception ex)
            {
                try
                {
                    string log = Path.Combine(Path.GetTempPath(), "ddl-timer-error.log");
                    File.WriteAllText(log, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n" + ex);
                }
                catch
                {
                }
                MessageBox.Show("ddl计时器启动失败：\n\n" + ex.Message, "ddl计时器",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
