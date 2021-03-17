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


        protected override void OnStart(string[] args)
        {
            this.loggingDirectory = "";
            if (ConfigurationManager.AppSettings.Get("homepath").Equals(""))
            {
                LogToFile("INFO: Config file paramater 'homepath' not set. Setting to C:\\Users\\diego\\CsvToSqlite");
                this.homepath = "C:\\Users\\diego\\CsvToSqlite";
                if (!Directory.Exists(homepath))
                {
                    try
                    {
                        Directory.CreateDirectory(homepath);
                    }
                    catch (IOException err)
                    {
                        LogToFile(DateTime.Now + " CRITICAL ERROR: An error occurred while creating the directory " +
                                  this.homepath + ". Try checking the permissions of " + this.homepath +
                                  " and make sure that Network Service has been granted access.");
                        if (!logBasic())
                        {
                            LogToFile("Error Message\n:" + err.ToString());
                        }

                        this.Stop();
                    }
                }
            }
            else
            {
                this.homepath = ConfigurationManager.AppSettings.Get("homepath");
                if (!Directory.Exists(homepath))
                {
                    try
                    {
                        Directory.CreateDirectory(homepath);
                    }
                    catch (IOException err)
                    {
                        LogToFile(DateTime.Now +
                                  " CRITICAL ERROR: An error occurred while creating the directory " +
                                  this.homepath + ". Try checking the permissions of " + this.homepath +
                                  " and make sure that Network Service has been granted access.");
                        if (!logBasic())
                        {
                            LogToFile("Error Message\n:" + err.ToString());
                        }

                        this.Stop();
                    }
                }
            }
            if (ConfigurationManager.AppSettings.Get("watchdirectory").Equals(""))
            {
                LogToFile("INFO: Config file paramater 'watchdirectory' not set. Setting to C:\\Users\\diego\\CsvToSqlite\\Convert");
                this.watchpath = "C:\\Users\\diego\\CsvToSqlite\\Convert";
                if (!Directory.Exists(watchpath))
                {
                    try
                    {
                        Directory.CreateDirectory(watchpath);
                    }
                    catch (IOException err)
                    {
                        LogToFile(DateTime.Now +
                                  " CRITICAL ERROR: An error occurred while creating the directory " +
                                  this.watchpath + ". Try checking the permissions of " + this.homepath +
                                  " and make sure that Network Service has been granted access.");
                        if (!logBasic())
                        {
                            LogToFile("Error Message\n:" + err.ToString());
                        }

                        this.Stop();
                    }


                }
                
            }
            else
            {
                this.watchpath = ConfigurationManager.AppSettings.Get("watchdirectory");
                if (!Directory.Exists(watchpath))
                {
                    try
                    {
                        Directory.CreateDirectory(watchpath);
                    }
                    catch (IOException err)
                    {
                        LogToFile(DateTime.Now +
                                  " CRITICAL ERROR: An error occurred while creating the directory " +
                                  this.watchpath + ". Try checking the permissions of " + this.homepath +
                                  " and make sure that Network Service has been granted access.");
                        if (!logBasic())
                        {
                            LogToFile("Error Message\n:" + err.ToString());
                        }

                        this.Stop();
                    }

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
                this.datapath = "C:\\Users\\diego\\CsvToSqlite\\CsvToSqlite.db";
                if (!File.Exists(datapath))  
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
                this.datapath = ConfigurationManager.AppSettings.Get("databasePath");
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
                LogToFile(DateTime.Now + " CRITICAL ERROR: Could not read "+this.parserConfigFile+". Please make sure the file exists and NETWORK SERVICE has access to it. Quiting ...");
                if (!logBasic())
                {
                    LogToFile("Error Message\n:" + err.ToString());
                }
                this.Stop();
            }
            catch (JsonException err)
            {
                
                LogToFile(DateTime.Now+" CRITICAL ERROR: An error occured while parsing "+this.parserConfigFile+". Please provide valid JSON. Quiting ...");
                if (!logBasic())
                {
                    LogToFile("Error Message:\n"+ err.ToString());
                }
                this.Stop();
            }
            catch (System.UnauthorizedAccessException err)
            {
                LogToFile(DateTime.Now + " CRITICAL ERROR: NETWORK SERVICE does not have permissions to access "+this.parserConfigFile+ ". Please make sure the file exists and NETWORK SERVICE has access to it. Quiting ...");
                if (!logBasic())
                {
                    LogToFile("Error Message:\n" + err.ToString());
                }
                this.Stop();
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
            String error = "";
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    fileData = File.ReadAllText(filename);
                    break;
                }
                catch (IOException err)
                {
                    error = err.ToString();
                }
            }
            if (fileData.Equals(null))
            {
                LogToFile(DateTime.Now + " ERROR: Could not read file "+ filename + ". Please make sure it exists and NETWORK SERVICE has access to it.");
                if (!logBasic())
                    LogToFile("Error Message:\n" + error);
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
                    LogToFile(DateTime.Now+" PARSE ERROR: Column " + headers[i] + " does not match any column defined in "+this.parserConfigFile);
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
            for (int i = 0; i < data.Count; i++)
            {
                String values = "";
                Boolean skip = false;
                for (int j = 0; i < data[i].Count; j++)
                {
                    var column = JsonSerializer.Deserialize<Dictionary<String, String>>(columns[headers[j]].ToString());
                    String format = (String)column["format"];
                    String cell = data[i][j];
                    LogToFile("Format:"+ format);
                    if (!IsValid(cell, format))
                    {
                        if (this.stopOnError)
                        {
                            LogToFile(DateTime.Now + " PARSE ERROR: Row " + i + " column " + j + ": Value '"+data[i][j]+"' does not match the format specified for column " + headers[j] + ". Stopping parser ...");
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
                try
                {
                    using (SQLiteConnection c = new SQLiteConnection("Data Source=C:\\Users\\diego\\CsvToSqlite\\CsvToSqlite.db;"))
                    {
                        c.Open();
                        String query = "INSERT INTO CsvToSqlite(" + columnNames + ") VALUES(" + values + ")";
                        using (SQLiteCommand command = new SQLiteCommand(query, c))
                        {
                            command.ExecuteNonQuery();
                            command.Dispose();
                        }
                        c.Close();
                    }
                }
                catch (SQLiteException err)
                {
                    LogToFile(DateTime.Now + " PARSE ERROR: Could not add row to " + this.datapath + ". Error: "+err.Message+". Stopping Parser...");
                    if (!logBasic())
                    {
                        LogToFile("Error Message:\n" + err.ToString());
                    }
                    return;
                }


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
            foreach (var format in formatSplit)
            {
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
                                List<Char> parserd = ranges.Skip(1).ToList();
                                parserd.RemoveAt(parserd.Count - 1);
                                List<String> splitRanges = split(String.Join("", parserd), ',');
                                if (splitRanges.Count == 2)
                                {
                                    long output = 0;
                                    if (long.TryParse(splitRanges[0], out output) || splitRanges[0].Equals("*"))
                                    {
                                        if (long.TryParse(splitRanges[1], out output) || splitRanges[1].Equals("*"))
                                        {
                                            long lowerRange;
                                            long upperRange;
                                            if (splitRanges[0].Equals("*"))
                                            {
                                                lowerRange = long.MaxValue;
                                            }
                                            else
                                            {
                                                lowerRange = long.Parse(splitRanges[0]);
                                            }

                                            if (splitRanges[1].Equals("*"))
                                            {
                                                upperRange = long.Parse(splitRanges[1]);
                                            }
                                            else
                                            {
                                                upperRange = long.Parse(splitRanges[1]);
                                            }

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
                                    List<Char> parserd = ranges.Skip(1).ToList();
                                    parserd.RemoveAt(parserd.Count - 1);
                                    List<String> splitRanges = split(String.Join("", parserd), ',');

                                    if (splitRanges.Count == 2)
                                    {
                                        long output = 0;
                                        if (long.TryParse(splitRanges[0], out output) || splitRanges[0].Equals("*"))
                                        {
                                            if (long.TryParse(splitRanges[1], out output) || splitRanges[1].Equals("*"))
                                            {
                                                long lowerRange;
                                                long upperRange;
                                                if (splitRanges[0].Equals("*"))
                                                {
                                                    lowerRange = long.MaxValue;
                                                }
                                                else
                                                {
                                                    lowerRange = long.Parse(splitRanges[0]);
                                                }

                                                if (splitRanges[1].Equals("*"))
                                                {
                                                    upperRange = long.Parse(splitRanges[1]);
                                                }
                                                else
                                                {
                                                    upperRange = long.Parse(splitRanges[1]);
                                                }

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
                   eventLog.WriteEntry("ERROR: An unexpected error occured while wriing the log file " + filepath + ". Please make sure the path exists and NETWORK SERVICE has permissions to access it.\nError Message:\n" + err.ToString());
                }
                
            }
        }
    }
}