using MigrateDocuments.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateDocuments
{
    public static class Config
    {
        private static RootConfigSection _config;

        static Config()
        {
            _config = (RootConfigSection)ConfigurationManager.GetSection("MigrateDocuments");
        }

        public static RootConfigSection Instance => _config;
    }
}
