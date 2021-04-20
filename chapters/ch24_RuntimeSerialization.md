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

## <a name="24_2">24.2 使类型可序列化</a>

## <a name="24_3">24.3 控制序列化和反序列化</a>

## <a name="24_4">24.4 格式化器如何序列化类型实例</a>

## <a name="24_5">24.5 控制序列化/反序列化的数据</a>

## <a name="24_6">24.6 流上下文</a>

## <a name="24_7">24.7 将类型序列化为不同的类型以及将对象反序列化为不同的对象</a>

## <a name="24_8">24.8 序列化代理</a>

## <a name="24_9">24.9 反序列化对象时重写程序集和/或类型</a>
