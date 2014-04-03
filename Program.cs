using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Diagnostics;

namespace Chocorep2
{
    public static class Program
    {

        [STAThread]
        public static void Main(string[] args)
        {
            if (Environment.CommandLine.IndexOf("/up", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                try
                {
                    string[] _args = Environment.GetCommandLineArgs();
                    int pid = Convert.ToInt32(_args[2]);
                    Process.GetProcessById(pid).WaitForExit();    // 終了待ち
                }
                catch
                {
                }
            }
            else if (Environment.CommandLine.IndexOf("/file", StringComparison.CurrentCultureIgnoreCase) != -1)
            {
                VersionManager.CreateNewLocalVersionFile();
                return;
            }
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
