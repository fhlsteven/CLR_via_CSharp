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

* 