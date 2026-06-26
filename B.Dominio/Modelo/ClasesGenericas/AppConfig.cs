using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modelo.ClasesGenericas
{
    public class AppConfig
    {
        public string ApplicationName { get; set; }
        public ConnectionStringsConfig ConnectionStrings { get; set; }
        public AppSettings AppSettingsKey { get; set; }
        public AppReportInfo ReportInfo { get; set; }
        public AppApiLogInfo ApiLogInfo { get; set; }
        public AppSettingsSW ConfigService { get; set; }
        public AppSettingsSoap ConfigSoap { get; set; }
        public JobConfigBpm Job { get; set; }
        public SchedulerConfig Scheduler { get; set; }
        public class ConnectionStringsConfig
        {
            public List<InfoContex> Contexto { get; set; }
        }

        public class SchedulerConfig
        {
            public List<JobConfig> SchedConfig { get; set; }
        }
        public class JobConfigBpm
        {
            public List<JobConfig> JobBpmConfig { get; set; }
        }
        public class AppSettingsSoap
        {
            public List<InfoiSoap> Soap { get; set; }
        }

        public class AppSettings
        {
            public string key { get; set; }
            public string iv { get; set; }
            public string sis { get; set; }
        }

        public class InfoiSoap
        {
            public string EndpointAddress { get; set; }
            public string BasicHttpBinding { get; set; }
            public string Name { get; set; }
        }

        public class AppSettingsSW
        {
            public int HoraInicio { get; set; }
            public int HoraFin { get; set; }
            public int TiempoEspera { get; set; }
            public string BathName { get; set; }
            public string ServiceName { get; set; }
        }

        public class AppReportInfo
        {
            public string user { get; set; }
            public string key { get; set; }
            public string dominio { get; set; }
            public string server { get; set; }
        }

        public class AppApiLogInfo
        {
            public string baseUrl { get; set; }
            public string name { get; set; }
            public string baseUrlRedmine { get; set; }
            public string apiKeyRedmine { get; set; }
        }

        public class Host
        {
            public Dictionary<string, Endpoint> Endpoints { get; set; }
        }

        public class JobConfig
        {           
            public string name { get; set; }
            public string group { get; set; }
            public string config { get; set; }
            public string description { get; set; }
        }

        public class Endpoint
        {
            public bool IsEnabled { get; set; }
            public string Address { get; set; }
            public int Port { get; set; }
            public Certificate Certificate { get; set; }
        }

        public class Certificate
        {
            public string Source { get; set; }
            public string Path { get; set; }
            public string Password { get; set; }
        }
    }
}

