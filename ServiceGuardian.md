# ServiceGuardian Overview

Simple app to monitor, re-start and install services.

# Installation

This is a topshelf service.  To install simply put binaries in a folder somewhere and run:

`c:\path\to\svc\>ServiceGuardian.exe install`

# Configuration

## Basic Configuration

These settings control how this service appears in Services in Windows:

```
  <appSettings>
    <add key="ServiceName" value="PlaysafePerformanceGuardian"/>
    <add key="ServiceDisplayName" value="PlaysafePerformanceGuardian"/>
    <add key="ServiceDescription" value="Playsafe Performance Guardian Service - Helps ensure other services are running"/>    
  </appSettings>
```

## Monitoring Services

Which services to monitor are controlled via the `MonitoredServices` section.
Topshelf-based services are assumed (can be installed via `service.exe install` command line)
Or the install command line can be overridden to use windows SC command instead.
```
  <MonitoredServices>
    <Services>      
      <!-- TopShelf service with automatic install, check every 5 seconds.-->
      <add name="TestSvc" path="d:\path\to\exe\testSvc.exe" checkFrequency="5"/>
      
      <!-- Monitored only service, never installed. default check frequency (60s)-->
      <add name="QuantumApi"/>

      <!-- service installed via SC command line.  Note: Doesn't usually work unless running as an app with admin rights! -->
      <add 
        name="AnotherTestService2" 
        path="d:\temp\testSvc\testSvc.exe" 
        install="sc create AnotherTestService binPath= d:\temp\testsvc\testsvc.exe"
        checkFrequency="7"
        />      
    </Services>
  </MonitoredServices>
```

### Options for each service:
* `name` - the service name (required)
* `path` - the path to the executable (optional) - if there, installation is attempted 
  via topshelf default `install`
* `install` - FULL command line to install service if not topshelf.
* `checkFrequency` - how frequently (in seconds) to check that the service is running & attempt to 
  install (if not there at all) and start the service.
  
# Notes
* a service with Just a name will only monitor - not attempt to install.
* service running as Local System (the default) should have permissions to install and start services. 
* will default to checking every 60 seconds if not specified for a service.
* will do one service at a time, but should get through them all when due;  This may cause a longer delay between 
  checks of one service if a different service took a while to install/start up.