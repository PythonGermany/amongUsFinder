﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

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
        int imagesToProcess = 0;
        int[] picturesProcessed;
        public int[] amongusCount;
        int[,] amongus = new int[5, 4] { { 0, 2, 2, 2},
                                         { 2, 2, 1, 1},
                                         { 2, 2, 2, 2},
                                         { 3, 2, 3, 2},
                                         { 0, 3, 0, 3} };
        int shapeW, shapeH;

        public bool initializeInputParameters(string[] args)
        {
            loadLocation = args[0];
            if (loadLocation.Contains("."))
            {
                //Input parameters for single picture processing
                if (args.Length != 1)
                    return false;
                if (!File.Exists(loadLocation))
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Error: Input image does not exist");
                    return false;
                }
                indexStart = 1;
                indexStop = 1;
                indexStep = 1;
                imagesToProcess = 1;
                swLogFile = new StreamWriter(getFileName(loadLocation, true) + $@"\log_{getFileName(loadLocation, false).Split('.')[0]}.txt");
            }
            else
            {
                //Input parameters for image sequence processing
                if (args.Length != 5)
                    return false;
                if (!Directory.Exists(loadLocation))
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Error: Input folder does not exist");
                    return false;
                }
                saveLocation = args[1];
                string[] loadFolderFiles = Directory.GetFiles(loadLocation, "*.*", SearchOption.TopDirectoryOnly);
                if (!Directory.Exists(saveLocation))
                    Directory.CreateDirectory(saveLocation);
                else if (loadFolderFiles.Length > 0)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Error: Output folder is not empty ({saveLocation})");
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Do you want to continue? (y/n) WARNING: Existing files will be deleted");
                    if (Console.ReadLine() == "n")
                        return false;
                    Directory.Delete(saveLocation, true);
                    Directory.CreateDirectory(saveLocation);
                }
                indexStart = Convert.ToInt32(args[2]);
                indexStop = Convert.ToInt32(args[3]);
                indexStep = Convert.ToInt32(args[4]);
                imagesToProcess = roundUpInt(((double)indexStop - indexStart + 1) / indexStep);

                //Check for missing files
                List<int> filesMissing = new List<int>();
                for (int i = indexStart; i < indexStop - indexStart + 1; i += indexStep)
                    if (!File.Exists(loadLocation + $@"\{i:00000}.png"))
                        filesMissing.Add(i);
                //Output error if files are missing
                if (filesMissing.Count > 0)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Error: {filesMissing.Count} of {imagesToProcess} images are missing");
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Do you want to know the missing image names? (y/n)");
                    if (Console.ReadLine() == "y")
                        foreach (var file in filesMissing)
                            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Image {file:00000}.png is missing");
                    if (loadFolderFiles.Length == 0)
                        Directory.Delete(saveLocation, true);
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
        public void initializeThreads()
        {
            mainThreads = new Thread[mainThreadCount];
            threadShift = new int[mainThreadCount];
            for (int i = 0; i < threadShift.Length; i++)
                threadShift[i] = i;
        }
        public void startMainThreads()
        {
            int ct = 0;
            foreach (int value in threadShift)
                mainThreads[ct++] = new Thread(() => processImageSequence(value, loadLocation, saveLocation));
            for (int i = 0; i < mainThreads.Length; i++)
                mainThreads[i].Start();
        }

        public void processImageSequence(int shift, string loadPath, string savePath)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            Thread[] threadsQ = new Thread[3];
            Thread saveThread = null;
            picturesProcessed[shift] = 0;
            int loopId = shift;
            Bitmap[] bmp = new Bitmap[2];
            BitmapData[] bmpData = new BitmapData[2];

            byte*[] ptrBmp;
            _ = initializeDataBitmaps($@"{loadLocation}\{indexStart + shift * indexStep:00000}.png", bmp, bmpData, null);
            int[,] splitParameter = generateSplitParameter(bmp[0]);
            int bytesPerPixel = Image.GetPixelFormatSize(bmp[0].PixelFormat) / 8;

            for (int i = indexStart + shift * indexStep; i <= indexStop; i += mainThreadCount * indexStep)
            {
                ptrBmp = initializeDataBitmaps($@"{loadPath}\{i:00000}.png", bmp, bmpData, saveThread);
                amongusCount[loopId] = startQuadThreads(ptrBmp, bmpData, threadsQ, splitParameter, bytesPerPixel);
                string fileName = $@"{savePath}\{i:00000}.png";
                saveThread = new Thread(() => saveFile(fileName, bmp[1], bmpData[1]));
                bmp[0].UnlockBits(bmpData[0]);
                bmp[0].Dispose();
                saveThread.Start();
                loopId += mainThreadCount;
                picturesProcessed[shift]++;
            }
        }
        public void processImage(string loadFile, string savePath)
        {
            Thread[] threadsQ = new Thread[3];
            Thread saveThread;
            Bitmap[] bmp = new Bitmap[2];
            BitmapData[] bmpData = new BitmapData[2];
            byte*[] ptrBmp = initializeDataBitmaps(loadFile, bmp, bmpData, null);
            int[,] splitParameter = generateSplitParameter(bmp[0]);
            int bytesPerPixel = Image.GetPixelFormatSize(bmp[0].PixelFormat) / 8;
            amongusCount[0] = startQuadThreads(ptrBmp, bmpData, threadsQ, splitParameter, bytesPerPixel);
            saveThread = new Thread(() => saveFile(savePath, bmp[1], bmpData[1]));
            bmp[0].UnlockBits(bmpData[0]);
            bmp[0].Dispose();
            saveThread.Start();
        }

        private void saveFile(string savePath, Bitmap bmp, BitmapData bmpData)
        {
            bmp.UnlockBits(bmpData);
            bmp.Save(savePath);
            bmp.Dispose();
        }

        public byte*[] initializeDataBitmaps(string loadPath, Bitmap[] bmp, BitmapData[] bmpData, Thread saveThread)
        {
            Bitmap temp;
            BitmapData tempData;
            bmp[0] = new Bitmap(loadPath);
            bmpData[0] = bmp[0].LockBits(new Rectangle(0, 0, bmp[0].Width, bmp[0].Height), ImageLockMode.ReadOnly, bmp[0].PixelFormat);
            temp = new Bitmap(bmp[0].Width, bmp[0].Height, bmp[0].PixelFormat);
            tempData = temp.LockBits(new Rectangle(0, 0, temp.Width, temp.Height), ImageLockMode.WriteOnly, temp.PixelFormat);
            if (saveThread != null)
                saveThread.Join();
            bmp[1] = temp;
            bmpData[1] = tempData;
            return new byte*[2] { (byte*)bmpData[0].Scan0, (byte*)bmpData[1].Scan0 };
        }
        public int[,] generateSplitParameter(Bitmap bitmap)
        {
            double threadW = 0.5 * bitmap.Width;
            double threadH = 0.5 * bitmap.Height;
            shapeW = amongus.GetUpperBound(1);
            shapeH = amongus.GetUpperBound(0);
            return new int[4, 4] { {0, 0, roundUpInt(threadW), roundUpInt(threadH)},
                                   {roundUpInt(threadW) - shapeW, 0, (int)threadW + shapeW, (int)threadH},
                                   {0, roundUpInt(threadH) - shapeH, (int)threadW, (int)threadH + shapeH},
                                   {(int)threadW - shapeW, (int)threadH - shapeH, roundUpInt(threadW) + shapeW, roundUpInt(threadH) + shapeH} };
        }
        public int startQuadThreads(byte*[] ptr, BitmapData[] bmpD, Thread[] threadsQ, int[,] splitParameters, int bytesPerPixel)
        {
            int amongUsFound = 0;
            int stride = bmpD[0].Stride;
            threadsQ[0] = new Thread(() => searchQuarter(0, 0, splitParameters[0, 0], splitParameters[0, 1], splitParameters[0, 2], splitParameters[0, 3]));
            threadsQ[1] = new Thread(() => searchQuarter(shapeW, 0, splitParameters[1, 0], splitParameters[1, 1], splitParameters[1, 2], splitParameters[1, 3]));
            threadsQ[2] = new Thread(() => searchQuarter(0, shapeH, splitParameters[2, 0], splitParameters[2, 1], splitParameters[2, 2], splitParameters[2, 3]));
            for (int t = 0; t < threadsQ.Length; t++)
            {
                threadsQ[t].Priority = ThreadPriority.Highest;
                threadsQ[t].Start();
            }
            searchQuarter(shapeW, shapeH, splitParameters[3, 0], splitParameters[3, 1], splitParameters[3, 2], splitParameters[3, 3]);
            //Wait for threads to finish
            for (int t = 0; t < threadsQ.Length; t++)
                threadsQ[t].Join();
            return amongUsFound;

            void searchQuarter(int mx, int my, int xStart, int yStart, int xStop, int yStop)
            {
                byte* c1;
                byte* c2;
                int border = 0;
                bool match;

                //Darken output bitmap
                for (int y = yStart + my; y < yStart + yStop; y++)
                {
                    byte* currentLine = ptr[0] + y * stride;
                    byte* currentLineB = ptr[1] + y * stride;
                    for (int x = (xStart + mx) * bytesPerPixel; x < (xStart + xStop) * bytesPerPixel; x += bytesPerPixel)
                    {
                        //Calculate new pixel value (R,G,B)
                        currentLineB[x + 3] = 255;
                        currentLineB[x + 2] = (byte)(currentLine[x + 2] * 0.25);
                        currentLineB[x + 1] = (byte)(currentLine[x + 1] * 0.25);
                        currentLineB[x] = (byte)(currentLine[x] * 0.25);
                    }
                }

                //Loop through bitmap/search amongus
                for (int y = yStart; y < yStart + yStop - shapeH; y++)
                {
                    for (int x = xStart * bytesPerPixel + 6; x < (xStart + xStop - shapeW) * bytesPerPixel + 6; x += bytesPerPixel)
                    {
                        for (int m = -1; m <= 1; m += 2)
                        {
                            border = 0;
                            c1 = getPixelColor(x + 2 * m, y);
                            c2 = getPixelColor(x + 2 * m, y + 1);

                            //Check amongus shape
                            if (!compareColor(c2, getPixelColor(x + 6 * m, y + 1)) || compareColor(c1, c2) || compareColor(c1, getPixelColor(x - 6 * m, y))) continue;
                            searchShape();
                            void searchShape()
                            {
                                for (int row = 0; row < shapeH + 1; row++)
                                {
                                    for (int column = 0; column < shapeW + 1; column++)
                                    {
                                        match = compareColor(c1, getPixelColor(x - (6 - column * bytesPerPixel) * m, y + row));
                                        if (amongus[row, column] == 2 && !match)
                                            return;
                                        border += (amongus[row, column] == 0 && match) ? 1 : 0;
                                    }
                                }
                                //Check border around amongus
                                if (x < bmpD[0].Stride - 5 * bytesPerPixel && m == 1 || x > 0 && m == -1)
                                    for (int row = 0; row < 5 && border < 5; row++)
                                        border += (compareColor(c1, getPixelColor(x + 10 * m, y + row))) ? 1 : 0;
                                if ((x > 0 && m == -1 || x < bmpD[0].Stride - 5 * bytesPerPixel && m == 1) && border < 5)
                                    for (int row = 1; row < 3 && border < 5; row++)
                                        border += (compareColor(c1, getPixelColor(x - 10 * m, y + row))) ? 1 : 0;
                                for (int column = 1; column < 4 && y > 0 && border < 5; column++)
                                    border += (compareColor(c1, getPixelColor(x - (6 - column * bytesPerPixel) * m, y - 1))) ? 1 : 0;
                                //If amongus found --> highlight amongus on output bitmap
                                if (border < 5)
                                {
                                    for (int row = 0; row < 5; row++)
                                    {
                                        byte* currentLine = ptr[1] + (y + row) * stride;
                                        for (int column = 0; column < 4; column++)
                                        {
                                            int xLox = (x - (6 - column * bytesPerPixel) * m) / bytesPerPixel;
                                            if (amongus[row, column] == 2)
                                                ((int *)currentLine)[xLox] = *((int *)c1);
                                            else if (amongus[row, column] > 2 && compareColor(c1, getPixelColor(x - (6 - column * bytesPerPixel) * m, y + row)))
                                                ((int*)currentLine)[xLox] = *((int*)c1);
                                            else if (amongus[row, column] == 1)
                                                ((int*)currentLine)[xLox] = *((int*)c2);
                                        }
                                    }
                                    amongUsFound++;
                                    x += 3 * bytesPerPixel;
                                    return;
                                }
                            }
                        }
                    }
                }
                byte* getPixelColor(int x, int y)
                {
                    return ptr[0] + y * stride + x;
                }
                bool compareColor(byte* color1, byte* color2)
                {
                    return (((int*)color1)[0] == ((int*)color2)[0]);
                }
            }
        }

        public void outputProgress()
        {
            int prev = 0;
            int currentPicturesProcessed = 0;
            double progressState = 0;
            Console.CursorTop += 2;
            while (progressState < 100)
            {
                progressState = currentPicturesProcessed / ((indexStop - indexStart + 1.0) / indexStep) * 100;
                if (prev != (int)progressState)
                {
                    Console.SetCursorPosition(0, Console.CursorTop - 2);
                    outputMessage($"({currentPicturesProcessed}|{imagesToProcess}) Progress: {(int)progressState}% done", true);
                    Console.Write('[');
                    for (int i = 1; i < Console.BufferWidth - 1; i++)
                    {
                        if (i < progressState / 100 * Console.BufferWidth)
                            Console.Write('=');
                        else
                            Console.Write(' ');
                    }
                    Console.Write("]");
                    prev = (int)progressState;
                }
                if (progressState >= 100) break;
                currentPicturesProcessed = 0;
                foreach (var item in picturesProcessed)
                    currentPicturesProcessed += item;
            }
        }
        public void waitMainThreads()
        {
            for (int i = 0; i < mainThreads.Length; i++)
                mainThreads[i].Join();
            processTime = DateTime.Now - startTime;
            outputMessage($"Picture processing completed in {processTime.ToString(@"mm\:ss\.fff")} with an average of {roundUpDouble(processTime.TotalSeconds / imagesToProcess, 4)}s per picture!", true);
        }
        public void renamePictures()
        {
            if (indexStep > 1)
            {
                int iNewName = 0;
                for (int i = indexStart; i <= indexStop; i += indexStep)
                    File.Move($@"{saveLocation}\{i:00000}.png", $@"{saveLocation}\{++iNewName:00000}.png");
                outputMessage($"Files renamed (from 00001 to {iNewName:00000})", true);
            }
        }
        public void generateTextFile()
        {
            StreamWriter swNum = new StreamWriter(saveLocation + @"\amongUsCount.txt");
            for (int i = 0; i < amongusCount.Length; i++)
                swNum.WriteLine(amongusCount[i]);
            swNum.Close();
            swNum.Dispose();
            outputMessage($@"Txt file generated at {saveLocation}\amongUsCount.txt", true);
        }
        public void generateStatisticImage(string dataInput = null)
        {
            string dataOutput = saveLocation;
            Bitmap output = new Bitmap(1000, 250);
            Graphics g = Graphics.FromImage(output);
            if (string.IsNullOrEmpty(dataInput))
                dataInput = saveLocation + @"\amongUsCount.txt";
            else
                dataOutput = dataInput.Substring(0, dataInput.LastIndexOf(@"\"));
            string[] values = File.ReadAllLines(dataInput);
            g.DrawLine(Pens.Black, 0, 0, output.Width - 1, 0);
            g.DrawLine(Pens.Black, 0, 0, 0, output.Height - 1);
            g.DrawLine(Pens.Black, 0, output.Height - 1, output.Width - 1, output.Height - 1);
            g.DrawLine(Pens.Black, output.Width - 1, 0, output.Width - 1, output.Height - 1);
            int maxValue = 0;
            for (int i = 0; i < values.Length; i++)
            {
                int value = Convert.ToInt32(values[i]);
                if (value > maxValue)
                    maxValue = value;
            }
            for (int i = 1; i < output.Width - 2; i++)
            {
                double index = (double)i / (output.Width - 2) * values.Length - 1;
                double nextIndex = (double)(i + 1) / (output.Width - 2) * values.Length - 1;
                double value = Convert.ToInt32(values[(int)index]) * (1 - (index - (int)index)) + Convert.ToInt32(values[roundUpInt(index)]) * (index - (int)index);
                double nextValue = Convert.ToInt32(values[(int)nextIndex]) * (1 - (nextIndex - (int)nextIndex)) + Convert.ToInt32(values[roundUpInt(nextIndex)]) * (nextIndex - (int)nextIndex);
                g.DrawLine(Pens.Red, i, output.Height - 2 - (int)Math.Round((double)value / maxValue * (output.Height - 3)), i + 1, output.Height - 2 - (int)Math.Round((double)nextValue / maxValue * (output.Height - 3)));
            }
            output.Save(dataOutput + @"\statistic.png");
            outputMessage($@"Statistic image generated at {dataOutput}\statistic.png", true);
        }

        string getFileName(string path, bool returnPath)
        {
            int folderIndex = path.LastIndexOf(@"\");
            if (folderIndex == -1)
                folderIndex = path.LastIndexOf(@"/");
            if (folderIndex == -1 && returnPath)
                return (string.Empty);
            else if (folderIndex == -1 && !returnPath)
                return (path);
            if (returnPath)
                return path.Substring(0, folderIndex);
            else
                return path.Substring(folderIndex + 1);
        }
        public void outputMessage(string output, bool log)
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | " + output);
            if (log)
                swLogFile.WriteLine($"{DateTime.Now:dd.MM.yyyy}; { DateTime.Now:HH:mm:ss.fff} | " + output);
        }
        public int roundUpInt(double value)
        {
            int result = (int)value;
            if (result < value)
                result++;
            return result;
        }
        double roundUpDouble(double value, int decimalpoint)
        {
            var result = Math.Round(value, decimalpoint);
            if (result < value)
                result += Math.Pow(10, -decimalpoint);
            return result;
        }
    }
}
