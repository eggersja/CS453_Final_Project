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
        /// The number of boids in the experiment.
        /// </summary>
        public int BoidCount { 
            get 
            {
                if (boidCount < 0)
                    InitializeBoidCount();
                return boidCount;
            }
        }

        /// <summary>
        /// The number of boids in the experiment. -1 if not yet calculated. Only access this through <see cref="BoidCount"/>.
        /// </summary>
        private int boidCount = -1;

        public float[] Bounds
        {
            get
            {
                if (bounds == null)
                    InitializeBounds();
                return bounds;
            }
        }

        /// <summary>
        /// minX, minY, maxX, maxY
        /// </summary>
        private float[] bounds;

        // TODO: Allow manual supply of bounds for visualization

        /// <summary>
        /// Creates a BoidsExperiment from a boids log file.
        /// </summary>
        /// <param name="inputFile">The file the experiment can be found in.</param>
        /// <param name="experiment_borders">minX, minY, maxX, maxY. <c>null</c> to <see cref="InitializeBounds(float)">autocalculate</see> (lazy eval.)</param>
        public BoidsExperiment(string inputFile, float[] experiment_borders = null)
        {
            string[] snapshotsRaw = File.ReadAllLines(inputFile); // Convenience function
            InitializeSnapshots(snapshotsRaw);
            bounds = experiment_borders;
        }

        /// <summary>
        /// Initializes the boids experiment from a serialized representation.
        /// </summary>
        /// <param name="rawSnapshots">A serialized representation of the snapshot conforming to <see href="https://regex101.com/r/2eIGmk/6/"/>. May have multiple lines.</param>
        /// <param name="experiment_borders">minX, minY, maxX, maxY. <c>null</c> to <see cref="InitializeBounds(float)">autocalculate</see> (lazy eval.)</param>
        public BoidsExperiment(string[] rawSnapshots, float[] experiment_borders = null)
        {
            InitializeSnapshots(rawSnapshots);
            bounds = experiment_borders;
        }

        /// <summary>
        /// Initializes the boids experiment from a serialized representation.
        /// </summary>
        /// <remarks>
        /// Instead of using the convenience function, the beginning of a proper setup would look something like:
        /// FileStream input = File.OpenRead(inputFile);
        /// Span<byte> inputBuffer; // Alternative consideration if this were a more serious project.
        /// </remarks>
        /// <param name="snapshotsRaw">A serialized representation of the snapshot conforming to <see href="https://regex101.com/r/2eIGmk/6/"/>.  May have multiple lines.</param>
        protected void InitializeSnapshots(string[] snapshotsRaw)
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

        /// <summary>
        /// Sets <see cref="boidCount"/> based on the current state of the experiment.
        /// </summary>
        private void InitializeBoidCount()
        {
            int maxBoids = 0;
            foreach(BoidsSnapshot s in Current)
            {
                if (s.Coords.Count > maxBoids)
                    maxBoids = s.Coords.Count;
            }
            boidCount = maxBoids;
        }

        private void InitializeBounds(float margin = 0)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Generates a grid PLY file based on traffic data sourced from this experiment. Intended for use in learnply by Dr. Eugene Zhang
        /// </summary>
        /// <param name="gridSize">The size of the .ply grid.</param>
        /// <returns>Traffic ply file</returns>
        public string TrafficPly(int gridSize = 20)
        {
            // Get the trafic density
            float[][] gridWeights = ProjectTrafficGrid(gridSize);

            // Format as .ply

            // Done!
            throw new NotImplementedException();
        }

        /// <summary>
        /// At a resolution determined by <paramref name="gridSize"/>, determine the relative traffic of all areas in the experiment.
        /// </summary>
        /// <param name="gridSize">The width and height of the grid the experiment should be superimposed upon.</param>
        /// <param name="mode">The type of mapping to perform between vertices on the grid and the bots</param>
        /// <returns>Traffic grid</returns>
        protected static float[][] ProjectTrafficGrid(int gridSize = 20, MappingMode mode = MappingMode.SAME_QUAD)
        {
            float[][] gridWeights = new float[gridSize][];
            for (int row = 0; row < gridSize; row++)
            {
                gridWeights[row] = new float[gridSize];
                for (int col = 0; col < gridSize; col++)
                {
                    gridWeights[row][col] = 0;
                }
            }

            return gridWeights;
        }

        public enum MappingMode
        {
            SAME_QUAD,
            ALL_QUAD
        }
    }
}
