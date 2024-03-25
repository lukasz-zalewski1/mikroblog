using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace mikroblog.videos_designer
{
    internal partial class Console
    {
        [LibraryImport("kernel32.dll")]
        public static partial IntPtr GetConsoleWindow();

        [LibraryImport("user32.dll")]
        [return:  MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetForegroundWindow(IntPtr hWnd);

        private const string SCRIPT_PATH = "pwsh\\CreateVideo.ps1";

        /// <summary>
        /// Runs WPF application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            App.Main();
        }

        /// <summary>
        /// Launches pwsh script which creates the final video.
        /// </summary>
        /// <param name="path">Folder with discussion files</param>
        /// <param name="videosPath">Folder where videos are stored</param>
        /// <param name="discussionId">Id of the discussion</param>
        public static void CreateAndExecuteVideoScript(string path, string videosPath, string discussionId)
        {
            string args = $" -File \"{SCRIPT_PATH}\" \"{path}\" \"{videosPath}\" \"{discussionId}\"";

            Process.Start(new ProcessStartInfo("powershell.exe", args));

            SetForegroundWindow(GetConsoleWindow());
        }
    }
}
