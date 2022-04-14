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
        public int picturesProcessed = 0;
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
            //Stopwatch s = new Stopwatch();

            //Loop through pictures
            for (int i = iName + shift * iNameStep; i <= iNameStop; i += 4 * iNameStep)
            {
                //s.Restart();
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
                picturesProcessed++;
                //s.Stop();
                //Console.WriteLine($"{loopId}: {s.ElapsedMilliseconds}ms");
            }


            void searchQuarter(int xStart, int yStart, int xStop, int yStop)
            {
                Color c1;
                Color c2;
                int[,] amongus = new int[5, 4] { { 0, 2, 2, 2},
                                                 { 2, 2, 1, 1},
                                                 { 2, 2, 2, 2},
                                                 { 3, 2, 3, 2},
                                                 { 0, 3, 0, 3}};
                mutex.WaitOne();
                Bitmap bmpTemp = place.Clone(new Rectangle(xStart, yStart, xStop, yStop), place.PixelFormat);
                mutex.ReleaseMutex();
                Bitmap bmpTempF = (Bitmap)bmpTemp.Clone();

                //Darken backgound/output bitmap
                for (int y = 0; y < bmpTempF.Height; y++)
                {
                    for (int x = 0; x < bmpTempF.Width; x++)
                    {
                        Color c = bmpTempF.GetPixel(x, y);
                        int bw = (int)Math.Round((double)(c.R + c.G + c.B) / 3, 0);
                        Color cNew = Color.FromArgb(c.A, (int)Math.Round(c.R * 0.25, 0), (int)Math.Round(c.G * 0.25, 0), (int)Math.Round(c.B * 0.25, 0));
                        bmpTempF.SetPixel(x, y, cNew);
                    }
                }

                //Source: http://csharpexamples.com/fast-image-processing-c/
                //BitmapData bmpData = bmpTempF.LockBits(new Rectangle(0, 0, bmpTempF.Width, bmpTempF.Height), ImageLockMode.ReadWrite, bmpTempF.PixelFormat);
                //int bytesPerPixel = Bitmap.GetPixelFormatSize(bmpTempF.PixelFormat) / 8;
                //int byteCount = bmpData.Stride * bmpTempF.Height;
                //byte[] pixels = new byte[byteCount];
                //IntPtr ptrFirstPixel = bmpData.Scan0;
                //Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
                //int heightInPixels = bmpData.Height;
                //int widthInBytes = bmpData.Width * bytesPerPixel;

                //for (int y = 0; y < heightInPixels; y++)
                //{
                //    int currentLine = y * bmpData.Stride;
                //    for (int x = 0; x < widthInBytes; x += bytesPerPixel)
                //    {
                //        int oldBlue = pixels[currentLine + x];
                //        int oldGreen = pixels[currentLine + x + 1];
                //        int oldRed = pixels[currentLine + x + 2];

                //        // calculate new pixel value
                //        pixels[currentLine + x] = (byte)(oldBlue * 0.25);
                //        pixels[currentLine + x + 1] = (byte)(oldGreen * 0.25);
                //        pixels[currentLine + x + 2] = (byte)(oldRed * 0.25);
                //    }
                //}
                // copy modified bytes back
                //Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
                //bmpTempF.UnlockBits(bmpData);
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
                            c1 = bmpTemp.GetPixel(tC(x, -0.5, mirror), y);
                            c2 = bmpTemp.GetPixel(tC(x, 0.5, mirror), y + 1);
                            if (c2 != bmpTemp.GetPixel(tC(x, 1.5, mirror), y + 1) || c2 == c1 || c1 == bmpTemp.GetPixel(tC(x, -1.5, mirror), y)) continue;
                            //Check amongus shape
                            for (int row = 0; row < 5; row++)
                            {
                                for (int column = 0; column < 4; column++)
                                {
                                    if (amongus[row, column] == 2 && c1 != bmpTemp.GetPixel(tC(x, -1.5 + column, mirror), y + row))
                                    {
                                        search = false;
                                        break;
                                    }
                                    else if (amongus[row, column] == 0 && c1 == bmpTemp.GetPixel(tC(x, -1.5 + column, mirror), y + row))
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
                                        if (c1 == bmpTemp.GetPixel(tC(x, 2.5, mirror), y + row))
                                        {
                                            border++;
                                        }
                                    }
                                }
                                if ((x > 0 && !mirror) || x < xStop - 5 && mirror)
                                {
                                    for (int row = 1; row < 3; row++)
                                    {
                                        if (c1 == bmpTemp.GetPixel(tC(x, -2.5, mirror), y + row))
                                        {
                                            border++;
                                        }
                                    }
                                }
                                if (y > 0)
                                {
                                    for (int column = 1; column < 4; column++)
                                    {
                                        if (c1 == bmpTemp.GetPixel(tC(x, -1.5 + column, mirror), y - 1))
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
                                        if (amongus[row, column] >= 2 && c1 == bmpTemp.GetPixel(tC(x, column - 1.5, mirror), y + row))
                                        {
                                            //pixels[getLine(y + row) + (x + column) * bytesPerPixel + 2] = (byte)c1.R;
                                            //pixels[getLine(y + row) + (x + column) * bytesPerPixel + 1] = (byte)c1.G;
                                            //pixels[getLine(y + row) + (x + column) * bytesPerPixel] = (byte)c1.B;
                                            bmpTempF.SetPixel(tC(x, column - 1.5, mirror), y + row, c1);
                                        }
                                        else if (amongus[row, column] == 1)
                                        {
                                            //pixels[getLine(y + row) + (x + column) * bytesPerPixel + 2] = (byte)c2.R;
                                            //pixels[getLine(y + row) + (x + column) * bytesPerPixel + 1] = (byte)c2.G;
                                            //pixels[getLine(y + row) + (x + column) * bytesPerPixel] = (byte)c2.B;
                                            bmpTempF.SetPixel(tC(x, column - 1.5, mirror), y + row, c2);
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
                //Marshal.Copy(pixels, 0, ptrFirstPixel, pixels.Length);
                //bmpTempF.UnlockBits(bmpData);
                mutex.WaitOne();
                g.DrawImage(bmpTempF, xStart, yStart, xStop, yStop);
                mutex.ReleaseMutex();
                bmpTempF.Dispose();

                //int getLine(int y)
                //{
                //    return y * bmpData.Stride;
                //}
            }
        }

        int tC(int xC, double shift, bool mirror)
        {
            int f = 1;
            if (mirror) f = -1;
            return (int)(xC + 1.5 + shift * f);
        }
    }
}
