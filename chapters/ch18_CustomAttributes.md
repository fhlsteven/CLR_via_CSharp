# 第 18 章 定制特性

本章内容

* <a href="#18_1">使用定制特性</a>
* <a href="#18_2">定义自己的特性类</a>
* <a href="#18_3">特性构造器和字段/属性数据类型</a>
* <a href="#18_4">检测定制特性</a>
* <a href="#18_5">两个特性实例的相互匹配</a>
* <a href="#18_6">检测定制特性时不创建从 `Attribute` 派生的对象</a>
* <a href="#18_7">条件特性类</a>

本章讨论 Microsoft .NET Framework 提供的最具创意的功能之一：**定制特性**(custom attribute)。利用定制特性，可宣告式地为自己的代码构造添加注解来实现特殊功能。定制特性允许为几乎每一个元数据表记录项定义和应用信息。这种可扩展的元数据信息能在运行时查询，从而动态改变代码的执行方式。使用各种 .NET Framework 技术(Windows 窗体、 WPF 和 WCF 等)，会发现它们都利用了定制特性，目的是方便开发者在代码中表达式他们的意图。任何 .NET Framework 开发人员都有必要完全掌握定制特性。

## <a name="18_1">18.1 使用定制特性</a>

我们都知道能将 `public`，`private`，`static` 这样的特性应用于类型和成员。我们都同意应用特性具有很大的作用。但是，如果能定义自己的特性，会不会更有用？例如，能不能定义一个类型，指出该类型能通过序列化来进行远程处理？能不能将特性应用于方法，指出执行该方法需要授予特定安全权限？

为类型和方法创建和应用用户自定义的特性能带来极大的便利。当然，编译器必须理解这些特性，才能在最终的元数据中生成特性信息。由于编译器厂商一般不会发布其编译器产品的源代码，所以 Microsoft 采取另一种机制提供对用户自定义特性的支持。这个机制称为**定制特性**。它的功能很强大，在应用程序的设计时和运行时都能发挥重要作用。任何人都能定义和使用定制特性。另外，面向 CLR 的所有编译器都必须识别定制特性，并能在最终的元数据中生成特性信息。

关于自定义特性，首先要知道它们只是将一些附加信息与某个目标元素关联起来的方式。编译器在托管模块的元数据中生成(嵌入)这些额外的信息。大多数特性对编译器来说没有意义；编译器只是机械地检测源代码中的特性，并生成对应的元数据。

.NET Framework 类库(FCL) 定义了几百个定制特性，可将它们应用于自己源代码中的各种元素。下面是一些例子。

* 将 `DllImport` 特性应用于方法，告诉 CLR 该方法的实现位于指定 DLL 的非托管代码中。

* 将 `Serializable` 特性应用于类型，告诉序列化格式化器<sup>①</sup>一个实例的字段可以序列化和反序列化。

> ① “格式化器”是本书的译法，文档翻译成“格式化程序”。格式化器是实现了 `System.Runtime.Serialization.IFormatter` 接口的类型，它知道如何序列化和反序列化一个对象图。————译注 

* 将 `AssemblyVersion` 特性应用于程序集，设置程序集的版本号。

* 将 `Flags` 特性应用于枚举类型，枚举类型就成了位标志(bit flag)集合。

以下 C# 代码应用了大量特性。在 C# 中，为了将定制特性应用于目标元素，要将特性放置于目标元素前的一对方括号中。代码本身做的事情不重要，重要的是对特性有一个认识。

```C#
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal sealed class OSVERSIONINFO {
    public OSVERSIONINFO() {
        OSVersionInfoSize = (UInt32) Marshal.SizeOf(this);
    }

    public UInt32 OSVersionInfoSize = 0;
    public UInt32 MajorVersion = 0;
    public UInt32 MinorVersion = 0;
    public UInt32 BuildNumer = 0;
    public UInt32 PlatformId = 0;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public String CSDVersion = null;
}

internal sealed class MyClass {
    [DllImport("Kernel32", CharSet.Auto, SetLastError = true)]
    public static extern Boolean GetVersionEx([In, Out] OSVERSIONINFO ver);
}
```

在这里，`StructLayout` 特性应用于 `OSVERSIONINFO` 类，`MarshalAs` 特性引用于 `CSDVersion` 字段，`DLLImport`特性应用于`GetVersionEx` 方法，而`In`和 `Out`特性应用于`GetVersionEx` 方法的 `ver` 参数。每种编程语言都定义了将定制特性应用于目标元素所采用的语法。例如，Microsoft Visual Basic .NET 要求使用一对尖括号(<>)而不是方括号。

CLR 允许将特性应用于可在文件的元数据中表示的几乎任何东西。不过，最常应用特性的还是以下定义表中的记录项：`TypeDef`(类、结构、枚举、接口和委托)，`MethodDef`(含构造器)，`ParamDef`，`FieldDef`，`PropertyDef`，`EventDef`，`AssemblyDef`和`ModuleDef`。更具体地说，C# 只允许将特性应用于定义以下任何目标元素的源代码：程序集、模块、类型(类、结构、枚举、接口、委托)、字段、方法(含构造器)、方法参数、方法返回值、属性、事件和泛型类型参数。

应用特性时，C# 允许用一个前缀明确指定特性要应用于的目标元素。以下代码展示了所有可能的前缀。许多时候，即使省略前缀，编译器也能判断特性要应用于什么目标元素(就像上例展示的那样)。但在其他时候，必须指定前缀向编译器清楚表明我们的意图。下面倾斜显示的前缀是必须的。

```C#
using System;

[assembly: SomeAttr]                    // 应用于程序集
[module: SomeAttr]                      // 应用于模块

[type: SomeAttr]                        // 应用于类型
internal sealed class SomeType<[typevar: SomeAttr] T> {     // 应用于泛型类型变量

    [field: SomeAttr]                   // 应用于字段
    public Int32 SomeField = 0;

    [return: SomeAttr]                  // 应用于返回值
    [method: SomeAttr]                  // 应用于方法
    public Int32 SomeMethod {
        [param: SomeAttr]               // 应用于参数
        Int32 SomeParam) { return SomeParam; }
    }

    [property: SomeAttr]                // 应用于属性
    public String SomeProp {
        [method: SomeAttr]              // 应用于 get 访问器方法
        get { return null; }
    }

    [event: SomeAttr]                   // 应用于事件
    [field: SomeAttr]                   // 引用于编译器生成的字段
    [method: SomeAttr]                  // 应用于编译器生成的 add & remove 方法
    public event EventHandler SomeEvent;
}
```

前面介绍了如何应用定制特性，现在看看特性到底是什么。定制特性其实是一个类型的实例。为了符合“公共语言规范”(CLS)的要求，定制特性类必须直接或间接从公共抽象类 `System.Attribute`派生。C# 只允许符合 CLS 规范的特性。查看文档会发现定义了以下类(参见前面的例子)；`StructLayoutAttribute`，`MarshalAsAttribute`，`DllImportAttribute`，`InAttribute` 和 `OutAttribute`。所有这些类碰巧都在`System.Runtime.InteropServices`命名空间中定义。但特性类可以在任何命名空间中定义。进一步查看，会发现所有这些类都从`System.Attribute`派生，所有符合 CLS 规范的特性类都肯定从这个类派生类。

> 注意 将特性应用于源代码中的目标元素时，C#编译器允许省略 `Attribute` 后缀以减少打字量，并提升源代码的可读性。本章许多示例代码都利用了 C# 提供的这一便利。例如，许多源代码用的都是`[DllImport(...)]`，而不是`[DllImportAttribute(...)]`。

如前所述，特性是类的实例。类必须有公共构造器才能创建它的实例。所以，将特性应用于目标元素时，语法类似于调用类的某个实例构造器。除此之外，语言可能支持一些特殊的语法，允许设置与特性类关联的公共字段或属性。前面的例子将`DllImport` 特性应用于`GetVersionEx`方法：

`[DllImport("Kernel32", CharSet = CharSet.Auto, SetLastError = true)]`

这一行代码的语法表面上看很奇怪，因为调用构造器时永远不会使用这样的语法。查阅`DllImportAttribute` 类的文档，会发现它的构造器要求接受一个 `String` 参数。在这个例子中。“`Kernel32`”将传给这个参数。构造器的参数称为**定位参数**(positional parameter)，而且是强制性的：也就是说，应用特性时必须指定参数。

那么，另外两个“参数”是什么呢？这种特殊的语法允许在构造好 `DllImportAttribute` 对象后设置对象的任何公共字段或属性。在这个例子中，将“`Kernel32`”传给构造器并构造好 `DllImportAttribute` 对象之后，对象的公共实例字段 `CharSet` 和 `SetLastError` 分别设为`CharSet.Auto`和`true`。用于设置字段或属性的“参数”称为**命名参数**(named parameter)。这种参数是可选的，因为在应用特性的实例时不一定要指定参数。稍后会解释是什么导致了实际地构造`DllImportAttribute`类的实例。

还要注意，可将多个特性应用于一个目标元素。例如，在本章的第一个示例程序中，`GetVersionEx` 方法的 `ver` 参数同时应用了 `In` 和 `Out` 这两个特性。将多个特性应用于单个目标元素时，注意特性的顺序无关紧要。另外，在 C# 中，既可将每个特性都封闭到一对方括号中，也可在一对方括号中封闭多个以逗号分隔的特性。如果特性类的构造器不获取参数，那么圆括号可以省略。最后，就像前面说到的那样，`Attribute`后缀也是可选的。下面代码行具有相同的行为，它们演示了应用多个特性时所有可能的方式：

```C#
[Serializable][Flags]
[Serializable, Flags]
[FlagsAttribute, SerializableAttribute]
[FlagsAttribute()][Serializable()]
```

## <a name="18_2">18.2 定义自己的特性类</a>

现在已经知道特性是从 `System.Attribute` 派生的一个类的实例，也知道了如何应用特性。接着研究如何定义定制特性类。假定你是 Microsoft 的员工，负责为枚举类型添加位标志(bit flag)支持，那么要做的第一件事就是定义一个 `FlagsAttribute` 类：

```C#
namespace System {
    public class FlagsAttribute : System.Attribute {
        public FlagsAttribute() { 
        }
    }
}
```

注意，`FlagsAttribute` 类从 `Attribute` 继承。这使 `FlagsAttribute` 类成为符合 CLS 规范的定制特性。此外，注意类名有`Attribute`后缀；这使为了保持与标准的相容性，但这并不是必须的。最后，所有非抽象特性至少要包含一个公共构造器。上述代码中的`FlagsAttribute` 构造器非常简单，不获取任何参数，也不做任何事情。

> 重要提示 应将特性想像成逻辑状态容器。也就是说，虽然特性类型是一个类，但这个类应该很简单。应该只提供一个公共构造器来接受特性的强制性(或定位性)状态信息，而且这个类可以提供公共字段和属性，以接受特性的可选(或命名)状态信息。类不应提供任何公共方法、事件或其他成员。

> 我通常不鼓励使用公共字段。特性也不例外，我同样不鼓励在这种类型中使用公共字段。使用属性要好得多。因为在更改特性类的实现方式时，属性能提供更大的灵活性。


现在的情况是`FlagsAttribute` 类的实例能应用于任何目标元素。但事实上，这个特性应该只能应用于枚举类型，应用于属性或方法是没有意义的。为了告诉编译器这个特性的合法应用范围，需要向特性类应用`System.AttributeUsageAttribute` 类的实例。下面是新的代码：

```C#
namespace System {

    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]

    public class FlagsAttribute : System.Attribute {

        public FlagsAttribute() {

        } 
    }
}
```

新版本将 `AttributeUsageAttribute` 的实例应用于特性。毕竟，特性类型本质上还是类，而类是可以应用特性的。`AttributeUsage`特性是一个简单的类，可利用它告诉编译器定制特性的合法应用范围。所有编译器都内建了对该特性的支持，并会在定制特性应用于无效目标时报错。在这个例子中，`AttributeUsage` 特性指出 `Flags` 特性的实例只能应用于枚举类型的目标。

由于特性不过是类型，所以 `AttributeUsageAttribute` 类理解起来很容易。以下是该类的 FCL 源代码：

```C#
[Serializable]
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class AttributeUsageAttribute : Attribute {
    internal static AttributeUsageAttribute Default = new AttributeUsageAttribute(AttributeTargets.All);

    internal Boolean m_allowMultiple = false;
    internal AttributeTargets m_attributeTarget = AttributeTargets.All;
    internal Boolean m_inherited = true;

    // 这是一个公共构造器
    public AttributeUsageAttribute(AttributeTargets validOn) {
        m_attributeTarget = validOn;
    }

    public AttributeUsageAttribute(AttributeTargets validOn,
            Boolean allowMultiple, Boolean inherited) {
        m_attributeTarget = validOn;
        m_allowMultiple = allowMultiple;
        m_inherited = inherited;
    }

    public Boolean AllowMultiple {
        get { return m_inherited; }
        set { m_allowMultiple = value; }
    }

    public bool Inherited {
        get { return m_inherited;  }
        set { m_inherited = value; }
    }

    public AttributeTargets ValidOn {
        get { return m_attributeTarget; }
    }
}
```

如你所见，`AttributeUsageAttribute` 类有一个公共构造器，它允许传递位标志(bit flag)来指明特性的合法应用范围。`System.AttributeTargets` 枚举类型在 FCL 中是像下面这样定义的：

```C#
[Flags, Serializable]
public enum AttributeTargets
{ 
	Assembly			= 0x0001,
	Module				= 0x0002, 
	Class				= 0x0004, 
	Struct				= 0x0008, 
	Enum				= 0x0010, 
	Constructor			= 0x0020, 
	Method				= 0x0040, 
	Property			= 0x0080, 
	Field				= 0x0100, 
	Event				= 0x0200, 
	Interface			= 0x0400, 
	Parameter			= 0x0800,
	Delegate			= 0x1000, 
	ReturnValue			= 0x2000,
	GenericParameter	= 0x4000, 
	All = Assembly      | Module | Class | Struct | Enum |
		  Constructor   | Method | Property | Field | Event |
		  Interface     | Parameter | Delegate | ReturnValue |
		  GenericParameter
}
```

`AttributeUsageAttribute` 类提供了两个附加的公共属性，即`AllowMultiple` 和 `Inherited`。可在向特性类应用特性时选择设置这两个属性。

大多数特性多次应用于同一个目标是没有意义的。例如，将 `Flags` 或 `Serializable` 特性多次应用于同一个目标不会有任何好处。事实上，编译如下所示的代码：

```C#
[Flags][Flags]
internal enum Color {
    Red
}
```

编译器会报告以下错误:

`error CS0579: 重复的“Flags”特性。`

但少数几个特性确实有必要多次应用于同一个目标。FCL 特性类 `ConditionalAttribute` 允许将它的多个实例应用于同一个目标元素。不将`AllowMultiple` 明确设为 `true`，特性就只能向选定的目标元素应用一次。

`AttributeUsageAttribute` 的另一个属性是 `Inherited`,它指出特性在应用于基类时,是否同时应用于派生类和重写的方法。以下代码演示了特性的继承：

```C#
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
internal class TastyAttribute : Attribute {
}

[Tasty][Serializable]
internal class BaseType {

    [Tasty] protected virtual void DoSomething() { }
}

internal class DerivedType : BaseType {
    protected override void DoSomething() { }
}
```

在上述代码中，`DerivedType` 及其 `DoSomething` 方法都被视为 `Tasty`，因为 `TastyAttribute` 类被标记为可继承。但 `DerivedType` 不可序列化，因为 FCL 的 `SerializableAttribute` 类被标记为不可继承。

注意，.NET Framework 只认为类、方法、属性、事件、字段、方法返回值和参数等目标元素是可继承的。所以，定义特性类型时，只有在该特性应用于上述某个目标的前提下，才应该将 `Inherited` 设为 `true`。注意，可继承特性不会造成在托管模块中为派生类型生成额外的元数据。18.4 节 “检测定制特性” 将进一步讨论这方面的问题。

> 注意 定义自己的特性类时，如果忘记向自己的类应用 `AttributeUsage` 特性，编译器和 CLR 将假定该特性能应用于所有目标元素，向每个目标元素都只能应用一次，而且可继承。这些假定模仿了 `AttributeUsageAttribute` 类中的默认字段值。

## <a name="18_3">18.3 特性构造器和字段/属性数据类型</a>

定制特性类可定义构造器来获取参数.开发人员在应用特性类的实例时必须指定这些参数.还可在类中定义非静态公共字段和属性,使开发人员能为特性类的实例选择恰当的设置.

定义特性类的实例构造器、字段和属性时，可供选择的数据类型并不多。具体地说，只允许 `Boolean`，`Char`，`Byte`，`SByte`，`Int16`，`UInt16`，`Int32`，`UInt32`，`Int64`，`UInt64`，`Single`，`Double`，`String`，`Type`，`Object`或枚举类型。此外，可使用上述任意类型的一维 0 基数组。但应尽量避免使用数组，因为对于定制特性，如果它的构造器要获取数组作为参数，就会失去与 CLS 的相容性。

应用特性时必须传递一个编译时常量表达式，它与特性类定义的类型匹配。在特性类定义了一个`Type`参数、`Type`字段或者`Type`属性的任何地方，都必须使用 C# `typeof` 操作符(如下例所示)。在特性类定义了一个 `Object` 参数、`Object`字段或者 `Object` 属性的任何地方，都可传递一个 `Int32`、`String` 或其他任何常量表达式(包括`null`)。如果常量表达式代表值类型，那么在运行时构造特性的实例时会对值类型进行装箱。以下是一个示例特性及其用法：

```C#
using System;

internal enum Color { Red }

[AttributeUsage(AttributeTargets.All)]
internal sealed class SomeAttribute : Attribute {
    public SomeAttribute(String name, Object o, Type[] types) {
        // ‘name’ 引用一个 String
        // 'o' 引用一个合法的类型(如有必要，就进行装箱)
        // 'types' 引用一个一维 0 基 Type 数组
    }
}

[Some("Jeff", Color.Red, new Type[] { typeof(Math), typeof(Console) })]
internal sealed class SomeType {
}
```

逻辑上，当编译器检测到向目标元素应用了定制特性时，会调用特性类的构造器，向它传递任何指定的参数，从而构造特性类的实例。然后，编译器采用增强型构造器语法所指定的值，对任何公共字段和属性进行初始化。构造并初始化好定制特性类的对象之后，编译器将它的状态序列化到目标元素的元数据表记录项中。

> 重要提示 为方便理解，可以这样想象定制特性：它是类的实例，被序列化成驻留在元数据中的字节流。运行时可对元数据中的字节进行反序列化，从而构造出类的实例。实际发生的事情是：编译器在元数据中生成创建特性类的实例所需的信息。每个构造器参数都会 1 字节的类型 ID，后跟具体的值。对构造器的参数进行“序列化”时，编译器先写入字段/属性名称，再跟上 1 字节的类型 ID，最后是具体的值。如果是数组，则会先保存数组元素的个数，再跟上每个单独的元素。

## <a name="18_4">18.4 检测定制特性</a>

仅仅定义特性类没有用。确实可以定义自己想要的所有特性类，并应用自己想要的所有实例。但这样除了在程序集中生成额外的元数据，没有其他任何意义。应用程序代码的行为不会有任何改变。

第 15 章 “枚举类型和位标志” 描述了如何将 `Flags` 特性应用于枚举类型，从而改变 `System.Enum` 的 `ToString` 和 `Format` 方法的行为。方法的行为之所以改变，是因为它们会在运行时检查自己操作的枚举类型是否关联了 `Flags` 特性元数据。代码利用一种称为**反射**的技术检测特性的存在。这里只是简单地演示一下反射。第 23 章 “程序集加载和反射”会完整地讨论这种技术。

假定你是 Microsoft 的员工，负责实现 `Enum` 的 `ToString` 方法，你会像下面这样实现它：

```C#
public override String ToString() {

    // 枚举类型是否应用了 FlagsAttribute 类型的实例 ？
    if (this.GetType().IsDefined(typeof(FlagsAttribute), false)) {
        // 如果是，就执行代码，将值视为一个位标志枚举类型
        ...
    } else {
        // 如果不是，就执行代码，将值视为一个普通枚举类型
        ...
    }
    ...
}
```

上述代码调用 `Type` 的 `IsDefined` 方法，要求系统查看枚举类型的元数据，检查是否关联了 `FlagsAttribute` 类的实例。如果`IsDefined`返回`true`，表明`FlagsAttribute`的一个实例已与枚举类型关联，`ToString`方法认为值包含一个位标志(bit flag)集合。如果`IsDefined`返回`false`，`Format`方法认为值是普通的枚举类型。

因此，在定义定制特性时，也必须实现一些代码来检测某些目标上是否存在该特性类的实例，然后执行一些逻辑分支代码。这样定制特性才能正真发挥作用。

FCL 提供了多种方式来检测特性的存在。如果通过 `System.Type` 对象来检测特性，可以像前面展示的那样使用 `IsDefined` 方法。但有时需要检测除了类型之外的其他目标(比如程序集、模块或方法)上的特性。为简化讨论，让我们聚焦于 `System.Reflection.CustomAttributeExtensions` 类定义的扩展方法。该类定义了三个静态方法来获取与目标关联的特性：`IsDefined`，`GetCustomAttributes` 和 `GetCustomAttribute`。 每个方法都有几个重载版本。例如，每个方法都有一个版本能操作类型成员(类、结构、枚举、接口、委托、构造器、方法、属性、字段、事件和返回类型)、参数、模块和程序集。还有一些版本能指示系统遍历继承层次结构，在结果中包含继承的特性。表 18-1 简要总结了每个方法的用途，它们在元数据上反射以查找与 CLS 相容的定制特性类型的实例。

表 18-1 `System.Reflection.CustomAttributeExtensions`定义的三个静态方法
|方法名称|说明|
|:---:|:---:|
|`IsDefined`|如果至少有一个指定的 `Attribute` 派生类的实例与目标关联，就返回 `true`。这个方法效率很高，因为它不构造(反序列化)特性类的任何实例|
|`GetCustomAttributes`|返回应用于目标的指定特性对象的集合。每个实例都使用编译时指定的参数、字段和属性来构造(反序列化)。如果目标没有应用指定特性类的实例，就返回一个空集合。该方法通常用于已将 `AllowMultiple` 设为 `true` 的特性，或者用于列出已应用的所有特性|
|`GetCustomAttribute`|返回应用于目标的指定特性类的实例。实例使用编译时指定的参数、字段和属性来构造(反序列化)。如果目标没有应用指定特性类的实例，就返回 `null`，如果目标应用了指定特性的多个实例，就抛出 `System.Reflection.AmbiguousMatchException` 异常。该方法通常用于已将 `AllowMultiple` 设为 `false` 的特性|

如果只想判断目标是否应用了一个特性，那么应该调用 `IsDefined`，因为它比另两个方法更高效。但我们知道，将特性应用于目标时，可以为特性的构造器指定参数，并可选择设置字段和属性。使用 `IsDefined` 不会构造特性对象，不会调用构造器，也不会设置字段和属性。

要构造特性对象，必须调用 `GetCustomAttributes` 或 `GetCustomAttribute` 方法。每次调用这两个方法，都会构造指定特性类型的新实例，并根据源代码中指定的值来设置每个实例的字段和属性。两个方法返回的都是对完全构造好的特性类实例的引用。

调用上述任何方法，内部都必须扫描托管模块的元数据，执行字符串比较来定位指定的定制特性类。显然，这些操作会耗费一定时间。假如对性能的要求比较高，可考虑缓存这些方法的调用结果，而不是反复调用来请求相同的信息。

`System.Reflection` 命名空间定义了几个类允许检查模块的元数据。这些类包括 `Assembly`，`Module`，`ParameterInfo`，`MemberInfo`，`Type`，`MethodInfo`，`Type`，`MethodInfo`，`ConstructorInfo`，`FieldInfo`，`EventInfo`，`PropertyInfo`及其各自的 `*Builder` 类。所有类都提供了 `IsDefined` 和 `GetCustomAttributes`方法。

反射类提供的 `GetCustomAttribute` 方法返回的是由 `Object` 实例构成的数组(`Object[]`)，而不是由 `Attribute` 实例构成的数组(`Attribute[]`)。这是由于反射类能返回不相容于 CLS 规范的特性类的对象。不过，大可不必关心这种不一致性，因为非 CLS 相容的特性是很稀少的。事实上，我与 .NET Framework 打交道至今，还没有见过一例。

> 注意 只有 `Attribute`，`Type` 和 `MethodInfo` 类才实现了支持 `Boolean` `inherit` 参数的反射方法。其他能检查特性的所有反射方法都会忽略 `inherit` 参数，而且不会检查继承层次结构。要检查事件、属性、字段、构造器或参数是否应用了继承的特性，只能调用`Attribute`的某个方法。

还要注意，将一个类传给 `IsDefined`，`GetCustomAttribute`或者`GetCustomAttribute`方法时，这些方法会检测是否应用了指定的特性类或者它的派生类。如果只是想搜索一个具体的特性类，应针对返回值执行一次额外的检查，确保方法返回的正是想搜索的类。还可考虑将自己的特性类定义成 `sealed`，减少可能存在的混淆，并避免执行这个额外的检查。

以下示例代码列出了一个类型中定义的所有方法，并显示应用于每个方法的特性。代码仅供演示，平时不会像这样将这些定制特性应用于这些目标元素。

```C#
using System;
using System.Diagnostics;
using System.Reflection;
using System.Linq;

[assembly: CLSCompliant(true)]

[Serializable]
[DefaultMemberAttribute("Main")]
[DebuggerDisplayAttribute("Richter", Name = "Jeff", Target = typeof(Program))]
public sealed class Program {
    [Conditional("Debug")]
    [Conditional("Release")]
    public void DoSomething() { }

    public Program() { 
    }

    [CLSCompliant(true)]
    [STAThread]
    public static void Main() {
        // 显示应用于这个类型的特性集
        ShowAttributes(typeof(Program));

        // 获取与类型关联的方法集
        var members = from m in typeof(Program).GetTypeInfo().DeclaredMembers.OfType<MethodBase>()
                      where m.IsPublic
                      select m;

        foreach (MemberInfo member in members) {
            // 显示应用于这个成员的特性集
            ShowAttributes(member);
        }
    }

    private static void ShowAttributes(MemberInfo attributeTarget) {
        var attributes = attributeTarget.GetCustomAttributes<Attribute>();

        Console.WriteLine("Attributes applied to {0}: {1}",
            attributeTarget.Name, (attributes.Count() == 0) ? "None" : String.Empty);

        foreach (Attribute attribute in attributes) {
            // 显示所应用的每个特性的类型
            Console.WriteLine(" {0}", attribute.GetType().ToString());

            if (attribute is DefaultMemberAttribute)
                Console.WriteLine(" MemberName={0}", ((DefaultMemberAttribute)attribute).MemberName);

            if (attribute is ConditionalAttribute)
                Console.WriteLine(" ConditionalString={0}", ((ConditionalAttribute)attribute).ConditionString);

            if (attribute is CLSCompliantAttribute)
                Console.WriteLine(" ISCompliant={0}", ((CLSCompliantAttribute)attribute).IsCompliant);

            DebuggerDisplayAttribute dda = attribute as DebuggerDisplayAttribute;
            if (dda != null) {
                Console.WriteLine(" Value={0}, Name={1}, Target={2}", dda.Value, dda.Name, dda.Target);
            }
        }
        Console.WriteLine();
    }
}
```

编译并运行上述应用程序得到以下输出：

```cmd
Attributes applied to Program: 
 System.SerializableAttribute
 System.Reflection.DefaultMemberAttribute
 MemberName=Main
 System.Diagnostics.DebuggerDisplayAttribute
 Value=Richter, Name=Jeff, Target=Program

Attributes applied to DoSomething: 
 System.Diagnostics.ConditionalAttribute
 ConditionalString=Debug
 System.Diagnostics.ConditionalAttribute
 ConditionalString=Release

Attributes applied to Main: 
 System.CLSCompliantAttribute
 ISCompliant=True
 System.STAThreadAttribute

Attributes applied to .ctor: None
```

## <a name="18_5">18.5 两个特性实例的相互匹配</a>

除了判断是否向目标应用了一个特性的实例，可能还需要检查特性的字段来确定它们的值。一个办法是老老实实写代码检查特性类的字段值。但`System.Attribute` 重写了 `Object` 的 `Equals` 方法，会在内部比较两个对象的类型。不一致会返回 `false`。如果一致，`Equals` 会利用反射来比较两个特性对象中的字段值(为每个字段都调用 `Equals`)。所有字段都匹配就返回 `true`；否则返回 `false`。可在自己的定制特性类中重写 `Equals` 来移除反射的使用，从而提升性能。

`System.Attribute` 还公开了虚方法 `Match`，可重写它来提供更丰富的语义。`Match`的默认实现只是调用 `Equal` 方法并返回它的结果。下例演示了如何重写 `Equals` 和 `Match`， 后者在一个特性代表另一个特性的子集的前提返回 `true`。另外，还演示了如何使用 `Match`。

```C#
using System;

[Flags]
internal enum Accounts {
    Savings   = 0x0001,
    Checking  = 0x0002,
    Brokerage = 0x0004
}

[AttributeUsage(AttributeTargets.Class)]
internal sealed class AccountsAttribute : Attribute {
    private Accounts m_accounts;

    public AccountsAttribute(Accounts accounts) {
        m_accounts = accounts;
    }

    public override Boolean Match(Object obj) {
        // 如果基类实现了 Match， 而且基类不是
        // Attribute，就取消对下面这行代码的注释
        // if (!base.Match(obj)) return false;

        // 出于 'this' 不为 null，所以假如 obj 为 null，
        // 那么对象肯定不匹配 
        // 注意：如果你信任基类正确实现了 Match，
        // 那么下面这一行可以删除
        if (obj == null) return false;

        // 如果对象属于不同的类型，肯定不匹配
        // 注意：如果你信任基类正确实现了 Match，
        // 那么下面这一行可以删除
        if (this.GetType() != obj.GetType()) return false;

        // 将 obj 转型为我们的类型以访问字段
        // 注意：转型不可能失败，因为我们知道
        // 连个对象是相同的类型
        AccountsAttribute other = (AccountsAttribute)obj;

        // 比较字段，判断它们是否有相同的值
        // 这个例子判断 'this' 的账户是不是
        // other 的账户的一个子集
        if ((other.m_accounts & m_accounts) != m_accounts)
            return false;

        return true;    // 对象匹配
    }

    public override Boolean Equals(Object obj) {
        // 如果基类实现了 Equals，而且基类不是
        // Object，就取消对下面这行代码的注释：
        // if (!base.Equals(obj)) return false

        // 由于 'this' 不为 null，所以假如 object 为 null，
        // 那么对象肯定不相等
        // 注意：如果你信基类正确实现了 Equals
        // 那么下面这一行可以删除
        if (obj == null) return false;

        // 如果对象属于不同的类型，肯定不相等
        // 注意：如果你信任基类正确实现了 Equals,
        // 那么下面这一行可以删除
        if (this.GetType() != obj.GetType()) return false;

        // 将 obj 转型为我们的类型以访问字段
        // 注意：转型不可能失败，因为我们知道
        // 两个对象是相同的类型
        AccountsAttribute other = (AccountsAttribute)obj;

        // 比较字段，判断它们是否有相同的值
        // 这个例子判断 'this' 的账户是不是
        // 与 other 的账户相同
        if (other.m_accounts != m_accounts)
            return false;

        return true;    // 对象相等
    }

    // 重写 GetHashCode，因为我们重写了 Equals
    public override Int32 GetHashCode() {
        return (Int32)m_accounts;
    }
}

[Accounts(Accounts.Savings)]
internal sealed class ChildAccount { }

[Accounts(Accounts.Savings | Accounts.Checking | Accounts.Brokerage)]
internal sealed class AdultAccount { }

public sealed class Program {
    public static void Main() {
        CanWriteCheck(new ChildAccount());
        CanWriteCheck(new AdultAccount());

        // 只是为了演示在一个没有应用 AccountsAttribute 的类型上，
        // 方法也能正确地工作
        CanWriteCheck(new Program());
    }

    private static void CanWriteCheck(Object obj) {
        // 构造 attribute 类型的一个实例，并把它初始化成
        // 我们要显式查找的内容
        Attribute checking = new AccountsAttribute(Accounts.Checking);

        // 构造应用于类型的特性实例
        Attribute validAccounts = Attribute.GetCustomAttribute(obj.GetType(), typeof(AccountsAttribute), false);

        // 如果向精英应用了特性，而且特性指定了
        // “Checking”账户，表明该类型可以开支票
        if ((validAccounts != null) && checking.Match(validAccounts)) {
            Console.WriteLine("{0} types can write checks.", obj.GetType());
        } else {
            Console.WriteLine("{0} types can NOT write checks.", obj.GetType());
        }
    }
}
```

编译并运行这个应用程序，会得到以下输出：

```cmd
ChildAccount types can NOT write checks.
AdultAccount types can write checks.
Program types can NOT write checks.
```


## <a name="18_6">18.6 检测定制特性时不创建从 Attribute 派生的对象</a>

本节将讨论如何利用另一种技术检测应用于元数据记录项的特性。在某些安全性要求严格的场合，这个技术能保证不执行从 `Attribute` 的 `GetCustomAttribute` 或者 `GetCustomAttributes` 方法时，这些方法会在内部调用特性类的构造器，而且可能调用属性的 `set` 访问器。此外，首次访问类型会造成 CLR 调用类型的类型构造器(如果有的话)。在构造器、`set`访问器方法以及类型构造器中，可能包含每次查找特性都要执行的代码。这就相当于允许未知代码在 `AppDomain` 中运行，所以存在安全隐患。

可用 `System.Reflection.CustomAttributeData` 类在查找特性的同时禁止执行特性类中的代码。该类定义了静态方法 `GetCustomAttributes` 来获取与目标关联的特性。方法有 4 个重载版本，分别获取一个 `Assembly`，`Module`，`ParameterInfo` 和 `MemberInfo`。 该类在 `System.Reflection` 命名空间(将在第 23 章“程序集加载和反射” 讨论)中定义。通过，先用 `Assembly` 的静态方法 `ReflectionOnlyLoad`(也在第 23 章讨论)加载程序集，再用`CustomAttributeData`类分析这个程序集的元数据中的特性。简单地说，`ReflectionOnlyLoad` 以特殊方式加载程序集，期间会禁止 CLR 执行程序集中的任何代码；其中包括类型构造器。

`CustomAttributeData` 的 `GetCustomAttributes` 方法是一个工厂(factory)方法。也就是说，调用它会返回一个`IList<CustomAttributeData>` 类型的对象，其中包含了由 `CustomAttributeData`对象构成的集合。集合中的每个元素都是应用于指定目标的一个定制特性。可查询每个`CustomAttributeData` 对象的只读属性，判断特性对象如何构造和初始化。具体地说，`Constructor` 属性指出构造器方法将如何调用。`ConstructorArguments` 属性以一个 `IList<CustomAttributeTypedArgument>` 实例的形式返回将传给这个构造器的实参。而`NamedArguments`属性以一个 `IList<CustomAttributeNamedArgument>` 实例的形式，返回将设置的字段/属性。注意，之所以说“将”，是因为不会实际地调用构造器和 `set` 访问器方法。禁止执行特性类的任何方法增强了安全性。

下面是之前例子的修改版本，它利用 `CustomAttributeData` 类来安全地获取应用于各个目标的特性：

```C#
using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

[assembly: CLSCompliant(true)]

[Serializable]
[DefaultMemberAttribute("Main")]
[DebuggerDisplayAttribute("Richter", Name = "Jeff" , Target = typeof(Program))]
public sealed class Program {
    [Conditional("Debug")]
    [Conditional("Release")]
    public void DoSomething() { }

    public Program() { 
    }

    [CLSCompliant(true)]
    [STAThread]
    public static void Main() {
        // 显示应用于这个类型的特性类
        ShowAttributes(typeof(Program));

        // 获取与类型关联的方法集
        var members = from m in typeof(Program).GetTypeInfo().DeclaredMembers.OfType<MethodBase>()
                      where m.IsPublic
                      select m;

        foreach (MemberInfo member in members) {
            // 显示应用于这个成员的特性集
            ShowAttributes(member);
        }
    }

    private static void ShowAttributes(MemberInfo attributeTarget)  {
        IList<CustomAttributeData> attributes = CustomAttributeData.GetCustomAttributes(attributeTarget);

        Console.WriteLine("Attributes applied to {0}: {1}", attributeTarget.Name, (attributes.Count ==0 ? "None" : String.Empty));

        foreach (CustomAttributeData attribute in attributes) {
            // 显示所应用的每个特性的类型
            Type t = attribute.Constructor.DeclaringType;
            Console.WriteLine(" {0}", t.ToString());
            Console.WriteLine("    Constructor called={0}", attribute.Constructor);

            IList<CustomAttributeTypedArgument> posArgs = attribute.ConstructorArguments;
            Console.WriteLine("    Positonal arguments passed to constructor:" + ((posArgs.Count == 0) ? " None" : String.Empty));
            foreach (CustomAttributeTypedArgument pa in posArgs) {
                Console.WriteLine("    Type={0}, Value={1}", pa.ArgumentType, pa.Value);
            }

            IList<CustomAttributeNamedArgument> namedArgs = attribute.NamedArguments;
            Console.WriteLine("    Named arguments set after construction:" + ((namedArgs.Count == 0) ? " None" : String.Empty));
            foreach (CustomAttributeNamedArgument na in namedArgs) {
                Console.WriteLine("    Name={0}, Type={1}, Value={2}", na.MemberInfo.Name, na.TypedValue.ArgumentType, na.TypedValue.Value);
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}
```

编译并运行上述应用程序，将获得以下输出：

```cmd
Attributes applied to Program: 
 System.SerializableAttribute
    Constructor called=Void .ctor()
    Positonal arguments passed to constructor: None
    Named arguments set after construction: None

 System.Reflection.DefaultMemberAttribute
    Constructor called=Void .ctor(System.String)
    Positonal arguments passed to constructor:
    Type=System.String, Value=Main
    Named arguments set after construction: None

 System.Diagnostics.DebuggerDisplayAttribute
    Constructor called=Void .ctor(System.String)
    Positonal arguments passed to constructor:
    Type=System.String, Value=Richter
    Named arguments set after construction:
    Name=Name, Type=System.String, Value=Jeff
    Name=Target, Type=System.Type, Value=Program


Attributes applied to DoSomething: 
 System.Diagnostics.ConditionalAttribute
    Constructor called=Void .ctor(System.String)
    Positonal arguments passed to constructor:
    Type=System.String, Value=Debug
    Named arguments set after construction: None

 System.Diagnostics.ConditionalAttribute
    Constructor called=Void .ctor(System.String)
    Positonal arguments passed to constructor:
    Type=System.String, Value=Release
    Named arguments set after construction: None


Attributes applied to Main: 
 System.CLSCompliantAttribute
    Constructor called=Void .ctor(Boolean)
    Positonal arguments passed to constructor:
    Type=System.Boolean, Value=True
    Named arguments set after construction: None

 System.STAThreadAttribute
    Constructor called=Void .ctor()
    Positonal arguments passed to constructor: None
    Named arguments set after construction: None


Attributes applied to .ctor: None
```

## <a name="18_7">18.7 条件特性类</a>

定义、应用和反射特性能带来许多便利，所以开发人员越来越频繁地使用这些技术。特性简化了对代码的注释，还能实现丰富的功能。进来，开发人员越来越喜欢在设计和调试期间利用特性来辅助开发。例如，Microsoft Visual Studio 代码分析工具(FxCopCmd.exe)提供了一个 `System.Diagnostics.CodeAnalysis.SuppressMessageAttribute` ，可将它应用于类型和成员，从而阻止报告特定的静态分析工具规则冲突(rule violation)。该特性仅对代码分析工具有用；程序平常运行时不会关注它。没有使用代码分析工具时，将 `SuppressMessage` 特性留在元数据中会使元数据无谓地膨胀，这会使文件变得更大，增大进程的工作集，损害应用程序的性能。假如有一种简单的方式，使编译器只有在使用代码分析工具时才生成 `SupperessMessage` 特性，结果会好很多。幸好，利用条件特性类真的能做到这一点。

应用了 `System.Diagnostics.ConditionalAttribute` 的特性类称为**条件特性类**。下面是一个例子：

```C#
// #define TEST
#define VERIFY

using System;
using System.Diagnostics;

[Conditional("TEST")]
[Conditional("VERIFY")]
public sealed class CondAttribute : Attribute {
}

[Cond]
public sealed class Program {
    public static void Main() {
        Console.WriteLine("ConAttribute is {0}applied to Program type.",
            Attribute.IsDefined(typeof(Program), typeof(CondAttribute)) ? "" : "not");
    }
}
```

编译器如果发现向目标元素应用了 `CondAttribute` 的实例，那么当含有目标元素的代码编译时，只有在定义 `TEST` 或 `VERIFY` 符号的前提下，编译器才会在元数据中生成特性信息。不过，特性类的定义元数据和实现仍存在于程序集中。