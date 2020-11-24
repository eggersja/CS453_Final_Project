using NUnit.Framework;
using boidsTransformer;

namespace boidTest
{

    /// <summary>
    /// Simple set of tests for the data transformer.
    /// </summary>
    public class transformerTests
    {
        public static readonly string SYNTHETIC_EASY = "10.0:20.0,30.0;40.0,50.0;";
        public static readonly float[] SYNTHETIC_EASY_ANSWERS = { 10, 20, 30, 40, 50 };
        public static readonly string SYNTHETIC_MEDIUM = "0.01:0,3234.43;-20.4,887;";

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
    }
}