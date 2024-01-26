namespace WykopDiscussions
{
    /// <summary>
    /// Class for calculating statistics of a discussion batch.
    /// </summary>
    internal class DiscussionBatchStatistics : Statistics
    {
        // Extended statistics show single discussion statistics
        // It says how much percent of discussion from discussion batch will have these statistics displayed
        private const double _TopDiscussionsExtendedStatisticsPercentage = 1.0;
        private int _TopDiscussionsExtendedStatisticsCount;

        // List of discussions statistics
        private readonly List<DiscussionStatistics> _statistics = new();

        /// <summary>
        /// Calculates and saves statistics of a batch of discussions.
        /// </summary>
        /// <param name="discussions">List of discussions</param>
        /// <param name="discussionsRange">Range of discussions, used in statistics filename</param>
        /// <returns></returns>
        public async Task CalculateAndSaveStatistics(List<Discussion> discussions, (int, int) discussionsRange)
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "Start"));

            // Calculates statistics for each discussion
            foreach (var discussion in discussions)
            {
                _statistics.Add(new DiscussionStatistics(discussion));
            }

            CalculateBatchStatistics();

            // Gets statistics and extended statistiscs string and concats them
            var statistics = CreateStatisticsString();
            statistics += CreateExtendedStatisticsString();

            // Saves statistics of a batch to a file
            await SaveDiscussionBatchStatistics(statistics, discussionsRange);

            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "End"));
        }

        /// <summary>
        /// Adds up every single discussion statistics and then takes the average of it.
        /// </summary>
        private void CalculateBatchStatistics()
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "Start Calculating"));

            // Calculates each statistics by single discusssion statistics
            _statistics.ForEach(x => YearsOld += x.YearsOld);
            _statistics.ForEach(x => PostRating += x.PostRating);
            _statistics.ForEach(x => CommentsCount += x.CommentsCount);
            _statistics.ForEach(x => CommentRating += x.CommentRating);

            foreach (var singleDiscussionStatistics in _statistics)
            {
                for (int i = 0; i < _TopCommentsCount; ++i)
                {
                    TopCommentsRatings[i] += singleDiscussionStatistics.TopCommentsRatings[i];
                }
            }

            // Gets the average
            YearsOld /= _statistics.Count;
            PostRating /= _statistics.Count;
            CommentsCount /= _statistics.Count;
            CommentRating /= _statistics.Count;

            // Takes average of each top comment
            TopCommentsRatings = TopCommentsRatings.Select(x => x / _statistics.Count).ToList();

            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "End Calculating"));
        }

        /// <summary>
        /// Creates and returns batch statistics string.
        /// </summary>
        /// <returns>Batch statistics string</returns>
        public override string CreateStatisticsString()
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "Start Creating Statistics String"));

            // Creates batch statistics string
            var result = "Loaded " + _statistics.Count + " Discussions out of " + _statistics.Count + "\n" +
                "Average Years Old - " + YearsOld.ToString("0.00") + "\n" +
                "Average Post Rating - " + PostRating.ToString("0.00") + "\n" +
                "Average Comments per Discussion - " + CommentsCount.ToString("0.00") + "\n" +
                "Average Comment Rating - " + CommentRating.ToString("0.00") + "\n";

            for (int topCommentRatingIndex = 0; topCommentRatingIndex < _TopCommentsCount; ++topCommentRatingIndex)
            {
                result += "Average " + (topCommentRatingIndex + 1) + " Comment Rating - " + TopCommentsRatings[topCommentRatingIndex].ToString("0.00") + "\n";
            }

            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "End Creating Statistics String"));

            return result;
        }

        /// <summary>
        /// Creates and returns extended batch statistics string.
        /// Extended statistics are top discussion statistics.
        /// </summary>
        /// <returns>Extended batch statistics string</returns>
        private string CreateExtendedStatisticsString()
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "Start Creating Extended Statistics String"));

            var result = "";

            // Top discussions are calculcated by taking PostRating and average CommentRating
            // It will almost always sort by post rating, because of that reason
            _TopDiscussionsExtendedStatisticsCount = Convert.ToInt32(Convert.ToDouble(_statistics.Count) / 100.0 * _TopDiscussionsExtendedStatisticsPercentage);
            var topDiscussions = _statistics.OrderByDescending(x => x.CommentRating + x.PostRating).Take(_TopDiscussionsExtendedStatisticsCount).ToList();

            // Lists top discussions statistics
            for (int i = 0; i < topDiscussions.Count; ++i)
            {
                result += "Top " + (i + 1) + ":\n";
                result += topDiscussions[i].CreateStatisticsString();
            }

            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "End Creating Extended Statistics String"));

            return result;
        }

        /// <summary>
        /// Saves discussion batch statistics string into a file.
        /// </summary>
        /// <param name="statistics">Statistics string</param>
        /// <param name="discussionRange">Range of discussions, used to calculate statistics</param>
        private async Task SaveDiscussionBatchStatistics(string statistics, (int, int) discussionRange)
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "Start Save To File"));

            // Saves statistics of a batch of files into a statistics discussion folder
            string filePath = Path.Combine(Manager.StatisticsDirectory);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            filePath += "\\" + discussionRange.Item1 + ";" + discussionRange.Item2;
            filePath = Path.ChangeExtension(filePath, "txt");

            await File.WriteAllTextAsync(filePath, statistics);

            Console.WriteLine(Manager.CreateString(-1, 0, "BATCH STATISTICS", "End Save To File"));
        }
    }
}
