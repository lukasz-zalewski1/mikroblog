namespace WykopDiscussions
{
    /// <summary>
    /// Class for calculating statistics of a single discussion.
    /// </summary>
    internal class DiscussionStatistics : Statistics
    {
        /// <summary>
        /// Calculates all statistics for a discussion.
        /// </summary>
        /// <param name="discussion"></param>
        public DiscussionStatistics(Discussion discussion)
        {
            YearsOld = discussion.YearsOld;
            PostRating = discussion.Post.Rating;
            CommentsCount = discussion.Comments.Count;
            CommentRating = GetAverageCommentRating(discussion);
            TopCommentsRatings = GetTopDiscussionCommentsRatings(discussion);
        }

        /// <summary>
        /// Gets the average rating of all comments in a discussion.
        /// </summary>
        /// <param name="discussion"></param>
        /// <returns>Average rating of comments in a discussion</returns>
        private double GetAverageCommentRating(Discussion discussion)
        {
            if (discussion.Comments.Any())
            {
                return discussion.Comments.Average(x => x.Rating);
            }

            return 0;
        }

        /// <summary>
        /// Returns {_TopCommentCount} top comments ratings from a discussion
        /// </summary>
        /// <param name="discussion"></param>
        /// <returns>Top comments ratings</returns>
        private List<double> GetTopDiscussionCommentsRatings(Discussion discussion)
        {
            // Order and get top comments from a discussion
            var topCommentsRatings = discussion.Comments.Select(x => x.Rating).OrderByDescending(x => x).ToList();

            // Make sure there is exactly {_TopCommentCount} comments
            // If not add zeros to the list
            if (topCommentsRatings.Count < _TopCommentsCount)
            {
                int difference = _TopCommentsCount - topCommentsRatings.Count;

                for (int i = 0; i < difference; ++i)
                {
                    topCommentsRatings.Add(0);
                }
            }
            else if (topCommentsRatings.Count > _TopCommentsCount)
            {
                topCommentsRatings = topCommentsRatings.GetRange(0, _TopCommentsCount);
            }

            return topCommentsRatings.Select(x => (double)x).ToList();
        }
    }
}
