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

        private List<float> deltaTime;

        public List<float> DeltaTime { 
            get
            {
                if (deltaTime == null || deltaTime.Count != Current.Count)
                    PopulateDeltaTime();
                return deltaTime;
            }
        }

        /// <summary>
        /// Fills the list <see cref="deltaTime"/> with the differences in times between the snapshots in <see cref="Current"/>
        /// </summary>
        /// <returns>True if the deltaTimes are all positive (i.e. <see cref="Current"/> is sorted)</returns>
        private bool PopulateDeltaTime()
        {
            bool ok = true;
            deltaTime = new List<float>();
            float avg = 0;
            for (int i = 0; i < Current.Count - 1; i++)
            {
                float dt = Current[i + 1].Timestamp - Current[i].Timestamp;
                if (dt < 0)
                {
                    Console.Error.WriteLine("Warning: BoidsExperiment.Current is not sorted. Is there an error in the source data file?");
                    ok = false;
                }
                deltaTime.Add(dt);
                avg += dt;
            }
            deltaTime.Add(avg / (Current.Count - 1));
            return ok;
        }

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
            Regex snapshotReader = new Regex(@"(?<time>[\d\.\-]+):(?:(?:-?[\d\.]+),(?:-?[\d\.]+);)*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
            // Reads each ordered pair in the snapshot.
            Regex orderedPairFinder = new Regex(@"(?<coordx>-?[\d\.]+),(?<coordy>-?[\d\.]+);",
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
        public string BoidPly(int gridSize = 21)
        {
            // Get the trafic density
            Tuple<float[][], float[][][]> gridWeights = ProjectBoidsToGrid(gridSize);
            float[][] traffic = gridWeights.Item1;
            float[][][] paths = gridWeights.Item2;
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
                    ply_file.Append("\t" + paths[i][j][0]); //vx
                    ply_file.Append("\t" + paths[i][j][1]); //vy
                    ply_file.Append("\t" + paths[i][j][2]); //vz
                    ply_file.Append("\t" + traffic[i][j] + "\n"); //s
                }
            }

            int offset = -1;
            for (int i = 0; i < faceCount; i++)
            { // For each face
                if (i % (gridSize-1) == 0)
                    offset++; // Don't wrap around corners
                int comb = i + offset;
                ply_file.Append("4 "); // Number of verticies.
                ply_file.Append(comb + " "); // SE corner (xHigh, yHigh)
                ply_file.Append((comb + 1) + " "); // SW corner (xLow, yHigh)
                ply_file.Append((comb + gridSize + 1) + " "); // NW corner (xLow, yLow)
                ply_file.Append((comb + gridSize) + "\n"); // NE corner (xHigh, yLow)
            }

            // Done!
            return ply_file.ToString();
        }

        /// <summary>
        /// Generates a <c>.ply</c> file header to associate with a <paramref name="gridSize"/>x<paramref name="gridSize"/> grid of values.<para/>
        /// Data to be stored in the <c>.ply</c> file is vector and scalar data for each vertex, and faces connecting the verticies on the grid.
        /// </summary>
        /// <param name="gridSize">The length and width of the grid.</param>
        /// <returns>A header for the <c>.ply</c> file format (newline terminated).</returns>
        /// <remarks>Could be augmented so the grid does not have to be square, but would also require work in whatever other functions are involved in generating the <c>ply</c> file.</remarks>
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
        /// <remarks>
        /// Weighted at 1 unit of weight per second.
        /// </remarks>
        protected Tuple<float[][], float[][][]> ProjectBoidsToGrid(int gridSize = 21, float maxTime = float.PositiveInfinity, MappingMode mode = MappingMode.SAME_QUAD)
        {
            // Initialization
            float[][] trafficGrid = new float[gridSize][];
            float[][][] pathGrid = new float[gridSize][][];
            float[][] weightGrid = new float[gridSize][];
            for (int row = 0; row < gridSize; row++)
            {
                trafficGrid[row] = new float[gridSize];
                pathGrid[row] = new float[gridSize][];
                weightGrid[row] = new float[gridSize];
                for (int col = 0; col < gridSize; col++)
                {
                    trafficGrid[row][col] = 0;
                    float[] zeroVector = { 0, 0, 0 };
                    pathGrid[row][col] = zeroVector;
                    weightGrid[row][col] = 0;
                }
            }

            if (mode == MappingMode.SAME_QUAD)
            {
                // Add weights
                for (int i = 0; i < Current.Count - 1; i++)
                {
                    BoidsSnapshot snapshot = Current[i];
                    if (snapshot.Timestamp < maxTime) // Since the list is not garunteed to be sorted, we can't break when we pass the max time.
                    {
                        for (int j = 0; j < snapshot.Coords.Count; j++)
                        {
                            float[] coord = snapshot.Coords[j];
                            float[] direction = 
                            {
                                Current[i+1].Coords[j][0] - snapshot.Coords[j][0], // x
                                Current[i+1].Coords[j][1] - snapshot.Coords[j][1], // y
                                0 // z
                            };
                            DistributeBoidWeightOverQuad(ref trafficGrid, ref pathGrid, coord, direction, ref weightGrid, DeltaTime[i]);
                        }
                    }
                }
                // Special case for last snapshot; no next vector direction, so vector must be either considered zero or set to the last vector for that boid.
                BoidsSnapshot lastSnapshot = Current[Current.Count - 1];
                for (int j = 0; j < lastSnapshot.Coords.Count; j++)
                {
                    float[] coord = lastSnapshot.Coords[j];
                    float[] direction = {0, 0, 0};
                    DistributeBoidWeightOverQuad(ref trafficGrid, ref pathGrid, coord, direction, ref weightGrid, DeltaTime[Current.Count-1]);
                }
            }
            else if (mode == MappingMode.ALL_QUAD)
                throw new System.NotImplementedException("Mapping mode not supported: All quads.");

            return new Tuple<float[][], float[][][]>(trafficGrid,pathGrid);
        }

        /// <summary>
        /// Increases weights within the traffic grid based on a boid's position therein.
        /// </summary>
        /// <param name="trafficGrid">The grid to increase the traffic upon.</param>
        /// <param name="pathGrid">The grid to increase the pathing on. Row = x, col = y</param>
        /// <param name="boidCoords">The coordinates of the boid to distribute the weight over. (x,y)</param>
        /// <param name="weightGrid">The number of vectors applied to each vertex. Used for adding new vectors as parts of the total being averaged. Does not need to be an int.</param>
        /// <param name="weight">The amount to increase traffic by, in total.</param>
        protected void DistributeBoidWeightOverQuad(ref float[][] trafficGrid, ref float[][][] pathGrid, float[] boidCoords, float[] boidVector, ref float[][] weightGrid, float weight = 1) =>
            DistributeBoidWeightOverQuad(ref trafficGrid, ref pathGrid, boidCoords, boidVector, Bounds, ref weightGrid, weight);

        /// <summary>
        /// Increases weights within the traffic grid based on a boid's position therein.
        /// </summary>
        /// <param name="trafficGrid">The grid to increase the traffic upon. Row = x, col = y</param>
        /// <param name="pathGrid">The grid to increase the pathing on. Row = x, col = y</param>
        /// <param name="boidCoords">The coordinates of the boid to distribute the weight over. (x,y)</param>
        /// <param name="boidVector">The direction the boid is traveling in. Should already be adjusted for time. (x,y,z)</param>
        /// <param name="bounds">The boundaries of the grid. minX, minY, maxX, maxY. Parameter should eventually be replaced.</param>
        /// <param name="weightGrid">The number of vectors applied to each vertex. Used for adding new vectors as parts of the total being averaged. Does not need to be an int.</param>
        /// <param name="trafficWeight">The amount to increase traffic by, in total.</param>
        /// <remarks>
        /// Many of the calculations in this function should be performed fewer times than this funciton is called. This needs to be fixed. Specifically, it would make more sense to pass 
        /// the cell size into this function, since this will usually remain constant.
        /// </remarks>

        public static void DistributeBoidWeightOverQuad(ref float[][] trafficGrid, ref float[][][] pathGrid, float[] boidCoords, float[] boidVector, float[] bounds, ref float[][] weightGrid, float trafficWeight = 1)
        {
            int[][] targetGridIndicies = new int[4][]; // nw, sw, ne, se
            float[][] targetVertexBoidSpace = new float[4][];
            float[] gridSize = { bounds[2] - bounds[0], bounds[3] - bounds[1] }; // Width, height
            // Remember: # cells = # verticies - 1
            float[] cellSize = { gridSize[0] / (trafficGrid.Length - 1), gridSize[1] / (trafficGrid[0].Length - 1)}; // Width(rows), height(cols)

            // Calculate the boid's quad. Lots of this could be easily put into a loop.
            int xLowerIndex = (int)MathF.Floor((boidCoords[0] - bounds[0]) / cellSize[0]);
            int yLowerIndex = (int)MathF.Floor((boidCoords[1] - bounds[1]) / cellSize[1]);
            int xUpperIndex = (int)MathF.Ceiling((boidCoords[0] - bounds[0]) / cellSize[0]);
            int yUpperIndex = (int)MathF.Ceiling((boidCoords[1] - bounds[1]) / cellSize[1]);
            targetGridIndicies[0] = new int[] { xLowerIndex, yLowerIndex };
            targetGridIndicies[1] = new int[] { xLowerIndex, yUpperIndex };
            targetGridIndicies[2] = new int[] { xUpperIndex, yLowerIndex };
            targetGridIndicies[3] = new int[] { xUpperIndex, yUpperIndex };
            targetVertexBoidSpace[0] = new float[] { (targetGridIndicies[0][0] * cellSize[0]) + bounds[0], (targetGridIndicies[0][1] * cellSize[1]) + bounds[1] };
            targetVertexBoidSpace[1] = new float[] { (targetGridIndicies[1][0] * cellSize[0]) + bounds[0], (targetGridIndicies[1][1] * cellSize[1]) + bounds[1] };
            targetVertexBoidSpace[2] = new float[] { (targetGridIndicies[2][0] * cellSize[0]) + bounds[0], (targetGridIndicies[2][1] * cellSize[1]) + bounds[1] };
            targetVertexBoidSpace[3] = new float[] { (targetGridIndicies[3][0] * cellSize[0]) + bounds[0], (targetGridIndicies[3][1] * cellSize[1]) + bounds[1] };

            // Calculate distance to corners of quad.
            float[] distances = new float[4];
            float distanceSum = 0;
            for (int i = 0; i < 4; i++) 
            {
                // Apply pythagorean theroem
                distances[i] = MathF.Sqrt(MathF.Pow(targetVertexBoidSpace[i][0]-boidCoords[0], 2) + MathF.Pow(targetVertexBoidSpace[i][1] - boidCoords[1], 2));
                distanceSum += distances[i];
            }

            // Add the weighted values to the underlying grids.
            for (int i = 0; i < 4; i++)
            {
                // Scalar data: Relative traffic at that vertex. Appply time-based weighting.
                trafficGrid[targetGridIndicies[i][0]][targetGridIndicies[i][1]] += trafficWeight - (trafficWeight * (distances[i] / distanceSum));
                // Vector data: Path at that vertex. Do not apply time-based weighting; vectors should already account for this.
                for (int j = 0; j < 3; j++)
                {
                    float portion = 1 - (distances[i] / distanceSum); // Portion of the new component to add.
                    float newComp = boidVector[j]*portion; // New component to be added to this specific vertex
                    float prevWeight = weightGrid[targetGridIndicies[i][0]][targetGridIndicies[i][1]]; // Amount of vectors that have been added so far.
                    float newWeight = prevWeight + portion;
                    pathGrid[targetGridIndicies[i][0]][targetGridIndicies[i][1]][j] *= prevWeight;
                    pathGrid[targetGridIndicies[i][0]][targetGridIndicies[i][1]][j] += newComp;
                    pathGrid[targetGridIndicies[i][0]][targetGridIndicies[i][1]][j] /= newWeight;
                    weightGrid[targetGridIndicies[i][0]][targetGridIndicies[i][1]] = newWeight;
                }
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
