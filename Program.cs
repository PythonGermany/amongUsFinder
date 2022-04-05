using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace amongUsFinder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter username: ");
            string username = Console.ReadLine();
            Console.WriteLine("Enter filename: ");
            var filePath = @"C:\Users\" + username + @"\Downloads\" + Console.ReadLine();
            Bitmap place = new Bitmap(filePath);
            Bitmap bmp = new Bitmap(filePath);
            List<Color> border;
            int[,] amongus = new int[5, 4] { { 0, 1, 1, 1},
                                             { 1, 1, 2, 2},
                                             { 1, 1, 1, 1},
                                             { 3, 1, 3, 1},
                                             { 0, 3, 0, 3}};
            for (int y = 0; y < bmp.Height - 1; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = bmp.GetPixel(x, y);
                    Color cNew = Color.FromArgb((int)(c.A * 0.2), c.R, c.G, c.B);
                    bmp.SetPixel(x, y, cNew);
                }
            }
            int numberFound = 0;
            bool search;
            bool mirror = false;
            for (int y = 0; y <= place.Height - 5; y++)
            {
                for (int x = 0; x <= place.Width - 4 - 1; x++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        search = true;
                        if (i == 0) mirror = false;
                        else if (i == 1) mirror = true;
                        Color c1 = place.GetPixel(tC(x, -0.5), y);
                        Color c2 = place.GetPixel(tC(x, 0.5), y + 1);
                        if (c2 != place.GetPixel(tC(x, 1.5), y + 1) || c2 == c1) search = false;
                        if (search)
                        {
                            for (int row = 0; row < 5; row++)
                            {
                                for (int column = 0; column < 4; column++)
                                {
                                    if (amongus[row, column] == 1 && c1 != place.GetPixel(tC(x, -1.5 + column), y + row))
                                    {
                                        search = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (search)
                        {
                            border = new List<Color>()
                        {
                            //place.GetPixel(tC(x, -1.5), y),
                            //place.GetPixel(tC(x, -1.5), y + 3),
                            //place.GetPixel(tC(x, -1.5), y + 4),
                            place.GetPixel(tC(x, 0.5), y + 4)
                            };
                            if (x > 0 && !mirror || x < place.Width - 4 && mirror)
                            {
                                //border.Add(place.GetPixel(tC(x, -2.5), y + 1));
                                //border.Add(place.GetPixel(tC(x, -2.5), y + 2));
                            }
                            if (y > 0)
                            {
                                //border.Add(place.GetPixel(tC(x, -0.5), y - 1));
                                //border.Add(place.GetPixel(tC(x, 0.5), y - 1));
                                //border.Add(place.GetPixel(tC(x, 1.5), y - 1));
                            }
                            if (x < place.Width - 4 && !mirror || x > 0 && mirror)
                            {
                                //border.Add(place.GetPixel(tC(x, 2.5), y));
                                border.Add(place.GetPixel(tC(x, 2.5), y + 2));
                                //border.Add(place.GetPixel(tC(x, 2.5), y + 3));
                                //border.Add(place.GetPixel(tC(x, 2.5), y + 4));

                            }
                            if (border.Contains(c1))
                            {
                                search = false;
                            }
                            else
                            {
                                for (int row = 0; row < 5; row++)
                                {
                                    for (int column = 0; column < 4; column++)
                                    {
                                        if (amongus[row, column] == 1)
                                        {
                                            bmp.SetPixel(tC(x, column - 1.5), y + row, c1);
                                        }
                                        else if (amongus[row, column] == 2)
                                        {
                                            bmp.SetPixel(tC(x, column - 1.5), y + row, c2);
                                        }
                                        else if (amongus[row, column] == 3 && c1 == place.GetPixel(tC(x, column - 1.5), y + row))
                                        {
                                            bmp.SetPixel(tC(x, column - 1.5), y + row, c1);
                                        }
                                    }
                                }
                                numberFound++;
                                i = 2;
                            }
                        }
                    }
                    Console.WriteLine($"{x.ToString("0000")}|{y.ToString("0000")} -- > {numberFound} found");
                }
            }
            bmp.Save(@"C:\Users\" + username + @"\Downloads\test.png", System.Drawing.Imaging.ImageFormat.Png);
            Console.ReadKey();

            int tC(int x, double shift)
            {
                int f = 1;
                if (mirror) f = -1;
                return (int)(x + 1.5 + shift * f);
            }
        }
    }
}
