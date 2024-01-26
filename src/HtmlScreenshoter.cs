using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;

namespace WykopDiscussions
{
    /// <summary>
    /// Used to make screenshots of local HTML files, displayed in Firefox.
    /// </summary>
    internal class HtmlScreenshoter : IDisposable
    {
        private readonly FirefoxOptions _options = new();
        private readonly IWebDriver _driver;

        private const int _MaxLoadPageSeconds = 15;

        /// <summary>
        /// Sets firefox parameters.
        /// </summary>
        public HtmlScreenshoter()
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "HTML Screenshoter", "Initialize Firefox"));

            // Parameters used to make Firefox display pages in the TikTok format in invisible browser
            _options.AddArgument("--headless");
            _options.AddArgument("--width=1080");
            _options.AddArgument("--height=1920");
            _options.AddArgument("--window-size=1080,1920");

            _driver = new FirefoxDriver(_options);
        }

        /// <summary>
        /// Closes Firefox.
        /// </summary>
        public void Dispose()
        {
            _driver.Quit();
        }

        /// <summary>
        /// Makes screenshot of an HTML file and saves it.
        /// </summary>
        /// <param name="discussionId">For logs</param>
        /// <param name="entryId">Id of an entry</param>
        /// <param name="path">Path to entries</param>
        public void MakeScreenshot(int discussionId, int entryId, string path)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "HTML Screenshoter", "Start Making Screenshot"));

            // Define path to HTML and Screenshot
            string pathHtml = Path.Combine(path, entryId.ToString());
            pathHtml = Path.ChangeExtension(pathHtml, "html");
            string pathScreenshot = Path.Combine(path, entryId.ToString());
            pathScreenshot = Path.ChangeExtension(pathScreenshot, "png");

            try
            {
                // Loads HTML file as a tab in Firefox
                _driver.Url = new Uri(pathHtml).AbsoluteUri;

                // Define wait for _MaxLoadPageSeconds seconds until all images on a page are displayed
                var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(_MaxLoadPageSeconds));
                var areImagesLoaded = wait.Until(driver =>
                {
                    // Checks if all img elements are loaded for _MaxLoadPageSeconds seconds
                    var images = driver.FindElements(By.TagName("img"));
                    foreach (var image in images)
                    {
                        if (!image.Displayed)
                        {
                            return false;
                        }
                    }
                    return true;
                });

                // Does a screeshot of the loaded page
                Screenshot screenshot = ((ITakesScreenshot)_driver).GetScreenshot();
                screenshot.SaveAsFile(pathScreenshot);
            }
            catch (Exception ex)
            {
                // If page didn't load in time or other exception
                Console.WriteLine(Manager.CreateString(discussionId, 0, "HTML Screenshoter", "End Making Screenshot Failed " + entryId + " - " + ex.Message));
            }

            Console.WriteLine(Manager.CreateString(discussionId, 0, "HTML Screenshoter", "End Making Screenshot"));
        }
    }
}
