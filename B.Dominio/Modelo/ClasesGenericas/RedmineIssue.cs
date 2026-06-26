using System.Collections.Generic;

namespace Modelo.ClasesGenericas
{
    public class RedmineRequest
    {
        public RedmineIssue issue { get; set; } = new();
    }

    public class RedmineIssue
    {
        public string project_id { get; set; } = "";
        public int tracker_id { get; set; }
        public string subject { get; set; } = "";
        // Nueva propiedad para la fecha de inicio
        public string? start_date { get; set; }

        // También podrías necesitar la fecha de vencimiento
        public string? due_date { get; set; }
        public string description { get; set; } = "";
        public List<RedmineUpload>? uploads { get; set; }
        public List<RedmineCustomField> custom_fields { get; set; } = new();
    }

    public class RedmineCustomField
    {
        public int id { get; set; }
        public string value { get; set; } = "";
    }

    public class RedmineUpload
    {
        public string token { get; set; } = "";
        public string filename { get; set; } = "";
        public string content_type { get; set; } = "application/pdf";
    }
}

