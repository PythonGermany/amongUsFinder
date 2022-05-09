using System;
using System.Drawing;
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
                if (s.loadLocation.Contains("."))
                {
                    //$"{ s.loadLocation.Split('.')[0]}_searched.{ s.loadLocation.Split('.')[1]}"
                    s.processImage(s.loadLocation, 0);
                    s.outputMessage($"{s.amongusCount[0]} amongi were found! ({s.loadLocation.Split('.')[0]}_searched.{s.loadLocation.Split('.')[1]})");
                }
                else
                {
                    s.initializeThreads();
                    s.startMainThreads();
                    Thread.CurrentThread.Priority = ThreadPriority.Normal;
                    s.outputProgress();
                    if (!s.checkSucessfulCompletion()) continue;
                    s.waitMainThreads();
                    s.renamePictures();
                    s.generateTextFile();
                }
                s.outputMessage($@"Task comlpeted in {DateTime.Now - s.startTime:mm\:ss\.fff}" + " --------------------------------\n");
                s.swLogFile.Close();
            }
        }
    }
}