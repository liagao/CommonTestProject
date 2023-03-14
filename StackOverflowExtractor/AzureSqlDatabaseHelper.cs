namespace StackOverflowExtractor
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;

    public static class AzureSqlDatabaseHelper
    {
        /// <summary>  
        /// Connects to the database, reads,  
        /// prints results to the console.  
        /// </summary>  
        static public void AccessDatabase()
        {
            //throw new TestSqlException(4060); //(7654321);  // Uncomment for testing.  

            using (var sqlConnection = new SqlConnection(GetSqlConnectionString()))
            {
                using (var dbCommand = sqlConnection.CreateCommand())
                {
                    dbCommand.CommandText = @"  
    SELECT TOP 3  
        ob.name,  
        CAST(ob.object_id as nvarchar(32)) as [object_id]  
      FROM sys.objects as ob  
      WHERE ob.type='IT'  
      ORDER BY ob.name;";

                    sqlConnection.Open();
                    var dataReader = dbCommand.ExecuteReader();

                    while (dataReader.Read())
                    {
                        Console.WriteLine("{0}\t{1}",
                          dataReader.GetString(0),
                          dataReader.GetString(1));
                    }
                }
            }
        }

        public static void InsertTable(IList<SearchResultItem> results)
        {
            using (var sqlConnection = new SqlConnection(GetSqlConnectionString()))
            {
                sqlConnection.Open();
                foreach (var searchResultItem in results)
                {
                    using (var cmd = sqlConnection.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO Documents VALUES(@param1,@param2,@param3,@param4,@param5)";

                        cmd.Parameters.AddWithValue("@param1", 3);
                        cmd.Parameters.AddWithValue("@param2", searchResultItem.ScrapeUrl);
                        cmd.Parameters.AddWithValue("@param3", searchResultItem.TiTle);
                        cmd.Parameters.AddWithValue("@param4", searchResultItem.Text);
                        cmd.Parameters.AddWithValue("@param5", searchResultItem.UpdateDateTime);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>  
        /// You must edit the four 'my' string values.  
        /// </summary>  
        /// <returns>An ADO.NET connection string.</returns>  
        private static string GetSqlConnectionString()
        {
            // Prepare the connection string to Azure SQL Database.  
            var sqlConnectionSb = new SqlConnectionStringBuilder();

            // Change these values to your values.  
            sqlConnectionSb.DataSource = "partnerdribot.database.windows.net"; //["Server"]  
            sqlConnectionSb.InitialCatalog = "PartnerDRIBot"; //["Database"]  

            sqlConnectionSb.UserID = "XAPBot";  // "@yourservername"  as suffix sometimes.  
            sqlConnectionSb.Password = "PartnerDRI_2016";
            sqlConnectionSb.IntegratedSecurity = false;

            // Adjust these values if you like. (ADO.NET 4.5.1 or later.)  
            sqlConnectionSb.ConnectRetryCount = 3;
            sqlConnectionSb.ConnectRetryInterval = 10;  // Seconds.  

            // Leave these values as they are.  
            sqlConnectionSb.IntegratedSecurity = false;
            sqlConnectionSb.Encrypt = true;
            sqlConnectionSb.ConnectTimeout = 30;

            return sqlConnectionSb.ToString();
        }
    }
}
