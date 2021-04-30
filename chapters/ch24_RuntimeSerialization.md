# 第 24 章 运行时序列化

本章内容

* <a href="#24_1">序列化/反序列化快速入门</a>
* <a href="#24_2">使类型可序列化</a>
* <a href="#24_3">控制序列化和反序列化</a>
* <a href="#24_4">格式化器如何序列化类型实例</a>
* <a href="#24_5">控制序列化/反序列化的数据</a>
* <a href="#24_6">流上下文</a>
* <a href="#24_7">将类型序列化为不同的类型以及将对象反序列化为不同的对象</a>
* <a href="#24_8">序列化代理</a>
* <a href="#24_9">反序列化对象时重写程序集和/或类型</a>

**序列化**是将对象或对象图<sup>①</sup>转换成字节流的过程。**反序列化**是将字节流转换回对象图的过程。在对象和字节流之间转换是很有用的机制。下面是一些例子。

> ① 本书将 object graph 翻译成“对象图”，对象图是一个抽象的概念，代表的是对象系统在特定时间点的一个视图。另一个常用的术语 object diagram 则是指总体 object graph 的一个子集。普通的对象模型(比如 UML 类图)描述的是对象之间的关系，而对象图侧重于它们的实例在特定时间点的状态。在面向对象应用程序中，相互关联的对象构成了一个复杂的网络。一个对象可能拥有或包含另一个对象，或者容纳了对另一个对象的引用。这样一来，不同的对象便相互链接起来了。这个对象网络便是对象图。它是一种比较抽象的结构，可在讨论应用程序的状态时使用它。注意，在 .NET Framework SDK 中文文档中，由对象相互连接而构成的对象图被称为“连接对象图形”。 ———— 译注

* 应用程序的状态(对象图)可轻松保存到磁盘文件或数据库中，并在应用层序下次运行时恢复。ASP.NET 就是利用序列化和反序列化来保存和还原会话状态的。

* 一组对象可轻松复制到系统的剪贴板，再粘贴回同一个或另一个应用程序，事实上， Windows 窗体和 Windows Presentation Foundation(WPF) 就利用了这个功能。

* 一组对象可克隆并放到一边作为“备份”；与此同时，用户操纵一组“主”对象。

* 一组对象可轻松地通过网络发送给另一台机器上运行的进程。Microsoft .NET Framework 的 Remoting(远程处理)架构会对按值封送(marshaled by value)的对象进行序列化和反序列化。这个技术还可跨 AppDomain 边界发送对象，具体如第 22 章“CLR 寄宿和 AppDomain”所述。

除了上述应用，一旦将对象序列化成内存中的字节流，就可方便地以一些更有用的方式处理数据，比如进行加密和压缩。

由于序列化如此有用，所以许多程序员耗费了大量时间写代码执行这些操作。历史上，这种代码很难编写，相当繁琐，还容易出错。开发人员需要克服的难题包括通信协议、客户端/服务器数据类型不匹配(比如低位优先/高位优先<sup>①</sup>问题)、错误处理、一个对象引用了其他对象、`in` 和 `out` 参数以及由结构构成的数组等。

> ① little-endian/big-endian，也译成小段和大端。 ———— 译注

让人高兴的是，.NET Framework 内建了出色的序列化和反序列化的支持。上述所有难题都迎刃而解，而且.NET Framework 是在后台悄悄帮你解决的。开发者现在只需负责序列化之前和反序列化之后的对象处理，中间过程由 .NET Framework 负责。

本章解释了 .NET Framework 如何公开它的序列化和序列化服务。对于几乎所有数据类型，这些服务的默认行为已经足够。也就是说，几乎不需要做任何工作就可以使自己打的类型“可序列化”。但对于少量类型，序列化服务的默认行为是不够的。幸好，序列化服务的扩展性极佳，本章将解释如何利用这些扩展性机制，在序列化或反序列化对象时采取一些相当强大的操作。例如，本章演示了如何将对象的“版本 1”序列化到磁盘文件，一年后把它反序列化成“版本2”的对象。

> 注意 本章重点在于 CLR 的运行时序列化技术。这种技术对 CLR 数据类型有很深刻的理解，能将对象的所有公共、受保护、内部甚至私有字段序列化到压缩的二进制流中，从而获得很好的性能。要把 CLR 数据类型序列化成 XML 流，请参见 `System.Runtime.Serialization.NetDataContractSerializer` 类。.NET Framework 还提供了其他序列化技术，它们主要是为 CLR 数据类型和非 CLR 数据类型之间的互操作而设计的。这些序列化技术用的是 `System.Xml.Serialization.XmlSerializer` 类和 `System.Runtime.Serialization.DataContractSerializer`类。

## <a name="24_1">24.1 序列化/反序列化快速入门</a>

下面先来看一些代码：

```C#
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

internal static class QuickStart {
    public static void Main() {
        // 创建对象图以便把它们序列化到流中
        var objectGraph = new List<String> { "Jeff", "Kristin", "Aidan", "Grant" };
        Stream stream = SerializeToMemory(objectGraph);

        // 为了演示，将一些都重置
        stream.Position = 0;
        objectGraph = null;

        // 反序列化对象，证明它能工作
        objectGraph = (List<String>) DeserializeFromMemory(stream);
        foreach (var s in objectGraph) Console.WriteLine(s);
    }

    private static MemoryStream SerializeToMemory(Object objectGraph) {
        // 构造流来容纳序列化的对象
        MemoryStream stream = new MemoryStream();

        // 构造序列化格式化器来执行所有真正的工作
        BinaryFormatter formatter = new BinaryFormatter();

        // 告诉格式化器将对象序列化到流中
        formatter.Serialize(stream, objectGraph);

        // 将序列化好的对象流返回给调用者
        return stream;
    }

    private static Object DeserializeFromMemory(Stream stream) {
        // 构造序列化格式化器来做所有真正的工作
        BinaryFormatter formatter = new BinaryFormatter();

        // 告诉格式化器从流中反序列化对象
        return formatter.Deserialize(stream);
    }
} 
```

一切似乎都很简单！`SerializeToMemory` 方法构造一个 `System.IO.MemoryStream` 对象。这个对象标明要将序列化好的字节块放到哪里。然后，方法构造一个 `BinaryFormatter` 对象(在 `System.Runtime.Serialization.Formatters.Binary` 命名空间中定义)。格式化器是实现了`System.Runtime.Serialization.IFormatter` 接口的类型，它知道如何序列化和反序列化对象图。FCL 提供了两个格式化器：`BinaryFormatter`(本例用的就是它)和 `SoapFormatter`(在 `System.Runtime.Serialization.Formatters.Soap`命名空间中定义，在 `System.Runtime.Serialization.Formatters.Soap.dll` 程序集中实现)。

> 注意 从 .NET Framework 3.5 开始便废了 `SoapFormatter` 类，不要在生产代码中使用它。但在调试序列化代码时，它仍有一定用处，因为它能生成便于阅读的 XML 文本。要在生产代码中使用 XML 序列化和反序列化，请参见 `XmlSerializer` 和 `DataContractSerializer` 类。


序列化对象图只需调用格式化器的 `Serialize` 方法，并向它传递两样东西：对流对象的引用，以及对想要序列化的对象图的引用。流对象标识了序列化好的字节应放到哪里，它可以是从 `System.IO.Stream` 抽象基类派生的任何类型的对象。也就是说，对象图可序列化成一个 `MemoryStream`，`FileStream` 或者 `NetworkStream`等。

`Serialize` 的第二个参数是一个对象引用。这个对象可以是任何东西，可以是一个 `Int32`，`String`，`DateTime`，`Exception`，`List<String>` 或者 `Dictionary<Int32, DateTime>`等。`objectGraph` 参数引用的对象可引用其他对象。例如，`objectGraph` 可引用一个集合，而这个集合引用了一组对象。这些对象还可继续引用其他对象，调用格式化器的 `Serialize` 方法时，对象图中的所有对象都被序列化到流中。

格式化器参考对每个对象的类型进行描述的元数据，从而了解如何序列化完整的对象图。序列化时，`Serialize` 方法利用反射来查看每个对象的类型中都有哪些实例字段。在这些字段中，任何一个引用了其他对象，格式化器的 `Serialize` 方法就知道那些对象也要进行序列化。

格式化器的算法非常智能。它们知道如何确保对象图中的每个对象都只序列化一次。换言之，如果对象图中的两个对象相互引用，格式化器会检测到这一点，每个对象都只序列化一次，避免发生死循环。

在上述代码的 `SerializeToMemory` 方法中，当格式化器的 `Serialize` 方法返回后，`MemoryStream` 直接返回给调用者。应用程序可以按照自己希望的任何方式利用这个字节数组的内容。例如，可以把它保存到文件中、复制到剪贴板或者通过网络发送等。

`DeserializeFromMemory` 方法将流反序列化为对象图。该方法比用于序列化对象图的方法还要简单。在代码中，我构建了一个 `BinaryFormatter` ，然后调用它的 `Deserialize` 方法。这个方法获取流作为参数，返回对反序列化好的对象图中的根对象的一个引用。

在内部，格式化器的 `Deserialize` 方法检查流的内容，构造流中所有对象的实例，并初始化所有这些对象中的字段，使它们具有与当初序列化时相同的值。通常要将 `Deserialize` 方法返回的对象引用转型为应用程序期待的类型。

> 注意 下面是一个有趣而实用的方法，它利用序列化创建对象的深拷贝(或者说克隆体)：

```C#
private static Object DeepClone(Object original) {
    // 构造临时内存流
    using (MemoryStream stream = new MemoryStream()) {

        // 构造序列化格式化器来执行所有实际工作
        BinaryFormatter formatter = new BinaryFormatter();

        // 值一行在本章 24.6 节“流上下文” 解释
        formatter.Context = new StreamingContext(StreamingContextStates.Clone);

        // 将对象图序列化到内存流中
        formatter.Serialize(stream, original);

        // 反序列化前，定位到内存流的起始位置
        stream.Position = 0;
        
        // 将对象图反序列化成一组新对象，
        // 向调用者返回对象图(深拷贝)的根
        return formatter.Deserialize(stream);
    }
} 
```

有几点需要注意。首先，是由你来保证代码为序列化和反序列化使用相同的格式化器。例如，不要写代码用 `SoapFormatter` 序列化一个对象图，再用`BinaryFormatter`反序列化。`Deserialize` 如果解释不了流的内容会抛出 `System.Runtime.Serialization.SerializationException`异常。

其次，可将多个对象图序列化到一个流中，这是很有用的一个操作。假如，假定有以下两个类定义：

```C#
[Serializable] internal sealed class Customer   { /* ... */ }
[Serializable] internal sealed class Order      { /* ... */ } 
```

然后，在应用程序的主要类定义了以下静态字段：

```C#
private static List<Customer> s_customers       = new List<Customer>();
private static List<Order>    s_pendingOrders   = new List<Order>();
private static List<Order>    s_processedOrders = new List<Order>(); 
```

现在，可利用如下所示的方法将应用程序的状态序列化到单个流中：

```C#
private static void SaveApplicationState(Stream stream) {
    // 构造序列化格式化器来执行所有实际的工作
    BinaryFormatter formatter = new BinaryFormatter();

    // 序列化我们的应用程序的完整状态
    formatter.Serialize(stream, s_customers);
    formatter.Serialize(stream, s_pendingOrders);
    formatter.Serialize(stream, s_processedOrders);
}
```

要重新构建应用程序的状态，可以使用如下所示的一个方法反序列化状态：

```C#
private static void RestoreApplicationState(Stream stream) {
    // 构造序列化格式化器来执行所有实际的工作
    BinaryFormatter formatter = new BinaryFormatter();

    // 反序列化应用程序的完整状态(和序列化时的顺序一样)
    s_customers = (List<Customer>)    formatter.Deserialize(stream);
    s_pendingOrders = (List<Order>)   formatter.Deserialize(stream);
    s_processedOrders = (List<Order>) formatter.Deserialize(stream);
} 
```

最后一个主意事项与程序集有关。序列化对象时，类型的全名和类型定义程序集的全名会被写入流。`BinaryFormatter` 默认输出程序集的完整标识，其中包括程序集的文件名(无扩展名)、版本号、语言文化以及公钥信息。反序列化对象时，格式化器首先获取程序集标识信息。并通过调用 `System.Refleciton.Assembly` 的 `Load`方法(参见 23.1 节“程序集加载”)，确保程序集已加载到正在执行的 AppDomain 中。

程序集加载好之后，格式化器在程序集中查找与要反序列化的对象匹配的类型。找不到匹配类型就抛出异常，不再对更多的对象进行反序列化。找到匹配的类型，就创建类型的实例，并用流中包含的值对其字段进行初始化。如果类型中的字段与流中读取的字段名不完全匹配，就抛出 `SerializationException` 异常，不再对更多的对象进行反序列化。本章以后会讨论一些高级机制，它们允许你覆盖某些行为。

本节讲述了序列化和反序列化对象图的基础知识。之后的小节将讨论如何定义自己的可序列化类型。还讨论了如何利用一些机制对序列化和反序列化进行更好的控制。

> 重要提示 有的可扩展应用程序使用 `Assembly.LoadFrom` 加载程序集，然后根据加载的程序集中定义的类型来构造对象。这些对象序列化到流中是没有问题的。但在反序列化时，格式化器会调用 `Assembly` 的 `Load` 方法(而非 `LoadFrom` 方法)来加载程序集。大多数情况下，CLR 都将无法定位程序集文件，从而造成 `SerializationException` 异常。许多开发人员对这个结果深感不解。序列化都能正确进行，他们当然预期反序列化也是正确的。

> 如果应用程序使用 `Assembly.LoadFrom` 加载程序集，再对程序集中定义的类型进行序列化，那么在调用格式化器的 `Deserialize` 方法之前，我建议你实现一个方法，它的签名要匹配 `System.ResolveEventHandler` 委托，并向 `System.AppDomain` 的 `AssemblyResolve` 事件注册这个方法。(`Deserialize` 方法返回后，马上向事件注销这个方法。)现在，每次格式化器加载一个程序集失败，CLR 都会自动调用你的 `ResolveEventHandler` 方法。加载失败的程序集的标识(Identity)会传给这个方法。方法可以从程序集的标识中提取程序集文件名，并用这个名称来构造路径，使应用程序知道去哪里寻找文件。然后，方法可调用 `Assembly.LoadFrom` 加载程序集，最后返回对结果程序集的引用。

## <a name="24_2">24.2 使类型可序列化</a>

设计类型时，设计人员必须珍重地决定是否允许类型的实例序列化。类型默认是不可序列化对的。例如，以下代码可能不会像你希望的那样工作：

```C#
internal struct Point { public Int32 x, y; }

private static void OptInSerialization() {
    Point pt = new Point { x = 1, y = 2 };
    using (var stream = new MemoryStream()) {
        new BinaryFormatter().Serialize(stream, pt); // 抛出 SerializationException
    }
} 
```

格式化器的 `Serialize` 方法会抛出 `System.Runtime.Serialization.SerializationException` 异常。问题在于，`Point` 类型的开发人员没有显式地指出 `Point` 对象可以序列化。为了解决这个问题，开发者必须像下面这样向类型应用定制特性 `System.SerializableAttribute`（注意该特性在 `System` 而不是 `System.Runtime.Serialization`命名空间中定义）。

```C#
[Serializable]
internal struct Point { public Int32 x, y; }
```

重新生成并运行，就会像预期的那样工作，`Point` 对象会顺利序列化到流中。序列化对象图时，格式化器会确认每个对象的类型都是可序列化的。任何对象不可序列化，格式化器的 `Serialize` 方法都会抛出 `SerializationException` 异常。

> 注意 序列化对象图时，也许有的对象的类型能序列化，有的不能。考虑到性能，在序列化之前，格式化器不会验证对象图中的所有对象都能序列化。所以，序列化对象图时，在抛出 `SerializationException` 异常之前，完全有可能已经有一部分对象序列化到流中。如果发生这种情况，流中就会包含已损坏的数据。序列化对象图时，如果你认为也许有一些对象不可序列化，那么写的代码就应该能得体地从这种情况中恢复。一个方案是先将对象序列化到一个 `MemoryStream` 中。然后，如果所有对象都成功序列化，就可以将 `MemoryStream` 中的字节复制到你真正希望的目标流中(比如文件和网络)。

`SerializableAttribute` 这个定制特性只能应用于引用类型(`class`)、值类型(`struct`)、枚举类型(`enum`)和委托类型(`delegate`)。注意，枚举和委托类型总是可序列化的，所以不必显式应用 `SerializableAttribute` 特性。除此之外，`SerializableAttribute` 特性不会被派生类型继承。所以，给定以下两个类型定义，那么 `Person` 对象可序列化，`Employee` 对象则不可：

```C#
[Serializable]
internal class Person { ... }

internal class Employee : Person { ... }
```

解决方案是也向 `Employee` 类型应用 `SerializableAttribute` 特性：

```C#
[Serializable]
internal class Person { ... }

[Serializable]
internal class Employee : Person { ... } 
```

上述问题很容易修正，反之则不然。如果基类型没有应用 `SerializableAttribute` 特性，那么很难想象如何从它派生出可序列化的类型。但这样设计是有原因的；如果基类型不允许它的实例序列化，它的字段就不能序列化，因为基对象实际是派生对象的一部分。这正是为什么 `System.Object` 已经很体贴地应用了 `SerializableAttribute` 特性的原因。

> 注意 一般建议将你定义的大多数类型都设置成可序列化。毕竟，这样能为类型的用户提供很大的灵活性。但必须注意的是，序列化会读取对象的所有字段，不管这些字段声明为 `public`，`protected`，`internal` 还是 `private`。如果类型的实例要包含敏感或安全数据(比如密码)，或者数据在转移之后便没有含义或者没有值，就不应使类型变得可序列化。

> 如果使用的类型不是为序列化而设计的，而且手上没有类型的源代码，无法从源头添加序列化支持，也不必气馁。在本章最后的 24.9 节“反序列化对象时重写程序集和/或类型”中，我会解释如何使任何不可序列化的类型变得可序列化。

## <a name="24_3">24.3 控制序列化和反序列化</a>

将 `SerializableAttribute` 定制特性应用于类型，所有实例字段(`public`，`private` 和 `protected`等)都会被序列化<sup>①</sup>。但类型可能定义了一些不应序列化的实例字段。一般有两个原因造成我们不想序列化部分实例字段。

> ① 在标记了 `[Serializable]` 特性的类型中，不要用 C#的“自动实现的属性”功能来定义属性。这是由于字段名是由编译器自动生成的，而生成的名称每次重新编译代码时都不同。这会阻止类型被反序列化。详情参见 10.1.1 节“自动实现的属性”。

* 字段含有反序列化后变得无效的信息。例如，假定对象包含 Windows 内核对象(如文件、进程、线程、互斥体、事件、信号量等)的句柄，那么在反序列化到另一个进程或另一台机器之后，就会失去意义。因为 Windows 内核对象是跟进程相关的值。

* 字段含有很容易计算的信息。这时要选出那些无须序列化的字段，减少需要传输的数据，增强应用程序的性能。

以下代码使用 `System.NonSerializedAttribute` 定制特性指出类型中不应序列化的字段。注意，该特性也在 `System`(而非 `System.Runtime.Serialization`)命名空间中定义。

```C#
[Serializable]
internal class Circle {
    private Double m_radius;        // 半径

    [NonSerialized]
    private Double m_area;          // 面积

    public Circle(Double radius) {
        m_radius = radius;
        m_area = Math.PI * m_radius * m_radius;
    }

    ...
}
```

在上述代码中，`Circle` 的对象可以序列化。但格式化器只会序列化对象的 `m_radius` 字段的值。`m_area` 字段的值不会被序列化，因为该字段已应用了 `NonSerializedAttribute` 特性。注意，该特性只能应用于类型中的字段，而且会被派生类型继承。当然，可向一个类型中的多个字段应用 `NonSerializedAttribute` 特性。

假定代码像下面这样构造了一个 `Circle` 对象：

`Circle c = new Circle(10);`

在内部，`m_area` 字段会设置成一个约为 314.159 的值。这个对象序列化时，只有 `m_radius` 字段的值(10) 才会写入流。这正是我们希望的，但当流反序列化成 `Circle` 对象的 `m_radius` 字段会被设为 10，但它的 `m_area` 字段会被初始化成 0 ———— 而不是 314.159！

以下代码演示了如何修改 `Circle` 类型来修正这个问题：

```C#
[Serializable]
internal class Circle {
    private Double m_radius;            // 半径

    [NonSerialized]
    private Double m_area;              // 面积

    public Circle(Double radius) {
        m_radius = radius;
        m_area = Math.PI * m_radius * m_radius;
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context) {
        m_area = Math.PI * m_radius * m_radius;
    }
} 
```

修改过的 `Circle` 类包含一个标记了 `System.Runtime.Serialization.OnDeserializedAttribute` 定制特性的方法<sup>①</sup>。每次反序列化类型的实例，格式化器都会检查类型中是否定义了应用了该特性的方法。如果是，就调用该方法。调用这个方法时，所有可序列化的字段都会被正确设置。在该方法中，可能需要访问这些字段来执行一些额外的工作，从而确保对象的完全反序列化。

> ① 要在对象反序列化时调用一个方法，`System.Runtime.Serialization.OnDeserialized` 定制特性是首选方案，而不是让类型实现 `System.Runtime.Serialization.IDeserializationCallback` 接口的 `OnDeserialization`方法。

在上述 `Circle` 修改版本中，我调用 `OnDeserialized` 方法，使用 `m_radius` 字段来计算圆的面积，并将结果放到 `m_area` 字段中。这样 `m_area` 就有了我们希望的值(314.159)。

除了 `OnDeserializedAttribute` 这个定制特性，`System.Runtime.Serialization` 命名空间还定义了包括 `OnSerializingAttribute`，`OnSerializedAttribute` 和 `OnDeserializingAttribute` 在内的其他定制特性。可将它们应用于类型中定义的方法，对序列化和反序列化过程进行更多的控制。在下面这个类中，这些特性被应用于不同的方法：

```C#
[Serializable]
public class MyType {
    Int32 x, y; [NonSerialized] Int32 sum;

    public MyType(Int32 x, Int32 y) {
        this.x = x; this.y = y; sum = x + y;
    }

    [OnDeserializing]
    private void OnDeserializing(StreamingContext context) {
        // 举例：在这个类型的新版本中，为字段设置默认值
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context) {
        // 举例：根据字段值初始化瞬时状态(比如 sum 的值)
        sum = x + y;
    }

    [OnSerializing]
    private void OnSerializing(StreamingContext context) {
        // 举例：在序列化前，修改任何需要修改的状态
    }

    [OnSerialized]
    private void OnSerialized(StreamingContext context) {
        // 举例：在序列化后，恢复任何需要恢复的状态
    }
}
```

使用这 4 个属性中的任何一个时，你定义的方法必须获取一个 `StreamingContext` 参数(在本章后面的 24.6 节“流上下文“中讨论)并返回 `void`。方法名可以是你希望的任何名称。另外，应将方法声明为 `private`，以免它被普通的代码调用；格式化器运行时有充足的安全权限，所以能调用私有方法。

> 注意 序列化一组对象时，格式化器首先调用对象的标记了 `OnSerializing` 特性的所有方法。接着，它序列化对象的所有字段。最后，调用对象的标记了 `OnSerialized` 特性的所有方法。类似地，反序列化一组对象时，格式化器首先调用对象的标记了 `OnDeserializing` 特性的所有方法。然后，它反序列化对象的所有字段。最后，它调用对象的标记了 `OnDeserialized` 特性的所有方法。

> 还要注意，在反序列化期间，当格式化器看到类型提供的一个方法标记了 `OnDeserialized` 特性时，格式化器会将这个对象的引用添加到一个内部列表中。所有对象都反序列化之后，格式化器反向遍历列表，调用每个对象的 `OnDeserialized` 方法，调用这个方法后，所有可序列化的字段都会被正确设置，可访问这些字段来执行任何必要的、进一步的工作，从而将对象完整地反序列化。之所以要以相反的顺序调用这些方法，因为这样才能使内层对象先于外层对象完成反序列化。

> 例如，假定一个集合对象(比如 `Hashtable` 或 `Dictionary`)内部用一个哈希表维护它的数据项列表。集合对象类型可实现一个标记了 `OnDeserialized` 特性的方法。即使集合对象先反序列化(先于它包含的数据项)，它的 `OnDeserialized` 方法也会最后调用(在调用完它的数据项的所有 `OnDeserialized` 方法之后)。这样一来，所有数据项在反序列化后，它们的所有字段都能得到正确的初始化，以便计算出一个好的哈希码值。然后，集合对象创建它的内部哈希桶，并利用数据项的哈希码将数据项放到桶中。本章稍后的 24.5 节”控制序列化/反序列化的数据“会提供一个例子，它展示了 `Dictionary` 类如何利用这个技术。

如果序列化类型的实例，在类型中添加新字段，然后试图反序列化不包含新字段的对象，格式化器会抛出 `SerializationException` 异常，并显示一条消息告诉你流中要反序列化的数据包含错误的成员数目。这非常不利于版本控制，因为我们经常都要在类型的新版本中添加新字段。幸好，这时可以利用 `System.Runtime.Serialization.OptionalFieldAttribute` 特性。

类型中新增的每个字段都要应用 `OptionalFieldAttribute` 特性。然后，当格式化器看到该特性应用于一个字段时，就不会因为流中的数据不包含这个字段而抛出 `SerializationException`.

## <a name="24_4">24.4 格式化器如何序列化类型实例</a>

本节将深入探讨格式化器如何序列化对象的字段。掌握这些知识后，可以更容易地理解本章后面要解释的一些更高级的序列化和反序列化技术。

为了简化格式化器的操作，FCL 在 `System.Runtime.Serialization` 命名空间提供了一个 `FormatterServices` 类型。该类型只包含静态方法，而且该类型不能实例化。以下步骤描述了格式化器如何自动序列化类型应用了 `SerializableAttribute`特性的对象。

1. 格式化器调用 `FormatterServices` 的 `GetSerializableMembers` 方法：  
   `public static MemberInfo[] GetSerializableMembers(Type type, StreamingContext context);`  
   这个方法利用反射获取类型的 `public` 和 `private` 实例字段(标记了 `NonSerializedAttribute` 特性的字段除外)。方法返回由 `MemberInfo` 对象构成的数组，其中每个元素都对应一个可序列化的实例字段。

2. 对象被序列化，`System.Reflection.MemberInfo` 对象数组传给 `FormatterServices` 的静态方法 `GetObjectData`:  
   `public static Object[] GetObjectData(Object obj, MemberInfo[] members); `  
    这个方法返回一个 `Object` 数组，其中每个元素都标识了被序列化的那个对象中的一个字段的值。这个 `Object` 数组和 `MemberInfo` 数组是并行(parallel)的；换言之，`Object` 数组中元素 0 是 `MemberInfo` 数组中的元素 0 所标识的那个成员的值。

3. 格式化器将程序集标识和类型的完整名称写入流中。

4. 格式化器然后遍历两个数组中的元素，将每个成员的名称和值写入流中。

以下步骤描述了格式化器如何自动反序列化类型应用了 `SerializableAttribute` 特性的对象。

1. 格式化器从流中读取程序集标识和完整类型名称。如果程序集当前没有加载到 AppDomain 中，就加载它(这一点前面已经讲过了)。如果程序集不能加载，就抛出一个 `SerializationException` 异常，对象不能反序列化。如果程序集已加载，格式化器将程序集标识信息和类型全名传给 `FormatterServices` 的静态方法 `GetTypeFromAssembly`:  
  `public static Type GetTypeFromAssembly(Assembly assem, String name);`  
  这个方法返回一个 `System.Type` 对象，它代表要反序列化的那个对象的类型。

2. 格式化器调用 `FormmatterServices` 的静态方法 `GetUninitializedObject`:  
   `public static Object GetUninitializedObject(Type type);`  
   这个方法为一个新对象分配内存，但不为对象调用构造器。然而，对象的所有字节都被初始为 `null` 或 `0`。

3. 格式化器现在构造并初始化一个 `MemberInfo` 数组，具体做法和前面一样，都是调用 `FormatterServices` 的 `GetSerializableMembers` 方法。这个方法返回序列化好、现在需要反序列化的一组字段。

4. 格式化器根据流中包含的数据创建并初始化一个 `Object` 数组。

5. 将新分配对象、`MemberInfo` 数组以及并行 `Object` 数组(其中包含字段值)的引用传给 `FormatterServices` 的静态方法 `PopulateObjectMembers`：  
  `public static Object PopulateObjectMembers(Object obj, MemberInfo[] members, Object[] data);`  
   这个方法遍历数组，将每个字段初始化成对应的值。到此为止，对象就算是被彻底反序列化了。

## <a name="24_5">24.5 控制序列化/反序列化的数据</a>

本章前面讨论过，控制序列化和反序列化过程的最佳方式就是使用 `OnSerializing`，`OnSerialized`，`OnDeserializing`，`OnDeserialized`，`NonSerialized` 和 `OptionalField` 等特性。然而，在一些极少见的情况下，这些特性不能提供你想要的全部控制。此外，格式化器内部使用的是反射，而反射的速度是比较慢的，这会增大序列化和反序列化对象所花的时间，为了对序列化/反序列化的数据进行完全的控制，并避免使用反射，你的类型可实现`System.Runtime.Serialization.ISerializable`接口，它的定义如下：

```C#
public interface ISerializable {
    void GetObjectData(SerializationInfo info, StreamingContext context);
}
```

这个接口只有一个方法，即 `GetObjectData`。但实现这个接口的大多数类型还实现了一个特殊的构造器，我稍后会详细描述它。

> 重要提示 `ISerializable` 接口最大的问题在于，一旦类型实现了它，所有派生类型也必须实现它，而且派生类型必须保证调用基类的 `GetObjectData` 方法和特殊构造器。此外，一旦类型实现了该接口，便永远不能删除它，否则会失去与派生类型的兼容性。所以，密封类实现 `ISerializable` 接口是最让人放心的。使用本章前面描述的各种定制特性，`ISerializable` 接口的所有问题都可以避免。

> 重要提示 `ISerializable` 接口和特殊构造器旨在由格式化器使用。但其他代码可能调用 `GetObjectData` 来返回敏感数据。另外，其他代码可能构造对象，并传入损坏的数据。因此，建议向 `GetObjectData` 方法和特殊构造器应用以下特性：
  `[SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]`

格式化器序列化对象图时会检查每个对象。如果发现一个对象的类型实现了 `ISerializable` 接口，就会忽略所有定制特性，改为构造新的 `System.Runtime.Serialization.SerializationInfo` 对象。该对象包含了要以对象序列化的值的集合。

构造 `SerializationInfo` 对象时，格式化器要传递两个参数：`Type` 和 `System.Runtime.Serialization.IFormatterConverter`。`Type`参数标识要序列化的对象。唯一性地标识一个类型需要两个部分的信息：类型的字符串名称及其程序集标识(包括程序集名、版本、语言文化和公钥)。构造好的 `SerializationInfo` 对象包含类型的全名(通过在内部查询 `Type` 的 `FullName`属性)，这个字符串会存储到一个私有字段中，如果你想获取类型的全名，可查询 `SerializationInfo` 的 `FullTypeName` 属性。类似地，构造器获取类型的定义程序集(通过在内部查询 `Type` 的 `Module` 属性，再查询 `Module` 的 `Assembly` 属性，再查询 `Assembly` 的 `FullName`属性)，这样个字符串会存储在一个私有字段中。如果你想获取程序集的标识，可查询 `SerializationInfo` 的 `AssemblyName` 属性。

> 注意 虽然可以设置一个 `SerializationInfo` 的 `FullTypeName` 和 `AssemblyName` 属性，但不建议这样做。如果想要更改被序列化的类型，建议调用 `SerializationInfo` 的 `SetType` 方法，传递对目标 `Type` 对象的引用。调用 `SetType` 可确保类型的全名和定义程序集被正确设置。本章后面的 24.7 节“类型序列化为不同类型以及对象反序列化为不同对象”将展示调用 `SetType` 的一个例子。

构造好并初始化好 `SerializationInfo` 对象后，格式化器调用类型的 `GetObjectData` 方法，向它传递对 `SerializationInfo` 对象的引用。`GetObjectData` 方法决定需要哪些信息来序列化对象，并将这些信息添加到 `SerializationInfo` 对象中。`GetObjectData` 调用 `SerializationInfo` 类型提供的 `AddValue` 方法的众多重载版本之一指定要序列化的信息。针对要添加的每个数据，都要调用一次 `AddValue`。

以下代码展示了 `Dictionary<TKey, TValue>` 类型如何实现 `ISerializable` 和 `IDeserializationCallback` 接口来控制其对象的序列化和反序列化。

```C#
[Serializable]
public class Dictionary<TKey, TValue> : ISerializable, IDeserializationCallback {
    // 私有字段放在这里(未列出)
    private SerializationInfo m_siInfo; // 只用于反序列化

    // 用于控制反序列化的特殊构造器(这是 ISerializable 需要的)
    [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
    protected Dictionary(SerializationInfo info, StreamingContext context) {
        // 反序列化期间，为 OnDeserialization 保存 SerializationInfo
        m_siInfo = info;
    }

    // 用于控制序列化的方法
    [SecurityCritical]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {

        info.AddValue("Version", m_version);
        info.AddValue("Comparer", m_comparer, typeof(IEqualityComparer<TKey>));
        info.AddValue("HashSize", (m_ buckets == null) ? 0 : m_buckets.Length);
        if (m_buckets != null) {
            KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[Count];
            CopyTo(array, 0);
            info.AddValue("KeyValuePairs", array, typeof(KeyValuePair<TKey, TValue>[]));
        }
    }

    // 所有 key/value 对象都反序列化好之后调用的方法
    public virtual void IDeserializationCallback.OnDeserialization(Object sender) {
        if (m_siInfo == null) return; // 从不设置，直接返回

        Int32 num = m_siInfo.GetInt32("Version");
        Int32 num2 = m_siInfo.GetInt32("HashSize");
        m_comparer = (IEqualityComparer<TKey>)
            m_siInfo.GetValue("Comparer", typeof(IEqualityComparer<TKey>));
        if (num2 != 0) {
            m_buckets = new Int32[num2];
            for (Int32 i = 0; i < m_buckets.Length; i++) m_buckets[i] = -1;
            m_entries = new Entry<TKey, TValue>[num2];
            m_freeList = -1;
            KeyValuePair<TKey, TValue>[] pairArray = (KeyValuePair<TKey, TValue>[])
                m_siInfo.GetValue("KeyValuePairs", typeof(KeyValuePair<TKey, TValue>[]));
            if (pairArray == null)
                ThrowHelper.ThrowSerializationException(
                    ExceptionResource.Serialization_MissingKeys);

            for (Int32 j = 0; j < pairArray.Length; j++) {
                if (pairArray[j].Key == null)
                    ThrowHelper.ThrowSerializationException(
                    ExceptionResource.Serialization_NullKey);

                Insert(pairArray[j].Key, pairArray[j].Value, true);
            }
        } else { m_buckets = null; }
        m_version = num;
        m_siInfo = null;
    }
}
```

每个 `AddValue` 方法都获取一个 `String` 名称好一些数据。数据一般是简单的值类型，比如 `Boolean`，`Char`，`Byte`，`SByte`，`Int16`，`Int32`，`UInt32`，`Int64`，`UInt64`，`Single`，`Double`，`Decimal` 或者 `DateTime`。然而，还可以在调用 `AddValue` 时向它传递对一个 `Object`(比如一个`String`)的引用。`GetObjectData` 添加好所有必要的序列化信息之后，会返回至格式化器。

> 注意 务必调用 `AddValue` 方法的某个重载版本为自己的类型添加序列化信息。如果一个字段的类型实现了 `ISerializable` 接口，就不要在字段上调用 `GetObjectData`。相反，调用 `AddValue` 来添加字段；格式化器会注意到字段的类型实现了 `ISerializable`，会帮你调用 `GetObjectData`。如果自己在字段对象上调用 `GetObjectData`，格式化器便不知道在对流进行反序列化时创建新对象。

现在，格式化器获取已经添加到 `SerializationInfo` 对象的所有值，并把它们都序列化到流中。注意，我们还向 `GetObjectData` 方法传递了另一个参数，也就是对一个 `System.Runtime.Serialization.StreamingContext` 对象的引用。大多数类型的 `GetObjectData` 方法都会完全忽略这个参数，所以我现在不准备讨论它。相反，我准备把它放到本章后面的 24.6 节“流上下文”讨论。

知道了如何设置序列化所需的全部信息之后，再来看反序列化。格式化器从流中提取一个对象时，会为新对象分配内存(通过调用 `System.Runtime.Serialize.FormatterServices` 类型的静态 `GetUninitializedObject` 方法)。最初，这个对象的所有字段都设为 `0` 或 `null`。然后，格式化器检查类型是否实现了 `ISerializable` 接口。如果存在这个接口，格式化器就尝试调用一个特殊构造器，它的参数和 `GetObjectData` 方法的完全一致。

如果你的类是密封类，强烈建议将这个特殊构造器声明为 `private`。这样可防止任何代码不慎调用它，从而提升安全性。如果不是密封类，应该将这个特殊构造器声明为 `protected`，确保只有派生类才能调用。注意，无论这个特殊构造器是如何声明的，格式化器都能调用它。

构造器获取一个 `SerializationInfo` 对象引用。在这个 `SerializationInfo` 对象中，包含了对象序列化时添加的所有值。特殊构造器可调用 `GetBoolean`，`GetChar`，`GetByte`，`GetSByte`，`GetInt16`，`GetUInt16`，`GetInt32`，`GetUInt32`，`GetInt64`，`GetUInt64`，`GetSingle`，`GetDouble`，`GetDecimal`，`GetDateTime`，`GetString` 和 `GetValue` 等任何一个方法，向它传递与序列化一个值所用的名称对应的字符串。上述每个方法返回的值再用于初始化新对象的各个字段。

反序列化对象的字段时，应调用和对象序列化时传给 `AddValue` 方法的值的类型匹配的 `Get` 方法。换言之，如果 `GetObjectData` 方法调用 `GetInt32` 方法。如果值在流中的类型和你试图获取(Get)的类型不符，格式化器会尝试用一个 `IFormatterConverter` 对象将流中的值转型成你指定的类型。

前面说过，构造 `SerializationInfo` 对象时，要向它传递类型实现了 `IFormatterConverter` 接口的一个对象。由于是格式化器负责构造 `SerializationInfo` 对象，所以要由它选择它想要的 `IFormatterConverter` 类型。 Microsoft 的 `BinaryFormatter` 和 `SoapFormatter` 类型总是构造 `System.Runtime.Serialization.FormatterConverter` 类型的实例。Microsoft 的格式化器没有提供任何方式让你选择不同的 `IFormatterConverter` 类型。

`FormatterConverter` 类型调用 `System.Convert` 类的各种静态方法在不同的核心类型之间对值进行转换，比如将一个 `Int32` 转换成一个 `Int64`。然而，为了在其他任意类型之间转换一个值，`FormatterConverter` 要调用 `Convert` 的 `ChangeType` 方法将序列化好的(或者原始的)类型转型为一个 `IConvertible` 接口，再调用恰当的接口方法。所以，要允许一个可序列化类型的对象反序列化成一个不同的类型，可考虑让自己的类型实现 `IConvertible` 接口。注意，只有在反序列化对象时调用一个 `Get` 方法，但发现它的类型和流中的值的类型不符时，才会使用 `FormatterConverter` 对象。

特殊构造器也可以不调用上面列出的各个 `Get` 方法，而是调用 `GetEnumerator`。该方法返回一个 `System.Runtime.Serialization.SerializationInfoEnumerator` 对象，可用该对象遍历 `SerializationInfo` 对象中包含的所有值。枚举的每个值都是一个 `System.Runtime.Serialization.SerializationEntry` 对象。

当然，完全可以定义自己的类型，让它从实现了 `ISerializable` 的 `GetObjectData` 方法和特殊构造器类型派生。如果你的类型也实现了 `ISerializable`，那么在你实现的 `GetObjectData` 方法和特殊构造器中，必须调用基类中的同名方法，确保对象能正确序列化和反序列化。这一点务必牢记，否则对象是不能正确序列化和反序列化的。下一节将解释如何正确地定义基类型未实现 `ISerializable` 接口一个 `ISerializable` 类型。

如果你的派生类型中没有任何额外的字段，因而没有特殊的序列化/反序列化需求，就完全不必实现 `ISerializable`。和所有接口成员相似，`GetObjectData` 是 `virtual` 的，调用它可以正确地序列化对象。此外，格式化器将特殊构造器视为“已虚拟化”(virtualized)。换言之，反序列化期间，格式化器会检查要实例化的类型。如果那个类型没有提供特殊构造器，格式化器会扫描基类，直到它找到实现了特殊构造器的一个类。

> 重要提示 特殊构造器中的代码一般从传给它的 `SerializationInfo` 对象中提取字段。提取字段后，不保证对象已完全反序列化，所以特殊构造器中的代码不应该尝试操作它提取的对象。

> 如果你的类型必须访问提取的对象中的成员(比如调用方法)，建议你的类型提供一个应用了 `OnDeserialized` 特性的方法，或者让类型实现 `IDeserializationCallback` 接口的 `OnDeserialization` 方法(就像前面的 `Dictionary` 示例中那样)。调用该方法时，所有对象的字段都已设置好。然而，对于多个对象来说，它们的 `OnDeserialized` 或 `OnDeserialization` 方法的调用顺序是没有保障的。所以，虽然字段可能已初始化，但你仍然不知道被引用的对象是否已完全反序列化好(如果那个被引用的对象也提供了一个 `OnDeserialized` 方法或者实现了 `IDeserializationCallback`)。

### 要实现 `ISerializable` 但基类型没有实现怎么办？

前面讲过，`ISerializable` 接口的功能非常强大，允许类型完全控制如何对类型的实例进行序列化和反序列化。但这个能力是有代价的：现在，该类型还要负责它的基类型的所有字段的序列化。如果基类型也实现了 `ISerializable` 接口，那么对基类型的字段进行序列化是很容易的。调用基类型的 `GetObjectData` 方法即可。

总有一天需要定义类型来控制它的序列化，但发现它的基类没有实现 `ISerializable` 接口。在这种情况下，派生类必须手动序列化基类的字段，具体的做法是获取它们的值，并把这些值添加到 `SerializationInfo` 集合中。然后，在你的特殊构造器中，还必须从集合中取出值，并以某种方式设置基类的字段。如果基类的字段是 `public` 或 `protected` 的，那么一切都很容易实现。如果是 `private` 字段，就很难或者根本不可能实现。

以下代码演示了如何正确实现 `ISerializable` 的 `GetObjectData` 方法和它的隐含的构造器，使基类的字段能被序列化：

```C#
[Serializable]
internal class Base {
    protected String m_name = "Jeff";
    public Base() { /* Make the type instantiable */ }
}

[Serializable]
internal sealed class Derived : Base, ISerializable {
    private DateTime m_date = DateTime.Now;
    public Derived() { /* Make the type instantiable*/ }

    // 如果这个构造器不存在，便会引发一个 SerializationException 异常、
    // 如果这个类不是密封类，这个构造器就应该是 protected 的
    [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
    private Derived(SerializationInfo info, StreamingContext context) {
        // 为我们的类和基类获取可序列化的成员集合
        Type baseType = this.GetType().BaseType;
        MemberInfo[] mi = FormatterServices.GetSerializableMembers(baseType, context);

        // 从 info 对象反序列化基类的字段
        for (Int32 i = 0; i < mi.Length; i++) {
            // 获取字段，并把它设为反序列化好的值
            FieldInfo fi = (FieldInfo)mi[i];
            fi.SetValue(this, info.GetValue(baseType.FullName + "+" + fi.Name, fi.FieldType));
        }

        // 反序列化为这个类序列化的值
        m_date = info.GetDateTime("Date");
    }

    [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
        // 为这个类序列化希望的值
        info.AddValue("Date", m_date);

        // 获取我们的类和基类的可序列化的成员
        Type baseType = this.GetType().BaseType;
        MemberInfo[] mi = FormatterServices.GetSerializableMembers(baseType, context); 
        
        // 将基类的字段序列化到 info 对象中
        for (Int32 i = 0; i < mi.Length; i++) {
            // 为字段名附加基类型全名作为前缀
            info.AddValue(baseType.FullName + "+" + mi[i].Name,
                ((FieldInfo)mi[i]).GetValue(this));
        }
    }

    public override String ToString() {
        return String.Format("Name={0}, Date={1}", m_name, m_date);
    }
} 
```

上述代码有一个名为 `Base` 的基类，它只用 `SerializableAttribute` 定制特性进行了标识。从 `Base` 派生的是 `Derived` 类，它除了也用`SerializableAttribute` 特性进行标识，还实现了 `ISerializable` 接口。为了使局面变得更有趣，两个类都定义了名为 `m_name` 的一个`String` 字段。调用 `SerializationInfo` 的 `AddValue` 方法时不能添加多个同名的值。在上述代码中，解决这个问题的方案是在字段名前附加类名作为前缀，从而对每个字段进行标识。例如，当 `GetObjectData` 方法调用 `AddValue` 来序列化 `Base` 的 `m_name` 字段时，写入的值的名称是“`Base+m_name`”。

## <a name="24_6">24.6 流上下文</a>

前面讲过，一组序列化好的对象可以有许多目的地：同一个进程、同一台机器上的不同进程、不同机器上的不同进程等。在一些比较少见的情况下，一个对象可能想知道它要在什么地方反序列化，从而以不同的当时生成它的状态。例如，如果对象中包装了 Windows 信号量(semaphore)对象，如果它知道要反序列化到同一个进程中，就可决定对它的内核句柄(kernel handle)进行序列化，这是因为内核句柄在一个进程中有效。但如果要反序列化到同一台计算机的不同进程中，就可决定对信号量的字符串名称进行序列化。最后，如果要反序列化到不同计算机上的进程，就可决定抛出异常，因为信号量只在一台机器内有效。

本章提到的大量方法都接受一个 `StreamingContext`(流上下文)。`StreamingContext` 结构是一个非常简单的值类型，它只提供了两个公共只读属性，如表 24-1 所示。

表 24-1 `StreamingContext` 的公共只读属性
|成员名称|成员类型|说明|
|---|---|---|
|`State`|`StreamingContextStates`|一组位标志(bit flag)，指定要序列化/反序列化的对象的来源或目的地|
|`Context`|`Object`|一个对象引用，对象中包含用户希望的任何上下文信息|

接受一个 `StreamingContext` 结构的方法能检查 `State` 属性的位标志，判断要序列化/反序列化的对象的来源或目的地。表 24-2 展示了可能的位标志值。

表 24-2 `StreamingContextStates` 的标志

|标志名称|标志值|说明|
|---|---|---|
|`CrossProcess`|0x0001|来源或目的地是同一台机器的不同进程|
|`CrossMachines`|0x0002|来源或目的地在不同机器上|
|`File`|0x0004|来源或目的地是文件。不保证反序列化数据的是同一个进程|
|`Persistence`|0x0008|来源或目的地是存储(store)，比如数据库或文件。不保证反序列化数据的是同一个进程|
|`Remoting`|0x0010|来源或目的地是远程的未知位置。这个位置可能在(也可能不在)同一台机器上|
|`Other`|0x0020|来源或目的地未知|
|`Clone`|0x0040|对象图被克隆。序列化代码可认为是由同一进程对数据进行反序列化，所以可安全地访问句柄或其他非托管资源|
|`CrossAppDomain`|0x0080|来源或目的地是不同的 AppDomain|
|`All`|0x00FF|来源或目的地可能是上述任何一个上下文。这是默认设定|

知道如何获取这些信息后，接着讨论如何设置。`IFormatter` 接口(同时由 `BinaryFormatter` 和 `SoapFormatter` 类型实现)定义了`StreamingContext` 类型的可读/可写属性 `Context`。构造格式化器时，格式化器会初始化它的 `Context` 属性，将 `StreamingContextStates` 设为 `All`，将对额外状态对象的引用设为 `null`。

格式化器构造好之后，就可以使用任何 `StreamingContextStates` 位标志来构造一个 `StreamingContext` 结构，并可选择传递一个对象引用(对象中包含你需要的任何额外的上下文信息)。现在，在调用格式化器的 `Serialize` 或 `Deserialize` 方法之前，你只需要将格式化器的 `Context` 属性设为这个新的 `StreamingContext` 对象。在本章前面的 24.1 节“序列化/反序列化快速入门”中，已通过 `DeepClone` 方法演示了如何告诉格式化器，对一个对象图进行序列化/反序列化的唯一目的就是克隆对象图中的所有对象。

## <a name="24_7">24.7 将类型序列化为不同的类型以及将对象反序列化为不同的对象</a>

.NET Framework 的序列化架构是相当全面的，本节要讨论如何设计类型将自己序列化或反序列化成不同的类型或对象。下面列举了一些有趣的例子。

* 有的类型(比如 `System.DBNull` 和 `System.Reflection.Missing`)设计成每个 AppDomain 一个实例。经常将这些类型称为**单实例**(singleton)类型。给定一个 `DBNull` 对象引用，序列化和反序列化它不应造成在 AppDomain 中新建一个 `DBNull` 对象。反序列化后，返回的引用应指向 AppDomain 中现有的 `DBNull` 对象。

* 对于某些类型(例如 `System.Type` 和 `System.Reflection.Assembly`，以及其他反射类型，例如 `MemberInfo`)，每个类型、程序集或者成员等都只能有一个实例。例如，假定一个数组中的每个元素都引用一个 `MemberInfo` 对象，其中 5 个元素引用的都是一个 `MemerInfo` 对象。序列化和反序列化这个数组后，那 5 个元素引用的应该还是一个 `MemberInfo` 对象(而不是分别引用 5 个不同的对象)。除此之外，这些元素引用的 `MemberInfo` 对象还必须实际对应于 AppDomain 中的一个特定成员。轮询数据库连接对象或者其他任何类型的对象时，这个功能也是很好用的。

* 对于远程控制的对象，CLR 序列化与服务器对象有关的信息。在客户端上反序列化时，会造成 CLR 创建一个代理对象。这个代理对象的类型有别于服务器对象的类型，但这对于客户端代码来说是透明的(客户端不需要关心这个问题)。客户端直接在代理对象上调用实例方法。然后，代理代码内部会调用远程发送给服务器，由后者实际执行请求的操作。

下面来看看一些示例代码，它们展示了如何正确地序列化和反序列化单实例类型：

```C#
// 每个 AppDomain 应该只有这个类型的一个实例
[Serializable]
public sealed class Singleton : ISerializable {
    // 这是该类型的一个实例
    private static readonly Singleton s_theOneObject = new Singleton();

    // 这些是实例字段
    public String Name = "Jeff";
    public DateTime Date = DateTime.Now;

    // 私有构造器，允许这个类型构造单实例
    private Singleton() { }

    // 该方法返回对单实例的引用
    public static Singleton GetSingleton() { return s_theOneObject; }

    // 序列化一个 Singleton 时调用的方法
    // 我建议在这里使用一个显式接口方法实现(EIMI)
    [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
        info.SetType(typeof(SingletonSerializationHelper));
        // 不需要添加其他值
    }

    [Serializable]
    private sealed class SingletonSerializationHelper : IObjectReference {
        // 这个方法在对象(它没有字段)
        public Object GetRealObject(StreamingContext context) {
            return Singleton.GetSingleton();
        }
    }
    // 注意：特殊构造器是不必要的，因为它永远不会调用
}
```

`Singleton` 类所带表的类型规定每个 AppDomain 只能存在它的一个实例。以下代码测试 `Singleton` 的序列化和反序列化代码，保证 AppDomain 中只有 `Singleton` 类型的一个实例：

```C#
private static void SingletonSerializationTest() {
    //  创建数组，其中多个元素引用一个 Singleton 对象
    Singleton[] a1 = { Singleton.GetSingleton(), Singleton.GetSingleton() };
    Console.WriteLine("Do both elements refer to the same object? "
        + (a1[0] == a1[1])); // "True"
    
    using (var stream = new MemoryStream()) {
        BinaryFormatter formatter = new BinaryFormatter();

        // 先序列化再反序列化数组元素
        formatter.Serialize(stream, a1);
        stream.Position = 0;
        Singleton[] a2 = (Singleton[])formatter.Deserialize(stream);

        // 证明它的工作和预期的一样：
        Console.WriteLine("Do both elements refer to the same object? "
            + (a2[0] == a2[1])); // "True"
        Console.WriteLine("Do all elements refer to the same object? "
            + (a1[0] == a2[0])); // "True"
    }
}
```

现在，我们通过分析代码来理解所发生的事情。`Singleton` 类型加载到 AppDomain 中时，CLR 调用它的静态构造器来构造一个 `Singleton` 对象，并将对它的引用保存到静态字段 `s_theOneObject` 中。`Singleton` 类没有提供任何公共构造器，这防止了其他任何代码构造该类的其他实例。

在 `SingletonSerializationTest` 中，我们创建包含两个元素的一个数组；每个元素都引用 `Singleton` 对象。为了初始化两个元素，我们调用 `Singleton` 的静态 `GetSingleton` 方法。这个方法返回对一个 `Singleton` 对象的引用。对 `Console` 的 `WriteLine` 方法的第一个调用显示"True"，证明两个数组元素引用同一个对象。

现在，`SingletonSerializationTest` 调用格式化器的 `Serialize` 方法序列化数组及其元素。序列化第一个 `Singleton` 时，格式化器检测到 `Singleton` 类型实现了 `ISerializable` 接口，并调用 `GetObjectData` 方法。这个方法调用 `SetType`，向它传递 `SingletonSerializationHelper` 类型，告诉格式化器将 `Singleton` 对象序列化成一个 `SingletonSerializationHelper` 对象。由于 `AddValue` 没有调用，所以没有额外的字段信息写入流。由于格式化器自动检测出两个数组元素都引用一个对象，所以格式化器只序列化一个对象。

序列化数组之后，`SingletonSerializationTest` 调用格式化器的 `Deserialize` 方法。对流进行反序列化时，格式化器尝试反序列化一个`SingletonSerializationHelper` 对象，这是格式化器之前被 “欺骗”所序列化的东西。(事实上，这正是为什么 `Singleton` 类不提供特殊构造器的原因：实现 `ISerializable` 接口时通常都要求提供这个特殊构造器。)构造好 `SingletonSerializationHelper` 对象后，格式化器发现这个类型实现了 `System.Runtime.Serialization.IObjectReference` 接口。这个接口在 FCL 中是像下面这样定义的：

```C#
public interface IObjectReference {
    Object GetRealObject(StreamingContext context);
}
```

如果类型实现了这个接口，格式化器会调用 `GetRealObject` 方法。这个方法返回在对象反序列化好之后你真正想引用的对象。在我的例子中，`SingletonSerializationHelper` 类型让 `GetRealObject` 返回对 AppDomain 中已经存在的 `Singleton` 对象的一个引用。所以，当格式化器的 `Deserialize` 方法返回时，`a2` 数组包含两个元素，两者都引用 AppDomain 的 `Singleton` 对象。用于帮助进行反序列化的 `SingletonSerializationHelper` 对象立即变得“不可达”了<sup>①</sup>，将来会被垃圾回收。

> ① 没有谁引用它了。 ———— 译注

对 `WriteLine` 的第二个调用显示 “True“，证明 `a2` 数组的两个元素都引用同一个对象。第三个(也是最后一个)`WriteLine`调用也显示”True“，证明两个数组中的元素引用的是同一个对象。

## <a name="24_8">24.8 序列化代理</a>

前面讨论了如何修改一个类型的实现，控制该类型如何对它本身的实例进行序列化和反序列化。然而，格式化器还允许不是”类型实现的一部分“的代码重写该类型”序列化和反序列化其对象“的方式。应用程序代码之所以要重写(覆盖)类型的行为，主要是出于两方面的考虑。

* 允许开发人员序列化最初没有设计成要序列化的类型。

* 允许开发人员提供一种方式将类型的一个版本映射到类型的一个不同的版本

简单地说，为了使这个机制工作起来，首先要定义一个”代理类型“(surrogate type)，它接管对现有类型进行序列化和反序列化的行动。然后，向格式化器登记该代理类型的实例，告诉格式化器代理类型要作用于现有的哪个类型。一旦格式化器要对现有类型的实例进行序列化或反序列化，就调用由你的代理对象定义的方法。下面用一个例子演示这一切是如何工作的。

序列化代理类型必须实现 `System.Runtime.Serialization.ISerializationSurrogate` 接口，它在 FCL 中像下面这样定义：

```C#
public interface ISerializationSurrogate {
    void GetObjectData(Object obj, SerializationInfo info, StreamingContext context);

    Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context,
        ISurrogateSelector selector);
} 
```

让我们分析使用了该接口的一个例子。假定程序含有一些 `DateTime` 对象，其中包含用户计算机的本地值。如果想把 `DateTime` 对象序列化到流中，同时希望值用国际标准时间(世界时)序列化，那么应该如何操作呢？这样一来，就可以将数据通过网络流发送给世界上其他地方的另一台机器，使 `DateTime` 值保持正确。虽然不能修改 FCL 自带的 `DateTime` 类型，但可以定义自己的序列化代理类，它能控制 `DateTime` 对象的序列化和反序列化方式。下面展示了如何定义代理类：

```C#
internal sealed class UniversalToLocalTimeSerializationSurrogate : ISerializationSurrogate {
    public void GetObjectData(Object obj, SerializationInfo info, StreamingContext context) {
        // 将 DateTime 从本地时间转换成 UTC
        info.AddValue("Date", ((DateTime)obj).ToUniversalTime().ToString("u"));
    }

    public Object SetObjectData(Object obj, SerializationInfo info, StreamingContext context,
        ISurrogateSelector selector) {
        // 将 DateTime 从 UTC 转换成本地时间
        return DateTime.ParseExact(info.GetString("Date"), "u", null).ToLocalTime();
    }
} 
```

`GetObjectData` 方法在这里的工作方式与 `ISerializable` 接口的 `GetObjectData` 方法差不多。唯一的区别在于，`ISerializationSurrogate` 的 `GetObjectData` 方法要获取一个额外的参数————对要序列化的”真实”对象的引用。在上述 `GetObjectData` 方法中，这个对象转型为 `DateTime`，值从本地时间转换为世界时，并将一个字符串(使用通用完整日期/时间模式来格式化)添加到 `SerializationInfo` 集合。

`SetObjectData` 方法用于反序列化一个 `DateTime` 对象。调用这个方法时要向它传递一个 `SerializationInfo` 对象引用。`SetObjectData` 从这个集合中获取字符串形式的日期，把它解析成通用完整日期/时间模式的字符串，然后将结果 `DateTime` 对象从世界时转换成计算机的本地时间。

传给 `SetObjectData` 第一个参数的 `Object` 有点儿奇怪。在调用 `SetObjectData` 之前，格式化器分配(通过 `FormatterServices` 的静态方法 `GetUninitializedObject`)要代理的那个类型的实例。实例的字段全是 `0/null`，而且没有在对象上调用构造器。`SetObjectData` 内部的代码为了初始化这个实例的字段，可以使用传入的 `SerializationInfo` 中的值，并让 `SetObjectData` 返回 `null`。另外，`SetObjectData`可以创建一个完全不同的对象，甚至创建不同类型的变量，并返回对新对象的引用。在这种情况下，格式化器会忽略对传给 `SetObjectData` 的对象的任何改变。

在我的例子中，`UniversalToLocalTimeSerializationSurrogate` 类扮演了 `DateTime` 类型的代理的角色。`DateTime` 是值类型，所以 `obj` 参数引用了一个 `DateTime` 的已装箱实例。大多数值类型中的字段都无法更改(值类型本来就设计成“不可变”)，所以我的 `SetObjectData` 方法会忽略`obj`参数，并返回一个新的 `DateTime`对象，其中已装好了期望的值。

此时，那肯定会问，序列化/反序列化一个 `DateTime` 对象时，格式化器怎么知道要用这个 `ISerializationSurrogate` 类型呢？以下代码对 `UniversalToLocalTimeSerializationSurrogate` 类进行了测试：

```C#
private static void SerializationSurrogateDemo() {
    using (var stream = new MemoryStream()) {
        // 1. 构造所需的格式化器
        IFormatter formatter = new SoapFormatter();

        // 2. 构造一个 SurrogateSelector (代理选择器)对象
        SurrogateSelector ss = new SurrogateSelector();

        // 3. 告诉代理选择器为 DateTime 对象使用我们的代理
        ss.AddSurrogate(typeof(DateTime), formatter.Context,
            new UniversalToLocalTimeSerializationSurrogate());            
        // 注意： AddSurrogate 可多次调用来登记多个代理

        // 4. 告诉格式化器使用代理选择器
        formatter.SurrogateSelector = ss; 

        // 创建一个 DateTime 来代表机器上的本地时间，并序列化它        
        DateTime localTimeBeforeSerialize = DateTime.Now;
        formatter.Serialize(stream, localTimeBeforeSerialize);

        // stream 将 Universal 时间作为一个字符串显示，证明能正常工作
        stream.Position = 0;
        Console.WriteLine(new StreamReader(stream).ReadToEnd());

        // 反序列化 Universal 时间字符串，并且把它转换成本地 DateTime
        stream.Position = 0;
        DateTime localTimeAfterDeserialize = (DateTime)formatter.Deserialize(stream);

        // 证明它正确工作
        Console.WriteLine("LocalTimeBeforeSerialize ={0}", localTimeBeforeSerialize);
        Console.WriteLine("LocalTimeAfterDeserialize={0}", localTimeAfterDeserialize);
    }
} 
```

步骤 1 到步骤 4 执行完毕后，格式化器就准备好使用已登记的代理类型。调用格式化器的 `Serialize` 方法时，会在 `SurrogateSelector` 维护的集合(一个哈希表)中查找(要序列化的)每个对象的类型。如果发现一个匹配，就调用 `ISerializationSurrogate` 对象的 `GetObjectData` 方法来获取应该写入流的信息。

格式化器的 `Deserialize` 方法在调用时，会在格式化器的 `SurrogateSelector` 中查找要反序列化的对象的类型。如果发现一个匹配，就调用`ISerializationSurrogate` 对象的 `SetObjectData` 方法来设置要反序列化的对象中的字段。

`SurrogateSelector` 对象在内部维护了一个私有哈希表。调用 `AddSurrogate` 时，`Type` 和 `StreamingContext` 构成了哈希表的键(key)，对应的值(value)就是 `ISerializationSurrogate` 对象。如果已经存在和要添加的 `Type/StreamingContext` 相同的一个键，`AddSurrogate` 会抛出一个 `ArgumentException`。通过在键中包含一个 `StreamingContext`，可以登记一个代理类型对象，它知道如何将 `DateTime` 对象序列化/反序列化到一个文件中；再登记一个不同的代理对象，它知道如何将 `DateTime` 对象序列化、反序列化到一个不同的进程中。

> 注意 `BinaryFormatter` 类有一个 bug，会造成代理无法序列化循环引用的对象，为了解决这个问题，需要将对自己的 `ISerializationSurrogate` 对象的引用传给 `FormatterServices` 的静态 `GetSurrogateForCyclicalReference` 方法。该方法返回一个 `ISerializationSurrogate` 对象。然后，可以将对这个对象的引用传给 `SurrogateSelector` 的 `AddSurrogate` 方法。但要注意，使用 `GetSurrogateForCyclicalReference` 方法时，代理的 `SetObjectData` 方法必须修改 `SetObjectData` 的 `obj` 参数所引用的对象中的值，而且最后要向调用方法返回 `null` 或 `obj`。在本书的配套资源中，有一个例子展示了如何修改 `UniversalToLocalTimeSerializationSurrogate` 类和 `SerializationSurrogateDemo` 方法来支持循环引用。

### 代理选择器链

多个 `SurrogateSelector` 对象可链接到一起。例如，可以让一个 `SurrogateSelector` 对象维护一组序列化代理，这些序列化代理(surrogate)用于将类型序列化成带代理(proxy)<sup>①</sup>，以便通过网络传送，或者跨越不同的 AppDomain 传送。还可以让另一个 `SurrogateSelector` 对象维护一组序列化代理，这些序列化代理用于将版本 1 的类型转换成版本 2 的类型。

> ① 两个“代理”是不同的概念。`surrogate` 对象的负责序列化，而 `proxy` 对象负责跨越 `AppDomain` 边界访问对象(参见 22.2.1 节“跨越 `AppDomain` 边界访问对象”)。 ———— 译注

如果有多个希望格式化器使用的 `SurrogateSelector` 对象，必须把它们链接到一个链表中。`SurrogateSelector` 类型实现了 `ISurrogateSelector` 接口，该接口定义了三个方法。这些方法全部跟链接有关。下面展示了 `ISurrogateSelector` 接口是如何定义的：

```C#
public interface ISurrogateSelector {
    void ChainSelector(ISurrogateSelector selector);
    ISurrogateSelector GetNextSelector();
    ISerializationSurrogate GetSurrogate(Type type, StreamingContext context,
        out ISurrogateSelector selector);
}
```

`ChainSelector` 方法紧接在当前操作的 `ISurrogateSelector` 对象(`this` 对象)之后插入一个 `ISurrogateSelector` 对象。`GetNextSelector` 方法返回对链表中的下一个 `ISurrogateSelector` 对象的引用；如果当前操作的对象是链尾，就返回 `null`。

`GetSurrogate` 方法在 `this` 所代表的 `ISurrogateSelector` 对象中查找一对 `Type/StreamingContext`。如果没有找到 `Type/StreamingContext` 对，就访问链中的下一个 `ISurrogateSelector` 对象，依次类推。如果找到一个匹配项，`GetSurrogate` 将返回一个 `ISerializationSurrogate` 对象，该对象负责对找到的类型进行序列化/反序列化。除此之外，`GetSurrogate`还会返回包含匹配项的 `ISurrogateSelector` 对象；一般都用不着这个对象，所以一般会将其忽略。如果链中所有 `ISurrogateSelector` 对象都不包含匹配的一对 `Type/StreamingContext`，`GetSurrogate` 将返回 `null`。

> 注意 FCL定义了一个 `ISurrogateSelector` 接口，还定义了一个实现了该接口的 `SurrogateSelector` 类型。然而，只有在一些非常罕见的情况下，才需要定义自己的类型来实现 `ISurrogateSelector` 接口。实现 `ISurrogateSelector` 接口的唯一原因就是将类型映射到另一个类型时需要更大的灵活性。例如，你可能希望以一种特殊方式序列化从一个特定基类继承的所有类型。`System.Runtime.Remoting.Messaging.RemotingSurrogateSelector` 类就是一个很好的例子。出于远程访问(remoting)目的而序列化对象时，CLR 使用 `RemotingSurrogateSelector` 来格式化对象。这个代理选择器(surrogate selector)以一种特殊方式序列化从 `System.MarshalByRefObject` 派生的所有对象，确保反序列化会造成在客户端创建代理对象(proxy object)。

## <a name="24_9">24.9 反序列化对象时重写程序集和/或类型</a>

序列化对象时，格式化器输出类型及其定义程序集的全名。反序列化对象时，格式化器根据这个信息确定要为对象构造并初始化什么类型。前面讨论了如何利用 `ISerializationSurrogate` 接口来接管特定类型的序列化和反序列化工作。实现了 `ISerializationSurrogate` 接口的类型与特定程序集中的特定类型关联。

但有的时候，`ISerializationSurrogate` 机制的灵活性显得有点不足。在下面列举的情形中，有必要将对象反序列化成和序列化时不同的类型。

* 开发人员可能想把一个类型的实现从一个程序集移动到另一个程序集。例如，程序集版本号的变化造成新程序集有别于原始程序集。

* 服务器对象序列化到发送客户端的流中。客户端处理流时，可以将对象反序列化成完全不同的类型，该类型的代码知道如何向服务器的对象发出远程方法调用。

* 开发人员创建了类型的新版本，想把已序列化的对象反序列化成类型的新版本。

利用 `System.Runtime.Serialization.SerializationBinder` 类，可以非常简单地将一个对象反序列化成不同类型。为此，要先定义自己的类型，让它从抽象类 `SerializationBinder` 派生。在下面的代码中，假定你的版本 1.0.0.0 的程序集定义了名为 `Ver1` 的类，并假定程序集的新版本定义了 `Ver1ToVer2SerializationBinder` 类，还定义了名为 `Ver2` 的类：

```C#
internal sealed class Ver1ToVer2SerializationBinder : SerializationBinder {
    public override Type BindToType(String assemblyName, String typeName) {
        // 将任何 Ver1 对象从版本 1.0.0.0 反序列化成一个 Ver2 对象

        // 计算定义 Ver1 类型的程序集名称
        AssemblyName assemVer1 = Assembly.GetExecutingAssembly().GetName();
        assemVer1.Version = new Version(1, 0, 0, 0);

        // 如果从 v1.0.0.0 反序列化 Ver1 对象，就把它转变成一个 Ver2 对象
        if (assemblyName == assemVer1.ToString() && typeName == "Ver1")
            return typeof(Ver2);

        // 否则，就只返回请求的同一个类型
        return Type.GetType(String.Format("{0}, {1}", typeName, assemblyName));
    }
} 
```

现在，在构造好格式化器之后，构造 `Ver1ToVer2SerializationBinder` 的实例，并设置格式化器的可读/可写属性 `Binder`，让它引用绑定器(binder)对象。设置好 `Binder` 属性后，调用格式化器的 `Deserialize` 方法。在反序列化期间，格式化器发现已设置了一个绑定器。每个对象要反序列化时，格式化器都调用绑定器的 `BindToType` 方法，向它传递程序集名称以及格式化器想要反序列化的类型。然后，`BindToType` 判断实际应该构建什么类型，并返回这个类型。

> 注意 `SerializationBinder` 类还可重写 `BindToName` 方法，从而序列化对象时更改程序集/类型信息，这个方法看起来像下面这样：  
`public virtual void BindToName(Type serializedType, out string assemblyName, out string typeName)`

> 序列化期间，格式化器调用这个方法，传递它想要序列化的类型。然后，你可以通过两个 out 参数返回真正想要序列化的程序集和类型。如果两个 out 参数返回 `null` 和 `null`(默认实现就是这样的)，就不执行任何更改。