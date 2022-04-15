using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace amongUsFinder
{
    internal class SearchAmongus
    {
        public string loadLocation;
        public string saveLocation;
        public int iName = 1;
        public int iNameStop = 10330;
        public int iNameStep = 1;
        public int[] picturesProcessed = new int[4] {0, 0, 0, 0};
        public int[] amongusCount;
        
        public void searchAmongus(int shift = 0)
        {
            int amongUsFound;
            int loopId = shift;
            Mutex mutex = new Mutex();
            Thread[] threadsQ = new Thread[4];
            Bitmap bmp = new Bitmap(2000, 2000);
            Graphics g = Graphics.FromImage(bmp);
            Bitmap place;

            //Loop through pictures
            for (int i = iName + shift * iNameStep; i <= iNameStop; i += 4 * iNameStep)
            {
                amongUsFound = 0;
                //Start quad threads
                if (loadLocation.Contains(".")) place = new Bitmap($"{loadLocation}");
                else place = new Bitmap($@"{loadLocation}\{i:00000}.png");
                threadsQ[0] = new Thread(() => searchQuarter(0, 0, 1000, 1000));
                threadsQ[1] = new Thread(() => searchQuarter(997, 0, 1003, 1000));
                threadsQ[2] = new Thread(() => searchQuarter(0, 997, 1000, 1003));
                threadsQ[3] = new Thread(() => searchQuarter(997, 997, 1003, 1003));
                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Start();
                }
                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Join();
                }
                if (saveLocation.Contains(".")) bmp.Save($"{saveLocation}" + Console.ReadLine(), ImageFormat.Png);
                else bmp.Save($@"{saveLocation}\{i:00000}.png", ImageFormat.Png);
                place.Dispose();
                amongusCount[loopId] = amongUsFound;
                loopId += 4;
                picturesProcessed[shift]++;
            }


            void searchQuarter(int xStart, int yStart, int xStop, int yStop)
            {
                int[] c1 = new int[3];
                int[] c2 = new int[3];
                int[,] amongus = new int[5, 4] { { 0, 2, 2, 2},
                                                 { 2, 2, 1, 1},
                                                 { 2, 2, 2, 2},
                                                 { 3, 2, 3, 2},
                                                 { 0, 3, 0, 3}};
                mutex.WaitOne();
                Bitmap bmpTemp = place.Clone(new Rectangle(xStart, yStart, xStop, yStop), place.PixelFormat);
                mutex.ReleaseMutex();
                Bitmap bmpTempF = (Bitmap)bmpTemp.Clone();

                unsafe
                {
                    //Parts from this Source: http://csharpexamples.com/fast-image-processing-c/
                    BitmapData bmpData = bmpTemp.LockBits(new Rectangle(0, 0, bmpTemp.Width, bmpTemp.Height), ImageLockMode.ReadWrite, bmpTemp.PixelFormat);
                    BitmapData bmpDataF = bmpTempF.LockBits(new Rectangle(0, 0, bmpTempF.Width, bmpTempF.Height), ImageLockMode.ReadWrite, bmpTempF.PixelFormat);
                    int bytesPerPixel = Bitmap.GetPixelFormatSize(bmpTempF.PixelFormat) / 8;
                    byte* ptrFirstPixel = (byte*)bmpData.Scan0;
                    byte* ptrFirstPixelF = (byte*)bmpDataF.Scan0;

                    //Darken backgound/output bitmap
                    for (int y = 0; y < bmpDataF.Height; y++)
                    {
                        byte* currentLine = ptrFirstPixelF + y * bmpDataF.Stride;
                        for (int x = 0; x < bmpDataF.Width * bytesPerPixel; x += bytesPerPixel)
                        {
                            //Calculate new pixel value (R,G,B)
                            currentLine[x + 2] = (byte)(currentLine[x + 2] * 0.25);
                            currentLine[x + 1] = (byte)(currentLine[x + 1] * 0.25);
                            currentLine[x + 0] = (byte)(currentLine[x] * 0.25);;
                        }
                    }
                    // copy modified bytes back
                    //-------------------------------------------------------------------------------------------------

                    //Loop through pixel/search amongus
                    for (int y = 0; y < yStop - 4; y++)
                    {
                        for (int x = 0; x < xStop - 3; x++)
                        {
                            for (int m = 0; m < 2; m++)
                            {
                                bool mirror = false;
                                bool search = true;
                                int border = 0;

                                if (m == 0) mirror = false;
                                else if (m == 1) mirror = true;

                                c1 = getPixelColor(tXco(x, -0.5, mirror), y);
                                c2 = getPixelColor(tXco(x, 0.5, mirror), y + 1);

                                if (!compareColor(c2, getPixelColor(tXco(x, 1.5, mirror), y + 1)) || compareColor(c1, c2) || compareColor(c1, getPixelColor(tXco(x, -1.5, mirror), y))) continue;
                                //Check amongus shape
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
                                //Check around amongus shape
                                if (search)
                                {
                                    if (x < xStop - 5 && !mirror || x > 0 && mirror)
                                    {
                                        for (int row = 0; row < 5; row++)
                                        {
                                            if (compareColor(c1, getPixelColor(tXco(x, 2.5, mirror), y + row)))
                                            {
                                                border++;
                                            }
                                        }
                                    }
                                    if ((x > 0 && !mirror) || x < xStop - 5 && mirror)
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
                                    if (border >= 5)
                                    {
                                        search = false;
                                        break;
                                    }
                                }
                                //If amongus found --> draw amongus to output bitmap
                                if (search)
                                {
                                    for (int row = 0; row < 5; row++)
                                    {
                                        for (int column = 0; column < 4; column++)
                                        {
                                            byte* currentLine = ptrFirstPixelF + (y + row) * bmpDataF.Stride;
                                            if (amongus[row, column] >= 2 && compareColor(c1, getPixelColor(tXco(x, -1.5 + column, mirror), y + row)))
                                            {
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel + 2] = (byte)c1[0];
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel + 1] = (byte)c1[1];
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel] = (byte)c1[2];
                                            }
                                            else if (amongus[row, column] == 1)
                                            {
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel + 2] = (byte)c2[0];
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel + 1] = (byte)c2[1];
                                                currentLine[tXco(x, column - 1.5, mirror) * bytesPerPixel] = (byte)c2[2];
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
                    bmpTemp.UnlockBits(bmpData);
                    bmpTempF.UnlockBits(bmpDataF);

                    int[] getPixelColor(int x, int y)
                    {
                        byte* currentLine = ptrFirstPixel + y * bmpData.Stride;
                        return new int[3] { currentLine[x * bytesPerPixel + 2], currentLine[x * bytesPerPixel + 1], currentLine[x * bytesPerPixel] };
                    }
                }
                mutex.WaitOne();
                g.DrawImage(bmpTempF, xStart, yStart, xStop, yStop);
                mutex.ReleaseMutex();
                bmpTemp.Dispose();
                bmpTempF.Dispose();

                bool compareColor(int[] color1, int[] color2)
                {
                    bool match = true;
                    for (int i = 0; i < color1.Length; i++)
                    {
                        if (color1[i] != color2[i]) match = false;
                    }
                    return match;
                }
            }
        }

        int tXco(int xC, double shift, bool mirror)
        {
            int f = 1;
            if (mirror) f = -1;
            return (int)(xC + 1.5 + shift * f);
        }
    }
}
