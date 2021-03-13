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
using Newtonsoft.Json;

namespace CsvToSqlite
{
    public partial class CsvToSqlite : ServiceBase
    {
        private String homepath;
        private String watchpath;
        private String datapath;
        private String parserConfigFile;
        private Dictionary<String, Object> parserConfig;
        private FileSystemWatcher watcher;
        private String connString;
        private SQLiteConnection conn;


        public CsvToSqlite()
        {
           
            InitializeComponent();

        }


        protected override void OnStart(string[] args)
        {
            
            if (ConfigurationManager.AppSettings.Get("homepath").Equals(""))
            {
                LogToFile("INFO: Config file paramater 'homepath' not set. Setting to C:\\Users\\diego\\CsvToSqlite");
                this.homepath = "C:\\Users\\diego\\CsvToSqlite";
                if (!Directory.Exists(homepath))
                {
                    Directory.CreateDirectory(homepath);
                }
            }
            else
            {
                this.homepath = ConfigurationManager.AppSettings.Get("homepath");
                if (!Directory.Exists(homepath))
                {
                    Directory.CreateDirectory(homepath);
                }
            }
            if (ConfigurationManager.AppSettings.Get("watchdirectory").Equals(""))
            {
                LogToFile("INFO: Config file paramater 'watchdirectory' not set. Setting to C:\\Users\\diego\\CsvToSqlite\\Convert");
                this.watchpath = "C:\\Users\\diego\\CsvToSqlite\\Convert";
                if (!Directory.Exists(watchpath))
                {
                    Directory.CreateDirectory(watchpath);
                }
                
            }
            else
            {
                this.watchpath = ConfigurationManager.AppSettings.Get("watchdirectory");
                if (!Directory.Exists(watchpath))
                {
                    Directory.CreateDirectory(watchpath);
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
                this.parserConfigFile = ConfigurationManager.AppSettings.Get("watchdirectory");
                if (!Directory.Exists(this.parserConfigFile))
                {
                    LogToFile(DateTime.Now + " CRITICAL ERROR: Could not find file " + this.parserConfigFile+". Quiting ...");

                }
                ConfigurationManager.AppSettings.Set("watchdirectory", watchpath);

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
                    this.Stop();
                }
                this.datapath = ConfigurationManager.AppSettings.Get("databasePath");
            }
            if ((!ConfigurationManager.AppSettings.Get("logginglevel").Equals("basic")) && (!ConfigurationManager.AppSettings.Get("logginglevel").Equals("advanced")))
            {
                LogToFile(DateTime.Now + " ERROR: Could not parse App.config key \'logginglevel\', value must be set to \'basic\' or \'advanced\'. Setting logginglevel to \'basic\'");
                ConfigurationManager.AppSettings.Set("logginglevel", "basic");
            }
            if (Directory.Exists(this.homepath + "\\Logs"))
            {
                LogToFile("INFO: Creating " + this.homepath + "\\Logs");
                Directory.CreateDirectory(this.homepath + "\\Logs");
            }
            try { 
                this.parserConfig = JsonConvert.DeserializeObject<Dictionary<String, Object>>(File.ReadAllText(this.parserConfigFile));
            }
            catch (IOException err)
            {
                LogToFile(DateTime.Now + " CRITICAL ERROR: Could not read "+this.parserConfigFile);
                if (!logBasic())
                {
                    LogToFile("Error Message\n:" + err.ToString());
                }
                this.Stop();
            }
            catch (Newtonsoft.Json.JsonException err) { }
            {

            }
            this.conn = new SQLiteConnection("Data Source=C:\\Users\\diego\\CsvToSqlite\\CsvToSqlite.db;");
            this.conn.Open();
            
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
        private void OnCreated(object source, FileSystemEventArgs e)
        {
            try { 
                SQLiteCommand cmd = new SQLiteCommand(this.conn);
                String filename = e.FullPath;
                Parser parser = new Parser(filename);
                Dictionary<String, Object> csv = parser.Parse();
                
                if (Parser.hasDuplicate((List<String>)csv["headers"]))
                {
                    String distinctColumns = "";
                    for (int i = 0; i < ((List<String>)csv["headers"]).Distinct().Count(); i++) { 
                        distinctColumns += ((List<String>)csv["headers"]).Distinct().ToArray()[i]+", ";
                    }
                    LogToFile(DateTime.Now + " PARSE ERROR: Found duplicates for columns "+ distinctColumns + ". Stopping ...");
                    return;
                }

                
                foreach (List<String> line in (List<List<String>>)csv["data"])
                {
                    foreach (String letter in line) {
                        LogToFile(letter+"\n");
                    }
                    cmd.CommandText = "INSERT INTO CsvToSqlite(Age, Email, Name) VALUES('"+line[0]+"','"+line[1]+ "','" + line[2]+"')";
                    cmd.ExecuteNonQuery();

                }
            }
            catch (SQLiteException err)
            {
                LogToFile(DateTime.Now + " ERROR: Could not connect to database. Please make sure it is not being used by another program");
                if (!logBasic()) {
                    LogToFile("Error Message:\n" + err.ToString());
                }
            }
        }

        protected override void OnStop()
        {
            this.conn.Close();

            LogToFile(DateTime.Now + " CsvToSqlite service has stopped");
        }
        public void LogToFile(string Message)
        {
            string filepath = this.homepath + "\\Logs\\log.txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
