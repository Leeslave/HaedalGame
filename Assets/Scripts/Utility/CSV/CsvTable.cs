using System;
using System.Collections.Generic;

public sealed class CsvTable
{
    private readonly List<string> _headers;
    private readonly List<CsvRow> _rows;
    private readonly Dictionary<string, int> _headerIndexByName;

    public IReadOnlyList<string> Headers => _headers;
    public IReadOnlyList<CsvRow> Rows => _rows;
    public int RowCount => _rows.Count;

    internal CsvTable(List<string> headers)
    {
        _headers = headers ?? new List<string>();
        _rows = new List<CsvRow>();
        _headerIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _headers.Count; i++)
        {
            string header = _headers[i];

            if (string.IsNullOrWhiteSpace(header))
                continue;

            if (_headerIndexByName.ContainsKey(header))
                continue;

            _headerIndexByName.Add(header, i);
        }
    }

    internal void AddRow(CsvRow row)
    {
        _rows.Add(row);
    }

    public bool HasColumn(string columnName)
    {
        return !string.IsNullOrWhiteSpace(columnName) && _headerIndexByName.ContainsKey(columnName);
    }

    public int GetColumnIndex(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
            return -1;

        return _headerIndexByName.TryGetValue(columnName, out int index) ? index : -1;
    }

    public void RequireColumns(params string[] columnNames)
    {
        if (columnNames == null)
            return;

        for (int i = 0; i < columnNames.Length; i++)
        {
            string columnName = columnNames[i];

            if (!HasColumn(columnName))
                throw new Exception($"CSV column not found: {columnName}");
        }
    }
}
