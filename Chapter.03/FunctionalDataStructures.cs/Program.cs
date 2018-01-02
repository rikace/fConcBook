using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace PersistentDataStructures
{
    class Program
    {
        // Listing 3.1 SignalR hub in C# that registers connections in context
        class Listing31
        {
            static ConcurrentDictionary<Guid, string> onlineUsers = new ConcurrentDictionary<Guid, string>(); //#A

    //      public override Task OnConnected()
    //      {
    //          string connectionId = new Guid(Context.ConnectionId); //#B
    //          System.Security.Principal.IPrincipal user = Context.User;
    //          string userName;
    //          if (!onlineUsers.TryGetValue(connectionId, out userName))
    //          { //#C
    //              RegisterUserConnection(connectionId, user.Identity.Name);
    //              onlineUsers.Add(connectionId, user.Identity.Name); //#D
    //          }
    //          return base.OnConnected();
    //      }
    //      public override Task OnDisconnected()
    //      {
    //          string connectionId = new Guid(Context.ConnectionId);
    //          string userName;
    //          if (onlineUsers.TryGetValue(connectionId, out userName))
    //          { //#C
    //              DeregisterUserConnection(connectionId, userName);
    //              onlineUsers.Remove(connectionId); //#D
    //          }
    //          return base.OnDisconnected();
            }
        
        static void Main(string[] args)
        {
            // Listing 3.2 Constructing BCL immutable collections
            var list = ImmutableList.Create<int>(); //#A
            list = list.Add(1); //#B
            list = list.Add(2);
            list = list.Add(3);

            var builder = ImmutableList.CreateBuilder<int>(); //#C
            builder.Add(1); //#D
            builder.Add(2);
            builder.Add(3);
            list = builder.ToImmutable(); //#E


            // Listing 3.14 A functional list in C#
            FList<int> list1 = FList<int>.Empty;
            FList<int> list2 = list1.Cons(1).Cons(2).Cons(3);
            FList<int> list3 = FList<int>.Cons(1, FList<int>.Empty);
            FList<int> list4 = list2.Cons(2).Cons(3);

            Demo.PrintSeparator();
            Console.WriteLine("Listing 3.15 Lazy list implementation");
            var lazyList1 =
                new LazyList<int>(42, new Lazy<LazyList<int>>(()=>
                    new LazyList<int>(21, LazyList<int>.Empty)));
            var lazyList =
                lazyList1.Append(
                    new LazyList<int>(3, LazyList<int>.Empty));
            lazyList.Iterate(Console.WriteLine);

            Demo.PrintSeparator();

            Console.WriteLine("Listing 3.16 Immutable B-tree representation");
            var tree =
                new BinaryTree<int>(20,
                    new BinaryTree<int>(9,
                        new BinaryTree<int>(4,
                            new BinaryTree<int>(2, null, null),
                            null),
                        new BinaryTree<int>(10, null, null)),
                    null);

            var exist9 = tree.Contains(9);
            Console.WriteLine($"Tree contains 9 : {exist9}");
            var tree21 = tree.Insert(21);
            var exist21 = tree21.Contains(21);
            Console.WriteLine($"Tree21 contains 21 : {exist21}\n");

            tree.InOrder(Console.WriteLine);

            Console.ReadLine();
        }
    }
}