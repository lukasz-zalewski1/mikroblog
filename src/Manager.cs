using OpenQA.Selenium.Support.UI;

namespace WykopDiscussions
{
    /// <summary>
    /// Used as an entry point to every program function
    /// </summary>
    internal class Manager
    {
        // To limnit ammount of logs
        public static bool MaxLog = false;

        public const string ExePath = "\"C:\\Users\\LZA\\OneDrive - FRABA\\General\\Private\\Github\\mikroblog\\bin\\Debug\\net7.0\\mikroblog.exe\"";

        // This constant sets up workplace directory, where are working files will be placed
        public const string WorkplaceDirectory = @"C:\Users\LZA\OneDrive - FRABA\General\Private\Github\mikroblog\bin\Debug\net7.0\Workplace";

        // Name of the folder and combined directory, where all downloaded discussions HTMLs are kept
        private const string _DiscussionsFolder = "Discussions";
        public static string DiscussionsDirectory = Path.Combine(WorkplaceDirectory, _DiscussionsFolder);

        // Folder, where entries are stored
        private const string EntriesFolder = "Entries";
        public static readonly string EntriesDirectory = Path.Combine(WorkplaceDirectory, EntriesFolder);

        // Folder, where statistics are stored
        private const string _StatisticsFolder = "Statistics";
        public readonly static string StatisticsDirectory = Path.Combine(WorkplaceDirectory, _StatisticsFolder);

        // Folder, where videos are stored
        private const string _VideosFolder = "Videos";
        public readonly static string VideosDirectory = Path.Combine(WorkplaceDirectory, _VideosFolder);

        private const string _SpeechFolder = "Speech";
        public readonly static string SpeechDirectory = Path.Combine(WorkplaceDirectory, _SpeechFolder);

        // File, where already prepared ranges are saved
        private const string _RangesFile = "ranges.txt";
        public readonly static string RangesPath = Path.Combine(WorkplaceDirectory, _RangesFile);

        public enum ProcessType
        {
            StatisticsCheck,
            DataPreparation,
            CreateVideoScript,
        };

        private readonly DiscussionDownloader _discussionDownloader = new();
        private readonly DiscussionPropertiesReader _discussionPropertiesReader = new();
        private readonly QualityCheck _qualityCheck = new();
        private readonly DiscussionBatchStatistics _discussionBatchStatistics = new();
        private readonly DataPreparation _dataPreparation = new();
        private readonly RangeTracker _rangeTracker = new();
        private readonly ManualAdjustments _manualAdjustments = new();

        /// <summary>
        /// Main entry of the manager class.
        /// </summary>
        /// <param name="processType">Process to run</param>
        /// <param name="discussionsRange">Discussion range to process</param>
        /// <param name="isMaxLog">If true, it spits every possible log. Select false for production</param>
        public async Task Run(ProcessType processType, (int, int) discussionsRange, bool isMaxLog)
        {
            MaxLog = isMaxLog;

            Console.WriteLine(CreateString(-1, 0, "Manager", "Start " + processType.ToString()));

            // Check if start of range is before the end
            if (discussionsRange.Item1 >= discussionsRange.Item2)
            {
                Console.WriteLine(CreateString(-1, 0, "Manager", "Bad Range"));
                return;
            }

            await RunAutomatedProcess(processType, discussionsRange);

            Console.WriteLine(CreateString(-1, 0, "Manager", "End " + processType.ToString()));
        }

        /// <summary>
        /// Runs manual adjustment on given entry.
        /// </summary>
        /// <param name="dataFolder">Folder with the entry</param>
        /// <param name="entryId">Id of the entry to adjust / redo</param>
        public async Task RunManualAdjustment(string dataFolder, int entryId, string screenshotOnly)
        {
            bool redoAudio = true;

            if (screenshotOnly == "-screenshot")
                redoAudio = false;

            await _manualAdjustments.RedoImageAndAudio(dataFolder, entryId, redoAudio);
        }

        /// <summary>
        /// Creates video script for a given discussion with given number of entries.
        /// </summary>
        /// <param name="discussionId">Id of a discussion</param>
        /// <param name="entriesFolder">Folder with entries</param>
        /// <param name="entriesCount">Entries count</param>
        public void RunCreateVideoScript(int discussionId, string entriesFolder, int entriesCount)
        {
            var videoScriptCreator = new VideoScriptCreator();

            videoScriptCreator.CreateEntriesVideosScript(discussionId, entriesCount, entriesFolder);
        }

        /// <summary>
        /// Manually creates speech file.
        /// </summary>
        public async Task RunManualSpeech()
        {
            TextToSpeech textToSpeech = new();

            List<string> lines = File.ReadAllLines(SpeechDirectory + "\\a.txt").ToList();

            bool isMale = Convert.ToBoolean(Convert.ToInt32(lines[0]));
            lines.RemoveAt(0);

            string text = string.Concat(lines);

            await textToSpeech.GenerateAudioFile(-1, -1, SpeechDirectory + "\\a.wav", text, isMale);
        }

        /// <summary>
        /// Runs one of the automated proceeses.
        /// </summary>
        /// <param name="processType">Process to run</param>
        /// <param name="discussionsRange">Range of discussion to run the process on</param>
        private async Task RunAutomatedProcess(ProcessType processType, (int, int) discussionsRange)
        {
            await _discussionDownloader.StartDownloadingDiscussions(discussionsRange);
            var discussions = await _discussionPropertiesReader.StartReadingDiscussions(discussionsRange);

            // Downloads, Reads and Runs Statistics Check
            if (processType == ProcessType.StatisticsCheck)
            {
                await _discussionBatchStatistics.CalculateAndSaveStatistics(discussions, discussionsRange);
            }
            else
            {
                _qualityCheck.RunQualityCheck(discussions);

                // If process is DataPreparation, then we don't do extended data prep
                // If it's CreateVideoScript, then we do extended data prep
                bool extendedDataPreparation;
                if (processType == ProcessType.DataPreparation) extendedDataPreparation = false;
                else extendedDataPreparation = true;

                await _dataPreparation.RunDataPreparation(extendedDataPreparation, discussions);

                // Saves prepared ranges of discussion IDs at the end, when preparation is done
                _rangeTracker.AddRange(discussionsRange);
            }
        }

        /// <summary>
        /// Generic function to unify logs and paths strings.
        /// </summary>
        /// <param name="discussionId">Id of a discussion</param>
        /// <param name="page">Currently handled page</param>
        /// <param name="messageType">Type (Header) of a message</param>
        /// <param name="message">Specific information to display to user</param>
        /// <param name="isPath">If function is used to create path. If true, it will add backslash before the string</param>
        /// <returns>Log or path string</returns>
        public static string CreateString(int discussionId, int page = 0, string messageType = "", string message = "", bool isPath = false)
        {
            string result;

            // The scheme is
            // {\\}DISCUSSION_{discussionId}_{PAGE_{page}_}{messageType{ - message}}
            if (isPath)
            {
                result = "\\DISCUSSION_" + discussionId;
            }
            else
            {
                result = "DISCUSSION_" + discussionId;
            }

            if (page != 0)
            {
                result += "_PAGE_" + page;
            }
            if (!string.IsNullOrEmpty(messageType))
            {
                result += " " + messageType.ToUpper();

                if (!string.IsNullOrEmpty(message))
                {
                    result += " - " + message;
                }
            }

            return result;
        }
    }
}
