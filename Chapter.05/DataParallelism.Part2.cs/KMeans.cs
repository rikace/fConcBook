using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataParallelism.Part2.CSharp
{
    public class KMeans
    {
        public KMeans(double[][] data)
        {
            this.data = data;
            this.N = data[0].Length;
        }

        protected int N;
        protected double[][] data;

        private double Dist(double[] u, double[] v)
        {
            double results = 0.0;
            for (var i = 0; i < u.Length; i++)
                results += Math.Pow(u[i] - v[i], 2.0);
            return results;
        }

        //Listing 5.8 Function to find the closest centroid(used to update the clusters)
        protected double[] GetNearestCentroid(double[][] centroids, double[] center)
        {
            return centroids.Aggregate((centroid1, centroid2) => //#A
                Dist(center, centroid2) < Dist(center, centroid1)
                ? centroid2
                : centroid1);
        }

        //Listing 5.9 Update the location of the centroids according to the center of the cluster
        protected virtual double[][] UpdateCentroids(double[][] centroids)
        {
            var partitioner = Partitioner.Create(data, true); //#A
            var result = partitioner.AsParallel() //#B
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism) //#C
                .GroupBy(u => GetNearestCentroid(centroids, u))
                .Select(points =>
                    points
                        .Aggregate(new double[N], //#D
                            (acc, item) => acc.Zip(item, (a, b) => a + b).ToArray()) //#E
                        .Select(items => items / points.Count())
                        .ToArray())
                .ToArray();

            Array.Sort(result, (a, b) => {
                for (var i = 0; i < N; i++)
                    if (a[i] != b[i])
                        return a[i].CompareTo(b[i]);
                return 0;
            });
            return result;
        }

        //Listing 5.10 UpdateCentroids function implemented without Aggregate
        double[][] UpdateCentroidsWithMutableState(double[][] centroids)
        {
            var result = data.AsParallel()
                .GroupBy(u => GetNearestCentroid(centroids, u))
                .Select(points => {
                    var res = new double[N];
                    foreach (var x in points) //#A
                        for (var i = 0; i < N; i++)
                            res[i] += x[i]; //#B
                    var count = points.Count();
                    for (var i = 0; i < N; i++)
                        res[i] /= count; //#B
                    return res;
                });
            return result.ToArray();
        }


        public double[][] Run(double[][] initialCentroids)
        {
            var centroids = initialCentroids;
            for (int i = 0; i <= 1000; i++)
            {
                var newCentroids = UpdateCentroids(centroids);
                var error = double.MaxValue;
                if (centroids.Length == newCentroids.Length)
                {
                    error = 0;
                    for (var j = 0; j < centroids.Length; j++)
                        error += Dist(centroids[j], newCentroids[j]);
                }
                if (error < 1e-9)
                {
                    Console.WriteLine($"Iterations {i}");
                    return newCentroids;
                }
                centroids = newCentroids;
            }
            Console.WriteLine($"Iterations 1000");
            return centroids;
        }
    }

    public class KMeansLinq : KMeans
    {
        public KMeansLinq(double[][] data) : base(data)
        {         }

        protected override double[][] UpdateCentroids(double[][] centroids)
        {
            var result =
              data
                .GroupBy(u => GetNearestCentroid(centroids, u))
                .Select(elements => {
                    var res = new double[N];
                    foreach (var x in elements)
                        for (var i = 0; i < N; i++)
                            res[i] += x[i];
                    var M = elements.Count();
                    for (var i = 0; i < N; i++)
                        res[i] /= M;
                    return res;
                })
                .ToArray();

            Array.Sort(result, (a, b) => {
                for (var i = 0; i < N; i++)
                    if (a[i] != b[i])
                        return a[i].CompareTo(b[i]);
                return 0;
            });
            return result;
        }
    }

    public class KMeansPLinq : KMeans
    {
        public KMeansPLinq(double[][] data) : base(data)
        { }

        protected override double[][] UpdateCentroids(double[][] centroids)
        {
            var result =
              data.AsParallel()
                .GroupBy(u => GetNearestCentroid(centroids, u))
                .Select(elements => {
                    var res = new double[N];
                    foreach (var x in elements)
                        for (var i = 0; i < N; i++)
                            res[i] += x[i];
                    var M = elements.Count();
                    for (var i = 0; i < N; i++)
                        res[i] /= M;
                    return res;
                })
                .ToArray();

            Array.Sort(result, (a, b) => {
                for (var i = 0; i < N; i++)
                    if (a[i] != b[i])
                        return a[i].CompareTo(b[i]);
                return 0;
            });
            return result;
        }
    }

}
