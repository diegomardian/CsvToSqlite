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
namespace CsvToSqlite
{
    public partial class CsvToSqlite : ServiceBase
    {
        private String homepath;
        private String watchpath;
        private String datapath;
        public FileSystemWatcher watcher;
        public String connString;
        public SQLiteConnection conn;
        public CsvToSqlite()
        {
           
            InitializeComponent();

        }


        protected override void OnStart(string[] args)
        {
            
            if (ConfigurationManager.AppSettings.Get("homepath").Equals(""))
            {
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
            if (ConfigurationManager.AppSettings.Get("databasePath").Equals(""))
            {
                this.datapath = "C:\\Users\\diego\\CsvToSqlite\\CsvToSqlite.db";
                if (!File.Exists(datapath))  
                {
                    LogToFile((DateTime.Now + " Critical Error: Could not find database path: "+datapath);
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
            this.conn = new SQLiteConnection(ConfigurationManager.AppSettings.Get("databasePath"));
            this.conn.Open();
            if ((!ConfigurationManager.AppSettings.Get("logginglevel").Equals("basic")) && (!ConfigurationManager.AppSettings.Get("logginglevel").Equals("complex")))
            {
                LogToFile(DateTime.Now + " Error: Could not parse App.config key \'logginglevel\', value must be set to \'basic\' or \'complex\'. Setting logginglevel to \'basic\'");
                ConfigurationManager.AppSettings.Set("logginglevel", "basic");
            }
            this.watcher = new FileSystemWatcher(this.watchpath, "*.*");
            watcher.Created += OnCreated;
            watcher.EnableRaisingEvents = true;
            LogToFile(DateTime.Now + " CsvToSqlite service has started");



        }
        private void OnCreated(object source, FileSystemEventArgs e)
        {
            String filename = e.FullPath;
            String contents = File.ReadAllText(filename);
            foreach (String line in contents.Split('\n'))
            {
                LogToFile(line.Replace("\n", "") + ";");
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
