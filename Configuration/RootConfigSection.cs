using System.Configuration;

namespace MigrateDocuments.Configuration
{
    public class RootConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("Connection", IsRequired = true)]
        public EWConnectionConfigElement Connection => (EWConnectionConfigElement)this["Connection"];
    }
}
