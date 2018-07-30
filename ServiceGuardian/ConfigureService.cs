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

                var services = MonitoredServicesConfig.GetConfig();

                conf.Service<MainService>(svc =>
                {
                    svc.ConstructUsing(s => new MainService(services.MonitoredServices));
                    svc.WhenStarted(s => s.Start());
                    svc.WhenStopped(s => s.Stop());
                });

                conf.RunAsLocalSystem();


                conf.SetStartTimeout(TimeSpan.FromSeconds(5));
            });
        }
    }
}
