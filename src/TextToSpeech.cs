using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace WykopDiscussions
{
    /// <summary>
    /// Used to generate audio text-to-speech files.
    /// </summary>
    internal class TextToSpeech
    {
        // EndPoint and ApiKey used to login into SpeechServices
        private readonly Uri _EndPoint = new Uri("https://westeurope.api.cognitive.microsoft.com/sts/v1.0/issuetoken");
        private readonly string _ApiKey = "";

        private SpeechConfig _speechConfig;

        // Voices names for male and female
        private const string _FemaleVoice = "pl-PL-ZofiaNeural";
        private const string _MaleVoice = "pl-PL-MarekNeural";

        // Default text
        private const string _PlaceholderText = "Placeholder";

        /// <summary>
        /// Creates config from EndPoint and ApiKey constants
        /// </summary>
        public TextToSpeech()
        {
            _speechConfig = SpeechConfig.FromEndpoint(_EndPoint, _ApiKey);
        }

        /// <summary>
        /// Generates audio file, from a text.
        /// </summary>
        /// <param name="discussionId">Id of a discussion, for logging purposes</param>
        /// <param name="entryId">Id of an entry, for logging purposes</param>
        /// <param name="filePath">Where to save the file</param>
        /// <param name="text">Text to speak</param>
        /// <param name="isMale">Whether to speak with male or female voice</param>
        public async Task<double> GenerateAudioFile(int discussionId, int entryId, string filePath, string text, bool isMale)
        {
            Console.WriteLine(Manager.CreateString(discussionId, 0, "TEXT-TO-SPEECH", "Start Generating Audio " + entryId));

            if (string.IsNullOrEmpty(text) || text == ":" || text == ": ")
                text = _PlaceholderText;

            SetVoiceGender(isMale);
            
            // Creates synthesizer with speechConfig and voice, where gender is defined by isMale variable
            var audioConfig = AudioConfig.FromWavFileOutput(filePath);
            var speechSynthesizer = new SpeechSynthesizer(_speechConfig, audioConfig);

            var result = await speechSynthesizer.SpeakTextAsync(text);        

            Console.WriteLine(Manager.CreateString(discussionId, 0, "TEXT-TO-SPEECH", "End Generating Audio " + entryId));

            return result.AudioDuration.TotalSeconds;
        }

        /// <summary>
        /// Depending on isMale, sets voice gender.
        /// </summary>
        /// <param name="isMale">Voice gender</param>
        private void SetVoiceGender(bool isMale)
        {
            if (isMale)
            {
                _speechConfig.SpeechSynthesisVoiceName = _MaleVoice;
            }
            else
            {
                _speechConfig.SpeechSynthesisVoiceName = _FemaleVoice;
            }
        }
    }
}
