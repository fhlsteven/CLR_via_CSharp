# 第 22 章 CLR 寄宿和 AppDomain

本章内容：

* <a href="#22_1">CLR 寄宿</a>
* <a href="#22_2">AppDomain</a>
* <a href="#22_3">卸载 AppDomain</a>
* <a href="#22_4">监视 AppDomain</a>
* <a href="#22_5">AppDomain FirstChance 异常通知</a>
* <a href="#22_6">宿主如何使用 AppDomain</a>
* <a href="#22_7">高级宿主控制</a>

本章主要讨论两个主题：寄宿和 AppDomain。这两个主题充分演示了 Microsoft .NET Framework 的巨大价值。寄宿(hosting)使任何应用程序都能利用 CLR 的功能。特别要指出的是，它使现有的应用程序至少能部分使用托管代码编写。另外，寄宿还为应用程序至少能部分使用托管代码编写。另外，寄宿还为应用程序提供了通过编程来进行自定义和扩展的能力。

允许可扩展性意味着第三方代码可在你的进程中运行。在 Windows 中将第三方 DLL 加载到进程中意味着冒险。DLL 中的代码很容易破坏应用程序的数据结构和代码。DLL 还可能企图利用应用程序程序的安全上下文来访问它本来无权访问的资源。CLR 的 AppDomain 功能解决了所有这些问题。AppDomain 允许第三方的、不受信任的代码在现有的进程中运行，而 CLR 保证数据结构、代码和安全上下文不被滥用或破坏。

程序员经常将寄宿和 AppDomain 与程序集的加载和反射一起使用。这 4 种技术一起使用，使 CLR 成为一个功能极其丰富和强大的平台。本章重点在于寄宿和 AppDomain。下一章则会重点放在程序集加载和反射上。学习并理解了所有这些技术后，会发现今天在 .NET Framework 上面的投资，将来必会获得丰厚回报。

## <a name="22_1">22.1 CLR 寄宿</a> 

.NET Framework 在 Windows 平台的顶部运行。这意味着 .NET Framework 必须用 Windows 能理解的技术来构建。首先，所有托管模块和程序集文件都必须使用 Windows PE 文件格式，而且要么是 Windows EXE 文件，要么是 DLL 文件。

开发 CLR 时，Microsoft 实际是把它实现成包含一个 DLL 中的 COM 服务器。也就是说，Microsoft 为 CLR 定义了一个标准的 COM 接口，并为该接口和 COM 服务器分配了 GUID。 安装 .NET Framework 时，代表 CLR 的 COM 服务器和其他 COM 服务器一样在 Windows 注册表中注册。要了解这方面的更多信息，可参考与 .NET Framework SDK 一起发布的 C++ 头文件 MetaHost.h。该头文件中定义了 GUID 和非托管 `ICLRMetaHost` 接口。

任何 Windows 应用程序都能寄宿(容纳)CLR。但不要通过调用 `CoCreateInstance` 来创建 CLR COM 服务器的实例，相反，你的非托管宿主应该调用 `MetaHost.h` 文件中声明的 `CLRCreateInstance` 函数。`CLRCreateInstance` 函数在 MSCorEE.dll 文件中实现，该文件一般在`C:\Windows\System32` 目录中。这个 DLL 被人们亲切地称为“垫片”(shim)，它的工作是决定创建哪个版本的CLR；垫片 DLL 本身不包含 CLR COM 服务器。

一台机器可安装多个版本的 CLR，但只有一个版本的 MSCorEE.dll 文件(垫片)<sup>①</sup>。机器上安装的 MSCorEE.dll 是与机器上安装的最新版本的 CLR 一起发布的那个版本。所以，该版本的 MSCorEE.dll 知道如何查找机器上的老版本 CLR。

> ① 使用 64 位 Windows 实际会安装两个版本的 MSCorEE.dll 文件。一个是 32 位 x86 版本，在 `C:\Windows\SysWOW64` 目录中；另一个是 64 位 x64 或 IA64 版本(取决于计算机的 CPU 架构)，在 `C:\Windows\System32` 目录中。

包含实际 CLR 代码的文件的名称在不同版本的 CLR 中是不同的。版本 1.0， 1.1 和 2.0 的 CLR 代码在 MSCorWks.dll 文件中；版本 4 则在 Clr.dll 文件中。由于一台机器可能安装多个版本的 CLR，所以这些文件安装到不同的目录，如下所示。<sup>②</sup>

> ② 注意，.NET Framework 3.0 和 3.5 是与 CLR 2.0 一起发布的。我没有显示 .NET Framework 3.0 和 3.5 的目录，因为 CLR DLL 是从 v2.0.50727 目录加载的。

* 版本 1.0 在 `C:\Windows\Microsoft.Net\Framework\v1.0.3705` 中。

* 版本 1.1 在 `C:\Windows\Microsoft.Net\Framework\V1.0.4322` 中。

* 版本 2.0 在 `C:\Windows\Microsoft.Net\Framework\v2.0.50727` 中。

* 版本 4 在 `C:\Windows\Microsoft.Net\Framework\v4.0.30319`<sup>③</sup>中。

> ③ 这是本书出版时的 .NET Framework 4 目录。在你的机器上可能有所不同。 ———— 译注

`CLRCreateInstance` 函数可返回一个 `ICLRMetaHost` 接口。宿主应用程序可调用这个接口的 `GetRuntime` 函数，指定宿主要创建的 CLR 的版本。然后，垫片将所需版本的 CLR 加载到宿主的进程中。

默认情况下，当一个托管的可执行文件启动时，垫片会检查可执行文件，提取当初生成和测试应用程序时使用的 CLR 的版本信息。但应用程序可以在它的 XML 配置文件中设置 `requiredRuntime` 和 `supportedRuntime` 这两项来覆盖该默认行为。(XML 配置文件的详情请参见第 2 章和第 3 章。)

`GetRuntime` 函数返回指向非托管 `ICLRRuntimeInfo` 接口的指针。有了这个指针后，就可利用 `GetInterface` 方法获得 `ICLRRuntimeHost` 接口。宿主应用程序可调用该接口定义的方法来做下面这些事情。

* 设置宿主管理器。告诉 CLR 宿主想参与涉及以下操作的决策：内存分配、线程调度/同步以及程序集加载等。宿主还可声明它想获得有关垃圾回收启动和停止以及特定操作超时的通知。

* 获取 CLR 管理器。告诉 CLR 阻止使用某些类/成员。另外，宿主能分辨哪些代码可以调试，哪些不可以，以及当特定事件(例如 AppDomain 卸载、CLR停止或者堆栈溢出异常)发生时宿主应调用哪个方法。

* 初始化并启动 CLR。

* 加载程序集并执行其中的代码。

* 停止 CLR，阻止任何更多的托管代码在 `Windows` 进程中运行。

> 注意 Windows 进程完全可以不加载 CLR，只有在进程中执行托管代码时才进行加载。在 .NET Framework 4 之前，CLR 只允许它的一个实例寄宿在 Windows 进程中。换言之，在一个进程中，要么不包含任何 CLR，要么只能包含 CLR v1.0, CLR v1.1 或者 CLR 2.0 之一。每进程仅一个版本的 CLR 显然过于局限。例如，这样 Microsoft Office Outlook 就不能加载为不同版本的 .NET Framework 生成和测试的两个加载项了。  

> 但是，随着 .NET Framework 4 的发布，Microsoft 支持在一个 Windows 进程中同时加载 CLR v2.0 和 v4.0，为 .NET Framework 2.0 和 4.0 写的不同组件能同时运行，不会出现任何兼容性问题。这是一个令人激动的功能，因为它极大扩展了 .NET Framework 组件的应用场合。可利用 CLrVer.exe 工具检查给定的进程加载的是哪个(哪些)版本的 CLR。

> 一个 CLR 加载到 Windows 进程之后，便永远不能卸载；在 `ICLRRuntimeHost` 接口上调用 `AddRef` 和 `Release` 方法是没有作用的。CLR 从进程中卸载的唯一途径就是终止进程，这会造成 Windows 清理进程使用的所有资源。

## <a name="22_2">22.2 AppDomain</a>

CLR COM 服务器初始化时会创建一个 AppDomain。AppDomain 是一组程序集的逻辑容器。 CLR 初始化时创建的第一个 AppDomain 称为“默认 AppDomain”，这个默认的 AppDomain 只有在 Windows 进程终止时才会被销毁。

除了默认 AppDomain，正在使用非托管 COM 接口方法或托管类型方法的宿主还可要求 CLR 创建额外的 AppDomain。AppDomain 是为了提供隔离而设计的。下面总结了 AppDomain 的具体功能。

* **一个 AppDomain 的代码不能直接访问另一个 AppDomain 的代码创建的对象**  
  一个 AppDomain 中的代码创建了一个对象后，该对象便被该 AppDomain “拥有”。换言之，它的生存期不能超过创建它的代码所在的 AppDomain。一个 AppDomain 中的代码要访问另一个 AppDomain 中的对象，只能使用“按引用封送”(marshal-by-reference)或者“按值封送”(marshal-by-value)的语义。这就强制建立了清晰的分隔边界，因为一个 AppDomain 中的代码不能直接引用另一个 AppDomain 中的代码创建的对象。这种隔离使 AppDomain 能很容易地从进程中卸载，不会影响其他 AppDomain 正在运行的代码。

* **AppDomain 可以卸载**
  CLR 不支持从 AppDomain 中卸载特定的程序集。但可以告诉 CLR 卸载一个 AppDomain，从而卸载该 AppDomain 当前包含的所有程序集。

* **AppDomain 可以单位保护**
  AppDomain 创建后应用一个权限集，
  