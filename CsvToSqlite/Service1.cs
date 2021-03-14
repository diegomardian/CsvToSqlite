﻿using System;
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
                //Dictionary<String, int> columnMapping = new Dictionary<String, int>();
                //for (int i = 0; i < columns.Count; i++)
                //{
                //    columnMapping.Add(columns.ElementAt(i).Key, headers.IndexOf(columns.ElementAt(i).Key));
                //}
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
                foreach (List<String> line in data)
                {
                    String values = "";
                    for (int i = 0; i < line.Count; i++)
                    {
                        if (i == line.Count-1)
                        {
                            values += "'" + line[i] + "'";
                        }
                        else { 
                            values += "'" + line[i] + "'" + ",";
                        }
                        //String column = headers[i];
                        //int index = columnMapping[column];
                        //if (i == line.Count - 1)
                        //{
                        //    values += "'"+line[index]+"'";
                        //}
                        //else
                        //{
                        //    values += "'"+line[index]+"'"+ ",";
                        //}
                        
                    }

                    try
                    {
                        using (SQLiteConnection c = new SQLiteConnection("Data Source=C:\\Users\\diego\\CsvToSqlite\\CsvToSqlite.db;"))
                        {
                            c.Open();
                            using (SQLiteCommand command = new SQLiteCommand("INSERT INTO CsvToSqlite(" + columnNames + ") VALUES(" + values + ")", c))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    catch (SQLiteException err)
                    {
                        LogToFile(DateTime.Now + " CRITICAL ERROR: Could not connect to database " + this.datapath + ".Quitting...");
                        if (!logBasic())
                        {
                            LogToFile("Error Message\n:" + err.ToString());
                        }
                        this.Stop();
                    }
                    

                }
           
        }

        protected override void OnStop()
        {
            
            LogToFile(DateTime.Now + " CsvToSqlite service has stopped");
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
