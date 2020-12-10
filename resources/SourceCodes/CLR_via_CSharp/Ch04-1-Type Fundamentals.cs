using System;

// This type is implicitly derived from System.Object.
internal class Employee /* : System.Object */ {
}

internal class Manager : Employee {
}

public static class Program {
   public static void Main() {
      // No cast needed since new returns an Employee object
      // and Object is a base type of Employee.
      Object o = new Employee();

      // Cast required since Employee is derived from Object.
      // Other languages (such as Visual Basic) might not require 
      // this cast to compile.
      Employee e = (Employee) o;
   }

   public static void Main2() {
      // Construct a Manager object and pass it to PromoteEmployee.
      // A Manager IS-A Object: PromoteEmployee runs OK.
      Manager m = new Manager();
      PromoteEmployee(m);

      // Construct a DateTime object and pass it to PromoteEmployee.
      // A DateTime is NOT derived from Employee. PromoteEmployee 
      // throws a System.InvalidCastException exception. 
      DateTime newYears = new DateTime(2007, 1, 1);
      PromoteEmployee(newYears);
   }


   public static void PromoteEmployee(Object o) {
      // At this point, the compiler doesn’t know exactly what
      // type of object o refers to. So the compiler allows the 
      // code to compile. However, at run time, the CLR does know 
      // what type o refers to (each time the cast is performed) and
      // it checks whether the object’s type is Employee or any type
      // that is derived from Employee.
      Employee e = (Employee) o;
   }

   public static void PromoteEmployee2(Object o) {
      if (o is Employee) {
         Employee e = (Employee)o;
         // Use e within the remainder of the 'if' statement. 
      }
   }
   public static void PromoteEmployee3(Object o) {
      Employee e = o as Employee;
      if (e != null) {
         // Use e within the 'if' statement.
      }
   }

   internal class B { // Base class
   }
   internal class D : B { // Derived class
   }

   private static void Main3() { // For Table 4-3 in the book
      Object o1 = new Object();
      Object o2 = new B();
      Object o3 = new D();
      Object o4 = o3;
      B b1 = new B();
      B b2 = new D();
      D d1 = new D();
      //B b3 = new Object();
      //D d2 = new Object();
      B b4 = d1;
      //D d3 = b2;
      D d4 = (D)d1;
      D d5 = (D)b2;
      D d6 = (D)b1;  // Throws InvalidCastException
      B b5 = (B)o1;  // Throws InvalidCastException
      B b6 = (D)b2;
   }
}
