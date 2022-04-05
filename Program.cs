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
            List<Color> amongus;
            List<Color> border;
            int numberFound = 0;
            Point amongusSize = new Point(3, 4);
            Bitmap place = new Bitmap("C:/Users/PythonGermany/Downloads/040420222340.png");
            Bitmap bmp = new Bitmap(place.Width, place.Height);
            bool stopSearch = false;
            for (int y = 0; y <= place.Height - amongusSize.Y - 1; y++)
            {
                for (int x = 0; x <= place.Width - amongusSize.X - 1; x++)
                {
                    stopSearch = false;
                    Color c1 = place.GetPixel(x + 1, y);
                    Color c2 = place.GetPixel(x + 2, y + 1);
                    if (c2 != place.GetPixel(x + 3, y + 1)) stopSearch = true;
                    if (!stopSearch)
                    {
                        amongus = new List<Color>()
                        {
                            place.GetPixel(x + 2, y), place.GetPixel(x + 3, y),
                            place.GetPixel(x, y + 1), place.GetPixel(x + 1, y + 1),
                            place.GetPixel(x, y + 2), place.GetPixel(x + 1, y + 2), place.GetPixel(x + 2, y + 2), place.GetPixel(x + 3, y + 2),
                            place.GetPixel(x + 1, y + 3), place.GetPixel(x + 2, y + 3), place.GetPixel(x + 3, y + 3),
                            place.GetPixel(x + 1, y + 4), place.GetPixel(x + 3, y + 4)
                        };
                        foreach (var pixel in amongus)
                        {
                            if (c1 != pixel)
                            {
                                stopSearch = true;
                                break;
                            }
                        }
                    }
                    if (!stopSearch)
                    {
                        border = new List<Color>()
                        {
                            place.GetPixel(x, y), 
                            place.GetPixel(x, y + 3), 
                            place.GetPixel(x, y + 4), place.GetPixel(x + 2, y + 4)
                        };
                        if (x > 0)
                        {
                            border.Add(place.GetPixel(x - 1, y + 1));
                            border.Add(place.GetPixel(x - 1, y + 2));
                        }
                        if (y > 0)
                        {
                            border.Add(place.GetPixel(x + 1, y - 1));
                            border.Add(place.GetPixel(x + 2, y - 1));
                            border.Add(place.GetPixel(x + 3, y - 1));
                        }
                        if (x < place.Width - amongusSize.X - 1)
                        {
                            border.Add(place.GetPixel(x + 4, y));
                            border.Add(place.GetPixel(x + 4, y + 2));
                            border.Add(place.GetPixel(x + 4, y + 3));
                            border.Add(place.GetPixel(x + 4, y + 4));
                        }
                        if (border.Contains(c1))
                        {
                            stopSearch = true;
                        }
                    }
                    if (!stopSearch)
                    {
                        bmp.SetPixel(x + 1, y, c1);
                        bmp.SetPixel(x + 2, y, c1);
                        bmp.SetPixel(x + 3, y, c1);
                        bmp.SetPixel(x, y + 1, c1);
                        bmp.SetPixel(x + 1, y + 1, c1);
                        bmp.SetPixel(x, y + 2, c1);
                        bmp.SetPixel(x + 1, y + 2, c1);
                        bmp.SetPixel(x + 2, y + 2, c1);
                        bmp.SetPixel(x + 3, y + 2, c1);
                        bmp.SetPixel(x + 1, y + 3, c1);
                        bmp.SetPixel(x + 2, y + 3, c1);
                        bmp.SetPixel(x + 3, y + 3, c1);
                        bmp.SetPixel(x + 1, y + 4, c1);
                        bmp.SetPixel(x + 3, y + 4, c1);

                        bmp.SetPixel(x + 2, y + 1, c2);
                        bmp.SetPixel(x + 3, y + 1, c2);
                        numberFound++;
                    }
                    Console.WriteLine($"{x}|{y} -- > {numberFound} found");
                }
            }
            bmp.Save(@"C:\Users\PythonGermany\Downloads\test.Png", System.Drawing.Imaging.ImageFormat.Png);
            Console.ReadKey();
        }
    }
}
