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

> ① 记住，除非指定了 `/unsafe` 编译器开关，否则 C# 代码必须是安全的或者说具有可验证性，确保代码不会引起安全风险和稳定性风险。详情参见 1.4.2 节”不安全的代码“。 —— 译注

C#提供了一些特殊语法来帮助开发人员在源代码中输入字面值(literal)字符串。对于换行符、回车符和退格符这样的特殊字符，C#采用的是 C/C++ 开发人员熟悉的转义机制：

```C#
// 包含回车符和换行符的字符串
String s = "Hi\r\nthere.";
```

> 重要提示 上例虽然在字符串中硬编码了回车符和换行符，但一般不建议这样做。相反，`System.Environment`类型定义了只读`NewLine`属性。应用程序在 Microsoft Windows 上运行时，该属性返回由回车符和换换行符构成的字符串。例如，如果将公共语言基础结构(CLI)移植到 UNIX 系统，`NewLine`属性将返回由单字符`\n‘构成的字符串。以下才是定义上述字符串的正确方式，它在任何平台上都能正确工作：    

>`String s = "Hi" + Environment.NewLine + "there."`；

可以使用C#的`+`操作符将几个字符串连接成一个。如下所示：

```C#
// 三个字面值(literal)字符串连接成一个字面值字符串
String s = "Hi" + " " + "there.";
```

在上述代码中，由于所有字符串都是字面值，所以 C# 编译器能在编译时连接它们，最终只将一个字符串(即`"Hi there."`)放到模块的元数据中。对非字面值字符串使用`+`操作符，连接则在运行时进行。运行时连接不要使用`+`操作符，因为这样会在堆上创建多个字符串对象，而堆是需要垃圾回收的，对性能有影响。相反，应该使用`System.Text.StringBuilder` 类型(本章稍后详细解释)。

最后，C# 提供了一种特殊的字符串声明方式。采用这种方式，引号之间的所有字符会都被视为字符串的一部分。这种特殊声明称为”逐字字符串“(verbatim string)，通常用于指定文件或目录的路径，或者与正则表达式配合使用。以下代码展示了如何使用和不使用逐字字符串字符(@)来声明同一个字符串：

```C#
// 指定应用程序路径
String file = "C:\\Windows\\System32\\Notepad.exe";

// 使用逐字字符串指定应用程序路径
String file = @"C:\Windows\System32\Notepad.exe";
```

两种写法在程序集的元数据中生成完全一样的字符串，但后者可读性更好。在字符串之前添加@符号使编译器知道这是逐字字符串。编译器会将反斜杠字符视为字面值(literal)而非转义符，使文件路径在源代码中更易读。

了解了如何构造字符串之后，接着探讨可以在 `String` 对象上执行的操作。

### 14.2.2 字符串是不可变的

`String` 对象最重要的一点就是不可变(immutable)。也就是说，字符串一经创建便不能更改，不能变长、变短或修改其中的任何字符。使字符串不可变有几方面的好处。首先，它允许在一个字符串上执行各种操作，而不实际地更改字符串：  

```C#
if (s.ToUpperInvariant().Substring(10, 21).EndsWith("EXE")){
    ...
}
```

`ToUpperInvariant`返回一个新字符串；它没有修改字符串`s`的字符。在`ToUpperInvariant`返回的字符串上执行的`SubString`操作也返回新字符串。然后，`EndsWith`对这个字符串进行检查。代码不会长时间引用由`ToUpperInvariant`和`Substring`创建的两个临时字符串，垃圾回收器会在下次回收它们的内存。如果执行大量字符串操作，会在堆上创建大量`String`对象，造成更频繁的垃圾回收，从而影响应用程序性能。要高效执行大量字符串操作，建议使用`StringBuilder`类。

字符串不可变还意味着在操纵或访问字符串时不会发生线程同步问题。此外，CLR 可通过一个`String`对象共享多个完全一致的`String`内容。这样能减少系统中的字符串数量——从而节省内存——这就是所谓的”字符串留用“(string interning)<sup>①</sup>。

> ① MSDN 文档将 interning 翻译成”拘留“，专门供字符串留用的表称为”拘留池“。本书采用”留用“这一译法。这个技术的详情将在本章后面详细解释。——译注

出于对性能的考虑，`String`类型与 CLR 紧密集成。具体地说，CLR 知道 `String` 类型中定义的字段如何布局，会直接访问这些字段。但为了获得这种性能和直接访问的好处，`String`只能是密封类。换言之，不能把它作为自己类型的基类。如果允许`String`作为基类来定义自己的类型，就能添加自己的字段，而这会破坏 CLR 对于 `String` 类型的各种预设。此外，还可能破坏 CLR 团队因为 `String` 对象”不可变“而做出的各种预设。

### 14.2.3 比较字符串 

”比较“或许是最常见的字符串操作。一般因为两个原因要比较字符串：判断相等性或者排序(通常是为了显示给用户看)。

判断字符串相等性或排序时，强烈建议调用`String`类定义的以下方法之一：

```C#
Boolean Equals(String value, StringComparison comparisonType)
static Boolean Equals(String a, String b, StringComparison comparisonType)

static Int32 Compare(String strA, String strB, StringComparison comparisonType)
static Int32 Compare(string strA, string strB, Boolean ignoreCase, CultureInfo culture)
static Int32 Compare(String strA, String strB, CultureInfo culture, CompareOptions options)
static Int32 Compare(String strA, String strB, CultureInfo culture, CompareOptions options)
static Int32 Compare(String strA, Int32 indexA, String strB, Int32 indexB, Int32 length, StringComparison comparisonType)
static Int32 Compare(String strA, Int32 indexA, String strB, Int32 indexB, Int32 length, CultureInfo culture, CompareOptions options)
static Int32 Compare(String strA, Int32 indexA, String strB, Int32 indexB, Int32 length, Boolean ignoreCase, CultureInfo culture)

Boolean StartsWith(String value, StringComparison comparisonType)
Boolean StartsWith(String value, Boolean ignoreCase, CultureInfo culture)

Boolean EndsWith(String value, StringComparison comparisonType)
Boolean EndsWith(String value, Boolean ignoreCase, CultureInfo culture)
```

排序时应该总是执行区分大小写的比较。原因是假如只是大小写不同的两个字符串被视为相等，那么每次排序都可能按不同顺序排列，用户会感到困惑。

`comparisonType` 参数(上述大多数方法都有)要求获取由 `StringComparison` 枚举类型定义的某个值。该枚举类型的定义如下所示：

```C#
public enum StringComparison {
	CurrentCulture = 0,
	CurrentCultureIgnoreCase = 1,
	InvariantCulture = 2,
	InvariantCultureIgnoreCase = 3,
	Ordinal = 4,
	OrdinalIgnoreCase = 5
}
```

另外，前面有两个方法要求传递一个 `options` 参数，它是 `CompareOptions` 枚举类型定义的值之一：

```C#
[Flags]
public enum CompareOptions {
	None = 0,
	IgnoreCase = 1,
	IgnoreNonSpace = 2,
	IgnoreSymbols = 4,
	IgnoreKanaType = 8,
	IgnoreWidth = 0x00000010,
	Ordinal = 0x40000000,
    OrdinalIgnoreCase = 0x10000000,
	StringSort = 0x20000000
}
```

接受`CompareOptions` 实参的方法要求显式传递语言文化。传递`Ordinal`或`OrdinalIgnoreCase`标志，这些`Compare`方法会忽略指定的语言文化。

许多程序都将字符串用于内部编程目的，比如路径名、文件名、URL、注册表项/值、环境变量、反射、XML标记、XML特性等。这些字符串通常只在程序内部使用，不向用户显示。出于编程目的而比较字符串时，应该总是使用`StringComparison.Ordinal`或者`StringComparison.OrdinalIgnoreCase`。忽略语言文化是字符串比较最快的方式。

另一方面，要以语言文化正确的方式来比较字符串(通常为了向用户显示)，就应该使用`StringComparison.CurrentCulture`或者`StringComparisonCurrentCultureIgnoreCase`。

> 重要提示 `StringComparison.InvariantCulture` 和 `StringComparison.InvariantCultureIgnoreCase`平时最好不要用。虽然这两个值保证比较时的语言文化正确性，但用来比较内部编程所需的字符串，所花的时间远超出序号比较<sup>①</sup>。此外，如果传递`StringComparison.InvariantCulture`(固定语言文化)，其实就是不使用任何具体的语言文化。所以在处理要向用户显示的字符串时，选择它并不恰当。

>>① 传递 `StringComparison.Ordinal` 执行的就是序号比较，也就是不考虑语言文化信息，只比较字符串中的每个`Char`的Unicode码位。——译注

> 重要提示 要在序号比较前更改字符串中的字符的大小写，应该使用`String`的`ToUpperInvariant`或`ToLowerInvariant`方法。强烈建议用`ToUpperInvariant`方法对字符串进行正规化(normalizing)，而不要用`ToLowerInvariant`，因为 Microsoft 对执行大写比较的代码进行了优化。事实上，执行不区分大小写的比较之前，FCL 会自动将字符串正规化为大写形式。之所以要用`ToUpperInvariant`和`ToLowerInvariant`方法，是因为`String`类没有提供`ToUpperOrdinal`和`ToLowerOrdinal`方法。之所以不用`ToUpper`和`ToLower`方法，是因为它们对语言文化敏感。

以语言文化正确的方式比较字符串时，有时需要指定另一种语言文化，而不是使用与调用线程关联的哪一种。这时可用前面列出的`StartsWith`，`EndsWith`和`Compare`方法的重载版本，它们都接受`Boolean`和`CultureInfo`参数。

> 重要提示 除了前面列出之外，`String`类型还为`Equals`，`StartsWith`,`EndsWith`和`Compare`方法定义了其他几个重载版本。但是，Microsoft 建议避免使用这些额外的版本(也就是本书没有列出的版本)。除此之外，`String` 的其他比较方法——`CompareTo`(`IComparable`接口所要求的)、用这些方法和操作符，是因为调用者不显式指出以什么方式执行字符串比较，而你无法从方法名看出默认比较方式。例如，`CompareTo` 默认执行对语言文化敏感的比较，而 `Equals` 执行普通的序号(ordinal)比较。如果总是显式地指出以什么方式执行字符串比较，代码将更容易阅读和维护。

现在重点讲一下如何执行语言文化正确的比较。.NET Framework 使用 `System.Globalization.CultureInfo` 类型表示一个“语言/国家”对(根据RFC 1766标准)。例如，“en-US”代表美国英语，“en-AU”代表澳大利亚英语，而“de-DE”代表德国德语。在CLR中，每个线程都关联了两个特殊属性，每个属性都引用一个`CultureInfo`对象。两个属性的具体描述如下。

* **CurrentUICulture 属性** 该属性获取要向用户显示的资源。它在 GUI 或 Web 窗体应用程序中特别有用，因为它标识了在显示 UI 元素(比如标签和和按钮)时应使用的语言。创建线程时，这个线程属性会被设置成一个默认的 `CultureInfo` 对象，该对象标识了正在运行应用程序的 Windows 版本所用的语言。而这个语言是用 Win32 函数 `GetUserDefaultUILanguage` 来获取的。如果应用程序在 Windows 的 MUI (多语言用户界面，Multilingual User Interface)版本上运行，可通过控制面板的“区域和语言”对话框来修改语言。在非 MUI 版本的 Windows 上，语言由安装的操作系统的本地化版本(或者安装的语言包)决定，而且这个语言不可更改。

* **CurrentCulture 属性** 不合适使用`CurrentUICulture`属性的场合就使用该属性，例如数字和日期格式化、字符串大小写转换以及字符串比较。格式化要同时用到`CultureInfo`对象的“语言”和“国家”部分。创建线程时，这个线程属性被设为一个默认的`CultureInfo`对象，其值通过调用 Win32 函数`GetUserDefaultLCID`来获取。可通过 Windows 控制面板的“区域和语言”对话框来修改这个值。

在许多计算机上，线程的 `CultureUIInfo` 和 `CurrentCulture` 属性都被设为同一个`CultureInfo` 对象。也就是说，它们使用相同的“语言/国家信息”。但也可把它们设为不同对象。例如，在美国运行的一个应用程序可能要用西班牙语来显示它的所有菜单项以及其他 GUI 元素，同时仍然要正确显示美国的货币和日期格式。为此，线程的`CurrentUICulture`属性要引用另一个`CultureInfo`对象，用“en-US”初始化。

`CultureInfo`对象内部的一个字段引用了一个`System.Globalization.CompareInfo`对象，该对象封装了语言文化的字符排序表信息(根据 Unicode 标准的定义)。以下代码演示了序号比较和对语言文化敏感的比较的区别：

```C#
using System;
using System.Globalization;

public static class Program {
    public static void Main() {
        String s1 = "Strasse";
        String s2 = "Straße";
        Boolean eq;

        // Compare 返回非零值 ①
        eq = String.Compare(s1, s2, StringComparison.Ordinal) == 0;
        Console.WriteLine("Ordinal comparison:'{0}' {2} '{1}'", s1, s2, eq ? "==" : "!=");

        // 面向在德国(DE)说德语(de)的人群，
        // 正确地比较字符串
        CultureInfo ci = new CultureInfo("de-DE");

        // Compare 返回零值
        eq = String.Compare(s1, s2, true, ci) == 0;
        Console.WriteLine("Cultural comparison:'{0}' {2} '{1}'", s1, s2, eq ? "==" : "!=");
    }
}
```

> ① `Compare`返回`Int32`值；非零表示不相等，零表示相等。非零和零分别对应`true`和`false`，所以后面的代码将比较结果与`0(false)`进行比较，将`true`或`false`结果赋给`eq`变量。 ——译注

生成并运行上述代码得到以下输出：

```cmd
Ordinal comparison: 'Strasse' != 'Straße'
Cultural comparison: 'Strasse' == 'Straße' 
```

> 注意 `Compare`方法如果执行的不是序号比较就会进行“字符展开”(character expansion)，也就是将一个字符展开成忽视语言文化的多个字符。在前例中，德语 Eszet 字符 “ß” 总是展开成“ss”。类似地，“Æ” 连字总是展开成“AE”。所以在上述代码中，无论传递什么语言文化，对`Compare`的第二个调用始终返回0.

比较字符串以判断相等性或执行排序时，偶尔需要更多的控制。例如，比较日语字符串时就可能有这个需要。额外的控制通过`CultureInfo`对象的`CompareInfo`属性获得。前面说过，`CompareInfo` 对象封装了一种语言文化的字符比较表，每种语言文化只有一个`CompareInfo`对象。

调用`String`的`Compare`方法时，如果调用者指定了语言文化，就使用指定的语言文化，就使用指定的语言文化；如果没有指定，就使用调用线程的`CurrentCulture`属性值。`Compare`方法内部会获取与特定语言文化匹配的`CompareInfo`对象引用，并调用`CompareInfo`对象的`Compare`方法，传递恰当的选项(比如不区分大小写)。自然，如果需要额外的控制，可以自己调用一个特定`CompareInfo`对象的`Compare`方法。

`CompareInfo`类型的`Compare`方法获取来自 `CompareOptions` 枚举类型<sup>①</sup>的一个值作为参数。该枚举类型定义的符号代表一组位标志(bit flag)，对这些位标志执行 OR 运算就能更全面地控制字符串比较。请参考文档获取这些符号的完整描述。

> ① 前面已经展示过 `CompareOptions` 枚举类型。

以下代码演示了语言文化对于字符串排序的重要性，并展示了执行字符串比较的各种方式：

```C#
using System;
using System.Text;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;

public sealed class Program {
    public static void Main() {
        String output = String.Empty;
        String[] symbol = new String[] { "<", "=", ">" };
        Int32 x;
        CultureInfo ci;

        // 以下代码演示了在不同语言文化中，
        // 字符串的比较方式也有所不同
        String s1 = "coté";
        String s2 = "côte";

        // 为法国法语排序字符串
        ci = new CultureInfo("fr-FR");
        x = Math.Sign(ci.CompareInfo.Compare(s1, s2));
        output += String.Format("{0} Compare: {1} {3} {2}",
        ci.Name, s1, s2, symbol[x + 1]);
        output += Environment.NewLine;

        // 为日本日语排序字符串
        ci = new CultureInfo("ja-JP");
        x = Math.Sign(ci.CompareInfo.Compare(s1, s2));
        output += String.Format("{0} Compare: {1} {3} {2}",
        ci.Name, s1, s2, symbol[x + 1]);
        output += Environment.NewLine;

        // 为当前线程的当前语言文化排序字符串
        ci = Thread.CurrentThread.CurrentCulture;
        x = Math.Sign(ci.CompareInfo.Compare(s1, s2));
        output += String.Format("{0} Compare: {1} {3} {2}",
        ci.Name, s1, s2, symbol[x + 1]);
        output += Environment.NewLine + Environment.NewLine;
        
        // 以下代码演示了如何将 CompareInfo.Compare 的
        // 高级选项应用于两个日语字符串。
        // 一个字符串代表用平假名写成的单词“shinkansen”(新干线)；
        // 另一个字符串代表用片假名写成的同一个单词
        s1 = "しんかんせん"; // ("\u3057\u3093\u304B\u3093\u305b\u3093")
        s2 = "シンカンセン"; // ("\u30b7\u30f3\u30ab\u30f3\u30bb\u30f3")
                 
        // 以下是默认比较结果
        ci = new CultureInfo("ja-JP");
        x = Math.Sign(String.Compare(s1, s2, true, ci));
        output += String.Format("Simple {0} Compare: {1} {3} {2}",
        ci.Name, s1, s2, symbol[x + 1]);
        output += Environment.NewLine;
        
        // 以下是忽略日语假名的比较结果
        CompareInfo compareInfo = CompareInfo.GetCompareInfo("ja-JP");
        x = Math.Sign(compareInfo.Compare(s1, s2, CompareOptions.IgnoreKanaType));
        output += String.Format("Advanced {0} Compare: {1} {3} {2}",
        ci.Name, s1, s2, symbol[x + 1]);
        MessageBox.Show(output, "Comparing Strings For Sorting");
    }
}
```

生成并运行以上代码得到如图 14-1 所示的结果。

![14_1](../resources/images/14_1.png)  
图 14-1 字符串排序结果

> 注意<sup>①</sup> 源代码不要用 ANSI 格式保存，否则日语字符会丢失。要在 Microsoft Visual Studio中保存这个文件，请打开“另存文件为”对话框，单击“保存”按钮右侧的下箭头，选择“编码保存”，并选择“Unicode(UTF-8带签名)-代码页 65001”。Microsoft C# 编译器用这个代码也就能成功解析源代码文件了。

> ① 中文版 Visual Studio 可忽略这个“注意”。——译注

除了`Compare`，`CompareInfo` 类还提供了`IndexOf`，`LastIndexOf`，`IsPrefix`和`IsSuffix`方法。由于所有这些方法都提供了接受`CompareOptions`枚举值的重载版本，所以能提供比`String`类定义的`Compare`，`IndexOf`，`LastIndexOf`，`StartsWith` 和 `EndsWith` 方法更全面的控制。另外，FCL 的`System.StringComparer`类也能执行字符串比较，它适合对大量不同的字符串比较，它适合对大量不同的字符串反复执行同一种比较。

### 14.2.4 字符串留用

如上一节所述，检查字符串相等性是应用程序的常见操作，也是一种可能严重损害性能的操作。执行序号(ordinal)相等性检查时，CLR 快速测试两个字符串是否包含相同数量的字符。答案否定，字符串肯定不相等；答案肯定，字符串则可能相等。然后，CLR 必须比较每个单独的字符才能最终确认。而执行对语言文化敏感的比较时，CLR 必须比较所有单独的字符，因为两个字符串即使长度不同也可能相等。

此外，在内存中复制同一个字符串的多个实例纯属浪费，因为字符串是“不可变”(immutable)的。在内存中只保留字符串的一个实例将显著提升内存的利用率。需要引用字符串的所有变量只需指向单独一个字符串的所有变量只需指向单独一个字符串对象。

如果应用程序经常对字符串进行区分大小写的序号比较，或者事先知道许多字符串对象都有相同的值，就可利用 CLR 的**字符串留用**(string interning)机制来显著提升性能。CLR 初始化时会创建一个内部哈希表。在这个表中，键(key)是字符串，而值(value)是对托管堆中的`String`对象的引用。哈希表最开始是空的(理应如此)。`String`类提供了两个方法，便于你访问这个内部哈希表：

```C#
public static String Intern(String str);
public static String IsInterned(String str);
```

第一个方法 `Intern` 获取一个 `String`， 获得它的哈希码，并在内部哈希表中检查是否有相匹配的。如果存在完全相同的字符串，就返回对现有 `String` 对象的引用。如果不存在完全相同的字符串，就创建字符串的副本，将副本添加到内部哈希表中，返回对该副本的引用。如果应用程序不再保持对原始`String`对象的引用，垃圾回收器就可释放那个字符串的内存。注意垃圾回收器不能释放内部哈希表引用的字符串，因为哈希表正在容纳对它们的引用。
除非卸载 AppDomain 或进程终止，否则内部哈希表引用的 `String` 对象不能被释放。

和 `Intern` 方法一样，`IsInterned` 方法也获取一个 `String`，并在内部哈希表中查找它。如果哈希表中有匹配的字符串，`IsInterned`就返回对这个留用(interned)字符串对象的引用。但如果没有，`IsInterned`会返回`null`，不会将字符串添加到哈希表中。

程序集加载时，CLR 默认留用程序集的元数据中描述的所有字面值(literal)字符串对象的引用。但如果没有，`IsInterned`就返回对这个留用(interned)字符串对象的引用。但如果没有，`IsInterned`会返回`null`，不会将字符串添加到哈希表中。

程序集加载时，CLR 默认留用程序集的元数据中描述的所有字面值(literal)字符串。Microsoft 知道可能因为额外的哈希表查找而显著影响性能，所以现在能禁用此功能。如果程序集用`System.Runtime.CompilerServices.CompilationRelaxationsAttribute` 进行了标记，并指定了 `System.Runtime.CompilerServices.CompilationRelaxations.NoStringInterning`标志值，那么根据 ECMA 规范，CLR 可能选择不留用那个程序集的元数据中定义的所有字符串。注意，为了提升应用程序性能，C#编译器在编译程序时总是指定上述两个特性和标志。

即使程序集指定了这些特性和标志，CLR 也可能选择对字符串进行留用，但不要依赖 CLR 的这个行为。事实上，除非显式调用`String`的`Intern`方法，否则永远都不要以“字符串已留用”为前提来写代码。以下代码演示了字符串留用：

```C#
String s1 = "Hello";
String s2 = "Hello";
Console.WriteLine(Object.ReferenceEquals(s1, s2));  // 显示 'False'

s1 = String.Intern(s1);
s2 = String.Intern(s2);
Console.Writeline(Object.ReferenceEquals(s1, s2)); // 显示 'True'
```

在第一个`ReferenceEquals`方法调用中，`s1`引用堆中的`"Hello"`字符串对象，而`s2`引用堆中的另一个"`Hello`"对象。由于引用不同，所以应该显示`False`。但在 CLR 的 4.5 版本上运行，实际显示的是 `True`。这是由于这个版本的 CLR 选择忽视 C# 编译器插入的特性和标志。程序集加载到 AppDomain 中时，CLR 对字面值(literal)字符串`"Hello"`进行留用，结果是 `s1`和`s2`引用堆中的同一个`“Hello”`字符串。但如前所述，你的代码永远不要依赖这个行为，因为未来版本的 CLR 有可能会重视这些特性和标志，从而不对`“Hello”`字符串进行留用。事实上，使用 NGen.exe 实用程序编译这个程序集的代码，CLR 的 4.5 版本确实会使用这些特性和标志。

在第二个`ReferenceEquals`方法调用之前，`“Hello”`字符串被显式留用，`s1`现在引用已留用的`“Hello”`。然后，通过再次调用`Intern`，`s2`引用和`s1`一样的`“Hello”`字符串。所以第二个`ReferenceEquals`调用保证结果是`True`，无论程序集在编译时是否设置了特性和标志。

现在通过一个例子理解如何利用字符串留用来提升性能并减少内存耗用。以下`NumTimesWordAppearsEquals`方法获取两个参数：一个单词和一个字符串数组。每个数组元素都引用了一个单词。方法统计指定单词在单词列表中出现了多少次并返回计数：

```C#
private static Int32 NumTimesWordAppearsEquals(String word, String[] wordlist) {
    Int32 count = 0;
    for (Int32 wordnum = 0; wordnum < wordlist.Length; wordnum++) {
        if (word.Equals(wordlist[wordnum], StringComparison.Ordinal))
            count++;
    }
    return count;
}
```

这个方法调用了`String`的`Equals`方法，后者在内部比较字符串的各个单独字符，并核实所有字符都匹配。这个比较可能很慢。此外，`wordlist`数组可能有多个元素引用了含有相同字符内容的不同`String`对象。这意味着堆中可能存在多个内容完全相同的字符串，并会在将来进行垃圾回收时幸存下来。

现在看一下这个方法的另一个版本。新版本利用了字符串留用：

```C#
private static Int32 NumTimesWordAppearsIntern(String word, String[] wordlist) {
    // 这个方法假定 wordlist 中的所有数组元素都引用已留用的字符串
    word = String.Intern(word);
    Int32 count = 0;
    for (Int32 wordnum = 0; wordnum < wordlist.Length; wordnum++) {
        if (Object.ReferenceEquals(word, wordlist[wordnum]))
            count++;
    }
    return count;
}
```

这个方法留用了单词，并假定`wordlist` 包含对已留用字符串的引用。首先，假如一个单词在单词列表中多次出现，这个版本有助于节省内存。因为在这个版本中，`worldlist`会包含对堆中同一个`String`对象的多个引用。其次，这个版本更快。因为比较指针就知道指定单词是否在数组中。

虽然`NumTimesWordAppearsIntern`方法本身比`NumTimesWordAppearsEquals`方法快，但在使用`NumTimesWordAppearsIntern`方法时，应用程序的总体性能是可能变慢的。这是因为所有字符串在添加到`wordlist`数组时(这里没有列出具体的代码)都要花时间进行留用。但如果应用程序要为同一个`wordlist`多次调用`NumTimesWordAppearsIntern`，这个方法真的能提升应用程序性能和内存利用率。总之，字符串留用虽然有用，但使用须谨慎。事实上，这正式 C# 编译器默认不想启用字符串留用的原因。<sup>①</sup>

> ① 虽然编译器应用了特性并设置了不进行字符串留用的标志，但 CLR 选择忽视这些设置你也没有办法。 ——译注

### 14.2.5 字符串池

编译源代码时，编译器必须处理每个字面值(literal)字符串，并在托管模块的元数据中嵌入。同一个字符串在源代码中多次出现，把它们都嵌入元数据会使生成的文件无谓地增大。

为了解决这个问题，许多编译器(包括 C#编译器)只在模块的元数据中只将字面值字符串写入一次。引用该字符串的所有代码都被改成引用元数据中的同一个字符串。编译器将单个字符串的多个实例合并成一个实例，能显著减少模块的大小。但这并不是新技术，C/C++ 编译器多年来一直在采用这个技术(Micrisoft 的 C/C++ 编译器称之为“字符串池”)。尽管如此，字符串池仍是提升字符串性能的另一种行之有效的方式，而你应注意到它的存在。

### 14.2.6 检查字符串中的字符和文本元素

虽然字符串比较于排序或测试相等性很有用，但有时只是想检查一下字符串中的字符。`String`类型为此提供了几个属性和方法，包括 `Length`，`Chars`(一个 C# 索引器<sup>②</sup>)，`GetEnumerator`，`ToCharArray`，`Contains`，`IndexOf`，`IndexOfAny`和`LastIndexOfAny`。

> ② 或者说有参属性。 ——译注

`String.Char`实际代表一个 16 位 Unicode 码值，而且该值不一定就等于一个抽象 Unicode 字符。例如，有的抽象 Unicode 字符是两个码值的组合。U+0625(阿拉伯字母 Alef with Hamza below)和 U+0650(Arabic Kasra)字符组合起来就构成了一个抽象字符或者**文本元素**(text element)。

除此之外，有的 Unicode 文本元素要求用两个16 位值表示。第一个称为“高位代理项”(high surrogate)。其中，高位代理项范围在 U+D800 到 U+ DBFF 之间，低位代理项范围在 U+DC00 到 U+DFFF 之间。有了代理项，Unicode 就能表示 100 万个以上不同的字符。

美国和欧洲很少使用代理项，东亚各国则很常用。为了正确处理文本元素，应当使用`System.Globalization.StringInfo`类型。使用这个类型最简单的方式就是构造它的实例，向构造器传递一个字符串。然后可以查询`StringInfo`的`LengthInTextElements`属性来了解字符串中有多少个文本元素。接着就可以调用`StringInfo`的`SubstringByTextElements`方法来提取所有文本元素，或者提取指定数量的连续文本元素。

`StringInfo`类还提供了静态方法`GetTextElementEnumerator`，它返回一个`System.Globalization.TextElementEnumerator`对象，允许枚举字符串中包含的所有抽象 Unicode 字符。最后，可调用`StringInfo` 的静态方法`ParseCombiningCharacters`来返回一个`Int32`数组。从数组长度就能可以字符串包含多少个文本元素。每个数组元素都是一个文本元素的起始码值索引。

以下代码演示了使用 `StringInfo` 类来处理字符串中的文本元素的各种方式：

```C#
using System;
using System.Text;
using System.Globalization;
using System.Windows.Forms;

public sealed class Program {
    public static void Main() {
        // 以下字符串包含组合字符
        String s = "a\u0304\u0308bc\u0327";
        SubstringByTextElements(s);
        EnumTextElements(s);
        EnumTextElementIndexes(s);
    }

    private static void SubstringByTextElements(String s) {
        String output = String.Empty;

        StringInfo si = new StringInfo(s);
        for (Int32 element = 0; element < si.LengthInTextElements; element++) {
            output += String.Format("Text element {0} is '{1}'{2}", element, si.SubstringByTextElements(element, 1), Environment.NewLine);
        }
        MessageBox.Show(output, "Result of SubstringByTextElements");
    }

    private static void EnumTextElements(String s) {
        String output = String.Empty;

        TextElementEnumerator charEnum = StringInfo.GetTextElementEnumerator(s);
        while (charEnum.MoveNext()) {
            output += String.Format("Character at index {0} is '{1}'{2}", charEnum.ElementIndex, charEnum.GetTextElement(), Environment.NewLine);
        }
        MessageBox.Show(output, "Result of GetTextElementEnumerator");
    }

    private static void EnumTextElementIndexes(String s) {
        String output = String.Empty;

        Int32[] textElemIndex = StringInfo.ParseCombiningCharacters(s);
        for (Int32 i = 0; i < textElemIndex.Length; i++) {
            output += String.Format("Character {0} starts at index {1}{2}", i, textElemIndex[i], Environment.NewLine);
        }
        MessageBox.Show(output, "Result of ParseCombiningCharacters");
    }
}
```

编译并运行上述代码，会显示如图 14-2、图 14-3 和 图 14-4 所示的对话框。

![14_2](../resources/images/14_2.png)  
图 14-2 `SubstringByTextElements`的结果 

![14_3](../resources/images/14_3.png)  
图 14-3 `GetTextElementEnumerator`的结果  

![14_4](../resources/images/14_4.png)  
图 14-4 `ParseCombiningCharacters`的结果  

### 14.2.7 其他字符串操作

还可利用`String`类型提供的一些方法来复制整个字符串或者它的一部分。表 14-1 总结了这些方法。

表 14-1 用于复制字符串的方法 
|成员名称|方法类型|说明|
|:---:|:---:|:---:|
|`Clone`|实例|返回对同一个对象(`this`)的引用。能这样做是因为`String`对象不可变(immutable)。该方法实现了 `String` 的 `ICloneable`接口|
|`Copy`|静态|返回指定字符串的新副本。该方法很少用，它的存在只是为了帮助一些需要把字符串当作 token 来对待的应用程序。通常，包含相同字符内容的多个字符串会被“留用”(intern)为单个字符串。该方法创建新字符串对象，确保即时字符串包含相同字符内容，引用(指针)也有所不同|
|`CopyTo`|实例|将字符串中的部分字符复制到一个字符数组中|
|`Substring`|实例|返回代表原始字符串一部分的新字符串|
|`ToString`|实例|返回对同一个对象(`this`)的引用|

除了这些方法，`String`还提供了多个用于处理字符串的静态方法和实例方法，比如`Insert`，`Remove`，`PadLeft`，`Replace`，`Split`，`Join`，`ToLower`，`ToUpper`，`Trim`，`Concat`，`Format`等。使用所有这些方法时都请牢记一点，它们返回的都是新的字符串对象。这是由于字符串是不可变的。一经创建，便不能修改(使用安全代码的话)。

## <a name="14_3">14.3 高效率构造字符串</a>

由于`String`类型代表不可变字符串，所以 FCL 提供了 `System.Text.StringBuilder` 类型对字符串和字符进行高效动态处理，并返回处理好的`String`对象。可将`StringBuilder`想像成创建 `String` 对象的特殊构造器。你的方法一般应获取 `String` 参数而非 `StringBuilder`参数。

从逻辑上说，`StringBuilder`对象包含一个字段，该字段引用了由`Char`结构构成的数组。可利用`StringBuilder`的各个成员来操纵该字符数组，高效率地缩短字符串或更改字符串中的字符。如果字符串变大，超过了事先分配的字符数组大小，`StringBuilder`会自动分配一个新的、更大的数组，复制字符，并开始使用新数组。前一个数组被垃圾回收。

用 `StringBuilder` 对象构造好字符串后，调用`StringBuilder`的`ToString`方法即可将`StringBuilder`的字符数组“转换”成`String`。这样会在堆上新建`String`对象，其中包含调用`ToString`时存在于`StringBuilder`中的字符串。之后可继续处理`StringBuilder`中的字符串。以后可再次调用`ToString`把它转换成另一个`String`对象。

### 14.3.1 构造`StringBuilder`对象

和`String`类不同，CLR 不觉得`StringBuilder`类有什么特别。此外，大多数语言(包括C#)都不将`StringBuilder`类视为基元类型。要像构造其他任何非基元类型那样构造`StringBuilder`对象：  

`StringBuilder sb = new StringBuilder();` 

`StringBuilder`类型提供了许多构造器。每个构造器的职责是分配和初始化由每个`StringBuilder`对象维护的状态。下面解释了`StringBuilder`类的关键概念。

* **最大容量**  
  一个`Int32`值，指定了能放到字符串中的最大字符数。默认值是`Int32.MaxValue`(约 20 亿)。一般不用更改这个值，但有时需要指定较小的最大容量以确保永远不会创建超出特定长度的字符串。构造好之后，这个`StringBuilder`的最大容量就固定下来了，不能再变。
  
* **容量**  
  一个`Int32`值，指定了由`StringBuilder`维护的字符数组的长度。默认为`16`。如果事先知道要在这个`StringBuilder`中放入多少字符，那么构造`StringBuilder`对象时应该自己设置容量。  
  向字符数组追加字符时，`StringBuilder`会检测数组会不会超过设定的容量。如果会，`StringBuilder`会自动倍增容量字段，用新容量来分配新数组，并将原始数组的字符复制到新数组中。随后，原始数组可以被垃圾回收。数组动态扩容会损害性能。要避免就要设置一个合适的初始容量。
  
* **字符数组**  
  一个由`Char` 结构构成的数组，负责维护“字符串”的字符内容。字符数总是小于或等于“容量”和“最大容量”值。可用`StringBuilder`的`Length`属性来获取数组中已经使用的字符数。`Length`总是小于或等于`StringBuilder`的“容量”值。可在构造`StringBuilder`时传递一个`String`来初始化字符数组。不传递字符串，数组刚开始不包含任何字符——也就是说，`Length`属性返回`0`。

### 14.3.2 `StringBuilder`的成员 

和`String`不同，`StringBuilder`代表可变(mutable)字符串。也就是说，`StringBuilder`的大多数成员都能更改字符数组的内容，同时不会造成在托管堆上分配新对象。`StringBuilder`只有以下两种情况才会分配新对象。

* 动态构造字符串，其长度超过了设置的“容量”。
* 调用`StringBuilder`的`ToString`方法。

表 14-2 总结了 `StringBuilder`的成员。

表 14-2 StringBuilder 的成员

|成员名称|成员类型|说明|
|:---:|:---:|:---:|
|`MaxCapacity`|只读属性|返回字符串能容纳的最大字符串(最大容量)|
|`Capacity`|可读/可写属性|获取或设置字符数组的长度(容量)。将容量设得比字符串长度小或者比`MaxCapacity`大将抛出`ArgumentOutOfRangeException`异常|
|`EnsureCapacity`|方法|保证字符数组至少具有指定的长度(容量)。如果传给方法的值大于`StringBuilder`的当前容量，当前容量会自动增大。如果当前容量已大于传给该属性的值，则不发生任何变化|
|`Length`|可读/可写属性|获取或设置“字符串”中的字符数。它可能小于字符数组的当前容量。将这个属性设为`0`，会将`StringBuilder`的内容重置为肯空字符串|
|`ToString`|方法|这个方法的无参版本返回代表`StringBuilder`的字符数组的一个`String`|
|`Chars`|可读/可写索引器属性|获取或设置字符数组指定索引位置的字符。在C#中，这是一个索引器(有参属性)，要用数组语法(`[]`)来访问|
|`Clear`|方法|清除`StringBuilder`对象的内容，等同于把它的`Length`属性设为`0`|
|`Append`|方法|在字符数组末尾追加一个对象；如果必要，数组会进行扩充。使用常规格式和与调用线程关联的语言文化将对象转换成字符串|
|`Insert`|方法|在字符数组中插入一个对象；如果必要，数组会进行扩充。使用常规格式和与调用线程关联的语言文化将对象转换成字符串|
|`AppendFormat`|方法|在字符数组末尾追加指定的零个或多个对象；如有必要，数组会进行扩充。使用调用者提供的格式化和语言文化信息，将这些对象转换成字符串。`AppendFormat`是处理`StringBuilder`对象时最常用的方法之一|
|`AppendLine`|方法|在字符数组末尾追加一个行中止符或者一个带行中止符的字符串；如有必要，会增大数组的容量|
|`Replace`|方法|将字符数组中的一个字符替换成另一个字符，或者将一个字符串替换成另一个字符串|
|`Remove`|方法|从字符数组中删除指定范围的字符|
|`Equals`|方法|只有两个`StringBuilder`对象具有相同最大容量、字符数组容量和字符内容才返回`true`|
|`CopyTo`|方法|将`StringBuilder`的字符内容的一个子集复制到一个`Char`数组中|

使用`StringBuilder`的方法要注意，大多数方法返回的都是对同一个`StringBuilder`对象的引用，所以几个操作能连接到一起完成：

```C#
StringBuilder sb = new StringBuilder();
String s = sb.AppendFormat("{0} {1}", "Jeffrey", "Richter").Replace(' ', '-').Remove(4, 3).ToString();
Console.WriteLine(s);       // 显示"Jeff-Richter"
```

`String`和`StringBuilder`类提供的方法并不完全对应。例如，`String`提供了`ToLower`，`ToUpper`，`EndsWith`，`PadLeft`，`PadRight`,`Trim`等方法；但 `StringBuilder` 类没有提供任何与之对应的方法。另一方面，`StringBuilder`类提供了功能更全面的`Replace`方法，允许替换作为字符串一部分的字符或者子字符串(而不是一定要替换整个字符串)。由于两个类不完全对应，所以有时需要在`String`和`StringBuiler`之间转换以完成特定任务。例如，要构造字符串将所有字符转换成大写，再在其中插入一个子字符串，需要写下面这样的代码：

```C#
// 构造一个 StringBuilder 来执行字符串操作
StringBuilder sb = new StringBuilder();

// 使用 StringBuilder 来执行一些字符串操作
sb.AppendFormat("{0} {1}", "Jeffrey", "Richter");

// 将 StringBuilder 转换成 String，以便将所有字符转换成大写
String s = sb.ToString().ToUpper();

// 清除 StringBuilder (分配新的 Char 数组)
sb.Length = 0;

// 将全部字符大写的 String 加载到 StringBuilder 中，执行其他操作
sb.Append(s).Insert(8, "Marc-");

// 将 StringBuilder 转换回 String
s = sb.ToString();

// 向用户显示 String
Console.WriteLine(s);  // “JEFFREY-Marc-RICHTER”
```

仅仅因为 `StringBuilder`没有提供`String`提供的所有操作就要像这样写代码，显然是不方便的，效率也很低。希望 Microsoft 将来能为`StringBuilder`添加更多的字符串操作，进一步完善这个类。

## <a name="14_4">14.4 获取对象的字符串表示：ToString</a>

经常都要获取对象的字符串表示。例如，可能需要向用户显示数值类型(比如 `Byte`，`Int32`和`Single`)或者`DateTime`对象。由于.NET Framework 是面向对象的平台，每个类型都有责任提供代码将实例的值转换成字符串表示。FCL 的设计者为此规划了统一的模式。本节将描述这个模式。

可调用`ToString` 方法获取任何对象的字符串表示。`System.Object`定义了一个`public`、`virtual`、无参的`ToString`方法，所以在任何类型的实例上都能调用该方法。在语义上，`ToString`返回代表对象当前值的字符串，该字符串应根据调用线程当前的语言文化进行格式化。例如，在数字的字符串表示中，应该使用与调用线程的语言文化关联的小数点符号、数字分组符号和其他元素。

`System.Object`实现的`ToString`只是返回对象所属类型的全名。这个值用处不大，但对许多不能提供有意义的字符串的类型来说，这也是一个合理的默认值。例如，一个`FileStream`或`Hashtable`对象的字符串表示应该是什么呢？

任何类型要想提供合理的方式获取对象当前值的字符串表示，就应重写`ToString`方法。FCL 内建的许多核心类型(`Byte`，`Int32`，`UInt64`，`Double`等)都重写了`ToString`，能返回符合语言文化的字符串。在 Visual Studio 调试器中，鼠标移动变量上方会出现一条数据提示(datatip)。提示中的文本正是通过调用对象的`ToString`方法来获取的。所以，定义类时应该总是重写`ToString`方法，以提供良好的调试支持。


