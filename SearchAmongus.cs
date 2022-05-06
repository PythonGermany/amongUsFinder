using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace amongUsFinder
{
    internal class SearchAmongus
    {
        public int tcNormal = 4;
        public string loadLocation;
        public string saveLocation;
        public int iName;
        public int iNameStop;
        public int iNameStep;
        public int[] picturesProcessed;
        public int[] amongusCount;

        public void searchAmongus(int shift = 0)
        {
            int[,] amongus = new int[5, 4] {
                { 0, 2, 2, 2},
                                                 { 2, 2, 1, 1},
                                                 { 2, 2, 2, 2},
                                                 { 3, 2, 3, 2},
                                                 { 0, 3, 0, 3}
            };
            int amongUsFound;
            int loopId = shift;
            Mutex mutex = new Mutex();
            Thread[] threadsQ = new Thread[3];
            Bitmap bmp;
            Graphics g;
            Bitmap place;
            picturesProcessed[shift] = 0;
            //Stopwatch s = new Stopwatch();
            //Stopwatch sw = new Stopwatch();
            //int iterations = 0;
            //int iterationsW = 0;
            //long overallTime = 0;
            //long overallTimeW = 0;

            if (loadLocation.Contains(".")) place = new Bitmap(loadLocation);
            else place = new Bitmap($@"{loadLocation}\{iName + shift * iNameStep:00000}.png");
            bmp = new Bitmap(place.Width, place.Height);
            g = Graphics.FromImage(bmp);
            double threadW = 0.5 * bmp.Width;
            double threadH = 0.5 * bmp.Height;
            int[,] splitParameters = new int[4, 4] { {0, 0, roundUp(threadW, true), roundUp(threadH, true)},
                                                      {roundUp(threadW, true) - 3, 0, roundUp(threadW, false) + 3, roundUp(threadH, false)},
                                                      {0, roundUp(threadH, true) - 3, roundUp(threadW, false), roundUp(threadH, false) + 3},
                                                      {roundUp(threadW, true) - 3, roundUp(threadH, true) - 3, roundUp(threadW, false) + 3, roundUp(threadH, false) + 3} };

            //Loop through pictures
            for (int i = iName + shift * iNameStep; i <= iNameStop; i += tcNormal * iNameStep)
            {
                //if (shift == 0) sw.Restart();
                amongUsFound = 0;
                //Start quad threads
                //threadsQ[0] = new Thread(() => searchQuarter(0, 0, 1000, 1000));
                //threadsQ[1] = new Thread(() => searchQuarter(997, 0, 1003, 1000));
                //threadsQ[2] = new Thread(() => searchQuarter(0, 997, 1000, 1003));
                //for (int t = 0; t < threadsQ.Length; t++)
                //{
                //    threadsQ[t].Priority = ThreadPriority.Highest;
                //    threadsQ[t].Start();
                //}
                ////if (shift == 0) Console.WriteLine($"{sw.ElapsedMilliseconds}ms | Load image");
                //searchQuarter(997, 997, 1003, 1003);
                threadsQ[0] = new Thread(() => searchQuarter(splitParameters[0, 0], splitParameters[0, 1], splitParameters[0, 2], splitParameters[0, 3]));
                threadsQ[1] = new Thread(() => searchQuarter(splitParameters[1, 0], splitParameters[1, 1], splitParameters[1, 2], splitParameters[1, 3]));
                threadsQ[2] = new Thread(() => searchQuarter(splitParameters[2, 0], splitParameters[2, 1], splitParameters[2, 2], splitParameters[2, 3]));
                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Priority = ThreadPriority.Highest;
                    threadsQ[t].Start();
                }
                //if (shift == 0) Console.WriteLine($"{sw.ElapsedMilliseconds}ms | Load image");
                searchQuarter(splitParameters[3, 0], splitParameters[3, 1], splitParameters[3, 2], splitParameters[3, 3]);

                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Join();
                }
                if (loadLocation.Contains(".")) bmp.Save($"{loadLocation.Split('.')[0]}_searched.{loadLocation.Split('.')[1]}", ImageFormat.Png);
                else bmp.Save($@"{saveLocation}\{i:00000}.png", ImageFormat.Png);
                amongusCount[loopId] = amongUsFound;
                loopId += tcNormal;
                picturesProcessed[shift]++;
                if (!loadLocation.Contains(".") && i < iNameStop - tcNormal * iNameStep)
                {
                    place.Dispose();
                    place = new Bitmap($@"{loadLocation}\{i + tcNormal * iNameStep:00000}.png");
                }
                //if (shift == 0)
                //{
                //    sw.Stop();
                //    iterationsW++;
                //    overallTimeW += sw.ElapsedMilliseconds;
                //    Console.WriteLine($"Iteration: {iterationsW} | {sw.ElapsedMilliseconds}ms ({overallTimeW / iterationsW}ms average) --> per picture");
                //}
            }


            void searchQuarter(int xStart, int yStart, int xStop, int yStop)
            {
                mutex.WaitOne();
                //if (shift == 0 && xStart == 997 && yStart == 997) s.Restart();
                Bitmap bmpTemp = place.Clone(new Rectangle(xStart, yStart, xStop, yStop), place.PixelFormat);
                mutex.ReleaseMutex();
                Bitmap bmpTempF = (Bitmap)bmpTemp.Clone();
                //if (shift == 0 && xStart == 997 && yStart == 997)
                //{
                //    s.Stop();
                //    iterations++;
                //    overallTime += s.ElapsedMilliseconds;
                //    Console.WriteLine($"Iteration: {iterations} | {s.ElapsedMilliseconds}ms ({overallTime / iterations}ms average) --> clone bitmap");
                //}

                unsafe
                {
                    int* c1;
                    int* c2;
                    //Parts from this Source: http://csharpexamples.com/fast-image-processing-c/
                    BitmapData bmpTempData = bmpTemp.LockBits(new Rectangle(0, 0, bmpTemp.Width, bmpTemp.Height), ImageLockMode.ReadWrite, bmpTemp.PixelFormat);
                    BitmapData bmpTempDataF = bmpTempF.LockBits(new Rectangle(0, 0, bmpTempF.Width, bmpTempF.Height), ImageLockMode.ReadWrite, bmpTempF.PixelFormat);
                    int bytesPerPixel = Image.GetPixelFormatSize(bmpTempF.PixelFormat) / 8;
                    byte* ptrFirstPixel = (byte*)bmpTempData.Scan0;
                    byte* ptrFirstPixelF = (byte*)bmpTempDataF.Scan0;

                    //Darken backgound/output bitmap data
                    //if (shift == 0 && xStart == 997 && yStart == 997) s.Restart();
                    for (int y = 0; y < bmpTempDataF.Height; y++)
                    {
                        byte* currentLine = ptrFirstPixelF + y * bmpTempDataF.Stride;
                        for (int x = 0; x < bmpTempDataF.Stride; x += bytesPerPixel)
                        {
                            //Calculate new pixel value (R,G,B)
                            currentLine[x + 2] = (byte)(currentLine[x + 2] * 0.25);
                            currentLine[x + 1] = (byte)(currentLine[x + 1] * 0.25);
                            currentLine[x] = (byte)(currentLine[x] * 0.25);
                        }
                    }
                    //if (shift == 0  && xStart == 997 && yStart == 997)
                    //{
                    //    s.Stop();
                    //    iterations++;
                    //    overallTime += s.ElapsedMilliseconds;
                    //    Console.WriteLine($"Iteration: {iterations} | {s.ElapsedMilliseconds}ms ({overallTime / iterations}ms average) --> darken background");
                    //}

                    //Loop through pixel/search amongus
                    //if (shift == 0 && xStart == 997 && yStart == 997) s.Restart();
                    for (int y = 0; y < yStop - 4; y++)
                    {
                        for (int x = 0; x < xStop - 3; x++)
                        {
                            for (int m = 0; m < 2; m++)
                            {
                                bool mirror = false;
                                bool search = true;
                                int border = 0;
                                byte* currentLine = ptrFirstPixel + y * bmpTempData.Stride;

                                if (m == 0) mirror = false;
                                else if (m == 1) mirror = true;

                                c1 = getColor(currentLine, tXco(x, -0.5, mirror) * bytesPerPixel);
                                c2 = getColor(currentLine, tXco(x, 0.5, mirror) * bytesPerPixel + bmpTempData.Stride);
                                int[] c1val = new int[4] { c1[0], c1[1], c1[2], c1[3] };

                                if (!compareColor(c2, getColor(currentLine, tXco(x, 1.5, mirror) * bytesPerPixel + bmpTempData.Stride)) || compareColor(c1, c2) || compareColor(c1, getColor(currentLine, tXco(x, -1.5, mirror) * bytesPerPixel))) continue;
                                //Check amongus shape
                                for (int row = 0; row < 5; row++)
                                {
                                    for (int column = 0; column < 4; column++)
                                    {
                                        currentLine = ptrFirstPixel + (y + row) * bmpTempData.Stride;
                                        if (amongus[row, column] == 2 && !compareColor(c1, getColor(currentLine, x + tXco(x, -1.5 + column, mirror) * bytesPerPixel)))
                                        {
                                            search = false;
                                            break;
                                        }
                                        else if (amongus[row, column] == 0 && compareColor(c1, getColor(currentLine, x + tXco(x, -1.5 + column, mirror) * bytesPerPixel)))
                                        {
                                            border++;
                                        }
                                    }
                                }
                                //Check around amongus shape
                                if (search)
                                {
                                    if (x < xStop - 5 && !mirror || x > 0 && mirror)
                                    {
                                        for (int row = 0; row < 5; row++)
                                        {
                                            currentLine = ptrFirstPixel + (y + row) * bmpTempData.Stride;
                                            if (compareColor(c1, getColor(currentLine, x + tXco(x, 2.5, mirror) * bytesPerPixel)))
                                            {
                                                border++;
                                            }
                                        }
                                    }
                                    if ((x > 0 && !mirror) || x < xStop - 5 && mirror)
                                    {
                                        for (int row = 1; row < 3; row++)
                                        {
                                            currentLine = ptrFirstPixel + (y + row) * bmpTempData.Stride;
                                            if (compareColor(c1, getColor(currentLine, x + tXco(x, -2.5, mirror) * bytesPerPixel)))
                                            {
                                                border++;
                                            }
                                        }
                                    }
                                    if (y > 0)
                                    {
                                        for (int column = 1; column < 4; column++)
                                        {
                                            currentLine = ptrFirstPixel + (y - 1) * bmpTempData.Stride;
                                            if (compareColor(c1, getColor(currentLine, x + tXco(x, -1.5 + column, mirror) * bytesPerPixel)))
                                            {
                                                border++;
                                            }
                                        }
                                    }
                                    if (border >= 5)
                                    {
                                        search = false;
                                        break;
                                    }
                                }
                                //If amongus found --> draw amongus to output bitmap data
                                if (search)
                                {
                                    for (int row = 0; row < 5; row++)
                                    {
                                        for (int column = 0; column < 4; column++)
                                        {
                                            byte* currentLineD = ptrFirstPixelF + (y + row) * bmpTempDataF.Stride;
                                            if (amongus[row, column] >= 2 && compareColor(c1, getColor(currentLine, x + tXco(x, -1.5 + column, mirror) * bytesPerPixel)))
                                            {
                                                currentLineD[tXco(x, column - 1.5, mirror) * bytesPerPixel + 2] = (byte)c1[0];
                                                currentLineD[tXco(x, column - 1.5, mirror) * bytesPerPixel + 1] = (byte)c1[1];
                                                currentLineD[tXco(x, column - 1.5, mirror) * bytesPerPixel] = (byte)c1[2];
                                            }
                                            else if (amongus[row, column] == 1)
                                            {
                                                currentLineD[tXco(x, column - 1.5, mirror) * bytesPerPixel + 2] = (byte)c2[0];
                                                currentLineD[tXco(x, column - 1.5, mirror) * bytesPerPixel + 1] = (byte)c2[1];
                                                currentLineD[tXco(x, column - 1.5, mirror) * bytesPerPixel] = (byte)c2[2];
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
                    //if (shift == 0 && xStart == 997 && yStart == 997)
                    //{
                    //    s.Stop();
                    //    iterations++;
                    //    overallTime += s.ElapsedMilliseconds;
                    //    Console.WriteLine($"Iteration: {iterations} | {s.ElapsedMilliseconds}ms ({overallTime / iterations}ms average) --> search amongus");
                    //}
                    bmpTemp.UnlockBits(bmpTempData);
                    bmpTempF.UnlockBits(bmpTempDataF);
                    mutex.WaitOne();
                    //if (shift == 0 && xStart == 997 && yStart == 997) s.Restart();
                    g = Graphics.FromImage(bmp);
                    g.DrawImage(bmpTempF, xStart, yStart, xStop, yStop);
                    //if (shift == 0  && xStart == 997 && yStart == 997)
                    //{
                    //    s.Stop();
                    //    iterations++;
                    //    overallTime += s.ElapsedMilliseconds;
                    //    Console.WriteLine($"Iteration: {iterations} | {s.ElapsedMilliseconds}ms ({overallTime / iterations}ms average) --> draw quarter to final image");
                    //}
                    mutex.ReleaseMutex();
                }
                bmpTemp.Dispose();
                bmpTempF.Dispose();

                unsafe int* getColor(byte* ptrC, int x)
                {
                    int* output = stackalloc int[4] { ptrC[x], ptrC[x + 1], ptrC[x + 2], ptrC[x + 3]};
                    return output;
                }

                unsafe bool compareColor(int* color1, int* color2)
                {
                    bool match = false;
                    if (color1[0] == color2[0] && color1[1] == color2[1] && color1[2] == color2[2]) match = true;
                    return match;
                }
            }
            g.Dispose();
            place.Dispose();
            bmp.Dispose();
            mutex.Dispose();

            int tXco(int xC, double move, bool mirror)
            {
                int f = 1;
                if (mirror) f = -1;
                return (int)(xC + 1.5 + move * f);
            }
            
        }
        public int roundUp(double value, bool roundUp)
        {
            int result = (int)value;
            if (result < value && !roundUp) result++;
            return result;
        }
    }
}
