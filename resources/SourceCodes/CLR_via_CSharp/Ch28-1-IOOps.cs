/******************************************************************************
Module:  Ch28-1-IOOps.cs
Notices: Copyright (c) 2013 by Jeffrey Richter
******************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32.SafeHandles;

public static class IOOps {
   [STAThread]
   public static void Main() {
      PipeDemo.Go().Wait();
      AsyncFuncCodeTransformation.Go();
      TaskLogger.Go().Wait();
      EventAwaiterDemo.Go();
      Features.Go();
      GuiDeadlockWindow.Go();
      Cancellation.Go().Wait();
      ThreadIO.Go();
      var s = AwaitWebClient(new Uri("http://Wintellect.com/")).Result;
   }

   private static async Task<String> AwaitWebClient(Uri uri) {
      // The System.Net.WebClient class supports the Event-based Asynchronous Pattern
      var wc = new System.Net.WebClient();

      // Create the TaskCompletionSource and its underlying Task object
      var tcs = new TaskCompletionSource<String>();

      // When a string completes downloading, the WebClient object raises the
      // DownloadStringCompleted event which completes the TaskCompletionSource
      wc.DownloadStringCompleted += (s, e) => {
         if (e.Cancelled) tcs.SetCanceled();
         else if (e.Error != null) tcs.SetException(e.Error);
         else tcs.SetResult(e.Result);
      };

      // Start the asynchronous operation
      wc.DownloadStringAsync(uri);

      // Now, we can the TaskCompletionSource’s Task and process the result as usual
      String result = await tcs.Task;
      // Process the resulting string (if desired)...

      return result;
   }
}

internal static class PipeDemo {
   public static async Task Go2() {
      var tasks = new Task[] {
         Task.Delay(10000),
         Task.Delay(1000),
         Task.Delay(5000),
         Task.Delay(3000),
      };
      foreach (var t in WhenEach(tasks)) {
         Console.WriteLine(Array.IndexOf(tasks, await t));
      }
   }

   public static async Task Go() {
      // Start the server which returns immediately since 
      // it asynchronously waits for client requests
      StartServer();

      // Make lots of async client requests; save each client's Task<String>
      Task<String>[] requests = new Task<String>[10000];
      for (Int32 n = 0; n < requests.Length; n++)
         requests[n] = IssueClientRequestAsync("localhost", "Request #" + n);

#if true   // Continue AFTER ALL tasks complete
      // Asynchronously wait until all client requests have completed
      String[] responses = await Task.WhenAll(requests);

      // Process all the responses
      for (Int32 n = 0; n < responses.Length; n++)
         Console.WriteLine(responses[n]);
#endif
#if true   // Continue AS EACH task completes
      List<Task<String>> pendingRequests = new List<Task<String>>(requests);
      while (pendingRequests.Count > 0) {
         // Asynchronously wait until a client request has completed
         Task<String> response = await Task.WhenAny(pendingRequests);
         pendingRequests.Remove(response); // Remove completed Task from collection

         // Process the response
         Console.WriteLine(response.Result);
      }
#endif
#if true   // More efficient way to continue AS EACH task completes
      foreach (var t in WhenEach(requests)) {
         // Asynchronously wait until the next client request has completed
         Task<String> response = await t;

         // Process the response
         Console.WriteLine(response.Result);
      }
#endif
   }

   public static IEnumerable<Task<Task<TResult>>> WhenEach<TResult>(params Task<TResult>[] tasks) {
      // Create a new TaskCompletionSource for each task
      var taskCompletions = new TaskCompletionSource<Task<TResult>>[tasks.Length];

      Int32 next = -1;  // Identifies the next TaskCompletionSource to complete
      // As each task completes, this callback completes the next TaskCompletionSource
      Action<Task<TResult>> taskCompletionCallback = t => taskCompletions[Interlocked.Increment(ref next)].SetResult(t);

      // Create all the TaskCompletionSource objects and tell each task to 
      // complete the next one as each task completes
      for (Int32 n = 0; n < tasks.Length; n++) {
         taskCompletions[n] = new TaskCompletionSource<Task<TResult>>();
         tasks[n].ContinueWith(taskCompletionCallback, TaskContinuationOptions.ExecuteSynchronously);
      }
      // Return each of the TaskCompledtionSource's Tasks in turn.
      // The Result property represents the original task that completed.
      for (Int32 n = 0; n < tasks.Length; n++) yield return taskCompletions[n].Task;
   }

   public static IEnumerable<Task<Task>> WhenEach(params Task[] tasks) {
      // Create a new TaskCompletionSource for each task
      var taskCompletions = new TaskCompletionSource<Task>[tasks.Length];

      Int32 next = -1;  // Identifies the next TaskCompletionSource to complete
      // As each task completes, this callback completes the next TaskCompletionSource
      Action<Task> taskCompletionCallback = t => taskCompletions[Interlocked.Increment(ref next)].SetResult(t);

      // Create all the TaskCompletionSource objects and tell each task to 
      // complete the next one as each task completes
      for (Int32 n = 0; n < tasks.Length; n++) {
         taskCompletions[n] = new TaskCompletionSource<Task>();
         tasks[n].ContinueWith(taskCompletionCallback, TaskContinuationOptions.ExecuteSynchronously);
      }
      // Return each of the TaskCompledtionSource's Tasks in turn.
      // The Result property represents the original task that completed.
      for (Int32 n = 0; n < tasks.Length; n++) yield return taskCompletions[n].Task;      
   }

   private static async void StartServer() {
      while (true) {
         var pipe = new NamedPipeServerStream("PipeName", PipeDirection.InOut, -1,
            PipeTransmissionMode.Message, PipeOptions.Asynchronous | PipeOptions.WriteThrough);

         // Asynchronously accept a client connection
         // NOTE: NamedPipServerStream uses the old Asynchronous Programming Model (APM) 
         // I convert the old APM to the new Task model via TaskFactory's FromAsync method
         await Task.Factory.FromAsync(pipe.BeginWaitForConnection, pipe.EndWaitForConnection, null);

         // Start servicing the client which returns immediately since it is asynchronous
         ServiceClientRequestAsync(pipe);
      }
   }

   // This field records the timestamp of the most recent client's request
   private static DateTime s_lastClientRequest = DateTime.MinValue;

   // The SemaphoreSlim protects enforces thread-safe access to s_lastClientRequest
   private static readonly SemaphoreSlim s_lock = new SemaphoreSlim(1);

   private static async void ServiceClientRequestAsync(NamedPipeServerStream pipe) {
      using (pipe) {
         // Asynchronously read a request from the client
         Byte[] data = new Byte[1000];
         Int32 bytesRead = await pipe.ReadAsync(data, 0, data.Length);

         // Get the timestamp of this client's request
         DateTime now = DateTime.Now;

         // We want to save the timestamp of the most-recent client request. 
         // Since many clients can run concurrently, this has to be thread-safe.
         await s_lock.WaitAsync(); // Asynchronously request exclusive access

         // When we get here, we know no other thread is touching s_lastClientRequest
         if (s_lastClientRequest < now) s_lastClientRequest = now;
         s_lock.Release();   // Relinquish access so other clients can update

         // My sample server just changes all the characters to uppercase.
         // You can replace this code with any compute-bound operation.
         data = Encoding.UTF8.GetBytes(
            Encoding.UTF8.GetString(data, 0, bytesRead).ToUpper().ToCharArray());

         // Asynchronously send the response back to the client
         await pipe.WriteAsync(data, 0, data.Length);
      } // Close the pipe to the client
   }

   private static async Task<String> IssueClientRequestAsync(String serverName, String message) {
      using (var pipe = new NamedPipeClientStream(serverName, "PipeName", PipeDirection.InOut,
         PipeOptions.Asynchronous | PipeOptions.WriteThrough)) {

         pipe.Connect(); // Must Connect before setting ReadMode
         pipe.ReadMode = PipeTransmissionMode.Message;

         // Asynchronously send data to the server
         Byte[] request = Encoding.UTF8.GetBytes(message);
         await pipe.WriteAsync(request, 0, request.Length);

         // Asynchronously read the server's response
         Byte[] response = new Byte[1000];
         Int32 bytesRead = await pipe.ReadAsync(response, 0, response.Length);
         return Encoding.UTF8.GetString(response, 0, bytesRead);
      }  // Close the pipe
   }
}

internal static class AsyncFuncCodeTransformation {
   public static void Go() {
      //var s = MyMethodAsync(5).Result;
      var s = MyMethodAsync_ActualImplementation(5).Result;
   }

   private sealed class Type1 { }
   private sealed class Type2 { }
   private static Task<Type1> Method1Async() { return Task.Run(() => { /*Task.Yield(); */return new Type1(); }); }
   private static Task<Type2> Method2Async() { return Task.Run(() => { /*Task.Yield(); */return new Type2(); }); }

   private static async Task<String> MyMethodAsync(Int32 argument) {
      Int32 local = argument;
      try {
         Type1 result1 = await Method1Async();
         for (Int32 x = 0; x < 3; x++) {
            Type2 result2 = await Method2Async();
         }
      }
      catch (Exception) {
         Console.WriteLine("Catch");
      }
      finally {
         Console.WriteLine("Finally");
      }
      return "Done";
   }

   // AsyncStateMachine attribute indicates an async method (good for tools using reflection); 
   // the type indicates which structure implements the state machine
   [DebuggerStepThrough, AsyncStateMachine(typeof(StateMachine))]
   private static Task<String> MyMethodAsync_ActualImplementation(Int32 argument) {
      // Create state machine instance & initialize it
      StateMachine stateMachine = new StateMachine() {
         // Create builder returning Task<String> from this stub method
         // State machine accesses builder to set Task completion/exception
         m_builder = AsyncTaskMethodBuilder<String>.Create(),

         m_state = -1,           // Initialize state machine location
         m_argument = argument   // Copy arguments to state machine fields
      };

      // Start executing the state machine
      stateMachine.m_builder.Start(ref stateMachine);
      return stateMachine.m_builder.Task; // Return state machine's Task
   }


   // This is the state machine structure
   [CompilerGenerated, StructLayout(LayoutKind.Auto)]
   private struct StateMachine : IAsyncStateMachine {
      // Fields for state machine's builder (Task) & its location
      public AsyncTaskMethodBuilder<String> m_builder;
      public Int32 m_state;

      // Argument and local variables are fields now:
      public Int32 m_argument, m_local, m_x;
      public Type1 m_resultType1;
      public Type2 m_resultType2;

      // There is 1 field per awaiter type.
      // Only 1 of these fields is important at any time. That field refers 
      // to the most recently executed await that is completing asynchronously:
      private TaskAwaiter<Type1> m_awaiterType1;
      private TaskAwaiter<Type2> m_awaiterType2;

      // This is the state machine method itself
      void IAsyncStateMachine.MoveNext() {
         String result = null;   // Task's result value

         // Compiler-inserted try block ensures the state machine’s task completes
         try {
            Boolean executeFinally = true;  // Assume we're logically leaving the 'try' block
            if (m_state == -1) {            // If 1st time in state machine method, 
               m_local = m_argument;        // execute start of original method
            }

            // Try block that we had in our original code
            try {
               TaskAwaiter<Type1> awaiterType1;
               TaskAwaiter<Type2> awaiterType2;

               switch (m_state) {
                  case -1: // Start execution of code in 'try'
                     // Call Method1Async and get its awaiter
                     awaiterType1 = Method1Async().GetAwaiter();
                     if (!awaiterType1.IsCompleted) {
                        m_state = 0;                   // 'Method1Async' is completing asynchronously
                        m_awaiterType1 = awaiterType1; // Save the awaiter for when we come back

                        // Tell awaiter to call MoveNext when operation completes
                        m_builder.AwaitUnsafeOnCompleted(ref awaiterType1, ref this);
                        // The line above invokes awaiterType1's OnCompleted which approximately 
                        // calls ContinueWith(t => MoveNext()) on the Task being awaited.
                        // When the Task completes, the ContinueWith task calls MoveNext

                        executeFinally = false;        // We're not logically leaving the 'try' block
                        return;                        // Thread returns to caller
                     }
                     // 'Method1Async' completed synchronously
                     break;

                  case 0:  // 'Method1Async' completed asynchronously
                     awaiterType1 = m_awaiterType1;  // Restore most-recent awaiter
                     break;

                  case 1:  // 'Method2Async' completed asynchronously
                     awaiterType2 = m_awaiterType2;  // Restore most-recent awaiter
                     goto ForLoopEpilog;
               }

               // After the first await, we capture the result & start the 'for' loop
               m_resultType1 = awaiterType1.GetResult(); // Get awaiter's result

               //             ForLoopPrologue:
               m_x = 0;          // 'for' loop initialization
               goto ForLoopBody; // Skip to 'for' loop body

            ForLoopEpilog:
               m_resultType2 = awaiterType2.GetResult();
               m_x++;            // Increment x after each loop iteration
            // Fall into the 'for' loop's body

            ForLoopBody:
               if (m_x < 3) {  // 'for' loop test
                  // Call Method2Async and get its awaiter
                  awaiterType2 = Method2Async().GetAwaiter();
                  if (!awaiterType2.IsCompleted) {
                     m_state = 1;                   // 'Method2Async' is completing asynchronously
                     m_awaiterType2 = awaiterType2; // Save the awaiter for when we come back

                     // Tell awaiter to call MoveNext when operation completes (see above)
                     m_builder.AwaitUnsafeOnCompleted(ref awaiterType2, ref this);
                     executeFinally = false;        // We're not logically leaving the 'try' block
                     return;                        // Thread returns to caller
                  }
                  // 'Method2Async' completed synchronously
                  goto ForLoopEpilog;  // Completed synchronously, loop around
               }
            }
            catch (Exception) {
               Console.WriteLine("Catch");
            }
            finally {
               // Whenever a thread physically leaves a 'try', the 'finally' executes
               // We only want to execute this code when the thread logically leaves the 'try'
               if (executeFinally) {
                  Console.WriteLine("Finally");
               }
            }
            result = "Done"; // What we ultimately want to return from the async function
         }
         catch (Exception exception) {
            // Unhandled exception: complete state machine's Task with exception
            m_builder.SetException(exception);
            return;
         }
         // No exception: complete state machine's Task with result
         m_builder.SetResult(result);
      }

      [DebuggerHidden]
      void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine param0) { m_builder.SetStateMachine(param0); }
   }
}

public static class TaskLogger {
   public static async Task Go() {
#if DEBUG
      // Using TaskLogger incurs a memory and performance hit; so turn it on in debug builds
      TaskLogger.LogLevel = TaskLogger.TaskLogLevel.Pending;
#endif

      // Initiate 3 task; for testing the TaskLogger, we control their duration explicitly
      var tasks = new List<Task> {
         Task.Delay(2000).Log("2s op"),
         Task.Delay(5000).Log("5s op"),
         Task.Delay(6000).Log("6s op")
      };

      try {
         // Wait for all tasks but cancel after 3 seconds; only 1 task above should complete in time
         await Task.WhenAll(tasks).
            WithCancellation(new CancellationTokenSource(3000).Token);
      }
      catch (OperationCanceledException) { }

      // Ask the logger which tasks have not yet completed and sort
      // them in order from the one that’s been waiting the longest
      foreach (var op in TaskLogger.GetLogEntries().OrderBy(tle => tle.LogTime))
         Console.WriteLine(op);
   }

   public enum TaskLogLevel { None, Pending }
   public static TaskLogLevel LogLevel { get; set; }

   public sealed class TaskLogEntry {
      public Task Task { get; internal set; }
      public String Tag { get; internal set; }
      public DateTime LogTime { get; internal set; }
      public String CallerMemberName { get; internal set; }
      public String CallerFilePath { get; internal set; }
      public Int32 CallerLineNumber { get; internal set; }
      public override string ToString() {
         return String.Format("LogTime={0}, Tag={1}, Member={2}, File={3}({4})",
            LogTime, Tag ?? "(none)", CallerMemberName, CallerFilePath, CallerLineNumber);
      }
   }

   private static readonly ConcurrentDictionary<Task, TaskLogEntry> s_log = new ConcurrentDictionary<Task, TaskLogEntry>();
   public static IEnumerable<TaskLogEntry> GetLogEntries() { return s_log.Values; }

   public static Task<TResult> Log<TResult>(this Task<TResult> task, String tag = null,
      [CallerMemberName] String callerMemberName = null,
      [CallerFilePath] String callerFilePath = null,
      [CallerLineNumber] Int32 callerLineNumber = -1) {
      return (Task<TResult>)Log((Task)task, tag, callerMemberName, callerFilePath, callerLineNumber);
   }

   public static Task Log(this Task task, String tag = null,
      [CallerMemberName] String callerMemberName = null,
      [CallerFilePath] String callerFilePath = null,
      [CallerLineNumber] Int32 callerLineNumber = -1) {
      if (LogLevel == TaskLogLevel.None) return task;
      var logEntry = new TaskLogEntry {
         Task = task,
         LogTime = DateTime.Now,
         Tag = tag,
         CallerMemberName = callerMemberName,
         CallerFilePath = callerFilePath,
         CallerLineNumber = callerLineNumber
      };
      s_log[task] = logEntry;
      task.ContinueWith(t => { TaskLogEntry entry; s_log.TryRemove(t, out entry); },
         TaskContinuationOptions.ExecuteSynchronously);
      return task;
   }
}

internal static class EventAwaiterDemo {
   public static void Go() {
      ShowExceptions();

      for (Int32 x = 0; x < 3; x++) {
         try {
            switch (x) {
               case 0: throw new InvalidOperationException();
               case 1: throw new ObjectDisposedException("");
               case 2: throw new ArgumentOutOfRangeException();
            }
         }
         catch { }
      }
   }

   private static async void ShowExceptions() {
      var eventAwaiter = new EventAwaiter<FirstChanceExceptionEventArgs>();
      AppDomain.CurrentDomain.FirstChanceException += eventAwaiter.EventRaised;
      while (true) {
         Console.WriteLine("AppDomain exception: {0}",
            (await eventAwaiter).Exception.GetType());
      }
   }

   public sealed class EventAwaiter<TEventArgs> : INotifyCompletion {
      private ConcurrentQueue<TEventArgs> m_events = new ConcurrentQueue<TEventArgs>();
      private Action m_continuation;

      #region Members invoked by the state machine
      // The state machine will call this first to get our awaiter; we return ourself
      public EventAwaiter<TEventArgs> GetAwaiter() { return this; }

      // Tell state machine if any events have happened yet
      public Boolean IsCompleted { get { return m_events.Count > 0; } }

      // The state machine tells us what method to invoke later; we save it
      public void OnCompleted(Action continuation) {
         Volatile.Write(ref m_continuation, continuation);
      }

      // The state machine queries the result; this is the await operator's result
      public TEventArgs GetResult() {
         TEventArgs e;
         m_events.TryDequeue(out e);
         return e;
      }
      #endregion

      // Potentially invoked by multiple threads simultaneously when each raises the event
      public void EventRaised(Object sender, TEventArgs eventArgs) {
         m_events.Enqueue(eventArgs);   // Save EventArgs to return it from GetResult/await

         // If there is a pending continuation, this thread takes it
         Action continuation = Interlocked.Exchange(ref m_continuation, null);
         if (continuation != null) continuation();   // Resume the state machine
      }
   }
}

internal static class Features {
   public static void Go() {
      AsyncLambdaExpression();
      OuterAsyncFunction().Wait();
   }

   private static void AsyncLambdaExpression() {
      Task.Run(async () => {
         // TODO: Do intensive compute-bound processing here...

         // Initiate asynchronous operation:
         await new Task(() => 5.GetType());// XxxAsync();
         // Do more processing here
      });
   }

   private static async Task OuterAsyncFunction() {
      // Here, it calls an async function but OuterAsyncFunction doesn't 
      // care about its completion so it doesn't await it; the method 
      // continues executing.
      InnerAsyncFunction().NoWarning();

      // Code here continues to execute...
      await Task.Delay(0);
   }

   private static async Task InnerAsyncFunction() {
      await Task.Delay(1000);
   }

   [MethodImpl(MethodImplOptions.AggressiveInlining)]  // Causes compiler to optimize the call away
   public static void NoWarning(this Task task) { }
}

internal sealed class GuiDeadlockWindow : Window {
   public static void Go() {
      // This is a WPF example
      new GuiDeadlockWindow().ShowDialog();
   }
   public GuiDeadlockWindow() { Title = "WPF Window"; }

   protected override void OnActivated(EventArgs e) {
      // Querying the Result property prevents the GUI thread from returning; 
      // the thread blocks waiting for the result
      String http = GetHttp3().Result;  // Get the string synchronously!

      base.OnActivated(e);
   }

   private async Task<String> GetHttp1() {
      // Issue the HTTP request and let the thread return from GetHttp
      HttpResponseMessage msg = await new HttpClient().GetAsync("http://Wintellect.com/");
      // We DO get here now because a thread pool can execute this code 
      // as opposed to forcing the GUI thread to execute it.      

      return await msg.Content.ReadAsStringAsync();
   }

   private async Task<String> GetHttp2() {
      // Issue the HTTP request and let the thread return from GetHttp
      HttpResponseMessage msg = await new HttpClient().GetAsync("http://Wintellect.com/").ConfigureAwait(false);
      // We DO get here now because a thread pool can execute this code 
      // as opposed to forcing the GUI thread to execute it.      

      return await msg.Content.ReadAsStringAsync().ConfigureAwait(false);
   }

   private Task<String> GetHttp3() {
      return Task.Run(async () => {
         // We run on a thread pool thread now which has no SynchronizationContext on it
         HttpResponseMessage msg = await new HttpClient().GetAsync("http://Wintellect.com/");
         // We DO get here because some thread pool can execute this code 

         return await msg.Content.ReadAsStringAsync();
      });
   }
}

internal static class Cancellation {
   public static async Task Go() {
      // Create a CancellationTokenSource that cancels itself after # milliseconds
      var cts = new CancellationTokenSource(15000); // To cancel sooner, call cts.Cancel()
      var ct = cts.Token;

      try {
         await Task.Delay(10000).WithCancellation(ct);
         Console.WriteLine("Task completed");
      }
      catch (OperationCanceledException) {
         Console.WriteLine("Task cancelled");
      }
   }

   private struct Void { } // Because there isn't a non-generic TaskCompletionSource class.

   public static async Task<TResult> WithCancellation<TResult>(this Task<TResult> orignalTask, CancellationToken ct) {
      // Create a Task that completes when the CancellationToken is canceled
      var cancelTask = new TaskCompletionSource<Void>();

      // When the CancellationToken is cancelled, complete the Task
      using (ct.Register(t => ((TaskCompletionSource<Void>)t).TrySetResult(new Void()), cancelTask)) {

         // Create another Task that completes when the original Task or when the CancellationToken's Task
         Task any = await Task.WhenAny(orignalTask, cancelTask.Task);

         // If any Task completes due to CancellationToken, throw OperationCanceledException         
         if (any == cancelTask.Task) ct.ThrowIfCancellationRequested();
      }

      // await original task (synchronously); if it failed, awaiting it 
      // throws 1st inner exception instead of AggregateException
      return await orignalTask;
   }

   public static async Task WithCancellation(this Task task, CancellationToken ct) {
      var tcs = new TaskCompletionSource<Void>();
      using (ct.Register(t => ((TaskCompletionSource<Void>)t).TrySetResult(default(Void)), tcs)) {
         if (await Task.WhenAny(task, tcs.Task) == tcs.Task) ct.ThrowIfCancellationRequested();
      }
      await task;          // If failure, ensures 1st inner exception gets thrown instead of AggregateException
   }
}

internal static class ThreadIO {
   public static void Go() {
      using (ThreadIO.BeginBackgroundProcessing()) {
         // Issue low-priority I/O request in here...
      }
   }

   public static BackgroundProcessingDisposer BeginBackgroundProcessing(Boolean process = false) {
      ChangeBackgroundProcessing(process, true);
      return new BackgroundProcessingDisposer(process);
   }

   public static void EndBackgroundProcessing(Boolean process = false) {
      ChangeBackgroundProcessing(process, false);
   }

   private static void ChangeBackgroundProcessing(Boolean process, Boolean start) {
      Boolean ok = process
         ? SetPriorityClass(GetCurrentWin32ProcessHandle(),
               start ? ProcessBackgroundMode.Start : ProcessBackgroundMode.End)
         : SetThreadPriority(GetCurrentWin32ThreadHandle(),
               start ? ThreadBackgroundgMode.Start : ThreadBackgroundgMode.End);
      if (!ok) throw new Win32Exception();
   }

   // This struct lets C#'s using statement end the background processing mode
   public struct BackgroundProcessingDisposer : IDisposable {
      private readonly Boolean m_process;
      public BackgroundProcessingDisposer(Boolean process) { m_process = process; }
      public void Dispose() { EndBackgroundProcessing(m_process); }
   }


   // See Win32’s THREAD_MODE_BACKGROUND_BEGIN and THREAD_MODE_BACKGROUND_END
   private enum ThreadBackgroundgMode { Start = 0x10000, End = 0x20000 }

   // See Win32’s PROCESS_MODE_BACKGROUND_BEGIN and PROCESS_MODE_BACKGROUND_END   
   private enum ProcessBackgroundMode { Start = 0x100000, End = 0x200000 }

   [DllImport("Kernel32", EntryPoint = "GetCurrentProcess", ExactSpelling = true)]
   private static extern SafeWaitHandle GetCurrentWin32ProcessHandle();

   [DllImport("Kernel32", ExactSpelling = true, SetLastError = true)]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static extern Boolean SetPriorityClass(SafeWaitHandle hprocess, ProcessBackgroundMode mode);


   [DllImport("Kernel32", EntryPoint = "GetCurrentThread", ExactSpelling = true)]
   private static extern SafeWaitHandle GetCurrentWin32ThreadHandle();

   [DllImport("Kernel32", ExactSpelling = true, SetLastError = true)]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static extern Boolean SetThreadPriority(SafeWaitHandle hthread, ThreadBackgroundgMode mode);

   // http://msdn.microsoft.com/en-us/library/aa480216.aspx
   [DllImport("Kernel32", SetLastError = true, EntryPoint = "CancelSynchronousIo")]
   [return: MarshalAs(UnmanagedType.Bool)]
   private static extern Boolean CancelSynchronousIO(SafeWaitHandle hThread);
}
