#if !DEBUG
#pragma warning disable 67
#endif

//#define TEST
#define VERIFY

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

#region Possible Targets
#pragma warning disable 67

[assembly: MyAttr(1)]         // Applied to assembly
[module: MyAttr(2)]           // Applied to module

[type: MyAttr(3)]             // Applied to type
internal sealed class SomeType
   <[typevar: MyAttr(4)] T> { // Applied to generic type variable

   [field: MyAttr(5)]         // Applied to field
   public Int32 SomeField = 0;

   [return: MyAttr(6)]        // Applied to return value
   [method: MyAttr(7)]        // Applied to method
   public Int32 SomeMethod(
      [param: MyAttr(8)]      // Applied to parameter
      Int32 SomeParam) { return SomeParam; }

   [property: MyAttr(9)]      // Applied to property
   public String SomeProp {
      [method: MyAttr(10)]    // Applied to get accessor method
      get { return null; }
   }

   [event: MyAttr(11)]        // Applied to event
   [field: MyAttr(12)]        // Applied to compiler-generated field
   [method: MyAttr(13)]       // Applied to compiler-generated add & remove methods
   public event EventHandler SomeEvent;
}

#pragma warning restore 67


[AttributeUsage(AttributeTargets.All)]
public class MyAttr : Attribute {
   public MyAttr(Int32 x) { }
}
#endregion

#region Applying Attributes
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal sealed class OSVERSIONINFO {
   public OSVERSIONINFO() {
      OSVersionInfoSize = (UInt32)Marshal.SizeOf(this);
   }

   public UInt32 OSVersionInfoSize = 0;
   public UInt32 MajorVersion = 0;
   public UInt32 MinorVersion = 0;
   public UInt32 BuildNumber = 0;
   public UInt32 PlatformId = 0;

   [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
   public String CSDVersion = null;
}

internal static class MyClass {
   [DllImport("Kernel32", CharSet = CharSet.Auto, SetLastError = true)]
   public static extern Boolean GetVersionEx([In, Out] OSVERSIONINFO ver);
}
#endregion

#region Attribute Parameter Types
public enum Color { Red }

[AttributeUsage(AttributeTargets.All)]
internal sealed class SomeAttribute : Attribute {
   public SomeAttribute(String name, Object o, Type[] types) {
      // 'name'  refers to a String
      // 'o'     refers to one of the legal types (boxing if necessary)
      // 'types' refers to a 1-dimension, 0-based array of Types
   }
}

[Some("Jeff", Color.Red, new Type[] { typeof(Math), typeof(Console) })]
internal sealed class SomeType {
}
#endregion

public static class CustomAttributes {
   public static void Main() {
      DetectingAttributes.Go();
      MatchingAttributes.Go();
      ConditionalAttributeDemo.Go();
   }

   [Serializable]
   [DefaultMemberAttribute("Main")]
   [DebuggerDisplayAttribute("Richter", Name = "Jeff", Target = typeof(DetectingAttributes))]
   public sealed class DetectingAttributes {
      [Conditional("Debug")]
      [Conditional("Release")]
      public void DoSomething() { }

      public DetectingAttributes() {
      }

      [MethodImpl(MethodImplOptions.NoInlining)]
      [STAThread]
      public static void Go() {
         Go(ShowAttributes);
         Go(ShowAttributesReflectionOnly);
      }

      private static void Go(Action<MemberInfo> showAttributes) {
         // Show the set of attributes applied to this type
         showAttributes(typeof(DetectingAttributes));

         // Get the set of methods associated with the type
         var members = 
            from m in typeof(DetectingAttributes).GetTypeInfo().DeclaredMembers.OfType<MethodBase>()
            where m.IsPublic select m;

         foreach (MemberInfo member in members) {
            // Show the set of attributes applied to this member
            showAttributes(member);
         }
      }

      private static void ShowAttributes(MemberInfo attributeTarget) {
         var attributes = attributeTarget.GetCustomAttributes<Attribute>();

         Console.WriteLine("Attributes applied to {0}: {1}",
            attributeTarget.Name, (attributes.Count() == 0 ? "None" : String.Empty));

         foreach (Attribute attribute in attributes) {
            // Display the type of each applied attribute
            Console.WriteLine("  {0}", attribute.GetType().ToString());

            if (attribute is DefaultMemberAttribute)
               Console.WriteLine("    MemberName={0}",
                  ((DefaultMemberAttribute)attribute).MemberName);

            if (attribute is ConditionalAttribute)
               Console.WriteLine("    ConditionString={0}",
                  ((ConditionalAttribute)attribute).ConditionString);

            if (attribute is CLSCompliantAttribute)
               Console.WriteLine("    IsCompliant={0}",
                  ((CLSCompliantAttribute)attribute).IsCompliant);

            DebuggerDisplayAttribute dda = attribute as DebuggerDisplayAttribute;
            if (dda != null) {
               Console.WriteLine("    Value={0}, Name={1}, Target={2}",
                  dda.Value, dda.Name, dda.Target);
            }
         }
         Console.WriteLine();
      }

      private static void ShowAttributesReflectionOnly(MemberInfo attributeTarget) {
         IList<CustomAttributeData> attributes =
            CustomAttributeData.GetCustomAttributes(attributeTarget);

         Console.WriteLine("Attributes applied to {0}: {1}",
            attributeTarget.Name, (attributes.Count == 0 ? "None" : String.Empty));

         foreach (CustomAttributeData attribute in attributes) {
            // Display the type of each applied attribute
            Type t = attribute.Constructor.DeclaringType;
            Console.WriteLine("  {0}", t.ToString());
            Console.WriteLine("    Constructor called={0}", attribute.Constructor);

            IList<CustomAttributeTypedArgument> posArgs = attribute.ConstructorArguments;
            Console.WriteLine("    Positional arguments passed to constructor:" +
               ((posArgs.Count == 0) ? " None" : String.Empty));
            foreach (CustomAttributeTypedArgument pa in posArgs) {
               Console.WriteLine("      Type={0}, Value={1}", pa.ArgumentType, pa.Value);
            }


            IList<CustomAttributeNamedArgument> namedArgs = attribute.NamedArguments;
            Console.WriteLine("    Named arguments set after construction:" +
               ((namedArgs.Count == 0) ? " None" : String.Empty));
            foreach (CustomAttributeNamedArgument na in namedArgs) {
               Console.WriteLine("     Name={0}, Type={1}, Value={2}",
                  na.MemberInfo.Name, na.TypedValue.ArgumentType, na.TypedValue.Value);
            }

            Console.WriteLine();
         }
         Console.WriteLine();
      }
   }
}


internal sealed class MatchingAttributes {
   public static void Go() {
      CanWriteCheck(new ChildAccount());
      CanWriteCheck(new AdultAccount());

      // This just demonstrates that the method works correctly on a
      // type that doesn't have the AccountsAttribute applied to it.
      CanWriteCheck(new MatchingAttributes());
   }

   private static void CanWriteCheck(Object obj) {
      // Construct an instance of the attribute type and initialize it
      // to what we are explicitly looking for.
      Attribute checking = new AccountsAttribute(Accounts.Checking);

      // Construct the attribute instance that was applied to the type
      Attribute validAccounts = obj.GetType().GetCustomAttribute<AccountsAttribute>(false);

      // If the attribute was applied to the type AND the 
      // attribute specifies the "Checking" account, then the
      // type can write a check
      if ((validAccounts != null) && checking.Match(validAccounts)) {
         Console.WriteLine("{0} types can write checks.", obj.GetType());
      } else {
         Console.WriteLine("{0} types can NOT write checks.", obj.GetType());
      }
   }

   [Flags]
   private enum Accounts {
      Savings = 0x0001,
      Checking = 0x0002,
      Brokerage = 0x0004
   }


   [AttributeUsage(AttributeTargets.Class)]
   private sealed class AccountsAttribute : Attribute {
      private Accounts m_accounts;

      public AccountsAttribute(Accounts accounts) {
         m_accounts = accounts;
      }

      public override Boolean Match(Object obj) {
         // If the base class implements Match and the base class
         // is not Attribute, then uncomment the line below.
         // if (!base.Match(obj)) return false;

         // Since ‘this’ isn’t null, if obj is null, 
         // then the objects can’t match
         // NOTE: This line may be deleted if you trust 
         // the base type implemented Match correctly.
         if (obj == null) return false;

         // If the objects are of different types, they can’t match
         // NOTE: This line may be deleted if you trust 
         // the base type implemented Match correctly.
         if (this.GetType() != obj.GetType()) return false;

         // Cast obj to our type to access fields. NOTE: This cast
         // can't fail since we know objects are of the same type
         AccountsAttribute other = (AccountsAttribute)obj;

         // Compare the fields as you see fit
         // This example checks if 'this' accounts is a subset 
         // of other's accounts
         if ((other.m_accounts & m_accounts) != m_accounts)
            return false;

         return true;   // Objects match
      }

      public override Boolean Equals(Object obj) {
         // If the base class implements Equals and the base class
         // is not Object, then uncomment the line below.
         // if (!base.Equals(obj)) return false;

         // Since ‘this’ isn’t null, if obj is null, 
         // then the objects can’t be equal
         // NOTE: This line may be deleted if you trust 
         // the base type implemented Equals correctly.
         if (obj == null) return false;

         // If the objects are of different types, they can’t be equal
         // NOTE: This line may be deleted if you trust 
         // the base type implemented Equals correctly.
         if (this.GetType() != obj.GetType()) return false;

         // Cast obj to our type to access fields. NOTE: This cast
         // can't fail since we know objects are of the same type
         AccountsAttribute other = (AccountsAttribute)obj;

         // Compare the fields to see if they have the same value
         // This example checks if 'this' accounts is the same
         // as other's accounts
         if (other.m_accounts != m_accounts)
            return false;

         return true;   // Objects are equal
      }

      // Override GetHashCode since we override Equals
      public override Int32 GetHashCode() {
         return (Int32)m_accounts;
      }
   }


   [Accounts(Accounts.Savings)]
   private sealed class ChildAccount { }

   [Accounts(Accounts.Savings | Accounts.Checking | Accounts.Brokerage)]
   private sealed class AdultAccount { }
}

[Cond]
public static class ConditionalAttributeDemo {
   [Conditional("TEST")]
   [Conditional("VERIFY")]
   public sealed class CondAttribute : Attribute {
   }

   public static void Go() {
      Console.WriteLine("CondAttribute is {0}applied to Program type.",
         Attribute.IsDefined(typeof(ConditionalAttributeDemo),
            typeof(CondAttribute)) ? "" : "not ");
   }
}