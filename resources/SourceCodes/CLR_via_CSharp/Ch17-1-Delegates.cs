using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;

public sealed class Program {
   public static void Main() {
      DelegateIntro.Go();
      GetInvocationList.Go();
      AnonymousMethods.Go();
      DelegateReflection.Go("TwoInt32s", "Add", "123", "321");
      DelegateReflection.Go("TwoInt32s", "Subtract", "123", "321");
      DelegateReflection.Go("OneString", "NumChars", "Hello there");
      DelegateReflection.Go("OneString", "Reverse", "Hello there");
   }
}


internal sealed class DelegateIntro {
   // Declare a delegate type; instances refer to a method that
   // takes an Int32 parameter and returns void.
   internal delegate void Feedback(Int32 value);

   public static void Go() {
      StaticDelegateDemo();
      InstanceDelegateDemo();
      ChainDelegateDemo1(new DelegateIntro());
      ChainDelegateDemo2(new DelegateIntro());
   }

   private static void StaticDelegateDemo() {
      Console.WriteLine("----- Static Delegate Demo -----");
      Counter(1, 3, null);
      Counter(1, 3, new Feedback(DelegateIntro.FeedbackToConsole));
      Counter(1, 3, new Feedback(FeedbackToMsgBox)); // "Program." is optional
      Console.WriteLine();
   }

   private static void InstanceDelegateDemo() {
      Console.WriteLine("----- Instance Delegate Demo -----");
      DelegateIntro di = new DelegateIntro();
      Counter(1, 3, new Feedback(di.FeedbackToFile));

      Console.WriteLine();
   }

   private static void ChainDelegateDemo1(DelegateIntro di) {
      Console.WriteLine("----- Chain Delegate Demo 1 -----");
      Feedback fb1 = new Feedback(FeedbackToConsole);
      Feedback fb2 = new Feedback(FeedbackToMsgBox);
      Feedback fb3 = new Feedback(di.FeedbackToFile);

      Feedback fbChain = null;
      fbChain = (Feedback)Delegate.Combine(fbChain, fb1);
      fbChain = (Feedback)Delegate.Combine(fbChain, fb2);
      fbChain = (Feedback)Delegate.Combine(fbChain, fb3);
      Counter(1, 2, fbChain);

      Console.WriteLine();
      fbChain = (Feedback)Delegate.Remove(fbChain, new Feedback(FeedbackToMsgBox));
      Counter(1, 2, fbChain);
   }

   private static void ChainDelegateDemo2(DelegateIntro di) {
      Console.WriteLine("----- Chain Delegate Demo 2 -----");
      Feedback fb1 = new Feedback(FeedbackToConsole);
      Feedback fb2 = new Feedback(FeedbackToMsgBox);
      Feedback fb3 = new Feedback(di.FeedbackToFile);

      Feedback fbChain = null;
      fbChain += fb1;
      fbChain += fb2;
      fbChain += fb3;
      Counter(1, 2, fbChain);

      Console.WriteLine();
      fbChain -= new Feedback(FeedbackToMsgBox);
      Counter(1, 2, fbChain);
   }

   private static void Counter(Int32 from, Int32 to, Feedback fb) {
      for (Int32 val = from; val <= to; val++) {
         // If any callbacks are specified, call them
         if (fb != null)
            fb(val);
      }
   }

   private static void FeedbackToConsole(Int32 value) {
      Console.WriteLine("Item=" + value);
   }

   private static void FeedbackToMsgBox(Int32 value) {
      MessageBox.Show("Item=" + value);
   }

   private void FeedbackToFile(Int32 value) {
      StreamWriter sw = new StreamWriter("Status", true);
      sw.WriteLine("Item=" + value);
      sw.Close();
   }
}

internal static class GetInvocationList {
   // Define a Light component.
   internal sealed class Light {
      // This method returns the light's status.
      public String SwitchPosition() {
         return "The light is off";
      }
   }

   // Define a Fan component.
   internal sealed class Fan {
      // This method returns the fan's status.
      public String Speed() {
         throw new InvalidOperationException("The fan broke due to overheating");
      }
   }

   // Define a Speaker component.
   internal sealed class Speaker {
      // This method returns the speaker's status.
      public String Volume() {
         return "The volume is loud";
      }
   }

   // Definition of delegate that allows querying a component's status.
   private delegate String GetStatus();

   public static void Go() {
      // Declare an empty delegate chain.
      GetStatus getStatus = null;

      // Construct the three components, and add their status methods 
      // to the delegate chain.
      getStatus += new GetStatus(new Light().SwitchPosition);
      getStatus += new GetStatus(new Fan().Speed);
      getStatus += new GetStatus(new Speaker().Volume);

      // Show consolidated status report reflecting 
      // the condition of the three components.
      Console.WriteLine(GetComponentStatusReport(getStatus));
   }

   // Method that queries several components and returns a status report
   private static String GetComponentStatusReport(GetStatus status) {

      // If the chain is empty, there’s is nothing to do.
      if (status == null) return null;

      // Use this to build the status report.
      StringBuilder report = new StringBuilder();

      // Get an array where each element is a delegate from the chain.
      Delegate[] arrayOfDelegates = status.GetInvocationList();

      // Iterate over each delegate in the array. 
      foreach (GetStatus getStatus in arrayOfDelegates) {

         try {
            // Get a component's status string, and append it to the report.
            report.AppendFormat("{0}{1}{1}", getStatus(), Environment.NewLine);
         }
         catch (InvalidOperationException e) {
            // Generate an error entry in the report for this component.
            Object component = getStatus.Target;
            report.AppendFormat(
               "Failed to get status from {1}{2}{0}   Error: {3}{0}{0}",
               Environment.NewLine,
               ((component == null) ? "" : component.GetType() + "."),
               getStatus.GetMethodInfo().Name, e.Message);
         }
      }

      // Return the consolidated report to the caller.
      return report.ToString();
   }
}

internal static class AnonymousMethods {
   public static void Go() {
      // Create and initialize a String array
      String[] names = { "Jeff", "Kristin", "Aidan" };

      // Get just the names that have a lowercase 'i' in them.
      Char charToFind = 'i';
      names = Array.FindAll(names, delegate(String name) { return (name.IndexOf(charToFind) >= 0); });

      // Convert each string's characters to uppercase
      names = Array.ConvertAll<String, String>(names, delegate(String name) { return name.ToUpper(); });

      // Sort the names
      Array.Sort(names, delegate(String name1, String name2) { return String.Compare(name1, name2); });

      // Display the results
      Array.ForEach(names, delegate(String name) { Console.WriteLine(name); });
   }

   private sealed class AClass {
      private static void CallbackWithoutNewingADelegateObject() {
         ThreadPool.QueueUserWorkItem(delegate(Object obj) { Console.WriteLine(obj); }, 5);
      }
   }

   private sealed class AClass2 {
      private static void UsingLocalVariablesInTheCallbackCode(Int32 numToDo) {

         // Some local variables
         Int32[] squares = new Int32[numToDo];
         AutoResetEvent done = new AutoResetEvent(false);

         // Do a bunch of tasks on other threads
         for (Int32 n = 0; n < squares.Length; n++) {
            ThreadPool.QueueUserWorkItem(
               delegate(Object obj) {
                  Int32 num = (Int32)obj;

                  // This task would normally more time consuming
                  squares[num] = num * num;

                  // If last task, let main thread continue running
                  if (Interlocked.Decrement(ref numToDo) == 0)
                     done.Set();
               }, n);
         }

         // Wait for all the other threads to finish
         done.WaitOne();

         // Show the results
         for (Int32 n = 0; n < squares.Length; n++)
            Console.WriteLine("Index {0}, Square={1}", n, squares[n]);
      }
   }
}

// Here are some different delegate definitions
internal delegate Object TwoInt32s(Int32 n1, Int32 n2);
internal delegate Object OneString(String s1);

internal static class DelegateReflection {
   public static void Go(params String[] args) {
      if (args.Length < 2) {
         String usage =
            @"Usage:" +
            "{0} delType methodName [Arg1] [Arg2]" +
            "{0}   where delType must be TwoInt32s or OneString" +
            "{0}   if delType is TwoInt32s, methodName must be Add or Subtract" +
            "{0}   if delType is OneString, methodName must be NumChars or Reverse" +
            "{0}" +
            "{0}Examples:" +
            "{0}   TwoInt32s Add 123 321" +
            "{0}   TwoInt32s Subtract 123 321" +
            "{0}   OneString NumChars \"Hello there\"" +
            "{0}   OneString Reverse  \"Hello there\"";
         Console.WriteLine(usage, Environment.NewLine);
         return;
      }

      // Convert the delType argument to a delegate type
      Type delType = Type.GetType(args[0]);
      if (delType == null) {
         Console.WriteLine("Invalid delType argument: " + args[0]);
         return;
      }

      Delegate d;
      try {
         // Convert the Arg1 argument to a method
         MethodInfo mi = typeof(DelegateReflection).GetTypeInfo().GetDeclaredMethod(args[1]);

         // Create a delegate object that wraps the static method
         d = mi.CreateDelegate(delType);
      }
      catch (ArgumentException) {
         Console.WriteLine("Invalid methodName argument: " + args[1]);
         return;
      }

      // Create an array that that will contain just the arguments
      // to pass to the method via the delegate object
      Object[] callbackArgs = new Object[args.Length - 2];

      if (d.GetType() == typeof(TwoInt32s)) {
         try {
            // Convert the String arguments to Int32 arguments
            for (Int32 a = 2; a < args.Length; a++)
               callbackArgs[a - 2] = Int32.Parse(args[a]);
         }
         catch (FormatException) {
            Console.WriteLine("Parameters must be integers.");
            return;
         }
      }

      if (d.GetType() == typeof(OneString)) {
         // Just copy the String argument
         Array.Copy(args, 2, callbackArgs, 0, callbackArgs.Length);
      }

      try {
         // Invoke the delegate and show the result
         Object result = d.DynamicInvoke(callbackArgs);
         Console.WriteLine("Result = " + result);
      }
      catch (TargetParameterCountException) {
         Console.WriteLine("Incorrect number of parameters specified.");
      }
   }

   // This callback method takes 2 Int32 arguments
   private static Object Add(Int32 n1, Int32 n2) {
      return n1 + n2;
   }

   // This callback method takes 2 Int32 arguments
   private static Object Subtract(Int32 n1, Int32 n2) {
      return n1 - n2;
   }

   // This callback method takes 1 String argument
   private static Object NumChars(String s1) {
      return s1.Length;
   }

   // This callback method takes 1 String argument
   private static Object Reverse(String s1) {
      return new String(s1.Reverse().ToArray());
   }
}
