using System;
using System.Collections.Generic;
using System.IO;

namespace boidsTransformer
{
    public class Program
    {
        const string DEFAULT_OUTPUT_DIRECTORY = "o";
        public static readonly float[] KNOWN_BOUNDS = { -1000f, -1000f, 1000f, 1000f }; // this isn't supplied in raw data file (sorry)

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

            // Get input
            outputDirectory = InterpretArguments(args, inputFiles, outputDirectory);

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
                File.WriteAllText(outputDirectory + "/" + Path.GetFileName(inputFiles[i]) + ".ply", experiments[i].TrafficPly());
            }
        }

        protected static string InterpretArguments(string[] args, List<string> inputFiles, string outputDirectory)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-o")
                { // Handle flags
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

            return outputDirectory;
        }
    }
}
