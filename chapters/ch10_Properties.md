# 第 10 章 属性

本章内容：

* <a href="#10_1">无参属性</a>
* <a href="#10_2">有参属性</a>
* <a href="#10_3">调用属性访问器方法时的性能</a>
* <a href="#10_4">属性访问器的可访问性</a>
* <a href="#10_5">泛型属性访问器方法</a>

本章讨论`属性`，它允许源代码用简化语法来调用方法。CLR 支持两种属性；`无参属性`，平时说的属性就是指它；`有参属性`，它在不同的编程语言中有不同的称呼。例如，C# 将有参属性称为`索引器`，Microsoft Visual Basic 将有参属性称为`默认属性`。还要讨论如何使用“对象和集合初始化器”来初始化属性，以及如何用 C# 的匿名类型和 `System.Tuple` 类型将多个属性打包到一起。

## <a name="10_1">10.1 无参属性</a>

许多类型都定义了能被获取或更改的状态信息。这种状态信息一般作为类型的字段成员实现。例如，以下类型定义包含两个字段：

```C#
public sealed class Employee {
    public String Name;         // 员工姓名
    public Int32 Age;           // 员工年龄
}
```

创建该类型的实例后，可以使用以下形式的代码轻松获取(get)或设置(set)它的状态信息：

```C#
Employee e = new Employee();
e.Name = "Jeffrey Richter";    // 设置员工姓名
e.Age = 45;                    // 设置员工年龄

Console.WriteLine(e.Name);     // 显示 "Jeffrey Richter"
```

这种查询和设置对象状态信息的做法十分常见。但我必须争辩的是，永远都不应该像这样实现。面向对象设计和编程的重要原则之一就是`数据封装`，意味着类型的字段永远不应该公开，否则很容易因为不恰当使用字段而破坏对象的状态。例如，以下代码可以很容易地破坏一个`Employee` 对象：

```C#
    e.Age = -5;     // 怎么可能有人是 -5 岁呢？
```

还有其他原因促使我们封装对类型中的数据字段的访问。其一，你可能希望访问字段来执行一些副作用<sup>①</sup>、缓存某些值或者推迟创建一些内部对象<sup>②</sup>。其二，你可能希望以线程安全的方式访问字段。其三，字段可能是一个逻辑字段，它的值不由内存中的字节表示，而是通过某个算法来计算获得。
> ① 即 side effect；在计算机编程中，如果一个函数或表达式除了生成一个值，还会造成状态的改变，就说它会造成副作用；或者说会执行一个副作用。——译注
> ② 推迟创建对象是指在对象第一次需要时才真正创建它。——译注

基于上述原因，强烈建议将所有字段都设为 `private`。要允许用户或类型获取或设置状态信息，就公开一个针对该用途的方法。封装了字段访问的方法通常称为`访问器(accessor)`方法。访问器方法可选择对数据的合理性进行检查，确保对象的状态永远不被破坏。例如，我将上一个类重写为以下形式：

```C#
public sealed class Employee {
    private String m_Name;   // 字段现在是私有的
    private Int32 m_Age;     // 字段现在是私有的

    public String GetName() {
        return (m_Name);
    }

    public void SetName(String value) {
        m_Name = value;
    }

    public Int32 GetAge() {
        return (m_Age);
    }

    public void  SetAge(Int32 value) {
        if (value < 0)
            throw new ArgumentOutOfRangeException("value", value.ToString(), 
                            "The value must be greater than or equal to 0");
        m_Age = value;
    }
}
```

虽然这只是一个简单的例子，但还是可以看出数据字段封装带来的巨大好处。另外可以看出，实现只读属性或只写属性是多么简单！只需选择不实现一个访问器即可。另外，将 `SetXxx` 方法标记为 `protected`，就可以只允许派生类型修改值。

但是，像这样进行数据封装有两个缺点。首先，因为不得不实现额外的方法，所以必须写更多的代码；其次，类型的用户必须调用方法，而不能直接引用字段名。

```C#
e.SetName("Jeffrey Richter");       // 更新员工姓名
String EmployeeName = e.GetName();  // 获取员工姓名
e.SetAge(41);                       // 更新员工年龄
e.SetAge(-5);                       // 抛出 ArgumentOutOfRangeException 异常
Int32 EmployeeAge = e.GetAge();     // 获取员工年龄
```

我个人认为这些缺点微不足道。不过，编程语言和 CLR 还是提供了一个称为**属性(property)**的机制。它缓解了第一个缺点所造成的影响，同时完全消除了第二个缺点。

```C#
public sealed class Employee {
    private String m_Name;
    private Int32 m_Age;

    public String Name {
        get { return (m_Name); }
        set { m_Name = value; }       // 关键字 value 总是代表新值
    }

    public Int32 Age {
        get { return (m_Age); }
        set {
            if (value < 0)           // 关键字 value 总是代表新值 
                throw new ArgumentOutOfRangeException("value", value.ToString(), 
                            "The value must be greater than or equal to 0");
            m_Age = value;
        }
    }
}
```

如你所见，属性使类型的定义稍微复杂了一些，但由于属性允许采用以下形式来写代码，所以额外的付出还是值得的：

```C#
e.Name = "Jeffrey Richter";         // set 员工姓名
String EmployeeName = e.Name;       // get 员工姓名
e.Age = 41;                         // set 员工年龄
e.Age = -5;                         // 抛出 ArgumentOutOfRangeException 异常
Int32 EmployeeAge = e.Age;          // get 员工年龄
```

可将属性想象成`智能字段`，即背后有额外逻辑的字段。CLR 支持静态、实例、抽象和虚属性。另外，属性可用任意“可访问性”修饰符来标记(详情参见第 6.3 节“成员的可访问性”)，而且可以在接口中定义(详情参见第 13 章“接口”)。

每个属性都有名称和类型(类型不能是 `void`)。属性不能重载，即不能定义名称相同、类型不同的两个属性。定义属性时通常同时指定 `get` 和 `set`两个方法。

经常利用属性的 `get` 和 `set` 方法操纵类型中定义的私有字段。私有字段通常称为**支持字段**(backing field)。但 `get` 和 `set` 方法并非一定要访问支持字段。例如，`System.Threading.Thread`类型提供了  `Priority` 属性来直接和操作系统通信。`Thread` 对象内部没有一个关于线程优先级的字段。没有支持字段的另一种典型的属性是在运行时计算的只读属性。例如，以 0 结束的一个数组的长度或者已知高度和宽度的一个矩形的面积。

定义属性时，取决于属性的定义，编译器在最后的托管程序集中生成以下两项或三项。

* 代表属性 `get` 访问器的方法。仅在属性定义了 `get` 访问器方法时生成。
* 代表属性 `set` 访问器的方法。仅在属性定义了 `set` 访问器方法时生成。
* 托管程序集元数据中的属性定义。这一项必然生成。

以前面的`Employee`类型为例。编译器编译该类型时发现其中的`Name`和`Age`属性。由于两者都有`get`和`set`访问器方法，所以编译器在`Employee`类型中生成 4 个方法定义，这造成原始代码似乎是像下面这样写的：

```C#
public sealed class Employee {
    private String m_Name;
    private Int32 m_Age;

    public String get_Name() {
        return m_Name;
    }

    public void set_Name (String value) {
        m_Name = value;         // 实参 value 总是代表新设的值
    }

    public Int32 get_Age() {
        return m_Age;
    }

    public void set_Age(Int32 value) {
        if (value < 0)  // value 总是代表新值
            throw new ArgumentOutOfRangeException("value", value.ToString(), 
                            "The value must be greater than or equal to 0");
        m_Age = value;
    }
}
```

编译器在你指定的属性名之前自动附加 `get_` 或 `set_` 前缀来生成方法名。C# 内建了对属性的支持。C# 编译器发现代码试图获取或设置属性时，实际会生成对上述某个方法的调用。即使编程语言不直接支持属性，也可调用需要的访问器方法来访问属性。效果一样，只是代码看起来没那么优雅。

除了生成访问器方法，针对源代码中定义的每一个属性，编译器还会在托管程序集的元数据中生成一个属性定义项。在这个记录项中，包含了一些标志(falgs)以及属性的类型。另外，它还引用了`get`和`set`访问器方法。这些信息唯一的作用就是在“属性”这种抽象概念与它的访问器方法之间建立起一个联系。编译器和其他工具可利用这种元数据信息(使用`System.Reflection.PropertyInfo` 类来获得)。CLR 不使用这种元数据信息，在运行时只需要访问器方法。

### 10.1.1 自动实现的属性

如果只是为了封装一个支持字段而创建属性，C#还提供了一种更简洁的语法，称为**自动实现的属性**(Automatically Implemented Property，后文简称为 AIP)，例如下面的 `Name` 属性：

```C#
public sealed class Employee {
    // 自动实现的属性
    public String Name { get; set; }

    private Int32 m_Age;

    public Int32 Age {
        get { return(m_Age); }
        set {
            if(value < 0)    // value 关键字总是代表新值
                throw new ArgumentOutOfRangeException("value", value.ToString(), 
                "The value must be greater than or equal to 0"); 

            m_Age = value;
        }
    }
}
```

声明属性而不提供 `get/set` 方法的实现，C# 会自动为你声明一个私有字段。在本例中，字段的类型是 `String`，也就是属性的类型。另外，编译器会自动实现`get_Name`和`set_Name`方法，分别返回和设置字段中的值。

和直接声明名为`Name`的`public String`字段相比，AIP 的优势在哪里？事实上，两者存在一处重要的区别。使用 AIP，意味着你已经创建了一个属性。访问该属性的任何代码实际都会调用`get`和`set`方法。如果以后决定自己实现`get`和/或`set`方法，而不是接受编译器的默认实现，访问属性的任何代码都不必重新编译。然而，如果将 `Name` 声明为字段，以后又想把它更改为属性，那么访问字段的所有代码都必须重新编译才能访问属性方法。

* 我个人不喜欢编译器的 AIP 功能，所以一般会避免使用它。理由是：字段声明语法可能包含初始化部分，所以要在一行代码中声明并初始化字段。但没有简单的语法初始化 AIP。所以，必须在每个构造器方法中显式初始化每个 AIP。

* 运行时序列化引擎将字段名持久存储到序列化的流中。AIP 的支持字段名称由编译器决定，每次重新编译代码都可能更改这个名称。因此，任何类型只要含有一个 AIP，就没办法对该类型的实例进行反序列化。在任何想要序列化或反序列化的类型中，都不要使用 AIP 功能。

* 调试时不能在 AIP 的`get`或`set`方法上添加断点，所以不好检测应用程序在什么时候获取或设置这个属性。相反，手动实现的属性可设置断点，查错更方便。

还要注意，如果使用 AIP，属性必然是可读和可写的。换言之，编译器肯定同时生成 `get`和`set`方法。这个设计的道理在于，只写字段的支持字段到底是什么名字，所以代码只能用属性名访问属性。另外，任何访问器方法(`get`或`set`)要显式实现，两个访问器方法都必须显式实现，就不能使用 AIP 功能了。换言之， AIP 是作用于整个属性的；要么都用，要么都不用。不能显式实现一个访问器方法，而让另一个自动实现。

### 10.1.2 合理定义属性

我个人不喜欢属性，我还希望 Microsoft .NET Framework 及其编程语言不要提供对属性的支持。理由是属性看起来和字段相似，单本质是方法。这造成了大量误解。程序员在看到貌似访问字段的代码时，会做出一些对属性来说不成立的假定，具体如下所示。

* 属性可以只读或只写，而字段访问总是可读和可写的(一个例外是`readonly`字段仅在构造器中可写)。如果定义属性，最好同时为它提供 `get` 和 `set`访问器方法。

* 属性方法可能抛出异常；字段访问永远不会。

* 属性不能作为 `out` 或 `ref` 参数传给方法，而字段可以。例如以下代码是编译不了的：

```C#
using System;

public sealed class SomeType {
    private static String Name {
        get { return null; }
        set { }
    }

    static void MethodWithOutParam(out String n） { n = null; }

    public static void Main() {
        // 对于下一代代码，C#编译器将报告一下错误消息：
        // errot CS0206：属性或所引器不得作为 out 或 ref 参数传递
        MethodWithOutParam(out Name);
    }
}
```

