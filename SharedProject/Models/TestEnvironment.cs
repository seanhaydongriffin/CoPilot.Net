
using System.Collections.Generic;
using System.Windows.Controls;

namespace SharedProject.Models
{
    public class TestEnvironment
    {
        public int id { get; set; }
        public string name { get; set; }
        public string external_id { get; set; }


        static public List<TestEnvironment> Query(string mysql_query, string db)
        {
            var dbConnect = new DBConnect(db);
            var result_arr = dbConnect.Select<TestEnvironment>(mysql_query);
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
