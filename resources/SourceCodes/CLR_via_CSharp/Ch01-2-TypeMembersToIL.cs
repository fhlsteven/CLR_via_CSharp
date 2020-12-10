#pragma warning disable 660, 661, 67

using System;

internal sealed class Test {
   // Constructor
   public Test() { }

   // Finalizer
   ~Test() { }

   // Operator overload
   public static Boolean operator ==(Test t1, Test t2) {
      return true;
   }
   public static Boolean operator !=(Test t1, Test t2) {
      return false;
   }

   // An operator overload
   public static Test operator +(Test t1, Test t2) { return null; }

   // A property
   public String AProperty {
      get { return null; }
      set { }
   }

   // An indexer
   public String this[Int32 x] {
      get { return null; }
      set { }
   }

   // An event
#pragma warning disable 67
   public event EventHandler AnEvent;
#pragma warning restore 67
}
