using System.Configuration;

namespace MigrateDocuments.Configuration
{
    public class EWConnectionConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("url", IsRequired = true)]
        public string Url => (string)this["url"];

        [ConfigurationProperty("userName", IsRequired = true)]
        public string UserName => (string)this["userName"];

        [ConfigurationProperty("password", IsRequired = true)]
        public string Password => (string)this["password"];

        [ConfigurationProperty("allowBackupField", IsRequired = false)]
        public string AllowBackupField => (string)this["allowBackupField"];
    }
}
