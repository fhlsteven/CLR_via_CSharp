/******************************************************************************
Module:  Arrays.cs
Notices: Copyright (c) 2013 Jeffrey Richter
******************************************************************************/

using System;
using System.Diagnostics;
using System.IO;

///////////////////////////////////////////////////////////////////////////////


public static class Program {
   public static void Main() {
      ArrayIntro();
      ArrayDemo();
      ArrayCasting();
      DynamicArrays.Go();
      ArrayTypes.Go();
      MultiDimArrayPerformance.Go();
      StackallocAndInlineArrays.Go();
   }

   private static void ArrayIntro() {
      String[] sa = new String[1];
      Array a1 = Array.CreateInstance(typeof(String), new Int32[] { 1 }, new Int32[] { 0 });
      Array a2 = Array.CreateInstance(typeof(String), new Int32[] { 1 }, new Int32[] { 1 });
      Console.WriteLine(sa.GetType().ToString());
      Console.WriteLine(a1.GetType().ToString());
      Console.WriteLine(a2.GetType().ToString());
   }

   private static void ArrayRankInfo(String name, Array a) {
      Console.WriteLine("Number of dimensions in \"{0}\" array (of type {1}): ",
         name, a.GetType().ToString(), a.Rank);
      for (int r = 0; r < a.Rank; r++) {
         Console.WriteLine("Rank: {0}, LowerBound = {1},  UpperBound = {2}",
            r, a.GetLowerBound(r), a.GetUpperBound(r));
      }
      Console.WriteLine();
   }

   private static void ArrayDemo() {
      Int32[] arrayOfInt32s = new Int32[10];
      ArrayRankInfo("arrayOfInt32s", arrayOfInt32s);

      String[,] matrixOfStrings = new String[10, 20];
      ArrayRankInfo("matrixOfStrings", matrixOfStrings);

      // Create an array of 3 Int32s: lower bound= -2, upper bound = 0
      Array arrayOfObjects = Array.CreateInstance(typeof(Object), new Int32[] { 3 }, new Int32[] { -2 });
      ArrayRankInfo("arrayOfObjects", arrayOfObjects);

      arrayOfObjects.SetValue(10, -2);
      arrayOfObjects.SetValue(20, -1);
      arrayOfObjects.SetValue(30, +0);
      Console.WriteLine("Array elements: a[-2] = {0}, a[-1] = {1}, a[0] = {2}",
         arrayOfObjects.GetValue(-2), arrayOfObjects.GetValue(-1), arrayOfObjects.GetValue(0));
   }

   private static void ArrayCasting() {
      // Create a 2-dim FileStream array
      FileStream[,] fs2dim = new FileStream[5, 10];

      // Implicit cast to a 2-dim Object array
      Object[,] o2dim = fs2dim;

      // Can't cast from 2-dim array to 1-dim array
      // Compiler error CS0030: Cannot convert type 'object[*,*]' to 'System.IO.Stream[]'
      //Stream[] s1dim = (Stream[]) o2dim;

      // Explicit cast to 2-dim Stream array
      Stream[,] s2dim = (Stream[,])o2dim;

      // Explicit cast to 2-dim Type array 
      // Compiles but throws InvalidCastException at runtime
      try {
         Type[,] t2dim = (Type[,])o2dim;
      }
      catch (InvalidCastException) {
      }

      // Create a 1-dim Int32 array (value types)
      Int32[] i1dim = new Int32[5];

      // Can't cast from array of value types to anything else
      // Compiler error CS0030: Cannot convert type 'int[]' to 'object[]'
      // Object[] o1dim = (Object[]) i1dim;

      // However, Array.Copy knows how to coerce an array 
      // of value types to an array of boxed references
      Object[] o1dim = new Object[i1dim.Length];
      Array.Copy(i1dim, o1dim, 0);
   }

   private static void DynamicArray() {
      // We want a 2-dim array
      Int32[] lowerBounds = { 1995, 1 };
      Int32[] lengths = { 10, 4 };
      Decimal[,] quarterlyRevenue = (Decimal[,])
         Array.CreateInstance(typeof(Decimal), lengths, lowerBounds);

      Int32 firstYear = quarterlyRevenue.GetLowerBound(0);
      Int32 lastYear = quarterlyRevenue.GetUpperBound(0);
      Console.WriteLine("{0,4}  {1,9}  {2,9}  {3,9}  {4,9}", "Year", "Q1", "Q2", "Q3", "Q4");

      Random r = new Random();
      for (Int32 year = firstYear; year <= lastYear; year++) {
         Console.Write(year + "  ");

         for (Int32 quarter = quarterlyRevenue.GetLowerBound(1); quarter <= quarterlyRevenue.GetUpperBound(1); quarter++) {
            quarterlyRevenue[year, quarter] = r.Next(10000);

            Console.Write("{0,9:C}  ", quarterlyRevenue[year, quarter]);
         }
         Console.WriteLine();
      }
   }
}

internal static class DynamicArrays {
   public static void Go() {
      // I want a two-dimension array [2005..2009][1..4].
      Int32[] lowerBounds = { 2005, 1 };
      Int32[] lengths = { 5, 4 };
      Decimal[,] quarterlyRevenue = (Decimal[,])
      Array.CreateInstance(typeof(Decimal), lengths, lowerBounds);

      Console.WriteLine("{0,4}  {1,9}  {2,9}  {3,9}  {4,9}",
         "Year", "Q1", "Q2", "Q3", "Q4");
      Int32 firstYear = quarterlyRevenue.GetLowerBound(0);
      Int32 lastYear = quarterlyRevenue.GetUpperBound(0);
      Int32 firstQuarter = quarterlyRevenue.GetLowerBound(1);
      Int32 lastQuarter = quarterlyRevenue.GetUpperBound(1);

      for (Int32 year = firstYear; year <= lastYear; year++) {
         Console.Write(year + "  ");
         for (Int32 quarter = firstQuarter; quarter <= lastQuarter; quarter++) {
            Console.Write("{0,9:C}  ", quarterlyRevenue[year, quarter]);
         }
         Console.WriteLine();
      }
   }
}

internal static class ArrayTypes {
   public static void Go() {
      Array a;

      // Create a 1-dim, 0-based array, with no elements in it
      a = new String[0];
      Console.WriteLine(a.GetType());	// System.String[]

      // Create a 1-dim, 0-based array, with no elements in it
      a = Array.CreateInstance(typeof(String), new Int32[] { 0 }, new Int32[] { 0 });
      Console.WriteLine(a.GetType());	// System.String[]

      // Create a 1-dim, 1-based array, with no elements in it
      a = Array.CreateInstance(typeof(String), new Int32[] { 0 }, new Int32[] { 1 });
      Console.WriteLine(a.GetType());	// System.String[*]  <-- INTERESTING!

      Console.WriteLine();

      // Create a 2-dim, 0-based array, with no elements in it
      a = new String[0, 0];
      Console.WriteLine(a.GetType());	// System.String[,]

      // Create a 2-dim, 0-based array, with no elements in it
      a = Array.CreateInstance(typeof(String), new Int32[] { 0, 0 }, new Int32[] { 0, 0 });
      Console.WriteLine(a.GetType());	// System.String[,]

      // Create a 2-dim, 1-based array, with no elements in it
      a = Array.CreateInstance(typeof(String), new Int32[] { 0, 0 }, new Int32[] { 1, 1 });
      Console.WriteLine(a.GetType());	// System.String[,]
   }
}

internal static class MultiDimArrayPerformance {
   private const Int32 c_numElements = 10000;

   public static void Go() {
      // Declare a 2-dimensional array
      Int32[,] a2Dim = new Int32[c_numElements, c_numElements];

      // Declare a 2-dimensional array as a jagged array (a vector of vectors)
      Int32[][] aJagged = new Int32[c_numElements][];
      for (Int32 x = 0; x < c_numElements; x++)
         aJagged[x] = new Int32[c_numElements];


      // 1: Access all elements of the array using the usual, safe technique
      Safe2DimArrayAccess(a2Dim);

      // 2: Access all elements of the array using the jagged array technique
      SafeJaggedArrayAccess(aJagged);

      // 3: Access all elements of the array using the unsafe technique
      Unsafe2DimArrayAccess(a2Dim);
   }

   private static Int32 Safe2DimArrayAccess(Int32[,] a) {
      Int32 sum = 0;
      for (Int32 x = 0; x < c_numElements; x++) {
         for (Int32 y = 0; y < c_numElements; y++) {
            sum += a[x, y];
         }
      }
      return sum;
   }

   private static Int32 SafeJaggedArrayAccess(Int32[][] a) {
      Int32 sum = 0;
      for (Int32 x = 0; x < c_numElements; x++) {
         for (Int32 y = 0; y < c_numElements; y++) {
            sum += a[x][y];
         }
      }
      return sum;
   }

   private static unsafe Int32 Unsafe2DimArrayAccess(Int32[,] a) {
      Int32 sum = 0, numElements = c_numElements * c_numElements;
      fixed (Int32* pi = a) {
         for (Int32 x = 0; x < numElements; x++) {
            sum += pi[x];
         }
      }
      return sum;
   }
}

internal static class StackallocAndInlineArrays {
   public static void Go() {
      StackallocDemo();
      InlineArrayDemo();
   }

   private static void StackallocDemo() {
      unsafe {
         const Int32 width = 20;
         Char* pc = stackalloc Char[width];	// Allocates array on stack

         String s = "Jeffrey Richter";	// 15 characters

         for (Int32 index = 0; index < width; index++) {
            pc[width - index - 1] =
               (index < s.Length) ? s[index] : '.';
         }
         Console.WriteLine(new String(pc, 0, width));	// ".....rethciR yerffeJ"
      }
   }

   private static void InlineArrayDemo() {
      unsafe {
         CharArray ca;	// Allocates array on stack
         Int32 widthInBytes = sizeof(CharArray);
         Int32 width = widthInBytes / 2;

         String s = "Jeffrey Richter";	// 15 characters

         for (Int32 index = 0; index < width; index++) {
            ca.Characters[width - index - 1] =
               (index < s.Length) ? s[index] : '.';
         }
         Console.WriteLine(new String(ca.Characters, 0, width));	// ".....rethciR yerffeJ"
      }
   }

   private unsafe struct CharArray {
      // This array is embedded inline inside the structure
      public fixed Char Characters[20];
   }
}


//////////////////////////////// End of File //////////////////////////////////
