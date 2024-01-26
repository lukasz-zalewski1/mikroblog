using HtmlAgilityPack;
using System.Globalization;

namespace WykopDiscussions
{
    /// <summary>
    /// This class reads discussions and gathers all discussion properties.
    /// </summary>
    internal class DiscussionPropertiesReader
    {
        // Date format of discussions on Wykop.pl
        // Used to calculate how old is a discussion
        private const string _DateTimeFormat = "dd.MM.yyyy, HH:mm:ss";

        /// <summary>
        /// Entry point of the class. Loads all HTMLs of discussion in a range.
        /// Calls other functions to read properties of every discussion and later deletes original HTMLs.
        /// </summary>
        /// <param name="discussionsRange">Range of discussions to read</param>
        public async Task<List<Discussion>> StartReadingDiscussions((int, int) discussionsRange)
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "DISCUSSION PROPERTIES READER", "Start"));

            var discussions = new List<Discussion>();

            for (int id = discussionsRange.Item1; id < discussionsRange.Item2; ++id)
            {             
                var discussion = new Discussion { Id = id };

                // Loads discussion pages HTMLs
                var pages = await LoadDiscussionPages(id);

                // If there are any pages (discussion exists on our drive)
                // Reads discussion's properties from pages and then removes pages HTMLs
                if (pages != null)
                {
                    // It will fail, when post is +18
                    if (!ReadDiscussionProperties(discussion, pages))
                        continue;

                    discussions.Add(discussion);
                }
            }

            // After all properties are read, we can remove HTML files
            RemoveDiscussionsHtmlFiles(discussionsRange);

            Console.WriteLine(Manager.CreateString(-1, 0, "DISCUSSION PROPERTIES READER", "End"));

            return discussions;
        }

        /// <summary>
        /// Tries to load all discussion pages HTMLs, for a given discussionId.
        /// </summary>
        /// <param name="discussionId">Id of a discussion</param>
        /// <returns>List of HTMLs strings or null</returns>
        private async Task<List<string>?> LoadDiscussionPages(int discussionId)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Loading Pages"));

            var pages = new List<string>();

            int page = 1;
            string? html;

            while (true)
            {
                // Loads single page HTML
                html = await LoadDiscussionPage(discussionId, page);

                // If HTML could be loaded, then try to load another page
                if (html != null)
                {
                    pages.Add(html);
                }
                else
                {
                    // If it couldn't load any page
                    if (page == 1)
                    {
                        pages = null;
                    }

                    break;
                }

                page += 1;
            }

            Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Loading Pages"));

            return pages;
        }
        
        /// <summary>
        /// Tries to load single discussion page's HTML.
        /// </summary>
        /// <param name="discussionId">Id of a discussion</param>
        /// <param name="page">Page to laod</param>
        /// <returns>HTML of that page or null</returns>
        private async Task<string?> LoadDiscussionPage(int discussionId, int page)
        {
            Console.WriteLine(Manager.CreateString(discussionId, page, "DISCUSSION PROPERTIES READER", "Start Load Page"));

            // Create path to a discussion page
            var path = Path.Combine(Manager.DiscussionsDirectory, discussionId.ToString());
            path += Manager.CreateString(discussionId, page, "", "", true);
            path = Path.ChangeExtension(path, "html");

            // Tries to load text from HTML file
            string html;
            try
            {
                html = await File.ReadAllTextAsync(path);
            }
            catch
            {
                Console.WriteLine(Manager.CreateString(discussionId, page, "DISCUSSION PROPERTIES READER", "End Load Page - Not Found"));
                return null;
            }

            Console.WriteLine(Manager.CreateString(discussionId, page, "DISCUSSION PROPERTIES READER", "End Load Page"));
            return html;
        }

        /// <summary>
        /// Reads properties of a single discussion.
        /// </summary>
        /// <param name="discussion">Initialized only with Id</param>
        /// <param name="pages">List of discussion pages HTMLs</param>
        private bool ReadDiscussionProperties(Discussion discussion, List<string> pages)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Properties"));

            // First page is the page, from which post and some other properties are read
            HtmlDocument docFirstPage = new HtmlDocument();
            docFirstPage.LoadHtml(pages[0]);

            // Calculate discussion's age
            if (!CalculateDiscussionAge(discussion, docFirstPage))
            {
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Reading Properties Failed +18"));
                return false;
            }
            // Reads post, based on first page. It can return false, when the post is +18 or contains video
            if (!ReadPostProperties(discussion, docFirstPage))
            {
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Reading Properties Failed +18 or Video"));
                return false;
            }

            // Read discussion comments
            ReadCommentsProperties(discussion, pages);
            ReadCommentsHtmls(discussion);
            ReadCommentsAuthorsGenders(discussion);
            ReadCommentsTexts(discussion);

            // We can easily check to what author a comment responds to, but we can't check to which comment
            // This method calculates it
            CalculateCommentsReplyToCommentId(discussion);

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Properties"));

            return true;
        }

        /// <summary>
        /// Calculate age of a discussion.
        /// </summary>
        /// <param name="discussion"></param>
        /// <param name="docFirstPage">Document with first HTML page of a discussion loaded</param>
        private bool CalculateDiscussionAge(Discussion discussion, HtmlDocument docFirstPage)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Calculating Discussion Age"));

            int yearsOld;

            var date = DateTime.Now;

            // Checks for specific nodes
            var section = docFirstPage.DocumentNode.SelectSingleNode("//section[@class='entry detailed']");

            if (section != null)
            {
                if (section.SelectSingleNode(".//section[@class='adult-placeholder entry']") != null)
                {
                    return false;
                }

                var time = docFirstPage.DocumentNode.SelectSingleNode(".//time[@class='date']");

                if (time != null)
                {
                    // Parses the date into a given format
                    try
                    {
                        date = DateTime.ParseExact(time.InnerText, _DateTimeFormat, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        date = DateTime.Now;
                    }
                }
            }

            // Calculates how old is the discussion, min. 1 year to not divide by 0
            yearsOld = Convert.ToInt32((DateTime.Now - date).Days / 365);
            if (yearsOld == 0)
            {
                yearsOld = 1;
            }

            discussion.YearsOld = yearsOld;

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Calculating Discussion Age"));

            return true;
        }

        /// <summary>
        /// Reads post's properties.
        /// </summary>
        /// <param name="discussion"></param>
        /// <param name="docFirstPage">Document with first HTML page of a discussion loaded</param>
        /// <returns>Returns false, if discussion is +18</returns>
        private bool ReadPostProperties(Discussion discussion, HtmlDocument docFirstPage)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Read Post Properties"));

            // If discussion is +18, it won't find an author, so returns false
            if (!ReadPostAuthor(discussion, docFirstPage))
            {
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Read Post Properties Failed - +18"));
                return false;
            }
            // Reads post's rating
            ReadPostRating(discussion, docFirstPage);
            // Reads post HTML from raw HTML
            if (!ReadPostHtml(discussion, docFirstPage))
            {
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Read Post Properties Failed - Video"));
                return false;
            }
            // Read post author gender
            ReadPostAuthorGender(discussion);
            // Read post text
            ReadPostText(discussion);

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Read Post Properties"));
            return true;
        }

        /// <summary>
        /// Reads post's author.
        /// </summary>
        /// <param name="discussion"></param>
        /// <param name="docFirstPage">Document with first HTML page of a discussion loaded</param>
        /// <returns>Returns false, if discussion is +18</returns>
        private bool ReadPostAuthor(Discussion discussion, HtmlDocument docFirstPage)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Post Author"));

            // Checks for the specific node
            var nodeAuthor = docFirstPage.DocumentNode.SelectSingleNode(".//section[@class='entry detailed']//div[@class='left']//span");

            // If it's not there, then the post is +18 and thus we can't read it
            if (nodeAuthor != null)
            { 
                discussion.Post.Author = nodeAuthor.InnerText;
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Post Author"));
                return true;
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Post Author Failed - +18"));
            return false;
        }

        /// <summary>
        /// Reads post's rating
        /// </summary>
        /// <param name="discussion"></param>
        /// <param name="docFirstPage">Document with first HTML page of a discussion loaded</param>
        private void ReadPostRating(Discussion discussion, HtmlDocument docFirstPage)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Post Rating"));

            // Checks for specific nodes
            var section = docFirstPage.DocumentNode.SelectSingleNode("//section[@class='entry detailed']");

            if (section != null)
            {
                var section2 = section.SelectSingleNode(".//section[@class='rating-box']");

                if (section2 != null)
                {
                    var li = section2.SelectSingleNode(".//li[@class='plus']");

                    // If the node is there, convert rating string to int
                    if (li != null)
                    {
                        discussion.Post.Rating = Convert.ToInt32(li.InnerText);

                        Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Post Rating - " + discussion.Post.Rating));
                    }
                }
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Post Rating"));
        }

        /// <summary>
        /// Reads post HTML from firstPage doc and removes unwanted nodes.
        /// </summary>
        /// <param name="discussion"></param>
        /// <param name="docFirstPage">Document with first HTML page of a discussion loaded</param>
        private bool ReadPostHtml(Discussion discussion, HtmlDocument docFirstPage)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Post Html"));

            // Commented out because it doesn't work apparently
            //if (DoesPostContainsVideo(discussion.Id, docFirstPage))
            //{
            //    Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Reading Post Rating Failed - Video"));
            //    return false;
            //}

            // Removes unwanted nodes and prepares HTML for TikTok video format
            RemoveUnwantedNodesFromPostHtml(discussion, docFirstPage);
            PrepareHtmlForTikTokFormat(discussion.Id, docFirstPage, true);

            // Save post HTML into discussion object
            discussion.Post.Html = docFirstPage.DocumentNode.OuterHtml;

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Post Rating"));

            return true;
        }

        /// <summary>
        /// Checks if post contains video.
        /// </summary>
        /// <param name="discussionId">Id of a discussion, for logging purposes</param>
        /// <param name="docFirstPage">HTML of discussion's first page</param>
        /// <returns>True if contains</returns>
        private bool DoesPostContainsVideo(int discussionId, HtmlDocument docFirstPage)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Post Contains Video Check"));

            var section = docFirstPage.DocumentNode.SelectSingleNode("//section[@class='embed streamable']");

            if (section != null)
            {
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Post Contains Video Check Failed - Contains Video"));

                return true;
            }

            Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Post Contains Video Check"));
            return false;
        }

        /// <summary>
        /// Removes unneeded nodes to leave only the post on a HTML page.
        /// </summary>
        /// <param name="discussion"></param>
        /// <param name="docFirstPage">Document with first HTML page of a discussion loaded</param>
        private void RemoveUnwantedNodesFromPostHtml(Discussion discussion, HtmlDocument docFirstPage)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Removal Unwanted Nodes From Post HTML"));

            // Delete all nodes beside the post one
            var nodeSidebar = docFirstPage.DocumentNode.SelectSingleNode("//section[@class='sidebar']");
            var nodeLeftPanel = docFirstPage.DocumentNode.SelectSingleNode("//aside[substring-after(@class, 'left-panel') or @class='left-panel']");
            var nodeStream = docFirstPage.DocumentNode.SelectSingleNode("//header[@class='stream-top']");
            var nodeHeader = docFirstPage.DocumentNode.SelectSingleNode("//header[@class='header']");

            RemoveNode(ref nodeSidebar);
            RemoveNode(ref nodeLeftPanel);
            RemoveNode(ref nodeStream);
            RemoveNode(ref nodeHeader);

            // If there are comments, remove them
            var nodeComments = docFirstPage.DocumentNode.SelectSingleNode("//div[@id='entry-comments']");
            if (nodeComments != null)
            {
                nodeComments.Remove();
            }

            // Remove Ad section
            var nodeAd = docFirstPage.DocumentNode.SelectSingleNode("//section[@class='pub-slot-wrapper top']");
            if (nodeAd != null)
            {
                nodeAd.Remove();
            }

            // In some discussions there is more than 1 page
            // Check if is pagination node and delete it
            var nodePagination = "//nav[@class='new-pagination number']";
            if (docFirstPage.DocumentNode.SelectSingleNode(nodePagination) != null)
            {
                docFirstPage.DocumentNode.SelectSingleNode(nodePagination).Remove();
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Removal Unwanted Nodes From Post HTML"));
        }

        /// <summary>
        /// Used to remove nodes, with null check
        /// </summary>
        /// <param name="node">Node to remove</param>
        private void RemoveNode(ref HtmlNode node)
        {
            if (node != null)
            {
                node.Remove();
            }
        }

        /// <summary>
        /// Styles elements so it looks good on TikTok Video.
        /// </summary>
        /// <param name="discussionId">Id of a discussion for logging purposes</param>
        /// <param name="docPage">Html Document</param>
        private void PrepareHtmlForTikTokFormat(int discussionId, HtmlDocument docPage, bool isPost)
        {
            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Preparation For TikTok Video Format"));

            var nodeMain = docPage.DocumentNode.SelectSingleNode("//main");
            var nodeDivContent = docPage.DocumentNode.SelectSingleNode("//div[@class='content']");
            var nodeSectionEntryContent = docPage.DocumentNode.SelectSingleNode("//section[@class='entry-content']");
            var nodeEntryPhoto = docPage.DocumentNode.SelectSingleNode("//section[@class='entry-photo']//figure");

            // Makes entry appear in the middle of the screen and full width
            if (nodeMain != null)
            {
                nodeMain.SetAttributeValue("style", "max-width:100%; width:100%; display:flex; align-items:center; justify-content:center;");
            }
            if (nodeDivContent!= null)
            {
                nodeDivContent.SetAttributeValue("style", "max-width:100%; width:100%;");
            }
            // Increase font size and line height
            if (nodeSectionEntryContent != null)
            {
                nodeSectionEntryContent.SetAttributeValue("style", "font-size:32px; line-height:44px;");
            }
            // Increase entry photo size
            if (nodeEntryPhoto != null)
            {
                nodeEntryPhoto.SetAttributeValue("style", "max-width: 100%; width: 100%");
            }

            if (isPost)
            {
                var nodeEntryDetailed = docPage.DocumentNode.SelectSingleNode("//section[substring-after(@class, 'entry detailed') or @class='entry detailed']");

                if (nodeEntryDetailed != null)
                    nodeEntryDetailed.SetAttributeValue("style", "margin:48px;");
            }

            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Preparation For TikTok Video Format"));
        }

        /// <summary>
        /// Reads post's author's gender
        /// </summary>
        private void ReadPostAuthorGender(Discussion discussion)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Post Author Gender"));

            var docPost = new HtmlDocument();
            docPost.LoadHtml(discussion.Post.Html);

            // If it contains such node, it means the author is female
            var nodeFemale = docPost.DocumentNode.SelectSingleNode(".//div[@class='content']//div[@class='left']//figure[substring-after(@class, 'female')]");

            if (nodeFemale != null)
            {
                discussion.Post.IsAuthorMale = false;
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Post Author Gender"));
        }

        /// <summary>
        /// Reads post text from its HTML.
        /// </summary>
        private void ReadPostText(Discussion discussion)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Post Text"));

            var docPost = new HtmlDocument();
            docPost.LoadHtml(discussion.Post.Html);

            // Get node with text
            var node = docPost.DocumentNode.SelectSingleNode(".//section[@class='entry-content']//div[@class='wrapper']");

            if (node != null)
            {
                discussion.Post.Text = node.InnerText;
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Post Text"));
        }

        /// <summary>
        /// Reads discussion comments.
        /// </summary>
        /// <param name="pages">List of discussion pages strings</param>
        private void ReadCommentsProperties(Discussion discussion, List<string> pages)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Comments Properties"));

            foreach (var page in pages)
            {
                var docPage = new HtmlDocument();
                docPage.LoadHtml(page);

                // Selects specific nodes, in which comment's HTML are kept
                var commentsNodes = docPage.DocumentNode.SelectNodes("//div[@id='entry-comments']//section[(substring-after(@class, 'entry reply') or @class='entry reply') and not(@class='entry reply deleted')]");

                // Reads every comment
                if (commentsNodes != null)
                {
                    foreach (var commentNode in commentsNodes)
                    {
                        ReadCommentProperties(discussion, commentNode, page);
                    }
                }
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Comments Properties"));
        }

        /// <summary>
        /// Reads single comment properties from a HtmlNode.
        /// </summary>
        /// <param name="commentNode">HtmlNode with a single comment node selected</param>
        /// <param name="pageHtml">Html string of a page, where a comments is posted</param>
        private void ReadCommentProperties(Discussion discussion, HtmlNode commentNode, string pageHtml)
        {
            if(Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Single Comment Properties"));

            Discussion.Comment comment = new Discussion.Comment();

            // Reads comment's properties one by one
            ReadCommentId(discussion.Id, comment, commentNode);
            // If author can't be found it means that comments is marked as +18
            if (!ReadCommentAuthor(discussion.Id, comment, commentNode))
            {
                return;
            }
            ReadCommentReplyToAuthor(discussion.Id, comment, commentNode);
            ReadCommentRating(discussion.Id, comment, commentNode);     
            ReadCommentPreHtml(discussion.Id, comment, commentNode, pageHtml);

            discussion.Comments.Add(comment);

            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment Properties"));
        }

        /// <summary>
        /// Reads comment's Id from a HtmlNode.
        /// </summary>
        /// <param name="discussionId">Id of a discussion, for loggin purposes</param>
        /// <param name="commentNode">HtmlNode with a single comment node selected</param>
        private void ReadCommentId(int discussionId, Discussion.Comment comment, HtmlNode commentNode)
        {
            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Reading Single Comment Id"));

            comment.Id = Convert.ToInt32(commentNode.Id.Replace("comment-", ""));

            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment Id"));
        }

        /// <summary>
        /// Reads comment's author.
        /// </summary>
        /// <param name="discussionId">Id of a discussion, for logging purposes</param>
        /// <param name="comment"></param>
        /// <param name="commentNode">HtmlNode with a single comment node selected</param>
        /// <returns>False when comments is 18+</returns>
        private bool ReadCommentAuthor(int discussionId, Discussion.Comment comment, HtmlNode commentNode)
        {
            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Reading Single Comment Author"));

            // Checks single node
            var node = commentNode.SelectSingleNode(".//div[@class='left']//span");

            if (node != null)
            {
                comment.Author = node.InnerText;
                if (Manager.MaxLog)
                    Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment Author"));
                return true;
            }
            // It happens when comment is marked as 18+
            else
            {
                if (Manager.MaxLog)
                    Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment Author Failed - +18"));
                return false;
            }       
        }

        /// <summary>
        /// Reads comment's inner text and checks if it's a reply to another comment. If so, reads author of original comment.
        /// </summary>
        /// <param name="discussionId">Id of a discussion, for logging purposes</param>
        /// <param name="comment"></param>
        /// <param name="commentNode">HtmlNode with a single comment node selected</param>
        private void ReadCommentReplyToAuthor(int discussionId, Discussion.Comment comment, HtmlNode commentNode)
        {
            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Reading Single Comment ReplyToAuthor"));

            // Checks single node
            var nodeCommentContent = commentNode.SelectSingleNode(".//section[@class='entry-content']//div[@class='wrapper']");

            if (nodeCommentContent != null)
            {
                // If comment's inner text contains <a> tag, then it gets what's inside it.
                // It's possible than comment replies to multiple authors. 
                // It's not yet implemented and it just checks the first one and selects it as author of original comment.
                var nodeReplyToAuthor = nodeCommentContent.SelectSingleNode(".//a");

                if (nodeReplyToAuthor != null)
                {
                    comment.ReplyToCommentAuthor = nodeReplyToAuthor.InnerText;
                }
            }

            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment ReplyToAuthor"));
        }

        /// <summary>
        /// Reads comment's rating.
        /// </summary>
        /// <param name="discussionId">Id o a discussion, for logging purposes</param>
        /// <param name="comment"></param>
        /// <param name="commentNode">HtmlNode with a single comment node selected</param>
        private void ReadCommentRating(int discussionId, Discussion.Comment comment, HtmlNode commentNode)
        {
            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Reading Single Comment Rating"));

            // Checks single node
            var nodeRating = commentNode.SelectSingleNode(".//section[@class='rating-box']//li[@class='plus']");

            if (nodeRating != null)
            {
                comment.Rating = Convert.ToInt32(nodeRating.InnerText);
            }
            else
            {
                comment.Rating = 0;
            }

            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment Rating"));
        }

        /// <summary>
        /// Sets comment's HTML to HTML string of a page, to later parse it and saves comment's node.
        /// </summary>
        /// <param name="discussionId">Id o a discussion, for logging purposes</param>
        /// <param name="commentNode">HtmlNode with a single comment node selected</param>
        /// <param name="page">HTML string of a page, where comment is posted</param>
        private void ReadCommentPreHtml(int discussionId, Discussion.Comment comment, HtmlNode commentNode, string page)
        {
            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Reading Single Comment PreHtml"));

            comment.Html = page;
            comment.Node = commentNode;

            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment PreHtml"));
        }

        /// <summary>
        /// Reads HTML of all comments in a discussion.
        /// </summary>
        /// <param name="discussion"></param>
        private void ReadCommentsHtmls(Discussion discussion)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Comments HTMLs"));

            // We assume that discussion's comments already have PreHtml in Html variable
            foreach (var comment in discussion.Comments)
            {
                ReadCommentHtml(discussion.Id, comment);
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Comments HTMLs"));
        }

        /// <summary>
        /// Reads HTML of a single comment.
        /// Comment's HTML now must contain HTML of an entire page.
        /// </summary>
        /// <param name="discussionId">Id o a discussion, for logging purposes</param>
        /// <param name="comment"></param>
        private void ReadCommentHtml(int discussionId, Discussion.Comment comment)
        {
            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Reading Single Comment HTML"));

            HtmlDocument docPage = new HtmlDocument();
            docPage.LoadHtml(comment.Html);

            RemoveUnwantedNodesFromCommentHtml(discussionId, comment, docPage);
            PrepareHtmlForTikTokFormat(discussionId, docPage, false);

            comment.Html = docPage.DocumentNode.OuterHtml;

            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment HTML"));
        }

        /// <summary>
        /// /// This method cuts unneeded nodes and leaves only comment node.
        /// </summary>
        /// <param name="discussionId">Id o a discussion, for logging purposes</param>
        /// <param name="comment">Comment where to remove unwanted nods</param>
        /// <param name="docPage">Doc of a page with a comment</param>
        void RemoveUnwantedNodesFromCommentHtml(int discussionId, Discussion.Comment comment, HtmlDocument docPage)
        {
            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Removing Unwanted Nodes From Single Comment"));

            // Delete all unneeded nodes
            var nodeArticle = docPage.DocumentNode.SelectSingleNode("//section[@class='entry detailed']//article");
            var nodeSidebar = docPage.DocumentNode.SelectSingleNode("//section[@class='sidebar']");
            var nodeLeftPanel = docPage.DocumentNode.SelectSingleNode("//aside[substring-after(@class, 'left-panel') or @class='left-panel']");
            var nodeStreamTop = docPage.DocumentNode.SelectSingleNode("//header[@class='stream-top']");
            var nodeheader = docPage.DocumentNode.SelectSingleNode("//header[@class='header']");

            RemoveNode(ref nodeArticle);
            RemoveNode(ref nodeSidebar);
            RemoveNode(ref nodeLeftPanel);
            RemoveNode(ref nodeStreamTop);
            RemoveNode(ref nodeheader);

            // Remove Ad section
            var nodesAds = docPage.DocumentNode.SelectNodes("//section[substring-after(@class, 'pub-slot-wrapper')]");
            if (nodesAds != null)
            {
                foreach (var node in nodesAds)
                {
                    node.Remove();
                }
            }

            // In some discussions there is more than 1 page
            // Check if is pagination node and delete it
            var nodePagination = "//nav[@class='new-pagination number']";
            if (docPage.DocumentNode.SelectSingleNode(nodePagination) != null)
            {
                docPage.DocumentNode.SelectSingleNode(nodePagination).Remove();
            }

            // Select all comments on the page
            var commentsNodes = docPage.DocumentNode.SelectNodes("//div[@id='entry-comments']//section[substring-after(@class, 'entry reply') or @class='entry reply']");

            // Removes all comments besides the one from the comment we parse
            foreach (var node in commentsNodes)
            {
                if (comment.Node != null)
                {
                    if (node.Id != comment.Node.Id)
                    {
                        node.Remove();
                    }
                }
            }

            if (Manager.MaxLog)
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Removing Unwanted Nodes From Single Comment"));
        }

        /// <summary>
        /// Reads all comments texts from HTMLs of these comment.
        /// </summary>
        private void ReadCommentsTexts(Discussion discussion)
        {
            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Comments Texts"));

            foreach (var comment in discussion.Comments)
            {
                ReadCommentText(discussion.Id, comment);
            }

            Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Comments Texts"));
        }

        /// <summary>
        /// Reads single comment text from HTML of this comment.
        /// </summary>
        /// <param name="discussionId">Id of a discussion, for logging purposes</param>
        /// <param name="comment">Comment to get text from</param>
        private void ReadCommentText(int discussionId, Discussion.Comment comment)
        {
            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Reading Single Comment Text"));

            var doc = new HtmlDocument();
            doc.LoadHtml(comment.Html);

            // Select node with the comment's text
            var node = doc.DocumentNode.SelectSingleNode("//section[substring-after(@class, 'entry')]//div[@class='wrapper']");

            if (node != null)
            {
                RemoveBlockQuoteFromText(node);

                RemoveAdditionalOriginalCommentatorsFromCommentText(node);
                comment.Text = RemoveUnnecessaryAtSigns(node.InnerText);
            }

            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment Text"));
        }

        /// <summary>
        /// Removes blockquotes from text.
        /// </summary>
        /// <param name="node">node to search for blockquote</param>
        public static void RemoveBlockQuoteFromText(HtmlNode node)
        {
            var quote = node.SelectSingleNode(".//blockquote");
            
            if (quote != null)
            {
                quote.Remove();
            }
        }

        /// <summary>
        /// Removes any link to additional original commentators in node's inner text.
        /// </summary>
        /// <param name="node">Node to remove commentators from</param>
        public static void RemoveAdditionalOriginalCommentatorsFromCommentText(HtmlNode node)
        {
            var commentators = node.SelectNodes(".//a");

            if (commentators == null) return;

            for (int i = 0; i < commentators.Count; i++)
            {
                commentators[i].Remove();
            }
        }
        
        /// <summary>
        /// Removes any @ sign which are leftovers after deleting additional original commentators from a node.
        /// </summary>
        /// <param name="text">Text to parse</param>
        /// <returns>Parsed text</returns>
        public static string RemoveUnnecessaryAtSigns(string text)
        {
            return text.Replace("@", "");
        }

        /// <summary>
        /// Reads genders of author of all comments in a discussion.
        /// </summary>
        /// <param name="discussion">Discussion to run Read Comments Author Genders on</param>
        private void ReadCommentsAuthorsGenders(Discussion discussion)
        {
            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Reading Comments Author Genders"));

            foreach (var comment in discussion.Comments)
            {
                ReadCommentAuthorGender(discussion.Id, comment);
            }

            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Reading Comments Author Genders"));
        }

        /// <summary>
        /// Reads comment's author's gender
        /// </summary>
        /// <param name="comment">Comment to run Read Comment Author Gender on</param>
        private void ReadCommentAuthorGender(int discussionId, Discussion.Comment comment)
        {
            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "Start Reading Single Comment Author Gender"));

            var docPost = new HtmlDocument();
            docPost.LoadHtml(comment.Html);

            // Checks if it's female
            var nodeFemale = docPost.DocumentNode.SelectSingleNode("//div[@class='left']//figure[substring-after(@class, 'female')]");

            if (nodeFemale != null)
            {
                comment.IsAuthorMale = false;
            }

            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION PROPERTIES READER", "End Reading Single Comment Author Gender"));
        }

        /// <summary>
        /// Checks if author of an entry is female, used by other classes
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static bool IsAuthorMale(HtmlDocument doc)
        {
            // Checks if it's female
            var nodeFemale = doc.DocumentNode.SelectSingleNode("//div[@class='left']//figure[substring-after(@class, 'female')]");

            if (nodeFemale != null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates to which comments Ids all other comments reply to.
        /// </summary>
        /// <param name="discussion">Discussion to Calculate Comment ReplyToCommentId on</param>
        private void CalculateCommentsReplyToCommentId(Discussion discussion)
        {
            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Calculating Comments ReplyToCommentId"));

            for (int i = 0; i < discussion.Comments.Count; i++) 
            { 
                CalculateCommentReplyToCommentId(discussion, i);
            }

            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Calculating Comments ReplyToCommentId"));
        }

        /// <summary>
        /// Calculates to which comment, other comment reply to, based on ReplyToCommentAuthor property.
        /// </summary>
        /// <param name="discussion">Discussion to run Calculate Comment Reply To Comment Id on</param>
        /// <param name="index">Index of a comment</param>
        private void CalculateCommentReplyToCommentId(Discussion discussion, int index)
        {
            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "Start Calculating Single Comment ReplyToCommentId"));

            bool authorFound = false;

            Discussion.Comment comment = discussion.Comments[index];
            Discussion.Comment commentToCheck;

            // If our comment has non-empty ReplyToCommentAuthor property
            if (!string.IsNullOrEmpty(comment.ReplyToCommentAuthor))
            {
                // Checks all comments below our comment until comment 0
                // If author of commentToCheck is the same as our comment's ReplyToCommentAuthor
                // Then it gets Id of commentToCheck and sets our comment's ReplyToCommentId to this id
                for (int indexToCheck = index - 1; indexToCheck >= 0; indexToCheck--)
                {
                    commentToCheck = discussion.Comments[indexToCheck];
                    
                    if (comment.ReplyToCommentAuthor == commentToCheck.Author)
                    {
                        comment.ReplyToCommentId = commentToCheck.Id;

                        authorFound = true;
                        break;
                    }
                }

                // If we couldn't find original comment
                // Then it means, that our comment replies to author of a post or original comment's author has deleted account
                if (!authorFound)
                {
                    if (comment.ReplyToCommentAuthor == discussion.Post.Author)
                    {
                        comment.ReplyToCommentId = 0;
                    }
                    else
                    {
                        comment.ReplyToCommentId = -2;
                    }
                }
            }
            // If property ReplyToCommentAuthor is empty, then comment doesn't reply to other comment, so we set Id to -1
            else
            {
                discussion.Comments[index].ReplyToCommentId = -1;
            }

            if (Manager.MaxLog) 
                Console.WriteLine(Manager.CreateString(discussion.Id, 0, "DISCUSSION PROPERTIES READER", "End Calculating Single Comment ReplyToCommentId"));
        }     

        /// <summary>
        /// Removes all basic HTMLs of all discussions within a range.
        /// </summary>
        /// <param name="range">Range of discussions IDs</param>
        private void RemoveDiscussionsHtmlFiles((int, int) range)
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "DISCUSSION PROPERTIES READER", "Start Removing Raw HTML Files " + range.Item1 + " - " + range.Item2));

            for (int i = range.Item1; i <= range.Item2; i++)
            {
                var discussionFolder = Path.Combine(Manager.DiscussionsDirectory, i.ToString());

                if (Directory.Exists(discussionFolder))
                {
                    Directory.Delete(discussionFolder, true);
                }

                Console.WriteLine(Manager.CreateString(i, 0, "DISCUSSION PROPERTIES READER", "Removing Raw HTML Files"));
            }

            Console.WriteLine(Manager.CreateString(-1, 0, "DISCUSSION PROPERTIES READER", "End Removing Raw HTML Files " + range.Item1 + " - " + range.Item2));
        }
    }
}
