using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CsvToSqlite
{
    public partial class CsvToSqlite : ServiceBase
    {
        private string databaseFile;
        private readonly EventLog eventLog;
        private string homeDirectory;
        private string loggingDirectory;
        public string loggingLevel;
        private Dictionary<string, object> parserConfig;
        private string parserConfigFile;
        private bool stopOnError;
        private string tableName;
        private string watchDirectory;
        private FileSystemWatcher watcher;

        public CsvToSqlite()
        {
            eventLog = new EventLog();
            eventLog.Source = "CsvToSqlite";
            loggingLevel = "advanced";
            InitializeComponent();
        }

        private void ShowErrors(Exception err, string message)
        {
            LogToFile(message);
            if (!logBasic())
                LogToFile("Error Message:\n" + err);
        }

        private void CreateDirectory(string dir, string message)
        {
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (IOException err)
            {
                ShowErrors(err, message);
                Stop();
            }
            catch (UnauthorizedAccessException err)
            {
                LogToFile(message);
            }
        }

        private void CheckKey(string key)
        {
            if (ConfigurationManager.AppSettings.Get(key) is null)
            {
                LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find key '" + key + "'. Quiting ...");
                Stop();
            }
        }

        protected override void OnStart(string[] args)
        {
            loggingDirectory = "";
            CheckKey("watchdirectory");
            CheckKey("parserconfigfile");
            CheckKey("logdirectory");
            CheckKey("tablename");
            CheckKey("logginglevel");
            CheckKey("databasepath");


            if (ConfigurationManager.AppSettings.Get("watchdirectory").Equals(""))
            {
                LogToFile(
                    "INFO: Config file paramater 'watchdirectory' not set. Setting to C:\\Users\\diego\\CsvToSqlite\\Convert");
                watchDirectory = "C:\\Users\\diego\\CsvToSqlite\\Convert";
                if (!Directory.Exists(watchDirectory))
                    CreateDirectory(watchDirectory,
                        DateTime.Now + " CRITICAL ERROR: An error occurred while creating the directory " +
                        watchDirectory + ". Try checking the permissions of " + watchDirectory +
                        " and make sure that Local System has been granted access.");
            }
            else
            {
                watchDirectory = ConfigurationManager.AppSettings.Get("watchdirectory");
                if (!Directory.Exists(watchDirectory))
                    CreateDirectory(watchDirectory,
                        DateTime.Now + " CRITICAL ERROR: An error occurred while creating the directory " +
                        watchDirectory + ". Try checking the permissions of " + watchDirectory +
                        " and make sure that Local System has been granted access.");
                ConfigurationManager.AppSettings.Set("watchdirectory", watchDirectory);
            }

            if (ConfigurationManager.AppSettings.Get("parserconfigfile").Equals(""))
            {
                LogToFile(DateTime.Now +
                          " CRITICAL ERROR: Config file paramater 'parserconfigfile' not set. Quiting ...");
                Stop();
            }
            else
            {
                parserConfigFile = ConfigurationManager.AppSettings.Get("parserconfigfile");
                if (!File.Exists(parserConfigFile))
                {
                    LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find file " + parserConfigFile +
                              ". Quiting ...");
                    Stop();
                }
            }

            if (ConfigurationManager.AppSettings.Get("databasepath").Equals(""))
            {
                databaseFile = "Data Source = C:\\Users\\diego\\CsvToSqlite\\CsvToSqlite.db;";
                if (!File.Exists("C:\\Users\\diego\\CsvToSqlite\\CsvToSqlite.db"))
                {
                    LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find database path: " + databaseFile);
                    Stop();
                }
            }
            else
            {
                if (!File.Exists(ConfigurationManager.AppSettings.Get("databasepath")))
                {
                    LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find database file " +
                              ConfigurationManager.AppSettings.Get("databasepath") + ".");
                    Stop();
                }

                databaseFile = "Data Source = " + ConfigurationManager.AppSettings.Get("databasepath") + ";";
            }

            if (ConfigurationManager.AppSettings.Get("tablename").Equals(""))
            {
                LogToFile("INFO: Config file paramater 'tablename' not set. Setting to CsvToSqlite");
                tableName = "CsvToSqlite";
                try
                {
                    using (var c = new SQLiteConnection(databaseFile))
                    {
                        c.Open();
                        var isTable = false;
                        var command = new SQLiteCommand(c);
                        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = '" +
                                              tableName + "';";
                        var reader = command.ExecuteReader();

                        while (reader.Read())
                            if (reader[0].Equals(tableName))
                                isTable = true;

                        reader.Close();
                        reader.Dispose();
                        command.Dispose();
                        if (!isTable)
                            LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find table " + tableName +
                                      " in database " + databaseFile);
                        c.Close();
                    }
                }
                catch (SQLiteException err)
                {
                    ShowErrors(err,
                        DateTime.Now +
                        " CRITICAL ERROR: An unexpected error occured while checking the if the table '" + tableName +
                        "' exists. Quiting ...");
                    Stop();
                }
            }
            else
            {
                try
                {
                    tableName = ConfigurationManager.AppSettings.Get("tableName");
                    using (var c = new SQLiteConnection(databaseFile))
                    {
                        c.Open();
                        var isTable = false;
                        var command = new SQLiteCommand(c);
                        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = '" +
                                              tableName + "';";
                        var reader = command.ExecuteReader();

                        while (reader.Read())
                            if (reader[0].Equals(tableName))
                                isTable = true;

                        reader.Close();
                        reader.Dispose();
                        command.Dispose();
                        if (!isTable)
                            LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find table " + tableName +
                                      " in database " + databaseFile);
                        c.Close();
                    }
                }
                catch (SQLiteException err)
                {
                    ShowErrors(err,
                        DateTime.Now +
                        " CRITICAL ERROR: An unexpected error occured while checking the if the table '" + tableName +
                        "' exists. Quiting ...");
                    Stop();
                }
            }

            if (!ConfigurationManager.AppSettings.Get("logginglevel").Equals("basic") &&
                !ConfigurationManager.AppSettings.Get("logginglevel").Equals("advanced"))
            {
                LogToFile(DateTime.Now +
                          " ERROR: Could not parse App.config key \'logginglevel\', value must be set to \'basic\' or \'advanced\'. Setting logginglevel to \'basic\'");
                loggingLevel = "basic";
            }
            else
            {
                if (ConfigurationManager.AppSettings.Get("logginglevel").Equals("basic"))
                    loggingLevel = "basic";
                else if (ConfigurationManager.AppSettings.Get("logginglevel").Equals("advanced"))
                    loggingLevel = "advanced";
            }

            try
            {
                var jsonText = File.ReadAllText(parserConfigFile);
                parserConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonText);
            }
            catch (IOException err)
            {
                ShowErrors(err,
                    DateTime.Now + " CRITICAL ERROR: Could not read " + parserConfigFile +
                    ". Please make sure the file exists and Local System has access to it. Quiting ...");
                Stop();
            }
            catch (JsonException err)
            {
                ShowErrors(err,
                    DateTime.Now + " CRITICAL ERROR: An error occured while parsing " + parserConfigFile +
                    ". Please provide valid JSON. Quiting ...");
                Stop();
            }
            catch (UnauthorizedAccessException err)
            {
                ShowErrors(err,
                    DateTime.Now + " CRITICAL ERROR: Local System does not have permissions to access " +
                    parserConfigFile +
                    ". Please make sure the file exists and Local System has access to it. Quiting ...");
            }

            if (!parserConfig.ContainsKey("columns"))
            {
                LogToFile(DateTime.Now +
                          " CRITICAL ERROR: Please include a 'columns' key in your parser config file. Quiting...");
                Stop();
            }

            if (!parserConfig.ContainsKey("stopOnError"))
            {
                LogToFile(DateTime.Now +
                          " ERROR: Could not find the key 'stopOnError' in your parser config file. Setting to 'True'.");
                stopOnError = true;
            }
            else
            {
                if (!(parserConfig["stopOnError"].ToString().Equals("False") ||
                      parserConfig["stopOnError"].ToString().Equals("True")))
                {
                    LogToFile(DateTime.Now +
                              " ERROR: Parser config file 'stopOnError' should be either 'True' or 'False'. Setting to 'True'.");
                    stopOnError = true;
                }
                else
                {
                    if (parserConfig["stopOnError"].ToString().Equals("False"))
                        stopOnError = false;
                    else
                        stopOnError = true;
                }
            }


            watcher = new FileSystemWatcher(watchDirectory, "*.*");
            watcher.Created += OnCreated;
            watcher.EnableRaisingEvents = true;
            LogToFile(DateTime.Now + " CsvToSqlite service has started");
        }

        private bool logBasic()
        {
            if (loggingLevel.Equals("basic"))
                return true;
            return false;
        }

        public void StartService()
        {
            OnStart(new string[3]);
            Console.ReadLine();
            OnStop();
        }


        private void OnCreated(object source, FileSystemEventArgs e)
        {
            var filename = e.FullPath;
            string fileData = null;
            var error = new Exception("");
            for (var i = 0; i < 10; i++)
                try
                {
                    fileData = File.ReadAllText(filename);
                    break;
                }
                catch (IOException err)
                {
                    error = err;
                }

            if (fileData.Equals(null))
            {
                ShowErrors(error,
                    DateTime.Now + " ERROR: Could not read file " + filename +
                    ". Please make sure it exists and Local System has access to it.");
                return;
            }

            if (fileData.Equals("")) return;
            var parser = new Parser(filename, fileData);
            var csv = parser.Parse();
            var headers = (List<string>) csv["headers"];
            var data = (List<List<string>>) csv["data"];
            var columns = JsonSerializer.Deserialize<Dictionary<string, object>>(parserConfig["columns"].ToString());
            if (Parser.hasDuplicate(headers))
            {
                var distinctColumns = "";
                if (headers.Distinct().Count() == 1)
                {
                    distinctColumns = "column " + headers.Distinct().ToArray()[0];
                }
                else if (headers.Distinct().Count() == 2)
                {
                    distinctColumns += "columns " + headers.Distinct().ToArray()[0] + " and " +
                                       headers.Distinct().ToArray()[1];
                }
                else
                {
                    distinctColumns += "columns ";
                    for (var i = 0; i < headers.Distinct().Count(); i++)
                        if (i == headers.Distinct().Count() - 1)
                            distinctColumns += "and " + headers.Distinct().ToArray()[i];
                        else
                            distinctColumns += headers.Distinct().ToArray()[i] + ", ";
                }

                LogToFile(DateTime.Now + " PARSE ERROR: Found duplicates for " + distinctColumns +
                          ". Stopping parsing...");
                return;
            }

            if (!(headers.Count == columns.Count))
            {
                LogToFile(DateTime.Now + " PARSE ERROR: Found " + headers.Count + " columns in the header of '" +
                          filename + "' but it should have " + columns.Count + " columns. Stopped parsing '" +
                          filename + "'.");
                return;
            }

            for (var i = 0; i < data.Count; i++)
                if (!(data[i].Count == headers.Count))
                {
                    LogToFile(DateTime.Now + " PARSE ERROR: Row " + (i + 2) + " has " + data[i].Count +
                              " columns while it should have " + columns.Count + " columns. Stopped parsing '" +
                              filename + "'.");
                    return;
                }

            for (var i = 0; i < headers.Count; i++)
                if (!columns.ContainsKey(headers[i]))
                {
                    LogToFile(DateTime.Now + " PARSE ERROR: Column '" + headers[i] +
                              "' does not match any column defined in '" + parserConfigFile + "'");
                    return;
                }

            var columnNames = "";
            for (var i = 0; i < headers.Count; i++)
                if (i == headers.Count - 1)
                    columnNames += "'" + headers[i].Replace("'", "''") + "'";
                else
                    columnNames += "'" + headers[i].Replace("'", "''") + "'" + ",";
            try
            {
                using (var c = new SQLiteConnection(databaseFile))
                {
                    c.Open();
                    for (var i = 0; i < data.Count; i++)
                    {
                        var values = "";
                        var skip = false;
                        for (var j = 0; j < data[i].Count; j++)
                        {
                            var column =
                                JsonSerializer.Deserialize<Dictionary<string, string>>(columns[headers[j]].ToString());
                            if (!column.ContainsKey("format"))
                            {
                                LogToFile(DateTime.Now + " PARSE ERROR: Could not find key 'format'" + " for column " +
                                          headers[j] + ". Please check " + parserConfigFile + ". Stopping parser ...");
                                c.Close();
                                return;
                            }

                            var format = column["format"];
                            var cell = data[i][j];
                            var notempty = false;
                            if (column.ContainsKey("notEmpty") && cell.Equals(""))
                                if (column["notEmpty"].ToLower().Equals("true"))
                                    notempty = true;
                            if (!IsValid(cell, format, notempty))
                            {
                                if (stopOnError)
                                {
                                    LogToFile(DateTime.Now + " PARSE ERROR: Row " + (i + 2) + " column " + (j+1) +
                                              ": Value '" + data[i][j] +
                                              "' does not match the format specified for column " + headers[j] +
                                              ". Stopping parser ...");
                                    c.Close();
                                    return;
                                }

                                LogToFile(DateTime.Now + " PARSE ERROR: Row " + (i + 2) + " column " + (j + 1) +
                                          ": Value does not match the format specified for column " + headers[j] +
                                          ". Skipping ...");
                                skip = true;
                                values = "";
                                break;
                            }

                            if (column.ContainsKey("notEmpty") && cell.Equals(""))
                                if (column["notEmpty"].ToLower().Equals("true"))
                                {
                                    if (stopOnError)
                                    {
                                        LogToFile(DateTime.Now + " PARSE ERROR: Row " + (i + 2) + " column " + (j + 1) +
                                                  ": Value for " + headers[j] + " cannot be empty. Skipping ...");
                                        break;
                                        c.Close();
                                        return;
                                    }

                                    LogToFile(DateTime.Now + " PARSE ERROR: Row " + (i + 2) + " column " + (j + 1) +
                                              ": Value for " + headers[j] + " cannot be empty. Skipping ...");
                                    skip = true;
                                    values = "";
                                    break;
                                }

                            if (j == data[i].Count - 1)
                            {
                                values += "'" + data[i][j].Replace("'", "''") + "'";
                                break;
                            }

                            values += "'" + data[i][j].Replace("'", "''") + "'" + ",";
                        }

                        if (skip || values.Equals("")) continue;

                        var query = "INSERT INTO " + tableName + "(" + columnNames + ") VALUES(" + values + ")";
                        LogToFile(query);
                        using (var command = new SQLiteCommand(query, c))
                        {
                            command.ExecuteNonQuery();
                            command.Dispose();
                        }
                    }

                    c.Close();
                }
            }
            catch (SQLiteException err)
            {
                ShowErrors(err,
                    DateTime.Now + " PARSE ERROR: Could not add row to " + databaseFile + ". Error: " + err.Message +
                    ". Stopping Parser...");
            }
        }

        protected override void OnStop()
        {
            LogToFile(DateTime.Now + " CsvToSqlite service has stopped");
        }

        private static List<string> split(string format, char seperator)
        {
            var tokens = new List<string>();
            var escaping = false;
            var quoteChar = ' ';
            var quoting = false;
            var lastCloseQuoteIndex = int.MaxValue;
            var current = new StringBuilder();
            for (var i = 0; i < format.Length; i++)
            {
                var c = format.ToCharArray()[i];
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
                else if (!quoting && c.Equals(seperator))
                {
                    if (current.Length > 0 || lastCloseQuoteIndex == i - 1)
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

            if (current.Length > 0 || lastCloseQuoteIndex == format.Length - 1) tokens.Add(current.ToString());
            return tokens;
        }

        private bool IsValid(string value, string formats, bool notempty)
        {
            if (value.Equals(""))
            {
                if (notempty)
                    return false;
                return true;
            }

            var isValid = false;
            var formatSplit = split(formats, '|');
            foreach (var formatRaw in formatSplit)
            {
                var format = formatRaw.Trim();
                if (format == "string")
                {
                    isValid = true;
                    break;
                }

                if (format == "int")
                {
                    long output = 0;
                    if (long.TryParse(value, out output))
                    {
                        isValid = true;
                        break;
                    }
                }

                if (format == "decimal")
                {
                    decimal outpuDecimal = 0;
                    if (decimal.TryParse(value, out outpuDecimal))
                    {
                        isValid = true;
                        break;
                    }
                }

                if (format.Equals("boolean"))
                    if (value.ToLower().Equals("true") || value.ToLower().Equals("false"))
                    {
                        isValid = true;
                        break;
                    }

                if (format.Length >= 3)
                {
                    if (format[0].Equals('(') && format[format.Length - 1].Equals(')'))
                    {
                        var splitOptions = split(format.Substring(1, format.Length - 2), '/');
                        if (splitOptions.Distinct().Count() == 1)
                        {
                            if (value.Equals(splitOptions[0]))
                            {
                                isValid = true;
                                break;
                            }
                        }
                        else
                        {
                            foreach (var option in splitOptions)
                                if (option.Equals(value))
                                {
                                    isValid = true;
                                    break;
                                }
                        }
                    }

                    if (format[0].Equals('@') && format[format.Length - 1].Equals('@'))
                    {
                        var pattern = format.Substring(1, format.Length - 2);
                        var rgx = new Regex(pattern);
                        if (rgx.IsMatch(value)) isValid = true;
                    }
                }

                if (format.StartsWith("string"))
                {
                    var ranges = format.Substring(6);
                    if (!ranges.Equals(""))
                        if (!(ranges.Length < 5))
                            if (ranges[0].Equals('[') && ranges[ranges.Length - 1].Equals(']'))
                            {
                                var parsed = ranges.Skip(1).ToList();
                                parsed.RemoveAt(parsed.Count - 1);
                                var splitRanges = split(string.Join("", parsed), ',');
                                splitRanges[0] = splitRanges[0].Trim();
                                splitRanges[1] = splitRanges[1].Trim();
                                if (splitRanges.Count == 2)
                                {
                                    long output = 0;
                                    if (long.TryParse(splitRanges[0], out output) || splitRanges[0].Equals("*"))
                                        if (long.TryParse(splitRanges[1], out output) || splitRanges[1].Equals("*"))
                                        {
                                            long lowerRange;
                                            long upperRange;
                                            var rangesArray = GetRanges(splitRanges);
                                            lowerRange = rangesArray[0];
                                            upperRange = rangesArray[1];
                                            if (value.Length <= upperRange && value.Length >= lowerRange)
                                            {
                                                isValid = true;
                                                break;
                                            }
                                        }
                                }
                            }
                }

                if (format.StartsWith("int"))
                {
                    var ranges = format.Substring(3);
                    if (!ranges.Equals(""))
                    {
                        long testOut = 0;
                        if (long.TryParse(value, out testOut))
                            if (!(ranges.Length < 5))
                                if (ranges[0].Equals('[') && ranges[ranges.Length - 1].Equals(']'))
                                {
                                    var parsed = ranges.Skip(1).ToList();
                                    parsed.RemoveAt(parsed.Count - 1);
                                    var splitRanges = split(string.Join("", parsed), ',');
                                    if (splitRanges.Count == 2)
                                    {
                                        long output = 0;
                                        if (long.TryParse(splitRanges[0], out output) || splitRanges[0].Equals("*"))
                                            if (long.TryParse(splitRanges[1], out output) || splitRanges[1].Equals("*"))
                                            {
                                                long lowerRange;
                                                long upperRange;
                                                var rangesArray = GetRanges(splitRanges);
                                                lowerRange = rangesArray[0];
                                                upperRange = rangesArray[1];

                                                if (long.Parse(value) <= upperRange && long.Parse(value) >= lowerRange)
                                                {
                                                    isValid = true;
                                                    break;
                                                }
                                            }
                                    }
                                }
                    }

                    if (format.Equals(value))
                    {
                        isValid = true;
                        break;
                    }
                }
            }

            return isValid;
        }

        private long[] GetRanges(List<string> rangesSplit)
        {
            var ranges = new long[2];
            if (rangesSplit[0].Equals("*"))
                ranges[0] = long.MaxValue;
            else
                ranges[0] = long.Parse(rangesSplit[0]);

            if (rangesSplit[1].Equals("*"))
                ranges[1] = long.Parse(rangesSplit[1]);
            else
                ranges[1] = long.Parse(rangesSplit[1]);
            return ranges;
        }

        public void LogToFile(string Message)
        {
            if (loggingDirectory.Equals(""))
            {
                if (ConfigurationManager.AppSettings.Get("logdirectory").Equals("") && loggingDirectory.Equals(""))
                {
                    eventLog.WriteEntry(
                        "INFO: Config file 'logdirectory' is not set. Setting to C:\\Users\\diego\\CsvToSqlite\\Logs");
                    loggingDirectory = "C:\\Users\\diego\\CsvToSqlite\\Logs";
                    if (!Directory.Exists(loggingDirectory))
                        try
                        {
                            Directory.CreateDirectory(loggingDirectory);
                        }
                        catch (UnauthorizedAccessException err)
                        {
                            eventLog.WriteEntry(
                                "CRITICAL ERROR: An upexpected error occured while creating the defualt log directory " +
                                "C:\\Users\\diego\\CsvToSqlite\\Logs" +
                                ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                                err);
                            Stop();
                        }
                        catch (IOException err)
                        {
                            eventLog.WriteEntry(
                                "CRITICAL ERROR: An upexpected error occured while creating the defualt log directory " +
                                "C:\\Users\\diego\\CsvToSqlite\\Logs" +
                                ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                                err);
                            Stop();
                        }
                }
                else
                {
                    loggingDirectory = ConfigurationManager.AppSettings.Get("logdirectory");
                    if (!Directory.Exists(loggingDirectory))
                    {
                        eventLog.WriteEntry("INFO: Could not find log directory " + loggingDirectory +
                                            ". Attempting to create it.");
                        try
                        {
                            Directory.CreateDirectory(loggingDirectory);
                        }
                        catch (UnauthorizedAccessException err)
                        {
                            eventLog.WriteEntry(
                                "CRITICAL ERROR: An unexpected error occured while creating the log directory " +
                                loggingDirectory +
                                ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                                err);
                            Stop();
                        }
                        catch (IOException err)
                        {
                            eventLog.WriteEntry(
                                "CRITICAL ERROR: An unexpected error occured while creating the log directory " +
                                loggingDirectory +
                                ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                                err);
                            Stop();
                        }
                    }
                }
            }

            if (!Directory.Exists(loggingDirectory))
            {
                eventLog.WriteEntry("INFO: " + loggingDirectory + " does not exist. Attemping to create it.");
                try
                {
                    Directory.CreateDirectory(loggingDirectory);
                }
                catch (UnauthorizedAccessException err)
                {
                    eventLog.WriteEntry(
                        "CRITICAL ERROR: An upexpected error occured while creating the log directory " +
                        loggingDirectory +
                        ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                        err);
                    Stop();
                }
                catch (IOException err)
                {
                    eventLog.WriteEntry(
                        "CRITICAL ERROR: An upexpected error occured while creating the log directory " +
                        loggingDirectory +
                        ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                        err);
                    Stop();
                }
            }

            var filepath = loggingDirectory + "\\log.txt";
            if (!File.Exists(filepath))
                using (var sw = File.CreateText(filepath))
                {
                    try
                    {
                        sw.WriteLine(Message);
                    }
                    catch (UnauthorizedAccessException err)
                    {
                        eventLog.WriteEntry("ERROR: An unexpected error occured while creating the log file " +
                                            filepath +
                                            ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                                            err);
                    }
                    catch (IOException err)
                    {
                        eventLog.WriteEntry("ERROR: An unexpected error occured while creating the log file " +
                                            filepath +
                                            ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                                            err);
                    }
                }
            else
                try
                {
                    using (var sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
                catch (UnauthorizedAccessException err)
                {
                    eventLog.WriteEntry("ERROR: An unexpected error occured while writing the log file " + filepath +
                                        ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                                        err);
                }
                catch (IOException err)
                {
                    eventLog.WriteEntry("ERROR: An unexpected error occured while writing the log file " + filepath +
                                        ". Please make sure the path exists and Local System has permissions to access it.\nError Message:\n" +
                                        err);
                }
        }
    }
}