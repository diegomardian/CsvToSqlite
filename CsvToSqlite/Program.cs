using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace CsvToSqlite
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                CsvToSqlite service1 = new CsvToSqlite();
                service1.StartService();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new CsvToSqlite()
                };
                ServiceBase.Run(ServicesToRun);
            }
            
        }
    }
}
