﻿using System;
using Nancy.Hosting.Self;
using System.Diagnostics;

namespace MEAME2
{
  class MainClass
  {
    public static void Main (string[] args)
    {
      // Console.ForegroundColor = ConsoleColor.Cyan;
      // Console.WriteLine ("STARTING MEAME SERVER...");

      // var nancyHost = new NancyHost(new Uri("http://localhost:8888/"));

      // nancyHost.Start();

      // Console.ForegroundColor = ConsoleColor.Green;
      // Console.WriteLine("MEAME is now listenin'");
      // Console.ForegroundColor = ConsoleColor.Cyan;
      // Console.WriteLine("Press any key to (maybe, threads are hard yo) shut down\n\n");
      // Console.ResetColor();
      // Console.ReadKey();
      // nancyHost.Stop();
      // Console.WriteLine("Stopped, see ya!");

      Console.WriteLine("Auxilliary test method");

      DSPComms dspComms = new DSPComms();
      dspComms.init();
      dspComms.pingTest();

      Console.WriteLine("Done, press any key to exit");
      Console.ReadKey();

    }



    public static void notServer (string[] args){
      Console.WriteLine("Auxilliary test method");

      DSPComms dspComms = new DSPComms();
      dspComms.init();

      Console.WriteLine("Done, press any key to exit");
      Console.ReadKey();
    }
  }
}
