using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Dynamic;
using System.Data;
using System.Reflection;

namespace DynamicORM
{
    /// <summary>
    /// DynamicORM MSSQL Database interface layer
    /// </summary>
    public class DynamicORM
    {
        SqlConnection m_connection = new SqlConnection();

        /// <summary>
        /// Opens a connection to the database, given a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to use</param>
        /// <returns>Connection success state</returns>
        public bool Connect(string connectionString)
        {
            m_connection.ConnectionString = connectionString;
            m_connection.Open();
            
            return m_connection.State == ConnectionState.Open;
        }

        /// <summary>
        /// Executes a command on the open connection.
        /// </summary>
        /// <param name="commandText">The SQL command to execute.</param>
        /// <returns>An IEnumerable of dynamics that are populated with one row from the result set.</returns>
        public IEnumerable<dynamic> Command(string commandText)
        {
            SqlCommand command = new SqlCommand(commandText, m_connection);
            SqlDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                dynamic results = new ExpandoObject();
                IDictionary<string, object> resultsDictionary = (IDictionary<string, object>)results;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    resultsDictionary.Add(reader.GetName(i), reader.GetValue(i));
                }

                yield return results;
            }
        }
    }
}
