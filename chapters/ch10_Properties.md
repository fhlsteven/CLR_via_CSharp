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

