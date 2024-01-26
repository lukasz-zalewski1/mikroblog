namespace WykopDiscussions
{
    /// <summary>
    /// Used to run quality check on discussion entries.
    /// </summary>
    internal class QualityCheck
    {
        public enum PostQuality
        {
            Bad = 0,
            Good = 1,
            VeryGood = 2
        }

        public enum CommentQuality
        {
            Bad = 0,
            Good = 1,
            GoodCommentReply = 2,
            VeryGood = 3
        }

        public enum CommentsQuality
        {
            Bad = 0,
            Good = 1,
            VeryGood = 2
        }

        /// <summary>
        /// It works by selecting top entry quality and making it discussion quality
        /// </summary>
        public enum DiscussionQuality
        {
            Bad = 0,
            GoodPost = 1,
            GoodComments = 2,
            Good = 3,
            VeryGoodPost = 4,
            VeryGoodComments = 5,
            VeryGood = 6
        }

        // If discussion is older that _OldestYearQualityConditions then its YearsOld property will be treated as it'd be _OldestYearQualityConditions
        private const int _OldestYearQualityConditions = 11;
        // Quality Conditions specified for every year { YearsOld, (PostRating, CommentRating) } 
        private readonly Dictionary<int, (int, int)> _QualityConditions = new Dictionary<int, (int, int)>
        {
            { 11, (50, 12) },
            { 10, (60, 15) },
            { 9, (150, 30) },
            { 8, (300, 60) },
            { 7, (500, 100) },
            { 6, (500, 100) },
            { 5, (500, 100) },
            { 4, (600, 120) },
            { 3, (500, 100) },
            { 2, (500, 100) },
            { 1, (500, 100) },
        };

        // How much percent of a very good entry rating is enough to classify as good entry
        private const double _GoodEntryThresholdPercentage = 0.5;

        // Every comment past 1 has less change to get good rating
        // Comments up to _CommentHandicapLimit need less rating to be considered quality
        // Every comment up to _CommentHandicapLimit will need (commentNumber * (_CommentHandicapPercentage / 100)) less upvotes
        // For example if _CommentHandicapPercentage is 50, then
        // 10th comment will need 5 less upvotes to be considered quality
        private const int _CommentHandicapLimit = 50;
        private const double _CommentHandicapMultiplierPercentage = 1.0;

        /// <summary>
        /// Runs quality check on a list of discussions.
        /// </summary>
        /// <param name="discussions">List of discussion to run QualityCheck on</param>
        public void RunQualityCheck(List<Discussion> discussions)
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "QUALITY CHECK", "Start"));

            foreach (var discussion in discussions)
            {   
                CheckSingleDiscussionQuality(discussion);
            }

            Console.WriteLine(Manager.CreateString(-1, 0, "QUALITY CHECK", "End"));
        }

        /// <summary>
        /// Calculates discussion quality.
        /// </summary>
        /// <param name="discussion">Discussion to run QualityCheck on</param>
        private void CheckSingleDiscussionQuality(Discussion discussion)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "Start Single Discussion"));

            PutDiscussionYearIntoQualityConditionsRange(discussion);

            CalculatePostQuality(discussion);
            CalculateCommentsQuality(discussion);
            CalculateDiscussionQuality(discussion);

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "End Single Discussion - " + discussion.Quality.ToString()));
        }

        /// <summary>
        /// Sets discussion's YearsOld to _OldestYearQualityConditions if it's older
        /// </summary>
        /// <param name="discussion"></param>
        private void PutDiscussionYearIntoQualityConditionsRange(Discussion discussion)
        {
            if (discussion.YearsOld > _OldestYearQualityConditions)
            {
                discussion.YearsOld = _OldestYearQualityConditions;
            }
        }

        /// <summary>
        /// Sets discussion.Post.Quality to calculcated quality.
        /// </summary>
        private void CalculatePostQuality(Discussion discussion)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "Start Calculating Post Quality"));

            // Gets quality post rating for specific year
            var qualityRating = _QualityConditions[discussion.YearsOld].Item1;

            if (discussion.Post.Rating >= qualityRating)
            {
                discussion.Post.Quality = PostQuality.VeryGood;
            }
            // If the post's quality is not very good but it's good enough quality
            else if (discussion.Post.Rating >= (int) (qualityRating * _GoodEntryThresholdPercentage))
            {
                discussion.Post.Quality = PostQuality.Good;
            }
            else
            {
                discussion.Post.Quality = PostQuality.Bad;
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "End Calculating Post Quality"));
        }

        /// <summary>
        /// Calculates quality comments rating for a discussion and runs DecideOnCommentQuality with handicapped rating for every comment in a discussion.
        /// </summary>
        private void CalculateCommentsQuality(Discussion discussion)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "Start Comments Quality Check"));

            // Gets quality comment rating for the specific year
            var qualityRating = _QualityConditions[discussion.YearsOld].Item2;
            
            double handicap;
            
            for (int id = 0; id < discussion.Comments.Count; id += 1)
            {
                // Calculates handicap and applies it to qualityRating
                handicap = Math.Min(id, _CommentHandicapLimit);
                handicap /= 100;                
                var qualityRatingWithHandicap = Convert.ToInt32(qualityRating - (qualityRating * handicap * _CommentHandicapMultiplierPercentage));

                // Checks comment quality for a single comment
                CalculateCommentQuality(discussion, id, qualityRatingWithHandicap);
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "End Calculating Post Quality"));
        }

        /// <summary>
        /// Calculates quality of a comment.
        /// </summary>
        /// <param name="id">Id of a comment in discussion's comment list</param>
        /// <param name="qualityRatingWithHandicap">Quality Rating for this specific comment</param>
        private void CalculateCommentQuality(Discussion discussion, int id, int qualityRatingWithHandicap)
        {
            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "Start Calculating Single Comment Quality"));

            var comment = discussion.Comments[id];
            // Quality rating for Good comments
            var qualityRatingGoodComment = (int)(qualityRatingWithHandicap * _GoodEntryThresholdPercentage);

            // If the rating is lower than needed for a Good comment
            if (comment.Rating < qualityRatingGoodComment)
            {
                comment.Quality = CommentQuality.Bad;
            }
            else
            {
                // Decide if it's Good or VeryGood
                if (comment.Rating >= qualityRatingWithHandicap)
                {
                    comment.Quality = CommentQuality.VeryGood;
                }
                else
                {
                    comment.Quality = CommentQuality.Good;
                }

                // If it replies to some other comment
                // Set that original comment quality to GoodCommentReply, only if it's Bad
                if (comment.ReplyToCommentId > 0)
                {
                    var commentRepliedTo = discussion.Comments.Find(x => x.Id == comment.ReplyToCommentId);

                    if (commentRepliedTo != null)
                    {
                        if (commentRepliedTo.Quality == CommentQuality.Bad)
                        {
                            commentRepliedTo.Quality = CommentQuality.GoodCommentReply;
                        }
                    }
                }
            }

            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "End Calculating Single Comment Quality"));
        }

        /// <summary>
        /// Calculates quality of a discussion, based on post and comments quality.
        /// </summary>
        /// <param name="discussion">Discussion to calculate quality on</param>
        private void CalculateDiscussionQuality(Discussion discussion)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "Start Calculating Discussion Quality"));

            var postQuality = discussion.Post.Quality;
            CommentsQuality commentsQuality;

            // Comments quality is top quality of any comment in that discussion
            if (discussion.Comments.Any(x => x.Quality == CommentQuality.VeryGood))
            {
                commentsQuality = CommentsQuality.VeryGood;
            }
            else if (discussion.Comments.Any(x => x.Quality == CommentQuality.Good))
            {
                commentsQuality = CommentsQuality.Good;
            }
            else
            {
                commentsQuality = CommentsQuality.Bad;
            }

            discussion.Quality = postQuality switch
            {
                PostQuality.VeryGood when commentsQuality == CommentsQuality.VeryGood   => DiscussionQuality.VeryGood,
                PostQuality.VeryGood                                                    => DiscussionQuality.VeryGoodPost,
                _ when commentsQuality == CommentsQuality.VeryGood                      => DiscussionQuality.VeryGoodComments,
                PostQuality.Good when commentsQuality == CommentsQuality.Good           => DiscussionQuality.Good,
                PostQuality.Good                                                        => DiscussionQuality.GoodPost,
                _ when commentsQuality == CommentsQuality.Good                          => DiscussionQuality.GoodComments,
                _                                                                       => DiscussionQuality.Bad
            };

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "QUALITY CHECK", "End Calculating Discussion Quality - " + discussion.Quality));
        }
    }
}
