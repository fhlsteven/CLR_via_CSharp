//#define GetSurrogateForCyclicalReference
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Security.Permissions;

[assembly:AssemblyVersion("1.0.0.0")]

public static class RuntieSerialization {
   public static void Main() {
      QuickStart.Go();
      UsingNonSerializedFields.Go();
      OptionalField.Go();
      ISerializableVersioning.Go();
      SerializingSingletons.Go();
      SerializationSurrogates.Go();
      SerializationBinderDemo.Go();
   }
}

internal static class QuickStart {
   public static void Go() {
      // Create a graph of objects to serialize them to the stream 
      var objectGraph = new List<String> { "Jeff", "Kristin", "Aidan", "Grant" };
      Stream stream = SerializeToMemory(objectGraph);

      // Reset everything for this demo
      stream.Position = 0;
      objectGraph = null;

      // Deserialize the objects and prove it worked
      objectGraph = (List<String>)DeserializeFromMemory(stream);
      foreach (var s in objectGraph) Console.WriteLine(s);

      var clone = DeepClone(objectGraph);
      MultipleGraphs();
      OptInSerialization();
   }

   private static MemoryStream SerializeToMemory(Object objectGraph) {
      // Construct a stream that is to hold the serialized objects
      MemoryStream stream = new MemoryStream();

      // Construct a serialization formatter that does all the hard work
      BinaryFormatter formatter = new BinaryFormatter();

      // Tell the formatter to serialize the objects into the stream
      formatter.Serialize(stream, objectGraph);

      // Return the stream of serialized objects back to the caller
      return stream;
   }

   private static Object DeserializeFromMemory(Stream stream) {
      // Construct a serialization formatter that does all the hard work
      BinaryFormatter formatter = new BinaryFormatter();

      // Tell the formatter to deserialize the objects from the stream
      return formatter.Deserialize(stream);
   }

   private static Object DeepClone(Object original) {
      // Construct a temporary memory stream
      using (MemoryStream stream = new MemoryStream()) {

         // Construct a serialization formatter that does all the hard work
         BinaryFormatter formatter = new BinaryFormatter();

         // This line is explained in this chapter's “Streaming Contexts” section 
         formatter.Context = new StreamingContext(StreamingContextStates.Clone);

         // Serialize the object graph into the memory stream
         formatter.Serialize(stream, original);

         // Seek back to the start of the memory stream before deserializing
         stream.Position = 0;

         // Deserialize the graph into a new set of objects and 
         // return the root of the graph (deep copy) to the caller
         return formatter.Deserialize(stream);
      }
   }

   [Serializable]
   private sealed class Customer { /* ... */ }
   [Serializable]
   private sealed class Order { /* ... */ }

   private static List<Customer> s_customers = new List<Customer>();
   private static List<Order> s_pendingOrders = new List<Order>();
   private static List<Order> s_processedOrders = new List<Order>();

   private static void MultipleGraphs() {
      using (var stream = new MemoryStream()) {
         SaveApplicationState(stream);
         stream.Position = 0;
         RestoreApplicationState(stream);
      }
   }


   private static void SaveApplicationState(Stream stream) {
      // Construct a serialization formatter that does all the hard work
      BinaryFormatter formatter = new BinaryFormatter();

      // Serialize our application’s entire state
      formatter.Serialize(stream, s_customers);
      formatter.Serialize(stream, s_pendingOrders);
      formatter.Serialize(stream, s_processedOrders);
   }

   private static void RestoreApplicationState(Stream stream) {
      // Construct a serialization formatter that does all the hard work
      BinaryFormatter formatter = new BinaryFormatter();

      // Deserialize our application’s entire state (same order as serialized)
      s_customers = (List<Customer>)formatter.Deserialize(stream);
      s_pendingOrders = (List<Order>)formatter.Deserialize(stream);
      s_processedOrders = (List<Order>)formatter.Deserialize(stream);
   }

   // Not marked [Serializable]
   private struct Point { public Int32 x, y; }

   private static void OptInSerialization() {
      Point pt = new Point { x = 1, y = 2 };
      using (var stream = new MemoryStream()) {
         new BinaryFormatter().Serialize(stream, pt); // throws SerializationException
      }
   }
}

internal static class UsingNonSerializedFields {
   [Serializable]
   internal class Circle /*: IDeserializationCallback */{
      private Double m_radius;

      [NonSerialized]
      private Double m_area;

      public Circle(Double radius) {
         m_radius = radius;
         m_area = Math.PI * m_radius * m_radius;
      }

      [OnDeserialized]
      private void OnDeserialized(StreamingContext context) {
         m_area = Math.PI * m_radius * m_radius;
      }

      [OnDeserializing]
      private void OnDeserializing(StreamingContext context) {
         m_area = Math.PI * m_radius * m_radius;
      }
      //void IDeserializationCallback.OnDeserialization(Object sender) { m_area = Math.PI * m_radius * m_radius; }
   }

   [Serializable]
   private class Outer {
      public Inner inner;
      [OnSerializing]
      private void OnSerializing(StreamingContext context) { }
      [OnSerialized]
      private void OnSerialized(StreamingContext context) { }

      [OnDeserializing]
      private void OnDeserializing(StreamingContext context) { }
      [OnDeserialized]
      private void OnDeserialized(StreamingContext context) { }
   }

   [Serializable]
   private class Inner {
      [OnSerializing]
      private void OnSerializing(StreamingContext context) { }
      [OnSerialized]
      private void OnSerialized(StreamingContext context) { }

      [OnDeserializing]
      private void OnDeserializing(StreamingContext context) { }
      [OnDeserialized]
      private void OnDeserialized(StreamingContext context) { }
   }


   public static void Go() {
      using (var stream = new MemoryStream()) {
         BinaryFormatter formatter = new BinaryFormatter();
         var outer = new Outer { inner = new Inner() };
         //Circle[] circles = new[] { new Circle(10), new Circle(20) };
         formatter.Serialize(stream, outer);
         stream.Position = 0;
         outer = (Outer)formatter.Deserialize(stream);
      }
   }
}

internal static class OptionalField {
   [Serializable]
   public class Foo {
      /*[OptionalField] */public String name = "jeff";
   }

   public static void Go() {
      const String filename = @"temp.dat";
      var formatter = new SoapFormatter();

      // Serialize
      using (var stream = File.Create(filename)) {
         formatter.Serialize(stream, new Foo());
      }

      // Deserialize
      using (var stream = File.Open(filename, FileMode.Open)) {
         Foo f = (Foo)formatter.Deserialize(stream);
      }

      File.Delete(filename);
   }
}

internal static class ISerializableVersioning {
   public static void Go() {
      using (var stream = new MemoryStream()) {
         BinaryFormatter formatter = new BinaryFormatter();
         formatter.Serialize(stream, new Derived());
         stream.Position = 0;
         Derived d = (Derived)formatter.Deserialize(stream);
         Console.WriteLine(d);
      }
   }

   [Serializable]
   private class Base {
      protected String m_name = "Jeff";
      protected String Name { get { return m_name; } }
      public Base() { /* Make the type instantiable*/ }
   }

   [Serializable]
   private class Derived : Base, ISerializable {
      new private String m_name = "Richter";
      public Derived() { /* Make the type instantiable*/ }

      // If this constructor didn't exist, we'd get a SerializationException
      // This constructor should be protected if this class were not sealed
      [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
      private Derived(SerializationInfo info, StreamingContext context) {
         // Get the set of serializable members for our class and base classes
         Type baseType = this.GetType().BaseType;
         MemberInfo[] mi = FormatterServices.GetSerializableMembers(baseType, context);

         // Deserialize the base class's fields from the info object
         for (Int32 i = 0; i < mi.Length; i++) {
            // Get the field and set it to the deserialized value
            FieldInfo fi = (FieldInfo)mi[i];
            fi.SetValue(this, info.GetValue(baseType.FullName + "+" + fi.Name, fi.FieldType));
         }

         // Deserialize the values that were serialized for this class
         m_name = info.GetString("Name");
      }

      [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
      public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
         // Serialize the desired values for this class
         info.AddValue("Name", m_name);

         // Get the set of serializable members for our class and base classes
         Type baseType = this.GetType().BaseType;
         MemberInfo[] mi = FormatterServices.GetSerializableMembers(baseType, context);

         // Serialize the base class's fields to the info object
         for (Int32 i = 0; i < mi.Length; i++) {
            // Prefix the field name with the fullname of the base type
            info.AddValue(baseType.FullName + "+" + mi[i].Name, ((FieldInfo)mi[i]).GetValue(this));
         }
      }
      public override String ToString() {
         return String.Format("Base Name={0}, Derived Name={1}", base.Name, m_name);
      }
   }
}

internal static class SerializingSingletons {
   public static void Go() {
      // Create an array with multiple elements refering to the one Singleton object
      Singleton[] a1 = { Singleton.GetSingleton(), Singleton.GetSingleton() };
      Console.WriteLine("Do both array elements refer to the same object? " + (a1[0] == a1[1]));     // "True"

      using (var stream = new MemoryStream()) {
         BinaryFormatter formatter = new BinaryFormatter();

         // Serialize and then deserialize the array elements
         formatter.Serialize(stream, a1);
         stream.Position = 0;
         Singleton[] a2 = (Singleton[])formatter.Deserialize(stream);

         Console.WriteLine("Do both array elements refer to the same object? " + (a2[0] == a2[1]));     // "True"
         Console.WriteLine("Do all  array elements refer to the same object? " + (a1[0] == a2[0]));     // "True"
      }
   }

   // There should be only one instance of this type per AppDomain
   [Serializable]
   public sealed class Singleton : ISerializable {
      // This is the one instance of this type
      private static readonly Singleton s_theOneObject = new Singleton();

      // Here are the instance fields
      public String Name = "Jeff";
      public DateTime Date = DateTime.Now;

      // Private constructor allowing this type to construct the singleton
      private Singleton() { }

      // Method returning a reference to the singleton
      public static Singleton GetSingleton() { return s_theOneObject; }

      // Method called when serializing a Singleton
      // I recommend using an Explicit Interface Method Impl. here
      [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
      void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
         info.SetType(typeof(SingletonSerializationHelper));
         // No other values need to be added
      }

      [Serializable]
      private sealed class SingletonSerializationHelper : IObjectReference {
         // Method called after this object (which has no fields) is deserialized
         public Object GetRealObject(StreamingContext context) {
            return Singleton.GetSingleton();
         }
      }

      // NOTE: The special constructor is NOT necessary because it's never called
   }
}

internal static class SerializationSurrogates {
   public static void Go() {
      using (var stream = new MemoryStream()) {
         // 1. Construct the desired formatter
         IFormatter formatter = new SoapFormatter();

         // 2. Construct a SurrogateSelector object
         SurrogateSelector ss = new SurrogateSelector();

         // 3. Tell the surrogate selector to use our surrogate for DateTime objects
         ISerializationSurrogate utcToLocalTimeSurrogate = new UniversalToLocalTimeSerializationSurrogate();
#if GetSurrogateForCyclicalReference
         utcToLocalTimeSurrogate = FormatterServices.GetSurrogateForCyclicalReference(utcToLocalTimeSurrogate); 
#endif
         ss.AddSurrogate(typeof(DateTime), formatter.Context, utcToLocalTimeSurrogate);

         // NOTE: AddSurrogate can be called multiple times to register multiple surrogates

         // 4. Tell the formatter to use our surrogate selector
         formatter.SurrogateSelector = ss;

         // Create a DateTime that represents the local time on the machine & serialize it
         DateTime localTimeBeforeSerialize = DateTime.Now;
         formatter.Serialize(stream, localTimeBeforeSerialize);

         // The Soap-formatted stream displays the Universal time as a string to prove it worked
         stream.Position = 0;
         Console.WriteLine(new StreamReader(stream).ReadToEnd());

         // Deserialize the Universal time string & convert it to a local DateTime for this machine
         stream.Position = 0;
         DateTime localTimeAfterDeserialize = (DateTime)formatter.Deserialize(stream);

         // Prove it worked correctly:
         Console.WriteLine("LocalTimeBeforeSerialize ={0}", localTimeBeforeSerialize);
         Console.WriteLine("LocalTimeAfterDeserialize={0}", localTimeAfterDeserialize);
      }
   }

   private sealed class UniversalToLocalTimeSerializationSurrogate : ISerializationSurrogate {
      public void GetObjectData(Object obj, SerializationInfo info, StreamingContext context) {
         info.AddValue("Date", ((DateTime)obj).ToUniversalTime().ToString("u")); // Convert the DateTime from local to UTC
      }

      public Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector) {
         DateTime dt = DateTime.ParseExact(info.GetString("Date"), "u", null).ToLocalTime();  // Convert the DateTime from UTC to local
#if GetSurrogateForCyclicalReference
         // When using GetSurrogateForCyclicalReference, you must modify 'obj' directly and return null or obj
         // So, I modify the boxed DateTime that is passed into SetObjectData
         FieldInfo fi = typeof(DateTime).GetField("dateData", BindingFlags.NonPublic | BindingFlags.Instance);
         fi.SetValue(obj, fi.GetValue(dt));
         return null;
#else
         return dt;
#endif
      }
   }
}

internal static class SerializationBinderDemo {
   public static void Go() {
      using (var stream = new MemoryStream()) {
         IFormatter formatter = new BinaryFormatter();
         formatter.Binder = new Ver1ToVer2SerializationBinder();
         formatter.Serialize(stream, new Ver1());

         stream.Position = 0;
         Ver2 t = (Ver2)formatter.Deserialize(stream);
         Console.WriteLine("Type deserialized={0}, ToString={{{1}}}", t.GetType(), t);
      }
   }

   [Serializable]
   private sealed class Ver1 {
      public Int32 x = 1, y = 2, z = 3;
   }

   [Serializable]
   private sealed class Ver2 : ISerializable {
      Int32 a, b, c;

      [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
      public void GetObjectData(SerializationInfo info, StreamingContext context) {
         /* Never called: do nothing */
      }

      [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
      private Ver2(SerializationInfo info, StreamingContext context) {
         a = info.GetInt32("x");
         b = info.GetInt32("y");
         c = info.GetInt32("z");
      }

      public override string ToString() {
         return String.Format("a={0}, b={1}, c={2}", a, b, c);
      }
   }

   private sealed class Ver1ToVer2SerializationBinder : SerializationBinder {
      public override void BindToName(Type serializedType, out string assemblyName, out string typeName) {
         assemblyName = Assembly.GetExecutingAssembly().FullName;
         typeName = typeof(Ver2).FullName;
      }
      public override Type BindToType(String assemblyName, String typeName) {
         // Deserialize any Ver1 object from version 1.0.0.0 of this assembly into a Ver2 object

         // Calculate the assembly name that defined the Ver1 type
         AssemblyName assemVer1 = Assembly.GetExecutingAssembly().GetName();
         assemVer1.Version = new Version(1, 0, 0, 0);

         // If deserializing the Ver1 object from v1.0.0.0 of our assembly, turn it into a Ver2 object 
         if (assemblyName == assemVer1.ToString() && typeName == "SerializationBinderDemo+Ver1")
            return typeof(Ver2);

         // Else, just return the same type being requested
         return Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
      }
   }
}

///////////////////////////////////////////////////////////////////////////////
