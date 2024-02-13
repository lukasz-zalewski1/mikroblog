namespace mikroblog.fast_quality_check
{
    class QualityCheck
    {
        /// <summary>
        /// Possible qualities of an entry.
        /// </summary>
        private enum EntryQuality
        {
            Bad = 0,
            Good = 1,
            VeryGood = 2
        }

        /// <summary>
        /// Possible qualities of a discussion.
        /// </summary>
        public enum DiscussionQuality
        {
            Bad = 0,
            Good = 1,
            VeryGoodPost = 2,
            VeryGoodComment = 3,
            VeryGood = 4
        }

        private readonly string CONFIG_QUALITY_CONDITIONS_NAME = "QualityConditions";

        private const string CONFIG_VERY_GOOD_POST_RATING_KEY = "VeryGoodPostRating";
        private const string CONFIG_GOOD_POST_RATING_KEY = "GoodPostRating";
        private const string CONFIG_VERY_GOOD_COMMENT_RATING_KEY = "VeryGoodCommentRating";
        private const string CONFIG_GOOD_COMMENT_RATING_KEY = "GoodCommentRating";

        private readonly Dictionary<EntryQuality, int> POST_QUALITY_CONDITIONS = new();
        private readonly Dictionary<EntryQuality, int> COMMENT_QUALITY_CONDITIONS = new();

        /// <summary>
        /// Reads QualityConditions config.
        /// </summary>
        public QualityCheck()
        {
            Config configQualityConditions = new(CONFIG_QUALITY_CONDITIONS_NAME);

            ReadQualityConditionsFromConfig(configQualityConditions);
        }

        /// <summary>
        /// Reads QualityConditions values from the config object.
        /// </summary>
        private void ReadQualityConditionsFromConfig(Config config)
        {
            var ratingVGP = config.GetInt(CONFIG_VERY_GOOD_POST_RATING_KEY);
            var ratingGP  = config.GetInt(CONFIG_GOOD_POST_RATING_KEY);
            var ratingVGC = config.GetInt(CONFIG_VERY_GOOD_COMMENT_RATING_KEY);
            var ratingGC = config.GetInt(CONFIG_GOOD_COMMENT_RATING_KEY);

            if (ratingVGP == null || ratingGP == null || ratingVGC == null || ratingGC == null)
            {
                Log.WriteError($"Quality Conditions read from config failed");
                return;
            }

            POST_QUALITY_CONDITIONS[EntryQuality.VeryGood]      = (int)ratingVGP;
            POST_QUALITY_CONDITIONS[EntryQuality.Good]          = (int)ratingGP;
            COMMENT_QUALITY_CONDITIONS[EntryQuality.VeryGood]   = (int)ratingVGC;
            COMMENT_QUALITY_CONDITIONS[EntryQuality.Good]       = (int)ratingGC;
        }

        /// <summary>
        /// Checks quality of a discussion based on post and top comment ratings.
        /// </summary>
        /// <param name="ratings">Post and comment rating</param>
        public DiscussionQuality Check(Tuple<int, int> ratings)
        {
            Log.Write("QualityCheck");

            var quality = CalculateDiscussionQuality(CalculatePostQuality(ratings.Item1), CalculateCommentQuality(ratings.Item2));

            if (quality != DiscussionQuality.Bad)
                Log.WriteSuccess($"{quality}");


            return quality;
        }

        /// <summary>
        /// Calculates post quality based on its rating.
        /// </summary>
        /// <param name="rating">Post rating</param>
        /// <returns>Quality of the post</returns>
        private EntryQuality CalculatePostQuality(int rating)
        {
            try
            {
                if (rating >= POST_QUALITY_CONDITIONS[EntryQuality.VeryGood])
                    return EntryQuality.VeryGood;
                else if (rating >= POST_QUALITY_CONDITIONS[EntryQuality.Good])
                    return EntryQuality.Good;
                else
                    return EntryQuality.Bad;
            }
            catch (Exception ex)
            {
                Log.WriteError($"Calculating Post Quality, Exception - {ex.Message}");
                return EntryQuality.Bad;
            }
        }

        /// <summary>
        /// Calculates top comment quality based on its rating.
        /// </summary>
        /// <param name="rating">Top comment rating</param>
        /// <returns>Quality of the top comment</returns>
        private EntryQuality CalculateCommentQuality(int rating)
        {
            try
            {
                if (rating >= COMMENT_QUALITY_CONDITIONS[EntryQuality.VeryGood])
                    return EntryQuality.VeryGood;
                else if (rating >= COMMENT_QUALITY_CONDITIONS[EntryQuality.Good])
                    return EntryQuality.Good;
                else
                    return EntryQuality.Bad;
            }
            catch (Exception ex)
            {
                Log.WriteError($"Calculating Comment Quality, Exception - {ex.Message}");
                return EntryQuality.Bad;
            }
        }

        /// <summary>
        /// Calculates quality of a discussion based on its post and top comment qualities.
        /// </summary>
        /// <returns>Quality of the discussion</returns>
        private DiscussionQuality CalculateDiscussionQuality(EntryQuality postQuality, EntryQuality commentQuality)
        {
            return postQuality switch
            {
                EntryQuality.VeryGood when commentQuality == EntryQuality.VeryGood  => DiscussionQuality.VeryGood,
                EntryQuality.VeryGood                                               => DiscussionQuality.VeryGoodPost,
                _ when commentQuality == EntryQuality.VeryGood                      => DiscussionQuality.VeryGoodComment,
                EntryQuality.Good when commentQuality == EntryQuality.Good          => DiscussionQuality.Good,
                _                                                                   => DiscussionQuality.Bad
            };
        }
    }
}
