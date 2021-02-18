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

namespace CsvToSqlite
{
    public partial class CsvToSqlite : ServiceBase
    {
        private String homepath;
        private String watchpath;
        public FileSystemWatcher watcher;
        public CsvToSqlite()
        {
            
            InitializeComponent();
            
        }


        protected override void OnStart(string[] args)
        {
            

            if (ConfigurationManager.AppSettings.Get("homepath").Equals("") &&
                ConfigurationManager.AppSettings.Get("watchdirectory").Equals(""))
            {
                this.watchpath = "C:\\Users\\diego\\CsvToSqlite\\Convert";
                this.homepath = "C:\\Users\\diego\\CsvToSqlite";
                if (!Directory.Exists(homepath))
                {
                    Directory.CreateDirectory(homepath);
                }

                if (!Directory.Exists(watchpath))
                {
                    Directory.CreateDirectory(watchpath);
                }
                ConfigurationManager.AppSettings.Set("watchdirectory", watchpath);
                ConfigurationManager.AppSettings.Set("homepath", homepath);

            }
            if ((!ConfigurationManager.AppSettings.Get("logginglevel").Equals("basic")) && (!ConfigurationManager.AppSettings.Get("logginglevel").Equals("complex")))
            {
                LogToFile(DateTime.Now+ " *** Error parsing App.config \'logginglevel\', value must be set to \'basic\' or \'complex\'. Setting logginglevel to \'basic\'");
                ConfigurationManager.AppSettings.Set("logginglevel", "basic");
            }
            this.watcher = new FileSystemWatcher(this.watchpath, "*.*");
            watcher.Created += OnCreated;
            watcher.EnableRaisingEvents = true;
            LogToFile(DateTime.Now + " *** CsvToSqlite service has started");



        }
       private void OnCreated(object source, FileSystemEventArgs e) {
            String filename = e.FullPath;
            String contents = File.ReadAllText(filename);
            foreach (String line in contents.Split('\n'))
            {
                LogToFile(line.Replace("\n", "")+";");
            }
        }

        protected override void OnStop()
        {       
            LogToFile(DateTime.Now + " *** CsvToSqlite service has stopped");
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
