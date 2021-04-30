# 第 19 章 可空值类型

本章内容

* <a href="#19_1">C# 对可空值类型的支持</a>
* <a href="#19_2">C# 的空接合操作符</a>
* <a href="#19_3">C# 对可空值类型的特殊支持</a>

我们知道值类型的变量永远不会为 `null`；它总是包含值类型的值本身。事实上，这正是“值类型”一次的由来。遗憾的是，这在某些情况下会成为问题。例如，设计数据库时，可将一个列的数据类型定义成一个 32 位整数，并映射到 FCL(Framework Class Library)的 `Int32` 数据类型。但是，数据库中的一个列可能允许值为空；也就是说，该列在某一行上允许没有任何值。用 Microsoft .NET Framework 处理数据库数据可能变得很困难，因为在 CLR 中，没有办法将 `Int32` 值表示成 `null`。

> 注意 Microsoft ADO.NET 的表适配器(table adapter)确实支持可空类型。遗憾的是 `System.Data.SqlTypes` 命名空间中的类型没有用可空类型替换，部分原因是类型之间没有“一对一”的对应关系。例如，`SqlDecimal` 类型最大允许 38 位数，而普通的 `Decimal` 类型最大允许 38 位数，而普通的 `Decimal` 类型最大只允许 29 位数。此外， `SqlString` 类型支持它自己的本地化和比较选项，而普通的 `String` 类型并不支持这些。

下面是以另一个例子：Java 的 `java.util.Date` 类是引用类型，所以该类型的变量能设为 `null`。但 CLR 的 `System.DateTime` 是值类型，`DateTime` 变量永远不能设为 `null`。如果用 Java 写的一个应用程序想和运行 CLR 的 Web 服务交流日期/时间，那么一旦 Java 程序发送 `null`，就会出问题，因为 CLR 不知道如何表示 `null`，也不知道如何操作它。

为了解决这个问题， Microsoft 在 CLR 中引入了**可空值类型**的概念。为了理解它们是如何工作的，先来看看 FCL 中定义的 `System.Nullable<T>`结构。以下是`System.Nullable<T>`定义的逻辑表示：

```C#
using System;
using System.Runtime.InteropServices;

[Serializable, StructLayout(LayoutKind.Sequential)]
public struct Nullable<T> where T : struct {
    // 这两个字段表示状态
    private Boolean hasValue = false;       // 假定 null
    internal T value = default(T);          // 假定所有位都为零

    public Nullable(T value) {
        this.value = value;
        this.hasValue = true;
    }

    public Boolean HasValue { get { return hasValue; } }

    public T Value {
        get {
            if (!hasValue) {
                throw new InvalidOperationException("Nullable object must have a value.");
            }
            return value;
        }
    }

    public T GetValueOrDefault() { return value; }

    public T GetValueOrDefault(T defaultValue) {
        if (!HasValue) return defaultValue;
        return value;
    }

    public override Boolean Equals(object other) {
        if (!HasValue) return (other == null);
        if (other == null) return false;
        return value.Equals(other);
    }

    public override int GetHashCode() {
        if (!HasValue) return 0;
        return value.GetHashCode();
    }

    public override string ToString() {
        if (!HasValue) return "";
        return value.ToString();
    }

    public static implicit operator Nullable<T>(T value) {
        return new Nullable<T>(value);
    }

    public static explicit operator T(Nullable<T> value) {
        return value.Value;
    }
}
```

可以看出，该结构能表示可为 `null` 的值类型。由于 `Nullable<T>` 本身是值类型，所以它的实例仍然是“轻量级”的。也就是说，实例仍然可以在栈上，而且实例的大小和原始值类型基本一样，只是多了一个 `Boolean` 字段。注意 `Nullable` 的类型参数 `T` 被约束为`struct`。这是由于引用类型的变量本来就可以为 `null`，所以没必要再去照顾它。

现在，要在代码中使用一个可空的 `Int32`，就可以像下面这样写：

```C#
Nullable<Int32> x = 5;
Nullable<Int32> y = null;
Console.WriteLine("x: HasValue={0}, Value={1}", x.HasValue, x.Value);
Console.WriteLine("y: HasValue={0}, Value={1}", y.HasValue, y.GetValueOrDefault());
```

编译并运行上述代码，将获得以下输出：

```cmd
x: HasValue=True, Value=5
y: HasValue=False, Value=0
```

## <a name="19_1">19.1 C#对可空值类型的支持</a>

C# 允许使用相当简单的语法初始化上述两个 `Nullable<Int32>` 变量 `x` 和 `y`。事实上，C# 开发团队的目的是将可空值类型集成到 C# 语言中，使之成为 “一等公民”。为此，C# 提供了更清新的语法来处理可空值类型。C# 允许用问号表示法来声明并初始化 `x` 和 `y` 变量：

```C#
Int32? x = 5;
Int32? y = null;
```

在 C#中， `Int32?` 等价于 `Nullable<Int32>`。但 C# 在此基础上更进一步，允许开发人员在可空实例上执行转换和转型<sup>①</sup>。C# 还允许向可空实例应用操作符。以下代码对此进行了演示：

> ① 作者在这里区分了转换和转型。例如，从 `Int32` 的可空版本到非可空版本(或相反)，称为“转换”。但是，涉及不同基元类型的转换，就称为“转型”或“强制类型转换”。 ———— 译注

```C#
private static void ConversionsAndCasting() {
    // 从非可空 Int32 隐式转换为 Nullable<Int32>
    Int32? a = 5;

    // 从 'null' 隐式转换为 Nullable<Int32>
    Int32? b = null;

    // 从 Nullable<Int32> 显式转换为非可空 Int32
    Int32 c = (Int32) a;

    // 在可空基元类型之间转型
    Double? d = 5;      // Int32 转型为 Double? (d 是 double 值 5.0)
    Double? e = b;      // Int32?转型为 Double? (e 是 null)
}
```

C# 还允许向可空实例应用操作符，例如：

```C#
private static void Operators() {
    Int32? a = 5;
    Int32? b = null;

    // 一元操作符 (+ ++ - -- ! ~)
    a++;            // a = 6
    b = -b;         // b = null

    // 二元操作符 (+ - * / % & | ^ << >>)
    a = a + 3;      // a = 9;
    b = b * 3;      // b = null;

    // 相等性操作符 ((== !=)
    if (a == null) { /* no */   } else { /* yes */ }
    if (b == null) { /* yes */  } else { /* no  */ }
    if (a != b) { /* yes */     } else { /* no  */ }

    // 比较操作符 (< > <= >=)
    if (a < b)          { /* no */ } else { /* yes */ }
}
```

下面总结了 C# 如何解析操作符。

* **一元操作符(`+`，`++`，`-`，`--`，`!`，`~`)**
  操作数是 `null`，结果就是 `null`。

* **二元操作符(`+`，`-`，`*`，`/`，`%`，`&`，`|`，`^`，`<<`，`>>`))**
  两个操作数任何一个是 `null`，结果就是 `null`。但有一个例外，它发生在将`&`和`|`操作符应用于 `Boolean?`操作数的时候。在这种情况下，两个操作符的行为和 SQL 的三值逻辑一样。对于这两个操作符，如果两个操作数都不是`null`，那么操作符和平常一样工作。如果连个操作数都是`null`，结果就是`null`。特殊行为仅在其中之一为`null`时发生。下表列出了针对操作数的`true`，`false` 和 `null`三个值的各种组合，两个操作符的求值情况。

  |操作数 1 →  <br/> 操作数2 ↑ |true|false|null|
  |:---:|:---:|:---:|:---:|
  |true|& = true <br/>\| =ture | & = false <br/> \| = true| & = null <br/> \| = true|
  |false|& = false <br/> \| = true| & = false <br/> \| = false| & = false <br/> \| = null|
  |null| & = null <br/> \| = true|& = false <br/>\| = null| & = null <br/>\| = null|

* **相等性操作符(`==`，`!=`)**
  两个操作数都是 `null`，两者相等。一个操作数是 `null`，两者不相等。两个操作数都不是 `null`，就比较值来判断是否相等。

* **关系操作符(`<`，`>`，`<=`，`>=`)**
  两个操作数任何一个是 `null`，结果就是 `false`。两个操作数都不是 `null`，就比较值。

注意，操作可空实例会生成大量代码。例如以下方法：

```C#
private static Int32? NullableCodeSize(Int32? a, Int32? b) {
    return (a + b);
}
```

编译这个方法会生成相当多的 IL 代码，而且操作可空类型的速度慢于非可空类型。编译器生成的 IL 代码等价于以下 C# 代码：

```C#
private static Nullable<Int32> NullableCodeSize(Nullable<Int32> a, Nullable<Int32> b) {
    Nullable<Int32> nullable1 = a;
    Nullable<Int32> nullable2 = a;
    if (!(nullable1.HasValue & nullable2.HasValue)) {
        return new Nullable<Int32>();
    }
    return new Nullable><Int32> (nullable1.GetvalueOrDefault() + nullable2.GetValueOrDefault());
}
```

最后要说明的是，可定义自己的值类型来重载上述各种操作符符。8.4 节 “操作符重载方法”已对此进行了讨论。使用自己的值类型的可空实例，编译器能正确识别它并调用你重载的操作符(方法)。以下 `Point` 值类型重载了 `==` 和 `!=` 操作符：

```C#
using System;

internal struct Point {
    private Int32 m_x, m_y;
    public Point(Int32 x, Int32 y) { m_x = x; m_y = y; }

    public static Boolean operator == (Point p1, Point p2) {
        return (p1.m_x == p2.m_x) && (p1.m_y == p2.m_y);
    }

    public static Boolean operator != (Point p1, Point p2) {
        return !(p1 == p2);
    }
}
```

然后可以使用 `Point` 类型的可空实例，编译器能自动调用你重载的操作符(方法):

```C#
Are points equal? False
Are points not equal? True
```

## <a name="19_2">19.2 C#的空接合操作符</a>

C# 提供了一个“空接合操作符”(null-coalescing operator)，即`??`操作符，它要获取两个操作数。假如左边的操作数不为 `null`，就返回这个操作数的值。如果左边的操作数为 `null`，就返回右边的操作数的值。利用空接合操作符，可以方便地设置变量的默认值。

空接合操作符的一个好处在于，它既能用于引用类型，也能用于可空值类型。以下代码演示了如何使用 `??` 操作符：

```C#
private static void NullCoalescingOperator() {
    Int32? b = null;

    // 下面这行等价于：
    // x = (b.HasValue) ? b.Value : 123
    Int32 x = b ?? 123;
    Console.WriteLine(x);           // "123"

    // 下面这行等价于：
    // String temp = GetFilename();
    // filename = (temp != null) ? temp : "Untitled";
    String filename = GetFilename() ?? "Untitled";
}
```

有人争辩说 `??` 操作符不过是 `?:`操作符的“语法糖”而已，所以C#编译器团队不应该将这个操作符添加到语言中。实际上，`??` 提供了重大的语法上的改进。第一个改进是`??`操作符能更好地支持表达式：

`Func<String> f = () => SomeMthod() ?? "Untitled";`

相比下一行代码，上述代码更容易阅读和理解。下面这行代码要求进行变量赋值，而且用一个语句还搞不定：

```C#
Func<String> f = () => { var temp = SomeMethod();
    return temp != null ? temp : "Untitled"; };
```

第二个改进是 `??` 在复合情形中更好用。例如，下面这行代码：

`String s = SomeMethod1() ?? SomeMethod2() ?? "Untitled";`

它比下面这一堆代码更容易阅读和理解：

```C#
String s;
var sm1 = SomeMethod1();
if (sm1 != null ) s = sm1;
else {
    var sm2 = SomeMethod2();
    if (sm2 != null ) s = sm2;
    else s = "Untitled";
}
```

## <a name="19_3">19.3 CLR 对可空值类型的特殊支持</a>

CLR 内建对可空值类型的支持。这个特殊的支持是针对装箱、拆箱、调用 `GetType` 和调用接口方法提供的，它使可空类型能无缝地集成到 CLR 中，而且使它们具有更自然的行为，更符合大多数开发人员的预期。下面深入研究一下 CLR 对可空类型的特殊支持。

### 19.3.1 可空值类型的装箱

假定有一个逻辑设为 `null` 的 `Nullable<Int32>` 变量。将其传给期待一个 `Object` 的方法，就必须对其进行装箱，并将对已装箱`Nullable<Int32>` 的引用传给方法。但对表面上为 `null`的值进行装箱不符合直觉————即使`Nullable<Int32>`变量本身非 `null`，它只是在逻辑上包含了 `null`。为了解决这个问题，CLR 会在装箱可空变量时执行一些特殊代码，从表面上维持可空类型的“一等公民”地位。

具体地说，当 CLR 对 `Nullable<T>` 实例进行装箱时，会检查它是否为 `null`。如果是，CLR 不装箱任何东西，直接返回 `null`。如果可空实例不为 `null`，CLR 从可空实例中取出值并进行装箱。也就是说，一个值为 5 的 `Nullable<Int32>` 会装箱成值为 5 的已装箱 `Int32`。以下代码演示了这一行为：

```C#
// 对 Nullable<T> 进行装箱，要么返回 null，要么返回一个已装箱的 T
Int32？ n = null;
Object o = n;       // o 为 null
Console.WriteLine("o is null={0}", o == null);   // "True"

n = 5;
o = n;   // o 引用一个已装箱的 Int32
Console.WriteLine("o's type={0}", o.GetType()); // "System.Int32"
```

### 19.3.2 可空值类型的拆箱

CLR 允许将已装箱的值类型 `T` 拆箱为一个 `T` 或者 `Nullable<T>`。如果对已装箱类型的引用是 `null`，而且要把它拆箱为一个 `Nullable<T>`，那么 CLR 会将 `Nullable<T>`的值设为 `null`。以下代码演示了这个行为：

```C#
// 创建已装箱的 Int32
Object o = 5;

// 
Int32? a = (Int32?) o;  // a = 5
Int32 b = (Int32) o;    // b = 5

// 创建初始化为 null 的一个引用
o = null;

// 把它“拆箱”为一个 Nullable<Int32> 和一个 Int32
a = (Int32?) o;     // a = null
b = (Int32) o;      // NullReferenceException
```

### 19.3.3 通过可空值类型调用 GetType

在 `Nullable<T>` 对象上调用 `GetType`，CLR实际会“撒谎”说类型是 `T`，而不是 `Nullable<T>`。以下代码演示了这一行为：

```C#
Int32? x = 5;
// 下面这行会显示 "System.Int32"，而非“System.Nullable<Int32>”
Console.WriteLine(x.GetType());
```

### 19.3.4 通过可空值类型调用接口方法

以下代码将 `Nullable<Int32>` 类型的变量 `n` 转型为接口类型 `IComparable<Int32>`。`Nullable<T>` 不像 `Int32` 那样实现了 `IComparable<Int32>` 接口，但 C# 编译器允许这样的代码通过编译，而且 CLR 的校验器也认为这样的代码可验证，从而允许使用更简洁的语法：

```C#
Int32? n = 5;
Int32 result = ((IComparable) n).CompareTo(5);      // 能顺利编译和运行
Console.WriteLine(result);                          // 0
```

假如 CLR 不提供这一特殊支持，要在可空值类型上调用接口方法，就必须写很繁琐的代码。首先要转型为已拆箱的值类型，然后才能转型为接口以出发调用：

`Int32 result = ((IComparable) (Int32) n).CompareTo(5);  // 很繁琐`