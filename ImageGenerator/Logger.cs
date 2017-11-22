using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageGenerator
{
    public class Logger
    {
        private const bool DebugLoggingEnabled = false; // set to true for debug details
        private static readonly ConsoleColor DefaultColour;
        private static int Fails = 0;

        static Logger()
        {
            DefaultColour = Console.ForegroundColor;
        }

        public static void Debug(string message)
        {
            if (DebugLoggingEnabled)
                Console.WriteLine(message);
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = DefaultColour;
        }

        public static void Report(int fileIndex, (int x, int y)[] brokenCells = null)
        {
            if (brokenCells == null)
            {
                Console.WriteLine("================================================");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Image:\t\t{fileIndex}");
                Console.WriteLine($"Correct:\tCannot be analysed!");
                Console.ForegroundColor = DefaultColour;
                return;
            }

            var allLines = File.ReadAllText(Path.Combine("img", "report.json"));
            var solutions = JsonConvert.DeserializeObject<(int, int)[][]>(allLines);
            var solution = solutions[fileIndex];

            var correct = true;
            if (brokenCells.Length == solution.Length)
                correct = solution.All(s => brokenCells.Any(bc => bc.x == s.Item1 && bc.y == s.Item2));
            else
                correct = false;

            if (!correct)
                Fails++;

            Console.WriteLine("================================================");
            Console.ForegroundColor = correct ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"Image:\t\t{fileIndex}");
            Console.WriteLine($"Correct:\t{correct}");
            Console.WriteLine($"Analysed:\t{Pretty(brokenCells)}");
            Console.WriteLine($"Actual:\t\t{Pretty(solution)}");
            Console.ForegroundColor = DefaultColour;
        }

        public static void Result(int total)
        {
            Console.WriteLine();
            Console.ForegroundColor = Fails == 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine("================================================");
            Console.WriteLine($"Final result: {total - Fails}/{total} ({(((float)total - Fails) / total)*100}%)");
            Console.ForegroundColor = DefaultColour;
            Console.WriteLine("Press enter...");
        }

        private static string Pretty((int x, int y)[] values)
        {
            var stringValues = new List<string>();
            foreach (var val in values)
            {
                switch (val.x)
                {
                    case 0: stringValues.Add($"A{val.y + 1}"); break;
                    case 1: stringValues.Add($"B{val.y + 1}"); break;
                    case 2: stringValues.Add($"C{val.y + 1}"); break;
                    case 3: stringValues.Add($"D{val.y + 1}"); break;
                    case 4: stringValues.Add($"E{val.y + 1}"); break;
                    case 5: stringValues.Add($"F{val.y + 1}"); break;
                    case 6: stringValues.Add($"G{val.y + 1}"); break;
                    case 7: stringValues.Add($"H{val.y + 1}"); break;
                    case 8: stringValues.Add($"I{val.y + 1}"); break;
                    case 9: stringValues.Add($"J{val.y + 1}"); break;
                }
            }
            stringValues.Sort();
            return string.Join(", ", stringValues);
        }
    }
}
