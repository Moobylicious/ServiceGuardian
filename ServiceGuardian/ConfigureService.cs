using ServiceGuardian.Configuration;
using System;
using System.Configuration;
using Topshelf;

namespace ServiceGuardian
{
    internal static class ConfigureService
    {
        internal static void Configure()
        {
            HostFactory.Run(conf =>
            {
                conf.SetServiceName(ConfigurationManager.AppSettings["ServiceName"]);
                conf.SetDisplayName(ConfigurationManager.AppSettings["ServiceDisplayName"]);
                conf.SetDescription(ConfigurationManager.AppSettings["ServiceDescription"]);

                int killTimeLengthInMins = 0;
                var killTimeLengthSetting = ConfigurationManager.AppSettings["StopAfterMins"];
                if (killTimeLengthSetting != null) 
                { 
                    int.TryParse(killTimeLengthSetting, out killTimeLengthInMins);
                }

                var services = MonitoredServicesConfig.GetConfig();

                conf.Service<MainService>(svc =>
                {
                    svc.ConstructUsing(s => new MainService(services.MonitoredServices, killTimeLengthInMins));
                    svc.WhenStarted((s, hf) => s.Start(hf));
                    svc.WhenStopped(s =>s.Stop());
                });

                conf.RunAsLocalSystem();


                conf.SetStartTimeout(TimeSpan.FromSeconds(5));
            });
        }
    }
}
