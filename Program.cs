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
                string saveLoc = $@"C:\Users\pythongermany\Downloads\" + Console.ReadLine();
                if (s.loadLocation != "") s.saveLocation = saveLoc;
                Console.WriteLine("Enter start point (default: 1):");
                string start = Console.ReadLine();
                if (start != "") s.iName = Convert.ToInt32(start);
                Console.WriteLine("Enter stop point (default: file number of input):");
                string stop = Console.ReadLine();
                if (stop != "") s.iNameStop = Convert.ToInt32(stop);
                else s.iNameStop = Directory.GetFiles(s.loadLocation, "*.*", SearchOption.TopDirectoryOnly).Length;
                Console.WriteLine("Enter step length (default: 1):");
                string step = Console.ReadLine();
                if (step != "") s.iNameStep = Convert.ToInt32(step);
                s.amongusCount = new int[(s.iNameStop - s.iName) / s.iNameStep + 2];
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} | Started!");
                Console.WriteLine(s.iNameStop);

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
                int p = s.picturesProcessed[0] + s.picturesProcessed[1] + s.picturesProcessed[2] + s.picturesProcessed[3];
                while (p < (double)(s.iNameStop - s.iName) / s.iNameStep)
                {
                    if (min != DateTime.Now.Minute)
                    {
                        p = s.picturesProcessed[0] + s.picturesProcessed[1] + s.picturesProcessed[2] + s.picturesProcessed[3];
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss} | {p} pictures processed --> {Math.Round(p / ((double)(s.iNameStop - s.iName) / s.iNameStep) * 100, 2)}% done");
                        min = DateTime.Now.Minute;
                    }
                    Thread.Sleep(990);
                }

                //Wait for each thread to finish
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Join();
                }

                for (int i = 0; i < 4; i++)
                {
                    s.picturesProcessed[i] = 0;
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
                    for (int i = 0; i < (s.iNameStop - s.iName) / s.iNameStep + 1; i++)
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