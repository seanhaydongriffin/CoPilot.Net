

namespace SharedProject.Models
{
    public class Machine
    {
        public int id { get; set; }
        public string name { get; set; }
        public string host_type { get; set; }
        public string host_name { get; set; }
        public string blank { get; set; }
        public string comment { get; set; }
        public string run_ids { get; set; }
        public bool? run_auto_archive_script_logs { get; set; }
        public string source_control_args { get; set; }

    }
}
