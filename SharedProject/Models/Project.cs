
using System.Collections.Generic;

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


        static public List<Project> Query(string mysql_query, string db)
        {
            var dbConnect = new DBConnect(db);
            var result_arr = dbConnect.Select<Project>(mysql_query);
            return result_arr;
        }


    }
}
