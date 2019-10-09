# Changelog

## 1.0.0 - 2019.10.10
- Target .NET Standard 2.0.
- Remove ElasticSearch and InfluxDB reporters from the library.
- Remove `MetricsConfig.WithHttpEndpoint()` feature from the library.
- Switch to SDK-style project format and dotnet core build tooling.
- Use [Nerdbank.GitVersioning](https://github.com/AArnott/Nerdbank.GitVersioning) to automate generation of assembly 
  and nuget package versions.

## 0.4.7 - 2018.05.11
* the Graphite and text file reporters now work with configurable error tolerance
* improved memory allocations in timers: the internal meter no longer records user values (@SkyterX)

## 0.4.6-pre / 2016-12-06
* reporting can now be configured to be error tolerant (Metrics.Reports.ToleratedConsecutiveFailures config key)
* an exception is now thrown during startup if an http endpoint is configured more than once

## 0.4.5-pre / 2016-12-02
* disabling metrics now properly works with config extensions
* performance counters are no longer being registered when metrics are disabled

## 0.4.4 / 2016-12-02
* health checks can now be updated at runtime (@bronsh)
* fixed the 404 response of core metrics http listener

## 0.4.3-pre / 2016-10-09
* Nancy.Metrics adapter: support for endpoint reports
* Owin.Metrics adapter: support for endpoint reports
* decoupled endpoint reports from http listener implementation
* graphite can now be disabled by omitting config entries (@slawwan)

## 0.4.2-pre / 2016-09-18
* graphite report can now be configured from app.config file (@slawwan)
* fixed owin adapter endpoints content type header (@glennular)

## 0.4.1-pre / 2016-07-24
* fixed issue with timer metric not using the uservalue when marking the interal meter (@epeshk)
* updated liblog to the latest version (@tsibelman)

## 0.4.0-pre / 2016-06-20
* support for configuring endpoint reports

## 0.3.7 / 2016-05-31
* fixed AppEnvironment.Current when Assembly.GetEntryAssembly() returns null

## 0.3.6 / 2016-05-29
* fixed timer total time unit info in human readable report (@Liwoj)
* support for reporting health checks to elasticsearch (@AmirSasson)
* AppEnvironment now contains EntryAssembly name and version

## 0.3.5 / 2016-04-24
* support rolling index for elasticsearch (@AmirSasson)
* fixed backwards compatibility with elasticsearch (@andrepnh)
* fixed graphite report sending incorrect values for meters (@AlistairClark7)
* fixed string formatting in HealthCheckResult (@bregger)

First 0.3.x stable release, contains all changes in previous 0.3.x-pre versions.

Note: The HDR sampling implementation is in beta stage, some issues might still be present. The default sampling type remains ExponentiallyDecaying for now.

Note for 0.2.x users: SamplingType.FavourRecent has been renamed to SamplingType.ExponentiallyDecaying.

## 0.3.4-pre / 2016-03-22
* support multiple, separate http endpoints
* initialize http endpoints async
* fixed compatibility with elasticsearch 2.x
* global context name can be retrieved from runtime environment

## 0.3.3-pre / 2015-06-300
* introduce configurable Default sampling type

## 0.3.2-pre / 2015-04-29
* better meter performance (on recording path meter has same performance as a counter)
* default to using HdrHistogram port reservoir for histogram and timer metrics ( greatly reduces contention and increases performance )
* remove context Merging as the latest changes remove most of the contention on the recording path

## 0.3.1-pre / 2015-04-17
* update packages & fix nuget package dependencies versions

## 0.3.0-pre / 2015-04-17
* use ConcurrentUtils StripedLongAdder for better performance with concurrent counter
* provide macros for config value for Metrics.GlobalContextName
* performance improvements - reduce memory allocations
* initial support for hdrhistogram (fully synchronized version)
* context merging functionality (will probably be removed after switching to hdr histogram)
* pre-release for now until the hdr histogram port is complete and integrated

## 0.2.16 / 2015-03-19
* retry starting httplistener on failure (better support for asp.net hosting where app pools are recycled)

## 0.2.15 / 2015-03-03
* fix disposal of httplistener (@nkot)
* Added Process CPU usage into app counters (@o-mdr)
* resharper cleanup
* update dependencies

## 0.2.14 / 2014-12-15
* fix possible issue when metrics are disabled and timer returns null TimerContext

## 0.2.13 / 2014-12-14
* fix error when trying to globally disable metrics
* first elasticsearch bits

## 0.2.11 / 2014-11-16
* graphite adapter (early bits, might have issues)
* refactor & cleanup reporting infra

## 0.2.10 / 2014-11-06
* fix error logging for not found performance counters

## 0.2.9 / 2014-11-05
* record active sessions for timers

## 0.2.8 / 2014-10-29
* handle access issues for perf counters

## 0.2.7 / 2014-10-28
* preparations for out-of-process metrics

## 0.2.6 / 2014-10-17
* fix http listener prefix handling

## 0.2.5 / 2014-10-12
* JSON metrics refactor
* remote metrics 

## 0.2.4 / 2014-10-07
* JSON version
* added environment 
* various fixes

## 0.2.3 / 2014-10-01
* add support for set counter & set meters [details](https://github.com/etishor/Metrics.NET/issues/21)
* cleanup owin adapter
* better & more resilient error handling

## 0.2.2 / 2014-09-27
* add support for tagging metrics (not yet used in reports or visualization)
* add support for suppling a string user value to histograms & timers for tracking min / max / last values
* tests cleanup, some refactoring

## 0.2.1 / 2014-09-25
* port latest changes from original metrics lib
* port optimization from ExponentiallyDecayingReservoir (https://github.com/etishor/Metrics.NET/commit/1caa9d01c16ff63504612d64771d52e9d7d9de5e)
* other minor optimizations
* add gauges for thread pool stats

## 0.2.0 / 2014-09-20
* implement metrics contexts (and child contexts)
* make config more friendly
* most used condig options are now set by default
* add logging based on liblog (no fixed dependency - automaticaly wire into existing logging framework)
* update nancy & owin adapters to use contexts
* add some app.config settings to ease configuration

## 0.1.11 / 2014-08-18
* update to latest visualization app (fixes checkboxes being outside dropdown)
* fix json caching in IE
* allow defining custom names for metric registry

## 0.1.10 / 2014-07-30
* fix json formating (thanks to Evgeniy Kucheruk @kpoxa)

## 0.1.9 / 2014-07-04
* make reporting more extensible

## 0.1.8
* remove support for .NET 4.0

## 0.1.6
* for histograms also store last value
* refactor configuration ( use Metric.Config.With...() )
* add option to completely disable metrics Metric.Config.CompletelyDisableMetrics() (useful for measuring metrics impact)
* simplify health checks
