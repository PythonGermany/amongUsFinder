using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;

namespace amongUsFinder
{
    unsafe internal class SearchAmongus
    {
        public DateTime startTime;
        TimeSpan processTime;
        Thread[] mainThreads;
        int[] threadShift;
        int mainThreadCount = Environment.ProcessorCount / 4;
        public string loadLocation;
        string saveLocation;
        public StreamWriter swLogFile;
        int indexStart;
        int indexStop;
        int indexStep;
        bool error = false;
        int imagesToProcess = 0;
        int[] picturesProcessed;
        public int[] amongusCount;
        int[,] amongus = new int[5, 4] { { 0, 2, 2, 2},
                                         { 2, 2, 1, 1},
                                         { 2, 2, 2, 2},
                                         { 3, 2, 3, 2},
                                         { 0, 3, 0, 3} };

        //Benchmark stuff
        bool benchmark = true;
        Stopwatch s = new Stopwatch();
        int iterations = 0;
        int locIteration = 0;
        long overallTime = 0;
        long timeFromLastAction = 0;

        public bool initializeInputParameters()
        {
            Console.WriteLine($@"{DateTime.Now:HH:mm:ss.fff} | Input location ({Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\YOUR INPUT):");
            loadLocation = $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\{Console.ReadLine()}";
            if (loadLocation.Contains("."))
            {
                //Input parameters for single picture processing
                if (!File.Exists(loadLocation))
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Error: Input image does not exist");
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Task terminated ---------------------------------------------------------------------------\n");
                    return false;
                }
                indexStart = 1;
                indexStop = 1;
                indexStep = 1;
                imagesToProcess = 1;
                saveLocation = getFileName(loadLocation, true);
                swLogFile = new StreamWriter(saveLocation + $@"\log_{getFileName(loadLocation, false).Split('.')[0]}.txt");
            }
            else
            {
                //Input parameters for image sequence processing
                if (!Directory.Exists(loadLocation))
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Error: Input folder does not exist");
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Task terminated ---------------------------------------------------------------------------\n");
                    return false;
                }
                Console.WriteLine($@"{DateTime.Now:HH:mm:ss.fff} | Output location ({Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\YOUR INPUT):");
                saveLocation = $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\{Console.ReadLine()}";
                if (!Directory.Exists(saveLocation)) Directory.CreateDirectory(saveLocation);
                else if (Directory.GetFiles(saveLocation, "*.*", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Error: Output folder is not empty ({saveLocation})");
                    if (saveLocation != "")
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Do you want to REPLACE the existing folder? (y/n)");
                        string input = Console.ReadLine();
                        if (input == "y")
                        {
                            Directory.Delete(saveLocation, true);
                            Directory.CreateDirectory(saveLocation);
                        }
                        else
                        {
                            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Task terminated ---------------------------------------------------------------------------\n");
                            return false;
                        } 
                    }
                    else return false;
                }
                string[] loadFolderFiles = Directory.GetFiles(loadLocation, "*.*", SearchOption.TopDirectoryOnly);
                indexStart = getInput("Enter start point", Convert.ToInt32(getFileName(loadFolderFiles[0], false).Split('.')[0]));
                indexStop = getInput("Enter stop point", Convert.ToInt32(getFileName(loadFolderFiles[loadFolderFiles.Length - 1], false).Split('.')[0]));
                indexStep = getInput("Enter step length", 1);
                imagesToProcess = roundUp(((double)indexStop - indexStart + 1) / indexStep);

                //Check for missing files
                List<int> filesMissing = new List<int>();
                for (int i = indexStart; i < indexStop - indexStart + 1; i += indexStep)
                {
                    if (!File.Exists(loadLocation + $@"\{i:00000}.png"))
                    {
                        filesMissing.Add(i);
                    }
                }
                if (filesMissing.Count > 0)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Error: {filesMissing.Count} of {imagesToProcess} images are missing");
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Do you want to know the missing image names? (y/n)");
                    string input = Console.ReadLine();
                    if (input == "y")
                    {
                        foreach (var file in filesMissing)
                        {
                            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Image {file:00000}.png is missing");
                        }
                    }
                    if (loadFolderFiles.Length == 0)
                    {
                        Directory.Delete(saveLocation, true);
                    }
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Task terminated ---------------------------------------------------------------------------\n");
                    return false;
                }
                swLogFile = new StreamWriter(saveLocation + @"\log.txt");
            }
            //images = new Bitmap[imagesToProcess];
            //imagesData = new BitmapData[imagesToProcess];
            amongusCount = new int[imagesToProcess];
            picturesProcessed = new int[mainThreadCount];
            return true;
        }

        public void setStartTime()
        {
            startTime = DateTime.Now;
        }

        public void initializeThreads()
        {
            //dataThreads = new Thread[mainThreadCount];
            mainThreads = new Thread[mainThreadCount];
            threadShift = new int[mainThreadCount];
            for (int i = 0; i < threadShift.Length; i++)
            {
                threadShift[i] = i;
            }
        }

        public void startMainThreads()
        {
            int ct = 0;
            foreach (int value in threadShift)
            {
                mainThreads[ct] = new Thread(() => processImage(loadLocation, value));
                ct++;
            }
            for (int i = 0; i < mainThreads.Length; i++)
            {
                mainThreads[i].Start();
            }
        }

        public void processImage(string inputFolder, int shift, string inputFile = null)
        {
            startBenchmark(shift);
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            picturesProcessed[shift] = 0;
            int amongUsFound = 0;
            int loopId = shift;
            Bitmap bmpInput;
            Thread[] threadsQ = new Thread[3];
            string tempPathLoad = $@"{loadLocation}\{indexStart + mainThreadCount * indexStep:00000}.png";
            try
            {
                bmpInput = new Bitmap(tempPathLoad);
            }
            catch (Exception)
            {
                Console.WriteLine($@"{ DateTime.Now:HH: mm: ss.fff} | Error: Input file missing ({tempPathLoad})");
                error = true;
                Thread.CurrentThread.Abort();
                return;
            }
            updateBenchmark(shift, 1, "Load", false);
            Bitmap bmpOutput = new Bitmap(bmpInput.Width, bmpInput.Height, bmpInput.PixelFormat);

            double threadW = 0.5 * bmpInput.Width;
            double threadH = 0.5 * bmpInput.Height;
            int[,] splitParameters = new int[4, 4] { {0, 0, roundUp(threadW), roundUp(threadH)},
                                                     {roundUp(threadW) - 3, 0, (int)threadW + 3, (int)threadH},
                                                     {0, roundUp(threadH) - 3, (int)threadW, (int)threadH + 3},
                                                     {roundUp(threadW) - 3, roundUp(threadH) - 3, (int)threadW + 3, (int)threadH + 3} };

            int bytesPerPixel = Image.GetPixelFormatSize(bmpInput.PixelFormat) / 8;
            BitmapData bmpInputData;
            BitmapData bmpOutputData;
            byte* ptrInput;
            byte* ptrOutput;
            updateBenchmark(shift, 1, "Setup", true);

            for (int i = indexStart + shift * indexStep; i <= indexStop; i += mainThreadCount * indexStep)
            {
                tempPathLoad = $@"{loadLocation}\{i:00000}.png";
                bmpInput = new Bitmap(tempPathLoad);
                bmpOutput = new Bitmap(bmpInput.Width, bmpInput.Height, bmpInput.PixelFormat);
                bmpInputData = bmpInput.LockBits(new Rectangle(0, 0, bmpInput.Width, bmpInput.Height), ImageLockMode.ReadOnly, bmpInput.PixelFormat);
                bmpOutputData = bmpOutput.LockBits(new Rectangle(0, 0, bmpOutput.Width, bmpOutput.Height), ImageLockMode.WriteOnly, bmpOutput.PixelFormat);
                ptrInput = (byte*)bmpInputData.Scan0;
                ptrOutput = (byte*)bmpOutputData.Scan0;
                updateBenchmark(shift, 20, "Load&Lock", false);

                threadsQ[0] = new Thread(() => searchQuarter(0, 0, splitParameters[0, 0], splitParameters[0, 1], splitParameters[0, 2], splitParameters[0, 3]));
                threadsQ[1] = new Thread(() => searchQuarter(3, 0, splitParameters[1, 0], splitParameters[1, 1], splitParameters[1, 2], splitParameters[1, 3]));
                threadsQ[2] = new Thread(() => searchQuarter(0, 3, splitParameters[2, 0], splitParameters[2, 1], splitParameters[2, 2], splitParameters[2, 3]));
                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Priority = ThreadPriority.Highest;
                    threadsQ[t].Start();
                }
                updateBenchmark(shift, 20, "StartThreads", false);
                searchQuarter(3, 3, splitParameters[3, 0], splitParameters[3, 1], splitParameters[3, 2], splitParameters[3, 3]);
                //Wait for threads to finish
                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Join();
                }
                updateBenchmark(shift, 20, "FinishThreads", false);
                bmpInput.UnlockBits(bmpInputData);
                bmpInput.Dispose();
                bmpOutput.UnlockBits(bmpOutputData);
                bmpOutput.Save($@"{saveLocation}\{i:00000}.png", ImageFormat.Png);
                bmpOutput.Dispose();
                amongusCount[loopId] = amongUsFound;
                loopId += mainThreadCount;
                picturesProcessed[shift]++;
                updateBenchmark(shift, 20, "Unlock&Save", true);
            }

            void searchQuarter(int mx, int my, int xStart, int yStart, int xStop, int yStop)
            {
                byte* c1 = stackalloc byte[3];
                byte* c2 = stackalloc byte[3];

                //Darken backgound/output bitmap data
                double* dark = stackalloc double[3];
                for (int y = yStart + my; y < yStart + yStop; y++)
                {
                    byte* currentLine = ptrInput + y * bmpInputData.Stride;
                    byte* currentLineB = ptrOutput + y * bmpOutputData.Stride;
                    for (int x = (xStart + mx) * bytesPerPixel; x < (xStart + xStop) * bytesPerPixel; x += bytesPerPixel)
                    {
                        //Calculate new pixel value (R,G,B)
                        currentLineB[x + 3] = currentLine[x + 3];
                        currentLineB[x + 2] = (byte)(currentLine[x + 2] * dark[2]);
                        currentLineB[x + 1] = (byte)(currentLine[x + 1] * dark[1]);
                        currentLineB[x] = (byte)(currentLine[x] * dark[0]);
                    }
                }

                //Loop through bitmap/search amongus
                for (int y = yStart; y < yStart + yStop - 4; y++)
                {
                    for (int x = xStart; x < xStart + xStop - 3; x++)
                    {
                        bool mirror;
                        for (int m = 0; m < 2; m++)
                        {
                            bool search = true;
                            int border = 0;

                            if (m == 0) mirror = false;
                            else mirror = true;

                            c1 = getPixelColor(tXco(x, -0.5, mirror), y);
                            c2 = getPixelColor(tXco(x, 0.5, mirror), y + 1);

                            //Check amongus shape
                            if (!compareColor(c2, getPixelColor(tXco(x, 1.5, mirror), y + 1)) || compareColor(c1, c2) || compareColor(c1, getPixelColor(tXco(x, -1.5, mirror), y))) continue;
                            for (int row = 0; row < 5; row++)
                            {
                                for (int column = 0; column < 4; column++)
                                {
                                    if (amongus[row, column] == 2 && !compareColor(c1, getPixelColor(tXco(x, -1.5 + column, mirror), y + row)))
                                    {
                                        search = false;
                                        break;
                                    }
                                    else if (amongus[row, column] == 0 && compareColor(c1, getPixelColor(tXco(x, -1.5 + column, mirror), y + row)))
                                    {
                                        border++;
                                    }
                                }
                            }

                            //Check border around amongus
                            if (search)
                            {
                                if (x < bmpInputData.Width - 5 && !mirror || x > 0 && mirror)
                                {
                                    for (int row = 0; row < 5; row++)
                                    {
                                        if (compareColor(c1, getPixelColor(tXco(x, 2.5, mirror), y + row)))
                                        {
                                            border++;
                                        }
                                    }
                                }
                                if ((x > 0 && !mirror) || x < bmpInputData.Width - 5 && mirror)
                                {
                                    for (int row = 1; row < 3; row++)
                                    {
                                        if (compareColor(c1, getPixelColor(tXco(x, -2.5, mirror), y + row)))
                                        {
                                            border++;
                                        }
                                    }
                                }
                                if (y > 0)
                                {
                                    for (int column = 1; column < 4; column++)
                                    {
                                        if (compareColor(c1, getPixelColor(tXco(x, -1.5 + column, mirror), y - 1)))
                                        {
                                            border++;
                                        }
                                    }
                                }
                                if (border < 5)
                                {
                                    //If amongus found --> draw amongus to output bitmap data
                                    search = false;
                                    for (int row = 0; row < 5; row++)
                                    {
                                        for (int column = 0; column < 4; column++)
                                        {
                                            byte* currentLine = ptrOutput + (y + row) * bmpOutputData.Stride;
                                            if (amongus[row, column] >= 2 && compareColor(c1, getPixelColor(tXco(x, -1.5 + column, mirror), y + row)))
                                            {
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel + 2] = c1[2];
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel + 1] = c1[1];
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel] = c1[0];
                                            }
                                            else if (amongus[row, column] == 1)
                                            {
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel + 2] = c2[2];
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel + 1] = c2[1];
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel] = c2[0];
                                            }
                                        }
                                    }
                                    amongUsFound++;
                                    x += 3;
                                    m = 2;
                                }
                            }
                        }
                    }
                }

                byte* getPixelColor(int x, int y)
                {
                    byte* currentLine = ptrInput + y * bmpInputData.Stride + x * bytesPerPixel;
                    return currentLine;
                }
                bool compareColor(byte* color1, byte* color2)
                {
                    bool match = true;
                    for (int i = 0; i < bytesPerPixel; i++)
                    {
                        if (color1[i] != color2[i])
                        {
                            match = false; break;
                        }
                    }
                    return match;
                }
                int tXco(int xC, double move, bool mirror)
                {
                    int f = 1;
                    if (mirror) f = -1;
                    return (int)(xC + 1.5 + move * f);
                }
            }
        }
        public void outputProgress()
        {
            int min = DateTime.Now.Minute;
            int currentPicturesProcessed = 0;
            double progressState = 0;
            while (progressState < 100)
            {
                progressState = currentPicturesProcessed / ((double)(indexStop - indexStart + 1) / indexStep) * 100;
                if (min != DateTime.Now.Minute)
                {
                    outputMessage($"({currentPicturesProcessed}|{imagesToProcess}) Progress: {Math.Round(progressState, 2)}% done");
                    min = DateTime.Now.Minute;
                }
                if (progressState >= 100) break;
                //Stop update loop if one main thread reports an error
                if (error)
                {
                    break;
                }
                Thread.Sleep(995);
                currentPicturesProcessed = 0;
                foreach (var item in picturesProcessed)
                {
                    currentPicturesProcessed += item;
                }
            }
        }

        public bool checkSucessfulCompletion()
        {
            if (error)
            {
                for (int i = 0; i < mainThreads.Length; i++)
                {
                    mainThreads[i].Abort();
                }
                swLogFile.Close();
                Directory.Delete(saveLocation, true);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | -------------------------------------------------------------------------------------------\n");
                return false;
            }
            return true;
        }

        public void waitMainThreads()
        {
            for (int i = 0; i < mainThreads.Length; i++)
            {
                mainThreads[i].Join();
            }
            processTime = DateTime.Now - startTime;
            outputMessage($"Picture processing completed in {processTime.ToString(@"mm\:ss\.fff")} with an average of {roundUpDouble(processTime.TotalSeconds / imagesToProcess, 3)}s per picture!");
        }

        public void renamePictures()
        {
            if (indexStep > 1)
            {
                int iNewName = 0;
                for (int i = indexStart; i <= indexStop; i += indexStep)
                {
                    iNewName++;
                    File.Move($@"{saveLocation}\{i:00000}.png", $@"{saveLocation}\{iNewName:00000}.png");
                }
                outputMessage($"Files renamed (from 00001 to {iNewName:00000})");
            }
        }

        public void generateTextFile()
        {
            using (StreamWriter swNum = new StreamWriter(saveLocation + @"\amongUsCount.txt"))
            {
                for (int i = 0; i < amongusCount.Length; i++)
                {
                    swNum.WriteLine(amongusCount[i]);
                }
            }
            outputMessage($"Txt file generated at {saveLocation + @"\amongUsCount.txt"}");
        }


        int getInput(string text, int defaultVal)
        {
            bool c = false;
            int output = defaultVal;
            while (!c)
            {
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | " + text + $" (default: {defaultVal})");
                string input = Console.ReadLine();
                if (input != "")
                {
                    try
                    {
                        output = Convert.ToInt32(input);
                        c = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Error: " + e.Message.Trim('.'));
                        c = false;
                    }
                }
                else
                {
                    c = true;
                }
            }
            return output;
        }
        string getFileName(string path , bool returnPath)
        {
            int folderIndex = path.LastIndexOf(@"\");
            if (returnPath) return path.Substring(0, folderIndex);
            else return path.Substring(folderIndex + 1);
        }

        public void outputMessage(string output)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | " + output);
            swLogFile.WriteLine($"{DateTime.Now:dd.MM.yyyy}; { DateTime.Now:HH:mm:ss.fff} | " + output);
        }

        public int roundUp(double value)
        {
            int result = (int)value;
            if (result < value) result++;
            return result;
        }

        double roundUpDouble(double value, int decimalpoint)
        {
            var result = Math.Round(value, decimalpoint);
            if (result < value) result += Math.Pow(10, -decimalpoint);
            return result;
        }

        void startBenchmark(int sh, int xs = 0, int ys = 0)
        {
            if (sh == 0 && xs == 0 && ys == 0) s.Restart();
        }

        void updateBenchmark(int sh, int it, string action, bool last, int xs = 0, int ys = 0)
        {
            if (sh == 0 && xs == 0 && ys == 0 && benchmark)
            {
                if (locIteration % it == 0)
                {
                    Console.Write($"{s.ElapsedMilliseconds - timeFromLastAction:000}-{action}|");
                    if (last) Console.WriteLine();
                    timeFromLastAction = s.ElapsedMilliseconds;
                }
            }
        }

        void stopBenchmark(int sh, int it, int xs = 0, int ys = 0)
        {
            if (sh == 0 && xs == 0 && ys == 0 && benchmark)
            {
                s.Stop();
                iterations++;

                overallTime += s.ElapsedMilliseconds;
                if (iterations % it == 0)
                {
                    Console.WriteLine($"Iteration: {iterations} | {s.ElapsedMilliseconds}ms ({overallTime / iterations}ms average)");
                    timeFromLastAction = 0;
                }
            }
        }
    }
}