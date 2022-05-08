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
                if (!s.initializeInputParameters()) continue;
                s.setStartTime();
                s.outputMessage("Task started!");
                s.initializeMainThreads();
                s.startMainThreads();
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;

                s.outputProgress();
                if (!s.checkSucessfulCompletion()) continue;
                s.waitMainThreads();
                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                if (!s.loadLocation.Contains("."))
                {
                   s.renamePictures();
                   s.generateTextFile();
                }

                if (s.loadLocation.Contains("."))
                {
                    s.outputMessage($"{s.amongusCount[0]} amongi were found! ({s.loadLocation.Split('.')[0]}_searched.{s.loadLocation.Split('.')[1]})");
                }
                s.outputMessage($@"Task comlpeted in {DateTime.Now - s.startTime:mm\:ss\.fff} --------------------------------\n");
                s.swLogFile.Close();
            }
        }
    }
}