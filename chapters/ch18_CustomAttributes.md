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

前面介绍了如何应用定制特性，现在看看特性到底是什么。定制特性其实是一个类型的实例。为了符合“公共语言规范”(CLS)的要求，定制特性类必须直接或间接从公共抽象类 `System.Attribute`派生。C# 只允许符合 CLS 规范的特性。查看文档会发现定义了以下````