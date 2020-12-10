#if !DEBUG
#pragma warning disable 414, 169
#endif
using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace Example1 {
    internal sealed class SomeType {
        private Int32 m_x = 5;
    }
}


namespace Example2 {
#pragma warning disable 169
   internal sealed class SomeType {
        private Int32 m_x = 5;
        private String m_s = "Hi there";
        private Double m_d = 3.14159;
        private Byte m_b;

        // Here are some constructors. 
        public SomeType() { /* ... */ }
        public SomeType(Int32 x) { /* ... */ }
        public SomeType(String s) { /* ...; */ m_d = 10; }
    }
#pragma warning restore 169
}


namespace Example3 {
    internal sealed class SomeType {
        // Do not explicitly initialize the fields here
        private Int32 m_x;
        private String m_s;
        private Double m_d;
        private Byte m_b;

        // This method MUST be called by all constructors.
        private void SetFieldDefaults() {
            m_x = 5;
            m_s = "Hi there";
            m_d = 3.14159;
            m_b = 0xff;
        }

        // This constructor sets all fields to their default.
        public SomeType() {
            SetFieldDefaults();
        }

        // This constructor sets all fields to their default, then changes m_x.
        public SomeType(Int32 x) {
            SetFieldDefaults();
            m_x = x;
        }

        // This constructor sets all fields to their default, then changes m_s.
        public SomeType(String s) {
            SetFieldDefaults();
            m_s = s;
        }

        // This constructor sets all fields to their default, then changes m_x & m_s.
        public SomeType(Int32 x, String s) {
            SetFieldDefaults();
            m_x = x;
            m_s = s;
        }
    }
}



internal struct SomeValType {
    static SomeValType() {
        Console.WriteLine("This never gets displayed");
    }
    public Int32 m_x;
}

public sealed class Program {
    public static void Main() {
        SomeValType[] a = new SomeValType[10];
        a[0].m_x = 123;
        Console.WriteLine(a[0].m_x);	// Displays 123

        new FieldInitializationInCtor("Test");
        TypeConstructorPerformance.Go();
        ConversionOperator.Go();
        ExtensionMethods.Go();
    }
}

internal sealed class FieldInitializationInCtor {
    // No code here to explicitly initialize the fields
    private Int32 x;
    private String s;
    private Double d;
    private Byte b;

    // This constructor must be called by all the other constructors.
    // This constructor contains the code to initialize the fields.
    public FieldInitializationInCtor() {
        x = 5;
        s = "Hi There!";
        d = 3.14159;
    }

    // This constructor calls the default constructor first.
    public FieldInitializationInCtor(Int32 x)
        : this() {
        this.x = x;
    }

    // This constructor calls the default constructor first.
    public FieldInitializationInCtor(String s)
        : this() {
        this.s = s;
    }
}


public sealed class TypeConstructorPerformance {
    public static void Go() {
        const Int32 iterations = 1000 * 1000 * 1000;
        PerfTest1(iterations);
        PerfTest2(iterations);
    }

    // Since this class doesn't explicitly define a type constructor,
    // C# marks the type definition with BeforeFieldInit in the metadata.
    internal sealed class BeforeFieldInit {
        public static Int32 s_x = 123;
    }

    // Since this class does explicitly define a type constructor,
    // C# doesn't mark the type definition with BeforeFieldInit in the metadata.
    internal sealed class Precise {
        public static Int32 s_x;
        static Precise() { s_x = 123; }
    }

    // When this method is JIT compiled, the type constructors for
    // the BeforeFieldInit and Precise classes HAVE NOT executed yet 
    // and therefore, calls to these constructors are embedded in 
    // this method's code making it run slower
    private static void PerfTest1(Int32 iterations) {
        Stopwatch sw = Stopwatch.StartNew();
        for (Int32 x = 0; x < iterations; x++) {
            // The JIT compiler hoists the code to call BeforeFieldInit's 
            // type constructor so that it executes before the loop starts
            BeforeFieldInit.s_x = 1;
        }
        Console.WriteLine("PerfTest1: {0} BeforeFieldInit", sw.Elapsed);

        sw = Stopwatch.StartNew();
        for (Int32 x = 0; x < iterations; x++) {
            // The JIT compiler emits the code to call Precise's 
            // type constructor here so that it checks whether it
            // has to call the constructor with each loop iteration
            Precise.s_x = 1;
        }
        Console.WriteLine("PerfTest1: {0} Precise", sw.Elapsed);
    }

    // When this method is JIT compiled, the type constructors for
    // the BeforeFieldInit and Precise classes HAVE executed 
    // and therefore, calls to these constructors are NOT embedded 
    // in this method's code making it run faster
    private static void PerfTest2(Int32 iterations) {
        Stopwatch sw = Stopwatch.StartNew();
        for (Int32 x = 0; x < iterations; x++) {
            BeforeFieldInit.s_x = 1;
        }
        Console.WriteLine("PerfTest2: {0} BeforeFieldInit", sw.Elapsed);

        sw = Stopwatch.StartNew();
        for (Int32 x = 0; x < iterations; x++) {
            Precise.s_x = 1;
        }
        Console.WriteLine("PerfTest2: {0} Precise", sw.Elapsed);
    }
}

internal sealed class ConversionOperator {
    public static void Go() {
        Rational r1 = 5;		    // Implicit cast from Int32  to Rational
        Rational r2 = 2.5f;	    // Implicit cast from Single to Rational

        Int32 x = (Int32)r1;	 // Explicit cast from Rational to Int32
        Single s = (Single)r2;	 // Explicit cast from Rational to Single
    }

    public sealed class Rational {
        // Constructs a Rational from an Int32
        public Rational(Int32 num) { /* ... */ }

        // Constructs a Rational from a Single
        public Rational(Single num) { /* ... */ }

        // Convert a Rational to an Int32
        public Int32 ToInt32() { /* ... */ return 0; }

        // Convert a Rational to a Single
        public Single ToSingle() { /* ... */ return 0f; }

        // Implicitly constructs and returns a Rational from an Int32
        public static implicit operator Rational(Int32 num) {
            return new Rational(num);
        }

        // Implicitly constructs and returns a Rational from a Single
        public static implicit operator Rational(Single num) {
            return new Rational(num);
        }

        // Explicitly returns an Int32 from a Rational
        public static explicit operator Int32(Rational r) {
            return r.ToInt32();
        }

        // Explicitly returns a Single from a Rational
        public static explicit operator Single(Rational r) {
            return r.ToSingle();
        }
    }
}

#region Extension Method Demo
internal static class StringBuilderExtensions {
    public static Int32 IndexOf(this StringBuilder sb, Char value) {
        for (Int32 index = 0; index < sb.Length; index++)
            if (sb[index] == value) return index;
        return -1;
    }
}


internal static class ExtensionMethods {
    public static void Go() {
        {
            var sb = new StringBuilder("Hello. My name is Jeff.");	  // The initial string

            // Change period to exclamation mark and get # characters in 1st sentence (5).
            Int32 index = StringBuilderExtensions.IndexOf(sb.Replace('.', '!'), '!');

            sb.Replace('.', '!');				   // Change period to exclamation mark
            index = StringBuilderExtensions.IndexOf(sb, '!');   // Get # characters in 1st sentence (5)

            // Change period to exclamation mark and get # characters in 1st sentence (5).
            index = sb.Replace('.', '!').IndexOf('!');
        }

        {
            // sb is null
            StringBuilder sb = null;

            // Calling extension method: NullReferenceException will NOT be thrown when calling IndexOf
            // NullReferenceException will be thrown inside IndexOf’s for loop
            sb.IndexOf('X');

            // Calling instance method: NullReferenceException WILL be thrown when calling Replace
            sb.Replace('.', '!');
        }
        SomeMethod();
    }

    public static void SomeMethod() {
        // Shows each Char on a separate line in the console
        "Grant".ShowItems();

        // Shows each String on a separate line in the console
        new[] { "Jeff", "Kristin" }.ShowItems();

        // Shows each Int32 value on a separate line in the console
        new List<Int32>() { 1, 2, 3 }.ShowItems();

        // Create an Action delegate that refers to the static ShowItems extension method
        // and has the first argument initialized to reference the “Jeff” string. 
        Action a = "Jeff".ShowItems;
        // Invoke the delegate which calls ShowItems passing it a reference to the “Jeff” string.
        a();
    }

    private static void ShowItems<T>(this IEnumerable<T> collection) {
        foreach (var item in collection)
            Console.WriteLine(item);
        Console.WriteLine();
    }
}
#endregion

internal static class PartialMethodsDemo {
    private static class Inheritance {
        // Tool-produced code in some source code file:
        internal class Base {
            private String m_name;

            // Called before changing the m_name field
            protected virtual void OnNameChanging(String value) {
            }

            public String Name {
                get { return m_name; }
                set {
                    OnNameChanging(value.ToUpper());  // Inform class of potential change
                    m_name = value;                   // Change the field
                }
            }
        }


        // Developer-produced code is some other source code file:
        internal class Derived : Base {
            protected override void OnNameChanging(string value) {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value");
            }
        }
    }
    internal static class PartialMethods {
        // Tool-produced code in some source code file:
        internal sealed partial class Base {
            private String m_name;

            // This defining-partial-method-declaration is called before changing the m_name field
            partial void OnNameChanging(String value);

            public String Name {
                get { return m_name; }
                set {
                    OnNameChanging(value.ToUpper());  // Inform class of potential change
                    m_name = value;         	      // Change the field
                }
            }
        }

        // Developer-produced code is some other source code file:
        internal sealed partial class Base {

#if false   // Make 'true' to test the code with this method existing
      // This implementing-partial-method-declaration is called before m_name is changed 
      partial void OnNameChanging(String value) {
         if (String.IsNullOrEmpty(value))
            throw new ArgumentNullException("value");
      }
#endif
        }
    }

    public static void Go() {
        var inheritance = new Inheritance.Derived();
        inheritance.Name = "Jeff";

        var partialMethods = new PartialMethods.Base();
        partialMethods.Name = "Jeff";
    }
}
