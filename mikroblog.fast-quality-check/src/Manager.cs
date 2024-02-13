namespace mikroblog.fast_quality_check
{
    class Manager
    {
        private static readonly DiscussionDownloader _downloader = new();
        private static readonly DiscussionRatingReader _ratingReader = new();
        private static readonly QualityCheck _qualityCheck = new();

        private static readonly Config _qualityDiscussions = new("QualityDiscussions");

        /// <summary>
        /// Gets the input data from user and checks discussions in the given range.
        /// </summary>
        public static async Task Work()
        {
            var firstDiscussionId = GetFirstDiscussionToDownloadFromUser();
            if (firstDiscussionId == null)
                return;

            var amountOfDiscussionsToCheck = GetAmountOfDiscussionsToDownloadFromUser();
            if (amountOfDiscussionsToCheck == null)
                return;

            await CheckDiscussions((int)firstDiscussionId, (int)amountOfDiscussionsToCheck);
        }

        private static int? GetFirstDiscussionToDownloadFromUser()
        {
            Log.Write("Type in number of the first discussion to download: ");

            var startString = Console.ReadLine();

            // In case you type bigger numbers with dots or commas for better visibility
            if (startString != null)
            {
                startString = startString.Replace(",", "");
                startString = startString.Replace(".", "");
            }

            if (!int.TryParse(startString, out int start))
            {
                Log.WriteError("First discussion to download incorrect number");
                return null;
            }

            return start;
        }

        private static int? GetAmountOfDiscussionsToDownloadFromUser()
        {
            Log.Write("Type in number of discussions to check or 0 if not specified: ");
            if (!int.TryParse(Console.ReadLine(), out int toDownload))
            {
                Log.WriteError("First discussion to download incorrect number");
                return null;
            }

            return toDownload;
        }

        /// <summary>
        /// Runs CheckSingleDiscussion function on discussion in the specified range.
        /// Saves that range into the RangeTracker.
        /// </summary>
        /// <param name="start">First discussion to check</param>
        /// <param name="toDownload">How many discussions to check</param>
        private static async Task CheckDiscussions(int start, int toDownload)
        {
            int currentDiscussionId = start;
            bool finished = false;

            do
            {
                while (!Console.KeyAvailable)
                {               
                    if (toDownload != 0 && currentDiscussionId == start + toDownload)
                    {
                        finished = true;
                        break;
                    }

                    SaveDiscussionQualityToFile(currentDiscussionId, await CheckSingleDiscussion(currentDiscussionId));

                    currentDiscussionId += 1;
                }

                if (finished)
                    break;
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            RangeTracker.Add((start, currentDiscussionId));
        }

        /// <summary>
        /// Downloads, Reads rating and performs a quality check on a discussion.
        /// </summary>
        /// <returns>Quality of the discussion</returns>
        private static async Task<QualityCheck.DiscussionQuality> CheckSingleDiscussion(int discussionId)
        {
            var html = await _downloader.Download(discussionId);
            if (html == null)
                return QualityCheck.DiscussionQuality.Bad;

            var ratings = _ratingReader.Read(html);
            var quality = _qualityCheck.Check(ratings);

            return quality;
        }

        /// <summary>
        /// Saves discussion quality to the file, if the quality was better than Bad.
        /// </summary>
        private static void SaveDiscussionQualityToFile(int discussionId, QualityCheck.DiscussionQuality quality)
        {
            if (quality != QualityCheck.DiscussionQuality.Bad)
                _qualityDiscussions.Add(discussionId.ToString(), quality.ToString());
        }
    }
}
