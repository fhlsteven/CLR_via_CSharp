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

```C#
private static void Marshalling() {
    // 获取 AppDomain 引用 (“调用线程”当前正在该 AppDomain 中执行)
    AppDomain adCallingThreadDomain = Thread.GetDomain();

    // 每个 AppDomain 都分配了友好字符串名称(以便调试)
    // 获取这个 AppDomain 的友好字符串名称并显示它
    String callingDomainName = adCallingThreadDomain.FriendlyName;
    Console.WriteLine("Default AppDomain's friendly name={0}", callingDomainName);

    // 获取并显示我们的 AppDomain 中包含了“Main”方法的程序集
    String exeAssembly = Assembly.GetEntryAssembly().FullName;
    Console.WriteLine("Main assembly={0}", exeAssembly);

    // 定义局部变量来引用一个 AppDomain
    AppDomain ad2 = null;

    // *** DEMO 1：使用 Marshal-by-Reference 进行跨 AppDomain 通信 ***
    Console.WriteLine("{0}Demo #1", Environment.NewLine);

    // 新建一个 AppDomain (从当前 AppDomain 继承安全性和配置)
    ad2 = AppDomain.CreateDomain("AD #2", null, null);
    MarshalByRefType mbrt = null;

    // 将我们的程序集加载到新 AppDomain 中，构造一个对象，把它
    // 封送回我们的 AppDomain (实际得到对一个代理的引用)
    mbrt = (MarshalByRefType)ad2.CreateInstanceAndUnwrap(exeAssembly, "MarshalByRefType");

    Console.WriteLine("Type={0}", mbrt.GetType());      // CLR 在类型上撒谎了

    // 证明得到的是对一个代理对象的引用
    Console.WriteLine("Is proxy={0}", RemotingServices.IsTransparentProxy(mbrt));

    // 看起来像是在 MarshalByRefType 上调用一个方法，实例不然、
    // 我们是在代理类型上调用一个方法，代理使线程切换到拥有对象的
    // 那个 AppDomain，并在真实的对象上调用这个方法
    mbrt.SomeMethod();

    // 卸载新的 AppDomain
    AppDomain.Unload(ad2);

    // mbrt 引用一个有效的代理独享；代理对象引用一个无效的 AppDomain

    try {
        // 在代理类型上调用一个方法。AppDomain 无效，造成抛出异常
        mbrt.SomeMethod();
        Console.WriteLine("Successful call.");
    }
    catch (AppDomainUnloadedException) {
        Console.WriteLine("Failed call.");
    }

    // *** DEMO 2: 使用 Marshal-by-Value 进行跨 AppDomain 通信 ***
    Console.WriteLine("{0}Deom #2", Environment.NewLine);

    // 新建一个 AppDomain(从当前 AppDomain 继承安全性和配置)
    ad2 = AppDomain.CreateDomain("AD #2", null, null);

    // 将我们的程序集加载到新 AppDomain 中，构造一个对象，把它
    // 封送回我们的 AppDomain (实际得到对一个代理的引用)
    mbrt = (MarshalByRefType)ad2.CreateInstanceAndUnwrap(exeAssembly, "MarshalByRefType");

    // 对象的方法返回所返回对象的副本；
    // 对象按值(而非按引用)封送
    MarshalByRefType mbvt = mbrt.MethodWithReturn();

    // 证明得到的不是对一个代理对象的引用
    Console.WriteLine("Is proxy={0}", RemotingServices.IsTransparentProxy(mbvt));

    // 看起来是在 MarshalByRefType 上调用一个方法，实际也是如此
    Console.WriteLine("Returned object created " + mbvt.ToString());

    // 卸载新的 AppDomain
    AppDomain.Unload(ad2);
    // mbvt 引用有效的对象；卸载 AppDomain 没有影响

    try {
        // 我们是在对象上调用一个方法；不会抛出异常
        Console.WriteLine("Returned object created " + mbvt.ToString());
        Console.WriteLine("Successful call.");
    }
    catch (AppDomainUnloadedException) {
        Console.WriteLine("Failed call.");
    }

    // DEMO 3：使用不可封送的类型进行跨 AppDomain 通信 ***
    Console.WriteLine("{0}Demo #3", Environment.NewLine);

    // 新建一个 AppDomain (从当前 AppDomain 继承安全性和配置)
    ad2 = AppDomain.CreateDomain("AD #2", null, null);
    // 将我们的程序集加载到新 AppDomain 中，构造一个对象，把它
    // 封送回我们的 AppDomain (实际得到对一个代理的引用)
    mbrt = (MarshalByRefType)ad2.CreateInstanceAndUnwrap(exeAssembly, "MarshalByRefType");

    // 对象的方法返回一个不可封送的对象；抛出异常
    NonMarshalableType nmt = mbrt.MethodArgAndReturn(callingDomainName);
    // 这里永远执行不到...
}


// 该类的实例可跨越 AppDomain 的边界“按引用封送”
public sealed class MarshalByRefType : MarshalByRefObject {
    public MarshalByRefType() {
        Console.WriteLine("{0} ctor running in {1}", this.GetType().ToString(), Thread.GetDomain().FriendlyName);
    }

    public void SomeMethod() {
        Console.WriteLine("Exexuting in " + Thread.GetDomain().FriendlyName);
    }

    public MarshalByValType MethodWithReturn() {
        Console.WriteLine("Executing in" + Thread.GetDomain().FriendlyName);
        MarshalByValType t = new MarshalByValType();
        return t;
    }

    public NonMarshalableType MethodArgAndReturn(String callingDomainName) {
        // 注意： callingDomainName 是可序列化的
        Console.WriteLine("Calling from '{0}' to '{1}'.", callingDomainName, Thread.GetDomain().FriendlyName);
        NonMarshalableType t = new NonMarshalableType();
        return t;
    }
}

// 该类的实例可跨越 AppDomain 的边界“按值封送”
[Serializable]
public sealed class MarshalByValType : Object {
    private DateTime m_creationTime = DateTime.Now;  // 注意：DateTime 是可序列化的

    public MarshalByValType() {
        Console.WriteLine("{0} ctor running in {1}, Created on {2:D}", this.GetType().ToString(), Thread.GetDomain().FriendlyName, m_creationTime);
    }

    public override String ToString() {
        return m_creationTime.ToLongDateString();
    }
}

// 该类的实例不能跨 AppDomain 边界进行封送
// [Serializable]
public sealed class NonMarshalableType : Object {
    public NonMarshalableType() {
        Console.WriteLine("Executing in " + Thread.GetDomain().FriendlyName);
    }
}
```

生成并运行 Ch22-1-AppDomains 应用程序，获得以下输出结果：<sup>①</sup>

> ① 在本书配套代码中，找到 C22-1 AppDomains 项目，在 Main() 方法中解除对 Marshalling() 方法调用的注释。————译注

```cmd
Default AppDomain's friendly name=Ch22-1-AppDomains.exe
Main assembly=Ch22-1-AppDomains, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null

Demo #1
MarshalByRefType ctor running in AD #2
Type=MarshalByRefType
Is proxy=True
Executing in AD #2
Failed call.

Demo #2
MarshalByRefType ctor running in AD #2
Executing in AD #2
MarshalByValType ctor running in AD #2, Created on 2021年3月11日
Is proxy=False
Returned object created 2021年3月11日
Returned object created 2021年3月11日
Successful call.

Demo #3
MarshalByRefType ctor running in AD #2
Calling from 'Ch22-1-AppDomains.exe' to 'AD #2'.
Executing in AD #2

Unhandled Exception: System.Runtime.Serialization.SerializationException:
Type ‘NonMarshalableType’ in assembly ‘Ch22-1-AppDomains, Version=0.0.0.0,
Culture=neutral, PublicKeyToken=null’ is not marked as serializable.
at MarshalByRefType.MethodArgAndReturn(String callingDomainName)
at Program.Marshalling()
at Program.Main() 
```

现在来讨论以上代码以及 CLR 所做的事情。

`Marshalling` 方法首先获得一个 `AppDomain` 对象引用，当前调用线程正在该 AppDomain 中执行。在 Windows 中，线程总是在一个进程的上下文中创建，而且线程的这个生存期都在该进程的生存期中。但线程和 AppDomain 没有一对一关系。AppDomain 是一项 CLR 功能；Windows 对 AppDomain 一无所知。由于一个 Windows 进程可包含多个 AppDomain，所以线程能执行一个 AppDomain 中的代码，再执行另一个 AppDomain 中的代码。从 CLR 的角度看，线程一次只能执行一个 AppDomain 中的代码。线程可调用 `System.Threading.Thread` 的静态方法 `GetDomain` 向 CLR 询问它正在哪个 AppDomain 中执行。线程还可查询 `System.AppDomain` 的静态只读属性 `CurrentDomain` 获得相同的信息。

AppDomain 创建后可被赋予一个**友好名称**。它是用于标识 AppDomain 的一个 `String`。友好名称主要是为了方便调试。由于 CLR 要在我们的任何执行前创建默认 AppDomain，所以使用可执行文件的文件名作为默认的 AppDomain 友好名称。`Marshalling` 方法使用 `System.AppDomain` 的只读 `FriendlyName` 属性来查询默认 AppDomain 的友好名称。

接着，`Marshalling` 方法查询默认 AppDomain 中加载的程序集的强命名标识，这个程序集定义了入口方法 `Main`(其中调用了 `Marshalling`)。程序集定义了几个类型：`Program`，`MarshalByRefType`，`MarshalByValType` 和 `NonMarshalableType`。现在已准备好研究上面的三个演示(Demo)，它们本质上很相似。

#### 演示 1：使用“按引用封送”进行跨 AppDomain 通信 

演示 1 调用 `System.AppDomain` 的静态 `CreateDomain` 方法指示 CLR 在同一个 Windows 进程中创建一个新 AppDomain。`AppDomain` 类型提供了 `CreateDomain` 方法的多个重载版本。建议仔细研究一下它们，并在新建 AppDomain 时选择最合适的一个。本例使用的 `CreateDomain` 接受以下三个参数。

* 代表新 AppDomain 的友好名称的一个 `String`。本例传递的是“AD #2”。

* 一个 `System.Security.Policy.Evidence`，是 CLR 用于计算 AppDomain 权限集的证据。本例为该参数传递 `null`，造成新 AppDomain 从创建它的 AppDomain 继承权限集。通常，如果希望围绕 AppDomain 中的代码创建安全边界，可构造一个 `System.Security.PermissionSet`对象，在其中添加希望的权限对象(实现了 `IPermission` 接口的类型的实例)，将得到的 `PermissionSet` 对象引用传给接受一个 `PermissionSet` 的 `CreateDomain` 方法重载。

* 一个 `System.AppDomainSetup`，代表 CLR 为新 AppDomain 使用的配置设置。同样，本例为该参数传递 `null`，使新 AppDomain 从创建它的 AppDomain 继承配置设置。如果希望对新 AppDomain 进行特殊设置，可构造一个 `AppDomainSetup` 对象，将它的各种属性(例如配置文件的名称)设为你希望的值，然后将得到的 `AppDomainSetup` 对象引用传给 `CreateDomain` 方法。

`CreateDomain`方法内部会在进程中新建一个 AppDomain，该 AppDomain 将被赋予指定的友好名称、安全性和配置设置。新 AppDomain 有自己的 Loader 堆，这个堆目前是空的，因为还没有程序集加载到新 AppDomain 中。创建 AppDomain 时，CLR 不在这个 AppDomain 中创建任何线程；AppDomain 中也不会运行代码，除非显式地让一个线程调用 AppDomain 中的代码。

现在，要在新 AppDomain 中创建类型的实例，首先要将程序集加载到新 AppDomain 中，然后构造程序集中定义的类型的实例。这就是 AppDomain 的公共实例方法 `CreateInstanceAndUnwrap` 所做的事情。调用这个方法时我传递了两个 `String` 实参；第一个标识了想在新 AppDomain(`ad2`变量引用的那个 AppDomain)中加载的程序集；第二个标识了想构建其实例的那个类型的名称("`MarshalByRefType`")。在内部，`CreateInstanceAndUnwrap` 方法导致调用线程从当前 AppDomain 切换新 AppDomain 。现在，线程(当前正在调用`CreateInstanceAndUnwrap`)将制定程序集加载到新 AppDomain 中，并扫描程序集的类型定义元数据表，查找指定类型("MarshalByRefType")。找到类型后，线程调用 `MarshalByRefType` 的无参构造器。现在，线程又切回默认 AppDomain ，使 `CreateInstanceAndUnwrap` 能返回对新`MarshalByRefType`对象的引用。

> 注意 `CreateInstanceAndUnwrap` 方法的一些重载版本允许在调用类型的构造器时传递实参。

所有这一切听起来都很美好，但还有一个问题；CLR 不允许一个 AppDomain 中的变量(根)引用另一个 AppDomain 中创建的对象。如果 `CreateInstanceAndUnwrap` 直接返回对象引用，隔离性就会被打破，而隔离是 AppDomain 的全部目的！因此，`CreateInstanceAndUnwrap`在返回对象引用，隔离性就会被打破，而隔离是 AppDomain 的全部目的！因此，`CreateInstanceAndUnwrap` 在返回对象引用前要执行一些额外的逻辑。

我们的 `MarshalByRefType` 类型从一个很特别的基类 `System.MarshalByRefObject` 派生。当 `CreateInstanceAndUnwrap` 发现它封送的一个对象的类型派生自 `MarshalByRefObject` 时，CLR 就会跨 AppDomain 边界按引用封送对象。下面讲述了按引用将对象从一个 AppDomain(源 AppDomain，这是真正创建对象的地方)封送到另一个 AppDomain(目标 AppDomain，这是调用 `CreateInstanceAndUnwrap` 的地方)的具体含义。

源 AppDomain 想向目标 AppDomain 发送或返回对象引用时，CLR 会在目标 AppDomain 的Loader 堆中定义一个代理类型。代理类型是用原始类型完全一样；有完全一样的实例成员(属性、事件和方法)。但是，实例字段不会成为(代理)类型的一部分，我稍后会具体解释这一点。代理类型确实定义了几个(自己的)实例字段，但这些字段和原始类型的不一致。`````````````````````