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

            while (true)
            {
                //Parameter input
                Console.WriteLine("Input location:");
                s.loadLocation = $@"C:\Users\pythongermany\Downloads\" + Console.ReadLine();
                Console.WriteLine("Output location: ");
                s.saveLocation = $@"C:\Users\pythongermany\Downloads\" + Console.ReadLine();
                Console.WriteLine("Enter start point (default: 1):");
                string start = Console.ReadLine();
                if (start != null) s.iName = Convert.ToInt32(start);
                Console.WriteLine("Enter stop point (default: file number of input):");
                string stop = Console.ReadLine();
                if (stop != null) s.iNameStop = Convert.ToInt32(stop);
                else s.iNameStop = Directory.GetFiles(s.loadLocation, "*.*", SearchOption.TopDirectoryOnly).Length;
                Console.WriteLine("Enter step length (default: 1):");
                string step = Console.ReadLine();
                if (step != null) s.iNameStep = Convert.ToInt32(step);
                s.amongusCount = new int[(s.iNameStop - s.iName) / s.iNameStep + 2];
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} | Started!");

                //Start main threads
                threads[0] = new Thread(() => s.searchAmongus());
                threads[1] = new Thread(() => s.searchAmongus(1));
                threads[2] = new Thread(() => s.searchAmongus(2));
                threads[3] = new Thread(() => s.searchAmongus(3));
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Start();
                }

                //Output progress updates
                int min = DateTime.Now.Minute;
                while (s.picturesProcessed < (double)(s.iNameStop - s.iName) / s.iNameStep)
                {
                    if (min != DateTime.Now.Minute)
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss} | {s.picturesProcessed} pictures processed --> {Math.Round(s.picturesProcessed / ((double)(s.iNameStop - s.iName) / s.iNameStep) * 100, 2)}% done");
                        min = DateTime.Now.Minute;
                    }
                    Thread.Sleep(990);
                }

                //Wait for each thread to finish
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Join();
                }

                //Rename files
                if (s.iNameStep > 1)
                {
                    int iNew = 1;
                    for (int i = s.iName; i <= s.iNameStop; i += s.iNameStep)
                    {
                        try
                        {
                            File.Move($@"{s.saveLocation}\{i:00000}.png", $@"{s.saveLocation}\{iNew:00000}.png");
                        }
                        catch { }
                        iNew++;
                    }
                }

                //Output txt file
                using (StreamWriter sr = new StreamWriter(s.saveLocation + @"\amongUsCount.txt"))
                {
                    for (int i = 0; i < s.picturesProcessed; i++)
                    {
                        sr.WriteLine(s.amongusCount[i]);
                    }
                }

                Console.WriteLine($"{DateTime.Now:HH:mm:ss} | Finished!"); 
            }
            //Console.ReadKey();
        }
    }
}