namespace WykopDiscussions
{
    /// <summary>
    /// It handles preparing discussion data for TikTok video scripts.
    /// The Format is entries going from best to worse and the post beign first.
    /// </summary>
    internal class DataPreparation
    {
        readonly TextToSpeech _speech = new();
        HtmlScreenshoter? _htmlScreenshoter = null;
        readonly VideoScriptCreator _videoScriptCreator = new();

        private const string _KeepEntriesFileName = "keep";

        /// <summary>
        /// Main entry of DataPreparation class. Runs Data Preparation on a list of discussions.
        /// </summary>
        /// <param name="isExtendedDataPreparation">Extended data preparation means that method will create audio files video script</param>
        /// <param name="discussions">List of discussions to prepare data</param>
        /// <param name="discussionRange">Range of discussions, used to create video script, which later runs Extended Data Preparation</param>
        /// <returns></returns>
        public async Task RunDataPreparation(bool isExtendedDataPreparation, List<Discussion> discussions)
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH DATA PREPARATION", "Start"));

            if (!isExtendedDataPreparation)
            {
                _htmlScreenshoter = new HtmlScreenshoter();
            }

            foreach (var discussion in discussions)
            {
                // If it's bad quality, then we don't want it to go further
                if (discussion.Quality == QualityCheck.DiscussionQuality.Bad)
                    continue;
                else
                {
                    // Get all entries in the Format and save them to a folder
                    var entries = PrepareSingleDiscussionData(discussion);

                    // Creates folder for the discussion data in the right quality folder
                    string dataFolder = Path.Combine(Manager.EntriesDirectory, discussion.Quality.ToString(), discussion.Id.ToString());
                    Directory.CreateDirectory(dataFolder);

                    if (!isExtendedDataPreparation)
                    {
                        // Saves prepared HTML files of discussion's entries
                        SaveDiscussionEntries(discussion, entries, dataFolder);

                        // Saves screenshots of all HTML entries in the discussion Data folder and disposed HTML screenshoter 
                        SaveDiscussionEntriesHtmlScreenshots(discussion, entries, dataFolder);

                        // If regular data preparation, then creates scripts to run extended data preparation
                        // It's not done in one go, because audio files are not free to create
                        // I want only worthy discussions to be remade into videos
                        CreatePrepareDiscussionScript(discussion.Id, dataFolder);
                        CreateWantedEntriesFile(discussion.Id, dataFolder, entries.Count);
                    }
                    // Extended data preparation creates audio files and video script
                    else
                    {
                        // Reads list of wanted to keep entries from a file
                        var wantedEntries = ReadWantedEntriesFile(discussion.Id, dataFolder);

                        await SaveDiscussionEntriesSpokenAudio(discussion, entries, dataFolder, wantedEntries);
                        _videoScriptCreator.PreCreateVideoScript(discussion.Id, dataFolder);

                        CreateRedoImageAndAudioScript(dataFolder);
                    }

                }
            }

            if (!isExtendedDataPreparation && _htmlScreenshoter != null)
            {
                _htmlScreenshoter.Dispose();
            }

            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH DATA PREPARATION", "End"));
        }

        /// <summary>
        /// Prepares list of discussion entries and returns it.
        /// </summary>
        /// <returns>List of discussion entries in the Format</returns>
        private List<Discussion.DiscussionEntry> PrepareSingleDiscussionData(Discussion discussion)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DATA PREPARATION", "Start"));

            var entries = new List<Discussion.DiscussionEntry>();
            // Get all comments, which are better quality than Bad in descending order
            var qualityComments = (from comment in discussion.Comments
                                   where comment.Quality != QualityCheck.CommentQuality.Bad
                                   orderby comment.Rating descending
                                   select comment).ToList();

            // We add post as the first entry
            entries.Add(discussion.Post);
            // Then we add comments in the Format
            AddCommentsToEntries(discussion.Id, entries, qualityComments);

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DATA PREPARATION", "End"));

            return entries;
        }

        /// <summary>
        /// Adds comments to entries list in the Format.
        /// </summary>
        /// <param name="discussionId">Discussion's id for logging purposes</param>
        /// <param name="entries">List of entries</param>
        /// <param name="qualityComments">List of quality comments</param>
        private void AddCommentsToEntries(int discussionId, List<Discussion.DiscussionEntry> entries, List<Discussion.Comment> qualityComments)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "DATA PREPARATION", "Start Adding Comments"));

            // When we go deeper to comment's replies, we'd lost track of original comments
            // Every time we go one step further and check reply, we save current comments into this list
            // This way we can then reverse the list and add it to the entries list
            List<Discussion.Comment> previousCommentsToAdd = new();

            foreach (var comment in qualityComments)
            {
                // If comments is already in entries, skip it
                if (entries.Contains(comment))
                    continue;

                // Check this comment for original comments
                var commentToCheck = comment;

                while (true)
                {
                    var commentOriginal = qualityComments.Find(x => x.Id == commentToCheck.ReplyToCommentId);

                    // If original comment exists is qualityComments
                    if (commentOriginal != null)
                    {
                        // If entries don't have this comment already
                        if (!entries.Contains(commentOriginal))
                        {
                            // If it's good comment reply, then it's easy.
                            // Add OriginalComment first and then our CommentToCheck
                            if (commentOriginal.Quality == QualityCheck.CommentQuality.GoodCommentReply)
                            {
                                entries.Add(commentOriginal);
                                entries.Add(commentToCheck);
                                break;
                            }
                            // If not then we have to go one step deeper.
                            // Save CommentToCheck in a reminder list.
                            // Change commentToCheck to CommentOriginal and run the loop again.
                            else
                            {
                                previousCommentsToAdd.Add(commentToCheck);
                                commentToCheck = commentOriginal;
                                continue;
                            }
                        }
                    }

                    // Add commentToCheck to entries list and break the loop.
                    entries.Add(commentToCheck);
                    break;
                }

                // If we have any comments on reminder list
                // Reverse their order and add them to entries
                // Let's show it on an example
                // TopPost doesn't matter so we skip it
                // QualityComments {id, rating, replyToId}
                // {1, 500 - vg, 2}
                // {2, 300 - vg, 3}
                // {3, 200 - g, 4}
                // {4, 50 - gr, 5}
                // We start with 1 but we have to go and check comment's 2 replyToId
                // Now we have to check comment's 3 replyToId
                // Now we checked comment 4 and it's just good original comment
                // We have to keep track of comments 1, 2 and 3 to later add them to entires
                // We reverse the list to put them as 4, 3, 2, 1 to show original comments before replies
                if (previousCommentsToAdd.Count > 0)
                {
                    previousCommentsToAdd.Reverse();

                    entries.AddRange(previousCommentsToAdd);

                    previousCommentsToAdd = new List<Discussion.Comment>();
                }
            }

            Console.WriteLine(Manager.CreateString(discussionId, 0, "DATA PREPARATION", "End Adding Comments"));
        }

        /// <summary>
        /// Saves entries HTMLs to a folder.
        /// </summary>
        /// <param name="discussion"></param>
        /// <param name="entries">List of entries in the Format</param>
        private void SaveDiscussionEntries(Discussion discussion, List<Discussion.DiscussionEntry> entries, string dataFolder)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DATA PREPARATION", "Start Save Entries HTMLs"));

            // Saves all entries into a folder
            // Naming convention is [index].html
            for (int i = 0; i < entries.Count; i++)
            {
                var filePath = dataFolder + "\\" + i + ".html";
                File.WriteAllText(filePath, entries[i].Html);
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DATA PREPARATION", "End Save Entries HTMLs"));
        }

        /// <summary>
        /// Reads list of unwanted entries from a file.
        /// </summary>
        /// <param name="discussionId">Id of a discussion for logging purposes</param>
        /// <param name="dataFolder">Folder with entries data</param>
        /// <returns>List of unwanted entries</returns>
        private List<int> ReadWantedEntriesFile(int discussionId, string dataFolder)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "DATA PREPARATION", "Start Read Wanted Entries File"));

            // Creates script path
            var filePath = Path.Combine(dataFolder, _KeepEntriesFileName);
            filePath = Path.ChangeExtension(filePath, "txt");

            var lines = File.ReadAllLines(filePath);

            Console.WriteLine(Manager.CreateString(discussionId, 0, "DATA PREPARATION", "End Read Wanted Entries File"));

            return lines.Select(int.Parse).ToList();
        }

        /// <summary>
        /// Saves audio files of all entries in a list.
        /// </summary>
        /// <param name="discussion">Discussion is used to set path</param>
        /// <param name="entries">List of entries</param>
        /// <param name="dataFolder">Data folder</param>
        private async Task SaveDiscussionEntriesSpokenAudio(Discussion discussion, List<Discussion.DiscussionEntry> entries, string dataFolder, List<int> wantedEntries)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DATA PREPARATION", "Start Save Entries Audio"));

            List<double> audiosLength = new();

            for (int i = 0; i < entries.Count; i++)
            {
                if (!wantedEntries.Contains(i))
                    continue;

                var entryText = entries[i].Text;

                var filePath = dataFolder + "\\" + i + ".wav";

#pragma warning disable CS8604 // Possible null reference argument. We checked that earlier
                var length = await _speech.GenerateAudioFile(discussion.Id, i, filePath, entryText, entries[i].IsAuthorMale);
#pragma warning restore CS8604 // Possible null reference argument.
                audiosLength.Add(length);
            }

            SaveDiscussionEntriesSpokenAudioLengths(dataFolder, audiosLength);

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DATA PREPARATION", "End Save Entries Audio"));
        }

        /// <summary>
        /// Writes length of all audio files into a file.
        /// </summary>
        /// <param name="dataFolder">Folder where to write the file</param>
        /// <param name="audiosLengths">List of audios length</param>
        private void SaveDiscussionEntriesSpokenAudioLengths(string dataFolder, List<double> audiosLengths)
        {
            var filePath = Path.Combine(dataFolder, "lengths");
            filePath = Path.ChangeExtension(filePath, ".txt");

            var list = audiosLengths.ConvertAll<string>(x => x.ToString());
            File.WriteAllLines(filePath, list);
        }

        /// <summary>
        /// Makes screenshots of every entry in a list.
        /// </summary>
        /// <param name="discussion">Discussion is needed to set the screenshots path</param>
        /// <param name="entries">List of entries</param>
        private void SaveDiscussionEntriesHtmlScreenshots(Discussion discussion, List<Discussion.DiscussionEntry> entries, string dataFolder)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DATA PREPARATION", "Start Make Screenshot"));

            for (int i = 0; i < entries.Count; i++)
            {
                _htmlScreenshoter?.MakeScreenshot(discussion.Id, i, dataFolder);
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DATA PREPARATION", "End Make Screenshot"));
        }

        /// <summary>
        /// Creates .bat script which runs extended data preparation for a discussion's data.
        /// </summary>
        /// <param name="discussionId">Id of a discussion</param>
        /// <param name="dataFolder">Folder where scripts is gonna be created</param>
        private void CreatePrepareDiscussionScript(int discussionId, string dataFolder)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "DATA PREPARATION", "Start Prepare Discussion Script"));

            // Creates script path
            var scriptPath = Path.Combine(dataFolder, "run");
            scriptPath = Path.ChangeExtension(scriptPath, "bat");

            // Contains script
            // It basically runs the application for only one discussion, doing extended data preparation
            string batch = "@echo off\n" +
                Manager.ExePath + " " + discussionId + " " + (discussionId + 1) + " -extended" + " > \"" + dataFolder + "\\log.txt\"";

            // Write script to batch file
            File.WriteAllText(scriptPath, batch);

            Console.WriteLine(Manager.CreateString(discussionId, 0, "DATA PREPARATION", "End Prepare Discussion Script"));
        }

        /// <summary>
        /// Creates file with all entries numbers, to later select which one to keep for extended data preparation.
        /// </summary>
        /// <param name="discussionId">Id of a discussion, for logging purposes</param>
        /// <param name="dataFolder">Entries folder</param>
        /// <param name="entriesCount">Entries count</param>
        private void CreateWantedEntriesFile(int discussionId, string dataFolder, int entriesCount)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "DATA PREPARATION", "Start Create Wanted Entries File"));

            // Creates file path
            var filePath = Path.Combine(dataFolder, _KeepEntriesFileName);
            filePath = Path.ChangeExtension(filePath, "txt");

            var entries = new List<string>();
            for (int i = 0; i < entriesCount; ++i)
            {
                entries.Add(i.ToString());
            }

            File.WriteAllLines(filePath, entries);

            Console.WriteLine(Manager.CreateString(discussionId, 0, "DATA PREPARATION", "End Create Wanted Entries File"));
        }

        /// <summary>
        /// Creates and puts script to run screenshoter and audio creator again for any entry in a folder.
        /// </summary>
        /// <param name="dataFolder">Folder with entries</param>
        private void CreateRedoImageAndAudioScript(string dataFolder)
        {
            var scriptPath = Path.Combine(dataFolder, "redo");
            scriptPath = Path.ChangeExtension(scriptPath, "bat");

            // Script is simply running redo function on given entry
            string batch = "@echo off\n" +
                "set /p number=Entry number: \n" +
                Manager.ExePath + " -1 -1 -redo -lowlog \"" + dataFolder + "\" %number% > \"" + dataFolder + "\\%number%.txt\"";

            File.WriteAllText(scriptPath, batch);
        }
    }
}
