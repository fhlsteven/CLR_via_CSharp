using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class Program {
    public static void Main() {
        ParameterlessProperties.Go();
        AnonymousTypesAndTuples.Go();
        BitArrayTest();
    }

    private static void BitArrayTest() {
        // Allocate a BitArray that can hold 14 bits.
        BitArray ba = new BitArray(14);

        // Turn all the even-numbered bits on by calling the set accessor.
        for (Int32 x = 0; x < 14; x++) {
            ba[x] = (x % 2 == 0);
        }

        // Show the state of all the bits by calling the get accessor.
        for (Int32 x = 0; x < 14; x++) {
            Console.WriteLine("Bit " + x + " is " + (ba[x] ? "On" : "Off"));
        }
    }
}

internal static class ParameterlessProperties {
    public static void Go() {
        Employee emp = new Employee();
        emp.Name = "Jeffrey Richter";
        emp.Age = 45;	   // Updates the age
        Console.WriteLine("Employee info: Name = {0}, Age = {1}", emp.Name, emp.Age);

        try {
            emp.Age = -5;	   // Throws an exception
        }
        catch (ArgumentOutOfRangeException e) {
            Console.WriteLine(e);
        }
        Console.WriteLine("Employee info: Name = {0}, Age = {1}", emp.Name, emp.Age);
    }

    private sealed class Employee {
        private String m_Name; // prepended 'm_' to avoid conflict
        private Int32 m_Age;  // prepended 'm_' to avoid conflict

        public String Name {
            get { return (m_Name); }
            set { m_Name = value; } // 'value' identifies new value
        }

        public Int32 Age {
            get { return (m_Age); }
            set {
                if (value <= 0)    // 'value' identifies new value
                    throw new ArgumentOutOfRangeException("value", "must be >0");
                m_Age = value;
            }
        }
    }
}

internal static class AnonymousTypesAndTuples {
    public static void Go() {
        AnonymousTypes();
        TupleTypes();
        Expando();
    }

    private static void AnonymousTypes() {
        // Define a type, construct an instance of it, & initialize its properties
        var o1 = new { Name = "Jeff", Year = 1964 };

        // Display the properties on the console:
        Console.WriteLine("Name={0}, Year={1}", o1.Name, o1.Year);

        // Property names/types inferred from variables
        String Name = "Grant";
        DateTime dt = DateTime.Now;
        var o2 = new { Name, dt.Year };

        // Show the C#-generated type names
        ShowVariableType(o1);
        ShowVariableType(o2);

        // Anonymous types have same definition: compiler generated 1 type
        Console.WriteLine("Types are same: " + (o1.GetType() == o2.GetType()));

        // 1 type allows equality and assignment operations.
        Console.WriteLine("Objects are equal: " + o1.Equals(o2));
        o1 = o2;  // Assignment

        var people = new[] {
         o1,
         new { Name = "Kristin", Year = 1970 },
         new { Name = "Aidan",   Year = 2003 },
         new { Name = "Grant",   Year = 2008 }
      };

        foreach (var person in people)
            Console.WriteLine("Person={0}, Year={1}", person.Name, person.Year);

        String myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var query =
           from pathname in Directory.GetFiles(myDocuments)
           let LastWriteTime = File.GetLastWriteTime(pathname)
           where LastWriteTime > (DateTime.Now - TimeSpan.FromDays(7))
           orderby LastWriteTime
           select new { Path = pathname, LastWriteTime };

        foreach (var file in query)
            Console.WriteLine("LastWriteTime={0}, Path={1}", file.LastWriteTime, file.Path);
    }

    private static void ShowVariableType<T>(T t) { Console.WriteLine(typeof(T)); }

    // Returns minimum in Item1 & maximum in Item2
    private static Tuple<Int32, Int32> MinMax(Int32 a, Int32 b) {
        return Tuple.Create(Math.Min(a, b), Math.Max(a, b));
        //return new Tuple<Int32, Int32>(Math.Min(a, b), Math.Max(a, b));
    }

    // This shows how to call the method and how to use the returned Tuple
    private static void TupleTypes() {
        var minmax = MinMax(6, 2);
        Console.WriteLine("Min={0}, Max={1}", minmax.Item1, minmax.Item2);
        var t = Tuple.Create(0, 1, 2, 3, 4, 5, 6, Tuple.Create(7, 8));
        Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}",
           t.Item1, t.Item2, t.Item3, t.Item4, t.Item5, t.Item6, t.Item7, 
           t.Rest.Item1.Item1, t.Rest.Item1.Item2);
    }

    private static void Expando() {
        dynamic e = new System.Dynamic.ExpandoObject();
        e.x = 6;	// Add an Int32 'x' property whose value is 6
        e.y = "Jeff";	// Add a String 'y' property whose value is “Jeff”
        e.z = null;	// Add an Object 'z' property whose value is null

        // See all the properties and their values:
        foreach (var v in (IDictionary<String, Object>)e)
            Console.WriteLine("Key={0}, V={1}", v.Key, v.Value);

        // Remove the 'x' property and its value
        ((IDictionary<String, Object>)e).Remove("x");

        // See all the properties and their values:
        foreach (var v in (IDictionary<String, Object>)e)
            Console.WriteLine("Key={0}, V={1}", v.Key, v.Value);
    }
}

internal sealed class BitArray {
    // Private array of bytes that hold the bits
    private Byte[] m_byteArray;
    private Int32 m_numBits;

    // Constructor that allocates the byte array and sets all bits to 0
    public BitArray(Int32 numBits) {
        // Validate arguments first.
        if (numBits <= 0)
            throw new ArgumentOutOfRangeException("numBits must be > 0");

        // Save the number of bits.
        m_numBits = numBits;

        // Allocate the bytes for the bit array.
        m_byteArray = new Byte[(m_numBits + 7) / 8];
    }


    // This is the indexer.
    public Boolean this[Int32 bitPos] {
        // This is the index property’s get accessor method.
        get {
            // Validate arguments first
            if ((bitPos < 0) || (bitPos >= m_numBits))
                throw new ArgumentOutOfRangeException("bitPos", "bitPos must be between 0 and " + m_numBits);

            // Return the state of the indexed bit.
            return ((m_byteArray[bitPos / 8] & (1 << (bitPos % 8))) != 0);
        }

        // This is the index property’s set accessor method.
        set {
            if ((bitPos < 0) || (bitPos >= m_numBits))
                throw new ArgumentOutOfRangeException("bitPos", "bitPos must be between 0 and " + m_numBits);

            if (value) {
                // Turn the indexed bit on.
                m_byteArray[bitPos / 8] = (Byte)
                   (m_byteArray[bitPos / 8] | (1 << (bitPos % 8)));
            } else {
                // Turn the indexed bit off.
                m_byteArray[bitPos / 8] = (Byte)
                   (m_byteArray[bitPos / 8] & ~(1 << (bitPos % 8)));
            }
        }
    }
}

