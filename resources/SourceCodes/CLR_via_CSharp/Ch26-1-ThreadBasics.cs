using System;
using System.Threading;

public static class ThreadBasics {
   public static void Main() {
      FirstThread.Go();
      BackgroundDemo.Go(true);
      BackgroundDemo.Go(false);
   }
}

internal static class FirstThread {
   public static void Go() {
      Console.WriteLine("Main thread: starting a dedicated thread " +
         "to do an asynchronous operation");
      Thread dedicatedThread = new Thread(ComputeBoundOp);
      dedicatedThread.Start(5);

      Console.WriteLine("Main thread: Doing other work here...");
      Thread.Sleep(10000);     // Simulating other work (10 seconds)

      dedicatedThread.Join();  // Wait for thread to terminate
      Console.ReadLine();
   }

   // This method's signature must match the ParametizedThreadStart delegate
   private static void ComputeBoundOp(Object state) {
      // This method is executed by another thread

      Console.WriteLine("In ComputeBoundOp: state={0}", state);
      Thread.Sleep(1000);  // Simulates other work (1 second)

      // When this method returns, the dedicated thread dies
   }
}

internal static class BackgroundDemo {
   public static void Go(Boolean background) {
      // Create a new thread (defaults to Foreground)
      Thread t = new Thread(new ThreadStart(ThreadMethod));

      // Make the thread a background thread if desired
      if (background) t.IsBackground = true;

      t.Start(); // Start the thread
      return;   // NOTE: the application won't actually die for about 10 seconds
   }

   private static void ThreadMethod() {
      Thread.Sleep(10000); // Simulate 10 seconds of work
      Console.WriteLine("ThreadMethod is exiting");
   }
}