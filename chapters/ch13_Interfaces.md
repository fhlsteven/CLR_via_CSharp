# 第 13 章 接口

本章内容：

* <a href="#13_1">类和接口继承</a>
* <a href="#13_2">定义接口</a>
* <a href="#13_3">继承接口</a>
* <a href="#13_4">关于调用接口方法的更多探讨</a>
* <a href="#13_5">隐式和显示接口方法实现(幕后发生的事情)</a>
* <a href="#13_6">泛型接口</a>
* <a href="#13_7">泛型和接口约束</a>
* <a href="#13_8">实现多个具有相同方法名和签名的接口</a>
* <a href="#13_9">用显式接口方法实现来增强编译时类型安全性</a>
* <a href="#13_10">谨慎使用显式接口方法实现</a>
* <a href="#13_11">设计：基类还是接口？</a>

对于多继承(multiple inheritance)的概念，虚度程序员并不陌生，它是指一个类从两个或多个基类派生的能力。例如，假定 `TransmitData`类的作用是发送数据，`ReceiveData`类的作用是接收数据。现在要创建`SocketPort`类，作用是发送和接收数据。在这种情况下，你会希望`SocketPort`从`TransmitData`和`ReceiveData`这两个类继承。

有的编程语言允许多继承，所以能从`TransmitData`和`ReceiveData`这两个基类派生出`SocketPort`。但 CLR 不支持多继承(因此所有托管编程语言也支持不了)。CLR 只是通过 **接口**提供了“缩水版”的多继承。本章将讨论如何定义和使用接口，还要提供一些指导性原则，以便你判断何时应该使用接口而不是基类。

## <a name="13_1">13.1 类和接口继承</a>

Microsoft .NET Framework 提供了`System.Object`类，它定义了 4 个公共实例方法：`ToString`，`Equals`，`GetHashCode` 和`GetType`。该类是其他所有类的根据或者说终极基类。换言之，所有类都继承了`Object`的 4 个实例方法。这还意味着只要代码能操作`Object`类的实例，就能操作任何类的实例。

由于 Microsoft 的开发团队已实现了 `Object` 的方法，所以从`Object`派生的任何类实际都继承了以下内容。

* **方法签名**  
  使代码认为自己是在操作`Object`类的实例，但实际操作的可能是其他类的实例。
* **方法实现**  
  使开发人员定义`Object`的派生类时不必手动实现`Object`的方法。
  
在 CLR 中，任何类都肯定从一个(而且只能是一个)派生类，后者最终从`Object`派生。这个类称为基类。基类提供了一组方法签名和这些方法的实现。你定义的新类可在将来由其他开发人员用作基类——所有方法签名和方法实现都会由新的派生类继承。

CLR 还允许开发人员定义接口，它实际只是对一组方法签名进行了统一命名。这些方法不提任何实现。类通过指定接口名称来继承接口，而且必须显式实现接口方法，否则 CLR 会认为此类型定义无效。当然，实现接口方法的过程可能比较烦琐，所以我才在前面说接口继承是实现多继承的一种“缩水版”机制。C#编译器和 CLR 允许一个类继承多个接口。当然，继承的所有接口方法都必须实现。

我们知道，类继承的一个重要特点是，凡是能使用基类型实例的地方，都能使用派生类型的实例。类似地，接口继承的一个重点特点是，凡是能使用具名接口类型的实例的地方，都能使用实现了接口的一个类型的实例。下面先看看如何定义接口。

## <a name="13_2">13.2 定义接口</a>

如前所述，接口对一组方法签名进行了统一命名。注意，接口还能定义事件、无参属性和有参属性(C# 的索引器)。如前所述，所有这些东西本质上都是方法，它们只是语法上的简化。不过，接口不能定义任何构造器方法，也不能定义任何实例字段。

虽然 CLR 允许接口定义静态方法、静态字段、常量和静态构造器，但符合 CLS 标准的接口绝不允许，因为有的编程语言不能定义或访问它们。事实上，C#禁止接口定义任何一种这样的静态成员。

C# 用 `Interface` 关键字定义接口。要为接口指定名称和一组实例方法签名。下面是 FCL 中的几个接口的定义：

```C#
public interface IDisposable {
    void Dispose();
}

public interface IEnumerable {
    IEnumerator GetEnumerator();
}

public interface IEnumerable<T> : IEnumerable {
    new IEnumerator<T> GetEnumerator();
}

public interface ICollection<T> : IEnumerable<T>, IEnumerable {            
    void     Add(T item);
    void     Clear();
    Boolean  Contains(T item);
    void     CopyTo(T[] array, Int32 arrayIndex);
    Boolean  Remove(T item);
    Int32    Count      { get; }  // 只读属性
    Boolean  IsReadOnly { get; }  //只读属性
}
```

在 CLR 看来，接口定义就是类型定义。也就是说，CLR 会为接口类型对象定义内部数据结构，同时可通过反射机制来查询接口类型的功能。和类型一样，接口可在文件范围中定义，也可嵌套在另一个类型中。定义接口类型时，可指定你希望的任何可见性/可访问性(`public`，`protected`，`internal`等)。

根据约定，接口类型名称以大写字母 `I` 开头，目的是方便在源代码中辨认接口类型。CLR 支持泛型接口(前面几个例子已进行了演示)和接口中的泛型方法。本章稍后会讨论泛型接口的许多土功能。另外，第 12 章“泛型”已全面讨论了泛型。

接口定义可从另一个或多个接口“继承”。但“继承”应打上引号，因为它并不是严格的继承。接口继承的工作方式并不完全和类继承一样。我个人倾向于将接口继承看成是将其他接口的协定(contract)包括到新接口中。例如，`ICollection<T>`接口定义就包含了`IEnumerable<T>`和`IEnumerable`两个接口的协定。这有下面两层含义。

* 继承`ICollection<T>`接口的任何类必须实现`ICollection<T>`，`IEnumerable<T>`和`IEnumerable`这三个接口所定义的方法。
* 任何代码在引用对象时，如果期待该对象的类型实现了`ICollection<T>`接口，可以认为该类型还实现了`IEnumerable<T>`和`IEnumerable`接口。

## <a name="13_3">13.3 继承接口</a>

本节介绍如何定义实现了接口的类型，然后介绍如何创建该类型的实例，并用这个对象调用接口的方法。C#将这个过程变得很简单，但幕后发生的事情还是有点复杂。本章稍后会详细解释。

下面是在`MSCorLib.dll`中定义的 `System.IComparable<T>`接口：

```C#
public interface IComparable<in T> {
    Int32 CompareTo(T other);
}
```

以下代码展示了如何定义实现了该接口的类型，同时还展示了对两个 `Point` 对象进行比较的代码：

```C#
using System;

// Point 从 System.Object 派生，并实现了 IComparable<T>
public sealed class Point : IComparable<Point> {
    private Int32 m_x, m_y;

    public Point(Int32 x, Int32 y) {
        m_x = x;
        m_y = y;
    }

    // 该方法为 Point 实现 IComparable<T>.CompareTo()
    public Int32 CompareTo(Point other) {
        return Math.Sign(Math.Sqrt(m_x * m_x + m_y * m_y) - Math.Sqrt(other.m_x * other.m_x + other.m_y * other.m_y));
    }

    public override String ToString() {
        return String.Format("{0}, {1}", m_x, m_y);
    }
}

public static class Program {
    public static void Main() {
        Point[] points = new Point[] {
            new Point(3, 3),
            new Point(1, 2)
        };

        // 下面调用由 Point 实现的 IComparable<T> 的 CompareTo 方法
        if (points[0].CompareTo(points[1]) > 0) {
            Point tempPoint = points[0];
            points[0] = points[1];
            points[1] = tempPoint;
        }
        Console.WriteLine("Points from closest to (0, 0) to farthest:");
        foreach (Point p in points)
            Console.WriteLine(p);
    }
}
```

C# 编译器要求将实现接口的方法(后文简称为“接口方法”)标记为 `public`。CLR 要求将接口方法标记为`virtual`。不将方法显式标记为`virtual`，编译器会将它们标记为`virtual`和`sealed`；这会阻止派生类重写接口方法。将方法显式标记为`virtual`，编译器就会将该方法标记为`virtual`(并保持它的非密封状态)，使派生类能重写它。

派生类不能重写`sealed`的接口方法。但派生类可重新继承同一个接口，并为接口方法提供自己的实现。在对象上调用接口方法时，调用的是该方法在该对象的类型中的实现。下例对此进行了演示：

```C#
using System;

public static void Main() {
    public static void Main() {
        /************************** 第一个例子 **************************/
        Base b = new Base();

        // 用 b 的类型来调用 Dispose，显示：“Base's Dispose”
        b.Dispose();

        // 用 b 的对象的类型来调用 Dispose，显示：“Base's Dispose”
        ((IDisposable)b).Dispose();

        /************************** 第二个例子 **************************/
        Derived d = new Derived();

        // 用 d 的类型来调用 Dispose，显示：“Derived's Dispose”
        d.Dispose();

        // 用 d 的对象的类型来调用 Dispose，显示：“Derived's Dispose”
        ((IDisposable)d).Dispose();

        /************************** 第三个例子 **************************/
        b = new Derived();

        // 用 b 的类型来调用 Dispose，显示：“Base's Dispose”
        b.Dispose();

        // 用 b 的对象的类型来调用 Dispose，显示：“Derived's Dispose”
        ((IDisposable)b).Dispose();
    }
}

// 这个类派生自 Object，它实现了 IDisposable
internal class Base : IDisposable {
    // 这个方法隐式密封，不能被重写
    public void Dispose() {
        Console.WriteLine("Base's Dispose");
    }
}

// 这个类派生自 Base，它重新实现了 IDisposable
internal class Derived : Base, IDisposable {
    // 这个方法不能重写 Base 的 Dispose，
    // 'new' 表明该方法重新实现了 IDisposable 的 Dispose 方法
    new public void Dispose() {
        Console.WriteLine("Derived's Dispose");

        // 注意，下面这行代码展示了如何调用基类的实现(如果需要的话)
        // base.Dispose();
    }
}
```

## <a name="13_4">13.4 关于调用接口方法的更多探讨</a>

FCL 的`System.String`类型继承了`System.Object`的方法签名及其实现。此外，`String`类型还实现了几个接口：`IComparable`，`ICloneable`,`IConvertible`，`IEnumerable`，`IComparable<String>`，`IEnumerable<Char>`和`IEquatable<String>`。这意味着`String`类型不需要实现(或重写)其 `Object` 基类型提供的方法，但必须实现所有接口声明的方法。

CLR 允许定义接口类型的字段、参数或局部变量。使用接口类型的变量可以调用该接口定义的方法。此外，CLR 允许调用 `Object` 定义的方法，因为所有类都继承了 `Object` 的方法。以下代码对此进行了演示：

```C#
// s 变量引用一个 String 对象
String s = "Jeffrey";
// 可以使用 s 调用在 String, Object, IComparable, ICloneable,
// IConvertible, IEnumerable 中定义的任何方法

// cloneable 变量引用同一个 String 对象
ICloneable cloneable = s;
// 使用 cloneable 只能调用 ICloneable 接口声明的
// 任何方法(或 Object 定义的任何方法)

// comparable 变量引用同一个 String 对象
IComparable comparable = s;
// 使用 comparable 只能调用 IComparable 接口声明的
// 任何方法(或 Object 定义的任何方法)

// enumerable 变量引用同一个 String 对象
// 可在运行时将变量从一个接口转换成另一个，只要
// 对象的类型实现了这两个接口
IEnumerable enumerable = (IEnumerable) comparable;
// 使用 enumerable 只能调用 IEnumerable 接口声明的
// 任何方法(或 Object 定义的任何方法)
```

在这段代码中，所有变量都引用同一个 “Jeffrey” `String` 对象。该对象在托管堆中；所以，使用其中任何变量时，调用的任何方法都会影响这个“Jeffrey” `String`对象。不过，变量的类型规定了能对这个对象执行的操作。`s` 变量是`String`类型，所以可以用`s`调用`String`类型定义的任何成员(比如`Length`属性)。还可用变量`s`调用从`Object`继承的任何方法(比如 `GetType`)。

`cloneable` 变量是 `ICloneable`接口类型。所以，使用`cloneable`变量可以调用该接口定义的 `Clone`方法。此外，可以调用 `Object`定义的任何方法(比如 `GetType`)，因为 CLR 知道所有类型都继承自 `Object`。不过，不能用`cloneable`变量调用`String`本身定义的公共方法，也不能调用由`String`实现的其他任何接口的方法。类似地，使用`comparable`变量可以调用`CompareTo`方法或`Object`定义的任何方法，但不能调用其他方法。

> 重要提示 和引用类型相似，值类型可实现零个或多个接口。但值类型的实例在转换为接口类型时必须装箱。这是由于接口变量是引用，必须指向堆上的对象，使 CLR 能检查对象的类型对象的类型对象指针，从而判断对象的确切类型。调用已装箱值类型的接口方法时，CLR 会跟随对象的类型对象指针找到类型对象的方法表，从而调用正确的方法。

## <a name="13_5">13.5 隐式和显式接口方法实现(幕后发生的事情)</a>

类型加载到 CLR 中时，会为该类型创建并初始化一个方法表(参见第 1 章“CLR的执行模型”)。在这个方法表中，类型引入的每个新方法都有对应的记录项；另外，还为该类型继承的所有虚方法添加了记录项。继承的虚方法既有继承层次结构中的各个基类型定义的，也有接口类型定义的。所以，对于下面这个简单的类型定义：

```C#
internal sealed class SimpleType : IDisposable {
    public void Dispose() { Console.WriteLine("Dispose"); }
}
```

类型的方法表将包含以下方法的记录项。

* `Object`(隐式继承的基类)定义的所有虚实例方法。
* `IDisposable`(继承的接口)定义所有接口方法。本例只有一个方法，即`Dispose`，因为`IDisposable`接口只定义了这个方法。
* `SimpleType`引入的新方法 `Dispose`。

为简化编程，C#编译器假定 `SimpleType` 引入的`Dispose`方法是对`IDisposable`的`Dispose`方法的可访问性是`public`，而接口方法的签名和新引入的方法完全一致。也就是说，两个方法具有相同的参数和返回类型。顺便说一句，如果新的`Dispose`方法被标记为`virtual`，C#编译器仍然认为该方法匹配接口方法。

C#编译器将新方法和接口方法匹配起来之后，会生成元数据，指明 `SimpleType` 类型的方法表中的两个记录项应引用同一个实现。为了更清楚地理解这一点，下面的代码演示了如何调用类的公共`Dispose`方法以及如何调用`IDisposable`的`Dispose`方法在类中的实现：

```C#
public sealed class Program {
    public static void Main() {
        SimpleType st = new SimpleType();

        // 调用公共 Dispose 方法实现
        st.Dispose();

        // 调用 IDisposable 的 Dispose 方法的实现
        IDisposable d = st;
        d.Dispose();
    }
}
```

在第一个 `Dispose` 方法调用中，调用的是 `SimpleType` 定义的 `Dispose` 方法。然后定义 `IDisposable` 接口类型的变量`d`，它引用`SimpleType`对象`st`。调用`d.Dispose()`时，调用的是`IDisposable`接口的`Dispose`方法的实现，所以会执行相同的代码。在这个例子中，两个调用你看不出任何区别。输出结果如下所示：

```cmd
Dispose
Dispose
```

现在重写 `SimpleType`，以便于看出区别：

```C#
internal sealed class SimpleType : IDisposable {
    public void Dispose() { Console.WriteLine("public Dispose"); }
    void IDisposable.Dispose() { Console.WriteLine("IDisposable Dispose"); }
}
```

在不改动前面的`Main`方法的前提下，重新编译并再次运行程序，输出结果如下所示：

```cmd
public Dispose
IDisposable Dispose        
```

在 C# 中，将定义方法的那个接口的名称作为方法名前缀(例如 `IDisposable.Dispose`)，就会创建**显式接口方法实现(Explicit Interface Method Implementation，EIMI<sup>①</sup>)**。注意，C# 中不允许在定义显式接口方法时指定可访问性(比如 `public`或`private`)。但是，编译器生成方法的元数据时，可访问性会自动设为 `private`，防止其他代码在使用类的实例时直接调用接口方法。只有通过接口类型的变量才能调用接口方法。

> ① 请记住 EIMI 的意思，本书后面会大量使用这个缩写词。——译注

还要注意，EIMI 方法不能标记为 `virtual`，所以不能被重写。这是用于 EIMI 方法并非真的是类型的对象模型的一部分，它只是将接口(一组行为或方法)和类型连接起来，同时避免公开行为/方法。如果觉得这一点不好理解，那么你的感觉没有错！它就是不太好理解。本章稍后会介绍 EIMI 有用的一些场合。

## <a name="13_6">13.6 泛型接口</a>

C# 和 CLR 所支持的泛型接口为开发人员提供了许多非常出色的功能。本节要讨论泛型接口提供的一些好处。

首先，泛型接口提供了出色的编译时类型安全性。有的接口(比如非泛型 `IComparable` 接口)定义的方法使用了 `Object` 参数或 `Object` 返回类型，在代码中调用这些接口方法时，可传递对任何类型的实例的引用。但这通常不是我们期望的。下面的代码对此进行了演示：

```C#
private void SomeMethod1() {
    Int32 x = 1, y = 2;
    IComparable c = x;

    // CompareTo 期待 Int32，传递y (一个 Int32)没有问题
    c.CompareTo(y);   // y 在这里不装箱

    // CompareTo 期待 Int32，传递“2”(一个 String)造成编译,
    // 但在运行时抛出 ArgumentException 异常
    c.CompareTo("2");           
}
```

接口方法理想情况下应该使用强类型。这正是 FCL 为什么包含泛型 `IComparable<in T>` 接口的原因。下面修改代码来使用泛型接口：

```C#
private void SomeMethod2() {
    Int32 x = 1, y = 2;
    IComparable<Int32> c = x;
    
    // CompareTo 期待 Int32， 传递 y (一个 Int32)没有问题
    c.CompareTo(y);     // y 在这里不装箱

    // CompareTo 期待 Int32，传递 "2"(一个 String)造成编译错误，
    // 指出 String 不能被转换为 Int32
    c.CompareTo("2");       // 错误
}
```

泛型接口的第二个好处在于，处理值类型时装箱次数会少很多。在 `SomeMethod1` 中，非泛型 `IComparable` 接口的 `CompareTo` 方法期待获取一个 `Object`；传递 `y`(`Int32`值类型)会造成`y`中的值装箱。但在 `SomeMethod2` 中，泛型 `IComparable<in T>`接口的 `CompareTo`方法本来期待的就是 `Int32`；`y`以传值的方式传递，无需装箱。

> 注意 FCL 定义了 `IComparable`，`ICollection`，`IList`和`IDictionary`等接口的泛型和非泛型版本。定义类型时要实现其中任何接口，一般应实现泛型版本。FCL 保留非泛型版本是为了向后兼容，照顾在 .NET Framework 支持泛型之前写的代码。非泛型版本还允许用户以较常规的、类型较不安全(more general,less type-safe)的方式处理数据。

> 有的泛型接口继承了非泛型版本，所以必须同时实现接口的泛型和非泛型版本。例如，泛型 `IEnumerable<out T>`接口继承了非泛型`IEnumerable`接口，所以实现`IEnumerable<out T>`就必须实现`IEnumerable`。

> 和其他代码集成时，有时必须实现非泛型接口，因为接口的泛型版本并不存在。这时，如果接口的任何方法获取或返回 `Object`，就会失去编译时的类型安全性，而且值类型将发生装箱。可利用本章 13.9 节“用显式接口方法实现来增强编译时类型安全性”介绍的技术来缓解该问题。

泛型接口的第三个好处在于，类可以实现同一个接口若干次，只要每次使用不同的类型参数。以下代码对这个好处进行了演示：

```C#
using System;

// 该类实现泛型 IComparable<T> 接口两次
public sealed class Number : IComparable<Int32>, IComparable<String> {
    private Int32 m_val = 5;

    // 该方法实现 IComparable<Int32> 的 CompareTo 方法
    public Int32 CompareTo(Int32 n) {
        return m_val.CompareTo(n);
    }

    // 该方法实现 IComparable<String> 的 CompareTo 方法
    public Int32 CompareTo(String s) {
        return m_val.CompareTo(Int32.Parse(s));
    }
}

public static class Program {
    public static void Main() {
        Number n = new Number();

        // 将 n 中的值和一个 Int32(5) 比较
        IComparable<Int32> cInt32 = n;
        Int32 result = cInt32.CompareTo(5);

        // 将 n 中的值和一个 String("5") 比较
        IComparable<String> cString = n;
        result = cString.CompareTo("5");
    }
}
```

接口的泛型类型参数可标记为逆变和协变，为泛型接口的使用提供更大的灵活性。欲知协变和逆变的详情，请参见 12.5 节“委托和接口的逆变和协变泛型类型实参”。

## <a name="13_7">13.7 泛型和接口约束</a>

上一节讨论了泛型接口的好处。本节要讨论将泛型类型参数约束为接口的好处。

第一个好处在于，可将泛型类型参数约束为多个接口。这样一来，传递的参数的类型必须实现全部接口约束。例如：

```C#
public static class SomeType {
    private static void Test() {
        Int32 x = 5;
        Guid g = new Guid();

        // 对 M 的调用能通过编译，因为 Int32 实现了
        // IComparable 和 IConvertible
        M(x);

        // 这个 M 调用导致编译错误，因为 Guid 虽然
        // 实现了 IComparable，但没有实现 IConvertible
        M(g);
    }

    // M 的类型参数 T 被约束为只支持同时实现了
    // IComparable 和 IConvertible 接口的类型
    private static Int32 M<T>(T t) where T : IComparable, IConvertible {
        ...
    }
}
```

这真的很“酷”！定义方法参数时，参数的类型规定了传递的实参必须是该类型或者它的派生类型。如果参数的类型是接口，那么实参可以是任意类类型，只是该类实现了接口。使用多个接口约束，实际是表示向方法传递的实参必须实现多个接口。

事实上，如果将 `T` 约束为一个类和两个接口，就表示传递的实参类型必须是指定的基类(或者它的派生类)，而且必须实现两个接口。这种灵活性使方法能细致地约束调用者能传递的内容。调用者不满足这些约束，就会产生编译错误。

接口约束的第二个好处是传递值类型的实例时减少装箱。上述代码向 `M`方法传递了 `x`(值类型 `Int32` 的实例)。`x`传给`M`方法时不会发生装箱。如果`M`方法内部的代码调用`t.CompareTo(...)`，这个调用本身也不会引发装箱(但传给`CompareTo`的实参可能发生装箱)。

另一方面，如果`M`方法像下面这样声明：

```C#
private static Int32 M(IComparable t) {
    ...
}
```

那么`x`要传给`M`就必须装箱。

C# 编译器为接口约束生成特殊 IL 指令，导致直接在值类型上调用接口方法而不装箱。不用接口约束便没有其他办法让 C# 编译器生成这些 IL 指令，如此一来，在值类型上调用接口方法总是发生装箱。一个例外是如果值类型实现了一个接口方法，在值类型的实例上调用这个方法不会造成值类型的实例装箱。

## <a name="13_8">13.8 实现多个具有相同方法名和签名的接口</a>

定义实现多个接口的类型时，这些接口可能定义了具有相同名称和签名的方法。例如，假定有以下两个接口：

```C#
private interface IWindow {
    Object GetMenu();
}

public Interface IRestaurant {
    Object GetMenu();
}
```

要定义实现这两个接口的类型，必须使用“显式接口方法实现”来实现这个类型的成员，如下所示：

```C#
// 这个类型派生自 System.Object
// 并实现了 IWindow 和 IRestaurant 接口
public sealed class MarioPizzeria : IWindow, IRestaurant {

    // 这是 IWindow 的 GetMenu 方法的实现
    Object IWindow.GetMenu() { ... }

    // 这是 IRestaurant 的 GetMenu 方法的实现
    Object IRestaurant.GetMenu() { ... }

    // 这个 GetMenu 方法是可选的，与接口无关
    public Object GetMenu() { ... }
}
```

由于这个类型必须实现多个接口的 `GetMenu` 方法，所以要告诉 C# 编译器每个`GetMenu`方法对应的是哪个接口的实现。

代码在使用`MarioPizzeria` 对象时必须将其转换为具体的接口才能调用所需的方法。例如：

```C#
MarioPizzeria mp = new MarioPizzeria();

// 这行代码调用 MarioPizzeria 的公共 GetMenu 方法
mp.GetMenu();

// 以下代码调用 MarioPizzeria 的 IWindow.GetMenu 方法
IWindow window = mp;
window.GetMenu();

// 以下代码调用 MarioPizzeria 的 IRestaurant.GetMenu 方法
IRestaurant restaurant = mp;
restaurant.GetMenu();
```

## <a name="13_9">13.9 用显式接口方法实现来增强编译时类型安全性</a>

接口很好用，它们定义了在类型之间进行沟通的标准方式。前面曾讨论了泛型接口，讨论了它们如何增强编译时的类型安全性和减少装箱操作。遗憾的是，有时由于不存在泛型版本，所以仍需实现非泛型接口。接口的任何方法接受`System.Object`类型的参数或返回`System.Object`类型的值，就会失去编译时的类型安全性，装箱也会发生。本节将介绍如何用“显式接口方法实现”(EIMI)在某种程度上改善这个局面。

下面是极其常用的`IComparable`接口：

```C#
public interface IComparable {
    Int32 CompareTo(Object other);
}
```

该接口定义了接受一个 `System.Object` 参数的方法。可以像下面这样定义实现了接口的类型：

```C#
internal struct SomeValueType : IComparable {
    private SomeValueType(Int32 x) { m_x = x; }
    public Int32 CompareTo(Object other) {
        return (m_x - ((SomeValueType) other).m_x);
    }
}
```

可用`SomeValueType`写下面这样的代码：

```C#
public static void Main() {
    SomeValueType v = new SomeValueType(0);
    Object o = new Object();
    Int32 n = v.CompareTo(v);       // 不希望的装箱操作
    n = v.CompareTo(o);             // InvalidCastException 异常
}
```

上述代码存在两个问题。

* **不希望的装箱操作**  
  `v`作为实参传给`CompareTo`方法时必须装箱，因为 `CompareTo`期待的是一个`Object`。

* **缺乏类型安全性**  
  代码能通过编译，但 `CompareTo` 方法内部试图将 `o` 转换为 `SomeValueType` 时抛出 `InvalidCastException` 异常。

这两个问题都可以用 EIMI 解决。下面是 `SomeValueType` 的修改版本，这次添加了一个 EIMI：

```C#
internal struct SomeValueType : IComparable {
    private Int32 m_x;
    public SomeValueType(Int32 x) { m_x = x; }

    public Int32 CompareTo(SomeValueType other) {
        return (m_x - other.m_x);
    }

    // 注意以下代码没有指定 pulbic/private 可访问性
    Int32 IComparable.CompareTo(Object other) {
        return CompareTo((SomeValueType) other);
    }
}
```

注意新版本的几处改动。现在有两个 `CompareTo` 方法。第一个 `CompareTo` 方法不是获取一个 `Object` 作为参数，而是获取一个 `SomeValueType`了，所以用于强制类型转换的代码被去掉了。修改了第一个`CompareTo`方法使其变得类型安全之后，`SomeValueType`还必须实现一个`CompareTo`方法来满足`IComparable`的协定。这正是第二个`IComparable.CompareTo`方法的作用，它是一个EIMI。

经过这两处改动之后，就获得了编译时的类型安全性，而且不会发生装箱：

```C#
public static void Main() {
    SomeValueType v = new SomeValueType(0);
    Object o = new Object();
    Int32 n = v.CompareTo(v);   // 不发生装箱
    n = v.CompareTo(o);         // 编译时错误
}
```

不过，定义接口类型的变量会再次失去编译时的类型安全性，而且会再次发生装箱：

```C#
public static void Main() {
    SomeValueType v = new SomeValueType(0);
    IComparable c = v;          // 装箱！

    Object o = new Object();
    Int32 n = c.CompareTo(v);  // 不希望的装箱操作
    n = c.CompareTo(o);        // InvalidCastException 异常    
}
```

事实上，如本章前面所述，将值类型的实例换换为接口类型时，CLR 必须对值类型的实例进行装箱。因此，前面的 `Main` 方法中会发生两次装箱。

实现 `IConvertible`，`ICollection`，`IList`和`IDictionary`等接口时 EIMI 很有用。可利用它为这些接口的方法创建类型安全的版本，并减少值类型的装箱。

## <a name="#13_10">13.10 谨慎使用显式接口方法实现</a>

使用 EIMI 也可能造成一些严重后果，所以应该尽量避免使用 EIMI。幸好，泛型接口可帮助我们在大多数时候避免使用 EIMI。但有时(比如实现具有相同名称和签名的两个接口方法时)仍然需要它们。EIMI 最主要的问题如下。

* 没有文档解释类型具体如何实现一个 `EIMI` 方法，也没有`Microsoft Visual Studio`“智能感知”支持。

* 值类型的实例在转换成接口时装箱。

* `EIMI`不能由派生类型调用。

下面详细讨论这些问题。

文档在列出一个类型的方法时，会列出显式接口方法实现(EIMI)，但没有提供类型特有的帮助，只有接口方法的常规性帮助。例如，`Int32`类型的文档只是说它实现了`IConvertible`接口的所有方法。能做到这一步已经不错，它使开发人员知道存在这些方法。但也使开发人员感到困惑，因为不能直接在一个`Int32`上调用一个`IConvertible`方法。例如，下面的代码无法编译：

```C#
public static void Main() {
    Int32 x = 5;
    Single s = x.ToSingle(null);        // 试图调用一个 IConvertible 方法
}
```

编译这个方法时，C# 编译器会报告以下消息：`error CS0117：“int”不包含“ToSingle”的定义`。这个错误信息使开发人员感到困惑，因为它明显是说`Int32`类型没有定义`ToSingle`方法，但实际上定义了。

要在一个`Int32`上调用`ToSingle`，首先必须将其转换为`IConvertible`，如下所示：

```C#
public static void Main() {
    Int32 x = 5;
    Single s = ((IConvertible) x).ToSingle(null);
}
```

对类型转换的要求不明确，而许多开发人员自己看不出来问题出在哪里。还有一个更让人烦恼的问题：`Int32`值类型转换为`IConvertible`会发生装箱，既浪费内存，又损害性能。这是本节开头提到的EIMI存在的第二个问题。

EIMI的第三个也可能是最大的问题是，它们不能被派生类调用。下面是一个例子：

```C#
internal class Base : IComparable {

    // 显式接口方法实现
    Int32 IComparable.CompareTo(Object o) {
        Console.WriteLine("Base's CompareTo");
        return 0;
    }
}


internal sealed class Derived : Base,IComparable {

    // 一个公共方法，也是接口的实现
    public Int32 CompareTo(Object o) {
        Console.WriteLine("Derived's CompareTo");

        // 试图调用基类的 EIMI 导致编译错误：
        // error CS0117：“Base” 不包含 “CompareTo” 的定义
        base.CompareTo(o);
        return 0;
    }
}
```

在`Derived`的`CompareTo`方法中调用 `base.CompareTo`导致C#编译器报错。现在的问题是，`Base`类没有提供一个可供调用的或受保护`CompareTo`方法，它提供的是一个只能用`IComparable`类型的变量来调用的`CompareTo`方法。可将`Derived`的`CompareTo`方法修改成下面这样：

```C#
// 一个公共方法，也是接口的实现
public Int32 CompareTo(Object o) {
    Console.WriteLine("Derived's CompareTo");

    // 试图调用基类的 EIMI 导致无穷递归
    IComparable c = this;
    c.CompareTo(o);

    return 0;
}
```

这个版本将`this`转换成`IComparable`变量`c`，然后用`c`调用`CompareTo`。但`Derived`的公共`CompareTo`方法充当了`Derived`的`IComparable.CompareTo`方法的实现，所以造成了无穷递归。这可以通过声明没有`IComparable`接口的`Derived`类来解决：

`internal sealed class Derived : Base /*, IComparable */ { ... }`  

现在，前面的`CompareTo`方法将调用`Base`中的`CompareTo`方法。但有时不能因为想在派生类中实现接口方法就将接口从类型中删除。解决这个问题的最佳方法是在基类中除了提供一个被选为显式实现的接口方法，还要提供一个虚方法。然后`Derived`类可以重写虚方法。下面展示了如何正确定义`Base`类和`Derived`类：

```C#
internal class Base : IComparable {
    
    // 显式接口方法实现(EIMI)
    Int32 IComparable.CompareTo(Object o) {
        Console.WriteLine("Base's IComparable.CompareTo");
        return CompareTo(o);        // 调用虚方法
    }

    // 用于派生类的虚方法(该方法可为任意名称)
    public virtual Int32 CompareTo(Object o) {
        Console.WriteLine("Base's virtual CompareTo");
        return 0;
    }
}

internal sealed class Derived : Base, IComparable {

    // 一个公共方法，也是接口的实现
    public override Int32 CompareTo(Object o) {
        Console.WriteLine("Derived's CompareTo");

        // 现在可以调用 Base 的虚方法
        return base.CompareTo(o);
    }
}
```

注意，这里是讲虚方法定义成公共方法，但有时可能需要定义成受保护方法。把方法定义为受保护(而不是公共)是可以的，但必须进行另一些小的改动。我们的讨论清楚证明了务必谨慎使用EIMI。许多开发人员在最初接触 EIMI 时，认为 EIMI 非常”酷“，于是开始肆无忌惮地使用。千万不要这样做！.NET在某些情况下确实有用，但应该尽量避免使用，因为它们导致类型变得不好用。

## <a name="13_11">13.11 设计：基类还是接口</a>

经常有人问：”英爱设计基类还是接口？“ 这个问题不能一概而论，以下设计规范或许能帮你理清思路：

* **IS-A对比CAN-DO关系<sup>①</sup>**  
  类型只能继承一个实现，如果派生类型和基类型建立不起 IS-A 关系，就不能用基类而用接口。接口意味着 CAN-DO 关系。如果多种对象类型都”能“做某事，就为它们创建接口。例如，一个类型能将自己的实例转换为另一个类型(`IConvertible`)，一个类型序列化自己的实例(`ISerializable`)。注意，值类型必须从`System.ValueType`派生，所以不能从任意的基类派生。这时必须使用CAN-DO关系并定义接口。

> ① IS-A 是指”属于“，例如，汽车属于交通工具；CAN-DO 是指”能做某事“，例如，一个类型能将自己的实例转换为另一个类型。 ——译注

* **易用性**  
  对于开发人员，定义从基类派生的新类型通常比实现接口的所有方法容易得多。基类型可提供大量功能，所以派生类型可能只需稍做改动。而提供接口的话，新类型必须实现所有成员。

* **一致性实现**  
  无论接口协定(contract)订立得有多好，都无法保证所有人百分之百正确实现它。事实上，COM颇受该问题之累，导致有的 COM 对象只能正常用于Microsoft Office Word 或 Microsoft Internet Explorer。而如果为基类型提供良好的默认实现，那么一开始得到的就是能正常工作并经过良好测试的类型。以后根据需要修改就可以了。

* **版本控制**  
  向基类型添加一个方法，派生类型将继承新方法。一开始使用的就是一个能正常工作的类型，用户的源代码甚至不需要重新编译。而向接口添加新成员，会强迫接口的继承者更改其源代码并重新编译。

FCL 中涉及数据流处理(streaming data)的类采用的是实现继承方案<sup>②</sup>。`System.IO.Stream`是抽象基类，提供了包括`Read`和`Write`在内的一组方法。其他类(`System.IO.FileStream`，`System.IO.MemoryStream`和`System.Net.Sockets.NetworkStream`)都从`Stream`派生。在这三个类中，每一个和`Stream`类都是 IS-A 关系，这使具体类<sup>③</sup>的实现变得更容易。例如，派生类只需实现同步 I/O 操作，异步 I/O 操作已经从`Stream`基类继承了。

> ② 即继承基类的实现。 —— 译注  
> ③ 对应于”抽象类“。 —— 译注

必须承认，为流类(`XXXStream`)选择继承的理由不是特别充分，因为`Stream`基类实际只提供了很少的实现。那么就以 Microsoft Windows 窗体控件类为例好了。`Button`，`CheckBox`，`ListBox` 和所有其他窗体控件都从`System.Windows.Forms.Control`派生。`Control`实现了大量代码，各种控件类简单继承一下即可正常工作。这时选择继承应该没有疑问了吧？

相反，Microsoft 采用基于接口的方式来设计FCL中的集合。`System.Collections.Generic`命名空间定义了几个与集合有关的接口：`IEnumerable<out T>`，`ICollection<T>`，`IList<T>`和`IDictionary<TKey, Tvalue>`。然后，Microsoft 提供了大量类来实现这些接口组合，包括`List<T>`，`Dictionary<Tkey, TValue>`，`Queue<T>`和`Stack<T>`等等。设计者在类和接口之间选择 CAN-DO 关系，因为不同集合类的实现迥然有异。换句话说，`List<T>`，`Dictionary<Tkey, Tvalue>`和`Queue<T>`之间没有多少能共享的代码。

不过，这些集合类提供的操作相当一致。例如，都维护了一组可枚举的元素，而且都允许添加和删除元素。假定有一个对象引用，对象的类型实现了`IList<T>`接口，就可在不需要知道集合准确类型的前提下插入、删除和搜索元素。这个机制太强大了！

最后要说的是，两件事情实际能同时做：定义接口，*同时*提供实现该接口的基类。例如，FCL定义了`ICOmparable<in T>`接口，任何类型都可选择实现该接口。此外，FCL提供了抽象基类`Comparer<T>`，它实现了该接口，同时为非泛型`IComparer`的`Compare`方法提供了默认实现。接口定义和基类同时存在带来了很大的灵活性，开发人员可根据需要选择其中一个。
