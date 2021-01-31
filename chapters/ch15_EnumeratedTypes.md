# 第 15 章 枚举类型和位标志

本章内容：

* <a href="#15_1">枚举类型</a>
* <a href="#15_2">位标志</a>
* <a href="#15_3">为枚举类型添加方法</a>

本章要讨论枚举类型和位标志。由于 Microsoft Windows 和许多编程语言多年来一直在使用这些结构，相信许多人已经知道了如何使用它们。不过，CLR 与 FCL 结合起来之后，枚举类型和位标志才正成为面向对象的类型。而它们提供的一些非常“酷”的功能，我相信大多数开发人员并不熟悉。让我惊讶的是，这些新功能极大地简化了应用程序开发，个中缘由且听我娓娓道来。

## <a name="15_1">15.1 枚举类型</a>

**枚举类型**(enumerated type)定义了一组“符号名称/值”配对。例如，以下 `Color` 类型定义了一组符号，每个符号都标识一种颜色：

```C#
internal enum Color {
    White,          // 赋值 0
    Red,            // 赋值 1
    Green,          // 赋值 2
    Blue,           // 赋值 3
    Orange          // 赋值 4
}
```

当然，也可写程序用 0 表示白色，用 1 表示红色，以此类推。不过，不应将这些数字硬编码到代码中，而应使用枚举类型，理由至少有二。

* 枚举类型使程序更容易编写、阅读和维护。有了枚举类型，符号名称可在代码中随便使用，程序员不用费心思量每个硬编码值的含义(例如，不用念叨 white 是 0 ， 或者 0 是 white)。而且，一旦与符号名称对应的值发生改变，代码也可以简单地重新编译，不需要对源代码进行任何修改。此外，文档工具和其他实用程序(比如调试程序)能向开发人员显示有意义的符号名称。

* 枚举类型是强类型的。例如，将 `Color.Orange` 作为参数传给要求 `Fruit` 枚举类型的方法，编译器会报错。<sup>①</sup>

> ① `ruit` 枚举类型定义的应该是水果，而 `Color` 枚举类型定义的是颜色。虽然两个枚举类型中都有一个 `Orange`，但分别代表橙子和橙色。 ——译注

在 Microsoft .NET Framework 中，枚举类型不只是编译器所关心的符号，它还是类型系统中的“一等公民”，能实现很强大的操作。而在其他环境(比如非托管 C++)中，枚举类型是没有这个特点的。

每个枚举类型都直接从 `System.Enum` 派生，后者从 `System.ValueType` 派生，而 `System.ValueType` 又从 `System.Object` 派生。所以，枚举类型是值类型(详情参见第 5 章“基元类型、引用类型和值类型”)，可用未装箱和已装箱的形式来表示。但有别于其他值类型，枚举类型不能定义任何方法、属性或事件。不过，可利用C#的“扩展方法”功能模拟向枚举类型添加方法，15.3节“向枚举类型添加方法”展示了一个例子。

编译枚举类型时，C# 编译器把每个符号转换成类型的一个常量字段。例如，编译器将前面的 `Color` 枚举类型看成是以下代码：

```C#
internal struct Color : System.Enum {
    // 以下是一些公共常量，它们定义了 Color 的符号和值
    public const Color White    = (Color) 0;
    public const Color Red      = (Color) 1;
    public const Color Green    = (Color) 2;
    public const Color Blue     = (Color) 3;
    public const Color Orange   = (Color) 4;

    // 以下是一个公共实例字段，包含 Color 变量的值，
    // 不能写代码来直接引用该字段
    public Int32 value__;
}
```

C# 编译器不会实际地编译上述代码，因为它禁止定义从特殊类型 `System.Enum` 派生的类型。不过，可通过上述伪类型定义了解内部的工作方式。简单地说，枚举类型只是一个结构，其中定义了一组常量字段和一个实例字段。常量字段会嵌入程序集的的元数据中，并可通过反射来访问。这意味着可以在运行时获得与枚举类型关联的所有符号及其值。还意味着可以将字符串符号转换成对应的数值。这些操作是通过`System.Enum`基类型来提供的，该类型提供了几个静态和实例方法，可利用它们操作枚举类型的实例，从而避免了必须使用反射的麻烦。下面将讨论其中一些操作。

> 重要提示 枚举类型定义的符号时常量值。所以当编译器发现代码引用了枚举类型的符号时，会在编译时用数值替换符号，代码不再引用定义了符号的枚举类型。这意味着运行时可能并不不要定义了枚举类型的程序集，编译时才需要。假如代码引用了枚举类型(而非仅仅引用类型定义的符号)，那么运行时就需要包含了枚举类型定义的程序集。可能会出现一些版本问题，因为枚举类型符号是常量，而非只读的值。7.1 节 “常量”已解释过这些问题。

例如，`System.Enum` 类型有一个名为`GetUnderlyingType`的静态方法，而`System.Type`类型有一个名为`GetEnumUnderlyingType`的实例方法：

```C#
public static Type GetUnderlyingType(Type enumType);    // System.Enum 中定义
public Type GetEnumUnderlyingType();                    // System.Type 中定义
```

这些方法返回用于容纳一个枚举类型的值的基础类型。每个枚举类型都有一个基础类型，它可以是`byte`，`sbyte`，`short`，`ushort`，`int`(最常用，也是 C#默认选择的)，`uint`，`long`或`ulong`。虽然这些 C# 基元类型<sup>①</sup>都有对应的 FCL 类型，但 C#编译器为了简化本身的实现，要求只能指定基元类型名称。如果使用 FCL 类型名称(比如 `Int32`)，会显示以下错误信息：`error CS1008：应输入类型 byte、sbyte、short、ushort、int、uint、long 或 ulong`。

> ① 不要混淆“基础类型”和“基元类型”(参见 5.1 节“编程语言的基元类型”)。虽然枚举的基础类型就是这些基元类型(其实就是除`Char`之外的所有整型)，但英语中用 underlying type 和 primitive type 进行了区分，中文翻译同样要区分。简而言之，基元类型是语言的内建类型，编译器能直接识别。——译注

以下代码演示了如何声明一个基础类型为 `byte(System.Byte)`的枚举类型：

```C#
internal enum Color : byte {
    White,
    Red,
    Green,
    Blue,
    Orange
}
```

基于这个`Color`枚举类型，以下代码显示了`GetUnderlyingType`的返回结果：

```C#
// 以下代码会显示 “System.Byte”
Console.WriteLine(Enum.GetUnderlyingType(typeof(Color)));
```

C# 编译器将枚举类型视为基元类型。所以可用许多熟悉的操作符(`==`，`!=`，`<`，`>`，`<=`，`>=`，`+`，`-`，`^`，`&`，`|`，`~`，`++`和`--`)来操纵枚举类型的实例。所有这些操作符实际作用于每个枚举类型实例内部的 `value__` 实例字段。此外，C# 编译器允许将枚举类型的实例显式转型为不同的枚举类型。也可显式将枚举类型实例转型为数值类型。

给定一个枚举类型的实例，可调用从`System.Enum` 继承的 `ToString` 方法，把这个值映射为以下几种字符串表示：

```C#
Color c = Color.Blue;   
Console.WriteLine(c);                   // “Blue” (常规格式)
Console.WriteLine(c.ToString());        // “Blue” (常规格式)
Console.WriteLine(c.ToString("G"));     // “Blue” (常规格式)
Console.WriteLine(c.ToString("D"));     // “3”  (十进制格式)
Console.WriteLine(c.ToString("X"));     // “03” (十六进制格式)
```

> 注意 使用十六进制格式时，`ToString`总是输出大写字母。此外，输出几位数取决于枚举的基础类型：`byte/sbyte` 输出 2 位数，`short/ushort`输出 4 位数，`int/uint` 输出 8 位数，而 `long/ulong` 输出 16 位数。如有必要会添加前导零。

除了`ToString`方法，`System.Enum`类型还提供了静态`Format`方法，可调用它格式化枚举类型的值：

`public static String Format(Type enumType, Object value, String format);`

个人倾向于调用`ToString`方法，因为它需要的代码更少，而且更容易调用。但`Format`有一个`ToString`没有的优势：允许为`value`参数传递数值。这样就不一定要有枚举类型的实例。例如，以下代码将显示“Blue”：

```C#
// 以下代码显示“Blue”
Console.WriteLine(Enum.Foramt(typeof(Color), 3, "G"));
```

> 注意 声明有多个符号的枚举类型时，所有符号都可以有相同的数值。使用常规格式将数值转换为符号时，`Enum`的方法会返回其中一个符号，但不保证具体返回哪一个符号名称。另外，如果没有为要查找的数值定义符号，会返回包含该数值的字符串。

也可调用`System.Enum`的静态方法`GetValues`或者`System.Type`的实例方法`GetEnumValues`来返回一个数组，数组中的每个元素都对应枚举类型中的一个符号名称，每个元素都包含符号名称的数值：

```C#
public static Array GetValues(Type enumType);   // System.Enum 中定义
public Array GetEnunmValues();                  // System.Type 中定义
```

该方法可以结合`ToString`方法使用以显示枚举类型中的所有符号名称及其对应数值，如下所示：

```C#
Color[] colors = (Color[])Enum.GetValues(typeof(Color));
Console.WriteLine("Number of symbols defined: " + colors.Length);
Console.WriteLine("Value\tSymbol\n-----\t------");
foreach (Color c in colors) {
    // 以十进制和常规格式显示每个符号
    Console.WriteLine("{0,5:D}\t{0:G}", c);
}
```

以上代码的输出如下：

```cmd
Number of symbols defined: 5
Value   Symbol
-----   ------
    0   White
    1   Red
    2   Green
    3   Blue
    4   Orange
```

我个人不喜欢 `GetValues` 和 `GetEnumValues` 方法，因为两者均返回一个`Array`，必须转型成恰当的数组类型。所以我总是定义自己的方法：

```C#
public static TEnum[] GetEnumValues<TEnum>() where TEnum : struct {
    return (TEnum[])Enum.GetValues(typeof(TEnum));
}
```

使用我的泛型 `GetEnumValues` 方法可获得更好的编译时类型安全性，而且上例的第一行代码可简化成以下形式：

`Color[] colors = GetEnumValues<Color>();`

