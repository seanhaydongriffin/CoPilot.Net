
namespace SharedProject
{
    class MySQL
    {
        static private DBConnect dbConnect;


        static public void Insert(string mysql_query, string db)
        {
            dbConnect = new DBConnect(db);
            dbConnect.Insert(mysql_query);
        }

        static public void Update(string mysql_query, string db)
        {
            dbConnect = new DBConnect(db);
            dbConnect.Update(mysql_query);
        }

        static public void Delete(string mysql_query, string db)
        {
            dbConnect = new DBConnect(db);
            dbConnect.Delete(mysql_query);
        }


    }
}
