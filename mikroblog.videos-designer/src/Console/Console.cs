using System;
using System.Diagnostics;

namespace mikroblog.videos_designer
{
    internal class Console
    {
        private const string SCRIPT_PATH = "pwsh\\CreateVideo.ps1";

        [STAThread]
        public static void Main()
        {
            App.Main();
        }

        public static void ExecuteCreateVideoScript(string path, string videosPath)
        {
            string args = $" -File \"{SCRIPT_PATH}\" \"{path}\" \"{videosPath}\"";

            Process.Start(new ProcessStartInfo("powershell.exe", args));
        }
    }
}
