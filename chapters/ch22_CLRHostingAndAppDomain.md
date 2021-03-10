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

* **AppDomain 可以单独保护**
  AppDomain 创建后应用一个权限集，它决定了向这个 AppDomain 中运行的程序集授予的最大权限，所以当宿主加载一些代码后，可以保证这些代码不会破坏(或读取)宿主本身使用的一些重要数据结构。

* **AppDomain 可以单独配置**  
  AppDomain 创建后会关联一组配置设置。这些设置主要影响 CLR 在 AppDomain 中加载程序集的方式。涉及搜索路径、版本绑定重定向、卷影复制以及加载器优化。

> 重要提示 Windows 的一个重要特色是让每个应用程序都在自己的进程地址空间中运行。这就保证了一个应用程序的代码不能访问另一个应用程序使用的代码或数据。进程隔离可防范安全漏洞、数据破坏和其他不可预测的行为，确保了 Windows 系统以及在它上面运行的应用程序的健壮性。遗憾的是，在 Windows 中创建进程的开销很大。Win32 CreateProcess 函数的速度很慢，而且 Windows 需要大量内存来虚拟化进程的地址空间。  

> 但是，如果应用程序安全由托管代码构成(这些代码的安全性可以验证)，同时这些代码没有调用非托管代码，那么在一个 Windows 进程中运行多个托管应用程序是没有问题的。AppDomain 提供了保护、配置和终止其中每一个应用程序所需的隔离。  

图 22-1 演示了一个 Windows 进程，其中运行着一个 CLR COM 服务器。该 CLR 当前管理着两个 AppDomain(虽然在一个 Windows 进程中可以运行的 AppDomain 数量没有硬性限制)。每个 AppDomain 都有自己的 Loader 堆，每个 Loader 堆都记录了自 AppDomain 创建以来已访问过哪些类型。这些类型对象已在第 4 章讨论过，Loader 堆中的每个类型对象都有一个方法表，方法表中的每个记录项都指向 JIT 编译的本机代码(前面是方法至少执行过一次)。

![22_1](../resources/images/22_1.png)  

图 22-1 寄宿了 CLR 和两个 AppDomain 的一个 Windows 进程  

除此之外，每个 AppDomain 都加载了一些程序集。AppDomain #1(默认 AppDomain)有三个程序集：MyApp.exe，TypeLib.dll 和 System.dll。AppDomain #2 有两个程序集：Wintellect.dll 和 System.dll。

如图 22-1 所示，两个 AppDomain 都加载了 System.dll 程序集。如果这两个 AppDomain 都使用了来自 System.dll 的一个类型，那么两个 AppDomain 的 Loader 堆会为相同的类型分别分配一个类型对象；类型对象的内存不会由两个 AppDomain 共享。另外，一个 AppDomain 中的代码调用一个类型定义的方法时，方法的 IL 代码会进行 JIT 编译，生成的本机代码单独与每个 AppDomain 关联，而不是由调用它的所有 AppDomain 共享。

不共享类型对象的内存或本机代码显得有些浪费。但 AppDomain 的设计宗旨就是提供隔离；CLR 要求在卸载某个 AppDomain 并释放其所有资源时不会影响到其他任何 AppDomain。复制 CLR 的数据结构才能保证这一点。另外，还保证多个 AppDomain 使用的类型在每个 AppDomain 中都有一组静态字段。

有的程序集本来就要由多个 AppDomain 使用。最典型的例子就是 MSCorLib.dll。该程序集包含了 `System.Object`，`System.Int32` 以及其他所有与 .NET Framework 密不可分的类型。CLR 初始化，该程序集会自动加载，而且所有 AppDomain 都共享该程序集中的类型。为了减少资源消耗，MSCorLib.dll 程序集以一种“AppDomain 中立”的方式加载。也就是说，针对以“AppDomain 中立”的方式加载的程序集，CLR 会为它们维护一个特殊的 Loader 堆。该 Loader 堆中的所有类型对象，以及为这些类型定义的方法 JIT 编译生成的所有本机代码，都会由进程中的所有 AppDomain 共享。遗憾的是，共享这些资源所获得的收益并不是没有代价的。这个代价就是，以 “AppDomain 中立”的方式加载的所有程序集永远不能卸载。要回收它们占用的资源，唯一的办法就是终止 Windows 进程，让 Windows 去回收资源。

### **跨越 AppDomain 边界访问对象**

一个 AppDomain 中的代码可以和另一个 AppDomain 中的类型和对象通信，但只能通过良好定义的机制进行。以下 Ch22-1-AppDomains 示例程序演示了如何创建新 AppDomain，在其中加载程序集并构造该程序集定义的类型的实例。代码演示了一下三种类型在构造时的不同行为：“按引用封送”(Marshal-by-Reference)类型，“按值封送”(Marshal-by-Value)类型，以及完全不能封送的类型。代码还演示了创建它们的 AppDomain 卸载时这些对象的不同行为。Ch22-1-AppDomains 示例程序的代码实际很少，只是我添加了大量注释。在代码清单之后，我将逐一分析这些代码，解释 CLR 所做的事情。


  