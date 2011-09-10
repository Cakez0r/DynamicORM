using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicORM_Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            //Deploy script for example database can be found in SQL\TestCreate.sql

            //Update this to use your connection string.
            DynamicORM.DynamicORM db = new DynamicORM.DynamicORM("Data Source=localhost\\SQLExpress; Initial Catalog=DynamicORM; User Id=DynamicORM; Password=password123;");

            //Clear the table via an SQL command
            db.Command("DELETE FROM People");

            //Add a new entry via stored procedure
            //This stored procedure is called INS_Person and takes an nchar(32) parameter called Name
            IEnumerable<dynamic> result = db.StoredProcedure("INS_Person", new { Name = "Dave" });
            Console.WriteLine("Inserted a new person with ID {0}.", result.First().PersonID);

            //Insert a bunch of people with random names
            for (int i = 0; i < 10; i++)
            {
                db.StoredProcedure("INS_Person", new { Name = Guid.NewGuid().ToString().Substring(0, 8) });
            }

            //Display all people in the table via an SQL command
            foreach (dynamic person in db.Command("SELECT * FROM People"))
            {
                Console.WriteLine("ID: {0} - Name: {1} - LastLogin: {2}", person.PersonID, person.Name.TrimEnd(), person.LastLogin);
            }

            Console.ReadKey();
        }
    }
}
