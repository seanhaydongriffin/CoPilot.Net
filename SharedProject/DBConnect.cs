using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Windows;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Collections;
using System.IO;
using System.Diagnostics;

namespace SharedProject
{
    class DBConnect
    {

        private MySqlConnection connection;
        private string server;
        private string _database;
        private string uid;
        private string password;

        //Constructor
        public DBConnect(string database)
        {
            _database = database;
            Initialize();
        }

        //Initialize values
        private void Initialize()
        {
            server = AppConfig.Get("DatabaseHostname");                             // "workstation54";
            //database =                                                            // "control_automation_machine";
            uid = AppConfig.Get("DatabaseUsername");                                // "auto";
            password = StringCipher.Decrypt(AppConfig.Get("DatabasePassword"));     // "janison";
            string connectionString = "SERVER=" + server + ";" + "DATABASE=" + _database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            Log.Debug("connectionString = " + connectionString);
            connection = new MySqlConnection(connectionString);
            Log.Debug("connection.State.ToString() = " + connection.State.ToString());
        }

        //open connection to database
        private bool OpenConnection()
        {
            try
            {
                Log.Debug("connection.Open()");
                connection.Open();
                Log.Debug("connection.State.ToString() = " + connection.State.ToString());
                return true;
            }
            catch (MySqlException ex)
            {
                //When handling errors, you can your application's response based 
                //on the error number.
                //The two most common error numbers when connecting are as follows:
                //0: Cannot connect to server.
                //1045: Invalid user name and/or password.

                Log.Debug(ex.ToString());

                switch (ex.Number)
                {
                    case 0:
                        var msg = "Error. Cannot connect to server.  Contact administrator";
                        Log.Debug(msg);
                        //App.CurrentStatusBarText.Text = msg;
                        break;

                    case 1045:
                        var msg2 = "Error. Invalid username/password, please try again";
                        Log.Debug(msg2);
                        //App.CurrentStatusBarText.Text = msg2;
                        break;
                }
                return false;
            }
        }

        //Close connection
        private bool CloseConnection()
        {
            try
            {
                Log.Debug("connection.Open()");
                connection.Close();
                Log.Debug("connection.State.ToString() = " + connection.State.ToString());
                return true;
            }
            catch (MySqlException ex)
            {
                Log.Debug(ex.Message);
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        //Insert statement
        public long Insert(string query)
        {
            long LastInsertedId = -1;

            Log.Debug("DBConnect.Insert(\"" + query + "\"");

            //open connection
            if (this.OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //Execute command
                cmd.ExecuteNonQuery();

                LastInsertedId = cmd.LastInsertedId;

                //close connection
                this.CloseConnection();
            }

            return LastInsertedId;
        }

        //Insert statement with parameterisation
        public long Insert(string query, Hashtable parameters)
        {
            long LastInsertedId = -1;

            Log.Debug("DBConnect.Insert(\"" + query + "\"");

            //open connection
            if (this.OpenConnection() == true)
            {
                //create command and assign the query and connection from the constructor
                MySqlCommand cmd = new MySqlCommand(query, connection);

                // Add the parameters
                foreach (DictionaryEntry parameter in parameters)

                    cmd.Parameters.AddWithValue(parameter.Key.ToString(), parameter.Value);

                //Execute command
                cmd.ExecuteNonQuery();

                LastInsertedId = cmd.LastInsertedId;

                //close connection
                this.CloseConnection();
            }

            return LastInsertedId;
        }

        //Update statement
        public bool Update(string query)
        {
            Log.Debug("DBConnect.Update(\"" + query + "\"");

            //Open connection
            if (this.OpenConnection() == true)
            {
                //create mysql command
                MySqlCommand cmd = new MySqlCommand();
                //Assign the query using CommandText
                cmd.CommandText = query;
                //Assign the connection using Connection
                cmd.Connection = connection;

                //Execute query
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection();

                return true;
            }

            return false;
        }

        //Update statement with parameterisation
        public void Update(string query, Hashtable parameters)
        {
            Log.Debug("DBConnect.Update(\"" + query + "\"");

            //Open connection
            if (this.OpenConnection() == true)
            {
                //create mysql command
                MySqlCommand cmd = new MySqlCommand();

                //Assign the query using CommandText
                cmd.CommandText = query;

                // Add the parameters
                foreach (DictionaryEntry parameter in parameters)

                    cmd.Parameters.AddWithValue(parameter.Key.ToString(), parameter.Value);

                //Assign the connection using Connection
                cmd.Connection = connection;

                //Execute query
                cmd.ExecuteNonQuery();

                //close connection
                this.CloseConnection();
            }

        }

        //Delete statement
        public bool Optimize(string query)
        {
            Log.Debug("DBConnect.Optimize(\"" + query + "\"");

            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
                return true;
            }

            return false;
        }

        //Delete statement
        public bool Delete(string query)
        {
            Log.Debug("DBConnect.Delete(\"" + query + "\"");

            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);
                cmd.ExecuteNonQuery();
                this.CloseConnection();
                return true;
            }

            return false;
        }

        //Delete statement with parameterisation
        public void Delete(string query, Hashtable parameters)
        {
            Log.Debug("DBConnect.Delete(\"" + query + "\"");

            if (this.OpenConnection() == true)
            {
                MySqlCommand cmd = new MySqlCommand(query, connection);

                // Add the parameters
                foreach (DictionaryEntry parameter in parameters)

                    cmd.Parameters.AddWithValue(parameter.Key.ToString(), parameter.Value);

                cmd.ExecuteNonQuery();
                this.CloseConnection();
            }
        }


        public List<T> Select<T>(string query) where T : new()
        {
            Log.Debug("DBConnect.Select(\"" + query + "\")");

            List<T> res = new List<T>();

            //Open connection
            if (this.OpenConnection() == true)
            {

                MySqlCommand q = new MySqlCommand(query, connection);
                MySqlDataReader r = q.ExecuteReader();

                while (r.Read())
                {
                    T t = new T();

                    for (int inc = 0; inc < r.FieldCount; inc++)
                    {
                        Type type = t.GetType();
                        PropertyInfo prop = type.GetProperty(r.GetName(inc));

                        if (r.GetValue(inc) != DBNull.Value)

                            prop.SetValue(t, r.GetValue(inc), null);
                    }

                    res.Add(t);
                }
                r.Close();
                this.CloseConnection();
            }
            else
            {
                return null;
            }

            return res;

        }



        public List<T> Select<T>(string query, Hashtable parameters) where T : new()
        {
            Log.Debug("DBConnect.Select(\"" + query + "\")");

            List<T> res = new List<T>();

            //Open connection
            if (this.OpenConnection() == true)
            {

                MySqlCommand q = new MySqlCommand(query, connection);

                // Add the parameters
                foreach (DictionaryEntry parameter in parameters)

                    q.Parameters.AddWithValue(parameter.Key.ToString(), parameter.Value);

                MySqlDataReader r = q.ExecuteReader();

                while (r.Read())
                {
                    T t = new T();

                    for (int inc = 0; inc < r.FieldCount; inc++)
                    {
                        Type type = t.GetType();
                        PropertyInfo prop = type.GetProperty(r.GetName(inc));

                        if (r.GetValue(inc) != DBNull.Value)

                            prop.SetValue(t, r.GetValue(inc), null);
                    }

                    res.Add(t);
                }
                r.Close();
                this.CloseConnection();
            }
            else
            {
                return null;
            }

            return res;

        }



        public ObservableCollection<T> SelectToObservableCollection<T>(string query) where T : new()
        {
            Log.Debug("DBConnect.Select(\"" + query + "\"");

            ObservableCollection<T> res = new ObservableCollection<T>();

            //Open connection
            if (this.OpenConnection() == true)
            {

                MySqlCommand q = new MySqlCommand(query, connection);
                MySqlDataReader r = q.ExecuteReader();

                while (r.Read())
                {
                    T t = new T();

                    for (int inc = 0; inc < r.FieldCount; inc++)
                    {
                        Type type = t.GetType();
                        PropertyInfo prop = type.GetProperty(r.GetName(inc));

                        if (r.GetValue(inc) != DBNull.Value)

                            prop.SetValue(t, r.GetValue(inc), null);
                    }

                    res.Add(t);
                }
                r.Close();
                this.CloseConnection();
            }
            else
            {
                return null;
            }

            return res;

        }


        public ObservableCollection<T> SelectToObservableCollection<T>(string query, Hashtable parameters) where T : new()
        {
            Log.Debug("DBConnect.Select(\"" + query + "\"");

            ObservableCollection<T> res = new ObservableCollection<T>();

            //Open connection
            if (this.OpenConnection() == true)
            {

                MySqlCommand q = new MySqlCommand(query, connection);

                // Add the parameters
                foreach (DictionaryEntry parameter in parameters)

                    q.Parameters.AddWithValue(parameter.Key.ToString(), parameter.Value);

                MySqlDataReader r = q.ExecuteReader();

                while (r.Read())
                {
                    T t = new T();

                    for (int inc = 0; inc < r.FieldCount; inc++)
                    {
                        Type type = t.GetType();
                        PropertyInfo prop = type.GetProperty(r.GetName(inc));

                        if (r.GetValue(inc) != DBNull.Value)

                            prop.SetValue(t, r.GetValue(inc), null);
                    }

                    res.Add(t);
                }
                r.Close();
                this.CloseConnection();
            }
            else
            {
                return null;
            }

            return res;

        }


        //Select statement
        //public string[][] Select(string query)
        //{
        //    string[][] Texts = null;

        //    try
        //    {
        //        //Open connection
        //        if (this.OpenConnection() == true)
        //        {
        //            //Create Command
        //            MySqlCommand cmd = new MySqlCommand(query, connection);
        //            MySqlDataReader dataReader = cmd.ExecuteReader();
        //            DataTable Result = new DataTable();
        //            Result.Load(dataReader);

        //            if (Result.Rows.Count > 0)
        //            {
        //                object[][] Objects = Result.AsEnumerable().Select(x => x.ItemArray).ToArray();
        //                Texts = new string[Objects.Length][];
        //                for (int i = 0; i < Texts.Length; i++)
        //                {
        //                    Texts[i] = new string[Objects[i].Length];
        //                    for (int j = 0; j < Objects[i].Length; j++)
        //                    {
        //                        Texts[i][j] = Objects[i][j].ToString();
        //                    }
        //                }
        //                //return Texts;
        //            }
        //            else
        //            {
        //                return null;
        //            }


        //            //close Data Reader
        //            dataReader.Close();

        //            //close Connection
        //            this.CloseConnection();
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }

        //    return Texts;

        //}







        //public List<string>[] Select(string query)
        //{
        //    try
        //    {
        //        //string query = "SELECT * FROM tableinfo";

        //        //Create a list to store the result
        //        List<string>[] list = new List<string>[3];
        //        list[0] = new List<string>();
        //        list[1] = new List<string>();
        //        list[2] = new List<string>();

        //        //Open connection
        //        if (this.OpenConnection() == true)
        //        {
        //            //Create Command
        //            MySqlCommand cmd = new MySqlCommand(query, connection);
        //            //Create a data reader and Execute the command
        //            MySqlDataReader dataReader = cmd.ExecuteReader();

        //            //Read the data and store them in the list
        //            while (dataReader.Read())
        //            {
        //                list[0].Add(dataReader["id"] + "");
        //                list[1].Add(dataReader["name"] + "");
        //                list[2].Add(dataReader["age"] + "");
        //            }

        //            //close Data Reader
        //            dataReader.Close();

        //            //close Connection
        //            this.CloseConnection();

        //            //return list to be displayed
        //            return list;
        //        }
        //        else
        //        {
        //            return list;
        //        }
        //    } catch (Exception e)
        //    {
        //        return null;
        //    }

        //    return null;

        //}

        //Count statement
        public int Count()
        {
            string query = "SELECT Count(*) FROM tableinfo";
            int Count = -1;

            //Open Connection
            if (this.OpenConnection() == true)
            {
                //Create Mysql Command
                MySqlCommand cmd = new MySqlCommand(query, connection);

                //ExecuteScalar will return one value
                Count = int.Parse(cmd.ExecuteScalar() + "");

                //close Connection
                this.CloseConnection();

                return Count;
            }
            else
            {
                return Count;
            }
        }

        //Backup

        public void Backup(string root_uid, string root_password)
        {
            try
            {
                DateTime Time = DateTime.Now;
                int year = Time.Year;
                int month = Time.Month;
                int day = Time.Day;
                int hour = Time.Hour;
                int minute = Time.Minute;
                int second = Time.Second;
                int millisecond = Time.Millisecond;

                //Save file to C:\ with the current date as a filename
                string path;
                path = "R:\\MySqlBackup" + year + "-" + month + "-" + day + "-" + hour + "-" + minute + "-" + second + "-" + millisecond + ".sql";
                StreamWriter file = new StreamWriter(path);


                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "mysqldump";
                psi.RedirectStandardInput = false;
                psi.RedirectStandardOutput = true;
//                psi.Arguments = "--all-databases --single-transaction --quick --lock-tables=false " + string.Format(@"-u{0} -p{1} -h{2} {3}", uid, password, server, database);
                psi.Arguments = "--all-databases --single-transaction --quick --lock-tables=false " + string.Format(@"-u{0} -p{1}", root_uid, root_password);
                psi.UseShellExecute = false;

                Process process = Process.Start(psi);

                string output;
                output = process.StandardOutput.ReadToEnd();
                file.WriteLine(output);
                process.WaitForExit();
                file.Close();
                process.Close();
            }
            catch (IOException ex)
            {
                MessageBox.Show("Error , unable to backup!");
            }
        }

        //Restore
        public void Restore()
        {
        }

    }
}
