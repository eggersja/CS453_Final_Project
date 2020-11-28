using NUnit.Framework;
using boidsTransformer;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System;

namespace boidTest
{

    /// <summary>
    /// Simple set of tests for the data transformer.
    /// </summary>
    public class transformerTests
    {
        public static readonly string SYNTHETIC_EASY = "10.0:20.0;30.0#40.0;50.0#";
        
        public static readonly float[] SYNTHETIC_EASY_ANSWERS = { 10f, 20f, 30f, 40f, 50f };
        
        public static readonly string SYNTHETIC_MEDIUM = "0.01:0;3234.43#-20.4;887#";
        
        public static readonly float[] SYNTHETIC_MEDIUM_ANSWERS = { 0.01f, 0f, 3234.43f, -20.4f, 887f };

        /// <summary>
        /// The relative location of the root directory of the test project compared to the location of the executable.
        /// </summary>
        public static readonly string TEST_DIRECTORY = "../../../";

        public static readonly string TEST_OUTPUT_DIRECTORY = "../../../o/";

        /// <remarks>Make sure not to move this by accident.
        /// Tests referencing this should include descriptive error messages describing failures that happen due to this mistake.</remarks>
        public static readonly string BASIC_TEST_DATA_PATH = TEST_DIRECTORY + "test_data/basic.t1.boids";

        [SetUp]
        public void Setup()
        {
        }

        /// <summary>
        /// Tests a single syntehtic intialization of dummy data, containing a single row of <see cref="SYNTHETIC_EASY"/>.
        /// </summary>
        [Test]
        public void SingleSnapshot()
        {
            // Initialize
            string[] easyDummy = { SYNTHETIC_EASY };

            // Execute
            BoidsExperiment easy = new BoidsExperiment(easyDummy);

            // Check
            Assert.IsTrue(easy.Current.Count == 1);
            BoidsExperiment.BoidsSnapshot snap = easy.Current[0];
            Assert.IsTrue(snap.Timestamp == SYNTHETIC_EASY_ANSWERS[0], "snap.Timestamp is " + snap.Timestamp + ", expected " + SYNTHETIC_EASY_ANSWERS[0]);
            int pairs = (SYNTHETIC_EASY_ANSWERS.Length - 1) / 2;
            Assert.IsTrue(snap.Coords.Count == pairs); // Probably could be simplified for test purposes?
            for(int i = 0; i < pairs; i++)
            {
                float[] currentPair = snap.Coords[i];
                Assert.IsTrue(currentPair[0] == SYNTHETIC_EASY_ANSWERS[i*2+1]);
                Assert.IsTrue(currentPair[1] == SYNTHETIC_EASY_ANSWERS[i * 2 + 2]);
            }
        }

        /// <summary>
        /// Makes sure a real <see cref="BoidsExperiment"/> can be instantiated with a real file.
        /// </summary>
        [Test]
        public void LoadRealFile()
        {
            // Sanity check
            FileAssert.Exists(BASIC_TEST_DATA_PATH, "The file " + BASIC_TEST_DATA_PATH + " was moved or is missing.\n" +
                "This test will not work without a proper test file in this location that has at least one boid.\n" +
                "(Present execution directory: " + Directory.GetCurrentDirectory() + ")");

            // Setup and Execute
            BoidsExperiment realExperiment = new BoidsExperiment(BASIC_TEST_DATA_PATH);

            // Test
            Assert.IsTrue(realExperiment.Current.Count > 0, "The file " + BASIC_TEST_DATA_PATH + "did not contain any parseable data.");
            Assert.IsTrue(realExperiment.BoidCount > 0, "The file " + BASIC_TEST_DATA_PATH + "did not result in a boid being instantiated.");
        }

        /// <summary>
        /// Verifies the ability of a <see cref="BoidsExperiment"/> to format itself as a <c>.ply</c> file. Depends on <see cref="SingleSnapshot"/>.
        /// </summary>
        [Test]
        public void PlyGeneration()
        {
            // Initialize
            string[] easyDummy = { SYNTHETIC_EASY };

            // Execute
            BoidsExperiment easy = new BoidsExperiment(easyDummy);
            string ply = easy.BoidPly(); // Don't accidentally ToString your BoidsExperiments; you will NOT get a ply file.

            // Check
            Assert.IsNotNull(ply);

            // Prepare the correct header.
            StringBuilder correctPlyHeader = new StringBuilder();
            correctPlyHeader.Append("ply\n");
            correctPlyHeader.Append("format ascii 1.0\n");
            correctPlyHeader.Append("comment created by boidsTransformer\n");
            correctPlyHeader.Append("element vertex " + 441 + "\n");
            correctPlyHeader.Append("property float64 x\n");
            correctPlyHeader.Append("property float64 y\n");
            correctPlyHeader.Append("property float64 z\n");
            correctPlyHeader.Append("property float64 vx\n");
            correctPlyHeader.Append("property float64 vy\n");
            correctPlyHeader.Append("property float64 vz\n");
            correctPlyHeader.Append("property float64 s\n");
            correctPlyHeader.Append("element face " + 400 + "\n");
            correctPlyHeader.Append("property list uint8 int32 vertex_indices\n");
            correctPlyHeader.Append("end_header\n");

            string correct_ply_str = correctPlyHeader.ToString();

            Assert.IsTrue(ply.Length > 0);
            Assert.IsTrue(ply.Length > correctPlyHeader.Length);
            Assert.IsTrue(correctPlyHeader.Equals(ply.Substring(0, correct_ply_str.Length)),
                "Expected: \"" + correct_ply_str + "\", but got \"" + ply.Substring(0, correct_ply_str.Length) + "\".");
            // REGEX check the body.
            Regex bodyCheck = new Regex(@"(?:(?:-?[\d\.]+\s+){7})+(?:(?:[\d]+\s+){5})+",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            Assert.IsTrue(bodyCheck.IsMatch(ply));
        }

        /// <summary>
        /// Runs the <see cref="Program.Main(string[])">main method</see> on the <see cref="TEST_OUTPUT_DIRECTORY">sample data</see>,
        /// producing output <c>.ply</c> data to <see cref="TEST_OUTPUT_DIRECTORY"/><c>/o/basic.t1.boids.ply</c>.
        /// </summary>
        [Test]
        public void IntegrationSuccess()
        {
            // Sanity check
            FileAssert.Exists(BASIC_TEST_DATA_PATH, "The file " + BASIC_TEST_DATA_PATH + " was moved or is missing.\n" +
                "This test will not work without a proper test file in this location that has at least one boid.\n" +
                "(Present execution directory: " + Directory.GetCurrentDirectory() + ")");

            // Setup
            string outFile = TEST_OUTPUT_DIRECTORY + "basic.t1.boids.ply";
            if (File.Exists(outFile))
                File.Delete(outFile);
            string[] args = { BASIC_TEST_DATA_PATH, "-o", TEST_OUTPUT_DIRECTORY.Substring(0, TEST_OUTPUT_DIRECTORY.Length-1) };

            // Execute
            Program.Main(args);

            // Test
            FileAssert.Exists(outFile, "Program failed to create " + outFile);
            // Cleanup
            File.Delete(outFile);
        }

        /// <summary>
        /// Runs the <see cref="Program.Main(string[])">main method</see> on the <see cref="TEST_OUTPUT_DIRECTORY">sample data</see>,
        /// producing output <c>.ply</c> data to <see cref="TEST_OUTPUT_DIRECTORY"/><c>/o/basic.t1.boids.ply</c>. Uses a high resolution.
        /// </summary>
        [Test]
        public void IntegrationSuccessHighRez()
        {
            // Sanity check
            FileAssert.Exists(BASIC_TEST_DATA_PATH, "The file " + BASIC_TEST_DATA_PATH + " was moved or is missing.\n" +
                "This test will not work without a proper test file in this location that has at least one boid.\n" +
                "(Present execution directory: " + Directory.GetCurrentDirectory() + ")");

            // Setup
            string outFile = TEST_OUTPUT_DIRECTORY + "basic.t1.boids.ply";
            if (File.Exists(outFile))
                File.Delete(outFile);
            string[] args = { BASIC_TEST_DATA_PATH, "-o", TEST_OUTPUT_DIRECTORY.Substring(0, TEST_OUTPUT_DIRECTORY.Length - 1), "-s", "49" };

            // Execute
            Program.Main(args);

            // Test
            FileAssert.Exists(outFile, "Program failed to create " + outFile);
            // Cleanup
            File.Delete(outFile);
        }
    }
}