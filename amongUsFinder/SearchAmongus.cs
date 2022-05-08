using System;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;

namespace amongUsFinder
{
    internal class SearchAmongus
    {
        public DateTime startTime;
        TimeSpan processTime;
        Thread[] threads;
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
        long overallTime = 0;
        long timeFromLastAction = 0;

        public bool initializeInputParameters()
        {
            Console.WriteLine($@"{DateTime.Now:HH:mm:ss.fff} | Input location ({Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\YOUR INPUT):");
            loadLocation = $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\{Console.ReadLine()}";
            if (loadLocation.Contains("."))
            {
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
                int folderIndex = loadLocation.LastIndexOf(@"\");
                saveLocation = loadLocation.Substring(0, folderIndex);
                string fileName = loadLocation.Substring(folderIndex + 1);
                swLogFile = new StreamWriter(saveLocation + $@"\log_{fileName.Split('.')[0]}.txt");
            }
            else
            {
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
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Do you want to REPLACE the existing folder? (y/n)");
                    string input = Console.ReadLine();
                    if (input == "y")
                    {
                        Directory.Delete(saveLocation, true);
                        Directory.CreateDirectory(saveLocation);
                    }
                    else if (input == "n") return false;
                }
                indexStart = getInput("Enter start point", 1);
                indexStop = getInput("Enter stop point", Directory.GetFiles(loadLocation, "*.*", SearchOption.TopDirectoryOnly).Length);
                indexStep = getInput("Enter step length", 1);
                imagesToProcess = roundUp(((double)indexStop - indexStart + 1) / indexStep);
                List<int> filesMissing = new List<int>();
                for (int i = indexStart; i <= indexStart + indexStop; i += indexStep)
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
                            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | IMage {file:00000}.png is missing");
                        }
                    }
                    if (Directory.GetFiles(saveLocation, "*.*", SearchOption.TopDirectoryOnly).Length == 0)
                    {
                        Directory.Delete(saveLocation, true);
                    }
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Task terminated ---------------------------------------------------------------------------\n");
                    return false;
                }
                swLogFile = new StreamWriter(saveLocation + @"\log.txt");
            }
            amongusCount = new int[imagesToProcess];
            picturesProcessed = new int[mainThreadCount];
            return true;
        }

        public void setStartTime()
        {
            startTime = DateTime.Now;
        }

        public void initializeMainThreads()
        {
            threads = new Thread[mainThreadCount];
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
                threads[ct] = new Thread(() => searchAmongus(value));
                ct++;
            }
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Start();
            }
        }

        unsafe public void searchAmongus(int shift = 0)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            int amongUsFound;
            int loopId = shift;
            Thread[] threadsQ = new Thread[3];
            Bitmap bmp;
            Bitmap place;
            picturesProcessed[shift] = 0;

            try
            {
                if (loadLocation.Contains(".")) place = new Bitmap(loadLocation);
                else place = new Bitmap($@"{loadLocation}\{indexStart + shift * indexStep:00000}.png");
            }
            catch (Exception)
            {
                Console.WriteLine($@"{DateTime.Now:HH:mm:ss.fff} | Error: Input file missing ({loadLocation}{indexStart + shift * indexStep:00000}.png)");
                error = true;
                Thread.CurrentThread.Abort();
                return;
            }
            bmp = new Bitmap(place.Width, place.Height, place.PixelFormat);
            int bytesPerPixel = Image.GetPixelFormatSize(place.PixelFormat) / 8;
            BitmapData placeData = place.LockBits(new Rectangle(0, 0, place.Width, place.Height), ImageLockMode.ReadOnly, place.PixelFormat);
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte* ptrFirstPixel = (byte*)placeData.Scan0;
            byte* ptrFirstPixelB = (byte*)bmpData.Scan0;

            double threadW = 0.5 * place.Width;
            double threadH = 0.5 * place.Height;
            int[,] splitParameters = new int[4, 4] { {0, 0, roundUp(threadW), roundUp(threadH)},
                                                     {roundUp(threadW) - 3, 0, (int)threadW + 3, (int)threadH},
                                                     {0, roundUp(threadH) - 3, (int)threadW, (int)threadH + 3},
                                                     {roundUp(threadW) - 3, roundUp(threadH) - 3, (int)threadW + 3, (int)threadH + 3} };
            
            //Loop through pictures
            for (int i = indexStart + shift * indexStep; i <= indexStop; i += mainThreadCount * indexStep)
            {
                startBenchmark(shift, 0, 0);
                amongUsFound = 0;
                //Start quad threads
                threadsQ[0] = new Thread(() => searchQuarter(0, 0, splitParameters[0, 0], splitParameters[0, 1], splitParameters[0, 2], splitParameters[0, 3]));
                threadsQ[1] = new Thread(() => searchQuarter(3, 0, splitParameters[1, 0], splitParameters[1, 1], splitParameters[1, 2], splitParameters[1, 3]));
                threadsQ[2] = new Thread(() => searchQuarter(0, 3, splitParameters[2, 0], splitParameters[2, 1], splitParameters[2, 2], splitParameters[2, 3]));
                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Priority = ThreadPriority.Highest;
                    threadsQ[t].Start();
                }
                searchQuarter(3, 3, splitParameters[3, 0], splitParameters[3, 1], splitParameters[3, 2], splitParameters[3, 3]);

                //Wait for threads to finish
                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Join();
                }
                place.UnlockBits(placeData);
                bmp.UnlockBits(bmpData);
                updateBenchmark(shift, 0, 0, 10, "UnlockB", false);
                if (loadLocation.Contains(".")) bmp.Save($"{loadLocation.Split('.')[0]}_searched.{loadLocation.Split('.')[1]}", ImageFormat.Png);
                else bmp.Save($@"{saveLocation}\{i:00000}.png", ImageFormat.Png);
                updateBenchmark(shift, 0, 0, 10, "SaveP", false);
                place.Dispose();
                bmp.Dispose();
                amongusCount[loopId] = amongUsFound;
                loopId += mainThreadCount;
                picturesProcessed[shift]++;
                if (!loadLocation.Contains(".") && i <= indexStop - mainThreadCount * indexStep)
                {
                    updateBenchmark(shift, 0, 0, 10, "DiB", false);
                    try
                    {
                        place = new Bitmap($@"{loadLocation}\{i + mainThreadCount * indexStep:00000}.png");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine($@"{ DateTime.Now:HH: mm: ss.fff} | Error: Input file missing ({loadLocation}{i + mainThreadCount * indexStep:00000}.png)");
                        error = true;
                        Thread.CurrentThread.Abort();
                    }
                    bmp = new Bitmap(place.Width, place.Height, place.PixelFormat);
                    updateBenchmark(shift, 0, 0, 10, "LoadP", true);
                    placeData = place.LockBits(new Rectangle(0, 0, place.Width, place.Height), ImageLockMode.ReadOnly, place.PixelFormat);
                    bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
                    ptrFirstPixel = (byte*)placeData.Scan0;
                    ptrFirstPixelB = (byte*)bmpData.Scan0;
                }
                stopBenchmark(shift, 0, 0, 10);
            }

            unsafe void searchQuarter(int mx, int my, int xStart, int yStart, int xStop, int yStop)
            {
                byte* c1 = stackalloc byte[3];
                byte* c2 = stackalloc byte[3];

                //Darken backgound/output bitmap data
                for (int y = yStart + my; y < yStart + yStop; y++)
                {
                    byte* currentLine = ptrFirstPixel + y * placeData.Stride;
                    byte* currentLineB = ptrFirstPixelB + y * bmpData.Stride;
                    for (int x = (xStart + mx) * bytesPerPixel; x < (xStart + xStop) * bytesPerPixel; x += bytesPerPixel)
                    {
                        //Calculate new pixel value (R,G,B)
                        currentLineB[x + 3] = currentLine[x + 3];
                        currentLineB[x + 2] = (byte)(currentLine[x + 2] * 0.25);
                        currentLineB[x + 1] = (byte)(currentLine[x + 1] * 0.25);
                        currentLineB[x] = (byte)(currentLine[x] * 0.25);
                    }
                }
                updateBenchmark(shift, xStart, yStart, 10, "DarkenB", false);

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
                                if (x < placeData.Width - 5 && !mirror || x > 0 && mirror)
                                {
                                    for (int row = 0; row < 5; row++)
                                    {
                                        if (compareColor(c1, getPixelColor(tXco(x, 2.5, mirror), y + row)))
                                        {
                                            border++;
                                        }
                                    }
                                }
                                if ((x > 0 && !mirror) || x < placeData.Width - 5 && mirror)
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
                                            byte* currentLine = ptrFirstPixelB + (y + row) * bmpData.Stride;
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
                updateBenchmark(shift, xStart, yStart, 10, "SearchP", false);

                byte* getPixelColor(int x, int y)
                {
                    byte* currentLine = ptrFirstPixel + y * placeData.Stride + x * bytesPerPixel;
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
            }
            
            int tXco(int xC, double move, bool mirror)
            {
                int f = 1;
                if (mirror) f = -1;
                return (int)(xC + 1.5 + move * f);
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

        public void waitMainThreads()
        {
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }
        }

        public bool checkSucessfulCompletion()
        {
            if (error)
            {
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Abort();
                }
                swLogFile.Close();
                Directory.Delete(saveLocation, true);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | -------------------------------------------------------------------------------------------\n");
                return false;
            }
            return true;
        }

        public void renamePictures()
        {
            processTime = DateTime.Now - startTime;
            outputMessage($"Picture processing completed in {processTime.ToString(@"mm\:ss\.fff")} with an average of {roundUpDouble(processTime.TotalSeconds / imagesToProcess, 3)}s per picture!");
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


        void startBenchmark(int sh, int xs, int ys)
        {
            if (sh == 0 && xs == 0 && ys == 0) s.Restart();
        }

        void updateBenchmark(int sh, int xs, int ys, int it, string action, bool last)
        {
            if (sh == 0 && xs == 0 && ys == 0 && benchmark)
            {
                if (iterations % it == 0)
                {
                    Console.Write($"{s.ElapsedMilliseconds - timeFromLastAction:000}-{action}|");
                    if (last) Console.WriteLine();
                    timeFromLastAction = s.ElapsedMilliseconds;
                }
            }
        }

        void stopBenchmark(int sh, int xs, int ys, int it)
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