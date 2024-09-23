using MigrateDocuments.Configuration;
using System.Configuration;

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
