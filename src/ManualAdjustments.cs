using HtmlAgilityPack;

namespace WykopDiscussions
{
    /// <summary>
    /// Used to manually adjust entries.
    /// </summary>
    internal class ManualAdjustments
    { 
        /// <summary>
        /// Reads HTML file and redos screenshot and audio file.
        /// </summary>
        /// <param name="dataFolder">Folder with the data</param>
        /// <param name="entryId">Id of the entry to redo</param>
        public async Task RedoImageAndAudio(string dataFolder, int entryId, bool redoAudio)
        {
            Console.WriteLine(Manager.CreateString(-1, 0, "MANUAL ADJUSTMENT", "Start Redoing Image and Audio"));

            // Makes a screenshot
            HtmlScreenshoter htmlScreenshoter = new();
            htmlScreenshoter.MakeScreenshot(-1, entryId, dataFolder);
            htmlScreenshoter.Dispose();

            if (redoAudio)
            {
                // Generates audio
                TextToSpeech speech = new();
                var properties = LoadEntryProperties(dataFolder, entryId);
                var filePath = dataFolder + "\\" + entryId + ".wav";
                await speech.GenerateAudioFile(-1, entryId, filePath, properties.Item1, properties.Item2);
            }

            Console.WriteLine(Manager.CreateString(-1, 0, "MANUAL ADJUSTMENT", "End Redoing Image and Audio"));
        }

        /// <summary>
        /// Loads text of HTML entry.
        /// </summary>
        /// <param name="dataFolder">Folder with the entry</param>
        /// <param name="entryId">Entry's Id</param>
        /// <returns>Inner text of an entry</returns>
        private (string, bool) LoadEntryProperties(string dataFolder, int entryId)
        {
            var html = File.ReadAllText(dataFolder + "\\" + entryId + ".html");
            HtmlDocument doc = new();
            doc.LoadHtml(html);

            // Get node with the text
            var node = doc.DocumentNode.SelectSingleNode(".//section[@class='entry-content']//div[@class='wrapper']");
            string text = "";

            // If there is any text removes unwanted original commentators and @ signs.
            if (node != null)
            {
                DiscussionPropertiesReader.RemoveBlockQuoteFromText(node);

                DiscussionPropertiesReader.RemoveAdditionalOriginalCommentatorsFromCommentText(node);
                text = DiscussionPropertiesReader.RemoveUnnecessaryAtSigns(node.InnerText);
            }

            // Check author's gender
            var isMale = DiscussionPropertiesReader.IsAuthorMale(doc);

            return (text, isMale);
        }
    }
}
