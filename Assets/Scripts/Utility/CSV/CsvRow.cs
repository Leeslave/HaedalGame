using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class CsvRow
{
    private readonly CsvTable _table;
    private readonly string[] _values;

    public int RowNumber { get; }

    internal CsvRow(CsvTable table, string[] values, int rowNumber)
    {
        _table = table;
        _values = values ?? Array.Empty<string>();
        RowNumber = rowNumber;
    }

    public string Get(string columnName, string defaultValue = "")
    {
        int index = _table.GetColumnIndex(columnName);

        if (index < 0 || index >= _values.Length)
            return defaultValue;

        return CsvReader.Clean(_values[index]);
    }

    public bool TryGet(string columnName, out string value)
    {
        int index = _table.GetColumnIndex(columnName);

        if (index < 0 || index >= _values.Length)
        {
            value = string.Empty;
            return false;
        }

        value = CsvReader.Clean(_values[index]);
        return true;
    }

    public int GetInt(string columnName, int defaultValue = 0)
    {
        string raw = Get(columnName);

        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : defaultValue;
    }

    public float GetFloat(string columnName, float defaultValue = 0f)
    {
        string raw = Get(columnName);

        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
            ? value
            : defaultValue;
    }

    public bool GetBool(string columnName, bool defaultValue = false)
    {
        string raw = Get(columnName);

        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        if (bool.TryParse(raw, out bool value))
            return value;

        if (raw == "1")
            return true;

        if (raw == "0")
            return false;

        return defaultValue;
    }

    public TEnum GetEnum<TEnum>(string columnName, TEnum defaultValue = default)
        where TEnum : struct, Enum
    {
        string raw = Get(columnName);

        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        return Enum.TryParse(raw, true, out TEnum value)
            ? value
            : defaultValue;
    }

    public IReadOnlyList<string> GetTokens(string columnName, params char[] separators)
    {
        string raw = Get(columnName);

        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        string[] tokens = raw.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < tokens.Length; i++)
            tokens[i] = CsvReader.Clean(tokens[i]);

        return tokens;
    }

    public List<int> GetIntList(string columnName, params char[] separators)
    {
        IReadOnlyList<string> tokens = GetTokens(columnName, separators);
        List<int> result = new List<int>(tokens.Count);

        for (int i = 0; i < tokens.Count; i++)
        {
            if (int.TryParse(tokens[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                result.Add(value);
        }

        return result;
    }
}
