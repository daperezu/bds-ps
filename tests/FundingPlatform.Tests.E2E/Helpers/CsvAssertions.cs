using System.Globalization;
using System.Text;

namespace FundingPlatform.Tests.E2E.Helpers;

public static class CsvAssertions
{
    public static IReadOnlyList<IReadOnlyDictionary<string, string>> ParseCsv(byte[] body)
    {
        var bytes = body;
        // Strip UTF-8 BOM if present.
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            bytes = bytes[3..];
        }
        var text = Encoding.UTF8.GetString(bytes);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return Array.Empty<IReadOnlyDictionary<string, string>>();

        var headers = ParseLine(lines[0]);
        var rows = new List<IReadOnlyDictionary<string, string>>(lines.Length - 1);
        for (var i = 1; i < lines.Length; i++)
        {
            var cells = ParseLine(lines[i]);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count && c < cells.Count; c++)
            {
                dict[headers[c]] = cells[c];
            }
            rows.Add(dict);
        }
        return rows;
    }

    public static void AssertHeaderEquals(byte[] body, params string[] expected)
    {
        var bytes = body;
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            bytes = bytes[3..];
        }
        var text = Encoding.UTF8.GetString(bytes);
        var firstLine = text.Split('\n', 2)[0];
        var headers = ParseLine(firstLine);
        Assert.That(headers, Is.EqualTo(expected.ToList()));
    }

    public static void AssertEveryRowHasNonEmptyCurrencyColumn(IReadOnlyList<IReadOnlyDictionary<string, string>> rows)
    {
        foreach (var row in rows)
        {
            row.TryGetValue("Currency", out var currency);
            Assert.That(currency, Is.Not.Null.And.Not.Empty,
                "Every CSV row must carry a non-empty Currency column.");
        }
    }

    private static IReadOnlyList<string> ParseLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }
            else
            {
                if (ch == ',')
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else if (ch == '"')
                {
                    inQuotes = true;
                }
                else if (ch == '\r')
                {
                    // ignore
                }
                else
                {
                    sb.Append(ch);
                }
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
