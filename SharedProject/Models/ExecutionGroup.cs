
namespace SharedProject.Models
{
    public class ExecutionGroup
    {
        public int id { get; set; }
        public string name { get; set; }
        public int iteration_id { get; set; }
        public string iteration_name { get; set; }
        public int environment_id { get; set; }
        public string environment_name { get; set; }
        public int auto_stop_tests { get; set; }
        public int auto_send_results { get; set; }
        public int device_notifications { get; set; }
        public int external_plan_id { get; set; }
        public string external_plan_name { get; set; }
        public int external_exe_rec_run_id { get; set; }
        public string external_exe_rec_run_name { get; set; }
        public System.DateTime start_date_time { get; set; }
        public System.DateTime end_date_time { get; set; }
        public string result { get; set; }



    }
}
