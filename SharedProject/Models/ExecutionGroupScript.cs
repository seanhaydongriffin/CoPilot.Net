

namespace SharedProject.Models
{
    public class ExecutionGroupScript
    {
        public int? id { get; set; }
        public string host_name { get; set; }
        public int script_id { get; set; }
        public string script_name { get; set; }
        public string selector { get; set; }
        public string post_run_delay { get; set; }
        public string state { get; set; }
        public bool? excluded { get; set; }
        public System.DateTime? end_date_time { get; set; }
        public string em_comment { get; set; }
        public string shared_folder_1 { get; set; }
        public int order_id { get; set; }
        public bool? in_progress { get; set; }



    }
}
