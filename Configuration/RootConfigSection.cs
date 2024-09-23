using System.Configuration;
using System.Reflection;

namespace MigrateDocuments.Configuration
{
    public class RootConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("Connection", IsRequired = true)]
        public EWConnectionConfigElement Connection => (EWConnectionConfigElement)this["Connection"];
        public System.Version AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version;
        public string AssemblyName => Assembly.GetExecutingAssembly().GetName().Name;
    }
}
