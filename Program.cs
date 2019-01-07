using System;
using System.IO;
using System.Linq;

namespace GainTable
{
    class Program
    {
        // filter stuff
        // FILTER_LEN = 25, cutoff freq = 0.0078125
        static double[] coeffs = new double[] {
                          0.0060796, 0.0072834, 0.0107989, 0.0163963, 0.0237011,
                          0.0322199, 0.0413733, 0.0505369, 0.0590842, 0.0664295,
                          0.0720689, 0.0756154, 0.0768252, 0.0756154, 0.0720689,
                          0.0664295, 0.0590842, 0.0505369, 0.0413733, 0.0322199,
                          0.0237012, 0.0163963, 0.0107989, 0.0072834, 0.0060796 };
        const int FILTER_LEN = 25;
        const uint INPUT_SAMPLE_LEN = 1;
        const uint INPUT_BUFFER_LEN = (FILTER_LEN - 1 + INPUT_SAMPLE_LEN);
        static float[] sampleBuffer = new float[INPUT_BUFFER_LEN];
        static uint idx;

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

        // Master calibration tables arrays
        static int[] nMLen = new int[64];              // Current length of master point table
        static float[,] fMTemp = new float[64, 450];   	// Temperatures
        static float[,] fMPress = new float[64, 450];		// Pressures
        static long[,] lMCounts = new long[64, 450];       // Counts

        static int nNumCalT = 15;            // Number of calibration temperature points
        static int nNumCalP = 25;          // Number of calibration pressure points

        static float[] fTemp = new float[64];

        // Low temp pressures for current temperature plane
        static float[] fLCTemp = new float[25];    // Temperatures
        static float[] fLCPress = new float[25];   // Pressures
        static long[] lLCCounts = new long[25];    // Counts

        // High temp pressures for current temperature plane
        static float[] fHCTemp = new float[25];    // Temperatures
        static float[] fHCPress = new float[25];   // Pressures
        static long[] lHCCounts = new long[25];   // Counts

        // Current temperature plane
        static float[,] fCPress = new float[64, 25];	// Pressures
        static long[,] lCCounts = new long[64, 25];	    // Counts
        static float[,] fCCounts = new float[64, 25];	// Counts in float form
        static int[] nCNumPress = new int[64];		// Number of pressures in the CTP

        // Current Temperature Plane Data Structure, used by pressure conversion
        static float[] fMNdxCtp = new float[64];
        static float[] fBNdxCtp = new float[64];
        static float[,] fMctp = new float[64, 25];
        static float[,] fBctp = new float[64, 25];

        public static void Main(string[] args)
        {
            // define some contants
            const int NUM_CHANNELS = 64;
            const int NUM_SAMPLES = 7299;
            const decimal MIN_GAIN = 0.00M;
            const decimal MAX_GAIN = 10.00M;
            const decimal STEP_GAIN = 0.05M;
            //const decimal GAIN_TO_WRITE = 2.35M;
            const float BIG_FLOAT = 1.0F;

            const string INPUT_PATH = "C:\\Users\\Jeremy.SV\\Desktop\\gaintable\\data\\";
            const string OUTPUT_PATH = "C:\\Users\\Jeremy.SV\\Desktop\\gaintable\\output\\";
            const string CAL_FILE = "CalSingleTemp.cfg";
            const string INPUT_FILE = "Ts_10_50_15.csv";
            const string OUTPUT_FILE = "ch59_filtered_8_15.csv";
            const string GAIN_FILE = "Tgain.cfg";

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
            // initialize temporary and filtered pressure arrays for step function
            float[] tmpArr = new float[NUM_SAMPLES];
            float[] filteredTmpArr = new float[NUM_SAMPLES];
            // initialize array of gain values
            decimal[] gainArr = new decimal[NUM_CHANNELS];
            
            // process the cal file
            string sLine;
            int nLineNo;
            string calFile = INPUT_PATH + CAL_FILE;
            StreamReader stCalFile;
            stCalFile = new StreamReader(calFile);
            Console.WriteLine("Opened file " + CAL_FILE);

            // Loop through file
            nLineNo = 0;
            while ((sLine = stCalFile.ReadLine()) != null)
            {
                // Break into tokens delimited by comma
                string[] sToken = sLine.Split(' ');
                ProcessCal(nLineNo, sToken);
                nLineNo++;
            }
            stCalFile.Close();
            Console.WriteLine("Closed file " + CAL_FILE);
            
            // read the transient step data into a structure
            string dataFile = INPUT_PATH + INPUT_FILE;
            StreamReader stDataFile;
            stDataFile = new StreamReader(dataFile);
            Console.WriteLine("Opened file " + INPUT_FILE);
            Console.WriteLine("Parsing lines into data structures...");
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
            float thisMax;
            float thisMin;
            float idxMax;
            float idxMin;
            float thisDelta;
            float curMinDelta = BIG_FLOAT;
            decimal thisGain;
            decimal curBestGain = MIN_GAIN;
            string sPress;

            // for output
            //StreamWriter stOutputFile = null;
            //string outfile = OUTPUT_PATH + OUTPUT_FILE;
            //stOutputFile = new StreamWriter(outfile);
            StreamWriter stGainFile = null;
            string gainfile = OUTPUT_PATH + GAIN_FILE;
            stGainFile = new StreamWriter(gainfile);

            // for each Px channel
            for (chIdx = 0; chIdx < NUM_CHANNELS; chIdx++)
            {
                //System.Console.WriteLine("Calculating optimum gain for Channel {0}", chIdx + 1);
                //if (chIdx != 58)
                //    continue;
                // for each gain value in gain array from 1 to 6 in 0.05 increments
                for (thisGain = MIN_GAIN; thisGain <= MAX_GAIN; thisGain += STEP_GAIN)
                {
                    //System.Console.WriteLine("Ch{0}: Trying gain={1}", chIdx + 1, thisGain);
                    // for each frame
                    //if (thisGain != 8.15M)
                    //    continue;
                    for (frameIdx = 0; frameIdx < NUM_SAMPLES; frameIdx++)
                    {
                        // calculate temperature correction
                        fCorTemp = tempData[frameIdx] - (dTemp[frameIdx] * (float)thisGain);
                        // calculate CTP
                        cvtCreateCtp(fCorTemp, chIdx);
                        // tmpArr <- corrected pressure
                        tmpArr[frameIdx] = cvtSingleRawPktToEu(chIdx, rawData[frameIdx, chIdx]);
                        //System.Console.WriteLine("tempArr[{0}]={1}", frameIdx, tmpArr[frameIdx]);
                        filteredTmpArr[frameIdx] = LowPassFilter(tmpArr[frameIdx]);
                    }

                    // find min and max errors in pressure array for this gain
                    // restrict min/max to area of transient

                    float[] truncArr = truncateArray(filteredTmpArr, 2000, 4000);
                    thisMax = truncArr.Max();
                    idxMax = Array.IndexOf(tmpArr, thisMax);
                    thisMin = truncArr.Min();
                    idxMin = Array.IndexOf(tmpArr, thisMin);
                    thisDelta = thisMax - thisMin;
                    if (thisDelta < curMinDelta)
                    {
                        curMinDelta = thisDelta;
                        curBestGain = thisGain;
                    }
                    //System.Console.WriteLine("thisMax={0} [{1}] thisMin={2} [{3}]",
                    //                            thisMax, idxMax, thisMin, idxMin);
                    //System.Console.WriteLine("thisGain={0} thisDelta={1} curMinDelta={2} curBestGain={3}",
                    //                            thisGain, thisDelta, curMinDelta, curBestGain);
                }
                gainArr[chIdx] = curBestGain;
                System.Console.WriteLine("SET TGAIN {0} {1}", chIdx + 1, gainArr[chIdx]);
                string sOut = Convert.ToString(gainArr[chIdx]);
                stGainFile.WriteLine("SET TGAIN {0} {1}", chIdx + 1, sOut);
#if (false)
                for (frameIdx = 0; frameIdx < NUM_SAMPLES; frameIdx++)
                {
                    stOutputFile.WriteLine(filteredTmpArr[frameIdx]);
                    //stOutputFile.WriteLine(tmpArr[frameIdx]);
                }
                stOutputFile.Close();
#endif


                // reset delta, gain
                curMinDelta = BIG_FLOAT;
                curBestGain = MIN_GAIN;
            }
            stGainFile.Close();
        }

        private static float LowPassFilter(float tempin)
        {
            float acc = 0;  // accumulator
            int k;          // coeff idx

            // circular buffer index
            uint n = idx % INPUT_BUFFER_LEN;

            // write new sample to the index of the oldest sample
            sampleBuffer[n] = tempin;

            // apply filter to the input sample:
            // y[n] = sum_{k=0}..{N-1}(h(k) * x(n-k))
            for (k = 0; k < FILTER_LEN; k++)
            {
                acc += (float)coeffs[k] * sampleBuffer[(n + INPUT_BUFFER_LEN - k) % INPUT_BUFFER_LEN];
            }

            // move the index for next function call and return sum
            idx++;
            return acc;
        }

        private static float[] truncateArray(float[] tmpArr, int lowerBnd, int upperBnd)
        {
            float[] retArr = new float[upperBnd - lowerBnd];
            int i,j;
            for (i = lowerBnd, j = 0; i < upperBnd; i++, j++)
            {
                retArr[j] = tmpArr[i];
            }
            return retArr;
        }

        // Processes the calibration file 
        private static void ProcessCal(int lineno, string[] token)
        {
            int nChan;
            int ndx;

            if (lineno <= 8)
            {
                for (int i=0; i<64; i++) {
                    nMLen[i] = 0;
                }
                return;    // Ignore the first 8 lines
            }

            // Extract cal data
            nChan = Convert.ToInt16(token[3]);  // Get channel from cal table
            ndx = nMLen[nChan-1];
            fMTemp[nChan-1, ndx] = Convert.ToSingle(token[2]);       // Get temp from cal table
            fMPress[nChan-1, ndx] = Convert.ToSingle(token[4]);     // Get pressure from cal table
            lMCounts[nChan-1, ndx] = Convert.ToInt32(token[5]);     // Get counts from cal table

#if (false)
            Console.WriteLine("SET PT " +
                fMTemp[nChan - 1, ndx].ToString() + " " +
                nChan.ToString() + " " +
                fMPress[nChan - 1, ndx].ToString() + " " +
                lMCounts[nChan - 1, ndx].ToString() + " " +
                nMLen[nChan - 1].ToString());
#endif

            nMLen[nChan - 1]++;
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

#if (true)
        private static void cvtCreateCtp(float flTemp, int nChan)
        {
            int p, n;
            int bFound;
            int bOneTp;
            float fMDeltaTemp;
            float fRatioP;
            float fRatioC;
            float fCurPres;
            long lCurCnts;
            float x0, x1, xval;
            float y0, y1;   // Interp values
            int t;

            bFound = 0;
            bOneTp = 0;

            //            pCtdsStaging->fTemp[nChan] = flTemp;
            if (nNumCalT == 1)
            {
                // One temperature plane
                t = 0;
                for (p = 0; p < nNumCalP; p++)
                {
                    fCPress[nChan,p] = fMPress[nChan,p];      // Pressures
                    lCCounts[nChan,p] = lMCounts[nChan,p];    // Counts
                    fCCounts[nChan,p] = (float)lCCounts[nChan,p];     // Counts table in float form
                }

                nCNumPress[nChan] = nNumCalP;
            }
            else
            {
                // Multiple temperature planes

                // Work one temperature grouping at a time
                for (t = 0, n = 0; t < nNumCalT; n += nNumCalP, t++)
                {
                    // If current temp is below the first, use the first
                    // If the table is correct all pressure will have a current temperature below the first in table

                    if (flTemp <= fMTemp[nChan,0])
                    {
                        // Current temperature is lower than the lowest temp plane
                        // Fill the current temp plane directly
                        for (p = 0; p < nNumCalP; p++, n++)
                        {
                            fCPress[nChan,p] = fMPress[nChan,n];      // Pressures
                            lCCounts[nChan,p] = lMCounts[nChan,n];    // Counts
                            fCCounts[nChan,p] = (float)lCCounts[nChan,p];     // Counts table in float form
                        }
                        bOneTp = 1;
                        bFound = 1;
                        nCNumPress[nChan] = nNumCalP;

                        break;
                    }
                    else
                    {
                        // Find first temp point just above current temp
                        if (fMTemp[nChan,n] >= flTemp)
                        {
                            // Copy over the high point, that is this point
                            for (p = 0; p < nNumCalP; p++, n++)
                            {
                                fHCTemp[p] = fMTemp[nChan,n];
                                fHCPress[p] = fMPress[nChan,n];
                                lHCCounts[p] = lMCounts[nChan,n];

                                // Copy over the low plane, that is this point - nNumCalP
                                fLCTemp[p] = fMTemp[nChan,(n - nNumCalP)];
                                fLCPress[p] = fMPress[nChan,(n - nNumCalP)];
                                lLCCounts[p] = lMCounts[nChan,(n - nNumCalP)];
                            }

                            bFound = 1;
                            nCNumPress[nChan] = nNumCalP;
                            break;
                        } // End temp in table just above current temp
                    } // End current temp not below first entry
                } // End loop each pressure

                if (bFound == 0)
                {
                    // If not found current temperature is higher than the higest temp
                    // Fill the current temp plane directly
                    t--;    // T is high by 1 if not found

                    n = n-nNumCalP;   // Table index is high by number of pressure if not found
                    for (p = 0; p < nNumCalP; p++, n++)
                    {
                        fCPress[nChan,p] = fMPress[nChan,n];
                        lCCounts[nChan,p] = lMCounts[nChan,n];
                        fCCounts[nChan,p] = (float)lCCounts[nChan,p];     // Counts table in float form
                    }

                    bOneTp = 1;
                    nCNumPress[nChan] = nNumCalP;
                } // End not found, use last

                // Interpolate current temperature plane when more than one temp plane
                if (bOneTp == 0)
                {
                    // Using linear interpolation
                    for (p = 0; p < nNumCalP; p++)
                    {
                        // Calculate delta temp x1 - x0, used by press and counts
                        x0 = fLCTemp[p];
                        x1 = fHCTemp[p];
                        fMDeltaTemp = x1 - x0;
                        xval = flTemp;

                        // Calculate current pressure
                        // Calculate delta press (y) / delta temp (x)
                        y0 = fLCPress[p];
                        y1 = fHCPress[p];

                        fRatioP = (y1 - y0) / fMDeltaTemp;
                        fCurPres = y0 + ((xval - x0) * fRatioP);

                        // Calculate current counts
                        // Calculate delta counts (y) / delta temp (x)
                        y0 = (float)lLCCounts[p];
                        y1 = (float)lHCCounts[p];

                        fRatioC = (y1 - y0) / fMDeltaTemp;
                        lCurCnts = (long)(y0 + ((xval - x0) * fRatioC));

                        // Put into current temp plane
                        fCPress[nChan,p] = fCurPres;   // Pressure table
                        lCCounts[nChan,p] = lCurCnts;  // Counts table
                        fCCounts[nChan,p] = (float)lCurCnts;   // Counts table in float form
                    } // End loop through all num points
                } // End not one temp plane
            } // End multiple temp points

            // Calculate M and B terms for each pressure segment. Works for one or many temp planes
            for (p = 0; p < nNumCalP - 1; p++)
            {
                // Calculate M and B terms for pressure conversion
                fMctp[nChan,p] = (fCPress[nChan,p] - fCPress[nChan,p + 1]) / (fCCounts[nChan,p] - fCCounts[nChan,p + 1]);
                fBctp[nChan,p] = fCPress[nChan,p + 1] - (fMctp[nChan,p] * fCCounts[nChan,p + 1]);
            }

            // Fill in the top entry, use the one just before
            if (nNumCalP > 1)
            {
                fMctp[nChan,nNumCalP - 1] = fMctp[nChan,nNumCalP - 2];
                fBctp[nChan,nNumCalP - 1] = fBctp[nChan,nNumCalP - 2];
            }

            // Calculate the slope and offset for the index conversion
            fMNdxCtp[nChan] = (0.0F - (nNumCalP - 1)) / (fCCounts[nChan,0] - fCCounts[nChan,nNumCalP - 1]);
            fBNdxCtp[nChan] = (nNumCalP - 1) - (fMNdxCtp[nChan] * fCCounts[nChan,nNumCalP - 1]);
        }

        private static float cvtSingleRawPktToEu(int c, long lCounts)
        {
            int n;
            float fPress;

            // Find table index
            n = (int)(fMNdxCtp[c] * lCounts + fBNdxCtp[c]);
            if (n < 0)
                n = 0;
            else if (n >= nCNumPress[c])
                n = (nCNumPress[c] - 1);

            // Calculate pressure from counts
            fPress = (fMctp[c,n] * lCounts) + fBctp[c,n];

            //	sprintf(szOneLine,"cvtSingleRawPktToEu %d %d %ld %f %f %f %f %f",c,n,lCounts,
            //			pCtdsActive->fMctp[c][n],
            //			pCtdsActive->fBctp[c][n],
            //			pCtdsActive->fMNdxCtp[c],
            //			pCtdsActive->fBNdxCtp[c],
            //			fPress);
            //	strcpy(szOneLineCr,szOneLine);
            //	strcat(szOneLineCr,"\r\n");

            return (fPress);
        }
#endif
    }
}
