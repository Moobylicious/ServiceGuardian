using System.Configuration;

namespace ServiceGuardian.Configuration
{
    public class MonitoredService : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string ServiceName
        {
            get
            {
                return this["name"] as string;
            }
        }

        [ConfigurationProperty("path", IsRequired = false)]
        public string ServiceExecutablePath
        {
            get
            {
                return this["path"] as string;
            }
        }

        //Install command line, if not specified defaults to Topshelf
        [ConfigurationProperty("install", IsRequired = false)]
        public string InstallCommandLine
        {
            get
            {
                return this["install"] as string;
            }
        }


        [ConfigurationProperty("checkFrequency", IsRequired =false)]
        public string CheckFrequency
        {
            get
            {
                return this["checkFrequency"] as string;
            }
        }

        public int CheckFrequencyValue
        {
            get
            {
                if (!int.TryParse(CheckFrequency, out var settingValue))
                {
                    settingValue = 60;
                }
                return settingValue;
            }
        }

    }
}
