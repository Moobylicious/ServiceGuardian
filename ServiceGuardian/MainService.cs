
using NLog;
using ServiceGuardian.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using Topshelf;

namespace ServiceGuardian
{
    public class MainService
    {
        private static readonly Logger _log =
        LogManager.GetCurrentClassLogger();
        private readonly MonitoredServices _serviceCollection;
        private readonly int _killTimeInMins;
        private HostControl _hostControl;

        public MainService(MonitoredServices serviceCollection, int killTimeInMins)
        {
            _serviceCollection = serviceCollection;
            _killTimeInMins = killTimeInMins;
        }

        ServiceMonitor _monitor;

        public bool Start(HostControl hostControl)
        {
            _hostControl = hostControl;
            if (!StopMonitor())
            {
                _log.Error("Attempt to re-start, but Could not stop existing service cleanly!");
                throw new Exception("Failed to stop service");
            }

            _log.Info("Starting Service");
            foreach(var svc in _serviceCollection)
            {
                var s = svc as MonitoredService;
                _log.Info($"Will monitor {s.ServiceName}");
            }
            _log.Debug("Creating new Monitor");
            _log.Debug("Starting Monitor");
            if (!StartMonitor())
            {
                _log.Error("Could not start monitor.");
                throw new Exception("Failed to Start service");
            }
            return true;
        }

        public void Stop()
        {
            _log.Info("Attempting to stop service");
            if (StopMonitor())
            {
                _log.Info("Service Stopped");
            }
            else
            {
                _log.Error("Could not stop service cleanly!");
                throw new Exception("Failed to stop service");
            }
        }


        bool StartMonitor()
        {            
            if (_killTimeInMins > 0)
            {
                var killTime = DateTime.Now.AddMinutes(_killTimeInMins);
                _log.Warn($"Service monitoring app will stop at {killTime:O}");
                _monitor = new ServiceMonitor(_log, killTime, _hostControl);
            }
            else 
            {
                _log.Info($"Service monitoring will continue indefinitely");
                _monitor = new ServiceMonitor(_log, _hostControl);
            }

            _monitor.Start(_serviceCollection);

            var timeoutTime = DateTime.Now.AddSeconds(5);
            while (_monitor.State != ServiceMonitorState.Monitoring && DateTime.Now < timeoutTime)
            {
                //hang around waiting to stop...
                Thread.Sleep(100);
            }

            if (_monitor.State != ServiceMonitorState.Monitoring)
            {
                _log.Error("Service Monitor could not start!");
                return false;

            }
            _log.Debug("Monitor Started");
            return true;

        }

        bool StopMonitor()
        {
            if (_monitor != null)
            {
                _log.Debug("Stopping Monitor...");

                _monitor.Stop();
                var timeoutTime = DateTime.Now.AddSeconds(5);
                while (_monitor.State != ServiceMonitorState.Stopped && DateTime.Now < timeoutTime)
                {
                    //hang around waiting to stop...
                    Thread.Sleep(100);
                }

                if (_monitor.State != ServiceMonitorState.Stopped)
                {
                    _log.Error("Service Monitor could not stop!");
                    return false;

                }
                _log.Debug("Monitor Stopped");
            }
            return true;
        }

    }


    internal enum ServiceMonitorState
    {
        Stopped,
        Starting,
        Monitoring,
        Stopping,
        Doomed
    }

    internal class ServiceMonitor
    {
        private readonly Logger _log;

        public ServiceMonitor(Logger log, HostControl hostControl)
        {
            _log = log;
            _hostControl = hostControl;
        }

        public ServiceMonitor(Logger log, DateTime killTime, HostControl hostControl)
        {
            _log = log;
            _killtime = killTime;
            _hostControl = hostControl;
        }

        private DateTime? _killtime = null;
        private readonly HostControl _hostControl;
        private ServiceMonitorState _state;
        public ServiceMonitorState State
        {
            get
            {
                return _state;
            }
            private set
            {
                WithLock(() => _state = value);
            }
        }

        object _locker = new object();
        void WithLock(Action lockAction)
        {
            lock (_locker)
            {
                lockAction();
            }
        }

        private class ServiceInfo
        {
            public string ServiceName { get; set; }
            public string InstallCommandLine { get; set; }
            public int MonitorInterval { get; set; }
            public DateTime NextCheck { get; set; }
        }


        private Thread _processingThread;

        private List<ServiceInfo> _services;

        public void Start(MonitoredServices serviceCollection)
        {

            if (_processingThread != null && _processingThread.ThreadState == ThreadState.Running)
            {
                _log.Error("Cannot start, thread still running!");
                State = ServiceMonitorState.Doomed;
                return;
            }


            State = ServiceMonitorState.Starting;

            _services = new List<ServiceInfo>();

            foreach( var svc in serviceCollection)
            {
                var s = svc as MonitoredService;

                var installCmd = string.Empty;
                if (!string.IsNullOrEmpty(s.InstallCommandLine))
                {
                    installCmd = s.InstallCommandLine;
                }
                else if (!string.IsNullOrEmpty(s.ServiceExecutablePath))
                {
                    //topshelf service.  install with 'install' param.
                    installCmd = $"{s.ServiceExecutablePath} install -servicename:{s.ServiceName} --autostart";
                }


                _services.Add(new ServiceInfo
                {
                    ServiceName = s.ServiceName,
                    InstallCommandLine = installCmd,
                    MonitorInterval = s.CheckFrequencyValue,
                    NextCheck = DateTime.Now.AddSeconds(s.CheckFrequencyValue)
                });
            }

            _processingThread = new Thread(new ThreadStart(MonitorServices));

            _processingThread.Start();

        }

        public void Stop()
        {
            if (State != ServiceMonitorState.Stopped)
            {
                State = ServiceMonitorState.Stopping;
            }
        }

        //Main thread method
        public void MonitorServices()
        {
            State = ServiceMonitorState.Monitoring;

            try
            {
                while (State == ServiceMonitorState.Monitoring)
                {
                    if (_killtime.HasValue && DateTime.Now > _killtime.Value)
                    {
                        _log.Warn($"Self-terminate time has been reached.  app will now stop");
                        Stop();
                    }

                    var servicesToCheck = _services.Where(s => s.NextCheck <= DateTime.Now);

                    if (servicesToCheck.Any())
                    {
                        foreach(var svc in servicesToCheck)
                        {
                            //ensure we stop soon as if requested.
                            if (State != ServiceMonitorState.Monitoring) continue;

                            CheckService(svc);

                            svc.NextCheck = DateTime.Now.AddSeconds(svc.MonitorInterval);
                        }
                    }

                    var nextCheck = servicesToCheck.OrderBy(s => s.NextCheck).FirstOrDefault()?.NextCheck;

                    //sleep for a short while to avoid eating CPU....
                    Thread.Sleep(500);
                }//Main While loop.

                //If we've come out of this, then we've stopped.
                State = ServiceMonitorState.Stopped;

            }
            catch(Exception ex)
            {
                State = ServiceMonitorState.Doomed;
                _log.Error($"exception raised While Monitoring! - {ex.Message}");
            }
            //Attempt to stop the service completely.
            _hostControl.Stop();
        }

        private void CheckService(ServiceInfo svc)
        {
            _log.Debug($"Looking for service named {svc.ServiceName}");

            var sc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == svc.ServiceName);            
            if (sc == null)
            {
                _log.Info($"Service {svc.ServiceName} does not seem to exist.  Will attempt to create.");
                if (!string.IsNullOrEmpty(svc.InstallCommandLine))
                {
                    _log.Debug($"Trying to start service {svc.ServiceName} with Cmd line: >>>{svc.InstallCommandLine}<<<");
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/C {svc.InstallCommandLine}";
                    process.StartInfo = startInfo;
                    _log.Debug("Attempting to run process to create svc");
                    process.Start();
                    _log.Debug("Waiting for process to complete");
                    process.WaitForExit();
                    _log.Info($"Create Service command completed with exit code {process.ExitCode}");
                }
                sc = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == svc.ServiceName);
            }

            if (sc == null)
            {
                _log.Error($"Could not find or create service {svc.ServiceName}");
                return;
            }

            try
            {
                if (sc.Status == ServiceControllerStatus.Stopped)
                {
                    _log.Info($"Service {svc.ServiceName} was stopped, attempting to start.");
                    sc.Start();
                    _log.Debug($"Service { svc.ServiceName} start request made.");
                }
            }
            catch(Exception ex)
            {
                _log.Error($"Error trying to start service! - {ex.Message}");
            }
        }
    }
}
