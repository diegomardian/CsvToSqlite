using System.ComponentModel;
using System.Configuration.Install;

namespace CsvToSqlite
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}