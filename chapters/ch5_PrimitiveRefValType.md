# 第 5 章 基元类型、引用类型和值类型

本章内容：
* <a href="#5_1">编程语言的基元类型</a>
* <a href="#5_2">引用类型和值类型</a>
* <a href="#5_3">值类型的装箱和拆箱</a>
* <a href="#5_4">对象哈希码</a>
* <a href="#5_5">dynamic 基元类型</a>

本章将讨论 Microsoft .NET Framework 开发人员经常要接触的各种类型。所以开发人员都应熟悉这些类型的不同行为。我首次接触 .NET Framework 时没有完全理解基元类型、引用类型和值类型的区别，造成在代码中不知不觉引入 bug 和性能问题。通过解释类型之间的区别，希望开发人员能避免我所经历的麻烦，同时提高编码效率。

## <a name="5_1">编程语言的基元类型</a>

某些数据类型如此常用，以至于许多编译器允许代码以简化语法来操纵它们。例如，可用以下语法分配一个整数：  

```C#
System.Int32 a = new System.Int32();
```

但你肯定不愿意用这种语法声明并初始化整数，它实在是太繁琐了。幸好，包括 C# 在内的许多编译器都允许换用如下所示的语法：

```C#
int a = 0;
```

这种语法不仅增强了代码可读性，生成的 IL 代码还与使用 `System.Int32` 生成的 IL 代码完全一致。编译器直接支持的数据类型称为 **基元类型**(primitive type)。基元类型直接映射到 Framework 类库(FCL)中存在的类型。例如，C# 的 `int` 直接映射到 `System.Int32` 类型。因此，以下 4 行代码都能正确编译，并生成完全相同的 IL：

```C#
  int            a = 0;                   // 最方便的语法
  System.Int32   a = 0;                   // 方便的语法
  int            a = new int();           // 不方便的语法
  System.Int32   a = new System.Int32();  // 最不方便的语法
```

表 5-1 列出的 FCL 类型在 C# 中都有对应的基元类型。只要是符合公共语言规范(CLS)的类型，其他语言都提供了类似的基元类型。但是，不符合 CLS 的类型语言就不一定要支持了。

表 5-1 C# 基元类型与对应的 FCL 类型
|C#基元类型|FCL类型|符合CLS|说明|
|:---:|:---:|:----:|:---:|
|`sbyte`|`System.SByte`|否|有符合 8 位值|
|`byte`|`System.Byte`|是|无符号 8 位值|
|`short`|`System.Int16`|是|有符号 16 位值|
|`ushort`|`System.UInt16`|否|无符号 16 位值|
|`int`|`System.Int32`|是|有符号 32 位值|
|`uint`|`System.UInt32`|否|无符号 32 位值|
|`long`|`System.Int64`|是|有符号 64 位值|
|`ulong`|`System.UInt64`|否|无符号 64 位值|
|`char`|`System.Char`|是|16位 Unicode 字符(`char` 不像在非托管 C++ 中那样代表一个 8 位置)|
|`float`|`System.Single`|是|IEEE 32 位浮点值|
|`double`|`System.Double`|是|IEEE 64 位浮点值|
|`bool`|`System.Boolean`|是|`true`/`false` 值|
|`decimal`|`System.Decimal`|是|128 位高精度浮点值，常用于不容许舍入误差的金融计算。 128 位中， 1 位是符号，96位是值本身(*N*)，8位是比例引子(*k*)。 `decimal` 实际值是 ±*N*×10<sup>k</supb>，其中 -28<= *k* <=0。其余位没有使用|
|`string`|`System.String`|是|字符数组|
|`object`|`System.Object`|是|所有类型的基类型|
|`dynamic`|`System.Object`|是|对于 CLR， `dynamic` 和 `object` 完全一致。但 C# 编译器允许使用简单的语法让 `dynamic` 变量参与动态调度。详情参见本章最后的 5.5 节“`dynamic` 基元类型”|

从另一个角度，可认为 C# 编译器自动假定所有源代码文件都添加了以下 `using` 指令(参考第 4 章)：

```C#
using sbyte  = System.SByte;
using byte   = System.Byte;
using short  = System.Int16;
using ushort = System.UInt16;
using int    = System.Int32;
using uint   = System.UInt32;
···
```

C# 语言规范称：“从风格上说，最好是使用关键字，而不是使用完整的系统类型名称。”我不同意语言规范：我情愿使用 FCL 类型名称，完全不用基元类型名称。事实上，我希望编译器根本不提供基元类型名称，而是强迫开发人员使用 FCL 类型名称。理由如下。  
1. 许多开发人员纠结于是用 `string` 还是 `String` 。由于 C# 的 `using`(一个关键字)直接映射到 `System.String`(一个 FCL 类型)，所以两者没有区别，都可使用。类似地，一些开发人员说应用程序在 32 位操作系统上运行， `int` 代表 32 位整数；在 64 位操作系统上运行， `int` 代表 64 位整数。这个说法完全错误。C# 的 `int` 始终映射到 `System.Int32`，所以不管在什么操作系统上运行，代表的都是 32 位整数。如果程序员习惯在代码中使用 `Int32`，像这样的误解就没有了。

2. C#的 `long` 映射到 `System.Int64` ，但在其他编程语言中，`long` 可能映射到 `Int16` 或 `Int32`。例如， C++/CLI 就将 `long` 视为 `Int32`。习惯于用一种语言写程序的人在看用另一种语言写程序的人在看用另一种语言写的源代码时，很容易错误理解代码意图。事实上，大多数语言甚至不将 `long` 当作关键字，根本不编译使用了它的代码。

3. FCL 的许多方法都将类型名作为方法名的一部分。例如， `BinaryReader` 类型的方法包括 `ReadBoolean`，`ReadInt32`，`ReadSingle` 等；而 `System.Convert` 类型的方法包括 `ToBoolean`，`ToInt32`，`ToSingle` 等。以下代码虽然语法没问题，但包含 `float` 的那一行显得很别扭，无法一下子判断该行的正确性：  

    ```C#
    BinaryReader br = new BinaryReaser(...);
    float val    = br.ReadSingle();    // 正确，但感觉别扭
    Single val   = br.ReadSingle();    // 正确，感觉自然
    ```
4. 平时只用 C# 的许多程序员逐渐忘了还可以用其他语言写面向 CLR 的代码，“C#主义”逐渐入侵类库代码。例如， Microsoft 的 FCL 几乎是完全用 C# 写的，FCL 团队向库中引入了像 `Array` 的 `GetLongLength` 这样的方法。该方法返回 `Int64` 值。这种值在 C# 中确实是 `long`，但在其他语言(比如 C++/CLI)中不是。另一个例子是 `System.Linq.Enumerable` 的 `LongCount` 方法。

考虑到所有这些原因，本书坚持使用 FCL 类型名称。

在许多编程语言中，以下代码都能正确编译并运行：

```C#
Int32 i = 5;   // 32 位值
Int64 l = i;   // 隐式转型为 64 位值
```

但根据第 4 章对类型转换的讨论，
