using System;
using System.Collections.Generic;
using System.Text;

public static class CsvReader
{
    public static CsvTable Read(string csvText, bool hasHeader = true)
    {
        if (string.IsNullOrEmpty(csvText))
            return new CsvTable(new List<string>());

        List<string[]> parsedRows = ParseRows(csvText);

        if (parsedRows.Count == 0)
            return new CsvTable(new List<string>());

        List<string> headers = BuildHeaders(parsedRows, hasHeader);
        CsvTable table = new CsvTable(headers);

        int startRowIndex = hasHeader ? 1 : 0;

        for (int i = startRowIndex; i < parsedRows.Count; i++)
        {
            string[] values = parsedRows[i];

            if (IsEmptyRow(values))
                continue;

            int rowNumber = i + 1;
            table.AddRow(new CsvRow(table, values, rowNumber));
        }

        return table;
    }

    private static List<string> BuildHeaders(List<string[]> parsedRows, bool hasHeader)
    {
        List<string> headers = new List<string>();

        if (parsedRows.Count == 0)
            return headers;

        if (hasHeader)
        {
            string[] headerRow = parsedRows[0];

            for (int i = 0; i < headerRow.Length; i++)
                headers.Add(Clean(headerRow[i]));

            return headers;
        }

        int columnCount = parsedRows[0].Length;

        for (int i = 0; i < columnCount; i++)
            headers.Add($"Column{i}");

        return headers;
    }

    private static List<string[]> ParseRows(string csvText)
    {
        List<string[]> rows = new List<string[]>();
        List<string> currentRow = new List<string>();
        StringBuilder currentCell = new StringBuilder();

        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    bool isEscapedQuote = i + 1 < csvText.Length && csvText[i + 1] == '"';

                    if (isEscapedQuote)
                    {
                        currentCell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentCell.Append(c);
                }

                continue;
            }

            if (c == '"')
            {
                inQuotes = true;
                continue;
            }

            if (c == ',')
            {
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();
                continue;
            }

            if (c == '\r')
                continue;

            if (c == '\n')
            {
                currentRow.Add(currentCell.ToString());
                currentCell.Clear();

                rows.Add(currentRow.ToArray());
                currentRow.Clear();
                continue;
            }

            currentCell.Append(c);
        }

        currentRow.Add(currentCell.ToString());
        rows.Add(currentRow.ToArray());

        return rows;
    }

    private static bool IsEmptyRow(string[] values)
    {
        if (values == null || values.Length == 0)
            return true;

        for (int i = 0; i < values.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(values[i]))
                return false;
        }

        return true;
    }

    internal static string Clean(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Trim().Trim('\uFEFF');
    }
}
