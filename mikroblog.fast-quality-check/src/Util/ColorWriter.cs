// List of colors to use:
// 0 BLACK
// 1 DARKBLUE
// 2 DARKGREEN
// 3 DARKCYAN
// 4 DARKRED
// 5 DARKMAGENTA
// 6 DARKYELLOW - FAILURE
// 7 GRAY
// 8 DARKGRAY 
// 9 BLUE - DISCUSSION
// 10 GREEN
// 11 CYAN
// 12 RED - ERROR
// 13 MAGENTA
// 14 YELLOW - WARNING
// 15 WHITE

namespace mikroblog.fast_quality_check
{
    public class ColorWriter
    {
        // Not a warning or error but something failed and needs to be logged appropriately
        private const ConsoleColor FAILURE_COLOR = ConsoleColor.DarkYellow;
        private const ConsoleColor WARNING_COLOR = ConsoleColor.Yellow;
        private const ConsoleColor ERROR_COLOR = ConsoleColor.Red;
        private const ConsoleColor SUCCESS_COLOR = ConsoleColor.DarkGreen;

        public const ConsoleColor DISCUSSION_COLOR = ConsoleColor.Blue;

        /// <summary>
        /// Writes a text in color.
        /// Text has to properly formatted before passing to the function.
        /// Add @COLOR_NUMBER| before a part of the text to colorize it.
        /// </summary>
        /// <param name="text">Formatted string</param>
        public static void Write(string text)
        {
            List<Tuple<string, int>> coloredStringList = new();
            List<string> stringList;

            if (!text.Contains('@'))
                WriteColor(text, null);
            else
            {
                var a = text.Count(x => x == '@');
                var b = text.Count(x => x == '|');
                if (text.Count(x => x == '@') != text.Count(x => x == '|'))
                {
                    WriteError($"Wrongly formatted color string: {text}");
                    return;
                }

                stringList = text.Split('@').ToList();
                stringList.RemoveAll(x => x == "");

                string colorString;
                string coloredString;

                foreach (var str in stringList)
                {
                    try
                    {
                        colorString = str[..str.IndexOf('|')];
                        var color = Convert.ToInt32(colorString);
                        if (color == ((int)WARNING_COLOR) || color == ((int)ERROR_COLOR))
                        {
                            WriteError($"Usage of not permitted color: {(ConsoleColor)color}");
                            return;
                        }

                        coloredString = str.Substring(str.IndexOf('|') + 1, str.Length - colorString.Length - 1);

                        coloredStringList.Add(new Tuple<string, int>(coloredString, Convert.ToInt32(colorString)));
                    }
                    catch (Exception ex)
                    {
                        WriteError($"Wrongly formatted color string - {text}, Exception: {ex.Message}");
                        return;
                    }
                }

                foreach (var str in coloredStringList)
                {
                    WriteColor(str.Item1, (ConsoleColor)str.Item2);
                }
            }
        }

        /// <summary>
        /// Writes text in a given color and resets the console color afterwards. 
        /// The color can be null to use the default console color.
        /// </summary>
        private static void WriteColor(string text, ConsoleColor? color)
        {
            if (color == null) 
                Console.ResetColor();
            else
                Console.ForegroundColor = (ConsoleColor)color;

            Console.WriteLine(text);

            Console.ResetColor();
        }

        /// <summary>
        /// Writes a failure message in FAILURE_COLOR color.
        /// </summary>
        public static void WriteFailure(string message)
        {
            WriteColor($"FAILURE: {message}", FAILURE_COLOR);
        }

        /// <summary>
        /// Writes a warning message in WARNING_COLOR color.
        /// </summary>
        public static void WriteWarning(string message)
        {
            WriteColor($"WARNING: {message}", WARNING_COLOR);
        }

        /// <summary>
        /// Writes an error message in ERROR_COLOR color.
        /// </summary>
        public static void WriteError(string message)
        {
            WriteColor($"ERROR: {message}", ERROR_COLOR);
        }

        /// <summary>
        /// Writes a success message in SUCCESS_COLOR color.
        /// </summary>
        public static void WriteSuccess(string message)
        {
            WriteColor($"SUCCESS: {message}", SUCCESS_COLOR);
        }

        /// <summary>
        /// Removes all color symbol from the text.
        /// </summary>
        /// <returns>text without color symbols</returns>
        public static string RemoveColorSymbols(string text)
        {
            int indexOfAt = 0;
            int indexOfPipe;

            string textOriginal = text;

            while (indexOfAt != -1)
            {
                indexOfAt = text.IndexOf('@');
                indexOfPipe = text.IndexOf('|');

                if (indexOfAt == -1 || indexOfPipe == -1)
                    break;

                try
                {
                    text = text.Remove(indexOfAt, indexOfPipe - indexOfAt + 1);
                }
                catch (Exception ex)
                {
                    WriteError($"Incorrectly formatted string: {textOriginal}, Exception: {ex.Message}");
                    return textOriginal;
                }
            }

            return text;
        }
    }
}
