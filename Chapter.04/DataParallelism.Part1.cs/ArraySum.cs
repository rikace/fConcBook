using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataParallelism.cs
{
    public static class ArraySum
    {
        public static int SequentialSum(int[] data)
        {
            //Listing 4.6 Common For loop
            int sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i]; //#A
            }
            return sum;
        }

        public static int SequentialSumLINQ(int[] data)
        {
            return data.Sum();
        }
    }
}
