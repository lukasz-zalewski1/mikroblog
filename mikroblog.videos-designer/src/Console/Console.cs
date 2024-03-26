using mikroblog.fast_quality_check;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

// TODO
// Fix screenshot size
// Round corners around screenshots

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
            try
            {
                string args = $" -File \"{SCRIPT_PATH}\" \"{path}\" \"{videosPath}\" \"{discussionId}\"";

                var process = Process.Start(new ProcessStartInfo("powershell.exe", args));

                SetForegroundWindow(GetConsoleWindow());

                process?.WaitForExit();              
            }
            catch (Exception ex)
            {
                Log.WriteError($"Couldn't start pwsh process, Exception - {ex.Message}");
            }
        }
    }
}
