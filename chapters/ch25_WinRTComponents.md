# 第 25 章 与 WinRT 组件互操作

本章内容：

* <a href="#25_1">CLR 投射与 WinRT 组件类型系统规则</a>
* <a href="#25_2">框架投射</a>
* <a href="#25_3">用 C# 定义 WinRT 组件</a>

Windows 8/8.1 带来了一个新类库，应用程序可通过它访问操作系统功能。类库正式名称是 **Windows 运行时**(Windows Runtime， WinRT)，其组件通过 WinRT 类型系统访问。首次发布时， WinRT 的许多目标都和 CLR 相同，例如简化应用程序开发，以及允许代码用不同编程语言实现以简化互操作。特别是，Microsoft 支持在原生 C/C++、JavaScript(通过 Microsoft 的 “Chakra” JavaScript 虚拟机)和 C#/Visual Basic(通过 CLR)中使用 WinRT 组件。

图 25-1 展示了 Windows 的 WinRT 组件所公开的功能，以及可以访问它们的 Microsoft 语言。对于原生 C/C++ 实现的应用程序，开发人员必须为每种 CPU 架构(x86，x64 和 ARM)单独编译代码。相比之下，.NET 开发人员只需编译一次(编译成 IL，CLR 自行将其编译成与主机 CPU 对应的本机代码)。JavaScript 应用程序则自带了源代码，“Chakra”虚拟机解析这些源代码，把它编译成与主机 CPU 对应的本机代码。其他公司也可制作能与 WinRT 组件互操作的语言和环境。

![25_1](../resources/images/25_1.png)  

图 25-1 Windows 的 WinRT 组件所公开的功能，以及访问它们的各种语言

Windows Store 应用和桌面应用程序可通过 WinRT 组件来利用操作系统的功能。Windows 配套提供的 WinRT 组件数量比 .NET Framework 类库小多了。但设计就是这样的，组件的目的是公开操作系统最擅长的事情，也就是对硬件和跨应用程序的功能进行抽象。所以，大多数 WinRT 组件都只是公开了功能，比如存储、联网、图形、媒体、安全性、线程处理等。而其他核心语言服务(比如字符串操作)和较复杂的框架(比如 LINQ)不是由操作系统提供，而是由访问 WinRT 组件的语言提供。

WinRT 组件内部作为“组件对象模型”(Component Object Model，COM)组件来实现，后者是 Microsoft 1993 年推出的技术。COM 当年被认为过于复杂，规则过于难解，是一个很让人头疼的编程模型。当 COM 实际是有许多亮点的。多年来，Microsoft 对其进行了大量修订，显著地进行了简化。Microsoft 对 WinRT 组件进行了一个很大的调整，不是使用类库来描述 COM 组件的 API，而是使用元数据。你没有看错，WinRT 组件使用由 ECMA 协会标准化的 .NET 元数据格式(ECMA-335)来描述其API。

这个元数据格式正是本书一直在讨论的。元数据比类库更有表现力，而且 CLR 已经对元数据有了全面理解。此外，CLR 一开始就通过**运行时可调用包装器**(Runtime Callable Wrapper，RCW)和**COM 可调用包装器**(COM Callable Wrapper，CCW)实现了与 COM 组件的互操作。这使在 CLR 顶部运行的语言(如 C#)能无缝地与 WinRT 类型和组件进行互操作。

在 C# 中引用 WinRT 对象，实际获得的是对一个 RCW 的引用，RCW 内部引用了 WinRT 组件。类似地，将一个 CLR 对象传给 WinRT API，实际传递的是一个 CCW 引用，CCW 内部容纳了对 CLR 对象的引用。

WinRT 组件将元数据嵌入扩展名为 .winmd 的文件中(winmd 代表 Windows MetaData)。Windows 搭载的 WinRT 组件将元数据存储到各种 Windows.*.winmd 文件中，这些文件可在 %WinDir%\System32\WinMetadata 目录中找到。生成应用时要引用安装到以下目录的 Windows.winmd 文件<sup>①</sup>：

> ① 针对 Windows 8.1。————译注

`%ProgramFiles(x86)%\Windows Kits\8.1\References\CommonConfiguration\Neutral\Windows.winmd`

Windows Runtime 类型系统的一个主要设计目标是使开发人员能使用他们擅长的技术、工具、实践以及约定写应用。为此，有的 WinRT 功能被投射<sup>②</sup>成对应的开发技术。针对.NET Framework 开发人员主要有两种投射：

> ② 本章所说的投射(projection)和映射(mapping)是一回事。 ———— 译注

* **CLR 投射**  
  CLR 投射由 CLR 隐式执行，通常和元数据的重写解释有关。下一节会讨论 WinRT 组件类型系统规则以及 CLR 如何将这些规则投射给 .NET Framework 开发人员。

* **Framework 投射**  
  Framework 投射由你的代码显式执行，这是通过 FCL 新引入的 API 来执行的。如果 WinRT 类型系统和 CLR 类型系统差异太大，造成 CLR 不能隐式地投射，就需要用到 Framework 投射。本章稍后会讨论这种投射。

## <a name="25_1">25.1 CLR 投射与 WinRT 组件类型系统规则</a>

## <a name="25_2">25.2 框架投射</a>

## <a name="25_3">25.3 用 C# 定义 WinRT 组件</a>