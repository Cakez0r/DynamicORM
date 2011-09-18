using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;

namespace DynamicORM
{
    /// <summary>
    /// DynamicORM MSSQL Database interface layer
    /// </summary>
    public class DynamicORM
    {
        /// <summary>
        /// A cached collection of stored procedure parameters.
        /// This is kept to cut down on expensive calls to reflection operations.
        /// </summary>
        Dictionary<int, SqlParameter[]> m_parameterCache = new Dictionary<int, SqlParameter[]>();


        /// <summary>
        /// Gets or sets the default connection string to user
        /// </summary>
        public string ConnectionString
        {
            get;
            set;
        }


        /// <summary>
        /// Initialize with a default connection string
        /// </summary>
        /// <param name="connectionString">The default connection string to use</param>
        public DynamicORM(string connectionString)
        {
            ConnectionString = connectionString;
        }


        /// <summary>
        /// Execute a SQL command
        /// </summary>
        /// <param name="commandText">The command to execute</param>
        /// <returns>The result set of the command</returns>
        public IEnumerable<dynamic> Command(string commandText)
        {
            return Command(commandText, ConnectionString);
        }


        /// <summary>
        /// Execute a SQL command
        /// </summary>
        /// <param name="commandText">The command to execute</param>
        /// <param name="connectionString">The connection string to use for the command</param>
        /// <returns>The result set of the command</returns>
        public IEnumerable<dynamic> Command(string commandText, string connectionString)
        {
            //Execute the command
            ResultSet results = new ResultSet(connectionString, commandText);

            return results;
        }


        /// <summary>
        /// Execute a stored procedure
        /// </summary>
        /// <param name="procedureName">The stored procedure name to execute</param>
        /// <param name="parameters">The parameters to pass to this stored procedure</param>
        /// <returns>The result set of the command</returns>
        public IEnumerable<dynamic> StoredProcedure(string procedureName)
        {
            return StoredProcedure(procedureName, null, ConnectionString);
        }


        /// <summary>
        /// Execute a stored procedure
        /// </summary>
        /// <param name="procedureName">The stored procedure name to execute</param>
        /// <param name="parameters">The parameters to pass to this stored procedure</param>
        /// <returns>The result set of the command</returns>
        public IEnumerable<dynamic> StoredProcedure(string procedureName, object parameters)
        {
            return StoredProcedure(procedureName, parameters, ConnectionString);
        }


        /// <summary>
        /// Execute a stored procedure
        /// </summary>
        /// <param name="procedureName">The stored procedure name to execute</param>
        /// <param name="parameters">The parameters to pass to this stored procedure</param>
        /// <param name="connectionString">The connection string to use for the command</param>
        /// <returns>The result set of the command</returns>
        public IEnumerable<dynamic> StoredProcedure(string procedureName, object parameters, string connectionString)
        {
            //Check to see if we already have the parameters cached
            SqlParameter[] sqlParameters = null;

            if (parameters != null)
            {
                int parametersHashCode = parameters.GetHashCode();
                lock (m_parameterCache)
                {
                    if (m_parameterCache.ContainsKey(parametersHashCode))
                    {
                        //Pull from the cache if we have them
                        sqlParameters = m_parameterCache[parametersHashCode];
                    }
                    else
                    {
                        //If the parameters aren't cached, then build up a dictionary for them
                        sqlParameters = ObjectToSqlParameterList(parameters).ToArray();

                        //And add it in to the cache
                        m_parameterCache.Add(parametersHashCode, sqlParameters);
                    }
                }
            }

            //Execute the command
            ResultSet results = new ResultSet(connectionString, procedureName, sqlParameters);

            return results;
        }


        /// <summary>
        /// Converts an object into a dictionary of the object's property fields -> values
        /// </summary>
        /// <param name="obj">The object to convert</param>
        private static List<SqlParameter> ObjectToSqlParameterList(object obj)
        {
            List<SqlParameter> parameters = new List<SqlParameter>();

            Type type = obj.GetType();

            //Find all public properties on the object
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            foreach (PropertyInfo property in properties)
            {
                //Add each property field and its value into the dictionary
                string name = property.Name;
                object value = property.GetValue(obj, null);

                parameters.Add(new SqlParameter(name, value));
            }

            return parameters;
        }
    }
}
