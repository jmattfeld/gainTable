using System;
using System.Linq;

namespace GainTable
{
    class Program
    {
        static void Main(string[] args)
        {
            const int NUM_CHANNELS = 64;
            const int NUM_SAMPLES = 7299;

            const decimal MIN_GAIN = 1.00M;
            const decimal MAX_GAIN = 6.00M;
            const decimal STEP_GAIN = 0.05M;

            //int maxValue = tempArray.Max();
            //int maxIndex = tempArray.ToList().IndexOf(maxValue);
            //System.Console.WriteLine("max of tempArray={0} at index={1}", maxValue, maxIndex);

            // initialize array of gain values
            float[] gainArr = new float[NUM_CHANNELS];

            // initialize temporary and best pressure arrays for step function
            float[] tempArr = new float[NUM_SAMPLES];
            float[] bestArr = new float[NUM_SAMPLES];

            // read the transient step data into a structure

            // channel index
            int chIdx;

            // for each Px channel
            for (chIdx = 0; chIdx < NUM_CHANNELS; chIdx++)
            {
                System.Console.WriteLine("Calculating optimum gain for Channel {0}", chIdx + 1);
                decimal thisGain;
                // for each gain value in gain array from 1 to 6 in 0.05 increments
                for (thisGain = MIN_GAIN; thisGain <= MAX_GAIN; thisGain += STEP_GAIN)
                {
                    System.Console.WriteLine("Ch{0}: Trying gain={1}", chIdx + 1, thisGain);
                    // for each frame
                        // calculate temperature correction
                            // get temp derivative
                        // calculate CTP
                        // calculate corrected pressure -> tempArray

                    // find min and max errors in pressure array for this gain
                    // if tempArray.avg(minErr,maxErr) < bestArray.avg(minErr,Maxerr)
                        // bestArray = tempArray
                }
            }
        }
    }
}
