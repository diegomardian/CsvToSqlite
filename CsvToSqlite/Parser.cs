using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CsvToSqlite
{
    internal class Parser
    {
        public string data;
        public string filename;

        public Parser(string filename, string data)
        {
            this.filename = filename;
            this.data = data;
        }

        public static bool hasDuplicate(List<string> headers)
        {
            if (headers.Count != headers.Distinct().Count()) return true;
            return false;
        }

        public Dictionary<string, object> Parse()
        {
            var csv = new Dictionary<string, object>();
            var lines = Regex.Split(this.data, "\r\n|\r|\n");
            var csvData = new List<List<string>>();
            var csvHeaders = new List<string>();
            foreach (var line in lines)
            {
                var data = ParseLine(line);
                if (!(data.Count == 0))
                    if (!(data.Distinct().Count() == 1 && data.Distinct().ToList()[0].Equals("")))
                        csvData.Add(data);
            }

            foreach (var header in csvData[0]) csvHeaders.Add(header);
            csvData = csvData.Skip(1).ToList();
            csv.Add("headers", csvHeaders);
            csv.Add("data", csvData);
            return csv;
        }

        public List<string> ParseLine(string line)
        {
            var tokens = new List<string>();
            var escaping = false;
            var quoteChar = ' ';
            var quoting = false;
            var lastCloseQuoteIndex = int.MinValue;
            var current = new StringBuilder();
            for (var i = 0; i < line.Length; i++)
            {
                var c = line.ToCharArray()[i];
                if (escaping)
                {
                    current.Append(c);
                    escaping = false;
                }
                else if (c == '\\' && !(quoting && quoteChar == '"'))
                {
                    escaping = true;
                }
                else if (quoting && c == quoteChar)
                {
                    quoting = false;
                    lastCloseQuoteIndex = i;
                }
                else if (!quoting && c == '"')
                {
                    quoting = true;
                    quoteChar = c;
                }
                else if (!quoting && c.ToString().Equals(","))
                {
                    if (current.Length >= 0 || lastCloseQuoteIndex == i - 1)
                    {
                        tokens.Add(current.ToString());
                        current = new StringBuilder();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length >= 0 || lastCloseQuoteIndex == line.Length - 1) tokens.Add(current.ToString());
            return tokens;
        }
    }
}