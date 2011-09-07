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

            return YieldCommandResults(command);
        }


        /// <summary>
        /// Execute a stored procedure on the open connection
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">Object containing all the parameters of the stored procedure. Property names must match parameter names of the stored procedure.</param>
        /// <returns>An IEnumerable of dynamics that are populated with one row from the result set.</returns>
        public IEnumerable<dynamic> StoredProcedure(string procedureName, object parameters)
        {
            SqlCommand command = new SqlCommand(procedureName, m_connection);
            command.CommandType = CommandType.StoredProcedure;

            //Build procedure parameters from the parameters object
            Dictionary<string, object> parameterNameValues = ObjectToDictionary(parameters);

            foreach (KeyValuePair<string, object> parameter in parameterNameValues)
            {
                SqlParameter sqlParameter = new SqlParameter(parameter.Key, parameter.Value);
                command.Parameters.Add(sqlParameter);
            }

            return YieldCommandResults(command);
        }


        /// <summary>
        /// Execute a SqlCommand on the database and yield the rows from the result set one by one.
        /// </summary>
        /// <param name="command">The command to execute</param>
        private IEnumerable<dynamic> YieldCommandResults(SqlCommand command)
        {
            //Execute the command
            SqlDataReader reader = command.ExecuteReader();

            //Start pulling rows
            while (reader.Read())
            {
                //Create a new dynamic object for each row in the result set
                dynamic results = new ExpandoObject();

                //Cast the dynamic to an IDictionary, so that we can late-bind fields on it
                IDictionary<string, object> resultsDictionary = (IDictionary<string, object>)results;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    //For each column in the row, add a new field onto the dynamic object
                    string key = reader.GetName(i);
                    object value = reader.GetValue(i);

                    if (resultsDictionary.ContainsKey(key))
                    {
                        //If the dynamic already has something for this column name then overwrite the value
                        resultsDictionary[key] = value;
                    }
                    else
                    {
                        resultsDictionary.Add(key, value);
                    }
                }

                yield return results;
            }

            //Close the reader when we are done.
            reader.Close();

            //TODO: Having this function yield rows causes massive problems with the
            //reader being closed in a timely manner. This will need to be addressed.
        }


        /// <summary>
        /// Converts an object into a dictionary of the object's property fields -> values
        /// </summary>
        /// <param name="obj">The object to convert</param>
        private static Dictionary<string, object> ObjectToDictionary(object obj)
        {
            //TODO: Show this function some caching love, if possible.

            Dictionary<string, object> dictionary = new Dictionary<string, object>();

            Type type = obj.GetType();

            //Find all public properties on the object
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo property in properties)
            {
                //Add each property field and its value into the dictionary
                string name = property.Name;
                object value = property.GetValue(obj, null);

                dictionary.Add(name, value);
            }

            return dictionary;
        }
    }
}
