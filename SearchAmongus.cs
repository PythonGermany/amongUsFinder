using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace amongUsFinder
{
    internal class SearchAmongus
    {
        public void searchAmongus(string loadLocation, string saveLocation, int start = 1, int stop = 1, int step = 1)
        {
            Mutex mutex = new Mutex();
            Thread[] threadsQ = new Thread[4];
            Bitmap bmp = new Bitmap(2000, 2000);
            Graphics g = Graphics.FromImage(bmp);
            Bitmap place;

            //Loop through pictures
            for (int i = start; i <= stop; i += 4 * step)
            {
                //Start quad threads
                if (loadLocation.Contains(".")) place = new Bitmap($"{loadLocation}");
                else place = new Bitmap($"{loadLocation}{i:00000}.png");
                threadsQ[0] = new Thread(() => searchQuarter(0, 0, 1000, 1000));
                threadsQ[1] = new Thread(() => searchQuarter(1000, 0, 1000, 1000));
                threadsQ[2] = new Thread(() => searchQuarter(0, 1000, 1000, 1000));
                threadsQ[3] = new Thread(() => searchQuarter(1000, 1000, 1000, 1000));
                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Start();
                }
                for (int t = 0; t < threadsQ.Length; t++)
                {
                    threadsQ[t].Join();
                }
                if (saveLocation.Contains(".")) bmp.Save($"{saveLocation}" + Console.ReadLine(), ImageFormat.Png);
                else bmp.Save($"{saveLocation}{i:0000}.png", ImageFormat.Png);
                place.Dispose();
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
                            //If amongus found --> draw to output bitmap
                            if (search)
                            {
                                for (int row = 0; row < 5; row++)
                                {
                                    for (int column = 0; column < 4; column++)
                                    {
                                        if (amongus[row, column] >= 2 && c1 == bmpTemp.GetPixel(tC(x, column - 1.5, mirror), y + row))
                                        {
                                            bmpTempF.SetPixel(tC(x, column - 1.5, mirror), y + row, c1);
                                        }
                                        else if (amongus[row, column] == 1)
                                        {
                                            bmpTempF.SetPixel(tC(x, column - 1.5, mirror), y + row, c2);
                                        }
                                    }
                                }
                                //numberFound++;
                                x += 3;
                                m = 2;
                            }
                        }
                    }
                }
                mutex.WaitOne();
                g.DrawImage(bmpTempF, xStart, yStart, xStop, yStop);
                mutex.ReleaseMutex();
                bmpTempF.Dispose();
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
