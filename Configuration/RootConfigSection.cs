using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace MigrateDocuments.Configuration
{
    public class RootConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("Connection", IsRequired = true)]
        public EWConnectionConfigElement Connection => (EWConnectionConfigElement)this["Connection"];
    }
}
