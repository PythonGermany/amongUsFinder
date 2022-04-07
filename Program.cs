using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.IO;

namespace amongUsFinder
{
    internal class Program
    {

        static void Main(string[] args)
        {
            //----------------------------------------------------------------------------------
            //Console.WriteLine("Start");
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 0)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 1)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 2)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 3)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 4)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 5)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 6)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 7)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 8)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 9)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 10)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 11)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 12)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 13)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 14)).Start();
            //new Thread(() => imageFromWeb(1648822500 + 18600 * 15)).Start();

            //void imageFromWeb(int start)
            //{
            //    int i2 = 0;
            //    int stop = start + 18600;
            //    for (int i = start; i < stop; i++)
            //    {
            //        i2++;
            //        if (i2 == 100)
            //        {
            //            i2 = 0;
            //            Console.WriteLine($"{Thread.CurrentThread.ManagedThreadId}" + (stop - i));
            //        }
            //        string url = $@"https://rplace.space/combined/{i}.png";
            //        try
            //        {
            //            WebClient webClient = new WebClient();
            //            Stream stream = webClient.OpenRead(url);
            //            Bitmap image = new Bitmap(stream);
            //            stream.Flush();
            //            stream.Close();
            //            webClient.Dispose();
            //            image.Save($@"C:\Users\pythongermany\Downloads\fromWeb\{i}.png", System.Drawing.Imaging.ImageFormat.Png);
            //            image.Dispose();
            //        }
            //        catch (Exception)
            //        {
            //        }
            //    }
            //}
            //----------------------------------------------------------------------------------
            int iName = 1;
            Mutex mutex = new Mutex();
            Bitmap place = new Bitmap($@"C:\Users\pythongermany\Downloads\fromWeb\timelapse ({iName}).png");
            Bitmap bmp = new Bitmap($@"C:\Users\pythongermany\Downloads\fromWeb\timelapse ({iName}).png");
            int numberFound = 0;

            for (int y = 0; y < bmp.Height - 1; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    int bw = (int)Math.Round((double)(c.R + c.G + c.B) / 3, 0);
                    Color cNew = Color.FromArgb((int)(0.75 * c.A), bw, bw, bw);
                    bmp.SetPixel(x, y, cNew);
                }
            }

            new Thread(() => searchAmongus(0, 0)).Start();
            new Thread(() => searchAmongus(500, 0)).Start();
            new Thread(() => searchAmongus(1000, 0)).Start();
            new Thread(() => searchAmongus(1500, 0)).Start();

            new Thread(() => searchAmongus(0, 500)).Start();
            new Thread(() => searchAmongus(500, 500)).Start();
            new Thread(() => searchAmongus(1000, 1000)).Start();
            new Thread(() => searchAmongus(1500, 1500)).Start();

            new Thread(() => searchAmongus(0, 1000)).Start();
            new Thread(() => searchAmongus(500, 1000)).Start();
            new Thread(() => searchAmongus(1000, 1000)).Start();
            new Thread(() => searchAmongus(1500, 1000)).Start();

            new Thread(() => searchAmongus(0, 1500)).Start();
            new Thread(() => searchAmongus(500, 1500)).Start();
            new Thread(() => searchAmongus(1000, 1500)).Start();
            new Thread(() => searchAmongus(1500, 1500)).Start();

            //Console.ReadLine();
            //Console.WriteLine(numberFound + " matches found");
            bmp.Save($@"C:\Users\pythongermany\Downloads\timelapse\timelapse ({iName}).png", ImageFormat.Png);
            bmp.Dispose();
            place.Dispose();

            void searchAmongus(int xStart, int yStart)
            {
                int xStop = 500, yStop = 500;
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} started; x|y: {xStart}|{yStart} --> {xStop}|{yStop}");
                Color c1;
                Color c2;
                int[,] amongus = new int[5, 4] { { 0, 2, 2, 2},
                                                 { 2, 2, 1, 1},
                                                 { 2, 2, 2, 2},
                                                 { 3, 2, 3, 2},
                                                 { 0, 3, 0, 3}};
                if (xStart > 3)
                {
                    xStart -= 3;
                    xStop += 3;
                }
                if (yStart > 3)
                {
                    yStart -= 3;
                    yStop += 3;
                }
                mutex.WaitOne();
                Bitmap bmpTemp = (Bitmap)place.Clone(new RectangleF((float)xStart, (float)yStart, (float)xStop, (float)yStop), new PixelFormat());
                mutex.ReleaseMutex();
                for (int y = 0; y < yStop - 5; y++)
                {
                    for (int x = 0; x < xStop - 4; x++)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            bool mirror = false;
                            bool search = true;
                            if (i == 0) mirror = false;
                            else if (i == 1) mirror = true;
                            c1 = bmpTemp.GetPixel(tC(x, -0.5, mirror), y);
                            c2 = bmpTemp.GetPixel(tC(x, 0.5, mirror), y + 1);
                            if (c2 != bmpTemp.GetPixel(tC(x, 1.5, mirror), y + 1) || c2 == c1) continue;
                            int priority = 2;
                            int border = 0;
                            while (priority < 4 && search)
                            {
                                for (int row = 0; row < 5; row++)
                                {
                                    for (int column = 0; column < 4; column++)
                                    {
                                        if (amongus[row, column] == priority && priority == 2 && c1 != bmpTemp.GetPixel(tC(x, -1.5 + column, mirror), y + row))
                                        {
                                            search = false;
                                            break;
                                        }
                                        else if (amongus[row, column] == 0 && c1 == bmpTemp.GetPixel(tC(x, -1.5 + column, mirror), y + row))
                                        {
                                            border++;
                                            if (border > 4)
                                            {
                                                Console.WriteLine($"search stopped (border) {border}");
                                                search = false;
                                                break;
                                            }
                                        }
                                    }
                                    //Solves one problem, causes another
                                    //border = 0;
                                }
                                priority++;
                            }
                            if (search)
                            {
                                for (int row = 0; row < 5; row++)
                                {
                                    for (int column = 0; column < 4; column++)
                                    {
                                        if (amongus[row, column] >= 2 && c1 == bmpTemp.GetPixel(tC(x, column - 1.5, mirror), y + row))
                                        {
                                            mutex.WaitOne();
                                            bmp.SetPixel(tC(x + xStart, column - 1.5, mirror), y + yStart + row, c1);
                                            mutex.ReleaseMutex();
                                        }
                                        else if (amongus[row, column] == 1)
                                        {
                                            mutex.WaitOne();
                                            bmp.SetPixel(tC(x + xStart, column - 1.5, mirror), y + yStart + row, c2);
                                            mutex.ReleaseMutex();
                                        }
                                    }
                                }
                                numberFound++;
                                x += 3;
                                i = 2;
                            }
                        }
                        if (x == 0 && y % 100 == 0 && y > 0)
                        {
                            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId}: y = {y} --> {numberFound} found");
                        }
                    }
                }
                bmpTemp.Dispose();
                Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId + " ended");
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