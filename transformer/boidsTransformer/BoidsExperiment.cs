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

        /// <summary>
        /// The size of the boundaries of the experiment.
        /// </summary>
        public float[] Bounds
        {
            get
            {
                if (bounds == null)
                    InitializeBounds();
                return bounds;
            }
            set
            {
                bounds = value;
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
            if (Current.Count == 0 || Current[0].Coords.Count == 0)
                return; // Can't calculate bounds on an empty experiment
            BoidsSnapshot firstSnapshot = Current[0];
            bounds = new float[] { firstSnapshot.Coords[0][0], firstSnapshot.Coords[0][1], firstSnapshot.Coords[0][0], firstSnapshot.Coords[0][1] };

            for(int i = 0; i < Current.Count; i++)
            {
                BoidsSnapshot s = Current[i];
                for(int j = 0; j < s.Coords.Count; j++)
                {
                    float[] c = s.Coords[j];
                    if (c[0] < bounds[0])
                        bounds[0] = c[0];
                    if (c[1] < bounds[1])
                        bounds[1] = c[1];
                    if (c[0] > bounds[2])
                        bounds[2] = c[0];
                    if (c[1] > bounds[3])
                        bounds[3] = c[1];
                }
            }
            //throw new System.NotImplementedException();
        }

        /// <summary>
        /// Generates a grid PLY file based on traffic data sourced from this experiment. Intended for use in learnply by Dr. Eugene Zhang
        /// </summary>
        /// <param name="gridSize">The size of the .ply grid.</param>
        /// <returns>Traffic ply file</returns>
        public string TrafficPly(int gridSize = 21)
        {
            // Get the trafic density
            float[][] gridWeights = ProjectTrafficGrid(gridSize);
            int faceCount = (gridSize - 1) * (gridSize - 1);

            // Format as .ply
            StringBuilder ply_file = GeneratePlyHeader(gridSize);
            for(int i = gridSize-1; i >= 0; i--)
            {
                for(int j = gridSize-1; j >= 0; j--)
                { // For each vertex
                    ply_file.Append(((float)j) - ((float)gridSize - 1) / 2); //x
                    ply_file.Append("\t");
                    ply_file.Append(((float)i) - ((float)gridSize - 1) / 2); //y
                    ply_file.Append("\t0.000000"); //z
                    ply_file.Append("\t0.000000"); //vx
                    ply_file.Append("\t0.000000"); //vy
                    ply_file.Append("\t0.000000"); //vz
                    ply_file.Append("\t" + gridWeights[i][j] + "\n"); //s
                }
            }

            for (int i = 0; i < faceCount; i++)
            { // For each face
                ply_file.Append("4 "); // Number of verticies.
                ply_file.Append(i + " "); // SE corner (xHigh, yHigh)
                ply_file.Append((i + 1) + " "); // SW corner (xLow, yHigh)
                ply_file.Append((i + gridSize + 1) + " "); // NW corner (xLow, yLow)
                ply_file.Append((i + gridSize) + "\n"); // NE corner (xHigh, yLow)
            }

            // Done!
            return ply_file.ToString();
        }


        public static StringBuilder GeneratePlyHeader(int gridSize)
        {
            if (gridSize < 0)
                throw new ArgumentException("Ply files cannot be on a negative grid size");
            int vertexCount = gridSize*gridSize;
            int faceCount = (gridSize - 1) * (gridSize - 1);
            StringBuilder header = new StringBuilder();
            header.Append("ply\n");
            header.Append("format ascii 1.0\n");
            header.Append("comment created by boidsTransformer\n");
            header.Append("element vertex " + vertexCount + "\n");
            header.Append("property float64 x\n");
            header.Append("property float64 y\n");
            header.Append("property float64 z\n");
            header.Append("property float64 vx\n");
            header.Append("property float64 vy\n");
            header.Append("property float64 vz\n");
            header.Append("property float64 s\n");
            header.Append("element face " + faceCount + "\n");
            header.Append("property list uint8 int32 vertex_indices\n");
            header.Append("end_header\n");

            return header;
        }

        /// <summary>
        /// At a resolution determined by <paramref name="gridSize"/>, determine the relative traffic of all areas in the experiment.
        /// </summary>
        /// <param name="gridSize">The width and height of the grid the experiment should be superimposed upon.</param>
        /// <param name="maxTime">The amount of seconds within the simulation to include when making the grid (up until the end of the simluation)</param>
        /// <param name="mode">The type of mapping to perform between vertices on the grid and the bots</param>
        /// <returns>Traffic grid</returns>
        protected float[][] ProjectTrafficGrid(int gridSize = 21, float maxTime = float.PositiveInfinity, MappingMode mode = MappingMode.SAME_QUAD)
        {
            // Initialization
            float[][] gridWeights = new float[gridSize][];
            for (int row = 0; row < gridSize; row++)
            {
                gridWeights[row] = new float[gridSize];
                for (int col = 0; col < gridSize; col++)
                {
                    gridWeights[row][col] = 0;
                }
            }

            if (mode == MappingMode.SAME_QUAD)
            {
                // Add weights
                foreach (BoidsSnapshot snapshot in Current)
                {
                    if (snapshot.Timestamp < maxTime) // Since the list is not garunteed to be sorted, we can't break when we pass the max time.
                    {
                        foreach (float[] coord in snapshot.Coords)
                        {
                            DistributeBoidWeightOverQuad(ref gridWeights, coord, 1);
                        }
                    }
                }
            }
            else if (mode == MappingMode.ALL_QUAD)
                throw new System.NotImplementedException("Mapping mode not supported: All quads.");

            return gridWeights;
        }

        /// <summary>
        /// Increases weights within the traffic grid based on a boid's position therein.
        /// </summary>
        /// <param name="trafficGrid">The grid to increase the traffic upon.</param>
        /// <param name="boidCoords">The coordinates of the boid to distribute the weight over. (x,y)</param>
        /// <param name="weight">The amount to increase traffic by, in total.</param>
        protected void DistributeBoidWeightOverQuad(ref float[][] trafficGrid, float[] boidCoords, float weight = 1) =>
            DistributeBoidWeightOverQuad(ref trafficGrid, boidCoords, Bounds, weight);

        /// <summary>
        /// Increases weights within the traffic grid based on a boid's position therein.
        /// </summary>
        /// <param name="trafficGrid">The grid to increase the traffic upon. Row = x, col = y</param>
        /// <param name="boidCoords">The coordinates of the boid to distribute the weight over. (x,y)</param>
        /// <param name="bounds">The boundaries of the grid. minX, minY, maxX, maxY. Parameter should eventually be replaced.</param>
        /// <param name="weight">The amount to increase traffic by, in total.</param>
        /// <remarks>
        /// Many of the calculations in this function should be performed fewer times than this funciton is called. This needs to be fixed. Specifically, it would make more sense to pass 
        /// the cell size into this function, since this will usually remain constant.
        /// </remarks>
        public static void DistributeBoidWeightOverQuad(ref float[][] trafficGrid, float[] boidCoords, float[] bounds, float weight = 1)
        {
            int[][] targetGridIndicies = new int[4][]; // nw, sw, ne, se
            float[][] targetVertexBoidSpace = new float[4][];
            float[] gridSize = { bounds[2] - bounds[0], bounds[3] - bounds[1] }; // Width, height
            // Remember: # cells = # verticies - 1
            float[] cellSize = { gridSize[0] / (trafficGrid.Length - 1), gridSize[1] / (trafficGrid[0].Length - 1)}; // Width(rows), height(cols)

            // Calculate the boid's quad. Lots of this could be easily put into a loop.
            Console.WriteLine("CellX: " + cellSize[0] + " CellY: " + cellSize[1]);
            Console.WriteLine("MinX: " + bounds[0] + " MinY: " + bounds[1] + " MaxX: " + bounds[2] + " MaxY: " + bounds[3]);
            int xLowerIndex = (int)MathF.Floor((boidCoords[0] - bounds[0]) / cellSize[0]);
            int yLowerIndex = (int)MathF.Floor((boidCoords[1] - bounds[1]) / cellSize[1]);
            int xUpperIndex = (int)MathF.Ceiling((boidCoords[0] - bounds[0]) / cellSize[0]);
            int yUpperIndex = (int)MathF.Ceiling((boidCoords[1] - bounds[1]) / cellSize[1]);
            Console.WriteLine("LowerX: " + xLowerIndex + " LowerY: " + yLowerIndex + " UpperX: " + xUpperIndex + " UpperY: " + yUpperIndex);
            targetGridIndicies[0] = new int[] { xLowerIndex, yLowerIndex };
            targetGridIndicies[1] = new int[] { xLowerIndex, yUpperIndex };
            targetGridIndicies[2] = new int[] { xUpperIndex, yLowerIndex };
            targetGridIndicies[3] = new int[] { xUpperIndex, yUpperIndex };
            targetVertexBoidSpace[0] = new float[] { (targetGridIndicies[0][0] * cellSize[0]) - bounds[0], (targetGridIndicies[0][1] * cellSize[1]) - bounds[1] };
            targetVertexBoidSpace[1] = new float[] { (targetGridIndicies[1][0] * cellSize[0]) - bounds[0], (targetGridIndicies[1][1] * cellSize[1]) - bounds[1] };
            targetVertexBoidSpace[2] = new float[] { (targetGridIndicies[2][0] * cellSize[0]) - bounds[0], (targetGridIndicies[2][1] * cellSize[1]) - bounds[1] };
            targetVertexBoidSpace[3] = new float[] { (targetGridIndicies[3][0] * cellSize[0]) - bounds[0], (targetGridIndicies[3][1] * cellSize[1]) - bounds[1] };

            // Calculate distance to corners of quad
            float[] distances = new float[4];
            float distanceSum = 0;
            for (int i = 0; i < 4; i++) 
            {
                // Apply pythagorean theroem
                distances[i] = MathF.Sqrt(MathF.Pow(targetVertexBoidSpace[i][0]-boidCoords[0], 2) + MathF.Pow(targetVertexBoidSpace[i][1] - boidCoords[1], 2));
                distanceSum += distances[i];
            }
            // Apply weights

            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine("Can I write to grid [" + targetGridIndicies[i][0] + "][" + targetGridIndicies[i][1] + "]?");
                if (targetGridIndicies[i][0] >= trafficGrid.Length || targetGridIndicies[i][1] >= trafficGrid[targetGridIndicies[i][0]].Length)
                Console.WriteLine("Cannot write to grid ["+ targetGridIndicies[i][0] + "]["+ targetGridIndicies[i][1] + "]");
                trafficGrid[targetGridIndicies[i][0]][targetGridIndicies[i][1]] += weight * (distances[i] / distanceSum);
            }
        }

        public enum MappingMode
        {
            /// <summary>
            /// Distribute 1 point of weight among the four verticies in the quad the boids belong to for each sample.
            /// </summary>
            SAME_QUAD,
            /// <summary>
            /// Distribute 1 point of weight across the entire grid for each boid for each sample.
            /// </summary>
            ALL_QUAD
        }
    }
}
