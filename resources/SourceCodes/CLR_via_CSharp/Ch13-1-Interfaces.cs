#define EIMITypeSafety_Version3
#define EIMINoBaseCall_Version2
#if !DEBUG
#pragma warning disable 219
#endif

using System;

public static class Interfaces {
   public static void Main() {
      ImplementingAnInterface.Go();
      InterfaceReimplementation.Go();
      ExplicitInterfaceMethodImpl.Go();
      GenericsAndInterfaces.Go();
      UsingEIMI.Go();
      EIMITypeSafety.Go();
      EIMINoBaseCall.Go();
   }
}

internal static class ImplementingAnInterface {
   public static void Go() {
      Point[] points = new Point[] {
         new Point(3, 3),
         new Point(1, 2),
      };

      if (points[0].CompareTo(points[1]) > 0) {
         Point tempPoint = points[0];
         points[0] = points[1];
         points[1] = tempPoint;
      }
      Console.WriteLine("Points from closest to (0, 0) to farthest:");
      foreach (Point p in points)
         Console.WriteLine(p);
   }

   // Point is derived from System.Object and implements IComparable<T>.
   public sealed class Point : IComparable<Point> {
      private Int32 m_x, m_y;

      public Point(Int32 x, Int32 y) {
         m_x = x;
         m_y = y;
      }

      // This method implements IComparable<T> for Point
      public Int32 CompareTo(Point other) {
         return Math.Sign(Math.Sqrt(m_x * m_x + m_y * m_y)
            - Math.Sqrt(other.m_x * other.m_x + other.m_y * other.m_y));
      }

      public override String ToString() {
         return String.Format("({0}, {1})", m_x, m_y);
      }
   }
}

internal static class InterfaceReimplementation {
   public static void Go() {
      /************************* First Example *************************/
      Base b = new Base();

      // Call's Dispose by using b's type: "Base's Dispose"
      b.Dispose();

      // Call's Dispose by using b's object's type: "Base's Dispose"
      ((IDisposable)b).Dispose();


      /************************* Second Example ************************/
      Derived d = new Derived();

      // Call's Dispose by using d's type: "Derived's Dispose"
      d.Dispose();

      // Call's Dispose by using d's object's type: "Derived's Dispose"
      ((IDisposable)d).Dispose();


      /************************* Third Example *************************/
      b = new Derived();

      // Call's Dispose by using b's type: "Base's Dispose"
      b.Dispose();

      // Call's Dispose by using b's object's type: "Derived's Dispose"
      ((IDisposable)b).Dispose();
   }

   // This class is derived from Object and it implements IDisposable
   internal class Base : IDisposable {
      // This method is implicitly sealed and cannot be overridden
      public void Dispose() {
         Console.WriteLine("Base's Dispose");
      }
   }

   // This class is derived from Base and it re-implements IDisposable
   internal class Derived : Base, IDisposable {
      // This method cannot override Base's Dispose. 'new' is used to indicate 
      // that this method re-implements IDisposable's Dispose method
      new public void Dispose() {
         Console.WriteLine("Derived's Dispose");

         // NOTE: The next line shows how to call a base class's implementation (if desired)
         // base.Dispose();
      }
   }
}

internal static class ExplicitInterfaceMethodImpl {
   public static void Go() {
      SimpleType st = new SimpleType();

      // This calls the public Dispose method implementation
      st.Dispose();

      // This calls IDisposable's Dispose method implementation
      IDisposable d = st;
      d.Dispose();
   }

   public sealed class SimpleType : IDisposable {
#if false
	public void Dispose() { Console.WriteLine("Dispose"); }
#else
      public void Dispose() { Console.WriteLine("public Dispose"); }
      void IDisposable.Dispose() { Console.WriteLine("IDisposable Dispose"); }
#endif
   }
}

internal static class GenericsAndInterfaces {
   public static void Go() {
      Number n = new Number();

      // Here, I compare the value in n with an Int32 (5)
      IComparable<Int32> cInt32 = n;
      Int32 result = cInt32.CompareTo(5);

      // Here, I compare the value in n with a String ("5")
      IComparable<String> cString = n;
      result = cString.CompareTo("5");
   }

   // This class implements the generic IComparable<T> interface twice
   public sealed class Number : IComparable<Int32>, IComparable<String> {
      private Int32 m_val = 5;

      // This method implements IComparable<Int32>’s CompareTo
      public Int32 CompareTo(Int32 n) {
         return m_val.CompareTo(n);
      }

      // This method implements IComparable<String>’s CompareTo
      public Int32 CompareTo(String s) {
         return m_val.CompareTo(Int32.Parse(s));
      }
   }

   private static void SomeMethod1() {
      Int32 x = 1, y = 2;
      IComparable c = x;

      // CompareTo expects an Object; passing y (an Int32) is OK
      c.CompareTo(y);     // Boxing occurs here

      // CompareTo expects an Object; passing "2" (a String) compiles
      // but an ArgumentException is thrown at runtime
      c.CompareTo("2");
   }

   private static void SomeMethod2() {
      Int32 x = 1, y = 2;
      IComparable<Int32> c = x;

      // CompareTo expects an Int32; passing y (an Int32) is OK
      c.CompareTo(y);     // Boxing occurs here

      // CompareTo expects an Int32; passing "2" (a String) results
      // in a compiler error indicating that String cannot be cast to an Int32
      // c.CompareTo("2");
   }

   public sealed class SomeType {
      private static void Test() {
         Int32 x = 5;

         // This call to M compiles fine because 
         // Int32 implements IComparable AND IConvertible
         M(x);

         // This call to M causes a compiler error because 
         // Guid implements IComparable but it does not implement IConvertible
         // Guid g = new Guid();
         // M(g);
      }

      // M’s type parameter, T, is constrained to work only with types that
      // implement both the IComparable AND IConvertible interfaces
      private static Int32 M<T>(T t) where T : IComparable, IConvertible {
         // ...
         return 0;
      }
   }
}

internal static class UsingEIMI {
   public static void Go() {
      MarioPizzeria mp = new MarioPizzeria();

      // This line calls MarioPizzeria’s public GetMenu method
      mp.GetMenu();

      // These lines call MarioPizzeria’s IWindow.GetMenu method
      IWindow window = mp;
      window.GetMenu();

      // These lines call MarioPizzeria’s IRestaurant.GetMenu method
      IRestaurant restaurant = mp;
      restaurant.GetMenu();
   }

   public interface IWindow {
      Object GetMenu();
   }

   public interface IRestaurant {
      Object GetMenu();
   }

   // This type is derived from System.Object and 
   // implements the IWindow and IRestaurant interfaces.
   public class MarioPizzeria : IWindow, IRestaurant {

      // This is the implementation for IWindow’s GetMenu method.
      Object IWindow.GetMenu() {
         // ...
         return null;
      }

      // This is the implementation for IRestaurant’s GetMenu method.
      Object IRestaurant.GetMenu() {
         // ...
         return null;
      }

      // This (optional method) is a GetMenu method that has nothing 
      // to do with an interface.
      public Object GetMenu() {
         // ...
         return null;
      }
   }
}

public static class EIMITypeSafety {
   internal struct SomeValueType : IComparable {
      private Int32 m_x;
      public SomeValueType(Int32 x) { m_x = x; }

#if EIMITypeSafety_Version1
   public Int32 CompareTo(Object other) {
      return (m_x - ((SomeValueType)other).m_x);
   }
#else
      public Int32 CompareTo(SomeValueType other) {
         return (m_x - other.m_x);
      }

      // NOTE: No public/private used on the next line
      Int32 IComparable.CompareTo(Object other) {
         return CompareTo((SomeValueType)other);
      }
#endif
   }

   public static void Go() {
#if EIMITypeSafety_Version1
      SomeValueType v = new SomeValueType(0);
      Object o = new Object();
      Int32 n = v.CompareTo(v);  // Undesired boxing
      n = v.CompareTo(o);        // InvalidCastException
#endif

#if EIMITypeSafety_Version2
      SomeValueType v = new SomeValueType(0);
      Object o = new Object();
      Int32  n = v.CompareTo(v); // No boxing
      n = v.CompareTo(o);        // compile-time error 
#endif

#if EIMITypeSafety_Version3
      SomeValueType v = new SomeValueType(0);
      IComparable c = v;          // Boxing!

      Object o = new Object();
      Int32 n = c.CompareTo(v);         // Undesired boxing
      n = c.CompareTo(o);   // InvalidCastException
#endif
   }
}

internal static class EIMINoBaseCall {
#if EIMINoBaseCall_Version1
internal class Base : IComparable {
   // Explicit Interface Method Implementation
   Int32 IComparable.CompareTo(Object o) {
      Console.WriteLine("Base's CompareTo");
      return 0;
   }
}

internal class Derived : Base, IComparable {
   // A public method that is also the interface implementation
   public Int32 CompareTo(Object o) {
      Console.WriteLine("Derived's CompareTo");

      // This attempt to call the base class's EIMI causes a compiler error:
      // error CS0117: 'Base' does not contain a definition for 'CompareTo'
      base.CompareTo(o);
      return 0;
   }
}
#endif

#if EIMINoBaseCall_Version2
   internal class Base : IComparable {

      // Explicit Interface Method Implementation
      Int32 IComparable.CompareTo(Object o) {
         Console.WriteLine("Base's IComparable CompareTo");
         return CompareTo(o);   // This now calls the virtual method
      }

      // Virtual method for derived classes (this method could have any name)
      public virtual Int32 CompareTo(Object o) {
         Console.WriteLine("Base's virtual CompareTo");
         return 0;
      }
   }

   internal class Derived : Base, IComparable {

      // A public method that is also the interface implementation
      public override Int32 CompareTo(Object o) {
         Console.WriteLine("Derived's CompareTo");

         // Now, we can call Base's virtual method
         base.CompareTo(o);
         return 0;
      }
   }
#endif

   public static void Go() {
      Derived d = new Derived();
      d.CompareTo(null);
   }
}