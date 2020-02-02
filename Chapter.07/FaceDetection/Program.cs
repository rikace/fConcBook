using System;

namespace FaceDetection
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var imagesFolder = "../../../../../../Common/Data/Faces";

            var faceDetection = new FaceDetection();

            faceDetection.StartFaceDetection_7_6(imagesFolder);
            // faceDetection.StartFaceDetection_7_7(imagesFolder);
            // faceDetection.StartFaceDetection_7_13(imagesFolder);
            // faceDetection.StartFaceDetection_Pipeline_FSharpFunc(imagesFolder);

            Console.ReadLine();
        }
    }
}