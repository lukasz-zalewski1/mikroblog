namespace WykopDiscussions
{
    /// <summary>
    /// Used to create batch to create video scripts.
    /// </summary>
    internal class VideoScriptCreator
    {
        private const string _FFMPEGFileName = "ffmpeg.exe";
        private static readonly string _FFMPEGPath = Path.Combine(Manager.WorkplaceDirectory, _FFMPEGFileName);

        private const string _PreCreateVideoScriptFileName = "PreCreateVideo.bat";
        private const string _CreateVideoScriptFileName = "CreateVideo.bat";

        /// <summary>
        /// Creates batch script, which asks about number of entries in the discussion and later creates another script, which creates mp4 files.
        /// </summary>
        /// <param name="discussionId">Id of a discussion</param>
        /// <param name="entriesFolder">Folder with discussion entries</param>
        public void PreCreateVideoScript(int discussionId, string entriesFolder)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "PRE VIDEO SCRIPT SCREATOR", "Start"));

            string command = "@echo off\r\n" +
                             "set /p num=Entries Number:\r\n" +
                             Manager.ExePath + " " + discussionId + " " + (discussionId + 1) + " -vs -lowlog \"" + entriesFolder + "\" %num%";
            string filePath = Path.Combine(entriesFolder, _PreCreateVideoScriptFileName);
            File.WriteAllText(filePath, command);

            Console.WriteLine(Manager.CreateString(discussionId, 0, "PRE VIDEO SCRIPT SCREATOR", "End"));
        }

        /// <summary>
        /// Creates video script for a discussion.
        /// </summary>
        /// <param name="discussionId">Id of a discussion</param>
        /// <param name="entriesFolder">Folder with discussion entries</param>
        public void CreateEntriesVideosScript(int discussionId, int entriesCount, string entriesFolder)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "VIDEO SCRIPT SCREATOR", "Start Creating Video Script"));

            var commands = new List<string>();

            PrepareCommands(commands, entriesCount, discussionId);
            SaveEntriesVideosScript(entriesFolder, commands);

            Console.WriteLine(Manager.CreateString(discussionId, 0, "VIDEO SCRIPT SCREATOR", "End Creating Video Script"));
        }

        /// <summary>
        /// Prepares commands for the video script.
        /// </summary>
        /// <param name="commands">List of commands</param>
        /// <param name="discussionId">Id of a discussion</param>
        private void PrepareCommands(List<string> commands, int entriesCount, int discussionId)
        {
            // Adds commands which read audios length to a script
            AddReadAudioLengthsToCommands(commands);

            // Add create video folder commands
            AddCreateVideoFolderCommand(commands, discussionId);

            // Adds all commands to create video files for every single entry
            AddAllSingleVideoCreationCommands(commands, entriesCount);

            // Set file path to combined video
            var combinedVideoDirectory = Path.Combine(Manager.VideosDirectory, discussionId.ToString());
            var combinedVideoFilePath = Path.Combine(combinedVideoDirectory, discussionId.ToString());
            combinedVideoFilePath = Path.ChangeExtension(combinedVideoFilePath, "mp4");

            AddCombineVideoCommands(commands, entriesCount, combinedVideoFilePath);

            AddFinishingCommands(commands, combinedVideoDirectory);
        }

        /// <summary>
        /// Add commands to read audios length inside of the batch file.
        /// </summary>
        /// <param name="commands">Commands list</param>
        private void AddReadAudioLengthsToCommands(List<string> commands)
        {
            commands.Add("@echo off");
            commands.Add("setlocal EnableDelayedExpansion\n");
            commands.Add("set i=0");
            commands.Add("for /f \"tokens=*\" %%a in (lengths.txt) do (");
            commands.Add("\tset /a i+=1");
            commands.Add("\tset \"length[!i!]=%%a");
            commands.Add(")");
        }

        /// <summary>
        /// Adds command to create new folder for a video.
        /// </summary>
        /// <param name="commands">List of commands</param>
        /// <param name="discussionId">Id of a discussion</param>
        private void AddCreateVideoFolderCommand(List<string> commands, int discussionId)
        {
            commands.Add("mkdir \"" + Manager.VideosDirectory + "\\" + discussionId + "\"");
        }

        /// <summary>
        /// Add to the list of commands, commands to create video files for every single entry.
        /// </summary>
        /// <param name="commands">List of commands</param>
        /// <param name="entriesCount">Ammount of entries</param>
        /// <param name="entriesFolder">Entries folder</param>
        private void AddAllSingleVideoCreationCommands(List<string> commands, int entriesCount)
        {
            // It loop for every image, because at this moment there has to be the same ammount of images and audios
            for (int i = 0; i < entriesCount; i++)
            {
                AddSingleVideoCreationCommands(commands, i);
            }
        }

        /// <summary>
        /// Adds to the list of commands, commands to create video for a single entry.
        /// </summary>
        /// <param name="commands">List of commands</param>
        /// <param name="index">Index of an entry</param>
        private void AddSingleVideoCreationCommands(List<string> commands, int index)
        {
            // Create commands by combining audio and images
            commands.Add("\"" + _FFMPEGPath + "\" "
                         + "-y -loop 1 -i "
                         + index + ".png" + " -i "
                         + index + ".wav"
                         + " -tune stillimage -c:a aac -b:a 192k -pix_fmt yuv420p -vf scale=1080:1920 -t !length[" + (index + 1) + "]! -filter:a \"volume=1\" "
                         + index + ".mp4");
        }

        /// <summary>
        /// Adds commands to combine all videos into one.
        /// </summary>
        /// <param name="commands">List of commands</param>
        /// <param name="entriesCount">Entries count</param>
        /// <param name="combinedVideoFilePath">Path to the combined video</param>
        private void AddCombineVideoCommands(List<string> commands, int entriesCount, string combinedVideoFilePath)
        {
            // Run FFMPEG
            var line = "\"" + _FFMPEGPath + "\""
                         + " -y ";

            // Add every entry
            for (int i = 0; i < entriesCount; ++i)
            {
                line += "-i " + (i + ".mp4 ");
            }

            // Use complex_filter to combine it and make sure it doesn't skip audioless parts
            line += "-filter_complex \"";

            for (int i = 0; i < entriesCount; ++i)
            {
                line += "[" + i + ":v][" + i + ":a]";
            }

            // Concatenates videos
            line += "concat=n=" + entriesCount + ":v=1:a=1[v][a]\" -map \"[v]\" -map \"[a]\" -c:a aac -b:a 192k \"" + combinedVideoFilePath + "\"";

            commands.Add(line);
        }

        /// <summary>
        /// Adds finishing commands, to open video folder and end the script to the commands list.
        /// </summary>
        /// <param name="commands">List of commands</param>
        /// <param name="combinedVideoFilePath">Path to combined video</param>
        private void AddFinishingCommands(List<string> commands, string combinedVideoDirectory)
        {
            // Opens folder with combined video
            commands.Add("start %windir%\\explorer.exe \"" + combinedVideoDirectory + "\"");
            // It has to be there for reading audio lengths purposes to work
            commands.Add("endlocal");
        }

        /// <summary>
        /// Saves commands to a script file.
        /// </summary>
        /// <param name="entriesFolder">Folder with discussion entries</param>
        /// <param name="commands">List of commands</param>
        private void SaveEntriesVideosScript(string entriesFolder, List<string> commands)
        {
            var scriptPath = Path.Combine(entriesFolder, _CreateVideoScriptFileName);

            File.WriteAllLines(scriptPath, commands);
        }
    }
}
