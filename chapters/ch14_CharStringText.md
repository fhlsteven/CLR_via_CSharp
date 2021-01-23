第 14 章 字符、字符串和文本处理

本章内容

* <a href="#14_1">字符</a>
* <a href="#14_2">`System.String`类型</a>
* <a href="#14_3">高效率构造字符串</a>
* <a href="#14_4">获取对象的字符串表示：`ToString`</a>
* <a href="#14_5">解析字符串来获取对象：`Parse`</a>
* <a href="#14_6">编码：字符和字节的相互转换</a>
* <a href="#14_7">安全字符串</a>

本章将解释在 Microsoft .NET Framework 中处理字符和字符串的机制。首先讨论 `System.Char` 结构以及处理字符的多种方式。然后讨论更有用的`System.String`类，它允许处理不可变(immutable)字符串(一经创建，字符串便不能以任何方式修改)。探讨了字符串之后，将介绍如何使用`System.Text.StringBuilder`类高效地动态构造字符串。掌握了字符串的基础知识之后，将讨论如何将对象格式化成字符串，以及如何使用各种编码方法高效率地持久化或传输字符串。最后讨论`System.Security.SecureString`类，它保护密码和信用卡资料等敏感字符串。

## <a name="14_1">14.1 字符</a>

在.NET Framework 中，字符总是表示成 16 位 Unicode 代码值，这简化了国际化应用程序的开发。每个字符都是`System.Char`结构(一个值类型)的实例。`System.Char` 类型很简单，提供了两个公共只读常量字段：`MinValue`(定义成 `‘\0’`)和`MaxValue`(定义成`'\uffff'`)。

为`Char`的实例调用静态`GetUnicodeCategory`方法返回`System.Globalization.UnicodeCategory`枚举类型的一个值，表明该字符是由 Unicode标准定义的控制字符、货币符号、小写字母、大写字母、标点符号、数学符号还是其他字符。

为了简化开发，`Char`类型还提供了几个静态方法，包括`IsDigit`，`IsLetter`，`IsUpper`，`IsLower`，`IsPunctuation`，`IsLetterOrDigit`，`IsControl`，`IsNumber`，`IsSeparator`，`IsSurrogate`，`IsLowSurrogate`，`IsHighSurrogate`和`IsSymbol`等。大多数都在内部调用了`GetUnicodeCategory`，并简单地返回`true`或`false`。注意，所有这些方法要么获取单个字符作为参数，要么获取一个`String`以及目标字符在这个`String`中的索引作为参数。

另外，可调用静态方法 `ToLowerInvariant`或者`ToUpperInvariant`，以忽略语言文化(culture)的方式将字符转换为小写或大写形式。另一个方案是调用`ToLower`和`ToUpper`方法来转换大小写，但转换时会使用与调用线程关联的语言文化信息(方法在内部查询`System.Threading.Thread`类的静态 `CurrentCulture` 属性来获得)。也可向这些方法传递`CultureInfo`类的实例来指定一种语言文化。`ToLower`和`ToUpper`之所以需要语言文化信息，是因为字母的大小写转换时一种依赖于语言文化的操作。比如在土耳其语中，字母U+0069(小写拉丁字母i)转换成大写是 U+0130(大写拉丁字母I，上面加一点)，而其他语言文化的转换结果是 U+0049(大写拉丁字母 I)。

除了这些静态方法，`Char`类型还有自己的实例方法。其中，`Equals`方法在两个`Char`实例代表同一个 16 位 Unicode 码位<sup>①</sup>的前提下返回`true`。`CompareTo`方法(由 `IComparable`和 `IComparable<Char>`接口定义)返回两个`Char`实例的忽略语言文化的比较结果。`ConvertFromUtf32`方法从一个 UTF-32 字符生成包含一个或两个 UTF-16 字符的字符串。`ConvertToUtf32`方法从一对低/高低理项或者字符串生成一个 UTF-32 字符。`ToString`方法返回包含单个字符的一个`String`。与`ToString`相反的是`Parse/TryParse`，它们获取单字符的`String`，`返回该字符的 UTF-16 码位。

> ① 在字符编码术语中，码位或称编码位置，即英文的 code point 或 code position，是组成码空间(或代码页)的数值。例如，ASCII 码包含 128 个码位。——维基百科

最后一个方法是 `GetNumericValue`，它返回字符的数值形式。以下代码演示了这个方法。

```C#
using System;

public static class Program {
    public static void Main() {
        Double d;                           // '\u0033'是”数字3“
        d = Char.GetNumericValue('\u0033'); // 也可直接使用'3'
        Console.WriteLine(d.ToString());    // 显示”3“

        // '\u00bc' 是 ”普通分数四分之一('1/4')“
        d = Char.GetNumericValue('\u00bc');
        Console.WriteLine(d.ToString());    // 显示”0.25“

        // 'A' 是 ”大写拉丁字母 A“
        d = Char.GetNumericValue('A');
        Console.WriteLine(d.ToString());    // 显示”-1“
    }
}
```

最后，可以使用三种技术实现各种数值类型与 `Char` 实例的相互转换。下面按照优先顺序列出这些技术。

* **转型(强制类型转换)**  
  将`Char`转换成数值(比如`Int32`)最简单的办法就是转型。这是三种技术中效率最高的，因为编译器会生成中间语言(IL)指令来执行转换，而且不必调用方法。此外，有的语言(比如 C#)允许指定转换时是使用 checked 还是 unchecked 代码(参见 5.1 节”编程语言的基元类型“)。

* **使用 Convert 类型**  
  `System.Convert` 类型提供了几个静态方法来实现 `Char` 和数值类型的相互转型。所有这些转换都以 checked 方式执行，发现转换将造成数据丢失就抛出 `OverflowException`异常。  

* **使用 IConvertible 接口**  
  `Char` 类型和 FCL 中的所有数值类型都实现了 `IConvertible` 接口。该接口定义了像`ToUInt16` 和 `ToChar` 这样的方法。这种技术效率最差，因为在值类型上调用接口方法`ToUInt16`和`ToChar`这样的方法。这种技术效率最差，因为在值类型上调用接口方法要求对实例进行装箱——`Char`和所有数值类型都是值类型。如果某个类型不能转换(比如 `Char` 转换成 `Boolean`)，或者转换将造成数据丢失，`IConvertible`的方法会抛出`System.InvalidCastException` 异常。注意，许多类型(包括 FCL 的`Char`和数值类型)都将`IConvertible`的方法实现为显式接口成员<sup>①</sup>。这意味着为了调用接口的任何方法，都必须先将实例显式转型为一个`IConvertible`。`IConvertible`的所有方法(`GetTypeCode`除外)都接受对实现了`IFormatProvider`接口的一个对象的引用。如果转换时需要考虑语言文化信息，该参数就很有用。但大多数时候都可以忽略语言文化，为这个参数传递`null`值。

> ① 参见 13.9 节”用显式接口方法实现来增强编译时类型安全性“。

以下代码演示了如何使用这三种技术。

```C#
using System;

public static class Program {
    public static void Main() {
        Char c;
        Int32 n;

        // 通过 C# 转型(强制类型转换)实现数字与字符的相互转换
        c = (Char)65;
        Console.WriteLine(c);               // 显示”A“

        n = (Int32)c;
        Console.WriteLine(n);               // 显示”65“

        c = unchecked((Char)(65536 + 65));
        Console.WriteLine(c);               // 显示"A"

        // 使用 Convert 实现数字与字符的相互转换
        c = Convert.ToChar(65);
        Console.WriteLine(c);               // 显示”A“

        n = Convert.ToInt32(c);
        Console.WriteLine(n);               // 显示”65“

        // 演示 Convert 的范围检查
        try {
            c = Convert.ToChar(70000);      // 对 16 位来说过大
            Console.WriteLine(c);           // 不执行
        }
        catch (OverflowException) {
            Console.WriteLine("Can't convert 70000 to a Char.");
        }

        // 使用 IConvertible 实现数字与字符的相互转换
        c = ((IConvertible)65).ToChar(null);
        Console.WriteLine(c);               // 显示”A“

        n = ((IConvertible)c).ToInt32(null);
        Console.WriteLine(n);               // 显示”65“
    }
}
```

## <a name="14_2">14.2 `System.String` 类型</a>

在任何应用程序中，`System.String`都是用得最多的类型之一。一个 `String` 代表一个不可变(immutable)的顺序字符集。`String`类型直接派生自`Object`，所以是引用类型。因此，`String`对象(它的字符数组)总是存在于堆上，永远不会跑到线程栈<sup>①</sup>。`String`类型还实现了几个接口(`IComparable/IComparable<String>`，`ICloneable`，`IConvertible`，`IEnumerable/IEnumerable<Char>`和`IEquatable<String>`)。

> ① 堆和线程栈的详情请参见 4.4 节 ”运行时的相互关系“

### 14.2.1 构造字符串  

许多编程语言(包括 C#)都将 `String` 视为基元类型——也就是说，编译器允许在源代码中直接使用字面值(literal)字符串。编译器将这些字符串放到模块的元数据中，并在运行时加载和引用它们。

C# 不允许使用 `new` 操作符从字面值字符串构造`String`对象：

```C#
using System;

public static class Program {
    public static void Main() {
        String s = new String("Hi there.");     // 错误
        Console.WriteLine(s);
    }
}
```

相反，必须使用以下简化语法：

```C#
using System;

public static class Program {
    public static void Main() {
        String s = "Hi there.";     // 错误
        Console.WriteLine(s);
    }
}
```

编译代码并检查 IL(使用 ILDasm.exe)，会看到以下内容：

```cmd
.method public hidebysig static void Main() cil managed
{
    .entrypoint
    // Code size 13 (0xd)
    .maxstack 1
    .locals init ([0] string s)
    IL_0000: ldstr "Hi there."
    IL_0005: stloc.0
    IL_0006: ldloc.0
    IL_0007: call void [mscorlib]System.Console::WriteLine(string)
    IL_000c: ret
} // end of method Program::Main
```

用于构造对象新实例的 IL 指令是 `newobj`。但上述 IL 代码中并没有出现`newobj`指令，只有一个特殊`ldstr`(即 load string)指令，它使用从元数据获得的字面值(literal)字符串构造`String`对象。这证明 CLR 实际是用一种特殊方式构造字面值`String`对象。

如果使用不安全的(unsafe)代码，可以从一个`Char*`或`Sbyte*`构造一个`String`。这时要使用C#的`new`操作符，并调用由`String`类型提供的、能接受`Char*`和`Sbyte*`参数的某个构造器。这些构造器将创建`String`对象那个，根据由`Char`实例或有符号(signed)字节构成的一个数组来初始化字符串。其他构造器则不允许接受任何指针参数，用任何托管编程语言写的安全(可验证)代码都能调用它们。<sup>①</sup>

C#提供了一些特殊语法来帮助开发人员在源代码中输入字面值(literal)字符串。对于换行符、回车符和退格符这样的特殊字符，C#采用的是 C/C++ 开发人员熟悉的转义机制：

```C#
// 包含回车符和换行符的字符串
String s = "Hi\r\nthere.";
```

> 重要提示 上例虽然在字符串中硬编码了回车符和换行符，但一般不建议这样做。相反，````````