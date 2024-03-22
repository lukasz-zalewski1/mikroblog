namespace mikroblog.fast_quality_check
{
    public class Util
    {
        private const string WORKPLACE_CONFIG_FILE = "workplace.txt";

        /// <summary>
        /// Tries to read workplace path from <see cref="WORKPLACE_CONFIG_FILE"/> file.
        /// </summary>
        /// <returns>Workplace path or empty string</returns>
        public static string GetWorkplacePath()
        {
            if (!File.Exists(WORKPLACE_CONFIG_FILE))
                return string.Empty;

            try
            {
                var path = File.ReadAllText(WORKPLACE_CONFIG_FILE);

                return Directory.Exists(path) ? path : string.Empty; 
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
