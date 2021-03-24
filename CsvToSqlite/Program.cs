using System;
using System.ServiceProcess;

namespace CsvToSqlite
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            //if (Environment.UserInteractive)
            //{
            //    var service1 = new CsvToSqlite();
            //    service1.StartService();
            //}
            //else
            //{
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new CsvToSqlite()
            };
            ServiceBase.Run(ServicesToRun);
            //}
        }
    }
}