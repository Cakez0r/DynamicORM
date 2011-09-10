using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;

namespace DynamicORM
{
    /// <summary>
    /// The result set of a SqlCommand
    /// </summary>
    internal class ResultSet : IDisposable, IEnumerator<object>, IEnumerable<object>
    {
        /// <summary>
        /// The reader to be used for the command
        /// </summary>
        SqlDataReader m_reader;

        /// <summary>
        /// The command to execute
        /// </summary>
        SqlCommand m_command;

        /// <summary>
        /// The connection to execute the command on
        /// </summary>
        SqlConnection m_connection;


        /// <summary>
        /// Begins executing a SQL command and evaluating the result set
        /// </summary>
        /// <param name="connectionString">The connection string to execute the command on</param>
        /// <param name="command">The command to execute</param>
        public ResultSet(string connectionString, string commandText) : this(connectionString, commandText, null) { }


        /// <summary>
        /// Begins executing a SQL command and evaluating the result set
        /// </summary>
        /// <param name="connectionString">The connection string to execute the command on</param>
        /// <param name="command">The command to execute</param>
        /// <param name="parameters">The parameters for this command</param>
        internal ResultSet(string connectionString, string commandText, SqlParameter[] parameters)
        {
            //Check the connection string
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("The specified connection string was null or empty. A valid connection string is necessary to execute a command.");
            }

            //Create and open the connection
            m_connection = new SqlConnection(connectionString);
            m_connection.Open();

            //Create the command
            m_command = new SqlCommand(commandText, m_connection);

            //Add parameters to the command, if there are any
            if (parameters != null)
            {
                foreach (SqlParameter parameter in parameters)
                {
                    //Parameters must be cloned. Cannot belong on more than one query at once.
                    m_command.Parameters.Add(new SqlParameter(parameter.ParameterName, parameter.Value));
                }
                m_command.CommandType = CommandType.StoredProcedure;
            }

            //Execute the command
            m_reader = m_command.ExecuteReader();
        }


        /// <summary>
        /// Return the current result for this enumerator
        /// </summary>
        object IEnumerator.Current
        {
            get { return this.Current; }
        }


        /// <summary>
        /// Return the current result for this enumerator
        /// </summary>
        public dynamic Current
        {
            get;
            private set;
        }


        /// <summary>
        /// Advance to the next result
        /// </summary>
        /// <returns>True if the enumerator advanced to the next result, or false if there are no more results.</returns>
        public bool MoveNext()
        {
            if (m_reader.Read())
            {
                //Create a new dynamic object for each row in the result set
                dynamic results = new ExpandoObject();

                //Cast the dynamic to an IDictionary, so that we can late-bind fields on it
                IDictionary<string, object> resultsDictionary = (IDictionary<string, object>)results;

                for (int i = 0; i < m_reader.FieldCount; i++)
                {
                    //For each column in the row, add a new field onto the dynamic object
                    string key = m_reader.GetName(i);
                    object value = m_reader.GetValue(i);

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

                //Set the current object of this enumerator to this result
                Current = results;

                return true;
            }

            //No more results!
            return false;
        }


        /// <summary>
        /// Don't allow resetting
        /// </summary>
        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Get this result enumerator
        /// </summary>
        /// <returns>This enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }


        /// <summary>
        /// Get this result enumerator
        /// </summary>
        /// <returns>This enumerator</returns>
        public IEnumerator<dynamic> GetEnumerator()
        {
            return this;
        }


        /// <summary>
        /// Dispose of this result set enumerator
        /// </summary>
        public void Dispose()
        {
            /*
             * The Close method fills in the values for output parameters, return values and RecordsAffected, increasing the time that it takes to close a SqlDataReader 
             * that was used to process a large or complex query. When the return values and the number of records affected by a query are not significant, the time 
             * that it takes to close the SqlDataReader can be reduced by calling the Cancel method of the associated SqlCommand object before calling the Close method.
             */
            m_command.Cancel();

            m_reader.Dispose();
            m_command.Dispose();
            m_connection.Dispose();
        }
    }
}
