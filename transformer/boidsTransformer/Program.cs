using System;
using System.Collections.Generic;
using System.IO;

namespace boidsTransformer
{
    public class Program
    {
        const string DEFAULT_OUTPUT_DIRECTORY = "o";
        public static readonly float[] KNOWN_BOUNDS = { -1000f, -1000f, 1000f, 1000f }; // this isn't supplied in raw data file (sorry)
        public static readonly int DEFAULT_GRID_SIZE = 21;

        /// <summary>
        /// Converts a raw log output from the Boids Simulator (early release) to .ply format.
        /// </summary>
        /// <param name="args">The filename(s) of the files to convert. If preceded by -o flag, the next argument is instead the output directory</param>
        public static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("No arguments supplied. Correct usage is:\n"
                    + "inputFile1.log inputFile2.log -o output_directory");
                return;
            }

            List<string> inputFiles = new List<string>();
            List<BoidsExperiment> experiments = new List<BoidsExperiment>();
            string outputDirectory = DEFAULT_OUTPUT_DIRECTORY;
            int gridSize = DEFAULT_GRID_SIZE;

            // Get input
            InterpretArguments(args, inputFiles, ref outputDirectory, ref gridSize);

            // Process input
            for(int i = 0; i < inputFiles.Count; i++)
            {
                experiments.Add(new BoidsExperiment(inputFiles[i], KNOWN_BOUNDS));
            }

            if (inputFiles.Count < experiments.Count)
                throw new DataMisalignedException("Something messed with the experiment list; expected " + inputFiles.Count + " experiments.");

            // Setup output directory
            if(!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Write output
            for (int i = 0; i < experiments.Count; i++)
            {
                File.WriteAllText(outputDirectory + "/" + Path.GetFileName(inputFiles[i]) + ".ply", experiments[i].TrafficPly(gridSize));
            }
        }

        protected static void InterpretArguments(string[] args, List<string> inputFiles, ref string outputDirectory, ref int gridRes)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-o")
                { // Handle output flag
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine("Argument \"-o\" supplied with no following output directory.\n"
                            + "Assuming default: " + outputDirectory);
                    }
                    else
                    {
                        // args[i+1] is the intended output directory.
                        outputDirectory = args[i + 1];
                        i++; // Skip interpretation of next argument
                    }
                }
                else if (args[i] == "-s")
                { // Handle resolution flag
                    if (i == args.Length - 1)
                    {
                        Console.WriteLine("Argument \"-s\" supplied with no following resolution.\n"
                            + "Assuming default: " + gridRes);
                    }
                    else
                    {
                        int newRes;
                        if (!int.TryParse(args[i + 1], out newRes))
                            Console.WriteLine(args[i + 1] + " is not an integer!");
                        else
                        {
                            if (newRes <= 0)
                                Console.WriteLine(args[i + 1] + " must be positive!");
                            else
                                gridRes = newRes;
                        }
                        i++;
                    }
                }
                else
                {
                    // Add to input buffer.
                    if (!File.Exists(args[i]))
                    {
                        Console.WriteLine("File \"" + args[i] + "\" does not exist.");
                        throw new ArgumentException("File \"" + args[i] + "\" does not exist.");
                    }
                    inputFiles.Add(args[i]);
                }
            }

        }
    }
}
