using System;
using System.IO;
using System.Threading;

namespace amongUsFinder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SearchAmongus s = new SearchAmongus();
            Thread[] threads = new Thread[s.tcNormal];
            int[] threadShift = new int[s.tcNormal];
            TimeSpan processTime;

            for (int i = 0; i < threadShift.Length; i++)
            {
                threadShift[i] = i;
            }

            while (true)
            {
                //Parameter input from console
                Console.WriteLine($@"Input location ({Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\YOUR INPUT):");
                s.loadLocation = $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\{Console.ReadLine()}";
                if (s.loadLocation.Contains("."))
                {
                    s.iName = 1;
                    s.iNameStop = 1;
                    s.iNameStep = 1;
                    int folderIndex = s.loadLocation.LastIndexOf(@"\");
                    s.saveLocation = s.loadLocation.Substring(0, folderIndex);
                }
                else
                {
                    Console.WriteLine($@"Output location ({Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\YOUR INPUT):");
                    s.saveLocation = $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\Downloads\{Console.ReadLine()}";
                    Console.WriteLine("Enter start point (default: 1):");
                    string start = Console.ReadLine();
                    if (start != "") s.iName = Convert.ToInt32(start);
                    else s.iName = 1;
                    Console.WriteLine("Enter stop point (default: file number of input):");
                    string stop = Console.ReadLine();
                    if (stop != "") s.iNameStop = Convert.ToInt32(stop);
                    else s.iNameStop = Directory.GetFiles(s.loadLocation, "*.*", SearchOption.TopDirectoryOnly).Length;
                    Console.WriteLine("Enter step length (default: 1):");
                    string step = Console.ReadLine();
                    if (step != "") s.iNameStep = Convert.ToInt32(step);
                    else s.iNameStep = 1;
                }
                s.amongusCount = new int[(s.iNameStop - s.iName) / s.iNameStep + 1];
                s.picturesProcessed = new int[s.tcNormal];

                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Started!");
                DateTime startTime = DateTime.Now;

                //Start main threads
                int ct = 0;
                foreach (int value in threadShift)
                {
                    threads[ct] = new Thread(() => s.searchAmongus(value));
                    threads[ct].Priority = ThreadPriority.Highest;
                    ct++;
                }
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Start();
                }
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                //Output progress updates to console every minute
                int min = DateTime.Now.Minute;
                int picturesProcessed = 0;
                double progressState = 0;
                while (progressState < 100)
                {
                    progressState = picturesProcessed / ((double)(s.iNameStop - s.iName + 1) / s.iNameStep) * 100;
                    if (min != DateTime.Now.Minute)
                    {
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | {picturesProcessed} pictures processed --> {Math.Round(progressState, 2)}% done");
                        min = DateTime.Now.Minute;
                    }
                    if (progressState >= 100) break;
                    Thread.Sleep(995);
                    picturesProcessed = 0;
                    foreach (var item in s.picturesProcessed)
                    {
                        picturesProcessed += item;
                    }
                }

                //Wait for each thread to finish
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Join();
                }

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                if (!s.loadLocation.Contains("."))
                {
                    processTime = DateTime.Now - startTime;
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Processing pictures completed in {processTime.ToString(@"mm\:ss\.fff")} with an average of {RoundUpValue(processTime.TotalSeconds / picturesProcessed, 3)}s per picture!");

                    //Rename processed files
                    if (s.iNameStep > 1)
                    {
                        int iNewName = 1;
                        for (int i = s.iName; i <= s.iNameStop; i += s.iNameStep)
                        {
                            try
                            {
                                File.Move($@"{s.saveLocation}\{i:00000}.png", $@"{s.saveLocation}\{iNewName:00000}.png");
                            }
                            catch { }
                            iNewName++;
                        }
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Files renamed (from 00001 to {iNewName:00000})");
                    }
                    //Generate txt file
                    using (StreamWriter sr = new StreamWriter(s.saveLocation + @"\amongUsCount.txt"))
                    {
                        for (int i = 0; i < (s.iNameStop - s.iName) / s.iNameStep + 1; i++)
                        {
                            sr.WriteLine(s.amongusCount[i]);
                        }
                    }
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} | Txt file generated at {s.saveLocation + @"\amongUsCount.txt"}");
                }

                //Output final informations to console
                processTime = DateTime.Now - startTime;
                Console.Write($"{DateTime.Now:HH:mm:ss.fff} | Task comlpeted in {processTime.ToString(@"mm\:ss\.fff")}");
                if (s.loadLocation.Contains("."))
                {
                    Console.Write($" and {s.amongusCount[0]} amongi were found!");
                }
                Console.WriteLine("\n-------------------------------------------------------------------------------------------\n");
            }

            //https://stackoverflow.com/questions/21599118/always-round-up-a-value-in-c-sharp
            double RoundUpValue(double value, int decimalpoint)
            {
                var result = Math.Round(value, decimalpoint);
                if (result < value) result += Math.Pow(10, -decimalpoint);
                return result;
            }
        }
    }
}