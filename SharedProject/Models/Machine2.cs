
using System.Collections.Generic;

namespace SharedProject.Models
{
    public class Machine2
    {
        public int id { get; set; }
        public string name { get; set; }
        public string host_type { get; set; }
        public string host_name { get; set; }
        public string blank { get; set; }
        public string comment { get; set; }


        static public List<Machine2> Query(string mysql_query, string db)
        {
            var dbConnect = new DBConnect(db);
            var result_arr = dbConnect.Select<Machine2>(mysql_query);
            return result_arr;
        }

    }



}
