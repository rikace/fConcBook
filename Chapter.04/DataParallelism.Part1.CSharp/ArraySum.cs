using System.Linq;

namespace DataParallelism.Part1.CSharp
{
    public static class ArraySum
    {
        public static int SequentialSum(int[] data)
        {
            //Listing 4.6 Common For loop
            var sum = 0;
            for (var i = 0; i < data.Length; i++) sum += data[i]; //#A
            return sum;
        }

        public static int SequentialSumLINQ(int[] data)
        {
            return data.Sum();
        }
    }
}