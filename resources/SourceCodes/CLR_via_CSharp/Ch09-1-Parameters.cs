#define V1
//#define V2
//#define V3

using System;
using System.Collections.Generic;

public static class Parameters {
   public static void Main() {
      OptionalAndNamedParameters.Go();
      MethodsThatTakeVariableArguments.Go();
   }
}

internal static class OptionalAndNamedParameters {
   private static Int32 s_n = 0;

   public static void Go() {
      ImplicitlyTypedLocalVariables();

      // 1. Same as: M(9, "A", default(DateTime), new Guid());
      M();

      // 2. Same as: M(8, "X", default(DateTime), new Guid());
      M(8, "X");

      // 3. Same as: M(5, "A", DateTime.Now, Guid.NewGuid());
      M(5, guid: Guid.NewGuid(), dt: DateTime.Now);

      // 4. Same as: M(0, "1", default(DateTime), new Guid());
      M(s_n++, s_n++.ToString());

      // 5. Same as: String t1 = "2"; Int32 t2 = 3; 
      //             M(t2, t1, default(DateTime), new Guid());
      M(s: (s_n++).ToString(), x: s_n++);
   }

   private static void M(Int32 x = 9, String s = "A",
      DateTime dt = default(DateTime), Guid guid = new Guid()) {

      Console.WriteLine("x={0}, s={1}, dt={2}, guid={3}", x, s, dt, guid);
   }

   private static void ImplicitlyTypedLocalVariables() {
      var name = "Jeff";
      ShowVariableType(name);    // Displays: System.String

      // var n = null;            // Error
      var x = (Exception)null;   // OK, but not much value
      ShowVariableType(x);       // Displays: System.Exception

      var numbers = new Int32[] { 1, 2, 3, 4 };
      ShowVariableType(numbers); // Displays: System.Int32[]

      // Less typing for complex types
      var collection = new Dictionary<String, Single>() { { ".NET", 4.0f } };

      // Displays: System.Collections.Generic.Dictionary`2[System.String,System.Single]
      ShowVariableType(collection);

      foreach (var item in collection) {
         // Displays: System.Collections.Generic.KeyValuePair`2[System.String,System.Single]
         ShowVariableType(item);
      }
   }

   private static void ShowVariableType<T>(T t) { Console.WriteLine(typeof(T)); }
}


internal static class OutAndRefParameters {
#if V1
   public static void Go() {
      Int32 x;               // x is uninitialized
      SetVal(out x);         // x doesn’t have to be initialized.
      Console.WriteLine(x);  // Displays "10"
   }

   private static void SetVal(out Int32 v) {
      v = 10;  // This method must initialize v.
   }
#endif
#if V2
		public static void Main() {
			Int32 x = 5;         // x is initialized
			AddVal(ref x);        // x must be initialized.
			Console.WriteLine(x); // Displays "15"
		}

		private static void AddVal(ref Int32 v) {
			v += 10;  // This method can use the initialized value in v.
		}
#endif
#if V3
		public static void Main() {
			Int32 x;              // x is not initialized.

			// The following line fails to compile, producing 
			// error CS0165: Use of unassigned local variable 'x'.
			AddVal(ref x);

			Console.WriteLine(x);
		}

		private static void AddVal(ref Int32 v) {
			v += 10;  // This method can use the initialized value in v.
		}
#endif

   public static void Swap(ref Object a, ref Object b) {
      Object t = b;
      b = a;
      a = t;
   }

#if true
   public static void SomeMethod() {
      String s1 = "Jeffrey";
      String s2 = "Richter";
      Swap(ref s1, ref s2);
      Console.WriteLine(s1);  // Displays "Richter"
      Console.WriteLine(s2);  // Displays "Jeffrey"
   }
#endif

   public static void SomeMethod2() {
      String s1 = "Jeffrey";
      String s2 = "Richter";

      // Variables that are passed by reference 
      // must match what the method expects.
      Object o1 = s1, o2 = s2;
      Swap(ref o1, ref o2);

      // Now cast the objects back to strings.
      s1 = (String)o1;
      s2 = (String)o2;

      Console.WriteLine(s1);  // Displays "Richter"
      Console.WriteLine(s2);  // Displays "Jeffrey"
   }

   public static void Swap<T>(ref T a, ref T b) {
      T t = b;
      b = a;
      a = t;
   }
}

internal static class MethodsThatTakeVariableArguments {
   public static void Go() {
      // Displays "15"
      Console.WriteLine(Add(new Int32[] { 1, 2, 3, 4, 5 }));

      // Displays "15"
      Console.WriteLine(Add(1, 2, 3, 4, 5));

      // Displays "0"
      Console.WriteLine(Add());

      DisplayTypes(new Object(), new Random(), "Jeff", 5);
   }

   private static Int32 Add(params Int32[] values) {
      // NOTE: it is possible to pass the 'values' 
      // array to other methods if you want to.

      Int32 sum = 0;
      for (Int32 x = 0; x < values.Length; x++)
         sum += values[x];
      return sum;
   }


   private static void DisplayTypes(params Object[] objects) {
      foreach (Object o in objects)
         Console.WriteLine(o.GetType());
   }
}

///////////////////////////////////////////////////////////////////////////////

public sealed class Point {
   static void Add(Point p) { /* ... */ }
   static void Add(ref Point p) { /* ... */ }

   // 'Add' cannot define overloaded methods that differ only on ref and out
   // static void Add(out Point p) { /* ... */ }
}

////////////////////////////// End of File ////////////////////////////////////
