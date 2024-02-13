namespace mikroblog.fast_quality_check
{
    public class Log
    {
        private static readonly string LOG_PATH = Path.Combine(Util.WORKPLACE_PATH, "logs", DateTime.Now.ToString("u").Replace(":", " ") + ".txt");

        /// <summary>
        /// Writes a text on the cmd and to the log file.
        /// </summary>
        /// <param name="message">Log message</param>
        public static void Write(string message)
        {
            ColorWriter.Write(message);
            WriteToLogFile(message);
        }

        /// <summary>
        /// Writes a failure text on the cmd and to the log file.
        /// </summary>
        /// <param name="message">Failure message</param>
        public static void WriteFailure(string message)
        {
            ColorWriter.WriteFailure(message);
            WriteToLogFile(message);
        }

        /// <summary>
        /// Writes a warning text on the cmd and to the log file.
        /// Afterwards the functions stops execution of the program until user presses a key.
        /// </summary>
        /// <param name="message">warning message</param>
        public static void WriteWarning(string message)
        {
            ColorWriter.WriteWarning(message);
            WriteToLogFile(message);

            Console.ReadKey(true);
        }

        /// <summary>
        /// Writes an error text on the cmd and to the log file.
        /// Afterwards the functions stops execution of the program until user presses a key.
        /// </summary>
        /// <param name="message">error message</param>
        public static void WriteError(string message)
        {
            ColorWriter.WriteError(message);
            WriteToLogFile(message);

            Console.ReadKey(true);
        }

        /// <summary>
        /// Writes a success text on the cmd and to the log file.
        /// </summary>
        /// <param name="message">success message</param>
        public static void WriteSuccess(string message)
        {
            ColorWriter.WriteSuccess(message);
            WriteToLogFile(message);
        }

        /// <summary>
        /// Writes a text to the log file. Function removes all color symbols from the text before it writes.
        /// </summary>
        /// <param name="text"></param>
        private static void WriteToLogFile(string text)
        {
            try
            {                 
                File.AppendAllText(LOG_PATH, ColorWriter.RemoveColorSymbols(text + "\n"));
            }
            catch (Exception ex)
            {
                ColorWriter.WriteError($"Can't write to the log file - {ex.Message}");
            }
        }
    }
}
