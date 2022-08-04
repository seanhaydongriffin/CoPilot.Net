
using System.Collections.Generic;
using SharedProject;

namespace SharedProject.Models
{
    public class Project2
    {
        public int id { get; set; }
        public string path { get; set; }
        public string schema_name { get; set; }
        public string external_app { get; set; }
        public string external_id { get; set; }
        public string external_name { get; set; }


        static public List<Project2> Query(string mysql_query, string db)
        {
            var dbConnect = new DBConnect(db);
            var result_arr = dbConnect.Select<Project2>(mysql_query);
            return result_arr;
        }


    }
}
