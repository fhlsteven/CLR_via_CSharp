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

