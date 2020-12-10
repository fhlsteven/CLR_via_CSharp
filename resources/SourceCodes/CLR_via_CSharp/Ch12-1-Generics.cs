#if !DEBUG
#pragma warning disable 660, 661, 219
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public static class Generics {
   public static void Main() {
      Performance.ValueTypePerfTest();
      Performance.ReferenceTypePerfTest();
      GenericArray();
      OpenTypes.Go();
      GenericInheritance.Go();
      Constraints.Go();
   }

   private static void GenericArray() {
      // Create & initialize a byte array
      Byte[] byteArray = new Byte[] { 5, 1, 4, 2, 3 };

      // Call Byte[] sort algorithm
      Array.Sort<Byte>(byteArray);

      // Call Byte[] binary search algorithm
      Int32 i = Array.BinarySearch<Byte>(byteArray, 1);
      Console.WriteLine(i);   // Displays "0"
   }

}

internal static class Performance {
   public static void ValueTypePerfTest() {
      const Int32 count = 100000000;

      using (new OperationTimer("List<Int32>")) {
         List<Int32> l = new List<Int32>();
         for (Int32 n = 0; n < count; n++) {
            l.Add(n); 		  // No boxing
            Int32 x = l[n];  // No unboxing
         }
         l = null;  // Make sure this gets GC'd 
      }

      using (new OperationTimer("ArrayList of Int32")) {
         ArrayList a = new ArrayList();
         for (Int32 n = 0; n < count; n++) {
            a.Add(n); 		         // Boxing
            Int32 x = (Int32)a[n]; 	// Unboxing
         }
         a = null;  // Make sure this gets GC'd 
      }
   }

   public static void ReferenceTypePerfTest() {
      const Int32 count = 100000000;

      using (new OperationTimer("List<String>")) {
         List<String> l = new List<String>();
         for (Int32 n = 0; n < count; n++) {
            l.Add("X"); 		  // Reference copy
            String x = l[n]; 	  // Reference copy
         }
         l = null;  // Make sure this gets GC'd 
      }

      using (new OperationTimer("ArrayList of String")) {
         ArrayList a = new ArrayList();
         for (Int32 n = 0; n < count; n++) {
            a.Add("X"); 		        // Reference copy
            String x = (String)a[n];  // Cast check & reference copy
         }
         a = null;  // Make sure this gets GC'd 
      }
   }

   // This is useful for doing operation performance timing.
   private sealed class OperationTimer : IDisposable {
      private Stopwatch m_stopwatch;
      private String m_text;
      private Int32 m_collectionCount;

      public OperationTimer(String text) {
         PrepareForOperation();

         m_text = text;
         m_collectionCount = GC.CollectionCount(0);

         // This should be the last statement in this 
         // method to keep timing as accurate as possible
         m_stopwatch = Stopwatch.StartNew();
      }

      public void Dispose() {
         Console.WriteLine("{0} (GCs={1,3}) {2}",
            (m_stopwatch.Elapsed),
            GC.CollectionCount(0) - m_collectionCount, m_text);
      }

      private static void PrepareForOperation() {
         GC.Collect();
         GC.WaitForPendingFinalizers();
         GC.Collect();
      }
   }
}

internal static class OpenTypes {
   public static void Go() {
      Object o = null;

      // Dictionary<,> is an open type having 2 type parameters
      Type t = typeof(Dictionary<,>);

      // Try to create an instance of this type (fails)
      o = CreateInstance(t);
      Console.WriteLine();

      // DictionaryStringKey<> is an open type having 1 type parameter
      t = typeof(DictionaryStringKey<>);

      // Try to create an instance of this type (fails)
      o = CreateInstance(t);
      Console.WriteLine();

      // DictionaryStringKey<Guid> is a closed type
      t = typeof(DictionaryStringKey<Guid>);

      // Try to create an instance of this type (succeeds) 
      o = CreateInstance(t);

      // Prove it actually worked
      Console.WriteLine("Object type=" + o.GetType());
   }

   private static Object CreateInstance(Type t) {
      Object o = null;
      try {
         o = Activator.CreateInstance(t);
         Console.Write("Created instance of {0}", t.ToString());
      }
      catch (ArgumentException e) {
         Console.WriteLine(e.Message);
      }
      return o;
   }

   // A partially specified open type
   internal sealed class DictionaryStringKey<TValue> :
      Dictionary<String, TValue> {
   }
}

internal static class GenericInheritance {
   public static void Go() {
      SameDataLinkedList();
      DifferentDataLinkedList();
   }

   private static void SameDataLinkedList() {
      Node<Char> head = new Node<Char>('C');
      head = new Node<Char>('B', head);
      head = new Node<Char>('A', head);
      Console.WriteLine(head.ToString());
   }

   private static void DifferentDataLinkedList() {
      Node head = new TypedNode<Char>('.');
      head = new TypedNode<DateTime>(DateTime.Now, head);
      head = new TypedNode<String>("Today is ", head);
      Console.WriteLine(head.ToString());
   }

   private sealed class Node<T> {
      public T m_data;
      public Node<T> m_next;

      public Node(T data)
         : this(data, null) {
      }

      public Node(T data, Node<T> next) {
         m_data = data; m_next = next;
      }

      public override String ToString() {
         return m_data.ToString() +
            ((m_next != null) ? m_next.ToString() : null);
      }
   }

   private class Node {
      protected Node m_next;

      public Node(Node next) {
         m_next = next;
      }
   }

   private sealed class TypedNode<T> : Node {
      public T m_data;

      public TypedNode(T data)
         : this(data, null) {
      }

      public TypedNode(T data, Node next)
         : base(next) {
         m_data = data;
      }

      public override String ToString() {
         return m_data.ToString() +
            ((m_next != null) ? m_next.ToString() : null);
      }
   }

   private sealed class GenericTypeThatRequiresAnEnum<T> {
      static GenericTypeThatRequiresAnEnum() {
         if (!typeof(T).IsEnum) {
            throw new ArgumentException("T must be an enumerated type");
         }
      }
   }
}

internal static class GenericMethods {
   public static void Go() {
      GenericType<String> gts = new GenericType<String>("123");
      Int32 n = gts.Converter<Int32>();

      CallingSwap();
      CallingSwapUsingInference();

      Display("Jeff");           // Calls Display(String)
      Display(123);              // Calls Display<T>(T)
      Display<String>("Aidan");  // Calls Display<T>(T)
   }

   private static void Swap<T>(ref T o1, ref T o2) {
      T temp = o1;
      o1 = o2;
      o2 = temp;
   }

   private static void CallingSwap() {
      Int32 n1 = 1, n2 = 2;
      Console.WriteLine("n1={0}, n2={1}", n1, n2);
      Swap<Int32>(ref n1, ref n2);
      Console.WriteLine("n1={0}, n2={1}", n1, n2);

      String s1 = "Aidan", s2 = "Kristin";
      Console.WriteLine("s1={0}, s2={1}", s1, s2);
      Swap<String>(ref s1, ref s2);
      Console.WriteLine("s1={0}, s2={1}", s1, s2);
   }

   static void CallingSwapUsingInference() {
      Int32 n1 = 1, n2 = 2;
      Swap(ref n1, ref n2);	// Calls Swap<Int32>

      // String s1 = "Aidan";
      // Object s2 = "Kristin";
      // Swap(ref s1, ref s2);	// Error, type can't be inferred
   }

   private static void Display(String s) {
      Console.WriteLine(s);
   }

   private static void Display<T>(T o) {
      Display(o.ToString());  // Calls Display(String)
   }

   internal sealed class GenericType<T> {
      private T m_value;

      public GenericType(T value) { m_value = value; }

      public TOutput Converter<TOutput>() {
         TOutput result = (TOutput)Convert.ChangeType(m_value, typeof(TOutput));
         return result;
      }
   }
}

internal static class Constraints {
   public static void Go() {
      Boolean b = ComparingGenericVariables.Op.M<ComparingGenericVariables.Op>(null, null);
   }

   private static Boolean MethodTakingAnyType<T>(T o) {
      T temp = o;
      Console.WriteLine(o.ToString());
      Boolean b = temp.Equals(o);
      return b;
   }

   private static T Min<T>(T o1, T o2) where T : IComparable<T> {
      if (o1.CompareTo(o2) < 0) return o1;
      return o2;
   }

   private static void CallMin() {
      Object o1 = "Jeff", o2 = "Richter";
      //Object oMin = Min<Object>(o1, o2);	// Error CS0309
   }

   internal sealed class OverloadingByArity {
      // It is OK to define the following types:
      internal sealed class AType { }
      internal sealed class AType<T> { }
      internal sealed class AType<T1, T2> { }

      // Error: conflicts with AType<T> that has no constraints
      //internal sealed class AType<T> where T : IComparable<T> { }

      // Error: conflicts with AType<T1, T2>
      //internal sealed class AType<T3, T4> { }

      internal sealed class AnotherType {
         // It is OK to define the following methods:
         private static void M() { }
         private static void M<T>() { }
         private static void M<T1, T2>() { }

         // Error: conflicts with M<T> that has no constraints
         //private static void M<T>() where T : IComparable<T> { }

         // Error: conflicts with M<T1, T2>
         //private static void M<T3, T4>() { }
      }
   }

   internal static class OverridingVirtualGenericMethod {
      internal class Base {
         public virtual void M<T1, T2>()
            where T1 : struct
            where T2 : class {
         }
      }

      internal sealed class Derived : Base {
         public override void M<T3, T4>()
            /*where T3 : struct 
            where T4 : class  */
         {
         }
      }
   }

   internal static class PrimaryConstraints {
      internal static class PrimaryConstraintOfStream<T> where T : Stream {
         public static void M(T stream) {
            stream.Close();   // OK
         }
      }

      internal static class PrimaryConstraintOfClass<T> where T : class {
         public static void M() {
#pragma warning disable 219
            T temp = null;	// Allowed because T must be a reference type
#pragma warning restore 219
         }
      }

      internal static class PrimaryConstraintOfStruct<T> where T : struct {
         public static T Factory() {
            // Allowed because all value types implicitly 
            // have a public, parameterless constructor
            return new T();
         }
      }
   }

   internal static class SecondaryConstraints {
      private static List<TBase> ConvertIList<T, TBase>(IList<T> list)
         where T : TBase {

         List<TBase> baseList = new List<TBase>(list.Count);
         for (Int32 index = 0; index < list.Count; index++) {
            baseList.Add(list[index]);
         }
         return baseList;
      }

      private static void CallingConvertIList() {
         // Construct and initialize a List<String> (which implements IList<String>)
         IList<String> ls = new List<String>();
         ls.Add("A String");

         // Convert the IList<String> to an IList<Object>
         IList<Object> lo = ConvertIList<String, Object>(ls);

         // Convert the IList<String> to an IList<IComparable>
         IList<IComparable> lc = ConvertIList<String, IComparable>(ls);

         // Convert the IList<String> to an IList<IComparable<String>>
         IList<IComparable<String>> lcs =
            ConvertIList<String, IComparable<String>>(ls);

         // Convert the IList<String> to an IList<Exception>
         //IList<Exception> le = ConvertIList<String, Exception>(ls);	// Error
      }
   }

   internal sealed class ConstructorConstraints {
      internal sealed class ConstructorConstraint<T> where T : new() {
         public static T Factory() {
            // Allowed because all value types implicitly 
            // have a public, parameterless constructor and because
            // the constraint requires that any specified reference 
            // type also have a public, parameterless constructor
            return new T();
         }
      }
   }

   internal sealed class CastingAGenericTypeVariable {
      private void CastingAGenericTypeVariable1<T>(T obj) {
         //Int32 x = (Int32)obj;    // Error
         //String s = (String)obj;  // Error
      }
      private void CastingAGenericTypeVariable2<T>(T obj) {
         Int32 x = (Int32)(Object)obj;    // No error
         String s = (String)(Object)obj;  // No error
      }
      private void CastingAGenericTypeVariable3<T>(T obj) {
         String s = obj as String;  // No error
      }
   }

   internal sealed class SettingAGenericTypeVariableToADefaultValue {
      private void SettingAGenericTypeVariableToNull<T>() {
         //T temp = null;	// Error, a value type can’t be set to null
      }

      private void SettingAGenericTypeVariableToDefaultValue<T>() {
#pragma warning disable 219
         T temp = default(T);	// OK
#pragma warning restore 219
      }
   }

   internal sealed class ComparingGenericVariables {
      private void ComparingAGenericTypeVariableWithNull<T>(T obj) {
         if (obj == null) { /* Never executes for a value type */ }
      }
      private void ComparingTwoGenericTypeVariables<T>(T o1, T o2) {
         //if (o1 == o2) { }	// Error
      }

#pragma warning disable 660, 661
      internal class Op {
         public static Boolean operator ==(Op o1, Op o2) { return true; }
         public static Boolean operator !=(Op o1, Op o2) { return false; }
         public static Boolean M<T>(T o1, T o2) where T : Op {
            return o1 == o2;
         }
      }
#pragma warning restore 660, 661
   }
}
#if false
internal static class UsingGenericTypeVariablesAsOperands {
   private T Sum<T>(T num) where T : struct {
      T sum = default(T);
      for (T n = default(T); n < num; n++)
         sum += n;
      return sum;
   }
}
#endif
