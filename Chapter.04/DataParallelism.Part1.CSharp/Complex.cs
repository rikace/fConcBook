using System;

namespace DataParallelism.Part1.CSharp
{
    // Listing 4.1 Complex number object
    internal class Complex
    {
        public Complex(float real, float imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public float Imaginary { get; } //#A
        public float Real { get; } //#A

        public float Magnitude
            => (float) Math.Sqrt(Real * Real + Imaginary * Imaginary); //#B

        public static Complex operator +(Complex c1, Complex c2)
        {
            return new Complex(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary); //#C
        }

        public static Complex operator *(Complex c1, Complex c2)
        {
            return new Complex(c1.Real * c2.Real - c1.Imaginary * c2.Imaginary,
                c1.Real * c2.Imaginary + c1.Imaginary * c2.Real); //#C
        }
    }

    internal struct ComplexStruct
    {
        public ComplexStruct(float real, float imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public float Imaginary { get; }
        public float Real { get; }

        public float Magnitude
            => (float) Math.Sqrt(Real * Real + Imaginary * Imaginary);

        public static ComplexStruct operator +(ComplexStruct c1, ComplexStruct c2)
        {
            return new ComplexStruct(c1.Real + c2.Real, c1.Imaginary + c2.Imaginary);
        }

        public static ComplexStruct operator *(ComplexStruct c1, ComplexStruct c2)
        {
            return new ComplexStruct(c1.Real * c2.Real - c1.Imaginary * c2.Imaginary,
                c1.Real * c2.Imaginary + c1.Imaginary * c2.Real);
        }
    }
}