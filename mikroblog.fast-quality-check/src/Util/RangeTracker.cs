namespace mikroblog.fast_quality_check
{
    class RangeTracker
    {
        private static readonly string RANGES_FILE_PATH = Path.Combine(Util.GetWorkplacePath(), "Ranges.txt");

        private const char RANGE_SEPARATOR = ',';

        private static readonly List<(int, int)> _ranges = new();

        /// <summary>
        /// Loads existing ranges file. Adds new range into it and saves it.
        /// </summary>
        public static void Add((int, int) range)
        {
            if (!Load())
                return;

            int index = 0;
            // Looks for first range in ranges, which has end smaller than start of given range
            while (index < _ranges.Count && _ranges[index].Item2 < range.Item1)
                index++;

            // Calculates where to start and end given range
            // Merges middle ranges into one
            int newStart = range.Item1;
            int newEnd = range.Item2;
            while (index < _ranges.Count && _ranges[index].Item1 <= range.Item2)
            {
                newStart = Math.Min(newStart, _ranges[index].Item1);
                newEnd = Math.Max(newEnd, _ranges[index].Item2);
                _ranges.RemoveAt(index);
            }

            _ranges.Insert(index, (newStart, newEnd));

            Save();
        }

        /// <summary>
        /// Loads ranges from the ranges file.
        /// </summary>
        /// <returns>True if success or file didn't exist</returns>
        private static bool Load()
        {
            _ranges.Clear();

            if (!File.Exists(RANGES_FILE_PATH))
                return true;

            try
            {
                var lines = File.ReadAllLines(RANGES_FILE_PATH);

                foreach (var line in lines)
                {
                    if (!ReadSingleRangeFromLine(line))
                        return false;
                }
            }
            catch (Exception ex)
            {
                Log.WriteError($"Can't read RangeTracker file - {RANGES_FILE_PATH}, Exception - {ex.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Decodes range from a line of text.
        /// </summary>
        /// <returns>True if succesfully decoded</returns>
        private static bool ReadSingleRangeFromLine(string line)
        {
            var splitIndex = line.IndexOf(RANGE_SEPARATOR);

            if (splitIndex < 0)
            {
                Log.WriteError($"RangeTracker can't load the file - incorrect data at line - {line}");
                return false;
            }

            try
            {
                string start = line[..splitIndex];
                string end = line[(splitIndex + 1)..];

                if (!int.TryParse(start, out int startValue) || !int.TryParse(end, out int endValue))
                {
                    Log.WriteError($"RangeTracker invalid value at line {line}");
                    return false;
                }

                if (startValue >= endValue)
                {
                    Log.WriteError($"RangeTracker invalid range {startValue} {endValue}");
                    return false;
                }

                _ranges.Add((startValue, endValue));
            }
            catch (Exception ex)
            {
                Log.WriteError($"RangeTracker can't load the file - incorrect data at line - {line}, Exception - {ex.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves ranges into the ranges file.
        /// </summary>
        private static void Save()
        {
            List<string> lines = new();

            foreach (var range in _ranges)
            {
                lines.Add(range.Item1.ToString() + RANGE_SEPARATOR + range.Item2.ToString());
            }

            try
            {
                File.WriteAllLines(RANGES_FILE_PATH, lines.ToArray());
            }
            catch (Exception ex)
            {
                Log.WriteError($"RangeTracker Save, Exception - {ex.Message}");
            }
        }
    }
}