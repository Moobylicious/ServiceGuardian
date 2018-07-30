using System.Configuration;

namespace ServiceGuardian.Configuration
{
    public class MonitoredServicesConfig : ConfigurationSection
    {
        public static MonitoredServicesConfig GetConfig()
        {
            return (MonitoredServicesConfig)ConfigurationManager.GetSection("MonitoredServices")
                ?? new MonitoredServicesConfig();
        }

        [ConfigurationProperty("Services")]
        [ConfigurationCollection(typeof(MonitoredServices), AddItemName ="add")]
        public MonitoredServices MonitoredServices
        {
            get
            {
                object o = this["Services"];
                return o as MonitoredServices;
            }
        }
    }
}
