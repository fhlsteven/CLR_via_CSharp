# 第 8 章 方法

本章内容：

* <a href="#8_1">实例构造器和类(引用类型)</a>
* <a href="#8_2">实例构造器和结构(值类型)</a>
* <a href="#8_3">类型构造器</a>
* <a href="#8_4">操作符重载方法</a>
* <a href="#8_5">转换操作符方法</a>
* <a href="#8_6">扩展方法</a>
* <a href="#8_7">分部方法</a>

本章重点讨论你将来可能遇到的各种方法，包括实例构造器和类型构造器。还会讲述如何定义方法来重载操作符和类型转换(以进行隐式和显示转型)。还会讨论扩展方法，以便将自己的实例在逻辑上“添加”到现在类型中。还会讨论分部方法，允许将类型的实现分散到多个组成部分中。

## <a name="8_1">8.1 实例构造器和类(引用类型)</a>

**构造器**是将类型的实例初始化为良好状态的特殊方法。构造器方法在“方法定义元数据表”中始终叫做 `.ctor`(constructor 的简称)。创建引用类型的实例时，首先为实例的数据字段分配内存，然后初始化对象的附加字段(类型对象指针和同步块索引)，最后调用类型的实例构造器来设置对象的初始状态。

> 这些附加的字段称为 overhead fields，“overhead”是开销的意思，意味着是创建对象时必须的“开销”。——译注

构造引用类型的对象时，在调用类型的实例构造器之前，为对象分配的内存总是先被归零。没有构造器显式重写的所有字段都保证获得 **0** 或 `null`值。

和其他方法不同，实例构造器永远不能被继承。也就是说，类只有类自己定义的实例构造器。由于永远不能继承实例构造器，所以实例构造器不能使用以下修饰符：`virtual`，`new`，`override`，`sealed` 和 `abstract`。如果类没有显式定义任何构造器，C#编译器将定义一个默认(无参)构造器。在它的实现中，只是简单地调用了基类的无参构造器。

例如下面这个类：

```C#
public class SomeType {
}
```

它等价于：

```C#
public class SomeType {
    public SomeType() : base() { }
}
```

如果类的修饰符为 `abstract`，那么编译器生成的默认构造器的可访问性就为 `protected`；否则，构造器会被赋予 `public` 可访问性。如果基类没有提供无参构造器，那么派生类必须显式调用一个基类构造器，否则编译器会报错。如果类的修饰符为`static`(`sealed` 和 `abstract`)，编译器根本不会在类的定义中生成默认构造器。

> 静态类在元数据中是抽象密封类。 —— 译注

一个类型可以定义多个实例构造器。每个构造器都必须有不同的签名，而且每个都可以有不同的可访问性。为了使代码"可验证"(verifiable)，类的实例构造器在访问从基类继承的任何字段之前，必须先调用基类的构造器。如果派生类的构造器没有显式调用一个基类构造器，C# 编译器会自动生成对默认的基类构造器的调用。最终，`System.Object` 的公共无参构造器会得到调用。该构造器什么都不做，会直接返回。由于 `System.Object` 没有定义实例数据字段，所以它的构造器无事可做。

极少数时候可以在不调用实例构造器的前提下创建类型的实例。一个典型的例子是 `Object` 的 `MemberwiseClone` 方法。该方法的作用是分配内存，初始化对象的附加字段(类型对象指针和同步块索引)，然后将源对象的字节数据复制到新对象中。另外，用运行时序列化器(runtime serializer)反序列化对象时，通常也不需要调用构造器。反序列化代码使用`System.Runtime.Serialization.FormatterServices`类型的`GetUninitializedObject`或者`GetSafeUninitializedObject`方法为对象分配内存，期间不会调用一个构造器。详情参见第 24 章“运行时序列化”。

> 重要提示 不要在构造器中调用虚方法。原因是假如被实例化的类型重写了虚方法，就会执行派生类型对虚方法的实现。但在这个时候，尚未完成对继承层次结构中的所有字段的初始化(被实例化的类型的构造器还没有运行呢)。所以，调用虚方法会导致无法预测的行为。归根到底，这是由于调用虚方法时，直到运行时之前都不会选择执行该方法的实际类型。

C# 语言用简单的语法在构造引用类型的实例时初始化类型中定义的字段：

```C#
internal sealed class SomeType {
    private Int32 m_x = 5;
}
```

构造 `SomeType` 的对象时，它的 `m_x` 字段被初始化为`5`。这是如何发生的呢？检查一下 `SomeType` 的构造器方法(也称作`.ctor`)的 IL 代码就明白了，如下所示：

```C#
.method public hidebysig specialname rtspecialname 
         instance void .ctor() cil managed
{
    // Code size 14 (0xe)
    .maxstack 8 
    IL_0000: ldarg.0 
    IL_0001: ldc.i4.5 
    IL_0002: stfld  int32 SomeType::m_x
    IL_0007: ldarg.0 
    IL_0008: call         instance void [mscorlib]System.Object::.ctor()
    IL_000d: ret
} // end of method SomeType::.ctor
```

可以看出，`SomeType`的构造器把值`5`存储到字段`m_x`，再调用基类的构造器。换句话说，C# 编译器提供了一个简化的语法，允许以“内联”(其实就是嵌入)方式初始化实例字段。但在幕后，它会将这种语法转换成构造器方法中的代码来执行初始化。这同时提醒我们注意代码的膨胀效应。如以下类定义所示：

```C#
internal sealed class SomeType {
    private Int32 m_x = 5;
    private String m_s = "Hi there"; 
    private Double m_d = 3.14159; 
    private Byte m_b;

    // 下面是一些构造器
    public SomeType() { ... }
    public SomeType(Int32 x) { ... }
    public SomeType(String s) { ...; m_d = 10; } 
} 
```

编译器为这三个构造器方法生成代码时，在每个方法的开始位置，都会包含用于初始化`m_x`，`m_s`和`m_d`的代码。在这些初始化代码之后，编译器会插入对基类构造器的调用。再然后，会插入构造器自己的代码。例如，对于获取一个`String`参数的构造器，编译器生成的代码首先初始化`m_x`，`m_s`和`m_d`，再调用基类(`Object`)的构造器，再执行自己的代码(最后是用值`10`覆盖`m_d`原先的值)。注意，即使没有代码显式初始化`m_b`，`m_b`也保证会被初始化为`0`。

> 注意 编译器在调用基类构造器前使用简化语法对所有字段进行初始化，以维持源代码给人留下的“这些字段总是有一个值”的印象。但假如基类构造器调用了虚方法并回调由派生类定义的方法，就可能出问题。在这种情况下，使用简化语法初始化的字段在调用虚方法之前就初始化好了。

由于有三个构造器，所以编译器生成三次初始化 `m_x`，`m_s` 和 `m_d` 的代码——每个构造器一次。如果有几个已初始化的实例字段和许多重载的构造器方法，可考虑不是在定义字段时初始化，而是创建单个构造器来执行这些公共的初始化。然后，让其他构造器都显式调用这个公共初始化构造器。这样能减少生成的代码。下例演示了如何在 C# 中利用 `this` 关键字显式调用另一个构造器：

```C#
using System;
internal sealed class SomeType {
    // 不要显式初始化下面的字段
    private Int32   m_x;
    private String  m_s;
    private Double  m_d;
    private Byte    m_b;

    // 该构造器将所有字段都设为默认值，
    // 其他所有构造器都显式调用该构造器
    public SomeType() {
        m_x = 5;
        m_s = "Hi there";
        m_d = 3.14159;
        m_b = 0xff;
    }

    // 该构造器将所有的字段都设为默认值，然后修改 m_x
    public SomeType(Int32 x) : this() {
        m_x = x;
    }

    // 该构造器所有的字段都设为默认值，然后修改 m_s
    public SomeType(String s) : this() {
        m_s = s;
    }

    // 该构造器首先将所有字段都设为默认值，然后修改 m_x 和 m_s
    public SomeType(Int32 x, String s) : this() {
        m_x = x;
        m_s = s;
    }
}
```

## <a name="8_2">8.2 实例构造器和结构(值类型)</a>

值类型(`struct`)构造器的工作方式与引用类型(`class`)的构造器截然不同。CLR 总是允许创建值类型的实例，并且没有办法阻止值类型的实例化。所以，值类型其实并不需要定义构造器，C#编译器根本不会为值类型内联(嵌入)默认的无参构造器。来看下面的代码：

```C#
internal struct Point {
    public Int32 m_x, m_y;
}

internal sealed class Rectangle {
    public Point m_topLeft, m_bottomRight;
}
```

为了构造一个 `Rectangle`，必须使用`new`操作符，而且必须指定构造器。在这个例子中，调用的是C#编译器自动生成的默认构造器。为 `Rectangle`分配内存时，内存中包含 `Point` 值类型的两个实例。考虑到性能，CLR 不会为包含子在引用类型中的每个值类型字段都主动调用构造器。但是，如前所述，值类型的字段会被初始化为 `0` 或 `null`。

CLR 确实允许为值类型定义构造器。但必须显示调用才会执行。下面是一个例子。

```C#
internal struct Point {
    public Int32 m_x, m_y;
    public Point(Int32 x, Int32 y) {
        m_x = x;
        m_y = y;
    }
}

internal sealed class Rectangle {
    public Point m_topLeft, m_bottomRight;

    public Rectangle() {
        m_topLeft = new Point(1, 2);
        m_bottomRight = new Point(100, 200);
    }
}
```

值类型的实例构造器只有显式调用才会执行。因此，如果 `Rectangle` 的构造器没有使用 `new` 操作符来调用 `Point` 的构造器，从而初始化 `Rectangle`的`m_topLeft`字段和`m_bottomRight`字段，那么两个`Point`字段中的`m_x`和`m_y`字段都将为 `0`。

前面展示的 `Point` 值类型没有定义默认的无参构造器。现在进行如下改写：

```C#
internal struct Point {
    public Int32 m_x, m_y;

    public Point() {
        m_x = m_y = 5;
    }
}

internal sealed class Rectangle {
    public Point m_topLeft, m_bottomRight;

    public Rectangle(){        
    }
}
```

现在，构造新的 `Rectangle` 类时，两个 `Point` 字段中的 `m_x`和`m_y`字段会被初始化成多少？是 `0`还是`5`？(提示：小心上当！)

许多开发人员(尤其是那些有 C++ 背景的)都觉得 C# 编译器会在 `Rectangle` 的构造器中生成代码，为 `Rectangle` 的两个字段自动调用 `Point` 的默认无参构造器。但是，为了增强应用程序的运行时性能，C# 编译器不会自动生成这样的代码。实际上，即便值类型提供了无参构造器，许多编译器也永远不会生成代码来自动调用它。为了执行值类型的无参构造器，开发人员必须增加显式调用值类型构造器的代码。

基于前面这一段的信息，可以确定在前面示例代码中，`Rectangle` 类的两个 `Point` 字段的 `m_x` 和 `m_y`字段会初始化为 `0`，因为代码中没有任何地方显式调用了 `Point`的构造器。

但我说过，这是一个容易让人上当的问题。这里的关键在于C#编译器不允许值类型定义无参构造器。所以，前面的代码实际是编译不了的。试图编译上述代码时，C#编译器会显示以下消息：`error CS0568:结构不能包含显式的无参数构造器。`

C#编译器故意不允许值类型定义无参构造器，目的是防止开发人员对这种构造器在什么时候调用产生迷惑。由于不能定义无参构造器，所以编译器永远不会生成自动调用它的代码。没有无参构造器，值类型的字段总是被初始化为 `0` 或 `null`。
