using HtmlAgilityPack;

namespace mikroblog.fast_quality_check
{
    class DiscussionRatingReader
    {
        private const string POST_RATING_NODE = "//section[@class='entry detailed']//section[@class='rating-box']//li[@class='plus']";
        private const string COMMENT_NODE = "//div[@id='entry-comments']//section[(substring-after(@class, 'entry reply') or @class='entry reply') and not(@class='entry reply deleted')]";
        private const string COMMENT_RATING_NODE = ".//section[@class='rating-box']//li[@class='plus']";

        /// <summary>
        /// Reads rating of discussion post and top comment from discussion html.
        /// </summary>
        /// <param name="html">Discussion html</param>
        /// <returns>Post rating and Top Comment rating</returns>
        public Tuple<int, int> Read(string html)
        {
            Log.Write("Read Ratings");

            HtmlDocument doc = new();
            doc.LoadHtml(html);

            return new Tuple<int, int>(ReadPostRating(doc), ReadCommentsRatings(doc));
        }

        /// <summary>
        /// Reads post rating.
        /// </summary>
        /// <param name="doc">HtmlDocument with discussion html loaded</param>
        /// <returns>Post rating</returns>
        private int ReadPostRating(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode(POST_RATING_NODE);

            if (node == null)
                return 0;

            if (!int.TryParse(node.InnerText, out var rating))
                return 0;

            return rating;
        }

        /// <summary>
        /// Reads comments ratings and gets top comment rating.
        /// </summary>
        /// <param name="doc">HtmlDocument with discussion html loaded</param>
        /// <returns>Top Comment rating</returns>
        private int ReadCommentsRatings(HtmlDocument doc)
        {
            var commentsNodes = doc.DocumentNode.SelectNodes(COMMENT_NODE);

            int topCommentRating = 0;

            if (commentsNodes != null)
            {
                foreach (var commentNode in commentsNodes)
                {
                    var rating = ReadCommentRating(commentNode);
                    if (rating > topCommentRating)
                        topCommentRating = rating;
                }
            }

            return topCommentRating;
        }

        /// <summary>
        /// Reads single comment rating.
        /// </summary>
        /// <param name="doc">HtmlDocument with discussion html loaded</param>
        /// <returns>Comment rating</returns>
        private int ReadCommentRating(HtmlNode commentNode)
        {
            var node = commentNode.SelectSingleNode(COMMENT_RATING_NODE);

            if (node == null) 
                return 0;

            if (!int.TryParse(node.InnerText, out var rating))
                return 0;

            return rating;
        }
    }
}
