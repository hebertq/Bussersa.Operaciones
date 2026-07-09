using System;
using System.Collections.Generic;

namespace Modelo.Comercial
{
    public class JobDescription
    {
        public int id { get; set; }
        public string title { get; set; } = "";
        public string tab_icon { get; set; } = "";
        public string department { get; set; } = "";
        public string reports_to { get; set; } = "";
        public string supervises { get; set; } = "";
        public string shift { get; set; } = "";
        public string employment_type { get; set; } = "";
        public string objective { get; set; } = "";
        public string education { get; set; } = "";
        public string experience { get; set; } = "";
        public string technical_knowledge { get; set; } = "";
        public string tools_languages { get; set; } = "";
        public string horary { get; set; } = "";
        public string epp_requirements { get; set; } = "";
        public string risks { get; set; } = "";
        public List<string> essential_functions { get; set; } = new();
        public List<string> occasional_functions { get; set; } = new();
        public List<string> competencies { get; set; } = new();
        public List<string> kpis { get; set; } = new();
    }
}
