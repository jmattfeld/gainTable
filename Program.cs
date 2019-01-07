using System;
using System.IO;
using System.Linq;

namespace GainTable
{
    class Program
    {
        static float[] fTempHist = new float[40];
        static float[] fTempHistF1 = new float[40];
        static float[] fTempHistF2 = new float[40];
        static float[] fTempDerHist = new float[40];
        static float[] fTempDerHistF1 = new float[40];
        static float[] fTempDerHistF2 = new float[40];
        static float[] fTempDerHistF3 = new float[40];
        static float[] fTempDerHistF4 = new float[40];
        static float[] fTempDerHistF5 = new float[40];
        static float[] fTempDerHistF6 = new float[40];
        static float[] fTempDerHistF7 = new float[40];
        static float[] fTempDerHistF8 = new float[40];

        public static void Main(string[] args)
        {
            // define some contants
            const int NUM_CHANNELS = 64;
            const int NUM_SAMPLES = 7299;
            const decimal MIN_GAIN = 0.00M;
            const decimal MAX_GAIN = 6.00M;
            const decimal STEP_GAIN = 0.05M;

            const string INPUT_PATH = "/home/jeremymelinda/Source/Cs_Projects/GainTable/data/";
            const string INPUT_FILE = "Ts_10_50_15.csv";

#if (true)
            // borrowed from TransTemp (for calculating the derivative signal)
            float fFilteredTemp;
            float fFilteredTemp1;
            float fDerUn;
            float fDer;
            float fDer1;
            float fDer2;
            float fDer3;
            float fDer4;
            float fDer5;
            float fDer6;
            float fDer7;
            float fDer8;
            int avgamt = 2; // Was 2, 4
            int avgamtd = 2; // Was 2, 4
            int deramt = 1;

            // initialize structures for input data
            float[] tempData = new float[NUM_SAMPLES]; //just using T1 for now
            int[,] rawData = new int[NUM_SAMPLES,NUM_CHANNELS];

            // array for the temperature derivative
            float[] dTemp = new float[NUM_SAMPLES];
            // initialize temporary and best pressure arrays for step function
            float[] tmpArr = new float[NUM_SAMPLES];
            float[] bestArr = new float[NUM_SAMPLES];
            // initialize array of gain values
            float[] gainArr = new float[NUM_CHANNELS];
            // read the transient step data into a structure
            string dataFile = INPUT_PATH + INPUT_FILE;
            StreamReader stDataFile;
            stDataFile = new StreamReader(dataFile);
            Console.WriteLine("Opened file " + INPUT_FILE);
            Console.WriteLine("Parsing lines into data structures...");
            string sLine;
            while ((sLine = stDataFile.ReadLine()) != null)
            {
                // Read a line
                // Break into tokens delimited by comma
                string[] sToken = sLine.Split(',');
                parseLine(tempData, rawData, sToken, NUM_CHANNELS);
            }
            stDataFile.Close();
            Console.WriteLine("Closed file " + INPUT_FILE);

            // calculate derivative
            Console.WriteLine("Calculating temperature derivative...");
            for (int i = 0; i < NUM_SAMPLES; i++)
            {
                UpdateHistory(tempData[i], fTempHist);
                fFilteredTemp = RollingAvgFilter(fTempHist, avgamt);

                UpdateHistory(fFilteredTemp, fTempHistF1);
                fFilteredTemp1 = RollingAvgFilter(fTempHistF1, avgamt);

                UpdateHistory(fFilteredTemp1, fTempHistF2);

                // Derivative
                fDerUn = Derivative(fTempHistF2, deramt);  // Was 1 with .5

                // Filter derivative
                UpdateHistory(fDerUn, fTempDerHist);
                fDer = RollingAvgFilter(fTempDerHist, avgamtd);

                UpdateHistory(fDer, fTempDerHistF1);
                fDer1 = RollingAvgFilter(fTempDerHistF1, avgamtd);

                UpdateHistory(fDer1, fTempDerHistF2);
                fDer2 = RollingAvgFilter(fTempDerHistF2, avgamtd);

                UpdateHistory(fDer2, fTempDerHistF3);
                fDer3 = RollingAvgFilter(fTempDerHistF3, avgamtd);

                UpdateHistory(fDer3, fTempDerHistF4);
                fDer4 = RollingAvgFilter(fTempDerHistF4, avgamtd);

                UpdateHistory(fDer4, fTempDerHistF5);
                fDer5 = RollingAvgFilter(fTempDerHistF5, avgamtd);

                UpdateHistory(fDer5, fTempDerHistF6);
                fDer6 = RollingAvgFilter(fTempDerHistF6, avgamtd);

                UpdateHistory(fDer6, fTempDerHistF7);
                fDer7 = RollingAvgFilter(fTempDerHistF7, avgamtd);

                UpdateHistory(fDer7, fTempDerHistF8);
                fDer8 = RollingAvgFilter(fTempDerHistF8, avgamtd);

                dTemp[i] = fDer8;
            }
#endif

            // channel index
            int chIdx;
            int frameIdx;
            float fCorTemp;

            // for each Px channel
            for (chIdx = 0; chIdx < NUM_CHANNELS; chIdx++)
            {
                System.Console.WriteLine("Calculating optimum gain for Channel {0}", chIdx + 1);
                if (chIdx == 6) // just do channel 7 for now
                {
                    decimal thisGain;
                    // for each gain value in gain array from 1 to 6 in 0.05 increments
                    for (thisGain = MIN_GAIN; thisGain <= MAX_GAIN; thisGain += STEP_GAIN)
                    {
                        //System.Console.WriteLine("Ch{0}: Trying gain={1}", chIdx + 1, thisGain);
                        // for each frame
                        for (frameIdx = 0; frameIdx < NUM_SAMPLES; frameIdx++)
                        {
                            // calculate temperature correction
                            fCorTemp = tempData[frameIdx] - (dTemp[frameIdx] * (float)thisGain);
                            // calculate CTP
                            //cvtCreateCtp(fCorTemp, chIdx + 1);
                            // calculate corrected pressure -> tempArray
                        }
                            // find min and max errors in pressure array for this gain
                            // if tempArray.avg(minErr,maxErr) < bestArray.avg(minErr,Maxerr)
                                // bestArray = tempArray
                    }
                }
                else
                    continue;
            }
        }

        private static void parseLine(float[] tempData, int[,] rawData, string[] sToken, int nCh)
        {
            // temp1 is token[3] raw data is tokens[140:203]
            int i;
            // get y index from frame number
            int idx = Convert.ToInt16(sToken[0]) - 1;

            // set tempData
            tempData[idx] = Convert.ToSingle(sToken[3]);

            // set rawData
            for (i = 0; i < nCh; i++)
            {
                rawData[idx,i] = Convert.ToInt32(sToken[i + 140]);
            }
        }

        private static float RollingAvgFilter(float[] fTempHistory, int samples)
        {
            float fRet;
            int s;
            float fSum = 0.0F;

            for (s=0; s<samples; s++)
            {
                fSum = fSum + fTempHistory[s];
            }

            fRet = fSum / (float)samples;

            return fRet;
        }

        private static void UpdateHistory(float newvalue, float[] fTempHistory)
        {
            // Shift old temp samples
            for (int i = 19; i > 0; i--)
            {
                fTempHistory[i] = fTempHistory[i - 1];
            }
            fTempHistory[0] = newvalue;

            return;
        }

        private static float Derivative(float[] fTempHist, int delta)
        {
            float fRet;
            fRet = fTempHist[0] - fTempHist[delta];
            return fRet;
        }
    }
}
