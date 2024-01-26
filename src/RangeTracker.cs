using WykopDiscussions;

class RangeTracker
{
    private List<(int, int)> _ranges = new List<(int, int)>();

    /// <summary>
    /// Add range to range tracker. Merges any ranges inside of given range span.
    /// </summary>
    /// <param name="range">Range of numbers to add to ranges tracker</param>
    public void AddRange((int, int) range)
    {
        LoadRanges();

        // We subtract 1 from end of range, because we don't want to miss any discussion
        range.Item2 -= 1;

        int index = 0;
        // Looks for first range in ranges, which has end smaller than start of given range
        while (index < _ranges.Count && _ranges[index].Item2 < range.Item1 - 1)
            index++;

        // Calculates where to start and end given range
        // Merges middle ranges into one
        int newStart = range.Item1;
        int newEnd = range.Item2;
        while (index < _ranges.Count && _ranges[index].Item1 <= range.Item2 + 1)
        {
            newStart = Math.Min(newStart, _ranges[index].Item1);
            newEnd = Math.Max(newEnd, _ranges[index].Item2);
            _ranges.RemoveAt(index);
        }

        _ranges.Insert(index, (newStart, newEnd));

        SaveRanges();
    }

    /// <summary>
    /// Loads ranges from the file.
    /// </summary>
    private void LoadRanges()
    {
        if (File.Exists(Manager.RangesPath))
        {
            using (var reader = new StreamReader(Manager.RangesPath))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = line.Split(',');
                    var range = (int.Parse(values[0]), int.Parse(values[1]));
                    _ranges.Add(range);
                }
            }
        }
    }

    /// <summary>
    /// Saves tracked ranges to the Ranges file.
    /// </summary>
    private void SaveRanges()
    {
        if (!File.Exists(Manager.RangesPath))
        {
            File.Create(Manager.RangesPath);
        }

        using (var writer = new StreamWriter(Manager.RangesPath))
        {
            foreach (var range in _ranges)
            {
                writer.WriteLine($"{range.Item1},{range.Item2}");
            }
        }
    }

    /// <summary>
    /// Gets ranges in a string format.
    /// </summary>
    /// <returns>Ranges string</returns>
    public override string ToString()
    {
        return string.Join(", ", _ranges);
    }
}