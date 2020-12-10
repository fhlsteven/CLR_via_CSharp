//#define CompilerImplementedEventMethods
using System;
using System.Threading;

public static class Events {
   public static void Main() {
      MailManager.Go();
      TypeWithLotsOfEventsTest();
   }

   private static void TypeWithLotsOfEventsTest() {
      // The code here tests the event
      TypeWithLotsOfEvents twle = new TypeWithLotsOfEvents();

      // Add a callback here
      twle.Foo += HandleFooEvent;
      twle.SimulateFoo();
      Console.WriteLine("The callback was invoked 1 time above" + Environment.NewLine);

      // Add another callback here
      twle.Foo += HandleFooEvent;
      twle.SimulateFoo();
      Console.WriteLine("The callback was invoked 2 times above" + Environment.NewLine);

      // Remove a callback here
      twle.Foo -= HandleFooEvent;
      twle.SimulateFoo();
      Console.WriteLine("The callback was invoked 1 time above" + Environment.NewLine);

      // Remove another callback here
      twle.Foo -= HandleFooEvent;
      twle.SimulateFoo();
      Console.WriteLine("The callback was invoked 0 times above" + Environment.NewLine);

      Console.WriteLine("Press <Enter> to terminate this application.");
      Console.ReadLine();
   }

   private static void HandleFooEvent(object sender, FooEventArgs e) {
      Console.WriteLine("Handling Foo Event here...");
   }
}

///////////////////////////////////////////////////////////////////////////////

// Step #1: Define a type that will hold any additional information that 
// should be sent to receivers of the event notification  
internal sealed class NewMailEventArgs : EventArgs {

   private readonly String m_from, m_to, m_subject;

   public NewMailEventArgs(String from, String to, String subject) {
      m_from = from; m_to = to; m_subject = subject;
   }

   public String From { get { return m_from; } }
   public String To { get { return m_to; } }
   public String Subject { get { return m_subject; } }
}

internal class MailManager {
   public static void Go() {
      // Construct a MailManager object
      MailManager mm = new MailManager();

      // Construct a Fax object passing it the MailManager object
      Fax fax = new Fax(mm);

      // Construct a Pager object passing it the MailManager object
      Pager pager = new Pager(mm);

      // Simulate an incoming mail message
      mm.SimulateNewMail("Jeffrey", "Kristin", "I Love You!");

      // Force the Fax object to unregister itself with the MailManager
      fax.Unregister(mm);

      // Simulate an incoming mail message
      mm.SimulateNewMail("Jeffrey", "Mom & Dad", "Happy Birthday.");
   }

#if CompilerImplementedEventMethods
   // Step #2: Define the event member 
	public event EventHandler<NewMailEventArgs> NewMail;
#else
	// Add private field which refers to the head of the delegate linked-list
	private EventHandler<NewMailEventArgs> m_NewMail;

	// Add an event member to the class
	public event EventHandler<NewMailEventArgs> NewMail {
		// Explicitly implement the 'add' method
		add {
			// Without thread-safety, add a handler 
         // (passed as 'value') to the delegate linked list
			m_NewMail += value; 
		}

		// Explicitly implement the 'remove' method
		remove {
			// Without thread-safety, remove a handler 
			// (passed as 'value') from the delegate linked list
			m_NewMail -= value;
		}
	}

#endif

   // Step #3: Define a method responsible for raising the event 
	// to notify registered objects that the event has occurred
	// If this class is sealed, make this method private and nonvirtual
	protected virtual void OnNewMail(NewMailEventArgs e) {
      // Copy a reference to the delegate field now into a temporary field for thread safety 
      //e.Raise(this, ref m_NewMail);

#if CompilerImplementedEventMethods
		EventHandler<NewMailEventArgs> temp = Volatile.Read(ref NewMail);
#else
		EventHandler<NewMailEventArgs> temp = Volatile.Read(ref m_NewMail);
#endif

		// If any methods registered interest with our event, notify them 
		if (temp != null) temp(this, e);
	}

	// Step #4: Define a method that translates the 
	// input into the desired event
   public void SimulateNewMail(String from, String to, String subject) {

      // Construct an object to hold the information we wish
      // to pass to the receivers of our notification
      NewMailEventArgs e = new NewMailEventArgs(from, to, subject);

      // Call our virtual method notifying our object that the event
      // occurred. If no type overrides this method, our object will
      // notify all the objects that registered interest in the event
      OnNewMail(e);
   }
}

public static class EventArgExtensions {
   public static void Raise<TEventArgs>(this TEventArgs e, Object sender, ref EventHandler<TEventArgs> eventDelegate) {
      // Copy a reference to the delegate field now into a temporary field for thread safety 
      EventHandler<TEventArgs> temp = Volatile.Read(ref eventDelegate);

      // If any methods registered interest with our event, notify them  
      if (temp != null) temp(sender, e);
   }
}

internal sealed class Fax {
   // Pass the MailManager object to the constructor
   public Fax(MailManager mm) {
      // Construct an instance of the EventHandler<NewMailEventArgs> 
      // delegate that refers to our FaxMsg callback method.
      // Register our callback with MailManager's NewMail event
		mm.NewMail += FaxMsg;
	}

   // This is the method the MailManager will call
   // when a new e-mail message arrives
   private void FaxMsg(Object sender, NewMailEventArgs e) {
      // 'sender' identifies the MailManager object in case 
      // we want to communicate back to it.

      // 'e' identifies the additional event information 
      // the MailManager wants to give us.

      // Normally, the code here would fax the e-mail message.
      // This test implementation displays the info in the console
      Console.WriteLine("Faxing mail message:");
      Console.WriteLine("   From={0}, To={1}, Subject={2}",
         e.From, e.To, e.Subject);
   }

	// This method could be executed to have the Fax object unregister itself with the NewMail 
   // event so that it no longer receives notifications
   public void Unregister(MailManager mm) {
      // Unregister ourself with MailManager's NewMail event
      mm.NewMail -= FaxMsg;
   }
}

///////////////////////////////////////////////////////////////////////////////

internal sealed class Pager {
   // Pass the MailManager object to the constructor
   public Pager(MailManager mm) {
      // Construct an instance of the ProcessMailMsgEventHandler 
      // delegate that refers to our SendMsgToPager callback method.
      // Register our callback with MailManager's ProcessMailMsg event
      mm.NewMail += SendMsgToPager;
   }

   // This is the method that the MailManager will call
   // when a new e-mail message arrives
   private void SendMsgToPager(Object sender, NewMailEventArgs e) {
      // 'sender' identifies the MailManager in case we want to communicate back to it.
      // 'e' identifies the additional event information that the MailManager wants to give us.

      // Normally, the code here would send the e-mail message to a pager.
      // This test implementation displays the info on the console
      Console.WriteLine("Sending mail message to pager:");
		Console.WriteLine("   From={0}, To={1}, Subject={2}", e.From, e.To, e.Subject);
	}

	public void Unregister(MailManager mm) {
		// Unregister ourself with MailManager's ProcessMailMsg event
		mm.NewMail -= SendMsgToPager;
	}
}

//////////////////////////////// End of File //////////////////////////////////
