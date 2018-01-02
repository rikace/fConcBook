using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DataParallelism.cs
{
    public class Mandelbrot
    {
        private const int Rows = 2000;
        private const int Cols = 2000;
        private static readonly Complex Center = new Complex(-0.75f, 0.0f);

        private const float Width = 2.5f;
        private const float Height = 2.5f;

        private static float ComputeRow(int col)
            => Center.Real - Width / 2.0f + (float)col * Width / (float)Cols;

        private static float ComputeColumn(int row)
            => Center.Imaginary - Height / 2.0f + (float)row * Height / (float)Rows;

        public static Bitmap SequentialMandelbrot()
        {
            var bitmap = new Bitmap(Rows, Cols, PixelFormat.Format24bppRgb);
            var bitmapData =
                bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var pixels = new byte[bitmapData.Stride * bitmap.Height];
            var ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

            // Listing 4.2 Sequential Mandelbrot
            Func<Complex, int, bool> isMandelbrot = (complex, iterations) => //#A
            {
                var z = new Complex(0.0f, 0.0f);
                int acc = 0;
                while (acc < iterations && z.Magnitude < 2.0)
                {
                    z = z * z + complex;
                    acc += 1;
                }
                return acc == iterations;
            };

            for (int col = 0; col < Cols; col++)        //#B
            {
                for (int row = 0; row < Rows; row++)    //#B
                {
                    var x = ComputeRow(row);            //#C
                    var y = ComputeColumn(col);         //#C
                    var c = new Complex(x, y);
                    var color = isMandelbrot(c, 100) ? Color.Black : Color.White; //#D
                    var offset = (col * bitmapData.Stride) + (3 * row);
                    pixels[offset + 0] = color.B; // Blue component      //#E
                    pixels[offset + 1] = color.G; // Green component     //#E
                    pixels[offset + 2] = color.R; // Red component       //#E
                }
            }

            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bitmap.UnlockBits(bitmapData);

            var image = (Bitmap)bitmap.Clone();
            return image;
        }

        public static Bitmap ParallelMandelbrot()
        {

            var bitmap = new Bitmap(Rows, Cols, PixelFormat.Format24bppRgb);
            var bitmapData =
                bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var pixels = new byte[bitmapData.Stride * bitmap.Height];
            var ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

            // Listing 4.3 Parallel Mandelbrot
            Func<Complex, int, bool> isMandelbrot = (complex, iterations) =>
            {
                var z = new Complex(0.0f, 0.0f);
                int acc = 0;
                while (acc < iterations && z.Magnitude < 2.0)
                {
                    z = z * z + complex;
                    acc += 1;
                }
                return acc == iterations;
            };

            Parallel.For(0, Cols - 1, col =>
            {    //#A
                for (int row = 0; row < Rows; row++)
                {
                    var x = ComputeRow(row);
                    var y = ComputeColumn(col);
                    var c = new Complex(x, y);
                    var color = isMandelbrot(c, 100) ? Color.DarkBlue : Color.White;
                    var offset = (col * bitmapData.Stride) + (3 * row);
                    pixels[offset + 0] = color.B; // Blue component
                    pixels[offset + 1] = color.G; // Green component
                    pixels[offset + 2] = color.R; // Red component
                }
            });

            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bitmap.UnlockBits(bitmapData);
            var image = (Bitmap)bitmap.Clone();
            return image;
        }

        public static Bitmap ParallelMandelbrotOversaturation()
        {
            var bitmap = new Bitmap(Rows, Cols, PixelFormat.Format24bppRgb);
            var bitmapData =
                 bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                 ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var pixels = new byte[bitmapData.Stride * bitmap.Height];
            var ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

            Func<Complex, int, bool> isMandelbrot = (complex, iterations) =>
            {
                var z = new Complex(0.0f, 0.0f);
                int acc = 0;
                while (acc < iterations && z.Magnitude < 2.0)
                {
                    z = z * z + complex;
                    acc += 1;
                }
                return acc == iterations;
            };

            Parallel.For(0, Cols - 1, col =>
            {
                Parallel.For(0, Rows - 1, row =>
                {
                    var x = ComputeRow(row);
                    var y = ComputeColumn(col);
                    var c = new Complex(x, y);
                    var color = isMandelbrot(c, 100) ? Color.DarkBlue : Color.White;
                    var offset = (col * bitmapData.Stride) + (3 * row);
                    pixels[offset + 0] = color.B; // Blue component
                    pixels[offset + 1] = color.G; // Green component
                    pixels[offset + 2] = color.R; // Red component
                });
            });

            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bitmap.UnlockBits(bitmapData);
            var image = (Bitmap)bitmap.Clone();
            return image;
        }

        private static ComplexStruct _centerStruct = new ComplexStruct(-0.75f, 0.0f);

        private static float ComputeRowStruct(int col)
            => _centerStruct.Real - Width / 2.0f + (float)col * Width / (float)Cols;

        private static float ComputeColumnStruct(int row)
            => _centerStruct.Imaginary - Height / 2.0f + (float)row * Height / (float)Rows;

        public static Bitmap ParallelStructMandelbrot()
        {
            var bitmap = new Bitmap(Rows, Cols, PixelFormat.Format24bppRgb);
            var bitmapData =
                 bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                 ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            var pixels = new byte[bitmapData.Stride * bitmap.Height];
            var ptrFirstPixel = bitmapData.Scan0;
            Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);

            Func<ComplexStruct, int, bool> isMandelbrot = (complex, iterations) =>
            {
                var z = new ComplexStruct(0.0f, 0.0f);
                int acc = 0;
                while (acc < iterations && z.Magnitude < 2.0)
                {
                    z = z * z + complex;
                    acc += 1;
                }
                return acc == iterations;
            };

            Parallel.For(0, Cols - 1, col =>
            {
                for (int row = 0; row < Rows; row++)
                {
                    var x = ComputeRowStruct(row);
                    var y = ComputeColumnStruct(col);
                    var c = new ComplexStruct(x, y);
                    var color = isMandelbrot(c, 100) ? Color.DarkBlue : Color.White;
                    var offset = (col * bitmapData.Stride) + (3 * row);
                    pixels[offset + 0] = color.B; // Blue component
                    pixels[offset + 1] = color.G; // Green component
                    pixels[offset + 2] = color.R; // Red component
                }
            });

            Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
            bitmap.UnlockBits(bitmapData);
            var image = (Bitmap)bitmap.Clone();
            return image;
        }
    }
}
