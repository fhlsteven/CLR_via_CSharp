// This project is a DLL that has the "Do not reference mscorlib.dll" option turned on

namespace System {
   public class  Object { }
   public struct Byte { }
   public struct SByte { }
   public struct Int16 { }
   public struct Int32 { }
   public struct Int64 { }
   public struct UInt16 { }
   public struct UInt32 { }
   public struct UInt64 { }
   public struct IntPtr { }
   public struct UIntPtr { }
   public struct Single { }
   public struct Double { }
   public struct Char { }
   public struct Boolean { }
   public class  Type { }
   public class  ValueType { }
   public class  Enum { }
   public struct Void { }
   public class  Array { }
   public class  Exception { }
   public class  ParamArrayAttribute { }
   public struct RuntimeTypeHandle { }
   public struct RuntimeFieldHandle { }
   public class  Attribute { }
   public class  Delegate { }
   public class  MulticastDelegate { }
   public class  String { }
   public interface IDisposable { }
   public enum AttributeTargets { Assembly = 1, Class = 4, }

   [AttributeUsage(AttributeTargets.Class, Inherited = true)]
   public sealed class AttributeUsageAttribute : Attribute {
       public AttributeUsageAttribute(AttributeTargets validOn) { }
       public bool AllowMultiple { get; set; }
       public bool Inherited { get; set; }
   }
}

namespace System.Runtime.InteropServices {
   public class OutAttribute { }
}

namespace System.Runtime.Versioning {
   [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
   public sealed class TargetFrameworkAttribute : Attribute {
       public TargetFrameworkAttribute(String frameworkName) { }
       public String FrameworkDisplayName { get; set; }
   }
 }

namespace System.Reflection {
   public class DefaultMemberAttribute { }
}

namespace System.Collections {
   public interface IEnumerable { }
   public interface IEnumerator { }
}
