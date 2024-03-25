using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

// TODO:
// Make images of entries round
// Comments
// Restructure

namespace mikroblog.videos_designer
{
    internal class Console
    {
        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        [return:  MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        private const string SCRIPT_PATH = "pwsh\\CreateVideo.ps1";

        [STAThread]
        public static void Main()
        {
            App.Main();
        }

        public static void ExecuteCreateVideoScript(string path, string videosPath, string discussionId)
        {
            string args = $" -File \"{SCRIPT_PATH}\" \"{path}\" \"{videosPath}\" \"{discussionId}\"";

            Process.Start(new ProcessStartInfo("powershell.exe", args));

            SetForegroundWindow(GetConsoleWindow());
        }
    }
}
