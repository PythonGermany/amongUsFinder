﻿using System;
using System.Threading;

namespace amongUsFinder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SearchAmongus s = new SearchAmongus();
            if (args.Length > 0)
            {
                if(!s.initializeInputParameters(args))
                    return;
                s.setStartTime();
                s.outputMessage("Task started!", true);
                if (s.loadLocation.Contains("."))
                {
                    s.processImage(s.loadLocation, $@"{s.loadLocation.Split('.')[0]}_searched.png");
                    s.outputMessage($"{s.amongusCount[0]} amongi were found! ({s.loadLocation.Split('.')[0]}_searched.{s.loadLocation.Split('.')[1]})", true);
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
                    s.generateStatisticImage();
                }
                s.outputMessage($@"Task comlpeted in {DateTime.Now - s.startTime:mm\:ss\.fff}", true);
                s.swLogFile.Close();
                s.swLogFile.Dispose();
            }
        }
    }
}