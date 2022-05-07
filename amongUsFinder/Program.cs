using System;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace amongUsFinder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SearchAmongus s = new SearchAmongus();
            Thread[] threads = new Thread[s.tcNormal];
            int[] threadShift = new int[s.tcNormal];
            StreamWriter swLog;
            TimeSpan processTime;

            Stopwatch sw = new Stopwatch();

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
                    string fileName = s.loadLocation.Substring(folderIndex + 1);
                    swLog = new StreamWriter(s.saveLocation + $@"\log_{fileName.Split('.')[0]}.txt");
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
                    swLog = new StreamWriter(s.saveLocation + @"\log.txt");
                }
                s.amongusCount = new int[s.roundUpDown((s.iNameStop - s.iName + 1) / s.iNameStep)];
                s.picturesProcessed = new int[s.tcNormal];

                outputText($"{DateTime.Now:HH:mm:ss.fff} | Started!");
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
                        outputText($"{DateTime.Now:HH:mm:ss.fff} | ({picturesProcessed}|{s.amongusCount.Length}) Progress: {Math.Round(progressState, 2)}% done");
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
                    outputText($"{DateTime.Now:HH:mm:ss.fff} | Processing pictures completed in {processTime.ToString(@"mm\:ss\.fff")} with an average of {roundUpValue(processTime.TotalSeconds / picturesProcessed, 3)}s per picture!");

                    //Rename processed files
                    if (s.iNameStep > 1)
                    {
                        int iNewName = 0;
                        for (int i = s.iName; i <= s.iNameStop; i += s.iNameStep)
                        {
                            iNewName++;
                            File.Move($@"{s.saveLocation}\{i:00000}.png", $@"{s.saveLocation}\{iNewName:00000}.png");
                        }
                        outputText($"{DateTime.Now:HH:mm:ss.fff} | Files renamed (from 00001 to {iNewName:00000})");
                    }
                    //Generate txt file
                    using (StreamWriter swNum = new StreamWriter(s.saveLocation + @"\amongUsCount.txt"))
                    {
                        for (int i = 0; i < s.amongusCount.Length; i++)
                        {
                            swNum.WriteLine(s.amongusCount[i]);
                        }
                    }
                    outputText($"{DateTime.Now:HH:mm:ss.fff} | Txt file generated at {s.saveLocation + @"\amongUsCount.txt"}");
                }

                //Output final informations to console
                processTime = DateTime.Now - startTime;
                outputText($"{DateTime.Now:HH:mm:ss.fff} | Task comlpeted in {processTime.ToString(@"mm\:ss\.fff")}");
                if (s.loadLocation.Contains("."))
                {
                    outputText($"{DateTime.Now:HH:mm:ss.fff} | {s.amongusCount[0]} amongi were found! ({s.loadLocation.Split('.')[0]}_searched.{s.loadLocation.Split('.')[1]})");
                }
                Console.WriteLine("\n-------------------------------------------------------------------------------------------\n");
                swLog.Close();
            }

            //https://stackoverflow.com/questions/21599118/always-round-up-a-value-in-c-sharp
            double roundUpValue(double value, int decimalpoint)
            {
                var result = Math.Round(value, decimalpoint);
                if (result < value) result += Math.Pow(10, -decimalpoint);
                return result;
            }

            void outputText(string output)
            {
                Console.WriteLine(output);
                swLog.WriteLine($"{DateTime.Now:dd.MM.yyyy}; " + output);
            }
        }
    }
}