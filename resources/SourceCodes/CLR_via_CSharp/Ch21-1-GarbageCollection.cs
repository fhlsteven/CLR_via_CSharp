using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

public sealed class Progam {
   public static void Main() {
      Roots.Go();
      DebuggingRoots.Go();
      GCNotifications.Go();
      SafeHandleInterop.Go();
      GCBeepDemo.Go();
      CircularDependency.Go();
      FixedStatement.Go();
      MemoryPressureAndHandleCollector.Go();
      MemoryFailPointDemo.Go();
      GCMethods.Go();
      ConditionalWeakTableDemo.Go();

      // Fun note: array of Doubles that have 1000+ elements are put in 
      // the LOH because objects in the LOH are 8-byte aligned which 
      // improves perf for accessing large arrays of Doubles
      Console.WriteLine(GC.GetGeneration(new Double[999]));    // 0  
      Console.WriteLine(GC.GetGeneration(new Double[1000]));   // 2
   }
}

internal static class Roots {
   public static void Go() {
      TextWriter tw = new StringWriter();
      SomeType st = new SomeType(tw);

      // PrepareDelegate forces WriteBytes to be compiled now
      RuntimeHelpers.PrepareDelegate(new Action<Byte[]>(st.WriteBytes));

      // Launch the debugger now so I can step into WriteBytes and capture the 
      // native code via the Disassembly window. NOTE: Launching the debugger 
      // after compiling the code causes optimized code to be generated
      Debugger.Launch();

      st.WriteBytes(new Byte[] { 1, 2, 3 });
   }

   internal sealed class SomeType {
      private TextWriter m_textWriter;

      public SomeType(TextWriter tw) {
         m_textWriter = tw;
      }

      public void WriteBytes(Byte[] bytes) {
         for (Int32 x = 0; x < bytes.Length; x++) {
            m_textWriter.Write(bytes[x]);
         }
      }
   }
}

internal static class DebuggingRoots {
   public static void Go() {
      // Create a Timer object that knows to call our TimerCallback 
      // method once every 2000 milliseconds.
      var t = new System.Threading.Timer(TimerCallback, null, 0, 2000);

      // Wait for the user to hit <Enter>
      Console.ReadLine();

      // Refer to t after ReadLine (this gets optimized away)
      //t = null;

      // Refer to t after ReadLine (t will survive GCs until Dispose returns)
      //t.Dispose();
   }

   private static void TimerCallback(Object o) {
      // Display the date/time when this method got called.
      Console.WriteLine("In TimerCallback: " + DateTime.Now);

      // Force a garbage collection to occur for this demo.
      GC.Collect();
   }
}

internal static class GCBeepDemo {
   public static void Go() {
      // Register a callback method to be invoked whenever a GC occurs. 
      GCNotification.GCDone += g => Console.Beep(g == 0 ? 800 : 8000, 200);
      var l = new List<Object>();
      // Construct a lot of 100-byte objects.
      for (Int32 x = 0; x < 500000; x++) {
         Console.WriteLine(x);
         Byte[] b = new Byte[100];
         l.Add(b);
      }
   }

   public static class GCNotification {
      private static Action<Int32> s_gcDone = null; // The event’s field

      public static event Action<Int32> GCDone {
         add {
            // If there were no registered delegates 
            // before, start reporting notifications now                   
            if (s_gcDone == null) { new GenObject(0); new GenObject(2); }
            s_gcDone += value;
         }
         remove { s_gcDone -= value; }
      }

      private sealed class GenObject {
         private Int32 m_generation;
         public GenObject(Int32 generation) { m_generation = generation; }
         ~GenObject() { // This is the Finalize method
            // If this object is in the generation we want (or higher), 
            // notify the delegates that a GC just completed
            if (GC.GetGeneration(this) >= m_generation) {
               Action<Int32> temp = Volatile.Read(ref s_gcDone);
               if (temp != null) temp(m_generation);
            }

            // Keep reporting notifications if there is at least one delegate
            // registered, the AppDomain isn't unloading, and the process 
            // isn’t shutting down
            if ((s_gcDone != null) &&
               !AppDomain.CurrentDomain.IsFinalizingForUnload() &&
               !Environment.HasShutdownStarted) {
               // For Gen 0, create a new object; for Gen 2, resurrect the object 
               // & let the GC call Finalize again the next time Gen 2 is GC'd
               if (m_generation == 0) new GenObject(0);
               else GC.ReRegisterForFinalize(this);
            } else { /* Let the objects go away */ }
         }
      }
   }
}

internal static class GCNotifications {
   public static void Go() {
      GC.RegisterForFullGCNotification(1, 1);
      ThreadPool.QueueUserWorkItem(_ => {
         for (; ; ) {
            Console.WriteLine("Approach: " + GC.WaitForFullGCApproach(-1));
            Console.WriteLine("Complete: " + GC.WaitForFullGCComplete(-1));
         }
      });
      List<Object> lo = new List<Object>();
      for (int x = 0; x < 100000; x++) {
         lo.Add(new Byte[80000]);
      }
      Console.WriteLine(lo.Count);
      GC.CancelFullGCNotification();
   }

   private static void LowLatencyDemo() {
      GCLatencyMode oldMode = GCSettings.LatencyMode;
      RuntimeHelpers.PrepareConstrainedRegions();
      try {
         GCSettings.LatencyMode = GCLatencyMode.LowLatency;
         // Run your code here...
      }
      finally {
         GCSettings.LatencyMode = oldMode;
      }
   }
}

internal static class SafeHandleInterop {
   [DllImport("Kernel32", CharSet = CharSet.Unicode, EntryPoint = "CreateEvent")]
   // This prototype is not robust
   private static extern IntPtr CreateEventBad(IntPtr pSecurityAttributes, Boolean manualReset, Boolean initialState, String name);

   // This prototype is robust
   [DllImport("Kernel32", CharSet = CharSet.Unicode, EntryPoint = "CreateEvent")]
   private static extern SafeWaitHandle CreateEventGood(IntPtr pSecurityAttributes, Boolean manualReset, Boolean initialState, String name);

   // This prototype is robust and type-safe
   [DllImport("Kernel32")]
   private static extern Boolean SetEvent(SafeWaitHandle swh);

   public static void Go() {
      IntPtr handle = CreateEventBad(IntPtr.Zero, false, false, null);
      SafeWaitHandle swh = CreateEventGood(IntPtr.Zero, false, false, null);
   }
}

internal static class ConstructorThrowsAndFinalize {
   internal sealed class TempFileV1 {
      private String m_filename = null;
      private FileStream m_fs;

      public TempFileV1(String filename) {
         // The following line might throw an exception.
         m_fs = new FileStream(filename, FileMode.Create);

         // Save the name of this file.
         m_filename = filename;
      }

      ~TempFileV1() {  // This is the Finalize method
         // The right thing to do here is to test filename against null because 
         // you can't be sure that filename was initialized in the constructor.
         if (m_filename != null) File.Delete(m_filename);
      }
   }

   internal sealed class TempFileV2 {
      private String m_filename = null;
      private FileStream m_fs;

      public TempFileV2(String filename) {
         try {
            // The following line might throw an exception.
            m_fs = new FileStream(filename, FileMode.Create);

            // Save the name of this file.
            m_filename = filename;
         }
         catch {
            // If anything goes wrong, tell the garbage collector not to call the 
            // Finalize method. I’ll discuss SuppressFinalize later in this chapter.
            GC.SuppressFinalize(this);

            // Let the caller know something failed.
            throw;
         }
      }

      ~TempFileV2() {  // This is the Finalize method
         // No if statement now because this code executes only if the constructor tan successfully.
         File.Delete(m_filename);
      }
   }
}

internal sealed class DisposePattern {
   public static void Go1() {
      // Create the bytes to write to the temporary file.
      Byte[] bytesToWrite = new Byte[] { 1, 2, 3, 4, 5 };

      // Create the temporary file.
      FileStream fs = new FileStream("Temp.dat", FileMode.Create);

      // Write the bytes to the temporary file.
      fs.Write(bytesToWrite, 0, bytesToWrite.Length);

      // Explicitly close the file when done writing to it.
      ((IDisposable)fs).Dispose();

      // Delete the temporary file.
      File.Delete("Temp.dat");  // This always works now
   }

   public static void Go2() {
      // Create the bytes to write to the temporary file.
      Byte[] bytesToWrite = new Byte[] { 1, 2, 3, 4, 5 };

      // Create the temporary file.
      FileStream fs = new FileStream("Temp.dat", FileMode.Create);
      try {
         // Write the bytes to the temporary file.
         fs.Write(bytesToWrite, 0, bytesToWrite.Length);
      }
      finally {
         // Explicitly close the file when done writing to it.
         if (fs != null)
            ((IDisposable)fs).Dispose();
      }

      // Delete the temporary file.
      File.Delete("Temp.dat");  // This always works now.
   }
}

internal static class CircularDependency {
   public static void Go() {
      FileStream fs = new FileStream("DataFile.dat", FileMode.Create);
      StreamWriter sw = new StreamWriter(fs);
      sw.Write("Hi there");

      // The following call to Close is what you should do.
      sw.Close();
      // NOTE: StreamWriter.Close closes the FileStream; the FileStream doesn't have to be explicitly closed.
   }
}

internal static class FixedStatement {
   unsafe public static void Go() {
      // Allocate a bunch of objects that immediately become garbage
      for (Int32 x = 0; x < 10000; x++) new Object();

      IntPtr originalMemoryAddress;
      Byte[] bytes = new Byte[1000];   // Allocate this array after the garbage object

      // Get the address in memory of the Byte[]
      fixed (Byte* pbytes = bytes) { originalMemoryAddress = (IntPtr)pbytes; }

      // Force a collection; the garbage objects will go away & the Byte[] might be compacted
      GC.Collect();

      // Get the address in memory of the Byte[] now & compare it to the first address
      fixed (Byte* pbytes = bytes) {
         Console.WriteLine("The Byte[] did{0} move during the GC",
            (originalMemoryAddress == (IntPtr)pbytes) ? " not" : null);
      }
   }
}

internal static class MemoryPressureAndHandleCollector {
   public static void Go() {
      MemoryPressureDemo(0);                 // 0    causes infrequent GCs
      MemoryPressureDemo(10 * 1024 * 1024);  // 10MB causes frequent GCs

      HandleCollectorDemo();
   }

   private static void MemoryPressureDemo(Int32 size) {
      Console.WriteLine();
      Console.WriteLine("MemoryPressureDemo, size={0}", size);
      // Create a bunch of objects specifiying their logical size
      for (Int32 count = 0; count < 15; count++) {
         new BigNativeResource(size);
      }

      // For demo purposes, force everything to be cleaned-up
      GC.Collect();
      GC.WaitForPendingFinalizers();
   }

   private sealed class BigNativeResource {
      private Int32 m_size;

      public BigNativeResource(Int32 size) {
         m_size = size;
         if (m_size > 0) {
            // Make the GC think the object is physically bigger
            GC.AddMemoryPressure(m_size);
         }
         Console.WriteLine("BigNativeResource create.");
      }

      ~BigNativeResource() {
         if (m_size > 0) {
            // Make the GC think the object released more memory
            GC.RemoveMemoryPressure(m_size);
         }
         Console.WriteLine("BigNativeResource destroy.");
      }
   }


   private static void HandleCollectorDemo() {
      Console.WriteLine();
      Console.WriteLine("HandleCollectorDemo");
      for (Int32 count = 0; count < 10; count++) {
         new LimitedResource();
      }

      // For demo purposes, force everything to be cleaned-up
      GC.Collect();
      GC.WaitForPendingFinalizers();
   }

   private sealed class LimitedResource {
      // Create a HandleCollector telling it that collections should
      // occur when two or more of these objects exist in the heap
      private static HandleCollector s_hc = new HandleCollector("LimitedResource", 2);

      public LimitedResource() {
         // Tell HandleCollector that 1 more LimitResource object is in the heap
         s_hc.Add();
         Console.WriteLine("LimitedResource create.  Count={0}", s_hc.Count);
      }
      ~LimitedResource() {
         // Tell HandleCollector that 1 less LimitResource object is in the heap
         s_hc.Remove();
         Console.WriteLine("LimitedResource destroy. Count={0}", s_hc.Count);
      }
   }
}

internal static class MemoryFailPointDemo {
   public static void Go() {
      try {
         // Logically reserve 1.5GB of memory
         using (MemoryFailPoint mfp = new MemoryFailPoint(1500)) {
            // Perform memory-hungry algorithm in here
         }
      }
      catch (InsufficientMemoryException e) {
         // The memory could not be reserved
         Console.WriteLine(e);
      }
   }
}

internal static class GCMethods {
   public static void Go() {
      Console.WriteLine("Maximum generations: " + GC.MaxGeneration);

      // Create a new GenObj in the heap.
      Object o = new GenObj();

      // Because this object is newly created, it is in generation 0.
      Console.WriteLine("Gen " + GC.GetGeneration(o)); // 0

      // Performing a garbage collection promotes the object's generation.
      GC.Collect();
      Console.WriteLine("Gen " + GC.GetGeneration(o)); // 1

      GC.Collect();
      Console.WriteLine("Gen " + GC.GetGeneration(o)); // 2

      GC.Collect();
      Console.WriteLine("Gen " + GC.GetGeneration(o)); // 2 (max)


      o = null; // Destroy the strong reference to this object.

      Console.WriteLine("Collecting Gen 0");
      GC.Collect(0);                    // Collect generation 0.
      GC.WaitForPendingFinalizers();    // Finalize is NOT called.

      Console.WriteLine("Collecting Gen 1");
      GC.Collect(1);                    // Collect generation 1.
      GC.WaitForPendingFinalizers();    // Finalize is NOT called.

      Console.WriteLine("Collecting Gen 2");
      GC.Collect(2);                    // Same as Collect()
      GC.WaitForPendingFinalizers();    // Finalize IS called.
   }

   internal sealed class GenObj {
      ~GenObj() {
         Console.WriteLine("In Finalize method");
      }
   }
}

#region ConditionalWeakTableDemo
internal static class ConditionalWeakTableDemo {
   public static void Go() {
      Object o = new Object().GCWatch("My Object created at " + DateTime.Now);
      GC.Collect();     // We will not see the GC notification here
      GC.KeepAlive(o);  // Make sure the object o refers to lives up to here
      o = null;         // The object that o refers to can die now

      GC.Collect();     // We'll see the GC notification here
      Console.ReadLine();
   }
}

internal static class GCWatcher {
   // NOTE: Be careful with Strings due to interning and MarshalByRefObject proxy objects
   private readonly static ConditionalWeakTable<Object, NotifyWhenGCd<String>> s_cwt =
      new ConditionalWeakTable<Object, NotifyWhenGCd<String>>();

   private sealed class NotifyWhenGCd<T> {
      private readonly T m_value;

      internal NotifyWhenGCd(T value) { m_value = value; }
      public override string ToString() { return m_value.ToString(); }
      ~NotifyWhenGCd() { Console.WriteLine("GC'd: " + m_value); }
   }

   public static T GCWatch<T>(this T @object, String tag) where T : class {
      s_cwt.Add(@object, new NotifyWhenGCd<String>(tag));
      return @object;
   }
}
#endregion

