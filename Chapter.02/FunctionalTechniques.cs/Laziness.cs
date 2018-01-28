using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionalTechniques.cs
{
    // Listing 2.26 Lazy initialization of the Person object
    public class Person                     //#A
    {
        public readonly string FullName;    //#B
        public Person(string firstName, string lastName)
        {
            FullName = firstName + " " + lastName;
            Console.WriteLine(FullName);
        }

        public static void RunDemo()
        {
            Console.WriteLine("Listing 2.26 Lazy initialization of the Person object");
            Lazy<Person> fredFlintstone =
                new Lazy<Person>(() => new Person("Fred", "Flintstone"), true); //#B

            Person[] freds = new Person[5];         //#C
            for (int i = 0; i < freds.Length; i++)  //#D
            {
                freds[i] = fredFlintstone.Value;    //#E
            }
        }
    }

    // Listing 2.27 Singleton pattern using Lazy<T>
    public sealed class Singleton
    {
        private static readonly Lazy<Singleton> lazy =
            new Lazy<Singleton>(() => new Singleton(), true); //#A

        public static Singleton Instance => lazy.Value;

        private Singleton()
        { }
    }

    class Laziness
    {
        static string cmdText = null;
        static SqlConnection conn = null;

        // Listing 2.29 Lazy asynchronous operation to initialize the Person object
        Lazy<Task<Person>> person =
            new Lazy<Task<Person>>(async () =>      // #A
            {
                using (var cmd = new SqlCommand(cmdText, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        string firstName = reader["first_name"].ToString();
                        string lastName = reader["last_name"].ToString();
                        return new Person(firstName, lastName);
                    }
                }
                throw new Exception("Failed to fetch Person");
            });

        async Task<Person> FetchPerson()
        {
            return await person.Value;              // #B
        }
    }
}