using System;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace DataParallelism.Part1.CSharp
{
    public class Mandelbrot
    {
        private const float Width = 2.5f;
        private const float Height = 2.5f;

        private static readonly Complex Center = new Complex(-0.75f, 0.0f);

        private static readonly Rgba32 black = new Rgba32(0, 0, 0, byte.MaxValue);
        private static readonly Rgba32 white = new Rgba32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);


        private static readonly ComplexStruct _centerStruct = new ComplexStruct(-0.75f, 0.0f);

        private static float ComputeRow(int col, int cols)
        {
            return Center.Real - Width / 2.0f + col * Width / cols;
        }

        private static float ComputeColumn(int row, int rows)
        {
            return Center.Imaginary - Height / 2.0f + row * Height / rows;
        }

        public static Image SequentialMandelbrot(int size)
        {
            var rows = size;
            var cols = size;

            var image = new Image<Rgba32>(rows, cols);

            // Listing 4.2 Sequential Mandelbrot
            Func<Complex, int, bool> isMandelbrot = (complex, iterations) => //#A
            {
                var z = new Complex(0.0f, 0.0f);
                var acc = 0;
                while (acc < iterations && z.Magnitude < 2.0)
                {
                    z = z * z + complex;
                    acc += 1;
                }

                return acc == iterations;
            };

            for (var col = 0; col < cols; col++) //#B
            {
                var pixelRowSpan = image.GetPixelRowSpan(col);

                for (var row = 0; row < rows; row++) //#B
                {
                    var x = ComputeRow(row, rows); //#C
                    var y = ComputeColumn(col, cols); //#C
                    var c = new Complex(x, y);
                    var color = isMandelbrot(c, 100) ? black : white; //#D

                    pixelRowSpan[row] = color; //#E
                }
            }

            // you can save the image using the ".Save" API (Ex.  image.Save("./myImage.jpg");
            return image;
        }

        public static Image ParallelMandelbrot(int size)
        {
            var rows = size;
            var cols = size;

            var image = new Image<Rgba32>(rows, cols);

            // Listing 4.3 Parallel Mandelbrot
            Func<Complex, int, bool> isMandelbrot = (complex, iterations) =>
            {
                var z = new Complex(0.0f, 0.0f);
                var acc = 0;
                while (acc < iterations && z.Magnitude < 2.0)
                {
                    z = z * z + complex;
                    acc += 1;
                }

                return acc == iterations;
            };

            Parallel.For(0, cols - 1, col =>
            {
                var pixelRowSpan = image.GetPixelRowSpan(col);
                //#A
                for (var row = 0; row < rows; row++)
                {
                    var x = ComputeRow(row, rows); //#C
                    var y = ComputeColumn(col, cols); //#C
                    var c = new Complex(x, y);
                    var color = isMandelbrot(c, 100) ? black : white; //#D

                    pixelRowSpan[row] = color; //#E
                }
            });

            return image;
        }

        public static Image ParallelMandelbrotOversaturation(int size)
        {
            var rows = size;
            var cols = size;

            var image = new Image<Rgba32>(rows, cols);

            Func<Complex, int, bool> isMandelbrot = (complex, iterations) =>
            {
                var z = new Complex(0.0f, 0.0f);
                var acc = 0;
                while (acc < iterations && z.Magnitude < 2.0)
                {
                    z = z * z + complex;
                    acc += 1;
                }

                return acc == iterations;
            };

            Parallel.For(0, cols - 1, col =>
            {
                var pixelRowSpan = new Memory<Rgba32>(image.GetPixelRowSpan(col).ToArray());

                Parallel.For(0, rows - 1, row =>
                {
                    var span = pixelRowSpan.Span;
                    var x = ComputeRow(row, rows); //#C
                    var y = ComputeColumn(col, cols); //#C
                    var c = new Complex(x, y);
                    var color = isMandelbrot(c, 100) ? black : white; //#D

                    span[row] = color; //#E
                });
            });

            return image;
        }

        private static float ComputeRowStruct(int col, int cols)
        {
            return _centerStruct.Real - Width / 2.0f + col * Width / cols;
        }

        private static float ComputeColumnStruct(int row, int rows)
        {
            return _centerStruct.Imaginary - Height / 2.0f + row * Height / rows;
        }

        public static Image ParallelStructMandelbrot(int size)
        {
            var rows = size;
            var cols = size;

            var image = new Image<Rgba32>(rows, cols);

            Func<ComplexStruct, int, bool> isMandelbrot = (complex, iterations) =>
            {
                var z = new ComplexStruct(0.0f, 0.0f);
                var acc = 0;
                while (acc < iterations && z.Magnitude < 2.0)
                {
                    z = z * z + complex;
                    acc += 1;
                }

                return acc == iterations;
            };

            Parallel.For(0, cols - 1, col =>
            {
                var pixelRowSpan = image.GetPixelRowSpan(col);
                for (var row = 0; row < rows; row++)
                {
                    var x = ComputeRowStruct(row, rows);
                    var y = ComputeColumnStruct(col, cols);
                    var c = new ComplexStruct(x, y);
                    var color = isMandelbrot(c, 100) ? black : white; //#D

                    pixelRowSpan[row] = color; //#E
                }
            });
            return image;
        }
    }
}