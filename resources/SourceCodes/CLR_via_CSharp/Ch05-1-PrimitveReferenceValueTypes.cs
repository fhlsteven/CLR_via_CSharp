/******************************************************************************
Module:  ReferenceVsValue.cs
Notices: Copyright (c) 2012 Jeffrey Richter
******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Dynamic;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;

///////////////////////////////////////////////////////////////////////////////

public static class Program {
   public static void Main() {
      PrimitiveDemo();
      BoxingDemo();
      ReferenceVsValue.Go();
      Boxing.Go();
      BoxingForInterfaceMethod.Go();
      MutateViaInterface.Go();
      DynamicDemo.Go();
   }

   private static void PrimitiveDemo() {
      // The following 4 lines generate identical IL code
      int a = new int();
      int b = 0;
      System.Int32 c = new System.Int32();
      Int32 d = 0;

      // Show that all variables contain 0
      Console.WriteLine("a = {0}, b = {1}, c = {2}, d = {3}",
         new Object[] { a, b, c, d });

      // Make all variables contain 5
      a = b = c = d = 5;
      // Show that all variables contain 5
      Console.WriteLine("a = {0}, b = {1}, c = {2}, d = {3}",
         new Object[] { a, b, c, d });
   }

   private static void BoxingDemo() {
      Int32 a = 5;  // Create an unboxed value type variable
      Object o = a;  // o refers to a boxed version of a
      a = 123;       // Changes the unboxed value to 123
      Console.WriteLine(a + ", " + (Int32)o);  // Displays "123, 5"
      Console.WriteLine(a + ", " + o);	         // Better

      Console.WriteLine(a);   // No boxing
   }

   private static class ReferenceVsValue {
      // Reference type (because of 'class')
      private class SomeRef { public Int32 x; }

      // Value type (because of 'struct')
      private struct SomeVal { public Int32 x; }

      public static void Go() {
         SomeRef r1 = new SomeRef();   // Allocated in heap
         SomeVal v1 = new SomeVal();   // Allocated on stack
         r1.x = 5;                     // Pointer dereference
         v1.x = 5;                     // Changed on stack
         Console.WriteLine(r1.x);      // Displays "5"
         Console.WriteLine(v1.x);      // Also displays "5"
         // The left side of Figure 5-2 reflects the situation
         // after the lines above have executed.

         SomeRef r2 = r1;              // Copies reference (pointer) only
         SomeVal v2 = v1;              // Allocate on stack & copies members
         r1.x = 8;                     // Changes r1.x and r2.x
         v1.x = 9;                     // Changes v1.x, not v2.x
         Console.WriteLine(r1.x);      // Displays "8"
         Console.WriteLine(r2.x);      // Displays "8"
         Console.WriteLine(v1.x);      // Displays "9"
         Console.WriteLine(v2.x);      // Displays "5"
         // The right side of Figure 5-2 reflects the situation 
         // after ALL the lines above have executed.
      }
   }

   private static class Boxing {
      public static void Go() {
         ArrayList a = new ArrayList();
         Point p;            // Allocate a Point (not in the heap).
         for (Int32 i = 0; i < 10; i++) {
            p.x = p.y = i;   // Initialize the members in the value type.
            a.Add(p);        // Box the value type and add the
            // reference to the Arraylist.
         }
      }

      // Declare a value type.
      private struct Point { public Int32 x, y;  }

      public static void Main2() {
         Int32 x = 5;
         Object o = x;         // Box x; o refers to the boxed object
         Int16 y = (Int16)o;   // Throws an InvalidCastException
      }

      public static void Main3() {
         Int32 x = 5;
         Object o = x;                 // Box x; o refers to the boxed object
         Int16 y = (Int16)(Int32)o;    // Unbox to the correct type and cast
      }

      public static void Main4() {
         Point p;
         p.x = p.y = 1;
         Object o = p;   // Boxes p; o refers to the boxed instance

         p = (Point)o;  // Unboxes o AND copies fields from boxed 
         // instance to stack variable
      }

      public static void Main5() {
         Point p;
         p.x = p.y = 1;
         Object o = p;   // Boxes p; o refers to the boxed instance

         // Change Point’s x field to 2
         p = (Point)o;  // Unboxes o AND copies fields from boxed 
         // instance to stack variable
         p.x = 2;        // Changes the state of the stack variable
         o = p;          // Boxes p; o refers to a new boxed instance
      }

      public static void Main6() {
         Int32 v = 5;            // Create an unboxed value type variable.
         Object o = v;            // o refers to a boxed Int32 containing 5.
         v = 123;                 // Changes the unboxed value to 123

         Console.WriteLine(v + ", " + (Int32)o);	// Displays "123, 5"
      }

      public static void Main7() {
         Int32 v = 5;                // Create an unboxed value type variable.
         Object o = v;               // o refers to the boxed version of v.

         v = 123;                    // Changes the unboxed value type to 123
         Console.WriteLine(v);       // Displays "123"

         v = (Int32)o;               // Unboxes and copies o into v
         Console.WriteLine(v);       // Displays "5"
      }

      public static void Main8() {
         Int32 v = 5;   // Create an unboxed value type variable.

#if INEFFICIENT
   // When compiling the following line, v is boxed 
   // three times, wasting time and memory.
   Console.WriteLine("{0}, {1}, {2}", v, v, v);
#else
         // The lines below have the same result, execute
         // much faster, and use less memory.
         Object o = v;  // Manually box v (just once).

         // No boxing occurs to compile the following line.
         Console.WriteLine("{0}, {1}, {2}", o, o, o);
#endif
      }
   }

   private static class BoxingAndInterfaces {
      private struct Point : ICloneable {
         public Int32 x, y;

         // Override ToString method inherited from System.ValueType
         public override String ToString() {
            return String.Format("({0}, {1})", x, y);
         }

         // Implementation of ICloneable’s Clone method
         public Object Clone() {
            return MemberwiseClone();
         }
      }

      public static void Go() {
         // Create an instance of the Point value type on the stack.
         Point p;

         // Initialize the instance’s fields.
         p.x = 10;
         p.y = 20;

         // p does NOT get boxed to call ToString.
         Console.WriteLine(p.ToString());

         // p DOES get boxed to call GetType.
         Console.WriteLine(p.GetType());

         // p does NOT get boxed to call Clone.
         // Clone returns an object that is unboxed,
         // and its fields are copied into p2.
         Point p2 = (Point)p.Clone();

         // p2 DOES get boxed, and the reference is placed in c.
         ICloneable c = p2;

         // c does NOT get boxed because it is already boxed.
         // Clone returns a reference to an object that is saved in o.
         Object o = c.Clone();

         // o is unboxed, and fields are copied into p.
         p = (Point)o;
      }
   }

   private static class BoxingForInterfaceMethod {
      private struct Point : IComparable {
         private Int32 m_x, m_y;

         // Constructor to easily initialize the fields
         public Point(Int32 x, Int32 y) {
            m_x = x;
            m_y = y;
         }

         // Override ToString method inherited from System.ValueType
         public override String ToString() {
            // Return the point as a string
            return String.Format("({0}, {1})", m_x, m_y);
         }

         // Implementation of type-safe CompareTo method
         public Int32 CompareTo(Point other) {
            // Use the Pythagorean Theorem to calculate 
            // which point is farther from the origin (0, 0)
            return Math.Sign(Math.Sqrt(m_x * m_x + m_y * m_y)
               - Math.Sqrt(other.m_x * other.m_x + other.m_y * other.m_y));
         }

         // Implementation of IComparable’s CompareTo method
         public Int32 CompareTo(Object o) {
            if (GetType() != o.GetType()) {
               throw new ArgumentException("o is not a Point");
            }
            // Call type-safe CompareTo method
            return CompareTo((Point)o);
         }
      }

      public static void Go() {
         // Create two Point instances on the stack.
         Point p1 = new Point(10, 10);
         Point p2 = new Point(20, 20);

         // p1 does NOT get boxed to call ToString (a virtual method).
         Console.WriteLine(p1.ToString());	// "(10, 10)"

         // p DOES get boxed to call GetType (a non-virtual method).
         Console.WriteLine(p1.GetType());	// "Point"

         // p1 does NOT get boxed to call CompareTo.
         // p2 does NOT get boxed because CompareTo(Point) is called.
         Console.WriteLine(p1.CompareTo(p2));	// "-1"

         // p1 DOES get boxed, and the reference is placed in c.
         IComparable c = p1;
         Console.WriteLine(c.GetType());	// "Point"

         // p1 does NOT get boxed to call CompareTo.
         // Since CompareTo is not being passed a Point variable, 
         // CompareTo(Object) is called which requires a reference to
         // a boxed Point. 
         // c does NOT get boxed because it already refers to a boxed Point.
         Console.WriteLine(p1.CompareTo(c));	// "0"

         // c does NOT get boxed because it already refers to a boxed Point.
         // p2 does get boxed because CompareTo(Object) is called.
         Console.WriteLine(c.CompareTo(p2));	// "-1"

         // c is unboxed, and fields are copied into p2.
         p2 = (Point)c;

         // Proves that the fields got copied into p2.
         Console.WriteLine(p2.ToString());	// "(10, 10)"
      }
   }

   private static class MutateViaInterface {
      // Interface defining a Change method
      private interface IChangeBoxedPoint {
         void Change(Int32 x, Int32 y);
      }

      // Point is a value type.
      private struct Point : IChangeBoxedPoint {
         private Int32 m_x, m_y;

         public Point(Int32 x, Int32 y) {
            m_x = x;
            m_y = y;
         }

         public void Change(Int32 x, Int32 y) {
            m_x = x; m_y = y;
         }

         public override String ToString() {
            return String.Format("({0}, {1})", m_x, m_y);
         }
      }

      public static void Go() {
         Point p = new Point(1, 1);

         Console.WriteLine(p);

         p.Change(2, 2);
         Console.WriteLine(p);

         Object o = p;
         Console.WriteLine(o);

         ((Point)o).Change(3, 3);
         Console.WriteLine(o);

         // Boxes p, changes the boxed object and discards it
         ((IChangeBoxedPoint)p).Change(4, 4);
         Console.WriteLine(p);

         // Changes the boxed object and shows it
         ((IChangeBoxedPoint)o).Change(5, 5);
         Console.WriteLine(o);
      }
   }

   private static class DynamicDemo {
      public static void Go() {
         ShowLoadedAssemblies("Assemblies loaded before use of dynamic");
         SimpleDynamic();
         ShowLoadedAssemblies("Assemblies loaded after simkple use of dynamic");

         Demo();
         ShowLoadedAssemblies("Assemblies loaded after all dynamic code runs");
         ExcelAutomation();
         DynamicStaticInvocations();
      }

      private static void ShowLoadedAssemblies(String caption) {
         Console.WriteLine(caption);
         foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            Console.WriteLine("   " + a.GetName().Name);
         Console.WriteLine();
      }

      private static Int32 SimpleDynamic() {
         return ((dynamic)0) + 0;
      }

      private static void Demo() {
         dynamic value;
         for (Int32 demo = 0; demo < 2; demo++) {
            value = (demo == 0) ? (dynamic)5 : (dynamic)"A";
            value = value + value;
            M(value);
         }

         Object o = 123;         // OK: Implicit cast from Int32 to Object
         //Int32 n1 = o;           // Error: No implicit cast from Object to Int32
         Int32 n2 = (Int32)o;   // OK: Explicit cast from Object to Int32

         dynamic d = 123;        // OK: Implicit cast from Int32 to dynamic
         Int32 n3 = d;           // OK: Implicit cast from dynamic to Int32

         try {
            var m = M(d);           // Note: 'var m' is the same as 'dynamic m'
         }
         catch (RuntimeBinderException) { }
         var x = (Int32)d;       // 'var x' is the same as 'Int32 x'
         var dt = new DateTime(d);  // 'vat dt' is the same as 'DateTime dt'
      }

      private static void M(Int32 n) { Console.WriteLine("M(Int32): " + n); }
      private static void M(String s) { Console.WriteLine("M(String): " + s); }


      /// <summary>Construct an instance of this class and cast the reference to 'dynamic' 
      /// to dynamically invoke a type's static members</summary>
      internal sealed class StaticMemberDynamicWrapper : DynamicObject {
         private readonly TypeInfo m_type;
         public StaticMemberDynamicWrapper(Type type) { m_type = type.GetTypeInfo(); }

         public override IEnumerable<String> GetDynamicMemberNames() {
            return m_type.DeclaredMembers.Select(mi => mi.Name);
         }

         public override bool TryGetMember(GetMemberBinder binder, out object result) {
            result = null;
            var field = FindField(binder.Name);
            if (field != null) { result = field.GetValue(null); return true; }

            var prop = FindProperty(binder.Name, true);
            if (prop != null) { result = prop.GetValue(null, null); return true; }
            return false;
         }

         public override bool TrySetMember(SetMemberBinder binder, object value) {
            var field = FindField(binder.Name);
            if (field != null) { field.SetValue(null, value); return true; }

            var prop = FindProperty(binder.Name, false);
            if (prop != null) { prop.SetValue(null, value, null); return true; }
            return false;
         }

         public override Boolean TryInvokeMember(InvokeMemberBinder binder, Object[] args, out Object result) {
            MethodInfo method = FindMethod(binder.Name, args.Select(a => a.GetType()).ToArray());
            if (method == null) { result = null; return false; }
            result = method.Invoke(null, args);
            return true;
         }

         private MethodInfo FindMethod(String name, Type[] paramTypes) {
            return m_type.DeclaredMethods.FirstOrDefault(mi => mi.IsPublic && mi.IsStatic && mi.Name == name && ParametersMatch(mi.GetParameters(), paramTypes));
         }

         private Boolean ParametersMatch(ParameterInfo[] parameters, Type[] paramTypes) {
            if (parameters.Length != paramTypes.Length) return false;
            for (Int32 i = 0; i < parameters.Length; i++)
               if (parameters[i].ParameterType != paramTypes[i]) return false;
            return true;
         }

         private FieldInfo FindField(String name) {
            return m_type.DeclaredFields.FirstOrDefault(fi => fi.IsPublic && fi.IsStatic && fi.Name == name);
         }

         private PropertyInfo FindProperty(String name, Boolean get) {
            if (get)
               return m_type.DeclaredProperties.FirstOrDefault(
                  pi => pi.Name == name && pi.GetMethod != null &&
                  pi.GetMethod.IsPublic && pi.GetMethod.IsStatic);

            return m_type.DeclaredProperties.FirstOrDefault(
               pi => pi.Name == name && pi.SetMethod != null &&
                  pi.SetMethod.IsPublic && pi.SetMethod.IsStatic);
         }
      }

      private static class StaticTestType {
         public static String Method(Int32 x) { return x.ToString(); }
#pragma warning disable 649 // Field is never assigned to, and will always have its default value
         public static DateTime Field;
#pragma warning restore 649
         public static Guid Property { get; set; }
      }

      private static void DynamicStaticInvocations() {
         dynamic staticType = new StaticMemberDynamicWrapper(typeof(String));
         Console.WriteLine(staticType.Concat("A", "B"));

         staticType = new StaticMemberDynamicWrapper(typeof(StaticTestType));
         Console.WriteLine(staticType.Method(5));
         staticType.Field = DateTime.Now;
         Console.WriteLine(staticType.Field);
         staticType.Property = Guid.NewGuid();
         Console.WriteLine(staticType.Property);
      }
   }

   private static void ExcelAutomation() {
#if ReferencingExcel // Microsoft.Office.Interop.Excel.dll
      var excel = new Microsoft.Office.Interop.Excel.Application();
      excel.Visible = true;
      excel.Workbooks.Add(Type.Missing);
      ((Range)excel.Cells[1, 1]).Value = "Text in cell A1";  // Put a string in cell A1
      excel.Cells[1, 1].Value = "Text in cell A1";  // Put a string in cell A1
#endif
   }
}


//////////////////////////////// End of File //////////////////////////////////
