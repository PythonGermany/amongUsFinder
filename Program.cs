using System;
using System.Threading;
using System.Drawing;

namespace amongUsFinder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Enter username: ");
            //string username = Console.ReadLine();
            string username = "pythongermany";
            //Console.WriteLine("Enter filename: ");
            //var filePath = @"C:\Users\" + username + @"\Downloads\" + Console.ReadLine();
            var filePath = @"C:\Users\pythongermany\Downloads\040420222340.png";
            int numberFound = 0;
            Bitmap place = new Bitmap(filePath);
            Bitmap bmp = new Bitmap(filePath);
            bool end = false;
            int[,] amongus = new int[5, 4] { { 0, 2, 2, 2},
                                             { 2, 2, 1, 1},
                                             { 2, 2, 2, 2},
                                             { 3, 2, 3, 2},
                                             { 0, 3, 0, 3}};
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
            Thread t = new Thread(() => searchAmongus(0, 0));
            t.Start();

            while (true)
            {
                Thread.Sleep(1000);
                if (end)
                {
                    bmp.Save(@"C:\Users\" + username + @"\Downloads\test.png", System.Drawing.Imaging.ImageFormat.Png);
                    Console.WriteLine($"0|2000 --> {numberFound} found");
                    break;
                }
            }
            Console.ReadKey();

            void searchAmongus(int xStart, int yStart)
            {
                int xStop = 2000, yStop = 2000;
                if (xStart > 3)
                {
                    xStart -= 3;
                    xStop += 3;
                }
                if(yStart > 3)
                {
                    yStart -= 3;
                    yStop += 3;
                }
                for (int y = yStart; y < yStart + yStop - 4; y++)
                {
                    for (int x = xStart; x < xStart + xStop - 3; x++)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            bool mirror = false;
                            bool search = true;
                            if (i == 0) mirror = false;
                            else if (i == 1) mirror = true;
                            Color c1 = place.GetPixel(tC(x, -0.5, mirror), y);
                            Color c2 = place.GetPixel(tC(x, 0.5, mirror), y + 1);
                            if (c2 != place.GetPixel(tC(x, 1.5, mirror), y + 1) || c2 == c1) search = false;
                            if (search)
                            {
                                int priority = 2;
                                int border = 0;
                                for (int p = priority; p >= 0; p--)
                                {
                                    for (int row = 0; row < 5; row++)
                                    {
                                        for (int column = 0; column < 4; column++)
                                        {
                                            if (amongus[row, column] == 2 && c1 != place.GetPixel(tC(x, -1.5 + column, mirror), y + row))
                                            {
                                                search = false;
                                                break;
                                            }
                                            else if (amongus[row, column] == 0 && c1 == place.GetPixel(tC(x, -1.5 + column, mirror), y + row))
                                            {
                                                border++;
                                            }
                                        }
                                    }
                                }
                                if (border >= 3)
                                {
                                    search = false;
                                    break;
                                }
                            }
                            if (search)
                            {
                                drawAmongus(x, y, mirror, c1, c2);
                                numberFound++;
                                x += 3;
                                i = 2;
                            }
                        }
                        if (x == 0 && y % 100 == 0)
                        {
                            Console.WriteLine($"{x}|{y} --> {numberFound} found"); 
                        }
                    }
                }
                end = true;
            }

            int tC(int xC, double shift, bool mirror)
            {
                int f = 1;
                if (mirror) f = -1;
                return (int)(xC + 1.5 + shift * f);
            }

            void drawAmongus(int xS, int yS, bool mirror, Color c1S, Color c2S)
            {
                for (int row = 0; row < 5; row++)
                {
                    for (int column = 0; column < 4; column++)
                    {
                        if (amongus[row, column] >= 2 && c1S == place.GetPixel(tC(xS, column - 1.5, mirror), yS + row))
                        {
                            bmp.SetPixel(tC(xS, column - 1.5, mirror), yS + row, c1S);
                        }
                        else if (amongus[row, column] == 1)
                        {
                            bmp.SetPixel(tC(xS, column - 1.5, mirror), yS + row, c2S);
                        }
                    }
                }
            }
        }
    }
}
