namespace mikroblog.fast_quality_check
{
    public class DiscussionDownloader
    {
        public const string DISCUSSION_NAME_TEMPLATE = @"https://wykop.pl/wpis/";

        private readonly HttpClient _httpClient = new();

        // Timeout in case of TooManyReqest response
        private const int TIMEOUT_BASE_VALUE = 4000;
        private int _timeoutValue = TIMEOUT_BASE_VALUE;
        // Timeout reduction, if the discussion was succesfully downloaded after single timeout
        private const int TIMEOUT_REDUCTION_STEP = 2000;
        // How many times in a row timeout was called
        private int _timeoutRepetition = 0;

        /// <summary>
        /// Downloads a single discussion.
        /// </summary>
        /// <returns>Html of the discussion</returns>
        public async Task<string?> Download(int discussionId)
        {
            Log.Write("Discussion " + discussionId + " - Download Start");

            HttpResponseMessage response;

            while (true)
            {
                try
                {
                    response = await _httpClient.GetAsync(DISCUSSION_NAME_TEMPLATE + discussionId);
                    break;
                }
                catch (Exception ex)
                {
                    Log.WriteWarning($"Discussion Download - {discussionId}, Exception - {ex.Message}");
                }
            }

            // Reads a html
            if (!response.IsSuccessStatusCode)
            {
                Log.WriteFailure($"Discussion Download - {discussionId}, Response - {response.StatusCode}");

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    HandleTooManyRequest();

                return null;
            }

            _timeoutRepetition = 0;

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Handles TooManyReqeust response by making the thread asleep for _timeoutValue miliseconds.
        /// </summary>
        private void HandleTooManyRequest()
        {
            _timeoutRepetition += 1;

            if (_timeoutRepetition > 1 && _timeoutValue < TIMEOUT_BASE_VALUE)
                _timeoutValue += TIMEOUT_REDUCTION_STEP;

            Thread.Sleep(_timeoutValue);

            if (_timeoutRepetition == 1 && _timeoutValue > 0)
                _timeoutValue -= TIMEOUT_REDUCTION_STEP;
        }
    }
}
