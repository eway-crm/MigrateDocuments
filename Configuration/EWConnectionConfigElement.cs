using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

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
    }
}
