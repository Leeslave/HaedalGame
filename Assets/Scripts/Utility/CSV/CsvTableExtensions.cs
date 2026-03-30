using System;
using System.Collections.Generic;

public static class CsvTableExtensions
{
    public static List<T> MapRows<T>(this CsvTable table, Func<CsvRow, T> mapper)
    {
        List<T> result = new List<T>();

        for (int i = 0; i < table.Rows.Count; i++)
            result.Add(mapper(table.Rows[i]));

        return result;
    }

    public static List<T> MapRows<T>(this CsvTable table, Func<CsvRow, int, T> mapper)
    {
        List<T> result = new List<T>();

        for (int i = 0; i < table.Rows.Count; i++)
            result.Add(mapper(table.Rows[i], i));

        return result;
    }
}
