using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Appendix.A
{
    public static class Listings
    {
        public static void HigherOrderFunctios()
        {
            Func<int, double> fCos = n => Math.Cos((double)n);

            double x = fCos(5);

            IEnumerable<double> values = Enumerable.Range(1, 10).Select(fCos);
        }

        public static void HigherOrderFunctiosAndLambda(string path)
        {
            string text;
            using (var stream = new StreamReader(path))
            {
                text = stream.ReadToEnd();
            }
        }


        public static R Using<T, R>(this T item, Func<T, R> func) where T : IDisposable
        {
            using (item)
                return func(item);
        }

        public static void HigherOrderFunctiosAndLambdaReusableCode(string path)
        {
            string text = new StreamReader(path).Using(stream => stream.ReadToEnd());
        }

        public static void LambdaExpressionWithSameBehavior()
        {
            Func<int, int, int> add1 = delegate (int x, int y) { return x + y; };
            Func<int, int, int> add2 = (int x, int y) => { return x + y; };
            Func<int, int, int> add3 = (x, y) => x + y;
        }

        public static void CurringFunctions()
        {
            Func<int, int, int> add = (x, y) => x + y;
            Func<int, Func<int, int>> curriedAdd = x => y => x + y;

            Func<int, int> increament = curriedAdd(1);

            int a = increament(30);
            int b = increament(41);


            Func<int, int> add30 = curriedAdd(30);
            int c = add30(12);
        }

        public static Func<A, Func<B, R>> Curry<A, B, R>(this Func<A, B, R> function)
        {
            return a => b => function(a, b);
        }

        public static void CurryingHelperExtenion()
        {
            Func<int, int, int> add = (x, y) => x + y;
            Func<int, Func<int, int>> curriedAdd = add.Curry();
        }

        public static Func<A, B, R> Uncurry<A, B, R>(Func<A, Func<B, R>> function)
                                      => (x, y) => function(x)(y);

        static Func<B, R> Partial<A, B, R>(this Func<A, B, R> function, A argument)
                             => argument2 => function(argument, argument2);


        public static void PartiallyAppliedFunctions()
        {
            Func<int, int, int> add = (x, y) => x + y;

            Func<int, int, int> max = Math.Max;
            Func<int, int> max5 = max.Partial(5);

            int a = max5(8);
            int b = max5(2);
            int c = max5(12);
        }

        static string ReadText(string filePath) => File.ReadAllText(filePath);

        public static void PartiallyAppliedFunctionExample()
        {
            string filePath = "TextFile.txt";
            Func<string> readText = () => ReadText(filePath);

            string text = readText.Retry();
        }

        public static void PartiallyAppliedExtensionMethodExample(string filePath)
        {
            Func<string, string> readText = (path) => ReadText(path);

            // string text = readText.Retry();   Error!!
            // string text = readText(filePath).Retry();   Error!!

            string text = readText.Partial("TextFile.txt").Retry();

            Func<string, Func<string>> curriedReadText = readText.Curry();

            text = curriedReadText("TextFile.txt").Retry();
        }

        public static Func<R> Partial<T, R>(this Func<T, R> function, T arg)
        {
            return () => function(arg);
        }

        public static Func<T, Func<R>> Curry<T, R>(this Func<T, R> function)
        {
            return arg => () => function(arg);
        }
    }

    public static class PartiallyAppliedRetryFunction
    {
        public static T Retry<T>(this Func<T> function)
        {
            int retry = 0;
            T result = default(T);
            bool success = false;
            do
            {
                try
                {
                    result = function();
                    success = true;
                }
                catch
                {
                    retry++;
                }
            } while (!success && retry < 3);
            return result;
        }
    }
}
