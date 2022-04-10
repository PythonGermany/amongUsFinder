using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;

namespace amongUsFinder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Bitmap place;
            Bitmap bmp;
            Graphics g;
            int iName = 1;
            int iNameStop = 10330;
            int iNameStep = 2;
            int picturesProcessed = iName - 1;
            int numberFound;
            int xThreads = 4, yThreads = 4;
            Point[,] threadLoc = new Point[xThreads, yThreads];
            Thread[] threads = new Thread[threadLoc.Length];
            Mutex mutex = new Mutex();
            Stopwatch stopwatch = new Stopwatch();

            for (int y = 0; y < yThreads; y++)
            {
                for (int x = 0; x < xThreads; x++)
                {
                    threadLoc[x, y] = new Point(x * 2000 / xThreads, y * 2000 / yThreads);
                }
            }

            while (iName <= iNameStop)
            {
                //stopwatch.Start();
                place = new Bitmap($@"C:\Users\pythongermany\Downloads\fromWeb\Timelapse ({iName}).png");
                bmp = new Bitmap(place.Width, place.Height);
                g = Graphics.FromImage(bmp);
                numberFound = 0;
                int ct = 0;
                foreach (var item in threadLoc)
                {
                    threads[ct] = new Thread(() => searchAmongus(item.X, item.Y, 2000 / xThreads, 2000 / yThreads));
                    threads[ct].Start();
                    ct++;
                }
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Join();
                }
                //Console.WriteLine($"Final number found {numberFound}");
                picturesProcessed++;
                bmp.Save($@"C:\Users\pythongermany\Downloads\timelapse3\{picturesProcessed.ToString("0000")}.png", ImageFormat.Png);
                File.AppendAllText(@"C:\Users\pythongermany\Downloads\timelapse3\amongUsCount.txt", numberFound.ToString() + "\n");
                Console.WriteLine($"Image {picturesProcessed} saved");
                if (picturesProcessed % 100 == 0)
                {
                    double progress = Math.Round((double)picturesProcessed / (iNameStop / iNameStep) * 100, 2);
                    Console.WriteLine("Progress --> " + progress + "%");
                }
                bmp.Dispose();
                place.Dispose();
                g.Dispose();
                iName += iNameStep;
                //Console.WriteLine("Processtime: " + stopwatch.ElapsedMilliseconds + "ms");
                //stopwatch.Reset();
            }
            Console.ReadKey();

            void searchAmongus(int xStart, int yStart, int xStop, int yStop)
            {
                //Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} started; x|y: {xStart}|{yStart} --> {xStop}|{yStop}");
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
                Graphics gTempF = Graphics.FromImage(bmpTempF);
                //BitmapData bmpTempFdata = bmpTempF.LockBits(new Rectangle(0, 0, bmpTempF.Width, bmpTempF.Height), ImageLockMode.ReadWrite, bmpTempF.PixelFormat);
                //IntPtr ptr = bmpTempFdata.Scan0;
                //int bytes = Math.Abs(bmpTempFdata.Stride) * bmpTempFdata.Height;
                //byte[] colorData = new byte[bytes];
                //System.Runtime.InteropServices.Marshal.Copy(ptr, colorData, 0, bytes);
                //for (int i = 0; i < colorData.Length - 3; i += 4)
                //{
                //    byte bw = (byte)Math.Round((double)(colorData[i] + colorData[i + 1] + colorData[i + 2]) / 3, 0);
                //    colorData[i] = bw;
                //    colorData[i + 1] = bw;
                //    colorData[i + 2] = bw;
                //}
                //System.Runtime.InteropServices.Marshal.Copy(colorData, 0, ptr, bytes);
                //bmpTempF.UnlockBits(bmpTempFdata);
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
                for (int y = 0; y < yStop - 4; y++)
                {
                    for (int x = 0; x < xStop - 3; x++)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            bool mirror = false;
                            bool search = true;
                            int border = 0;
                            if (i == 0) mirror = false;
                            else if (i == 1) mirror = true;
                            c1 = bmpTemp.GetPixel(tC(x, -0.5, mirror), y);
                            c2 = bmpTemp.GetPixel(tC(x, 0.5, mirror), y + 1);
                            if (c2 != bmpTemp.GetPixel(tC(x, 1.5, mirror), y + 1) || c2 == c1 || c1 == bmpTemp.GetPixel(tC(x, -1.5, mirror), y)) continue;
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
                                numberFound++;
                                x += 3;
                                i = 2;
                            }
                        }
                    }
                }
                mutex.WaitOne();
                g.DrawImage(bmpTempF, xStart, yStart, xStop, yStop);
                mutex.ReleaseMutex();
                bmpTemp.Dispose();
                bmpTempF.Dispose();
                gTempF.Dispose();
                //Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " ended");
            }

            int tC(int xC, double shift, bool mirror)
            {
                int f = 1;
                if (mirror) f = -1;
                return (int)(xC + 1.5 + shift * f);
            }
        }
    }
}