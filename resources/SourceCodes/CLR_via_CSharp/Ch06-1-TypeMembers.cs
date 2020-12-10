// Only one of the following 4 symbol be uncomment at any one time
#define V1
//#define V2a
//#define V2b
//#define V2c

#if !DEBUG
#pragma warning disable 414, 67
#endif
using System;

public sealed class SomeType {                                 //  1
   // Nested class
   private class SomeNestedType { }                            //  2

   // Constant, read-only, and static read/write field
   private const Int32 c_SomeConstant = 1;                     //  3
   private readonly String m_SomeReadOnlyField = "2";          //  4
   private static Int32 s_SomeReadWriteField = 3;              //  5

   // Type constructor
   static SomeType() { }                                       //  6

   // Instance constructors
   public SomeType() { }                                       //  7
   public SomeType(Int32 x) { }                                //  8


   // Static and instance methods
   public static void Main() { }                               //  9

   public String InstanceMethod() { return null; }             // 10

   // Instance property
   public Int32 SomeProp {                                     // 11
      get { return 0; }                                        // 12
      set { }                                                  // 13
   }

   // Instance parameterful property (indexer)
   public Int32 this[String s] {                               // 14
      get { return 0; }                                        // 15
      set { }                                                  // 16
   }

   // Instance event
   public event EventHandler SomeEvent;                        // 17
}

internal static class OverridingAccessibility {
   class Base {
      protected virtual void M() { }
   }

   class Derived1 : Base {
      protected override void M() { }
   }

   class Derived2 : Base {
      protected override void M() { }
      public static void Main() { }
   }
}

internal static class DifferentCalls {
   public static void Go() {
      Console.WriteLine();			// Call a static method

      Object o = new Object();
      o.GetHashCode();				// Call a virtual instance method
      o.GetType();					// Call a nonvirtual instance method
   }
}

#region Versioning Components With Virtual Methods
public sealed class VersioningComponentsWithVirtualMethods {
   public static void Main() {
      CompanyB.BetterPhone phone = new CompanyB.BetterPhone();
      phone.Dial();
   }
}


///////////////////////////////////////////////////////////////////////


#if V1
namespace CompanyA {
   public class Phone {
      public void Dial() {
         Console.WriteLine("Phone.Dial");
         // Do work to dial the phone here.
      }
   }
}


namespace CompanyB {
   public class BetterPhone : CompanyA.Phone {

      // This Dial method has nothing to do with Phone's Dial method
      new public void Dial() {
         Console.WriteLine("BetterPhone.Dial");
         EstablishConnection();
         base.Dial();
      }

      protected virtual void EstablishConnection() {
         Console.WriteLine("BetterPhone.EstablishConnection");
         // Do work to establish the connection.
      }
   }
}
#endif


///////////////////////////////////////////////////////////////////////


#if V2a || V2b || V2c
namespace CompanyA {
	public class Phone {
		public void Dial() {
			Console.WriteLine("Phone.Dial");
			EstablishConnection();
			// Do work to dial the phone here.
		}

		protected virtual void EstablishConnection() {
	      Console.WriteLine("Phone.EstablishConnection");
		   // Do work to establish the connection.
		}
	}
}
#endif


///////////////////////////////////////////////////////////////////////


#if V2a
namespace CompanyB {
	public class BetterPhone : CompanyA.Phone {

		// Keep 'new' to mark this method as having no
		// relationship to the base type's Dial method.
		new public void Dial() { 
			Console.WriteLine("BetterPhone.Dial");
			EstablishConnection();
			base.Dial();
		}

		// Add 'new' to mark this method as having no
		// relationship to the base type's EstablishConnection method.
		new protected virtual void EstablishConnection() {
			Console.WriteLine("BetterPhone.EstablishConnection");
			// Do work to establish the connection.
		}
	}
}
#endif


#if V2b
namespace CompanyB {
	public class BetterPhone : CompanyA.Phone {
		// Delete our Dial method (inherit Dial from base)

		// Remove 'new' and change 'virtual' to 'override' to
		// mark this method as having a relationship to the base
		protected override void EstablishConnection() {
			Console.WriteLine("BetterPhone.EstablishConnection");
			// Do work to establish the connection.
		}
	}
}
#endif


#if V2c
namespace CompanyB {
	public class BetterPhone : CompanyA.Phone {

		// Keep 'new' to mark this method as having no
		// relationship to the base type's Dial method.
		new public void Dial() { 
			Console.WriteLine("BetterPhone.Dial");
			EstablishConnection();
			base.Dial();
		}

		// Remove 'new' and change 'virtual' to 'override' to
		// mark this method as having a relationship to the base
		protected override void EstablishConnection() {
			Console.WriteLine("BetterPhone.EstablishConnection");
			// Do work to establish the connection.
		}
	}
}
#endif
#endregion