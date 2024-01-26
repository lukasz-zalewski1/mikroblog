namespace WykopDiscussions
{
    /// <summary>
    /// Base class for statistics gathering.
    /// </summary>
    internal class Statistics
    {
        // Discussion Properties
        public double YearsOld;
        public double PostRating;
        public double CommentsCount;
        public double CommentRating;
        public List<double> TopCommentsRatings;

        // How many top comments are kept in statistics view
        protected const int _TopCommentsCount = 5;

        /// <summary>
        /// Adds 0.0 to all top comments, to populate the list
        /// </summary>
        public Statistics()
        {
            TopCommentsRatings = new List<double>();

            for (int i = 0; i < _TopCommentsCount; ++i)
            {
                TopCommentsRatings.Add(0.0);
            }
        }

        /// <summary>
        /// Return basic statistics string.
        /// </summary>
        /// <returns>Basic statistics string</returns>
        public virtual string CreateStatisticsString()
        {
            var result = YearsOld + "\n" +
                PostRating + "\n" +
                CommentsCount + "\n" +
                CommentRating + "\n";

            foreach (var rating in TopCommentsRatings)
            {
                result += rating + "\n";
            }

            return result;
        }
    }
}
