using HtmlAgilityPack;
using System.Net;

namespace WykopDiscussions
{
    /// <summary>
    /// Used to download raw HTML files of discussions
    /// </summary>
    internal class DiscussionDownloader
    {
        // Client used to get HTML of discussionss
        private HttpClient _httpClient = new();

        // This constant says how much seconds we wait until next request is run
        private const int _TooManyRequestTimeoutSeconds = 3;

        // This constant says how links to the discussions look like
        private const string _DiscussionNameTemplate = @"https://wykop.pl/wpis/";

        // This list is used during downloading of additional discussions pages
        // Every page contains 50 comments
        // We want to get all comments and thus we need to download all pages
        private List<int> _discussionsWithAdditionalPagesIds = new();

        /// <summary>
        /// Main entry of the DiscussionDownloader class.
        /// It handles downloading one batch of discussions.
        /// </summary>
        /// <param name="discussionsRange">Range of discussions to download</param>
        public async Task StartDownloadingDiscussions((int, int) discussionsRange)
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "DISCUSSION DOWNLOADER", "Start"));

            for (int id = discussionsRange.Item1; id < discussionsRange.Item2; ++id)
            {
                // Downloads a single discussion's first page
                await DownloadAndSaveSingleDiscussion(id, 1);
            }

            // After every first page is downloaded it runs downloading additional pages
            await StartDownloadingAdditionalPages();

            Console.WriteLine(Manager.CreateString(-1, 0, "DISCUSSION DOWNLOADER", "End"));
        }

        /// <summary>
        /// It tries to download a single page of a discussion.
        /// </summary>
        /// <param name="discussionId">Id of a discussion</param>
        /// <param name="page">Page of a discussion to download</param>
        private async Task DownloadAndSaveSingleDiscussion(int discussionId, int page)
        {
            Console.WriteLine(Manager.CreateString(discussionId, page, "DISCUSSION DOWNLOADER", "Start Download"));

            // Tries to GET HTML of a discussion's page
            HttpResponseMessage response;

            // Tries to downloads a single discussion
            while (true)
            {
                try
                {
                    // Tries to get raw HTML of a page
                    response = await _httpClient.GetAsync(_DiscussionNameTemplate + discussionId + "/strona/" + page);

                    // Logs status code for discussionId and a page
                    Console.WriteLine(Manager.CreateString(discussionId, page, "DISCUSSION DOWNLOADER", "Response Code - " + response.StatusCode.ToString()));

                    // Delay, in case of response - TOO_MANY_REQUESTS
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        await Task.Delay(_TooManyRequestTimeoutSeconds * 1000);
                    }
                    else
                    {
                        break;
                    }
                }
                // In case of any exception, reset HttpClient and delay next requests
                catch (Exception ex)
                {
                    Console.WriteLine(Manager.CreateString(discussionId, page, "DISCUSSION DOWNLOADER", "Exception - " + ex.Message));

                    _httpClient = new HttpClient();
                    await Task.Delay(_TooManyRequestTimeoutSeconds * 1000);
                }
            }

            // Reads a html
            string html = await response.Content.ReadAsStringAsync();

            // If the response was success, then save discussion page into a file
            if (response.IsSuccessStatusCode)
            {
                await SaveHtmlIntoFile(discussionId, page, html);

                // Check on first page if there are additional pages
                if (page == 1)
                {
                    if (!IsDiscussionMultiplePages(discussionId, html))
                    {
                        return;
                    }
                }

                // If the discussion has this specific page. Then it may contain additional page
                // Adds the ID it to _discussionsWithAdditionalPagesIds to run the same task on the next page
                // If the request wasn't succesfull, then it means that the page doesn't exist
                _discussionsWithAdditionalPagesIds.Add(discussionId);
            }

            Console.WriteLine(Manager.CreateString(discussionId, page, "DISCUSSION DOWNLOADER", "End Download"));
        }

        /// <summary>
        /// Saves HTML of a discussion's page into a file
        /// </summary>
        /// <param name="discussionId">Id of the discussion</param>
        /// <param name="page">Discussion's page</param>
        /// <param name="html">HTML code of the discussion's page</param>
        /// <returns></returns>
        private async Task SaveHtmlIntoFile(int discussionId, int page, string html)
        {
            Console.WriteLine(Manager.CreateString(discussionId, page, "DISCUSSION DOWNLOADER", "Start Save Raw HTML"));

            // Creates a filePath
            // Filename ends with _page.html, where page is a page number
            string filePath = Path.Combine(Manager.DiscussionsDirectory, discussionId.ToString());

            Directory.CreateDirectory(filePath);

            filePath += Manager.CreateString(discussionId, page, "", "", true);
            filePath = Path.ChangeExtension(filePath, "html");

            // Writes the discussion HTML
            await File.WriteAllTextAsync(filePath, html);

            Console.WriteLine(Manager.CreateString(discussionId, page, "DISCUSSION DOWNLOADER", "End Save Raw HTML"));
        }

        /// <summary>
        /// Checks if discussion has more than one page, based on Html string of first page.
        /// </summary>
        /// <param name="discussionId">Discussion's Id for logging purposes</param>
        /// <param name="htmlFirstPage">Html of the first page</param>
        /// <returns>True if multipage discussion</returns>
        private bool IsDiscussionMultiplePages(int discussionId, string htmlFirstPage)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION DOWNLOADER", "Start Check MultiPage"));

            HtmlDocument docFirstPage = new();
            docFirstPage.LoadHtml(htmlFirstPage);

            var pagination = docFirstPage.DocumentNode.SelectSingleNode("//nav[@class='new-pagination number']");

            Console.WriteLine(Manager.CreateString(discussionId, 0, "DISCUSSION DOWNLOADER", "End Check MultiPage"));

            if (pagination != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Method downloads every page (besides first one) of every discussion in a batch.
        /// </summary>
        private async Task StartDownloadingAdditionalPages()
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "DISCUSSION DOWNLOADER", "Start Downloading Additional Pages"));

            // Starts from page 2, because page 1 is handled in StartDownloadingDiscussions method
            int page = 2;

            // Creates local discussionsWithAdditionalPagesIds list and copies all entries from object list
            // This list is used to loop over every discussion
            // The object list is used to keep information, which discussions may have additional pages
            List<int> discussionsWithAdditionalPagesIds = new(_discussionsWithAdditionalPagesIds);

            // If there are any discussions with possible additional pages
            while (discussionsWithAdditionalPagesIds.Count > 0)
            {
                // Clear the object list
                _discussionsWithAdditionalPagesIds = new List<int>();

                // Loop over every discussion that has possible additional pages and try to download them
                for (int i = 0; i < discussionsWithAdditionalPagesIds.Count; ++i)
                {
                    // Tries to download single discussion with a specified page
                    await DownloadAndSaveSingleDiscussion(discussionsWithAdditionalPagesIds[i], page);
                }

                // Again, It copies all entries from object list to the local one 
                // Increases page
                discussionsWithAdditionalPagesIds = new List<int>(_discussionsWithAdditionalPagesIds);
                ++page;
            }

            Console.WriteLine(Manager.CreateString(-1, 0, "DISCUSSION DOWNLOADER", "End Downloading Additional Pages"));
        }
    }
}
