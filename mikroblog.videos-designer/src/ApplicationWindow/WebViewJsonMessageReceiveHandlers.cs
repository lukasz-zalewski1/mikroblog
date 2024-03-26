using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading;
using System.Windows;

using mikroblog.fast_quality_check;

namespace mikroblog.videos_designer
{
    public partial class VideosDesignerWindow : Window
    {
        /// <summary>
        /// There has to be a delay between getting screenshot data, taking screenshot and restoring <see cref="_webView"/> page old state, to ensure screenshot comes out correctly.
        /// </summary>
        private const int SCREENSHOT_DELAY = 100;

        private readonly TextToSpeech _speechService = new();

        /// <summary>
        /// Tries to deserialize <paramref name="jsonMessage"/> into JsonObject and parses it.
        /// </summary>
        /// <param name="jsonMessage">Json string</param>
        private void ParseJsonMessage(string jsonMessage)
        {
            JsonObject? json;
            try
            {
                json = JsonSerializer.Deserialize<JsonObject>(jsonMessage);

                if (json == null)
                    return;
            }
            catch (Exception ex)
            {
                Log.WriteError($"Error when deserializing json, Exception - {ex.Message}");
                return;
            }

            if (json["message"] == null)
            {
                Log.WriteError("Incorrect json message");
                return;
            }

#pragma warning disable CS8602 // Editor doesn't recognize checking for null value earlier in the code.
            switch (json["message"].ToString())
            {
                case "EntriesCount":
                    JsonMessageEntriesCount(json);
                    break;
                case "ScreenshotData":
                    JsonMessageScreenshotData(json);
                    break;
                case "SpeechData":
                    JsonMessageSpeechData(json);
                    break;
            }
#pragma warning restore CS8602
        }

        /// <summary>
        /// Populates <see cref="_listboxEntries"/> with numbers from 1 to entries count.
        /// </summary>
        /// <param name="json">JsonObject received from <see cref="_webView"/></param>
        private void JsonMessageEntriesCount(JsonObject json)
        {
            var valueJson = json["value"];

            if (valueJson == null)
            {
                Log.WriteError($"Json {json["message"]} missing parameter");
                return;
            }

            if (!JS.TryGetIntFromJsonNode(valueJson, out int value))
            {
                Log.WriteError($"Json parameter value is not an int");
                return;
            }

            _listboxEntries.Items.Clear();
            for (int i = 0; i < value; ++i)
            {
                _listboxEntries.Items.Add((i + 1).ToString());
            }
        }

        /// <summary>
        /// Calls <see cref="ValidateScreenshotData"/> and <see cref="TakeScreenshot"/> methods.
        /// </summary>
        /// <param name="json">JsonObject received from <see cref="_webView"/></param>
        private void JsonMessageScreenshotData(JsonObject json)
        {
            if (!ValidateScreenshotData(json, out Rectangle rect, out int entryNumber))
                return;

            TakeScreenshot(entryNumber, CalculateActualScreenshotRectangle(rect));
        }

        /// <summary>
        /// Validates if data in <paramref name="json"/> is correct and populates <paramref name="rect"/> and <paramref name="entryNumber"/> parameters.
        /// </summary>
        /// <param name="json">JsonObject received from <see cref="_webView"/></param>
        /// <param name="rect">Coordinates and size of the screen's area to do a screenshot of.</param>
        /// <param name="entryNumber">Number of entry to do a screenshot of</param>
        /// <returns>True if data is valid, otherwise false.</returns>
        private bool ValidateScreenshotData(JsonObject json, out Rectangle rect, out int entryNumber)
        {
            rect = new();
            entryNumber = -1;

            if (json["entryNumber"] == null || json["x"] == null || json["y"] == null || json["width"] == null || json["height"] == null)
            {
                Log.WriteError("Invalid screenshot data");
                return false;
            }

            if (!JS.TryGetIntFromJsonNode(json["entryNumber"], out entryNumber) ||
                !JS.TryGetIntFromJsonNode(json["x"], out int x) ||
                !JS.TryGetIntFromJsonNode(json["y"], out int y) ||
                !JS.TryGetIntFromJsonNode(json["width"], out int width) ||
                !JS.TryGetIntFromJsonNode(json["height"], out int height)
                )
            {
                Log.WriteError("Invalid screenshot data");
                return false;
            }

            rect = new(x, y, width, height);

            return true;
        }

        /// <summary>
        /// Calculates and returns actual rectangle of screen's area which the screenshot will be taken.
        /// To calculate the actual values it takes dpi scaling and <see cref="_webView"/> position into account.
        /// </summary>
        private Rectangle CalculateActualScreenshotRectangle(Rectangle rect)
        {
            double displayScaling = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice.M11;

            rect.X = (int)(rect.X * displayScaling);
            rect.X += (int)_webView.Margin.Left;
            rect.Y = (int)(rect.Y * displayScaling) + (int)_webView.Margin.Top;
            rect.Width = (int)(rect.Width * displayScaling);
            rect.Height = (int)(rect.Height * displayScaling);

            return rect;
        }

        /// <summary>
        /// Calls <see cref="GetCanvasWithScreenshotDrawnInsideOfRoundedRect"/> and saves prepared bitmap to a file.
        /// </summary>
        private void TakeScreenshot(int entryNumber, Rectangle rect)
        {
            Thread.Sleep(SCREENSHOT_DELAY);

            try
            {
                string path = Path.ChangeExtension(Path.Combine(GetCurrentDiscussionFolder(), (entryNumber + 1).ToString()), ".png");
                var bitmapFinalImage = GetCanvasWithScreenshotDrawnInsideOfRoundedRect(rect);

                if (bitmapFinalImage == null)
                    return;

                bitmapFinalImage.Save(path, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                Log.WriteError($"Couldn't save a screenshot {ex.Message}");
            }

            UpdateControls(ControlUpdateType.Screenshot);
        }

        /// <summary>
        /// Calls <see cref="ValidateSpeechData"/>. If valid calls <see cref="TextToSpeech.GenerateAudioFile"/> and saves speech length to a file.
        /// </summary>
        /// <param name="json">JsonObject received from <see cref="_webView"/></param>
        private async void JsonMessageSpeechData(JsonObject json)
        {
            ValidateSpeechData(json, out int entryNumber, out string text, out bool isMale);

            string path = Path.ChangeExtension(Path.Combine(GetCurrentDiscussionFolder(), (entryNumber + 1).ToString()), ".wav");
            var speechLength = await _speechService.GenerateAudioFile(path, PrepareTextForSpeech(text), isMale);

            SaveSpeechLengthToFile(entryNumber, speechLength);

            UpdateControls(ControlUpdateType.Speech);
        }

        /// <summary>
        /// Validates if data in <paramref name="json"/> is correct and populates <paramref name="entryNumber"/>, <paramref name="text"/> and <paramref name="isMale"/> parameters.
        /// </summary>
        /// <param name="json">JsonObject received from <see cref="_webView"/></param>
        /// <param name="entryNumber">Number of entry to do a speech of</param>
        /// <param name="text">Text to speak</param>
        /// <param name="isMale">Text author gender</param>
        /// <returns>True if data is valid, otherwise false.</returns>
        private bool ValidateSpeechData(JsonObject json, out int entryNumber, out string text, out bool isMale)
        {
            text = string.Empty;
            entryNumber = -1;
            isMale = true;

            if (json["entryNumber"] == null || json["text"] == null || json["isMale"] == null)
            {
                Log.WriteError("Invalid speech data");
                return false;
            }

            if (!JS.TryGetIntFromJsonNode(json["entryNumber"], out entryNumber) ||
                !JS.TryGetBoolFromJsonNode(json["isMale"], out isMale))
            {
                Log.WriteError("Invalid speech data");
                return false;
            }

#pragma warning disable CS8602 // Editor doesn't recognize checking for null values earlier in the code.
            text = json["text"].ToString();
#pragma warning restore CS8602

            return true;
        }

        /// <summary>
        /// Removes reply to references from text so it's not read by <see cref="TextToSpeech"/> service.
        /// </summary>
        /// <returns>Prepared text</returns>
        private string PrepareTextForSpeech(string text)
        {
            if (!text.Contains('@'))
                return text;

            while (text[0] == '\n')
            {
                text = text[(text.IndexOf('\n') + 1)..];
            }

            while (text[0] == '@')
            {
                text = text[(text.IndexOf(' ') + 1)..];
            }

            return text;
        }

        /// <summary>
        /// Saves speech length to a file.
        /// </summary>
        private void SaveSpeechLengthToFile(int entryNumber, double speechLength)
        {
            string path = Path.ChangeExtension(Path.Combine(GetCurrentDiscussionFolder(), (entryNumber + 1).ToString()), ".txt");

            try
            {
                File.WriteAllText(path, speechLength.ToString());
            }
            catch (Exception ex)
            {
                Log.WriteError($"Can't create speech length file - {ex.Message}");
            }
        }
    }
}
