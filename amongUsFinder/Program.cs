using System;
using System.Threading;

namespace amongUsFinder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SearchAmongus s = new SearchAmongus();
            while (true)
            {
                //Console.WriteLine(args.Length);
                //foreach (var item in args)
                //{
                //    Console.WriteLine(item);
                //}
                if (!s.initializeInputParameters()) continue;
                s.setStartTime();
                s.outputMessage("Task started!");
                if (s.loadLocation.Contains("."))
                {
                    s.processImage(s.loadLocation, $@"{s.loadLocation.Split('.')[0]}_searched.png");
                    s.outputMessage($"{s.amongusCount[0]} amongi were found! ({s.loadLocation.Split('.')[0]}_searched.{s.loadLocation.Split('.')[1]})");
                }
                else
                {
                    s.initializeThreads();
                    Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                    s.startMainThreads();
                    s.outputProgress();
                    s.waitMainThreads();
                    Thread.CurrentThread.Priority = ThreadPriority.Normal;
                    s.renamePictures();
                    s.generateTextFile();
                }
                s.outputMessage($@"Task comlpeted in {DateTime.Now - s.startTime:mm\:ss\.fff}" + " ---------------------------------------------------------------\n");
                s.swLogFile.Close();
                s.swLogFile.Dispose();
            }
        }
    }
}