using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
namespace CsvToSqlite
{
    class Parser
    {
        public String filename;
        public String data;
        public Parser(String filename, String data)
        {
            this.filename = filename;
            this.data = data;
        }

        public static Boolean hasDuplicate(List<String> headers)
        {
            if (headers.Count != headers.Distinct().Count())
            {
                return true;
            }
            return false;
        }
        public Dictionary<String, Object> Parse()
        {
            Dictionary<String, Object> csv = new Dictionary<String, Object>();
            var lines = Regex.Split(this.data, "\r\n|\r|\n");
            List<List<String>> csvData = new List<List<String>>();
            List<String> csvHeaders = new List<String> ();
            foreach (String line in lines)
            {
                List<String> data = ParseLine(line);
                if (!(data.Count == 0))
                {
                    csvData.Add(data);
                }
            }
            foreach (String header in csvData[0])
            {
                csvHeaders.Add(header);
            }
            csvData = csvData.Skip(1).ToList();
            csv.Add("headers", csvHeaders);
            csv.Add("data", csvData);
            return csv;
        }
        public List<String> ParseLine(String line)
        {
            List<String> tokens = new List<String>();
            Boolean escaping = false;
            char quoteChar = ' ';
            Boolean quoting = false;
            int lastCloseQuoteIndex = -2147483648;
            StringBuilder current = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line.ToCharArray()[i];
                if (escaping)
                {
                    current.Append(c);
                    escaping = false;
                }
                else if (c == '\\' && !(quoting && quoteChar == '\''))
                {
                    escaping = true;
                }
                else if (quoting && c == quoteChar)
                {
                    quoting = false;
                    lastCloseQuoteIndex = i;
                }
                else if (!quoting && (c == '\'' || c == '"'))
                {
                    quoting = true;
                    quoteChar = c;
                }
                else if (!quoting && c.ToString().Equals(","))
                {
                    if (current.Length > 0 || lastCloseQuoteIndex == (i - 1))
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
            if (current.Length > 0 || lastCloseQuoteIndex == (line.Length - 1))
            {
                tokens.Add(current.ToString());
            }
            return tokens;
        }

    }
}