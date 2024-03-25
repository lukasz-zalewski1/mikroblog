using System.Threading.Tasks;

using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

using mikroblog.fast_quality_check;

namespace mikroblog.videos_designer
{
    internal class TextToSpeech
    {
        private readonly Config _configTextToSpeechApiKeys = new("TextToSpeechApi");

        private const string FEMALE = "pl-PL-ZofiaNeural";
        private const string MALE = "pl-PL-MarekNeural";

        private readonly SpeechConfig? _speechConfig = null;

        /// <summary>
        /// Initializes <see cref="_speechConfig"/> by reading "key" and "region" values from TextToSpeech config file.
        /// </summary>
        public TextToSpeech()
        {
            var apiKey = _configTextToSpeechApiKeys.GetString("key");
            var region = _configTextToSpeechApiKeys.GetString("region");

            if (apiKey == null || region == null)
                return;

            _speechConfig = SpeechConfig.FromSubscription(apiKey, region);
        }

        /// <summary>
        /// Generates text to speech audio file.
        /// </summary>
        /// <param name="filePath">Where to save audio file</param>
        /// <param name="text">Text to read</param>
        /// <param name="isMale">Voice gender</param>
        /// <returns>Length of the audio file or -1 when <see cref="_speechConfig"/> was not initialized correctly.</returns>
        public async Task<double> GenerateAudioFile(string filePath, string text, bool isMale)
        {
            if (_speechConfig == null)
                return -1;

            SetVoiceGender(isMale);

            var audioConfig = AudioConfig.FromWavFileOutput(filePath);
            var speechSynthesizer = new SpeechSynthesizer(_speechConfig, audioConfig);

            var result = await speechSynthesizer.SpeakTextAsync(text);

            return result.AudioDuration.TotalSeconds;
        }

        /// <summary>
        /// Sets voice gender depending on <paramref name="isMale"/>.
        /// </summary>
        private void SetVoiceGender(bool isMale)
        {
            if (_speechConfig == null)
                return;

            if (isMale)
                _speechConfig.SpeechSynthesisVoiceName = MALE;
            else
                _speechConfig.SpeechSynthesisVoiceName = FEMALE;
        }
    }
}
