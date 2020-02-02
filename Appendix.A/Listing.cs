using System;
using System.IO;
using System.Linq;

namespace Appendix.A
{
    public static class Listings
    {
        public static void HigherOrderFunctios()
        {
            Func<int, double> fCos = n => Math.Cos(n);

            var x = fCos(5);

            var values = Enumerable.Range(1, 10).Select(fCos);
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
            {
                return func(item);
            }
        }

        public static void HigherOrderFunctiosAndLambdaReusableCode(string path)
        {
            var text = new StreamReader(path).Using(stream => stream.ReadToEnd());
        }

        public static void LambdaExpressionWithSameBehavior()
        {
            Func<int, int, int> add1 = delegate(int x, int y) { return x + y; };
            Func<int, int, int> add2 = (x, y) => { return x + y; };
            Func<int, int, int> add3 = (x, y) => x + y;
        }

        public static void CurringFunctions()
        {
            Func<int, int, int> add = (x, y) => x + y;
            Func<int, Func<int, int>> curriedAdd = x => y => x + y;

            var increament = curriedAdd(1);

            var a = increament(30);
            var b = increament(41);


            var add30 = curriedAdd(30);
            var c = add30(12);
        }

        public static Func<A, Func<B, R>> Curry<A, B, R>(this Func<A, B, R> function)
        {
            return a => b => function(a, b);
        }

        public static void CurryingHelperExtenion()
        {
            Func<int, int, int> add = (x, y) => x + y;
            var curriedAdd = add.Curry();
        }

        public static Func<A, B, R> Uncurry<A, B, R>(Func<A, Func<B, R>> function)
        {
            return (x, y) => function(x)(y);
        }

        private static Func<B, R> Partial<A, B, R>(this Func<A, B, R> function, A argument)
        {
            return argument2 => function(argument, argument2);
        }


        public static void PartiallyAppliedFunctions()
        {
            Func<int, int, int> add = (x, y) => x + y;

            Func<int, int, int> max = Math.Max;
            var max5 = max.Partial(5);

            var a = max5(8);
            var b = max5(2);
            var c = max5(12);
        }

        private static string ReadText(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public static void PartiallyAppliedFunctionExample()
        {
            var filePath = "TextFile.txt";
            Func<string> readText = () => ReadText(filePath);

            var text = readText.Retry();
        }

        public static void PartiallyAppliedExtensionMethodExample(string filePath)
        {
            Func<string, string> readText = path => ReadText(path);

            // string text = readText.Retry();   Error!!
            // string text = readText(filePath).Retry();   Error!!

            var text = readText.Partial("TextFile.txt").Retry();

            var curriedReadText = readText.Curry();

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
            var retry = 0;
            T result = default;
            var success = false;
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