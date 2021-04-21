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

## <a name="24_6">24.6 流上下文</a>

## <a name="24_7">24.7 将类型序列化为不同的类型以及将对象反序列化为不同的对象</a>

## <a name="24_8">24.8 序列化代理</a>

## <a name="24_9">24.9 反序列化对象时重写程序集和/或类型</a>
