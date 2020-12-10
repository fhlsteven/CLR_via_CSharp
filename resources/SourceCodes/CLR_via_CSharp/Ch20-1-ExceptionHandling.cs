/******************************************************************************
Module:  Ch20-1-ExceptionHandling.cs
Notices: Copyright (c) 2013 Jeffrey Richter
******************************************************************************/

#if !DEBUG
#pragma warning disable 1058
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;

public static class Program {
   public static void Main() {
      Mechanics.SomeMethod();
      GenericException.Go();
      OneStatementDemo.Go();
      CodeContracts.Go();
      UnhandledException.Go();
      ConstrainedExecutionRegion.Go();
   }
}

internal static class Mechanics {
   public static void SomeMethod() {
      try {
         // Put code requiring graceful recovery and/or cleanup operations here...
      }
      catch (InvalidOperationException) {
         // Put code that recovers from an InvalidOperationException here...
      }
      catch (IOException) {
         // Put code that recovers from an IOException here...
      }
      catch (Exception) {
         // Before C# 2.0, this block catches CLS-compliant exceptions only 
         // In C# 2.0, this block catches CLS- & non-CLS- compliant exceptions 
         throw; // Re-throws whatever got caught 
      }
      catch {
         // In all versions of C#, this block catches CLS- & non-CLS- compliant exceptions 
         throw; // Re-throws whatever got caught 
      }
      finally {
         // Put code that cleans up any operations started within the try block here...
         // The code in this block ALWAYS executes, regardless of whether an exception is thrown. 
      }
      // Code below the finally block executes if no exception is thrown within the try block
      // or if a catch block catches the exception and doesn't throw or re-throw an exception. 
   }
}

internal static class GenericException {
   public static void Go() {
      try {
         throw new Exception<DiskFullExceptionArgs>(new DiskFullExceptionArgs(@"C:\"), "The disk is full");
      }
      catch (InvalidOperationException e) {// Exception<DiskFullExceptionArgs> e) {
         Console.WriteLine(e.Message);
      }

      // Verify that the exception is serializable
      using (var stream = new MemoryStream()) {
         var e = new Exception<DiskFullExceptionArgs>(new DiskFullExceptionArgs(@"C:\"), "The disk is full");
         var formatter = new BinaryFormatter();
         formatter.Serialize(stream, e);
         stream.Position = 0;
         e = (Exception<DiskFullExceptionArgs>)formatter.Deserialize(stream);
         Console.WriteLine(e.Message);
      }
   }

   [Serializable]
   private sealed class DiskFullExceptionArgs : ExceptionArgs {
      private readonly String m_diskpath; // private field set at construction time

      public DiskFullExceptionArgs(String diskpath) { m_diskpath = diskpath; }

      // Public read-only property that returns the field
      public String DiskPath { get { return m_diskpath; } }

      // Override the Message property to include our field (if set)
      public override String Message {
         get {
            return (m_diskpath == null) ? base.Message : "DiskPath=" + m_diskpath;
         }
      }
   }

   /// <summary>Represents errors that occur during application execution.</summary>
   /// <typeparam name="TExceptionArgs">The type of exception and any additional arguments associated with it.</typeparam>
   [Serializable]
   public sealed class Exception<TExceptionArgs> : Exception, ISerializable where TExceptionArgs : ExceptionArgs {
      private const String c_args = "Args";     // For (de)serialization
      private readonly TExceptionArgs m_args;

      /// <summary>Returns a reference to this exception's additional arguments.</summary>
      public TExceptionArgs Args { get { return m_args; } }

      /// <summary>
      /// Initializes a new instance of the Exception class with a specified error message 
      /// and a reference to the inner exception that is the cause of this exception. 
      /// </summary>
      /// <param name="message">The error message that explains the reason for the exception.</param>
      /// <param name="innerException">The exception that is the cause of the current exception, 
      /// or a null reference if no inner exception is specified.</param>
      public Exception(String message = null, Exception innerException = null)
         : this(null, message, innerException) { }

      // The fourth public constructor because there is a field
      /// <summary>
      /// Initializes a new instance of the Exception class with additional arguments, 
      /// a specified error message, and a reference to the inner exception 
      /// that is the cause of this exception. 
      /// </summary>
      /// <param name="args">The exception's additional arguments.</param>
      /// <param name="message">The error message that explains the reason for the exception.</param>
      /// <param name="innerException">The exception that is the cause of the current exception, 
      /// or a null reference if no inner exception is specified.</param>
      public Exception(TExceptionArgs args, String message = null, Exception innerException = null)
         : base(message, innerException) {
         m_args = args;
      }

      // Because at least 1 field is defined, define the special deserialization constructor
      // Since this class is sealed, this constructor is private
      // If this class were not sealed, this constructor should be protected
      [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
      private Exception(SerializationInfo info, StreamingContext context)
         : base(info, context) { // Let the base deserialize its fields
         m_args = (TExceptionArgs)info.GetValue(c_args, typeof(TExceptionArgs));
      }

      // Because at least 1 field is defined, define the serialization method
      /// <summary>
      /// When overridden in a derived class, sets the SerializationInfo with information about the exception.
      /// </summary>
      /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
      /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
      [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
      public override void GetObjectData(SerializationInfo info, StreamingContext context) {
         info.AddValue(c_args, m_args);
         base.GetObjectData(info, context);
      }

      /// <summary>Gets a message that describes the current exception.</summary>
      public override String Message {
         get {
            String baseMsg = base.Message;
            return (m_args == null) ? baseMsg : baseMsg + " (" + m_args.Message + ")";
         }
      }

      /// <summary>
      /// Determines whether the specified Object is equal to the current Object.
      /// </summary>
      /// <param name="obj">The Object to compare with the current Object. </param>
      /// <returns>true if the specified Object is equal to the current Object; otherwise, false.</returns>
      public override Boolean Equals(Object obj) {
         Exception<TExceptionArgs> other = obj as Exception<TExceptionArgs>;
         if (other == null) return false;
         return Object.Equals(m_args, other.m_args) && base.Equals(obj);
      }
      public override int GetHashCode() { return base.GetHashCode(); }
   }

   /// <summary>
   /// A base class that a custom exception would derive from in order to add its own exception arguments.
   /// </summary>
   [Serializable]
   public abstract class ExceptionArgs {
      /// <summary>The string message associated with this exception.</summary>
      public virtual String Message { get { return String.Empty; } }
   }
}

internal static class OneStatementDemo {
   public static void Go() {
      var o = OneStatement(new MemoryStream(), 'X');
   }
   private static Object OneStatement(Stream stream, Char charToFind) {
      return (charToFind + ": " + stream.GetType() + String.Empty + (512M + stream.Position))
         .Where(c => c == charToFind).ToArray();
   }

   private static Object s_myLockObject = new Object();
   private static void MonitorWithStateCorruption() {
      Monitor.Enter(s_myLockObject);  // If this throws, did the lock get taken or not? 
      // If it did, then it won’t get released!
      try {
         // Do thread-safe operation here...
      }
      finally {
         Monitor.Exit(s_myLockObject);
      }
   }

   private static void MonitorWithoutStateCorruption() {
      Boolean lockTaken = false;  // Assume the lock was not taken
      try {
         // This works whether an exception is thrown or not! 
         Monitor.Enter(s_myLockObject, ref lockTaken);

         // Do thread-safe operation here...
      }
      finally {
         // If the lock was taken, release it
         if (lockTaken) Monitor.Exit(s_myLockObject);
      }
   }
}

internal sealed class UnhandledException {
   public static void Go() {
      //var em = SetErrorMode(ErrorMode.NoGPFaultErrorBox);
      int x = 0;
      x = 100 / x;
      RecurseWithStackCheck();

      Console.Write("Install UE Handler (Y/N)?");
      ConsoleKeyInfo key = Console.ReadKey(false);
      Console.WriteLine();
      Console.Write("UE Handler installed=");
      if (Char.ToUpper(key.KeyChar) == 'Y') {
         // Register our MgdUEFilter callback method with the AppDomain
         // so that it gets called when an unhandled exception occurs.
         AppDomain.CurrentDomain.UnhandledException +=
            new UnhandledExceptionEventHandler(MgdUEFilter);
         Console.WriteLine(true);
      } else Console.WriteLine(false);

      while (true) {
         Console.WriteLine("What kind of UE do you want to force?");
         Console.Write("1=Main, 2=ThreadPool, 3=Manual, 4=Finalizer, 5=Native, 6=WindowsFormWindowProc?");
         key = Console.ReadKey(false);
         Console.WriteLine();

         switch (key.KeyChar) {
            case '1':
               Throw("Main");
               break;

            case '2':
               ThreadPool.QueueUserWorkItem(delegate { Throw("ThreadPool"); });
               break;

            case '3':
               Thread t = new Thread((ThreadStart)delegate { Throw("Manual"); });
               t.Start();
               break;

            case '4':
               new UnhandledException();
               GC.Collect();
               break;

            case '5':
               new NativeThread();
               break;

            case '6':
               Application.Run(new MyForm());
               break;
         }
         Console.ReadLine();
      }
   }

   public enum ErrorMode {
      None = 0x0000,
      FailCriticalErrors = 0x0001,
      NoAlignmentFaultExcept = 0004,
      NoGPFaultErrorBox = 0x0002,
      NoOpenFileErrorBox = 0x8000
   }
   [DllImport("Kernel32", SetLastError = true, ExactSpelling = true)]
   private static extern ErrorMode SetErrorMode(ErrorMode mode);

   private static void RecurseWithStackCheck() {
      try {
         RuntimeHelpers.EnsureSufficientExecutionStack();
         RecurseWithStackCheck();
      }
      catch (Exception e) {
         Console.WriteLine(e.Message);
      }
   }

   private static void Throw(String threadType) {
      throw new InvalidOperationException(
         String.Format("Forced exception in {0} thread.", threadType));
   }

   ~UnhandledException() {
      Throw("Finalize");
   }

   private sealed class MyForm : Form {
      protected override void OnPaint(PaintEventArgs e) {
         Throw("Windows Form Window Procedure");
      }
   }


   private static void MgdUEFilter(Object sender, UnhandledExceptionEventArgs e) {
      // This string contains the info to display or log
      String info;
      Console.WriteLine("MgdUEFilter");

      // Initialize the contents of the string
      Exception ex = e.ExceptionObject as Exception;
      if (ex != null) {
         // An unhandled CLS-Compliant exception was thrown
         // Do whatever: you can access the fields of Exception
         // (Message, StackTrace, HelpLink, InnerException, etc.)
         info = ex.ToString();
      } else {
         // An unhandled non-CLS-Compliant exception was thrown
         // Do whatever: all you can call are the methods defined by Object
         // (ToString, GetType, etc.)
         info = String.Format("Non-CLS-Compliant exception: Type={0}, String={1}",
            e.ExceptionObject.GetType(), e.ExceptionObject.ToString());
      }

#if DEBUG
      // For DEBUG builds of the application, launch the debugger
      // to understand what happened and to fix it.
      if (!e.IsTerminating) {
         // Unhandled exception occurred in a thread pool or finalizer thread
         Debugger.Launch();
      } else {
         // Unhandled exception occurred in a managed thread
         // By default, the CLR will automatically attach a debugger but
         // we can force it with the line below:
         Debugger.Launch();
      }
#else
      // For RELEASE builds of the application, display or log the exception
      // so that the end-user can report it back to us.
      if (!e.IsTerminating) {
         // Unhandled exception occurred in a thread pool or finalizer thread
         // For thread pool or finalizer threads, you might just log the exception
         // and not display the problem to the user. However, each application
         // should do whatever makes the most sense.
      } else {
         // Unhandled exception occurred in a managed thread
         // The CLR is going to kill the application, you should display and/or 
         // log the exception.
      }
#endif

      Console.WriteLine("Catching an unhandled Exception:");
      Console.WriteLine(e.ExceptionObject);
      Console.WriteLine("IsTerminating: " + e.IsTerminating);
      Console.ReadLine();
      //if (!Debugger.IsAttached) Debugger.Launch();
   }

   private sealed class NativeThread {
      public static void SetUnhandledExceptionFilter() {
         SetUnhandledExceptionFilter(TLEF);
      }

      private delegate ExceptionFilterDisposition TopLevelExceptionFilter(UIntPtr exceptionPointers);
      private enum ExceptionFilterDisposition {
         ExecuteHandler = 1,
         ContinueSearch = 0,
         ContinueExecution = -1
      }
      [DllImport("Kernel32")]
      private static extern UIntPtr SetUnhandledExceptionFilter(TopLevelExceptionFilter filter);
      private static ExceptionFilterDisposition TLEF(UIntPtr ep) {
         Console.WriteLine("TopLevelExceptionFilter");
         return ExceptionFilterDisposition.ContinueSearch;
      }


      public NativeThread() {
         UInt32 threadId;
         IntPtr hThread = CreateThread(IntPtr.Zero, 0,
            new ThreadStartRoutine(UnmgdThreadFunc), UIntPtr.Zero, 0, out threadId);
         CloseHandle(hThread);
      }

      public delegate void ThreadStartRoutine(UIntPtr ThreadParameter);

      [DllImport("Kernel32")]
      public static extern IntPtr CreateThread(IntPtr SecurityAttributes, UInt32 StackSize,
         ThreadStartRoutine StartFunction, UIntPtr ThreadParameter,
         UInt32 CreationFlags, out UInt32 ThreadId);

      [DllImport("Kernel32")]
      private static extern Boolean CloseHandle(IntPtr handle);

      private static void UnmgdThreadFunc(UIntPtr p) {
         Console.WriteLine("In UnmgdThreadFunc");
         Int32 x = 0;
         x = 10 / x;
      }
   }
}

internal static class ConstrainedExecutionRegion {
   public static void Go() {
      ExecuteCodeWithGuaranteedCleanupDemo(false);
      ExecuteCodeWithGuaranteedCleanupDemo(true);
      Demo1();
      Demo2();
   }

   private static void Demo1() {
      Console.WriteLine("In Demo1");
      try {
         Console.WriteLine("In try");
      }
      finally {
         // Type1’s static constructor is implicitly called in here
         Type1.M();
      }
   }

   private sealed class Type1 {
      static Type1() {
         // if this throws an exception, M won’t get called
         Console.WriteLine("Type1's static ctor called");
      }

      public static void M() { }
   }

   private static void Demo2() {
      Console.WriteLine("In Demo2");
      // Force the code in the finally to be eagerly prepared
      RuntimeHelpers.PrepareConstrainedRegions();  // In the System.Runtime.CompilerServices namespace
      try {
         Console.WriteLine("In try");
      }
      finally {
         // Type2’s static constructor is implicitly called in here
         Type2.M();
      }
   }

   public class Type2 {
      static Type2() {
         Console.WriteLine("Type2's static ctor called");
      }

      [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
      public static void M() { }
   }

   private static readonly Object s_myLock = new Object();
   private static void ExecuteCodeWithGuaranteedCleanupDemo(Boolean test) {
      Thread t = new Thread(ThreadFunc);
      t.Start(test);
      Thread.Sleep(2000);
      t.Abort();
      ThreadFunc(false); // Deadlock
   }

   private static void ThreadFunc(Object o) {
      Boolean taken = true;
      if ((Boolean)o) {
         Monitor.Enter(s_myLock/*, ref taken*/);
         Thread.Sleep(10000);
         if (taken) Monitor.Exit(s_myLock);
      } else {
         RuntimeHelpers.ExecuteCodeWithGuaranteedCleanup(
            userData => { Monitor.Enter(s_myLock/*, ref taken*/); Thread.Sleep(10000); },
            (userData, exceptionThrown) => { if (taken) Monitor.Exit(s_myLock); }, null);
      }
   }
}

internal static class CodeContracts {
   public static void Go() {
      var shoppingCart = new ShoppingCart();
      shoppingCart.AddItem(new Item());
   }

   public sealed class Item { /* ... */ }

   public sealed class ShoppingCart {
      private List<Item> m_cart = new List<Item>();
      private Decimal m_totalCost = 0;

      public ShoppingCart() {
      }

      public void AddItem(Item item) {
         AddItemHelper(m_cart, item, ref m_totalCost);
      }

      private static void AddItemHelper(List<Item> m_cart, Item newItem, ref Decimal totalCost) {
         // Preconditions: 
         Contract.Requires(newItem != null);
         Contract.Requires(Contract.ForAll(m_cart, s => s != newItem));

         // Postconditions:
         Contract.Ensures(Contract.Exists(m_cart, s => s == newItem));
         Contract.Ensures(totalCost >= Contract.OldValue(totalCost));
         Contract.EnsuresOnThrow<IOException>(totalCost == Contract.OldValue(totalCost));

         // Do some stuff (which could throw an IOException)...
         m_cart.Add(newItem);
         totalCost += 1.00M;
         //throw new IOException(); // Prove contract violation
      }

      // Object invariant
      [ContractInvariantMethod]
      private void ObjectInvariant() {
         Contract.Invariant(m_totalCost >= 0);
      }
   }
}

//////////////////////////////// End of File //////////////////////////////////
