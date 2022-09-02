using System.Windows.Controls;

namespace SharedProject
{
    public static class ListViewEx
    {



        public static void FromQuery<T>(this ListView lv, string mysql_query, string db) where T : new()
        {
            var dbConnect = new DBConnect(db);
            var result = dbConnect.Select<T>(mysql_query, false);
            lv.ItemsSource = null;
            lv.ItemsSource = result;
        }


    }
}
