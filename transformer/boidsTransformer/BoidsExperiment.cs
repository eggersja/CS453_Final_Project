using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace boidsTransformer
{
    /// <summary>
    /// Log files representing a boid simulator experiment.
    /// </summary>
    public class BoidsExperiment
    {
        /// <summary>
        /// Status of the boids simulation at a moment in time.
        /// </summary>
        public struct BoidsSnapshot
        {
            /// <summary>
            /// The time the timestamp was taken at.
            /// </summary>
            public float Timestamp;

            /// <summary>
            /// The coordinates of each boid, in ordered-pair format. 
            /// </summary>
            public List<float[]> Coords;

            /// <summary>
            /// Direct constructor.
            /// </summary>
            /// <param name="timestamp"><see cref="Timestamp"/></param>
            /// <param name="coords"><see cref="Coords"/></param>
            public BoidsSnapshot(float timestamp, List<float[]> coords)
            {
                Timestamp = timestamp;
                Coords = coords;
            }
        }

        /// <summary>
        /// The entire boids simulation.
        /// </summary>
        public List<BoidsSnapshot> Current { get; protected set; }

        /// <summary>
        /// Creates a BoidsExperiment from a boids log file.
        /// </summary>
        /// <param name="inputFile">The file the experiment can be found in.</param>
        public BoidsExperiment(string inputFile)
        {
            string[] snapshotsRaw = File.ReadAllLines(inputFile); // Convenience function

            InitializeSnapshot(snapshotsRaw);
            
        }

        /// <summary>
        /// Initializes the boids experiment from a serialized representation.
        /// </summary>
        /// <param name="rawSnapshots">A serialized representation of the snapshot conforming to <see href="https://regex101.com/r/2eIGmk/6/"/>. May have multiple lines.</param>
        public BoidsExperiment(string[] rawSnapshots) => InitializeSnapshot(rawSnapshots);

        /// <summary>
        /// Initializes the boids experiment from a serialized representation.
        /// </summary>
        /// <remarks>
        /// Instead of using the convenience function, the beginning of a proper setup would look something like:
        /// FileStream input = File.OpenRead(inputFile);
        /// Span<byte> inputBuffer; // Alternative consideration if this were a more serious project.
        /// </remarks>
        /// <param name="snapshotsRaw">A serialized representation of the snapshot conforming to <see href="https://regex101.com/r/2eIGmk/6/"/>.  May have multiple lines.</param>
        protected void InitializeSnapshot(string[] snapshotsRaw)
        {
            Current = new List<BoidsSnapshot>();

            // Reads the snapshot as a whole.
            Regex snapshotReader = new Regex(@"(?<time>[\d\.\-]+):(?:(?:[\d\.]+),(?:[\d\.]+);)*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            // Reads each ordered pair in the snapshot.
            Regex orderedPairFinder = new Regex(@"(?<coordx>[\d\.]+),(?<coordy>[\d\.]+);",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);


            for (int i = 0; i < snapshotsRaw.Length; i++)
            { // For each snapshot
                // Get the overallsnapshat data.
                if (snapshotsRaw.Equals("") && i == snapshotsRaw.Length - 1) return; // Allow blank line at EOF.
                Match parsedSnapshot = snapshotReader.Match(snapshotsRaw[i]);
                if (!parsedSnapshot.Success)
                {
                    string err = "Could not interpret line " + i + " of input.";
                    Console.WriteLine(err);
                    throw new ArgumentException(err);
                }

                float t = float.Parse(parsedSnapshot.Groups["time"].Value);

                // Get snapshot data for each boid.
                List<float[]> pairs = new List<float[]>();

                MatchCollection rawPairs = orderedPairFinder.Matches(snapshotsRaw[i]);

                for (int j = 0; j < rawPairs.Count; j++)
                {
                    Match currentPair = rawPairs[j];
                    float[] parsedPair = {
                        float.Parse(currentPair.Groups["coordx"].Value),
                        float.Parse(currentPair.Groups["coordy"].Value)
                    };
                    pairs.Add(parsedPair);
                }

                // Save the snapshot
                BoidsSnapshot s = new BoidsSnapshot(t, pairs);
                Current.Add(s);
            }
        }

        public string ToPly()
        {
            throw new NotImplementedException();
        }
    }
}
