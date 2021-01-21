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

## <a name="13_6">泛型接口</a>

C# 和 CLR 所支持的泛型接口为开发人员提供了许多非常出色的功能。本节要讨论泛型接口提供的一些好处。
