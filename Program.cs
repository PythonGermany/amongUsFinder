using System;
using System.Threading;
using System.IO;

namespace amongUsFinder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SearchAmongus s = new SearchAmongus();
            Thread[] threads = new Thread[4];
            int iName = 1;
            int iNameStop = 10330;
            int iNameStep = 1;
            int picturesProcessed = 0;

            //Start main threads
            Console.WriteLine("Enter start point:");
            iName = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Enter stop point:");
            iNameStop = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Enter step length:");
            iNameStep = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Input location:");
            string loadFolder = $@"C:\Users\pythongermany\Downloads\" + Console.ReadLine();
            Console.WriteLine("Output location: ");
            string saveFolder = $@"C:\Users\pythongermany\Downloads\" + Console.ReadLine();
            threads[0] = new Thread(() => s.searchAmongus(loadFolder, saveFolder, iName + 0 * iNameStep, iNameStop, iNameStep));
            threads[1] = new Thread(() => s.searchAmongus(loadFolder, saveFolder, iName + 1 * iNameStep, iNameStop, iNameStep));
            threads[2] = new Thread(() => s.searchAmongus(loadFolder, saveFolder, iName + 2 * iNameStep, iNameStop, iNameStep));
            threads[3] = new Thread(() => s.searchAmongus(loadFolder, saveFolder, iName + 3 * iNameStep, iNameStop, iNameStep));
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Start();
            }
            while (picturesProcessed < (double)(iNameStop - iName) / iNameStep)
            {
                picturesProcessed = Directory.GetFiles(loadFolder, "*.*", SearchOption.TopDirectoryOnly).Length;
                Console.WriteLine($"Saved {picturesProcessed} --> {picturesProcessed / ((double)(iNameStop - iName) / iNameStep) * 100}% done");
            }

            //Rename files
            int iNew = iName;
            for (int i = iName; i <= iNameStop; i+=4)
            {
                try
                {
                    File.Move($"{saveFolder}{i:00000}.png", $"{saveFolder}{iNew:00000}.png");
                }
                catch (Exception)
                {
                }
                iNew++;
            }
            Console.WriteLine("Finished! Press enter to exit");
            Console.ReadKey();
        }
    }
}