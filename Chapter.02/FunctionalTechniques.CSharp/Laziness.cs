using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace FunctionalTechniques.CSharp
{
    // Listing 2.26 Lazy initialization of the Person object
    public class Person //#A
    {
        public readonly string FullName; //#B

        public Person(string firstName, string lastName)
        {
            FullName = firstName + " " + lastName;
            Console.WriteLine(FullName);
        }

        public static void RunDemo()
        {
            Console.WriteLine("Listing 2.26 Lazy initialization of the Person object");
            var fredFlintstone =
                new Lazy<Person>(() => new Person("Fred", "Flintstone"), true); //#B

            var freds = new Person[5]; //#C
            for (var i = 0; i < freds.Length; i++) //#D
                freds[i] = fredFlintstone.Value; //#E
        }
    }

    // Listing 2.27 Singleton pattern using Lazy<T>
    public sealed class Singleton
    {
        private static readonly Lazy<Singleton> lazy =
            new Lazy<Singleton>(() => new Singleton(), true); //#A

        private Singleton()
        {
        }

        public static Singleton Instance => lazy.Value;
    }

    internal class Laziness
    {
        private static readonly string cmdText = null;
        private static readonly SqlConnection conn = null;

        // Listing 2.29 Lazy asynchronous operation to initialize the Person object
        private readonly Lazy<Task<Person>> person =
            new Lazy<Task<Person>>(async () => // #A
            {
                using (var cmd = new SqlCommand(cmdText, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var firstName = reader["first_name"].ToString();
                        var lastName = reader["last_name"].ToString();
                        return new Person(firstName, lastName);
                    }
                }

                throw new Exception("Failed to fetch Person");
            });

        private async Task<Person> FetchPerson()
        {
            return await person.Value; // #B
        }
    }
}