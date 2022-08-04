
using SharedProject;
using System.Collections.Generic;
using System.Windows.Controls;

namespace SharedProject.Models
{
    public class ExecutionGroup2
    {
        public int id { get; set; }
        public string name { get; set; }
        public string iteration { get; set; }
        public string test_environment { get; set; }
        public System.DateTime start_date_time { get; set; }
        public System.DateTime end_date_time { get; set; }
        public string result { get; set; }


        static public List<ExecutionGroup2> Query(string mysql_query, string db)
        {
            var dbConnect = new DBConnect(db);
            var result_arr = dbConnect.Select<ExecutionGroup2>(mysql_query);
            return result_arr;
        }

        static public void QueryToListView(ListView lv, string mysql_query, string db)
        {
            var result = Query(mysql_query, db);
            lv.ItemsSource = null;
            lv.ItemsSource = result;
        }


    }
}
