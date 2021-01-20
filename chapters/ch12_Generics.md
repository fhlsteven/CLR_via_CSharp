# 第 12 章 泛型

本章内容

* <a href="#12_1">FCL 中的泛型</a>
* <a href="#12_2">泛型基础结构</a>
* <a href="#12_3">泛型接口</a>
* <a href="#12_4">泛型委托</a>
* <a href="#12_5">委托和接口的逆变和协变泛型类型实参</a>
* <a href="#12_6">泛型方法</a>
* <a href="#12_7">泛型和其他成员</a>
* <a href="#12_8">可验证性和约束</a>

熟悉面向对象编程的开发人员都深谙这种编程方式的好处。其中一个好处是“代码重用”，它极大提高了开发效率。也就是说，可以派生出一个类，让它继承基类的所有能力。派生类只需重写虚方法，或添加一些新方法，就可定制派生类的行为，使之满足开发人员的需求。**泛型**(generic)是 CLR 和编程语言的一种特殊机制，它支持另一种形式的代码重用，即“算法重用”。

简单地说，开发人员先定义好算法，比如排序、搜索、交换、比较或者转换等。但是，定义算法的开发人员并不设定该算法要操作什么数据类型；该算法可广泛地应用于不同类型的对象。然后，另一个开发人员只要指定了算法要操作的具体数据类型，就可以开始使用这个算法了。例如，一个排序算法可操作`Int32` 和 `String` 等类型的对象，而一个比较算法可操作`DateTime`和`Version`等类型的对象。

大多数算法都封装在一个类型中，CLR 允许创建泛型引用类型和泛型值类型，但不允许创建泛型枚举类型。此外，CLR 还允许创建泛型接口和泛型委托。方法偶尔也封装有用的算法，所以 CLR 允许在引用类型、值类型或接口中定义泛型方法。

先来看一个简单的例子。Framework 类库(Framework Class Library，FCL)定义了一个泛型列表算法，它知道如何管理对象集合。泛型算法没有设定对象的数据类型。要在使用这个泛型列表算法时指定具体数据类型。

封装了泛型列表算法的FCL类称为 `List<T>`(读作 List of Tee)。这个类是在 `System.Collections.Generic` 命名空间中定义的。下面展示了类定义(代码被大幅简化)：

```C#
[Serializable]
public class List<T> : IList<T>, ICollection<T>, IEnumerable<T>,
 IList, ICollection, IEnumerable {
 public List();
 public void Add(T item);
 public Int32 BinarySearch(T item);
 public void Clear();
 public Boolean Contains(T item);
 public Int32 IndexOf(T item);
 public Boolean Remove(T item);
 public void Sort();
 public void Sort(IComparer<T> comparer);
 public void Sort(Comparison<T> comparison);
 public T[] ToArray();
 public Int32 Count { get; }
 public T this[Int32 index] { get; set; }
}
```

泛型 `List` 类的设计者紧接在类名后添加了一个`<T>`，表明它操作的是一个未指定的数据类型。定义泛型类型或方法时，为类型指定的任何变量(比如`T`)都称为**类型参数**(type parameter)。`T`是变量名，源代码能使用数据类型的任何地方都能使用`T`。例如，在`List`类定义中，`T`被用于方法参数(`Add`方法接受一个`T`类型的参数)和返回值(`ToArray`方法返回`T`类型的一维数组)。另一个例子是索引器方法(在C#中称为`this`)。索引器有一个`get`访问器方法，它返回`T`类型的值；一个`set`访问器方法，它接受`T`类型的参数。由于凡是能指定一个数据类型的地方都能使用`T`变量，所以在方法内部定义一个局部变量时，或者在类型中定义字段时，也可以使用`T`。

> 注意 根据 Microsoft 的设计原则，泛型参数变量要么称为`T`，要么至少以大写`T`开头(如`TKey`和`TValue`)。大写`T`代表类型(Type)，就像大写`I`代表接口(interface)一样，比如`IComparable`。

定义好泛型`List<T>`类型之后，其他开发人员为了使用这个泛型算法，要指定由算法操作的具体数据类型。使用泛型类型或方法时指定的具体数据类型称为**类型实参**(type argument)。例如，开发人员可指定一个`DateTime`类型实参来使用`List`算法。以下代码对此进行了演示：

```C#
private static void SomeMethod() {
    // 构造一个 List 来操作 DateTime 对象
    List<DateTime> dtList = new List<DateTime>();

    // 向列表添加一个 DateTime 对象
    dtList.Add(DateTime.Now);       // 不进行装箱

    // 向列表添加另一个 DateTime 对象
    dtList.Add(DateTime.MinValue);  // 不进行装箱

    // 尝试向列表中添加一个 String 对象
    dtList.Add("1/1/2004");         // 编译时错误

    // 从列表提取一个 DateTime 对象
    DateTime dt = dtList[0];        // 不需要转型
}
```

从以上代码可看出泛型为开发人员提供了以下优势。

* **源代码保护**  
  使用泛型算法开发人员不需要访问算法的源代码。然而，使用 C++ 模板的泛型技术时，算法的源代码必须提供给准备使用算法的用户。

* **类型安全**  
  将泛型算法应用于一个具体的类型时，编译器和 CLR 能理解开发人员的意图，并保证只有与指定数据类型兼容的对象才能用于算法。试图使用不兼容类型的对象会造成编译时错误，或在运行时抛出异常。在上例中，试图将 `String`对象传给`Add`方法造成编译器报错。
  
* **更清晰的代码**  
  由于编译器强制类型安全性，所以减少了源代码中必须进行的强制类型转换次数，使代码更容易编写和维护。在`SomeMethod`的最后一行，开发人员不需要进行(`DateTime`)强制类型转换就能将索引器的结果(查询索引位置0处的元素)存储到`dt`变量中。
  
* **更佳的性能**  
  没有泛型的时候，要想定义常规化的算法，它的所有成员都要定义成操作`Object`数据类型。要用这个算法来操作值类型的实例，CLR必须在调用算法的成员之前对值类型实例进行装箱。正如第 5 章“基元类型、引用类型和值类型”讨论的那样，装箱造成在托管堆上进行内存分配，造成更频繁的垃圾回收，从而损害应用程序的性能。由于现在能创建一个泛型算法来操作一种具体的类型，所以值类型的实例能以传值方式传递，CLR不再需要执行任何装箱操作。此外，由于不再需要进行强制类型转换(参见上一条)，所以CLR无需验证这种转型是否类型安全，这同样提高了代码的运行速度。
  
为了理解泛型的性能优势，我写了一个程序来比较泛型`List`算法和FCL的非泛型`ArrayList`算法的性能。我打算同时使用值类型的对象和引用类型的对象来测试这两个算法的性能。下面是程序本身：

```C#
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

public static class Program {
    public static void Main() {
        ValueTypePerfTest();
        ReferenceTypePerTest();
    }

    private static void ValueTypePerfTest() {
        const Int32 count = 100000000;

        using(new OperationTimer("List<Int32>")) {
            List<Int32> l = new List<int>();
            for (Int32 n = 0; n < count; n++) {
                l.Add(n);       // 不发生装箱
                Int32 x = l[n]; // 不发生拆箱
            }
            l = null;       // 确保进行垃圾回收
        }

        using (new OperationTimer("ArrayList of Int32")) {
            ArrayList a = new ArrayList();
            for (Int32 n = 0; n < count; n++) {
                a.Add(n);               // 发生装箱
                Int32 x = (Int32)a[n];  // 发生拆箱
            }
            a = null;                   // 确保进行垃圾回收
        }
    }

    private static void ReferenceTypePerTest() {
        const Int32 count = 100000000;

        using(new OperationTimer("List<String>")) {
            List<String> l = new List<string>();
            for (Int32 n = 0; n < count; n++) {
                l.Add("X");                 // 复制引用
                String x = (String) l[n];   // 复制引用
            }
            l = null;                       // 确保进行垃圾回收
        }

        using(new OperationTimer("ArrayList of String")) {
            ArrayList a = new ArrayList();
            for (Int32 n = 0; n < count; n++) {
                a.Add("X");                 // 复制引用
                String x = (String)a[n];    // 检查强制类型转换 & 复制引用
            }
            a = null;       // 确保进行垃圾回收
        }
    }

}

// 这个类用于进行运算性能计时
internal sealed class OperationTimer : IDisposable {
    private Stopwatch m_stopwatch;
    private String m_text;
    private Int32 m_collectionCount;

    public OperationTimer(String text) {
        PrepareForOperation();

        m_text = text;
        m_collectionCount = GC.CollectionCount(0);

        // 这应该是方法的最后一个语句，从而最大程度保证计时的准确性
        m_stopwatch = Stopwatch.StartNew();
    }

    public void Dispose() {
        Console.WriteLine("{0} (GCs={1, 3}) {2}", (m_stopwatch.Elapsed), 
            GC.CollectionCount(0) - m_collectionCount, m_text);
    }

    private static void PrepareForOperation() {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
```

在我的机器上编译并运行这个程序(打开优化开关)，得到以下输出：

```cmd
00:00:01.1394268 (GCs=  8) List<Int32>
00:00:11.9487660 (GCs=401) ArrayList of Int32
00:00:01.6982451 (GCs=  1) List<String>
00:00:01.8257693 (GCs=  0) ArrayList of String
```

这证明在操作 `Int32` 类型时，泛型 `List` 算法比非泛型 `ArrayList` 算法快得多。1.1 秒和 11秒，约7倍的差异！此外，用 `ArrayList` 操作值类型(`Int32`)会造成大量装箱，最终要进行 390 次垃圾回收。对应地，`List` 算法只需进行 6 次。

不过，用引用类型来测试，差异就没那么明显了，时间和垃圾回收次数基本相同。所以，泛型 `List` 算法在这里表面上没什么优势。但要注意的是，泛型算法的优势还包括更清晰的代码和编译时类型安全。所以，虽然性能提升不是很明显，但泛型算法的其他优势也不容忽视。

> 注意 应该意识到，首次为特定数据类型调用方法时，CLR 都会为这个方法生成本机代码。这会增大应用程序的工作集(working set)大小，从而损害性能。12.2“泛型基础结构”将进一步探讨这个问题。

## <a name="12_1">12.1 FCL 中的泛型</a>

泛型最明显的应用就是集合类。FCL 在 `System.Collections.Generic` 和 `System.Collections.ObjectModel` 命名空间中提供了多个泛型集合类。`System.Collections.Concurrent` 命名空间则提供了线程安全的泛型集合类。不建议使用非泛型集合类。这是出于几方面的考虑。首先，使用非泛型集合类，无法像使用泛型集合类那样获得类型安全性、更清晰的代码以及更佳的性能。其次，泛型类具有比非泛型类更好的对象模型。例如，虚方法数量显著变少，性能更好。另外，泛型集合类增添了一些新成员，为开发人员提供了新的功能。

集合类实现了许多接口，放入集合中的对象可实现接口来执行排序和搜索等操作。FCL 包含许多泛型接口定义，所以使用接口时也能享受到泛型带来的好处。常用接口在`System.Collections.Generic`命名空间中提供。

新的泛型接口不是为了替代旧的非泛型接口。许多时候两者都要使用(为了向后兼容)。例如，如果`List<T>`类只实现了`IList<T>`接口，代码就不能将一个`List<DateTime>`对象当作一个`IList`来处理。

还要注意，`System.Array`类(所有数组类型的基类)提供了大量静态泛型方法，比如`AsReadOnly`，`BinarySearch`，`ConcertAll`，`Exists`，`Find`，`FindAll`，`FindIndex`，`FindLast`，`FindLastIndex`，`ForEach`，`IndexOf`，`LastIndexOf`，`Resize`，`Sort`和`TrueForAll`等。下面展示了部分方法：

```C#
public abstract class Array : ICloneable, IList, ICollection, IEnumerable, IStructuralComparable, IStructuralEquatable {
    public static void Sort<T>(T[] array);
    public static void Sort<T>(T[] array, IComparer<T> comparer);

    public static Int32 BinarySearch<T>(T[] array, T value);
    public static Int32 BinarySearch<T>(T[] array, T value, IComparer<T> comparer);
    ...
}
```

以下代码展示了如何使用其中一些方法：

```C#
public static void Main() {
    // 创建并初始化字节数组
    Byte[] byteArray = new byte[] { 5, 1, 4, 2, 3 };

    // 调用 Byte[] 排序算法
    Array.Sort<Byte>(byteArray);

    // 调用 Byte[] 二分搜索算法
    Int32 i = Array.BinarySearch<Byte>(byteArray, 1);
    Console.WriteLine(i);   // 显示“0”
}
```

## <a name="12_2">12.2 泛型基础结构</a>

泛型在 CLR 2.0 中加入泛型，许多人花费了大量时间来完成这个大型任务。具体地说，为了使泛型能够工作，Microsoft 必须完成以下工作。

* 创建新的 IL 指令，使之能够识别类型实参。
* 修改现有元数据表的格式，以便表示具有泛型参数的类型名称和方法。
* 修改各种编程语言(C#，Microsoft Visual Basic .NET等)来支持新语法，允许开发人员定义和引用泛型类型和方法。
* 修改编译器，使之能生成新的 IL 指令和修改的元数据格式。
* 修改 JIT 编译器，以便处理新的支持类型实参的 IL 指令来生成正确的本机代码。
* 创建新的反射成员，使开发人员能查询类型和成员，以判断它们是否具有泛型参数。另外，还必须定义新的反射成员，使开发人员能在运行时创建泛型类型和方法定义。
* 修改调试器以显示和操纵泛型类型、成员、字段以及局部变量。
* 修改 Microsoft Visual Studio 的 “智能感知”(IntelliSense)功能。将泛型类型或方法应用于特定数据类型时能显示成员的原型。

现在让我们花些时间讨论 CLR 内部如何处理泛型。这一部分的知识可能影响你构建和设计泛型算法的方式。另外，还可能影响你是否使用一个现有泛型算法的决策。

### 12.2.1 开发类型和封闭类型

贯穿全书，我讨论了 CLR 如何为应用程序使用的各种类型创建称为**类型对象**(type object)的内部数据结构。具有泛型类型参数的类型仍然是类型，CLR 同样会为它创建内部的类型对象。这一点适合应用类型(类)、值类型(结构)、接口类型和委托类型。然而，具有泛型类型参数的类型称为**开放类型**，CLR禁止构造开放类型的任何实例。这类似于 CLR 禁止构造接口类型的实例。

代码引用泛型类型时可指定一组泛型类型实参。为所有类型参数都传递了实际的数据类型，类型就成为**封闭类型**。CLR 允许构造封闭类型的实例。然而，代码引用泛型类型的时候，可能留下一些泛型类型实参未指定。这会在 CLR 中创建新的开放类型对象，而且不能创建该类型的实例。以下代码更清楚地说明了这一点：

```C#
using System;
using System.Collections.Generic;

// 一个部分指定的开放类型
internal sealed class DictionaryStringKey<TValue> : Dictionary<String, TValue> { }

public static class Program {
    public static void Main() {
        Object o = null;

        // Dictionary<,>是开放类型，有2个类型参数
        Type t = typeof(Dictionary<,>);

        // 尝试创建该类型的实例(失败)
        o = CreateInstance(t);
        Console.WriteLine();

        // DictionaryStringKey<>是开放类型，有 1 个类型参数
        t = typeof(DictionaryStringKey<>);

        // 尝试创建该类型的实例(失败)
        o = CreateInstance(t);
        Console.WriteLine();

        // DictionaryStringKey<Guid>是封闭类型
        t = typeof(DictionaryStringKey<Guid>);

        // 尝试创建该类型的一个实例(成功)
        o = CreateInstance(t);

        // 证明它确实能够工作
        Console.WriteLine(" 对象类型 = " + o.GetType());
    }

    private static Object CreateInstance(Type t) {
        Object o = null;
        try {
            o = Activator.CreateInstance(t);
            Console.WriteLine("已创建 {0} 的实例。", t.ToString());
        }
        catch (ArgumentException e) {
            Console.WriteLine(e.Message);
        }
        return o;
    }
}
```

编译并运行上述代码得到以下输出:

```cmd
无法创建 System.Collections.Generic.Dictionary`2[TKey,TValue] 的实例，
因为 Type.ContainsGenericParameters 为 true

无法创建 DictionaryStringKey`1[TValue] 的实例，
因为 Type.ContainsGenericParameters 为 true.

已创建 DictionaryStringKey`1[System.Guid] 的实例。
对象类型 = DictionaryStringKey`1[System.Guid]
```

可以看出，`Activator` 的 `CreateInstance` 方法会在试图构造开放类型的实例时抛出`ArgumentException`异常。注意，异常的字符串消息指明类型中仍然含有一些泛型参数。

从输出可以看出，类型名以“'”字符和一个数字结尾。数字代表类型的元数，也就是类型要求的类型参数个数。例如，`Dictionary`类的元数为 2，要求为`TKey`和`TValue`这两个类型参数指定具体类型。`DictionaryStringKey`类的元数为1，只要求为`TValue`指定具体类型。

还要注意，CLR 会在类型对象内部分配类型的静态字段(本书第 4 章“类型基础”对此进行了讨论)。因此，每个封闭类型都有自己的静态字段。换言之，假如`List<T>`定义了任何静态字段，这些字段不会在一个`List<DateTime>`和一个`List<String>`之间共享；每个封闭类型对象都有自己的静态字段。另外，假如泛型类型定义了静态构造器(参见第 8 章 “方法”)，那么针对每个封闭类型，这个构造器都会执行一次。泛型类型定义静态构造器的目的是保证传递的类型实参满足特定条件。例如，我们可以像下面这样定义只能处理么枚举类型的泛型类型：

```C#
internal sealed class GenericTypeThatRequiresAnEnum<T> {
    static GenericTypeThatRequiresAnEnum() {
        if (!typeof(T).IsEnum) {
            throw new ArgumentException("T must be an enumerated type");
        }
    }
}
```

CLR 提供了名为**约束**的功能，可以更好地指定有效的类型实参。本章稍后会详细讨论。遗憾的是，约束无法将类型实参限制为“仅枚举类型”。正是因为这个原因，所以上例需要用静态构造器来保证类型是一个枚举类型。

### 12.2.2 泛型类型和继承

泛型类型仍然是类型，所以能从其他任何类型派生。使用泛型类型并指定类型实参时，实际是在CLR中定义一个新的类型对象，新的类型对象从泛型类型派生自的那个类型派生。换言之，由于`List<T>`从`Object`派生，所以`List<String>`和`List<Guid>`也从`Object`派生。类似地，由于`DictionaryStringKey<TValue>`从`Dictionary<String, TValue>`派生，所以`DictionaryStringKey<Guid>`也从`Dictionary<String, Guid>`派生。指定类型实参不影响继承层次结构——理解这一点，有助于你判断哪些强制类型转换是允许的，哪些不允许。

假定像下面这样定义一个链表节点类：

```C#
internal sealed class Node<T> {
    public T m_data;
    public Node<T> m_next;

    public Node(T data) : this(data, null) { }

    public Node(T data, Node<T> next) {
        m_data = data; m_next = next;
    }

    public override string ToString() {
        return m_data.ToString() + ((m_next != null) ? m_next.ToString() : String.Empty);
    }
}
```

那么可以写代码来构造链表，例如：

```C#
private static void SameDataLinkedList() {
    Node<Char> head = new Node<Char>('C');
    head = new Node<Char>('B', head);
    head = new Node<Char>('A', head);
    Console.WriteLine(head.ToString());     // 显示“ABC”
}
```

在这个 `Node` 类中，对于`m_next`字段引用的另一个节点来说，它的`m_data`字段必须包含相同的数据类型。这意味着在链表包含的节点中，所有数据项都必须具有相同的类型(或派生类型)。例如，不能使用 `Node` 类来创建这样一个链表：其中一个元素包含`Char`值，另一个包含`DateTime`值，另一个元数则包含`String`值。当然，如果到处都用`Node<Object>`，那么确实可以做到，但会丧失编译时类型安全性，而且值类型会被拆箱。

所以，更好的办法是定义非泛型`Node`基类，再定义泛型`TypedNode`类(用`Node`类作为基类)。这样就可以创建一个链表，其中每个节点都可以是一种具体的数据类型(不能是`Object`)，同时获得编译时的类型安全性，并防止值类型装箱。下面是新的类型定义：

```C#
internal class Node {
    protected Node m_next;

    public Node(Node next) {
        m_next = next;
    }
}

internal sealed class TypeNode<T> : Node {
    public T m_data;

    public TypeNode(T data) : this(data, null) {
    }

    public TypeNode(T data, Node next) : base(next) {
        m_data = data;
    }

    public override string ToString() {
        return m_data.ToString() + ((m_next != null) ? m_next.ToString() : String.Empty);
    }
}
```

现在可以写代码来创建一个链表，其中每个节点都是不同的数据类型。例如：

```C#
private static void DifferentDataLinkedList() {
    Node head = new TypeNode<Char>('.');
    head = new TypeNode<DateTime>(DateTime.Now, head);
    head = new TypeNode<String>("Today is ", head);
    Console.WriteLine(head.ToString());
}
```

### 12.2.3 泛型类型同一性

泛型语法有时会将开发人员弄糊涂，因为源代码中可能散布着大量“<”和“>”符合，这有损可读性。为了对语法进行增强，有的开发人员定义了一个新的非泛型类类型，它从一个泛型类型派生，并指定了所有类型实参。例如，为了简化下面这样的代码：

`List<DateTime> dtl = new List<DateTime>();`  

一些开发人员可能首先定义下面这样的类：

```C#
internal sealed class DateTimeList : List<DateTime> {
    // 这里无需放入任何代码！
}
```

然后就可以简化创建列表的代码(没有了“<” 和 “>” 符号)：

`DateTimeList dt1 = new DateTimeList();`

这样做表面上是方便了(尤其是要为参数、局部变量和字段使用新类型的时候)，但是，绝对不要单纯出于增强源码可读性的目的来定义一个新类。这样会丧失类型同一性(identity)和相等性(equivalence)，如以下代码所示：

`Boolean sameType = (typeof(List<DateTime>) == typeof(DateTimeList));`

上述代码运行时，`sameType`会被初始化为`false`，因为比较的是两个不同类型的对象。这也意味着如果方法的原型接受一个`DateTimeList`，那么不可以将一个 `List<DateTime>` 传给它。然而，如果方法的原型接受一个 `List<DateTime>`，那么可以将一个`DateTimeList`传给它，因为`DateTimeList`从`List<DateTime>`派生。开发人员很容易被所有这一切搞糊涂。

幸好，C# 允许使用简化的语法来引用泛型封闭类型，同时不会影响类型的相等性。这个语法要求在源文件顶部使用传统的 `using`指令，例如：

`using DateTimeList = System.Collections.Generic.List<System.DateTime>;`  

`using`指令实际定义的是名为`DateTimeList`的符号。代码编译时，编译器将代码中出现的所有`DateTimeList`替换成`System.Collections.Generic.List<System.DateTime>`。这样就允许开发人员使用简化的语法，同时不影响代码的实际含义。所以，类型的同一性和相等性得到了维护。现在，执行下面这行代码时，`sameType`会被初始化为`true`：

`Boolean sameType = (typeof(List<DateTime>) == typeof(DateTimeList));`  

另外，可以利用C#的“隐式类型局部变量”功能，让编译器根据表达式的类型来推断方法的局部变量的类型：

```C#
using System;
using System.Collections.Generic;
...
internal sealed class SomeType {
    private static void SomeMethod () {

        // 编译器推断出 dt1 的类型
        // 是 System.Collections.Generic.List<System.DateTime>
        var dt1 = new List<DateTime>();
        ...
    }
}
```

### 12.2.4 代码爆炸

使用泛型类型参数的方法在进行 JIT 编译时，CLR 获取方法的 IL，用指定的类型实参替换，然后创建恰当的本机代码(这些代码为操作指定数据类型“量身定制”)。这正是你希望的，也是泛型的重要特点。但这样做有一个缺点：CLR 要为每种不同的方法/类型组合生成本机代码。我们将这个现象称为**代码爆炸**。它可能造成应用程序的工作集显著增大，从而损害性能。

幸好，CLR 内建了一些优化措施能缓解代码爆炸。首先，假如为特定的类型实参调用了一个方法，以后再用相同的类型实参调用这个方法，CLR 只会为这个方法/类型组合编译一次代码。所以，如果一个程序集使用`List<DateTime>`，一个完全不同的程序集(加载到同一个 AppDomain 中)也使用`List<DateTime>`编译一次方法。这样就显著缓解了代码爆炸。

CLR 还有另一个优化，它认为所有引用类型实参都完全相同，所以代码能够共享。例如，CLR 为 `List<String>`的方法编译的代码可直接用于`List<Stream>`的方法，因为`String`和`Stream`均为引用类型。事实上，对于任何引用类型，都会使用相同的代码。CLR 之所以能执行这个优化，是因为所有引用类型的实参或变量实际只是指向堆上对象的指针(32 位 Windows 系统上是 32 位指针；64 位 Windows 系统上是 64 为指针)，而所有对象指针都以相同方式操纵。

但是，假如某个类型实参是值类型，CLR 就必须专门为那个值类型生成本机代码。这是因为值类型的大小不定。即使两个值类型大小一样(比如 `Int32`和`UInt32`，两者都是32位)，CLR仍然无法共享代码，因为可能要用不同的本机 CPU 指令来操纵这些值。

## <a name="12_3">12.3 泛型接口</a>

显然，泛型的主要作用就是定义泛型的引用类型和值类型。然而，对泛型接口的支持对CLR来说也很重要。没有泛型接口，每次用非泛型接口(如 `IComparable`)来操纵值类型都会发生装箱，而且会失去编译时的类型安全性。这将严重制约泛型类型的应用范围。因此，CLR 提供了对泛型接口的支持。引用类型或值类型可指定类型实参实现泛型接口。也可保持类型实参的未指定状态来实现泛型接口。下面来看一些例子。

以下泛型接口定义是 FCL 的一部分(在 `System.Collections.Generic`命名空间中)：

```C#
public interface IEnumerator<T> : IDisposable, IEnumerator {
    T Current { get; }
}
```

下面的示例类型实现了上述泛型接口，而且指定了类型实参。注意，`Triangle`对象可枚举一组`Point`对象。还要注意，`Current`属性具有`Point`数据类型：

```C#
internal sealed class Triangle : IEnumerator<Point> {
    private Point[] m_vertices;

    // IEnumerator<Point> 的 Current 属性是 Point 类型
    public Point Current { get { ... } }

    ...
}
```

下例实现了相同的泛型接口，但保持类型实参的未指定状态：

```C#
internal sealed class ArrayEnumerator<T> : IEnumerator<T> {
    private T[] m_array;

    // IEnumerator<T>的 Current 属性是 T 类型
    public T Current { get { ... } }

    ...
}
```

## <a name="12_4">12.4 泛型委托</a>

CLR 支持泛型委托，目的是保证任何类型的对象都能以类型安全的方式传给回调方法。此外，泛型委托允许值类型实例在传给回调方法时不进行任何装箱。第 17 章“委托”会讲到，委托实际只是提供了4个方法的一个类定义。4个方法包括一个构造器、一个`Invoke`方法，一个`BeginInvoke`方法和一个`EndInvoke`方法。如果定义的委托类型指定了类型参数，编译器会定义委托类的方法，用指定的类型参数替换方法的参数类型和返回值类型。

例如，假定像下面这样定义泛型委托：

`public delegate TReturn CallMe<TReturn, TKey, TValue>(TKey key, TValue value);`

编译器会将它转换成如下所示的类：

```C#
public sealed class CallMe<TReturn, TKey, TValue> : MulticastDelegate {
    public CallMe(Object object, IntPtr method);
    public virtual TReturn Invoke(TKey key, TValue value);
    public virtual IAsyncResult BeginInvoke(TKey key, TValue value, AsyncCallback callback, Object object);
    public virtual TReturn EndInvoke(IAsyncResult result);
}
```

> 注意 建议尽量使用 FCL 预定义的泛型 `Action` 和 `Func` 委托。这些委托的详情将在 17.6 节“委托定义不要太多(泛型委托)”讲述。

## <a name="12_5">12.5 委托和接口的逆变和协变泛型类型实参</a>

委托的每个泛型类型参数都可标记为协变量或逆变量<sup>①</sup>。利用这个功能，可将泛型委托类型的变量转换为相同的委托类型(但泛型参数类型不同)。泛型类型参数可以是以下任何一种形式。
> ① 本书采用目前约定俗称的译法：covariant=协变量， contravariant=逆变量；covariance=协变性，contravariance=逆变性。另外， variance=可变性。至于两者更详细的区别，推荐阅读 Eric Lippert 的 Covariance and contravariance in C#系列博客文章。简而言之，协变性指定返回类型的兼容性，而逆变性指定参数的兼容性。——译注

* **不变量(invariant)**  意味着泛型类型参数不能更改。到目前为止，你在本章看到的全是不变量形式的泛型类型参数。

* **逆变量(contravariant)**  意味着泛型类型参数可以从一个类更改为它的某个派生类。在 C# 是用 `in` 关键字标记逆变量形式的泛型类型参数。协变量泛型类型参数只出现在输入位置，比如作为方法的参数。

* **协变量(covariant)**  意味着泛型类型参数可以从一个类更改为它的某个基类。C#是用`out`关键字标记协变量形式的泛型类型参数。协变量泛型类型参数只能出现在输出位置，比如作为方法的返回类型。

例如，假定存在以下委托类型定义(顺便说一下，它真的存在)：

`public delegate TResult Func<in T, out TResult>(T arg); `

其中，泛型类型参数`T`用`in`关键字标记，这使它成为逆变量；泛型类型参数`TResult`用`out`关键字标记，这使它成为协变量。

所以，如果像下面这样声明一个变量：

`Func<Object, ArgumentException> fn1 = null;`  

就可将它转型为另一个泛型类型参数不同的 `Func` 类型：

```C#
Func<String, Exception> fn2 = fn1;   // 不需要显式转型
Exception e = fn2("");
```

上述代码的意思是说：`fn1`变量引用一个方法，获取一个 `Object`，返回一个 `ArgumentException`。而 `fn2` 变量引用另一个方法，获取一个`String`，返回一个`Exception`。由于可将一个`String`传给期待`Object`的方法(因为 `String` 从 `Object` 派生)，而且由于可以获取返回`ArgumentException`的一个方法的结果，并将这个结果当成一个`Exception`(因为`Exception`是`ArgumentException` 的基类)，所以上述代码能正常编译，而且编译时能维持类型安全性。

> 注意 只有编译器能验证类型之间存在引用转换，这些可变性才有用。换言之，由于需要装箱，所以值类型不具有这种可变性。我个人认为正是因为存在这个限制，这些可变性的用处不是特别大。例如，假定定义以下方法：

`void ProcessCollection(IEnumerable<Object> collection) { ... }`

> 我不能在调用它时传递一个 `List<DateTime>` 对象引用，因为 `DateTime` 值类型和 `Object` 之间不存在引用转换——虽然`DateTime`是从`Object`派生的，为了解决这个问题，可像下面这样声明`ProcessCollection`:  

`void ProcessCollection<T>(IEnumerable<Object> collection) { ... }`  

> 另外，`ProcessColleciton(IEnumerable<Object> collection)`最大的好处是 JIT 编译得到的代码只有一个版本。但如果使用 `ProcessCollection<T>(IEnumerable<T> collection)`，那么只有在`T`是引用类型的前提下，才可共享同一个版本的JIT编译代码；不过，起码能在调用方法时传递一个值类型结合了。

> 另外，对于泛型类型参数，如果要将该类型的实参传给使用`out`或`ref`关键字的方法，便不允许可变性。例如，以下代码会造成编译器报告错误消息：`无效的可变性：类型参数“T”在“SomeDelegate<T>.Invoke(ref T)”中必须是不变量。现在的“T“是逆变量。`<sup>①</sup>  
`delegate void SomeDelegate<in T>(ref T t);`

> ① Visual Studio 2012/2013 实际显示的消息有点让人摸不着头脑脑：变体无效：类型参数”T“必须为对于"`SomeDelegate<T>.Invoke(ref T)`"有效的固定式。”T“为逆变。——译注

使用要获取泛型参数和返回值的委托时，建议尽量为逆变性和协变性指定`in`和`out`关键字。这样做不会有不良反应，并使你的委托能在更多的情形中使用。

和委托相似，具有泛型类型参数的接口也可将类型参数标记为逆变量和协变量。下面的示例接口有一个逆变量泛型类型参数：

```C#
public interface IEnumerator<in T> : IEnumerator {
    Boolean MoveNext();
    T Current { get; }
}
```

由于`T`是逆变量，所以以下代码可顺利编译和运行：

```C#
// 这个方法接收任意引用类型的一个 IEnumerable
Int32 Count(IEnumerable<Object> collection) { ... }

...
// 以下调用向 Count 传递一个 IEnumerable<String>
Int32 c = Count(new[] { "Grant" });
```

> 重要提示 开发人员有时会问：”为什么必须显式用`in`或`out`标记泛型类型参数？“他们认为编译器应该能检查委托或接口声明，并自动检测哪些泛型类型参数能够逆变和协变。虽然编译器确实能，但C#团队认为必须由你订立协定(contract)，明确说明想允许什么。例如，假定编译器判断一个泛型类型参数是逆变量(用在输入位置)，但你将来向某个接口添加了成员，并将类型参数用在了输出位置。下次编译时，编译器将认为该类型参数是不变量。但在引用了其他成员的所有地方，只要还以为”类型参数是逆变量“就可能出错。

>因此，编译器团队决定，在声明泛型类型参数时，必须由你显使用`in`或`out`来标记可变性。以后使用这个类型参数时，假如用法与声明时指定的不符，编译器就会报错，提醒你违反了自己订立的协定。如果为泛型类型参数添加`in`或`out`来打破原来的协定，就必须修改使用旧协定的代码。

## <a name="12_6">12.6 泛型方法</a>

定义泛型类、结果或接口时，类型中定义的任何方法都可引用类型指定的类型参数。类型参数可作为方法参数、方法返回值或方法内部定义的局部变量的类型使用。然而，CLR还允许方法指定它自己的类型参数。这些类型参数也可作为、返回值或局部变量的类型使用。

在下例中，类型定义了一个类型参数，方法也定义了自己的：

```C#
internal sealed class GenericType<T> {
    private T m_value;

    public GenericType (T value) { m_value = value; }

    public TOutput Converter<TOutput>() {
        TOutput result = (TOutput) Convert.ChangeType(m_value, typeof(TOutput));
        return result;   // 返回类型转换之后的结果
    }
}
```

在这个例子中，`GenericType`类定义了类型参数(`T`)，`Converter`方法也定义了自己的类型参数(`TOutput`)。这样的`GenericType`可以处理任意类型。`Converter`方法能将`m_value`字段引用的对象转换成任意类型——具体取决于调用时传递的类型实参是什么。泛型方法的存在，为开发人员提供了极大的灵活性。

泛型方法的一个很好的例子是`Swap`方法：

```C#
private static void Swap<T>(ref T o1, ref T o2) {
    T temp = o1;
    o1 = o2;
    o2 = temp;
}
```

现在可以这样调用 `Swap`：

```C#
private static void CallingSwap() {
    Int32 n1 = 1, n2 = 2;
    Console.WriteLine("n1={0}, n2={1}", n1, n2);
    Swap<Int32>(ref n1, ref n2);
    Console.WriteLine("n1={0}, n2={1}", n1, n2);

    String s1 = "Aidan", s2 = "Grant";
    Console.WriteLine("s1={0}, s2={1}", s1, s2);
    Swap<String>(ref s1, ref s2);
    Console.WriteLine("s1={0}, s2={1}", s1, s2);
}
```

为获取`out`和`ref`参数的方法使用泛型类型很有意思，因为作为`out/ref`实参传递的变量必须具有与方法参数相同的类型，以防止损害类型安全性。涉及`out/ref`参数的这个问题已在 9.3 节“以传引用的方式向方法传递参数”讨论。事实上，`Interlocked`类的`Exchange`和`CompareExchange`方法就是因为这个原因才提供泛型重载的<sup>①</sup>：  

> ① `where` 子句将在本章稍后的 12.8 节”可验证性和约束“讨论。

```C#
public static class Interlocked {
    public static T Exchange<T>(ref T location1, T value) where T : class;
    public static T CompareExchange<T>(ref T location1, T value, T comparand) where T : class;
}
```

### 泛型方法和类型推断

C#泛型语法因为涉及大量”<“和”>“符号，所以开发人员很容易被弄得晕头转向。为了改进代码的创建，增强可读性和可维护性，C# 编译器支持在调用泛型方法时进行**类型推断**。这意味着编译器会在调用泛型方法时自动判断(或者说推断)要使用的类型。以下代码对类型推断进行了演示：

```c#
private static void CallingSwapUsingInference() {
    Int32 n1 = 1, n2 = 2;
    Swap(ref n1, ref n2);       // 调用 Swap<Int32>

    String s1 = "Aidan";
    Object s2 = "Grant";
    Swap(ref s1, ref s2);       // 错误，不能推断类型
}
```

在上述代码中，对 `Swap`的调用没有在一对"<"和”>“中指定类型实参。在第一个`Swap`调用中，C#编译器推断 `n1`和`n2`都是`Int32`，所以应该使用`Int32`类型实参来调用`Swap`。

推断类型时，C#使用变量的数据类型，而不是变量引用的对象的实际类型。所以在第二个`Swap`调用中，C#发现`s1`是`String`，而`s2`是`Object`(即使它恰好引用一个`String`)。由于`s1`和`s2`是不同数据类型的变量，编译器拿不准要为`Swap`传递什么类型实参，所以会报告以下消息：`error CS0411：无法从用法推断出方法“Program.Swap<T>(ref T,ref T)”的类型参数。请尝试显式指定类型参数。`

类型可定义多个方法，让其中一个方法接受具体数据类型，让另一个接受泛型类型参数，如下例所示：

```C#
private static void Display(String s) {
    Console.WriteLine(s);
}

private static void Display<T>(T o) {
    Display(o.ToString());        // 调用 Display(String)
}
```

下面展示了 `Display` 方法的一些调用方式：

```C#
Display("Jeff");            // 调用 Display(String)
Display(123);               // 调用 Display<T>(T)
Display<String>("Aidan");   // 调用 Display<T>(T)
```

在第一个调用中，编译器可调用接受一个`String`参数的`Display`方法，也可调用泛型`Display`方法(将`T`替换成`String`)。但C#编译器的策略是先考虑较明确的匹配，再考虑泛型匹配。所以，它会生成对非泛型`Display`方法的调用，也就是接受一个`String`参数的版本。对于第二个调用，编译器不能调用接受`String`参数的非泛型`Display`方法，所以必须调用泛型`Display`方法。顺便说一句，编译器优先选择明确的匹配，开发人员应对此感到庆幸。假如编译器优先选择泛型方法，那么由于泛型`Display`方法会再次调用`Display`(但传递由`ToString`返回的一个`String`)，所以会造成无限递归。

对`Display`的第三个调用明确指定了泛型类型实参`String`。这告诉编译器不要尝试推断类型实参。相反，应使用显式指定的类型实参。在本例中，编译器还假定我肯定是想调用泛型`Display`方法，所以会毫不犹豫地调用泛型`Display`方法。在内部，泛型`Display`方法会为传入的字符串调用`ToString`方法，然后将转换所得的字符串传给非泛型`Display`方法。

## <a name="12_7">12.7 泛型和其他成员</a>

在C#中，属性、索引器、事件、操作符方法、构造器和终结器本身不能有类型参数。但它们能在泛型类型中定义，而且这些成员中的代码能使用类型参数。

C#之所以不允许这些成员指定自己的泛型类型参数，是因为 Microsoft C# 团队认为开发人员很少需要将这些成员作为泛型使用。除此之外，为这些成员添加泛型支持的代价是相当高的，因为必须为语言设计足够的语法。例如，在代码中使用一个`+`操作符时，编译器可能要调用一个操作符重载方法。而在代码中，没有任何办法能伴随`+`操作符指定类型实参。

## <a name="12_8">12.8 可验证性和约束</a>

编译泛型代码时，C#编译器会进行分析，确保代码适用于当前已有或将来可能定义的任何类型。看看下面这个方法：

```C#
private static Boolean MethodTakingAnyType<T>(T o) {
    T temp = o;
    Console.WriteLine(o.ToString());
    Boolean b = temp.Equals(o);
    return b;
}
```

这个方法声明了`T`类型临时变量(`temp`)。然后，方法执行两次变量赋值和几次方法调用。这个方法适用于任何类型。无论`T`是引用类型，是值类型或枚举类型，还是接口或委托类型，它都能工作。这个方法适用于当前存在的所有类型，也适用于将来可能定义的任何类型，因为所有类型都支持对`Object`类型的变量的赋值，也支持对`Object`类型定义的方法的调用(比如`ToString`和`Equals`)。

再来看看下面这个方法：

```C#
private static T Min<T>(T o1, T o2) {
    if (o1.CompareTo(o2) < 0) return o1;
    return o2;
}
```

`Min`方法试图使用`o1`变量来调用`CompareTo`方法。但是，许多类型都没有提供`CompareTo`方法，所以 C#编译器不能编译上述代码，它不能保证这个方法适用于所有类型。强行编译上述代码会报告消息：  

`error CS0117:"T"不包含“CompareTo”的定义，并且找不到可接受类型为“T”的第一个参数的扩展方法“CompareTo”(是否缺少 using 指令或程序集引用?)。`

所以从表面看，使用泛型似乎做不了太多事情。只能声明泛型类型的变量，执行变量赋值，再调用`Object`定义的方法，如此而已！显然，假如泛型只能这么用，可以说它几乎没有任何用。幸好，编译器和 CLR 支持称为**约束**的机制，可通过它使泛型变得真正有用！

约束的作用是限制能指定成泛型实参的类型数量。通过限制类型的数量，可以对那些类型执行更多操作。以下是新版本的`Min`方法，它指定了一个约束(加粗显示)：

```C#
public static T Min<T>(T ol, T o2) where T : IComparable<T> {
    if (o1.CompareTo(o2) < 0) return o1;
    return o2;
}
```

C# 的`where` 关键字告诉编译器，为`T`指定的任何类型都必须实现同类型(`T`)的泛型`IComparable`接口。有了这个约束，就可以在方法中调用`CompareTo`，因为已知`IComparable<T>`接口定义了`CompareTo`。

现在，当代码引用泛型类型或方法时，编译器要负责保证类型实参符合指定的约束。例如，假如编译以下代码：

```C#
private static void CallMin() {
    Object o1 = "Jeff", o2 = "Richter";
    Object oMin = Min<Object>(o1, o2);      // Error CS0311
}
```

编译器会报告以下消息：

`error CS0311:不能将类型“object”用作泛型类型或方法“SomeType.Min<T>(T, T)”中的类型参数“T“。没有从”object“到”System.IComparable<object>“的隐式引用转换。`

编译器报错是因为 `System.Object` 没有实现 `IComparable<Object>` 接口。事实上，`System.Object` 没有实现任何接口。

对约束及其工作方式有了一个基本的认识宾，让我们更深入地研究一下它。约束可应用于泛型类型的类型参数，也可应用于泛型方法的类型参数(如 `Min` 方法所示)。CLR 不允许基于类型参数名称或约束来进行重载；只能基于元数(类型参数个数)对类型或方法进行重载。下例对此进行了演示：

```C#
// 可定义以下类型
internal sealed class AType { }
internal sealed class AType<T> { }
internal sealed class AType<T1, T2> { }

// 错误：与没有约束的 AType<T> 冲突
internal sealed class AType<T> where T : IComparable<T> { }

// 错误：与 AType<T1, T2>冲突
internal sealed class AType<T3, T4> { }

internal sealed class AnotherType {
    // 可定义以下方法，参数个数不同：
    private static void M() { }
    private static void M<T>() { }
    private static void M<T1, T2>() { }

    // 错误：与没有约束的 M<T> 冲突
    private static void M<T>() where T : IComparable<T> { }

    // 错误：与 M<T1, T2>冲突
    private static void M<T3, T4>() { }
}
```

重写虚泛型方法时，重写的方法必须指定相同数量的类型参数，而且这些类型参数会继承在基类方法上指定的约束。事实上，根本不允许为重写方法的类型参数指定任何约束。但类型参数的名称是可以改变的。类似地，实现接口方法时，方法必须指定与接口方法等量的类型参数，这些类型参数将继承由接口方法指定的约束。下例使用虚方法演示了这一规则：

```C#
internal class Base {
    public virtual void M<T1, T2>()
        where T1 : struct
        where T2 : class {
    }
}

internal sealed class Derived : Base {
    public override void M<T3, T4>()
        where T3 : EventArgs    // 错误
        where T4 : class        // 错误
    {}
}
```

试图编译上述代码，编译器会报告以下错误：

`error CS0460：重写和显式接口实现方法的约束是从基方法继承的，因此不能直接指定这些约束。`

从`Derived` 类的 `M<T3, T4>`方法中移除两个 `where` 子句，代码就能正常编译了。注意，类型参数的名称可以更改，比如将`T1`改成`T3`，将`T2`改成`T4`)；但约束不能更改(甚至不能指定)。

下面讨论编译器/CLR 允许向类型参数应用的各种约束。可用一个主要约束、一个次要约束以及/或者一个构造器约束来约束类型擦书。接下来的三个小节分别讨论这些约束。

### 12.8.1 主要约束

类型参数可以指定零个或者一个**主要约束**。主要约束可以是代表非密封类的一个引用类型。不能指定以下特殊引用类型：`System.Object`，`System.Array`，`System.Delegate`，`System.MulticastDelegate`，`System.ValueType`，`System.Enum`或者`System.Void`。

指定引用类型约束时，相当于向编译器承诺：一个指定的类型实参要么是与约束类型相同的类型，要么是从约束类型派生的类型。例如以下泛型类：

```C#
internal sealed class PrimaryConstraintOfStream<T> where T : Stream {
    public void M(T stream) {
        stream.Close();         // 正确
    }
}
```

在这个类定义中，类型参数 `T` 设置了主要约束 `Stream`(在 `System.IO` 命名空间中定义)。这就告诉编译器，使用 `PrimaryConstraintOfStream` 的代码在指定类型实参时，必须指定 `Stream` 或者从`Stream`派生的类型(比如 `FileStream`)。如果类型参数没有指定主要约束，就默认为 `System.Object`。但如果在源代码中显式指定`System.Object`， C#会报错：`error CS0702:约束不能是特殊类”object“`。

有两个特殊的主要约束：`class`和`struct`。其中，`class约束向编译器承诺类型实参是引用类型。任何类类型、接口类型、委托类型或者数组类型都满足这个约束。例如以下泛型类：

```C#
internal sealed class PrimaryConstraintOfStream<T> where T : class {
    public void M() {
        T temp = null;      // 允许，因为 T 肯定是引用类型
    }
}
```

在这个例子中，将`temp`设为`null`是合法的，因为 `T`  已知是引用类型，而所有引用类型的变量都能设为`null`。不对`T`进行约束，上述代码就通不过编译，因为`T`可能是值类型，而值类型的变量不能设为`null`。

`struct`约束向编译器承诺类型参数是值类型。包括枚举在内的任何值类型都满足这个约束。但编译器和CLR将任何`System.Nullable<T>`值类型视为特殊类型，不满足这个`struct`约束。

原因是 `Nullable<T>`类型将它的类型参数约束为`struct`，而 CLR 希望禁止像`Nullable<Nullable<T>>`这样的递归类型。可空类型将在第19章”可空值类型“讨论。

以下示例类使用 `struct` 约束来约束它的类型参数：

```C#
internal sealed class PrimaryConstraintOfStruct<T> where T : struct {
    public static T Factory() {
        // 允许。因为所有值类型都隐式有一个公共无参构造器
        return new T();
    }
}
```

这个例子中的`new T()` 是合法的，因为 `T` 已知是值类型，而所有值类型都隐式地有一个公共无参构造器。如果`T`不约束，约束为引用类型，或者约束为`class`，上述代码将无法通过编译，因为有的引用类型没有公共无参构造器。

### 12.8.2 次要约束

类型参数可以指定零个或者多个**次要约束**，次要约束代表接口类型。这种约束向编译器承诺类型实参实现了接口。由于能指定多个接口约束，所以类型实参必须实现了所有接口约束(以及主要约束，如果有的话)。第 13 章”接口“将详细讨论接口约束。

还有一种次要约束称为**类型参数约束**，有时也称为**裸类型约束**。这种约束用得比接口约束少得多。它允许一个泛型类型或方法规定：指定的类型实参要么就是约束的类型，要么是约束的类型的派生类。一个类型参数可以指定零个或者多个类型参数约束。下面这个泛型方法演示了如何使用类型参数约束：

```C#
private static List<TBase> ConvertIList<T, TBase>(IList<T> list) where T : TBase {
    List<TBase> baseList = new List<TBase>(list.Count);
    for (Int32 index = 0; index < list.Count; index++) {
        baseList.Add(list[index]);
    }
    return baseList;
}
```

`ConvertIList`方法指定了两个类型参数，其中`T`参数由`TBase`类型参数约束。意味着不管为`T`指定什么类型实参，都必须兼容于为`TBase`指定的类型实参。下面这个方法演示了对`ConvertIList`的合法调用和非法调用：

```C#
private static void CallingConvertIList() {
    // 构造并初始化一个 List<String>(它实现了 IList<String>)
    IList<String> ls = new List<String>();
    ls.Add("A String");

    // 1. 将 IList<String> 转换成一个 IList<Object>
    IList<Object> lo = ConvertIList<String, Object>(ls);

    // 2. 将 IList<String> 转换成一个 IList<IComparable>
    IList<IComparable> lc = ConvertIList<String, IComparable>(ls);

    // 3. 将 IList<String> 转换成一个 IList<IComparable<String>>
    IList<IComparable<String>> lcs = ConvertIList<String, IComparable<String>>(ls);

    // 4. 将 IList<String> 转换成一个 IList<String>
    IList<String> ls2 = ConvertIList<String, String>(ls);

    // 5. 将 IList<String> 转换成一个 IList<Exception>
    IList<Exception> le = ConvertIList<String, Exception>(ls);  // 错误
}
```

在对`ConvertIList` 的第一个调用中，编译器检查 `String` 是否兼容于`Object`。由于`String`从`Object`派生，所以第一个调用满足类型参数约束。在对 `ConvertIList` 的第二个调用中，编译器检查`String`是否兼容于`IComparable`。由于`String`实现了`IComparable`接口，所以第二个调用满足类型参数约束。在对`ConvertIList`的第三个调用中，编译器检查`String`是否兼容于`IComparable<String>`。由于`String`实现了`IComparable<String>`。由于 `String` 实现了 `IComparable<String>`接口，所以第三个调用满足类型参数约束。在对`ConvertIList`的第4个调用中，编译器知道`String`兼容于它自己。在对`ConvertIList`的第 5 个调用中，编译器检查`String`是否兼容于`Exception`。由于 `String` 不兼容于`Exception`，所以第 5 个调用不满足类型参数约束，编译器报告以下消息：  
`error CS0311: 不能将类型“string”用作泛型类型或方法“Program.ConvertIList<T, TBase>(System.Collections.Generic.IList<T>)”中的类型参数“T”。没有从“string”到“System.Exception”的隐式引用转换。`  

### 12.8.3 构造器约束

类型参数可指定零个或一个**构造器约束**，它向编译器承诺类型实参是实现了公共无参构造器的非抽象类型。注意，如果同时使用构造器约束和 `struct` 约束，C# 编译器会认为这是一个错误，因为这是多余的；所有值类型都隐式提供了公共无参构造器。以下示例类使用构造器约束来约束它的类型参数：

```C#
internal sealed class ConstructorConstraint<T> where T : new() {
    public static T Factory() {
        // 允许，因为所有值类型都隐式有一个公共无参构造器.
        // 而如果指定的是引用类型，约束也要求它提供公共无参构造器
        return new T();
    }
}
```

这个例子中的 `new T()` 是合法的，因为已知`T`是拥有公共无参构造器的类型。对所有值类型来说，这一点(拥有公共无参构造器)肯定成立。对于作为类型实参指定的任何引用类型，这一点也成立，因为构造器约束要求它必须成立。

开发人员有时想为类型参数指定一个构造器约束，并指定构造器要获取多个参数。目前，CLR(以及C# 编译器)只支持无参构造器。Microsoft 认为这已经能满足几乎所有情况，我对次也表示同意。

### 12.8.4 其他可验证性问题

本节剩下的部分将讨论另外几个特殊的代码构造。由于可验证性问题，这些代码构造在和泛型共同使用时，可能产生不可预期的行为。另外，还讨论了如何利用约束使代码重新变得可以验证。

1. **泛型类型变量的转型**  
  将泛型类型的变量转型为其他类型是非法的，除非转型为与约束兼容的类型：

  ```C#
  private static void CastingAGenericTypeVariablel<T>(T obj) {
      Int32 x = (Int32) obj;    // 错误
      String s = (String) obj;  // 错误
  }
  ```
  上述两行会造成编译器报错，因为 `T` 可能是任意类型，无法保证成功转型。为了修改上述代码使其能通过编译，可以先转型为`Object`：  
  
  ```C#
  private static void CastingAGenericTypeVariable2<T>(T obj) {
      Int32 x = (Int32) (Object) obj;     // 无错误
      String s = (String) (Object) obj;   // 无错误
  }
  ```

  虽然代码现在能编译，但 CLR 仍有可能在运行时抛出 `InvalidCastException` 异常。

  转型为引用类型时还可使用C# `as` 操作符。下面对代码进行了修改，为 `String` 使用了 `as` 操作符 (`Int32` 是值类型不能用)：

  ```C#
  private static void CastingAGenericTypeVariable3<T>(T obj) {
      String s = obj as String;     // 无错误
  }
  ```

2. **将泛型类型变量设为默认值**  
  将泛型类型变量设为 `null` 是非法的，除非将泛型类型约束成引用类型。

  ```C#
  private static void SettingAGenericTypeVariableToNull<T>() {
      T temp = null; // error CS0403 - 无法将 null 转换为类型参数 “T”，
                     // 因为它可能是不可以为 null 的值类型。请考虑改用 default(T)
  }
  ```

  由于未对 `T` 进行约束，所以它可能是值类型，而将值类型的变量设为`null`是不可能的。如果`T`被约束成引用类型，将`temp`设为`null`就是合法的，代码能顺利编译并运行。
  
  Microsoft 的 C# 团队认为有必要允许开发人员将变量设为它的默认值，并专门为此提供了 `default` 关键字：
  
  ```C#
  private static void SettingAGenericTypeVariableToDefaultValue<T>() {
      T temp = default(T);  // 正确
  }
  ```

  以上代码中的 `default` 关键字告诉 C# 编译器和 CLR 的 JIT 编译器，如果 `T` 是引用类型，就将 `temp` 设为 `null`；如果是值类型，就将 `temp` 的所有位设为 `0`。

3. **将泛型类型变量与null进行比较**  
  无论泛型类型是否被约束，使用 `==`或 `!=` 操作符将泛型类型变量与 `null` 进行比较都是合法的：

  ```C#
  private static void ComparingAGenericTypeVariableWithNull<T>(T obj) {
      if (obj == null) { /* 对于值类型，永远都不会执行 */ }
  }
  ```
  
  由于 `T` 未进行约束，所以可能是引用类型或值类型。如果`T`是值类型，`obj`永远都不会为`null`。你或许以为C#编译器会报错。但C# 编译器并不报错；相反，它能顺利地编译代码。调用这个方法时，如果为类型参数传递值类型，那么 JIT 编译器知道`if`语句永远都不会为`true`，所以不会为`if`测试或者大括号内的代码生成本机代码。如果换用`!=`操作符，JIT编译器不会为`if`测试生成代码(因为它肯定为`true`)，但会为`if`大括号内的代码生成本机代码。
  
  顺便说一句，如果`T`被约束成`struct`，C# 编译器会报错。值类型的变量不能与`null`进行比较，因为结果始终一样。
  
4. **两个泛型类型变量相互比较**  
  如果泛型类型参数不能肯定是引用类型，对同一个泛型类型的两个变量进行比较是非法的：  
  
  ```C#
  private static void ComparingTwoGenericTypeVariables<T>(T o1, T o2) {
      if (o1 == o2) { }  // 错误
  }
  ```
  
  在这个例子中，`T` 未进行约束。虽然两个引用类型的变量相互比较是合法的，但两个值类型的变量相互比较是非法的，除非值类型重载了`==`操作符。如果`T`被约束成`class`，上述代码能通过编译。如果变量引用同一个对象，`==`操作符会返回`true`。注意，如果`T`被约束成引用类型，而且该引用类型重载了`operator==`方法，那么编译器会在看到`==`操作符时生成对这个方法的调用。显然，所有些讨论也适合`!=`操作符。
  
  写代码来比较基元值类型(`Byte`，`Int32`，`Single`，`Decimal`等)时，C#编译器知道如何生成正确的代码。然而，对于非基元值类型，C#编译器不知道如何生成代码来进行比较。所以，如果`ComparingTwoGenericTypeVariables` 方法的`T`被约束成`struct`，编译器会报错。

  不允许将类型参数约束成具体的值类型，因为值类型隐式密封，不可能存在从值类型派生的类型。如果允许将类型参数约束成具体的值类型，那么泛型方法会被约束为只支持该具体类型，这还不如不要泛型呢！

5. **泛型类型变量作为操作数使用**  
  最后要注意，将操作符应用于泛型类型的操作数会出现大量问题。第 5 章讨论了 C# 如何处理它的基元类型：`Byte`，`Int16`，`Int32`，`Int64`，`Decimal`等。我特别指出 C# 知道如何解释应用于基元类型的操作符(比如`+`，`-`，`*`和`/`)。但不能将这些擦作符应用于泛型类型的变量。编译器在编译时确定不了类型，所以不能向泛型类型的变量应用任何操作符。所以，不可能写一个能处理任何数值数据类型的算法。下面是我想写的一个示例泛型方法：

  ```C#
  private static T Sum<T>(T num) where T : struct {
      T sum = default(T) ;
      for (T n = default(T); n < num ; n++)
        sum += n;
      return sum;
  }
  ```

  我千方百计想让这个方法通过编译。我将`T`约束成一个`struct`，而且使用`default(T)`将`sum`和`n`初始化为`0`。但编译时得到以下错误： 

  + `error CS0019`: 运算符"<"无法应用于“T”和“T”类型的操作数
  + `error CS0023`：运算符“++”无法应用于“T”类型的操作数
  + `error CS0019`：运算符“+=”无法应用于“T”和“T”类型的操作数

  这是 CLR 的泛型支持体系的一个严重限制，许多开发人员(尤其是科学、金融和数学领域的开发人员)对这个限制感到很失望。许多人尝试用各种技术来避开这一限制，其中包括反射(参见第 23 章“程序集加载和反射”)、`dynamic`基元类型(5.5 节)和操作符重载等。但所有这些技术都会严重损害性能或者影响代码的可读性。希望 Microsoft 在 CLR 和编译器未来的版本中解决这个问题。
  