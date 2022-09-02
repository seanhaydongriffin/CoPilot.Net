

namespace SharedProject.Models
{
    public class Project
    {
        public int id { get; set; }
        public string path { get; set; }
        public string schema_name { get; set; }
        public string external_app { get; set; }
        public string external_id { get; set; }
        public string external_name { get; set; }
        public string webhook_url { get; set; }
        public string run_reports_url { get; set; }
    }
}
