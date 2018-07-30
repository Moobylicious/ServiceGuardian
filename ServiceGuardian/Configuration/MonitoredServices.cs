using System.Configuration;

namespace ServiceGuardian.Configuration
{
    public class MonitoredServices : ConfigurationElementCollection
    {
        public MonitoredService this[int index]
        {
            get
            {
                return base.BaseGet(index) as MonitoredService;
            }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                this.BaseAdd(index, value);
            }
        }

        public new MonitoredService this[string responseString]
        {
            get { return (MonitoredService)BaseGet(responseString); }
            set
            {
                if (BaseGet(responseString) != null)
                {
                    BaseRemoveAt(BaseIndexOf(BaseGet(responseString)));
                }
                BaseAdd(value);
            }
        }
        protected override ConfigurationElement CreateNewElement()
        {
            return new MonitoredService();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MonitoredService)element).ServiceName;
        }
    }
}
