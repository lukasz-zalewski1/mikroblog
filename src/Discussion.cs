using HtmlAgilityPack;

namespace WykopDiscussions
{
    /// <summary>
    /// Contains all properties and entries types.
    /// </summary>
    internal class Discussion
    {
        /// <summary>
        /// Entry in a discussion, abstract base for Post and Comment.
        /// </summary>
        public abstract class DiscussionEntry
        {
            // Number of upvotes for that specific entry
            public int Rating;

            // Entry's author;
            public string Author = string.Empty;

            // We later check if it's female, but we set it to male as default
            public bool IsAuthorMale = true;

            // Html code of that entry
            public string? Html;

            // Text of an entry
            public string? Text;
        }

        /// <summary>
        /// Discussion's post class
        /// </summary>
        public class MainPost : DiscussionEntry
        {
            // Post quality
            public QualityCheck.PostQuality Quality;
        }

        /// <summary>
        /// Discussion's comment class 
        /// </summary>
        public class Comment : DiscussionEntry
        {
            // Comment's unique ID
            public int Id;

            // Comment's quality
            public QualityCheck.CommentQuality Quality;

            // This is used to store comment's node to easily read comment's HTML from it
            public HtmlNode? Node = null;

            // Some comments start with a @nickname tag, meaning it's a reply to comment of that person
            public string ReplyToCommentAuthor = string.Empty;

            // It will be calculated automatically from ReplyToCommentAuthor
            // For regular comments, that are not a reply, value is -1
            // For comments that reply to the main post, value is 0
            // For other comments, value is Id of comment which this comment replies to
            public int ReplyToCommentId;
        }

        // Id of a post
        public int Id;

        // Discussion's quality
        public QualityCheck.DiscussionQuality Quality;

        // Property tells us how old is a post in years
        public int YearsOld;

        // Main post
        public MainPost Post = new();

        // Comments
        public List<Comment> Comments = new();
    }
}
