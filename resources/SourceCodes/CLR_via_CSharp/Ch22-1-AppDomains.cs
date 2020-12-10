using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Threading;

public sealed class Program {
   public static void Main() {
      //Marshalling();
      FieldAccessTiming();
      AppDomainResourceMonitoring();
      UnloadTimeout.Go();
   }

   private static void Marshalling() {
      // Get a reference to the AppDomain that that calling thread is executing in
      AppDomain adCallingThreadDomain = Thread.GetDomain();

      // Every AppDomain is assigned a friendly string name (helpful for debugging)
      // Get this AppDomain's friendly string name and display it
      String callingDomainName = adCallingThreadDomain.FriendlyName;
      Console.WriteLine("Default AppDomain's friendly name={0}", callingDomainName);

      // Get & display the assembly in our AppDomain that contains the 'Main' method
      String exeAssembly = Assembly.GetEntryAssembly().FullName;
      Console.WriteLine("Main assembly={0}", exeAssembly);

      // Define a local variable that can refer to an AppDomain
      AppDomain ad2 = null;

      // *** DEMO 1: Cross-AppDomain Communication using Marshal-by-Reference
      Console.WriteLine("{0}Demo #1", Environment.NewLine);

      // Create new AppDomain (security & configuration match current AppDomain)
      ad2 = AppDomain.CreateDomain("AD #2", null, null);
      MarshalByRefType mbrt = null;

      // Load our assembly into the new AppDomain, construct an object, marshal 
      // it back to our AD (we really get a reference to a proxy)
      mbrt = (MarshalByRefType)
         ad2.CreateInstanceAndUnwrap(exeAssembly, "MarshalByRefType");

      Console.WriteLine("Type={0}", mbrt.GetType());  // The CLR lies about the type

      // Prove that we got a reference to a proxy object
      Console.WriteLine("Is proxy={0}", RemotingServices.IsTransparentProxy(mbrt));

      // This looks like we're calling a method on MarshalByRefType but, we're not.
      // We're calling a method on the proxy type. The proxy transitions the thread
      // to the AppDomain owning the object and calls this method on the real object.
      mbrt.SomeMethod();

      // Unload the new AppDomain
      AppDomain.Unload(ad2);
      // mbrt refers to a valid proxy object; the proxy object refers to an invalid AppDomain

      try {
         // We're calling a method on the proxy type. The AD is invalid, exception is thrown
         mbrt.SomeMethod();
         Console.WriteLine("Successful call.");
      }
      catch (AppDomainUnloadedException) {
         Console.WriteLine("Failed call.");
      }


      // *** DEMO 2: Cross-AppDomain Communication using Marshal-by-Value
      Console.WriteLine("{0}Demo #2", Environment.NewLine);

      // Create new AppDomain (security & configuration match current AppDomain)
      ad2 = AppDomain.CreateDomain("AD #2", null, null);

      // Load our assembly into the new AppDomain, construct an object, marshal 
      // it back to our AD (we really get a reference to a proxy)
      mbrt = (MarshalByRefType)
         ad2.CreateInstanceAndUnwrap(exeAssembly, "MarshalByRefType");

      // The object's method returns a COPY of the returned object; 
      // the object is marshaled by value (not be reference).
      MarshalByValType mbvt = mbrt.MethodWithReturn();

      // Prove that we did NOT get a reference to a proxy object
      Console.WriteLine("Is proxy={0}", RemotingServices.IsTransparentProxy(mbvt));

      // This looks like we're calling a method on MarshalByValType and we are.
      Console.WriteLine("Returned object created " + mbvt.ToString());

      // Unload the new AppDomain
      AppDomain.Unload(ad2);
      // mbvt refers to valid object; unloading the AppDomain has no impact.

      try {
         // We're calling a method on an object; no exception is thrown
         Console.WriteLine("Returned object created " + mbvt.ToString());
         Console.WriteLine("Successful call.");
      }
      catch (AppDomainUnloadedException) {
         Console.WriteLine("Failed call.");
      }


      // DEMO 3: Cross-AppDomain Communication using non-marshalable type
      Console.WriteLine("{0}Demo #3", Environment.NewLine);

      // Create new AppDomain (security & configuration match current AppDomain)
      ad2 = AppDomain.CreateDomain("AD #2", null, null);

      // Load our assembly into the new AppDomain, construct an object, marshal 
      // it back to our AD (we really get a reference to a proxy)
      mbrt = (MarshalByRefType)
         ad2.CreateInstanceAndUnwrap(exeAssembly, "MarshalByRefType");

      // The object's method returns an non-marshalable object; exception
      NonMarshalableType nmt = mbrt.MethodArgAndReturn(callingDomainName);
      // We won't get here...
   }

   private sealed class MBRO : MarshalByRefObject { public Int32 x; }
   private sealed class NonMBRO : Object { public Int32 x; }

   private static void FieldAccessTiming() {
      const Int32 count = 100000000;
      NonMBRO nonMbro = new NonMBRO();
      MBRO mbro = new MBRO();

      Stopwatch sw = Stopwatch.StartNew();
      for (Int32 c = 0; c < count; c++) nonMbro.x++;
      Console.WriteLine("{0}", sw.Elapsed);

      sw = Stopwatch.StartNew();
      for (Int32 c = 0; c < count; c++) mbro.x++;
      Console.WriteLine("{0}", sw.Elapsed);
   }

   private sealed class AppDomainMonitorDelta : IDisposable {
      private AppDomain m_appDomain;
      private TimeSpan m_thisADCpu;
      private Int64 m_thisADMemoryInUse;
      private Int64 m_thisADMemoryAllocated;

      static AppDomainMonitorDelta() {
         // Make sure that AppDomain monitoring is turned on
         AppDomain.MonitoringIsEnabled = true;
      }

      public AppDomainMonitorDelta(AppDomain ad) {
         m_appDomain = ad ?? AppDomain.CurrentDomain;
         m_thisADCpu = m_appDomain.MonitoringTotalProcessorTime;
         m_thisADMemoryInUse = m_appDomain.MonitoringSurvivedMemorySize;
         m_thisADMemoryAllocated = m_appDomain.MonitoringTotalAllocatedMemorySize;
      }

      public void Dispose() {
         GC.Collect();
         Console.WriteLine("FriendlyName={0}, CPU={1}ms",
            m_appDomain.FriendlyName,
            (m_appDomain.MonitoringTotalProcessorTime - m_thisADCpu).TotalMilliseconds);

         Console.WriteLine("   Allocated {0:N0} bytes of which {1:N0} survived GCs",
            m_appDomain.MonitoringTotalAllocatedMemorySize - m_thisADMemoryAllocated,
            m_appDomain.MonitoringSurvivedMemorySize - m_thisADMemoryInUse);
      }
   }

   private static void AppDomainResourceMonitoring() {
      using (new AppDomainMonitorDelta(null)) {
         // Allocate about 10 million bytes that will survive collections
         var list = new List<Object>();
         for (Int32 x = 0; x < 1000; x++) list.Add(new Byte[10000]);

         // Allocate about 20 million bytes that will NOT survive collections
         for (Int32 x = 0; x < 2000; x++) new Byte[10000].GetType();

         // Spin the CPU for about 5 seconds
         Int64 stop = Environment.TickCount + 5000;
         while (Environment.TickCount < stop) ;
      }
   }

   private static class UnloadTimeout {
      private static Int32 s_testCode = 0;   // 0=InfiniteLoop, 1=ManagedSleep, 2=UnmanagedSleep
      private static AppDomain s_ad;

      public static void Go() {
         // Create an AppDomain
         s_ad = AppDomain.CreateDomain("AD #2", null, null);

         // Spawn thread to enter the other AppDomain
         Thread t = new Thread((ThreadStart)delegate { s_ad.DoCallBack(Loop); });
         t.Start();
         Thread.Sleep(5000);  // The other thread a chance to run

         Stopwatch sw = null;
         try {
            // Time how long it takes to unload the AppDomain
            Console.WriteLine("Calling unload");
            sw = Stopwatch.StartNew();
            AppDomain.Unload(s_ad);
         }
         catch (Exception e) {
            Console.WriteLine(e.ToString());
         }
         Console.WriteLine("Unload returned after {0}", sw.Elapsed);
         Console.ReadLine();
      }

      private static void Loop() {
         try {
            switch (s_testCode) {
               case 0: while (true) ;              // Infinite loop
               case 1: Thread.Sleep(10000); break;	// Managed sleep
               case 2: Sleep(10000); break;		   // Unmanaged sleep
            }
         }
         catch (ThreadAbortException) {
            Console.WriteLine("Caught ThreadAbortException: Hit return to exit");
            Console.ReadLine();
         }
      }

      [DllImport("Kernel32")]
      private static extern void Sleep(UInt32 ms);
   }
}


// Instances can be marshaled-by-reference across AppDomain boundaries
public sealed class MarshalByRefType : MarshalByRefObject {
   public MarshalByRefType() {
      Console.WriteLine("{0} ctor running in {1}",
         this.GetType().ToString(), Thread.GetDomain().FriendlyName);
   }

   public void SomeMethod() {
      Console.WriteLine("Executing in " + Thread.GetDomain().FriendlyName);
   }

   public MarshalByValType MethodWithReturn() {
      Console.WriteLine("Executing in " + Thread.GetDomain().FriendlyName);
      MarshalByValType t = new MarshalByValType();
      return t;
   }

   public NonMarshalableType MethodArgAndReturn(String callingDomainName) {
      // NOTE: callingDomainName is [Serializable]
      Console.WriteLine("Calling from '{0}' to '{1}'.",
         callingDomainName, Thread.GetDomain().FriendlyName);
      NonMarshalableType t = new NonMarshalableType();
      return t;
   }

   [DebuggerStepThrough]
   public override Object InitializeLifetimeService() {
      return null;   // We want an infinite lifetime
   }
}


// Instances can be marshaled-by-value across AppDomain boundaries
[Serializable]
public sealed class MarshalByValType : Object {
   private DateTime m_creationTime = DateTime.Now; // NOTE: DateTime is [Serializable]

   public MarshalByValType() {
      Console.WriteLine("{0} ctor running in {1}, Created on {2:D}",
         this.GetType().ToString(),
         Thread.GetDomain().FriendlyName,
         m_creationTime);
   }

   public override String ToString() {
      return m_creationTime.ToLongDateString();
   }
}

// Instances cannot be marshaled across AppDomain boundaries
// [Serializable]
public sealed class NonMarshalableType : Object {
   public NonMarshalableType() {
      Console.WriteLine("Executing in " + Thread.GetDomain().FriendlyName);
   }
}