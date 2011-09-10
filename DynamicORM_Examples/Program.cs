using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace DynamicORM_Examples
{
    class Program
    {
        //Update this to use your connection string.
        static DynamicORM.DynamicORM db = new DynamicORM.DynamicORM("Data Source=localhost\\SQLExpress; Initial Catalog=DynamicORM; User Id=DynamicORM; Password=password123;");

        static void Main(string[] args)
        {
            //Deploy script for example database can be found in SQL\TestCreate.sql

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

            //Fire an event when a property changes on a person
            //This is a little convoluted, but works because the dynamic is actually an ExpandoObject
            dynamic dave = db.Command("SELECT TOP 1 * FROM People WHERE [Name] = 'Dave'").First();
            ((INotifyPropertyChanged)dave).PropertyChanged += (dynamic sender, PropertyChangedEventArgs e) =>
                {
                    IDictionary<string, object> daveProperties = (IDictionary<string, object>)dave;
                    db.Command(string.Format("UPDATE People SET {0} = '{1}'", e.PropertyName, daveProperties[e.PropertyName]));
                }; 

            //Update dave's last login, which will cause the value to update in the database;
            dave.LastLogin = DateTime.Now - TimeSpan.FromDays(1);

            Console.WriteLine("Done! Press any key to exit.");

            Console.ReadKey();
        }
    }
}
