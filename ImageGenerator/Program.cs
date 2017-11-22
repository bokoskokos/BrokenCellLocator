using System;
using System.IO;

namespace ImageGenerator
{
    class Program
    {
        public const int FrameSize = 200;
        static void Main(string[] args)
        {
            try
            {
                var debug = false;
                if (args.Length > 0)
                    debug = args[0].Trim() == "d";

                if (!Directory.Exists("img"))
                    Directory.CreateDirectory("img");

                var allFiles = Directory.GetFiles("img");
                foreach (var f in allFiles)
                    File.Delete(f);

                Console.WriteLine("Enter number of images to analyse");
                var numberOfFiles = Console.ReadLine();
                if (!int.TryParse(numberOfFiles, out int num))
                {
                    Console.WriteLine("WARNING: Invalid input, creating 10 images");
                    num = 10;
                }

                using (var ic = new ImageCreator())
                    ic.Create(num);

                using (var ip = new ImageProcessor())
                    ip.Process(debug);

                Logger.Result(num);

                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                Console.ReadKey();
            }
        }
    }
}
