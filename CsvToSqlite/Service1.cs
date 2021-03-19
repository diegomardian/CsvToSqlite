using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace CsvToSqlite
{
    public partial class CsvToSqlite : ServiceBase
    {
        private String homepath;
        private String watchpath;
        private String datapath;
        private String parserConfigFile;
        private String loggingLevel;
        private String loggingDirectory;
        private Dictionary<String, Object> parserConfig;
        private FileSystemWatcher watcher;
        private String connString;
        private bool stopOnError;
        private EventLog eventLog;

        public CsvToSqlite()
        {
            eventLog = new System.Diagnostics.EventLog();
            eventLog.Source = "CsvToSqlite";
            InitializeComponent();


        }
        private void ShowErrors(Exception err, String message) 
        {
            LogToFile(message);
            if (!logBasic())
                LogToFile("Error Message:\n" + err.ToString());
        }
        private void CreateDirectory(String dir, String message)
        {
            try
            {
                Directory.CreateDirectory(dir);
            }
            catch (IOException err)
            {
                ShowErrors(err, message);
                this.Stop();
            }
            catch (UnauthorizedAccessException err)
            {
                LogToFile(message);
            }
        }


        protected override void OnStart(string[] args)
        {
            this.loggingDirectory = "";
            if (ConfigurationManager.AppSettings.Get("homepath").Equals(""))
            {
                LogToFile("INFO: Config file paramater 'homepath' not set. Setting to C:\\Users\\diego\\CsvToSqlite");
                this.homepath = "C:\\Users\\diego\\CsvToSqlite";
                if (!Directory.Exists(homepath))
                {
                    CreateDirectory(this.homepath, DateTime.Now + " CRITICAL ERROR: An error occurred while creating the directory " + this.homepath + ". Try checking the permissions of " + this.homepath + " and make sure that NETWORK SERVICE has been granted access.");
                }
            }
            else
            {
                this.homepath = ConfigurationManager.AppSettings.Get("homepath");
                if (!Directory.Exists(homepath))
                {
                    CreateDirectory(this.homepath, DateTime.Now + " CRITICAL ERROR: An error occurred while creating the directory " + this.homepath + ". Try checking the permissions of " + this.homepath + " and make sure that NETWORK SERVICE has been granted access.");
                }
            }
            if (ConfigurationManager.AppSettings.Get("watchdirectory").Equals(""))
            {
                LogToFile("INFO: Config file paramater 'watchdirectory' not set. Setting to C:\\Users\\diego\\CsvToSqlite\\Convert");
                this.watchpath = "C:\\Users\\diego\\CsvToSqlite\\Convert";
                if (!Directory.Exists(watchpath))
                {
                    CreateDirectory(this.watchpath, DateTime.Now + " CRITICAL ERROR: An error occurred while creating the directory " + this.watchpath + ". Try checking the permissions of " + this.watchpath + " and make sure that NETWORK SERVICE has been granted access.");
                }

            }
            else
            {
                this.watchpath = ConfigurationManager.AppSettings.Get("watchdirectory");
                if (!Directory.Exists(watchpath))
                {
                    CreateDirectory(this.watchpath, DateTime.Now + " CRITICAL ERROR: An error occurred while creating the directory " + this.watchpath + ". Try checking the permissions of " + this.watchpath + " and make sure that NETWORK SERVICE has been granted access.");

                }
                ConfigurationManager.AppSettings.Set("watchdirectory", watchpath);

            }
            if (ConfigurationManager.AppSettings.Get("parserConfigFile").Equals(""))
            {
                LogToFile(DateTime.Now + " CRITICAL ERROR: Config file paramater 'parserConfigFile' not set. Quiting ...");
                this.Stop();
            }
            else
            {
                this.parserConfigFile = ConfigurationManager.AppSettings.Get("parserConfigFile");
                if (!File.Exists(this.parserConfigFile))
                {
                    LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find file " + this.parserConfigFile+". Quiting ...");
                    this.Stop();
                }

            }
            if (ConfigurationManager.AppSettings.Get("databasePath").Equals(""))
            {
                this.datapath = "Data Source = C:\\Users\\diego\\CsvToSqlite\\CsvToSqlite.db;";
                if (!File.Exists("C:\\Users\\diego\\CsvToSqlite\\CsvToSqlite.db"))  
                {
                    LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find database path: " + datapath);
                    this.Stop();
                }
            }
            else
            {
                if (!File.Exists(ConfigurationManager.AppSettings.Get("databasePath")))
                {
                    LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find database file " + ConfigurationManager.AppSettings.Get("databasePath") + ".");
                    this.Stop();
                }
                this.datapath = "Data Source = "+ConfigurationManager.AppSettings.Get("databasePath")+ ";";
            }
            if ((!ConfigurationManager.AppSettings.Get("logginglevel").Equals("basic")) && (!ConfigurationManager.AppSettings.Get("logginglevel").Equals("advanced")))
            {
                LogToFile(DateTime.Now + " ERROR: Could not parse App.config key \'logginglevel\', value must be set to \'basic\' or \'advanced\'. Setting logginglevel to \'basic\'");
                ConfigurationManager.AppSettings.Set("logginglevel", "basic");
                this.loggingLevel = ConfigurationManager.AppSettings.Get("logginglevel");
            }
            else
            {
                this.loggingLevel = ConfigurationManager.AppSettings.Get("logginglevel");
            }
            
            try { 
                String jsonText = File.ReadAllText(this.parserConfigFile);
                this.parserConfig = JsonSerializer.Deserialize<Dictionary<String, Object>>(jsonText);
            }
            catch (IOException err)
            {
                ShowErrors(err, DateTime.Now + " CRITICAL ERROR: Could not read " + this.parserConfigFile + ". Please make sure the file exists and NETWORK SERVICE has access to it. Quiting ...");
                this.Stop();
            }
            catch (JsonException err)
            {
                ShowErrors(err, DateTime.Now + " CRITICAL ERROR: An error occured while parsing " + this.parserConfigFile + ". Please provide valid JSON. Quiting ...");
                this.Stop();
            }
            catch (System.UnauthorizedAccessException err)
            {
                ShowErrors(err, DateTime.Now + " CRITICAL ERROR: NETWORK SERVICE does not have permissions to access " + this.parserConfigFile + ". Please make sure the file exists and NETWORK SERVICE has access to it. Quiting ...");
            }
            if (!this.parserConfig.ContainsKey("columns"))
            {
                LogToFile(DateTime.Now + " CRITICAL ERROR: Please include a 'columns' key in your parser config file. Quiting...");
                this.Stop();
            }
            if (!this.parserConfig.ContainsKey("stopOnError"))
            {
                LogToFile(DateTime.Now + " ERROR: Could not find the key 'stopOnError' in your parser config file. Setting to 'True'.");
                this.stopOnError = true;
            }
            else
            {
                if (!(this.parserConfig["stopOnError"].ToString().Equals("False") || this.parserConfig["stopOnError"].ToString().Equals("True")))
                {
                    LogToFile(DateTime.Now + " ERROR: Parser config file 'stopOnError' should be either 'True' or 'False'. Setting to 'True'.");
                    this.stopOnError = true;
                }
                else
                {
                    if (this.parserConfig["stopOnError"].ToString().Equals("False"))
                    {
                        this.stopOnError = false;
                    }
                    else 
                    {
                        this.stopOnError = true;
                    }
                }
            }


            this.watcher = new FileSystemWatcher(this.watchpath, "*.*");
            watcher.Created += OnCreated;
            watcher.EnableRaisingEvents = true;
            LogToFile(DateTime.Now + " CsvToSqlite service has started");



        }
        private Boolean logBasic()
        {
            if (ConfigurationManager.AppSettings.Get("logginglevel").Equals("basic"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void StartService()
        {
            this.OnStart(new string[3]);
            Console.ReadLine();
            this.OnStop();
        }
        

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            

            String filename = e.FullPath;
            String fileData = null;
            Exception error = new Exception("");
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    fileData = File.ReadAllText(filename);
                    break;
                }
                catch (IOException err)
                {
                    error = err;
                }
            }
            if (fileData.Equals(null))
            {
                this.ShowErrors(error, DateTime.Now + " ERROR: Could not read file " + filename + ". Please make sure it exists and NETWORK SERVICE has access to it.");
                return;
            }
            if (fileData.Equals(""))
            {
                return;
            }
            Parser parser = new Parser(filename, fileData);
            Dictionary<String, Object> csv = parser.Parse();
            List<String> headers = (List<String>)csv["headers"];
            List<List<String>> data = (List<List<String>>) csv["data"];
            var columns = JsonSerializer.Deserialize<Dictionary<String, Object>>(this.parserConfig["columns"].ToString());
            if (Parser.hasDuplicate(headers))
            {
                String distinctColumns = "";
                if (headers.Distinct().Count() == 1)
                {
                    distinctColumns = "column " + headers.Distinct().ToArray()[0];
                }
                else if (headers.Distinct().Count() == 2)
                {
                    distinctColumns += "columns " + headers.Distinct().ToArray()[0] + " and " + headers.Distinct().ToArray()[1];
                }
                else { 
                    distinctColumns += "columns ";
                    for (int i = 0; i < headers.Distinct().Count(); i++) { 
                        if (i == headers.Distinct().Count()-1)
                            distinctColumns += "and "+headers.Distinct().ToArray()[i];
                        else
                            distinctColumns += headers.Distinct().ToArray()[i]+", ";
                    }
                }
                LogToFile(DateTime.Now + " PARSE ERROR: Found duplicates for "+ distinctColumns + ". Stopping parsing...");
                return;
            }
            if (!(headers.Count == columns.Count))
            {
                LogToFile(DateTime.Now + " PARSE ERROR: Found " + headers.Count + " columns in the header of "+filename+" but it should have " + columns.Count + " columns. Stopped parsing " + filename + ".");
                return;
            }
            for (int i = 0; i < data.Count; i++)
            {
                if (!(data[i].Count == headers.Count))
                {
                    LogToFile(DateTime.Now + " PARSE ERROR: Row  " + i + " has "+ data[i].Count + " columns while it should have "+ columns.Count + " columns. Stopped parsing "+filename+".");
                    return;
                }
            }
            for (int i = 0; i < headers.Count; i++)
            {
                if (!columns.ContainsKey(headers[i]))
                {
                    LogToFile(DateTime.Now+" PARSE ERROR: Column '" + headers[i] + "' does not match any column defined in "+this.parserConfigFile);
                    return;
                }
            }   
            
            String columnNames = "";
            for (int i = 0; i < headers.Count; i++)
            {
                if (i == headers.Count-1)  
                {
                    columnNames += headers[i];
                }
                else { 
                    columnNames += headers[i] + ",";
                }
            }
            try
            {
                using (SQLiteConnection c = new SQLiteConnection(this.datapath))
                {
                    c.Open();
                    for (int i = 0; i < data.Count; i++)
                    {
                        String values = "";
                        Boolean skip = false;
                        for (int j = 0; j < data[i].Count; j++)
                        {
                            var column = JsonSerializer.Deserialize<Dictionary<String, String>>(columns[headers[j]].ToString());
                            String format = (String)column["format"];
                            String cell = data[i][j];
                            if (!IsValid(cell, format))
                            {
                                if (this.stopOnError)
                                {
                                    LogToFile(DateTime.Now + " PARSE ERROR: Row " + i + " column " + j + ": Value '"+data[i][j]+"' does not match the format specified for column " + headers[j] + ". Stopping parser ...");
                                    c.Close();
                                    return;
                                }
                                else
                                {
                                    LogToFile(DateTime.Now + " PARSE ERROR: Row " + i + " column " + j + ": Value does not match the format specified for column " + headers[j] + ". Skipping ...");
                                    skip = true;
                                    values = "";
                                    break;
                                }

                            }
                            else
                            {
                                if (j == data[i].Count - 1)
                                {
                                    values += "'" + data[i][j] + "'";
                                    break;
                                }
                                else
                                {
                                    values += "'" + data[i][j] + "'" + ",";
                                }


                        
                            }
                        }
                        if (skip || values.Equals(""))
                        {
                            continue;
                        }
                
                            String query = "INSERT INTO CsvToSqlite(" + columnNames + ") VALUES(" + values + ")";
                            using (SQLiteCommand command = new SQLiteCommand(query, c))
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
                
                ShowErrors(err, DateTime.Now + " PARSE ERROR: Could not add row to " + this.datapath + ". Error: " + err.Message + ". Stopping Parser...");
                return;
            }
        }
    
        protected override void OnStop()
        {
            LogToFile(DateTime.Now + " CsvToSqlite service has stopped");
        }
        private static List<String> split(String format, char seperator)
        {
            List<String> tokens = new List<String>();
            Boolean escaping = false;
            char quoteChar = ' ';
            Boolean quoting = false;
            int lastCloseQuoteIndex = int.MaxValue;
            StringBuilder current = new StringBuilder();
            for (int i = 0; i < format.Length; i++)
            {
                char c = format.ToCharArray()[i];
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
            if (current.Length > 0 || lastCloseQuoteIndex == (format.Length - 1))
            {
                tokens.Add(current.ToString());
            }
            return tokens;
        }

        private Boolean IsValid(String value, String formats)
        {
            Boolean isValid = false;
            List<String> formatSplit = split(formats, '|');
            foreach (var formatRaw in formatSplit)
            {
                String format = formatRaw.Trim();
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
                {
                    if (value.ToLower().Equals("true") || value.ToLower().Equals("false"))
                    {
                        isValid = true;
                        break;
                    }
                }

                if (format.Length >= 3)
                {
                    if (format[0].Equals('(') && format[format.Length - 1].Equals(')'))
                    {

                        List<String> splitOptions = split(format.Substring(1, format.Length - 2), '/');
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
                            foreach (String option in splitOptions)
                            {
                                if (option.Equals(value))
                                {
                                    isValid = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (format[0].Equals('@') && format[format.Length - 1].Equals('@'))
                    {
                        string pattern = format.Substring(1, format.Length - 2);
                        Regex rgx = new Regex(pattern);
                        if (rgx.IsMatch(value))
                        {
                            isValid = true;
                        }

                    }
                }

                if (format.StartsWith("string"))
                {
                    String ranges = format.Substring(6);
                    if (!ranges.Equals(""))
                    {
                        if (!(ranges.Length < 5))
                        {
                            if (ranges[0].Equals('[') && ranges[ranges.Length - 1].Equals(']'))
                            {
                                List<Char> parsed = ranges.Skip(1).ToList();
                                parsed.RemoveAt(parsed.Count - 1);
                                List<String> splitRanges = split(String.Join("", parsed), ',');
                                splitRanges[0] = splitRanges[0].Trim();
                                splitRanges[1] = splitRanges[1].Trim();
                                if (splitRanges.Count == 2)
                                {
                                    long output = 0;
                                    if (long.TryParse(splitRanges[0], out output) || splitRanges[0].Equals("*"))
                                    {
                                        if (long.TryParse(splitRanges[1], out output) || splitRanges[1].Equals("*"))
                                        {
                                            long lowerRange;
                                            long upperRange;
                                            long[] rangesArray = GetRanges(splitRanges);
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
                        }
                    }
                }
                if (format.StartsWith("int"))
                {
                    String ranges = format.Substring(3);
                    if (!ranges.Equals(""))
                    {
                        long testOut = 0;
                        if (long.TryParse(value, out testOut))
                        {
                            if (!(ranges.Length < 5))
                            {
                                if (ranges[0].Equals('[') && ranges[ranges.Length - 1].Equals(']'))
                                {
                                    List<Char> parsed = ranges.Skip(1).ToList();
                                    parsed.RemoveAt(parsed.Count - 1);
                                    List<String> splitRanges = split(String.Join("", parsed), ',');
                                    if (splitRanges.Count == 2)
                                    {
                                        long output = 0;
                                        if (long.TryParse(splitRanges[0], out output) || splitRanges[0].Equals("*"))
                                        {
                                            if (long.TryParse(splitRanges[1], out output) || splitRanges[1].Equals("*"))
                                            {
                                                long lowerRange;
                                                long upperRange;
                                                long[] rangesArray = GetRanges(splitRanges);
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
        private long[] GetRanges(List<String> rangesSplit)
        {
            long[] ranges = new long[2];
            if (rangesSplit[0].Equals("*"))
            {
                ranges[0] = long.MaxValue;
            }
            else
            {
                ranges[0] = long.Parse(rangesSplit[0]);
            }

            if (rangesSplit[1].Equals("*"))
            {
                ranges[1] = long.Parse(rangesSplit[1]);
            }
            else
            {
                ranges[1] = long.Parse(rangesSplit[1]);
            }
            return ranges;
        }
        public void LogToFile(string Message)
        {
            if (this.loggingDirectory.Equals(""))
            {
                if (ConfigurationManager.AppSettings.Get("logDirectory").Equals("") && this.loggingDirectory.Equals(""))
                {
                    eventLog.WriteEntry("INFO: Config file 'logDirectory' is not set. Setting to C:\\Users\\diego\\CsvToSqlite\\Logs");
                    this.loggingDirectory = "C:\\Users\\diego\\CsvToSqlite\\Logs";
                    if (!Directory.Exists(loggingDirectory))
                    {
                        try
                        {
                            Directory.CreateDirectory(loggingDirectory);
                        }
                        catch (Exception err)
                        {
                            eventLog.WriteEntry("CRITICAL ERROR: An upexpected error occured while creating the defualt log directory "+ "C:\\Users\\diego\\CsvToSqlite\\Logs" + ". Please make sure the path exists and NETWORK SERVICE has permissions to access it.\nError Message:\n" + err.ToString());
                            this.Stop();
                        }


                    }

                }
                else
                {
                    this.loggingDirectory = ConfigurationManager.AppSettings.Get("loggingDirectory");
                    if (!Directory.Exists(loggingDirectory))
                    {
                        eventLog.WriteEntry("INFO: Could not find log directory "+this.loggingDirectory+". Attempting to create it.");
                        try
                        {
                            Directory.CreateDirectory(loggingDirectory);
                        }
                        catch (Exception err)
                        {
                            eventLog.WriteEntry("CRITICAL ERROR: An upexpected error occured while creating the log directory " + this.loggingDirectory + ". Please make sure the path exists and NETWORK SERVICE has permissions to access it.\nError Message:\n" + err.ToString());
                            this.Stop();
                        }

                    }

                }
            }
            if (!Directory.Exists(this.loggingDirectory))
            {
                eventLog.WriteEntry("INFO: "+this.loggingDirectory+" does not exist. Attemping to create it.");
                try
                {
                    Directory.CreateDirectory(loggingDirectory);
                }
                catch (Exception err)
                {
                    eventLog.WriteEntry("CRITICAL ERROR: An upexpected error occured while creating the log directory " + this.loggingDirectory + ". Please make sure the path exists and NETWORK SERVICE has permissions to access it.\nError Message:\n" + err.ToString());
                    this.Stop();
                }
                
            }
            string filepath = this.loggingDirectory + "\\log.txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    try { 
                        sw.WriteLine(Message);
                    }
                    catch (Exception err)
                    {
                        eventLog.WriteEntry("ERROR: An unexpected error occured while creating the log file " + filepath + ". Please make sure the path exists and NETWORK SERVICE has permissions to access it.\nError Message:\n" + err.ToString());
                    }
                }
            }
            else
            {
                try
                {
                    using (StreamWriter sw = File.AppendText(filepath))
                    {
                        sw.WriteLine(Message);
                    }
                }
                catch (Exception err)
                {
                   eventLog.WriteEntry("ERROR: An unexpected error occured while writing the log file " + filepath + ". Please make sure the path exists and NETWORK SERVICE has permissions to access it.\nError Message:\n" + err.ToString());
                }
                
            }
        }
    }
}