using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp.RuntimeBinder;

public sealed class Program {
   public static void Main() {
      DynamicLoadFromResource.Go();
      DiscoverTypes.Go();
      ConstructingGenericType.Go();
      MemberDiscover.Go();
      InterfaceDiscover.Go();
      Invoker.Go();
      ExceptionTree.Go();
   }
}

internal static class DynamicLoadFromResource {
   public static void Go() {
      // For testing: delete the DLL from the runtime directory so 
      // that we have to load as a resource from this assembly
      String path = Assembly.GetExecutingAssembly().Location;
      path = Path.GetDirectoryName(path);
      path = Path.Combine(path, @"Ch01-1-SomeLibrary.dll");
      File.Delete(path);

      // Install a callback with this AppDomain's AssemblyResolve event
      AppDomain.CurrentDomain.AssemblyResolve += ResolveEventHandler;

      // Call a method that references types in the assembly we wan tto load from resources
      Test();
   }

   private static Assembly ResolveEventHandler(Object sender, ResolveEventArgs args) {
      Debugger.Break();
      String dllName = new AssemblyName(args.Name).Name + ".dll";

      var assem = Assembly.GetExecutingAssembly();
      String resourceName = assem.GetManifestResourceNames().FirstOrDefault(rn => rn.EndsWith(dllName));
      if (resourceName == null) return null; // Not found, maybe another handler will find it
      using (var stream = assem.GetManifestResourceStream(resourceName)) {
         Byte[] assemblyData = new Byte[stream.Length];
         stream.Read(assemblyData, 0, assemblyData.Length);
         return Assembly.Load(assemblyData);
      }
   }

   private static void Test() {
      var slt = new SomeLibrary.SomeLibraryType();
      Console.WriteLine(slt.Abc());
   }
}

internal static class DiscoverTypes {
   public static void Go() {
      String dataAssembly = "System.Data, version=4.0.0.0, " +
         "culture=neutral, PublicKeyToken=b77a5c561934e089";
      LoadAssemAndShowPublicTypes(dataAssembly);
   }

   private static void LoadAssemAndShowPublicTypes(String assemId) {
      // Explicitly load an assembly in to this AppDomain
      Assembly a = Assembly.Load(assemId);

      // Execute this loop once for each Type 
      // publicly-exported from the loaded assembly 
      foreach (Type t in a.ExportedTypes) {
         // Display the full name of the type
         Console.WriteLine(t.FullName);
      }
   }
}

internal static class ExceptionTree {
   public static void Go() {
      // Explicitly load the assemblies that we want to reflect over
      LoadAssemblies();

      // Filter & sort all the types
      var allTypes =
         (from a in new [] { typeof(Object).Assembly }//AppDomain.CurrentDomain.GetAssemblies()
          from t in a.ExportedTypes
          where typeof(Exception).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo())
          orderby t.Name
          select t).ToArray();

      // Build the inheritance hierarchy tree and show it
      Console.WriteLine(WalkInheritanceHierarchy(new StringBuilder(), 0, typeof(Exception), allTypes));
   }

   private static StringBuilder WalkInheritanceHierarchy(StringBuilder sb, Int32 indent, Type baseType, IEnumerable<Type> allTypes) {
      String spaces = new String(' ', indent * 3);
      sb.AppendLine(spaces + baseType.FullName);
      foreach (var t in allTypes) {
         if (t.GetTypeInfo().BaseType != baseType) continue;
         WalkInheritanceHierarchy(sb, indent + 1, t, allTypes);
      }
      return sb;
   }

   private static void LoadAssemblies() {
      String[] assemblies = {
            "System,                        PublicKeyToken={0}",
            "System.Core,                   PublicKeyToken={0}",
            "System.Data,                   PublicKeyToken={0}",
            "System.Design,                 PublicKeyToken={1}",
            "System.DirectoryServices,      PublicKeyToken={1}",
            "System.Drawing,                PublicKeyToken={1}",
            "System.Drawing.Design,         PublicKeyToken={1}",
            "System.Management,             PublicKeyToken={1}",
            "System.Messaging,              PublicKeyToken={1}",
            "System.Runtime.Remoting,       PublicKeyToken={0}",
            "System.Runtime.Serialization,  PublicKeyToken={0}",
            "System.Security,               PublicKeyToken={1}",
            "System.ServiceModel,           PublicKeyToken={0}",
            "System.ServiceProcess,         PublicKeyToken={1}",
            "System.Web,                    PublicKeyToken={1}",
            "System.Web.RegularExpressions, PublicKeyToken={1}",
            "System.Web.Services,           PublicKeyToken={1}",
            "System.Xml,                    PublicKeyToken={0}",
            "System.Xml.Linq,               PublicKeyToken={0}",
            "Microsoft.CSharp,              PublicKeyToken={1}",
         };

      const String EcmaPublicKeyToken = "b77a5c561934e089";
      const String MSPublicKeyToken = "b03f5f7f11d50a3a";
                                 
      // Get the version of the assembly containing System.Object
      // We'll assume the same version for all the other assemblies
      Version version = typeof(System.Object).Assembly.GetName().Version;

      // Explicitly load the assemblies that we want to reflect over
      foreach (String a in assemblies) {
         String AssemblyIdentity =
            String.Format(a, EcmaPublicKeyToken, MSPublicKeyToken) +
               ", Culture=neutral, Version=" + version;
         Assembly.Load(AssemblyIdentity);
      }
   }
}

internal static class ConstructingGenericType {
   private sealed class Dictionary<TKey, TValue> { }

   public static void Go() {
      // Get a reference to the generic type's type object
      Type openType = typeof(Dictionary<,>);

      // Close the generic type by using TKey=String, TValue=Int32
      Type closedType = openType.MakeGenericType(typeof(String), typeof(Int32));

      // Construct an instance of the closed type
      Object o = Activator.CreateInstance(closedType);

      // Prove it worked
      Console.WriteLine(o.GetType());
   }
}

internal static class MemberDiscover {
   public static void Go() {
      // Loop through all assemblies loaded in this AppDomain
      Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
      foreach (Assembly a in assemblies) {
         Show(0, "Assembly: {0}", a);

         // Find Types in the assembly
         foreach (Type t in a.ExportedTypes) {
            Show(1, "Type: {0}", t);

            // Discover the type's members
            foreach (MemberInfo mi in t.GetTypeInfo().DeclaredMembers) {
               String typeName = String.Empty;
               if (mi is Type) typeName = "(Nested) Type";
               if (mi is FieldInfo) typeName = "FieldInfo";
               if (mi is MethodInfo) typeName = "MethodInfo";
               if (mi is ConstructorInfo) typeName = "ConstructoInfo";
               if (mi is PropertyInfo) typeName = "PropertyInfo";
               if (mi is EventInfo) typeName = "EventInfo";

               Show(2, "{0}: {1}", typeName, mi);
            }
         }
      }
   }

   private static void Show(Int32 indent, String format, params Object[] args) {
      Console.WriteLine(new String(' ', 3 * indent) + format, args);
   }
}

internal static class InterfaceDiscover {
   // Define two interfaces for testing
   private interface IBookRetailer : IDisposable {
      void Purchase();
      void ApplyDiscount();
   }

   private interface IMusicRetailer {
      void Purchase();
   }

   // This class implements 2 interfaces defined by this assembly and 1 interface defined by another assembly
   private sealed class MyRetailer : IBookRetailer, IMusicRetailer, IDisposable {
      // IBookRetailer methods
      void IBookRetailer.Purchase() { }
      public void ApplyDiscount() { }

      // IMusicRetailer method
      void IMusicRetailer.Purchase() { }

      // IDisposable method
      public void Dispose() { }

      // MyRetailer method (not an interface method)
      public void Purchase() { }
   }

   public static void Go() {
      // Find interfaces implemented by MyRetailer where the interface is defined in our own assembly.
      // This is accomplished using a delegate to a filter method that we pass to FindInterfaces.
      TypeInfo ti = typeof(MyRetailer).GetTypeInfo();
      IEnumerable<Type> interfaces = ti.ImplementedInterfaces.Where(i => i.Assembly == typeof(InterfaceDiscover).Assembly);
      Console.WriteLine("MyRetailer implements the following interfaces (defined in this assembly):");

      // Show information about each interface
      foreach (Type i in interfaces) {
         Console.WriteLine("\nInterface: " + i);

         // Get the type methods that map to the interface's methods
         InterfaceMapping map = ti.GetRuntimeInterfaceMap(i);

         for (Int32 m = 0; m < map.InterfaceMethods.Length; m++) {
            // Display the interface method name and which type method implements the interface method.
            Console.WriteLine("   {0} is implemented by {1}",
               map.InterfaceMethods[m], map.TargetMethods[m]);
         }
      }
   }
}

internal static class ReflectionExtensions {
   // Helper extension method to simplify syntax to create a delegate
   public static TDelegate CreateDelegate<TDelegate>(this MethodInfo mi, Object target = null) {
      return (TDelegate)(Object)mi.CreateDelegate(typeof(TDelegate), target);
   }
}

internal static class Invoker {
   // This class is used to demonstrate reflection
   // It has a field, constructor, method, property, and an event
   private sealed class SomeType {
      private Int32 m_someField;
      public SomeType(ref Int32 x) { x *= 2; }
      public override String ToString() { return m_someField.ToString(); }
      public Int32 SomeProp {
         get { return m_someField; }
         set {
            if (value < 1) throw new ArgumentOutOfRangeException("value", "value must be > 0");
            m_someField = value;
         }
      }
      public event EventHandler SomeEvent;
      private void NoCompilerWarnings() {
         SomeEvent.ToString();
      }
   }

   public static void Go() {
      Type t = typeof(SomeType);
      BindToMemberThenInvokeTheMember(t);
      Console.WriteLine();
      BindToMemberCreateDelegateToMemberThenInvokeTheMember(t);
      Console.WriteLine();
      UseDynamicToBindAndInvokeTheMember(t);
      Console.WriteLine();
   }

   private static void BindToMemberThenInvokeTheMember(Type t) {
      Console.WriteLine("BindToMemberThenInvokeTheMember");

      // Construct an instance
      Type ctorArgument = Type.GetType("System.Int32&"); // or typeof(Int32).MakeByRefType();
      ConstructorInfo ctor = t.GetTypeInfo().DeclaredConstructors.First(c => c.GetParameters()[0].ParameterType == ctorArgument);
      Object[] args = new Object[] { 12 };  // Constructor arguments
      Console.WriteLine("x before constructor called: " + args[0]);
      Object obj = ctor.Invoke(args);
      Console.WriteLine("Type: " + obj.GetType().ToString());
      Console.WriteLine("x after constructor returns: " + args[0]);

      // Read and write to a field
      FieldInfo fi = obj.GetType().GetTypeInfo().GetDeclaredField("m_someField");
      fi.SetValue(obj, 33);
      Console.WriteLine("someField: " + fi.GetValue(obj));

      // Call a method
      MethodInfo mi = obj.GetType().GetTypeInfo().GetDeclaredMethod("ToString");
      String s = (String)mi.Invoke(obj, null);
      Console.WriteLine("ToString: " + s);

      // Read and write a property
      PropertyInfo pi = obj.GetType().GetTypeInfo().GetDeclaredProperty("SomeProp");
      try {
         pi.SetValue(obj, 0, null);
      }
      catch (TargetInvocationException e) {
         if (e.InnerException.GetType() != typeof(ArgumentOutOfRangeException)) throw;
         Console.WriteLine("Property set catch.");
      }
      pi.SetValue(obj, 2, null);
      Console.WriteLine("SomeProp: " + pi.GetValue(obj, null));

      // Add and remove a delegate from the event
      EventInfo ei = obj.GetType().GetTypeInfo().GetDeclaredEvent("SomeEvent");
      EventHandler eh = new EventHandler(EventCallback); // See ei.EventHandlerType
      ei.AddEventHandler(obj, eh);
      ei.RemoveEventHandler(obj, eh);
   }

   private static void BindToMemberCreateDelegateToMemberThenInvokeTheMember(Type t) {
      Console.WriteLine("BindToMemberCreateDelegateToMemberThenInvokeTheMember");

      // Construct an instance (You can't create a delegate to a constructor)
      Object[] args = new Object[] { 12 };  // Constructor arguments
      Console.WriteLine("x before constructor called: " + args[0]);
      Object obj = Activator.CreateInstance(t, args);
      Console.WriteLine("Type: " + obj.GetType().ToString());
      Console.WriteLine("x after constructor returns: " + args[0]);

      // NOTE: You can't create a delegate to a field

      // Call a method
      MethodInfo mi = obj.GetType().GetTypeInfo().GetDeclaredMethod("ToString");
      var toString = mi.CreateDelegate<Func<String>>(obj);
      String s = toString();
      Console.WriteLine("ToString: " + s);

      // Read and write a property
      PropertyInfo pi = obj.GetType().GetTypeInfo().GetDeclaredProperty("SomeProp");
      var setSomeProp = pi.SetMethod.CreateDelegate<Action<Int32>>(obj);
      try {
         setSomeProp(0);
      }
      catch (ArgumentOutOfRangeException) {
         Console.WriteLine("Property set catch.");
      }
      setSomeProp(2);
      var getSomeProp = pi.GetMethod.CreateDelegate<Func<Int32>>(obj);
      Console.WriteLine("SomeProp: " + getSomeProp());

      // Add and remove a delegate from the event
      EventInfo ei = obj.GetType().GetTypeInfo().GetDeclaredEvent("SomeEvent");
      var addSomeEvent = ei.AddMethod.CreateDelegate<Action<EventHandler>>(obj);
      addSomeEvent(EventCallback);
      var removeSomeEvent = ei.RemoveMethod.CreateDelegate<Action<EventHandler>>(obj);
      removeSomeEvent(EventCallback);
   }

   private static void UseDynamicToBindAndInvokeTheMember(Type t) {
      Console.WriteLine("UseDynamicToBindAndInvokeTheMember");

      // Construct an instance (You can't use dynamic to call a constructor)
      Object[] args = new Object[] { 12 };  // Constructor arguments
      Console.WriteLine("x before constructor called: " + args[0]);
      dynamic obj = Activator.CreateInstance(t, args);
      Console.WriteLine("Type: " + obj.GetType().ToString());
      Console.WriteLine("x after constructor returns: " + args[0]);

      // Read and write to a field 
      try {
         obj.m_someField = 5;
         Int32 v = (Int32)obj.m_someField;
         Console.WriteLine("someField: " + v);
      }
      catch (RuntimeBinderException e) {
         // We get here because the field is private
         Console.WriteLine("Failed to access field: " + e.Message);
      }

      // Call a method
      String s = (String)obj.ToString();
      Console.WriteLine("ToString: " + s);

      // Read and write a property
      try {
         obj.SomeProp = 0;
      }
      catch (ArgumentOutOfRangeException) {
         Console.WriteLine("Property set catch.");
      }
      obj.SomeProp = 2;
      Int32 val = (Int32)obj.SomeProp;
      Console.WriteLine("SomeProp: " + val);

      // Add and remove a delegate from the event
      obj.SomeEvent += new EventHandler(EventCallback);
      obj.SomeEvent -= new EventHandler(EventCallback);
   }

   // Callback method added to the event
   private static void EventCallback(Object sender, EventArgs e) { }
}
