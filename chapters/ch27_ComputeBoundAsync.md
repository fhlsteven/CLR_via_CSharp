# 第 27 章 计算限制的异步操作

本章内容：

* <a href="#27_1">CLR 线程池基础</a>
* <a href="#27_2">执行简单的计算限制操作</a>
* <a href="#27_3">执行上下文</a>
* <a href="#27_4">协作式取消和超时</a>
* <a href="#27_5">任务</a>
* <a href="#27_6">`Parallel` 的静态 `For`，`ForEach` 和 `Invoke`方法</a>
* <a href="#27_7">并行语言集成查询(PLINQ)</a>
* <a href="#27_8">执行定时的计算限制操作</a>
* <a href="#27_9">线程池如何管理线程</a>

本章将讨论以异步方式操作的各种方式。异步的计算限制操作要用其他线程执行，例子包括编译代码、拼写检查、语法检测、电子表格重计算、音频或视频数据转码以及生成图像的缩略图。在金融和工程应用程序中，计算限制的操作也是十分普遍的。

大多数应用程序都不会花太多时间处理内存数据或执行计算。要验证这一点，可以打开“任务管理器”，选择“性能”标签。如果 CPU 利用率不到 100%(大多数时候都如此)，就表明当前运行的进程没有使用由计算机的 CPU 内核提供的全部计算能力。CPU 利用率低于 100% 时，进程中的部分(但不是全部)线程根本没有运行。相反，这些线程正在等待某个输入或输出操作。例如，这些线程可能正在等待一个计时器到期<sup>①</sup>；等待在数据库/Web服务器/文件/网络/其他硬件设备中读取或写入数据；或者等待按键、鼠标移动或鼠标点击等。执行 I/O 限制的操作时，Microsoft Windows 设备驱动程序让硬件设备为你“干活儿”，但 CPU 本身“无所事事”。由于线程不在 CPU 上运行，所以“任务管理器”说 CPU 利用率很低。

> ① 计时器“到期”(come due)的意思是还有多久触发它。

但是，即使 I/O 限制非常严重的应用程序也要对接收到的数据执行一些计算，而并行执行这些计算能显著提升应用程序的吞吐能力。本章首先介绍 CLR 的线程池，并解释了和它的工作和使用有关的一些基本概念。这些信息非常重要。为了设计和实现可伸缩的、响应灵敏和可靠的应用程序和组件，线程池是你必须采用的核心技术。然后，本章展示了通过线程池执行计算限制操作的各种机制。

## <a name="27_1">27.1 CLR 线程池基础</a>

如第 26 章所述，创建和销毁线程时一个昂贵的操作，要耗费大量时间。另外，太多的线程会浪费内存资源。由于操作系统必须调度可运行的线程并执行上下文切换，所以太多的线程还对性能不利。为了改善这个情况，CLR 包含了代码来管理它自己的**线程池**(thread pool)。线程池是你的应用程序能使用的线程集合。每 CLR 一个线程池；这个线程池由 CLR 控制的所有 AppDomain 共享。如果一个进程中加载了多个 CLR，那么每个 CLR 都有它自己的线程池。

CLR 初始化时，线程池中是没有线程的。在内部，线程池维护了一个操作请求队列。应用程序执行一个异步操作时，就调用某个方法，将一个记录项(entry)追加到线程池的队列中。线程池的代码从这个队列中提取记录项，将这个记录项派发(dispatch)给一个线程池线程。如果线程池中没有线程，就创建一个新线程。创建线程会造成一定的性能损失(前面已讨论过了)。然而，当线程池线程完成任务后，线程不会被销毁。相反，线程会返回线程池，在那里进入空闲状态，等待响应另一个请求。由于线程不销毁自身，所以不再产生额外的性能损失。

如果你的应用程序向线程池发出许多请求，线程池会尝试只用这一个线程来服务所有请求。然而，如果你的应用程序发出请求的速度超过了线程池线程处理它们的速度，就会创建额外的线程。最终，你的应用程序的所有请求都能由少量线程处理，所以线程池不必创建大量线程。

如果你的应用程序停止向线程池发出请求，池中会出现大量什么都不做的线程。这是对内存资源的浪费。所以，当一个线程池线程闲着没事儿一段时间之后(不同版本的 CLR 对这个时间的定义不同)，线程会自己醒来终止自己以释放资源。线程终止自己会产生一定的性能损失。然而，线程终止自己是因为它闲的慌，表明应用程序本身就么有做太多的事情，所以这个性能损失关系不大。

线程池可以只容纳少量线程，从而避免浪费资源；也可以容纳更多的线程，以利用多处理器、超线程处理器和多核处理器。它能在这两种不同的状态之间从容地切换。线程池是启发式的。如果应用程序需要执行许多任务，同时有可能的 CPU，那么线程池会创建更多的线程。应用程序负载减轻，线程池线程就终止它们自己。

## <a name="27_2">27.2 执行简单的计算限制操作</a>

要将一个异步的计算限制操作放到线程池的队列中，通常可以调用 `ThreadPool` 类定义的以下方法之一：

```C#
static bool QueueUserWorkItem(WaitCallback callBack);
static bool QueueUserWorkItem(WaitCallback callBack, Object state);
```

这些方法向线程池的队列添加一个“工作项”(work item)以及可选的状态数据。然后，所有方法会立即返回。工作项其实就是由 `callBack` 参数标识的一个方法，该方法将由线程池线程调用。可向方法传递一个 `state` 实参(状态数据)。无 `state` 参数的那个版本的 `QueueUserWorkItem` 则向回调方法传递`null`。最终，池中的某个线程会处理工作项，造成你指定的方法被调用。你写的回调方法必须匹配 `System.Threading.WaitCallback`委托类型，后者的定义如下：

`delegate void WaitCallback(Object state);`

> 注意 `WaitCallback` 委托、`TimerCallback` 委托(参见本章 27.8 节“执行定时计算限制操作”的讨论)和 `ParameterizedThreadStart` 委托(在第 26 章“线程基础” 中讨论)签名完全一致。定义和该签名匹配的方法后，使用 `ThreadPool.QueueUserWorkItem`、`System.Threading.Timer` 和 `System.Threading.Thread` 对象都可调用该方法。

```C#
using System;
using System.Threading;

public static class Program {
    public static void Main() {
        Console.WriteLine("Main thread: queuing an asynchronous operation");
        ThreadPool.QueueUserWorkItem(ComputeBoundOp, 5);
        Console.WriteLine("Main thread: Doing other work here...");
        Thread.Sleep(10000);    // 模拟其他工作(10秒)
        Console.WriteLine("Hit <Enter> to end this program...");
        Console.ReadLine();
    }

    private static void ComputeBoundOp(Object state) {
        // 这个方法由一个线程池线程执行

        Console.WriteLine("In ComputeBoundOp: state={0}", state);
        Thread.Sleep(1000);             // 模拟其他工作(1秒)

        // 这个方法返回后，线程回到池中，等待另一个任务
    }
}
```

编译并运行上述代码得到以下输出：

```cmd
Main thread: queuing an asynchronous operation
Main thread: Doing other work here...
In ComputeBoundOp: state=5
```

有时也得到以下输出：

```cmd
Main thread: queuing an asynchronous operation
In ComputeBoundOp: state=5
Main thread: Doing other work here...
```

之所以输出行的顺序会发生变化，是因为两个方法相互之间是异步运行的。Windows 调度器决定先调度哪一个线程。如果应用程序在多核机器上运行，可能同时调度它们。

> 注意 一旦回调方法抛出未处理的异常，CLR 会终止进程(除非宿主强加了它自己的策略)。未处理异常的详情已在第 20 章“异常和状态管理”进行了讨论。

> 注意 对于 Windows Store 应用，`System.Threading.ThreadPool` 类是没有公开的。但在使用 `System.Threading.Tasks` 命名空间中的类型时，这个类被间接地使用(详情参见本章稍后的 27.5 节“任务”)。

## <a name="27_3">27.3 执行上下文</a>

每个线程都关联了一个执行上下文数据结构。**执行上下文**(execution context)包括的东西有安全设置(压缩栈、`Thread` 的 `Principal`属性和 Windows 身份)、宿主设置(参见 `System.Threading.HostExecutionContextManager`)以及逻辑调用上下文数据(参见`System.Runtime.Remoting.Messaging.CallContext` 的 `LogicalSetData` 和 `LogicalGetData`方法)。线程执行它的代码时，一些操作会受到线程执行上下文设置(尤其是安全设置)的影响。理想情况下，每当一个线程(初始线程)使用另一个线程(辅助线程)执行任务时，前者的执行上下文应该流向(复制到)辅助线程。这就确保了辅助线程执行的任何操作使用的是相同的安全设置和宿主设置。还确保了再初始线程的逻辑调用上下文中存储的任何数据都适用于辅助线程。

默认情况下，CLR 自动造成初始线程的执行上下文“流向”任何辅助线程。这造成将上下文信息传给辅助线程，但这会对性能造成一定影响。这是因为执行上下文中包含大量信息，而收集所有这些信息，再把它们复制到辅助线程，要耗费不少时间。如果辅助线程又采用了更多的辅助线程，还必须创建和初始化更多的执行上下文数据结构。

`System.Threading`命名空间有一个 `ExecutionContext` 类，它允许你控制线程的执行上下文如何从一个线程“流”向另一个。下面展示了这个类的样子：

```C#
public sealed class ExecutionContext : IDisposable, ISerializable {
    [SecurityCritical] public static AsyncFlowControl SuppressFlow();
    public static void RestoreFlow();
    public static Boolean IsFlowSuppressed();

    // 为列出不常用的方法
}
```

可用这个类阻止执行上下文流动以提升应用程序的性能。对于服务器应用程序，性能的提升可能非常显著。但客户端应用程序的性能提升不了多少。另外，由于 `SuppressFlow` 方法用 `[SecurityCritical]` 特性进行了标识，所以在某些客户端应用程序(比如 Silverlight)中是无法调用的。当然，只有在辅助线程不需要或者不访问上下文信息时，才应阻止执行上下文的流动。当然，只有在辅助线程不需要或者不访问上下文信息时，才应阻止执行上下文的流动。如果初始线程的执行上下文不流向辅助线程，辅助线程会使用上一次和它关联的任意执行上下文。在这种情况下，辅助线程不应执行任何要依赖于执行上下文状态(不如用户的 Windows 身份)的代码。

下例展示了向 CLR 的线程池队列添加一个工作项的时候，如何通过阻止执行上下文的流动来影响线程逻辑调用上下文中的数据<sup>①</sup>：

> ① 添加到逻辑调用上下文的项必须是可序列化的，详情参见第 24 章“运行时序列化”。对于包含了逻辑调用上下文数据项的执行上下文，让它流动起来可能严重损害性能，因为为了捕捉执行上下文，需要对所有数据项进行序列化和反序列化。

```C#
public static void Main() {
    // 将一些数据放到 Main 线程的逻辑调用上下文中
    CallContext.LogicalSetData("Name", "Jeffrey");

    // 初始化要由一个线程池线程做的一些工作
    // 线程池线程能访问逻辑调用上下文数据
    ThreadPool.QueueUseWorkItem(
        state => Console.WriteLine("Name={0}", CallContext.LogicalGetData("Name")));

    // 现在，阻止 Main 线程的执行上下文的流动
    ExecutionContext.SuppressFlow();

    // 初始化要由线程池线程做的工作，
    // 线程池线程不能访问逻辑调用上下文数据
    ThreadPool.QueueUserWorkItem(state => Console.WriteLine("Name={0}", CallContext.LogicalGetData("Name")));

    // 恢复 Main 线程的执行上下文的流动，
    // 以免将来使用更多的线程池线程
    ExecutionContext.RestoreFlow();
    ...
    Console.ReadLine();
}
```

编译并运行上述代码得到以下输出：

```cmd
Name=Jeffrey
Name=
```

## <a name="27_4">27.4 协作式取消和超时</a>

Microsoft .NET Framework 提供了标准的**取消操作**模式。这个模式是**协作式**的，意味着要取消的操作必须显式支持**取消**。换言之，无论执行操作的代码，还是试图取消操作的代码，还是试图取消操作的代码，都必须使用本节提到的类型。对于长时间运行的计算限制操作，支持取消是一件很“棒”的事情。所以，你应该考虑为自己的计算限制操作添加取消能力。本节将解释具体如何做。但首先解释一下作为标准协作式取消模式一部分的两个 FCL 类型。

取消操作首先要创建一个 `System.Threading.CancellationTokenSource` 对象。这个类看起来像下面这样：

```C#
public sealed class CancellationTokenSource : IDisposable {  // 一个引用类型
    public CancellationTokenSource();
    public void Dispose();          // 释放资源(比如 WaitHandle)

    public Boolean IsCancellationRequested { get; }
    public CancellationToken Token { get; }

    public void Cancel();   // 内部调用 Cancel 并传递 false
    public void Cancel(Boolean throwOnFirstException);
    ...
}
```

这个对象包含了和管理取消有关的所有状态。构造好一个 `CancellationTokenSource`(一个引用类型)之后，可从它的 `Token` 属性获得一个或多个`CancellationToken`(一个值类型)实例，并传给你的操作，使操作可以取消。以下是 `CancellationToken` 值类型最有用的成员：

```C#
public struct CancellationToken {           // 一个值类型
    public static CancellationToken None { get;	}       // 很好用

    public Boolean IsCancellationRequested { get; }     // 由非通过 Task 调用的操作调用
    public void ThrowIfCancellationRequested();         // 由通过 Task 调用的操作调用

    // CancellationTokenSource 取消时，WaitHandle 会收到信号
    public WaitHandle WaitHandle { get; }
    // GetHashCode，Equals，operator== 和 operator!= 成员未列出

    public bool CanBeCanceled { get; } // 很少使用

    public CancellationTokenRegistration Register(Action<Object> callback, Object state, Boolean useSynchronizationContext);        // 未列出更简单的重载版本
}
```

`CancellationToken` 实例是轻量级值类型，包含单个私有字段，即对其 `CancellationTokenSource` 对象的引用。在计算限制操作的循环中，可定时调用 `CancellationToken` 的 `IsCancellationRequested` 属性，了解循环是否应该提前终止，从而终止计算限制的操作。提前终止的好处在于，CPU 不需要再把时间浪费在你对结果不感兴趣的操作上。以下代码将这些概念全部梳理了一遍：

```C#
internal static class CancellationDemo {
    public static void Go() {
        CancellationTokenSource cts = new CancellationTokenSource();

        // 将 CancellationToken 和 “要数到的数” (number-to-count-to)传入操作
        ThreadPool.QueueUserWorkItem(o => Count(cts.Token, 1000));

        Console.WriteLine("Press <Enter> to cancel the operation.");
        Console.ReadLine();
        cts.Cancel();   // 如果 Count 方法已返回，Cancel 没有任何效果
        // Cancel 立即返回，方法从这里继续运行...

        Console.ReadLine();
    }

    private static void Count(CancellationToken token, Int32 countTo) {
        for (Int32 count = 0; count < countTo; count++) {
            if (token.IsCancellationRequested) {
                Console.WriteLine("Count is cancelled");
                break;  // 退出循环以停止操作
            }
            Console.WriteLine(count);
            Thread.Sleep(200);      // 出于演示目的而浪费一些时间
        }
        Console.WriteLine("Count is done");
    }
}
```

> 注意 要执行一个不允许被取消的操作，可向该操作传递通过调用`CancellationToken`的静态`None`属性而返回的`CancellationToken`。该属性返回一个特殊的`CancellationToken`实例，它不和任何`CancellationTokenSource`对象关联(实例的私有字段为`null`)。由于没有`CancellationTokenSource`，所以没有代码能调用 `Cancel`。一个操作如果查询这个特殊 `CancellationToken` 的`IsCancellationRequested`属性，将总是返回`false`。使用某个特殊`CancellationToken`实例查询`CancellationToken`的`CanBeCanceled`属性，属性会返回`false`。相反，对于通过查询`CancellationTokenSource`对象的`Token`属性而获得的其他所有`CancellationToken`实例，该属性(`CancellationToken`)都会返回`true`。

如果愿意，可调用 `CancellationTokenSource` 的 `Register` 方法登记一个或多个在取消一个 `CancellationTokenSource` 时调用的方法。要向方法传递一个 `Action<Object>` 委托；一个要通过委托传给回到(方法)的状态值；以及一个`Boolean`值(名为`useSynchronizationContext`)，该值指明是否要使用调用线程的 `SynchronizationContext` 来调用委托。如果为 `useSynchronizationContext` 参数传递 `false`，那么调用`Cancel` 的线程会顺序调用已登记的所有方法。为 `useSynchronizationContext` 参数传递 `true`，则回调(方法)会被 send(而不是post<sup>①</sup>)给已捕捉的 `SynchronizationContext` 对象，后者决定由哪个线程调用回调(方法)。`SynchronizationContext` 类的详情将在 28.9 节“应用程序及其线程处理模型”讨论。

> ① 简单地说，如果执行 send 操作，要等到在目标线程哪里处理完毕之后才会返回。在此期间，调用线程会被阻塞。这相当于同步调用。而如果执行 post 操作，是指将东西 post 到一个队列中便完事儿，调用线程立即返回，相当于异步调用。————译注

> 注意 向被取消的 `CancellationTokenSource` 登记一个回调方法，将由调用 `Register` 的线程调用回调方法(如果为 `useSynchronizationContext` 参数传递了 `true` 值，就可能要通过调用线程的 `SynchronizationContext` 进行)。

多次调用 `Register`，多个回调方法都会调用。这些回调方法可能抛出未处理的异常。如果调用 `CancellationTokenSource` 的 `Cancel` 方法，向它传递 `true`，那么抛出了未处理异常的第一个回调方法会阻止其他回调方法的执行，抛出的异常也会从 `Cancel` 中抛出。如果调用 `Cancel` 并向它传递 `false`，那么登记的所有回调方法都会调用。所有未处理的异常都会添加到一个集合中。所有回调方法都执行好后，其中任何一个抛出了未处理的异常，`Cancel` 就会抛出一个 `AggregateException`，该异常实例的 `InnerExceptions` 属性被设为已抛出的所有异常对象的集合。如果登记的所有回调方法都没有抛出未处理的异常，那么 `Cancel` 直接返回，不抛出任何异常。

> 重要提示 没有办法将 `AggregateException` 的 `InnerExceptions` 集合中的一个异常对象和特的操作对应起来；你只知道某个操作出错，并通过异常类型知道出了什么错。要跟踪错误的具体位置，需要检查异常对象的 `StackTrace` 属性，并手动扫描你的源代码。

`CancellationToken` 的 `Register` 方法返回一个 `CancellationTokenRegistration`，如下所示：

```C#
public readonly struct CancellationTokenRegistration : 
    IEquatable<CancellationTokenRegistration>, IDisposable {
    public void Dispose();
    // GetHashCode, Equals, operator== 和 operator!=成员未列出
}
```

可以调用 `Dispose` 从关联的 `CancellationTokenSource` 中删除已登记的回调；这样一来，在调用 `Cancel` 时，便不会再调用这个回调。以下代码演示了如何向一个 `CancellationTokenSource` 登记两个回调：

```C#
var cts = new CancellationTokenSource();
cts.Token.Register(() => Console.WriteLine("Canceled 1"));
cts.Token.Register(() => Console.WriteLine("Canceled 2"));

// 出于测试的目的，让我们取消它，以便执行 2 个回调
cts.Cancel();
```

运行上述代码，一旦调用 `Cancel` 方法，就会得到以下输出： 

```cmd
Canceled 2
Canceled 1
```

最后，可以通过链接另一组 `CancellationTokenSource` 来新建一个 `CancellationTokenSource` 对象。任何一个链接的 `CancellationTokenSource` 被取消，这个新的 `CancellationTokenSource` 对象就会被取消。以下代码对此进行了演示：

```C#
// 创建一个 CancellationTokenSource
var cts1 = new CancellationTokenSource();
cts1.Token.Register(() => Console.WriteLine("cts1 canceled"));

// 创建另一个 CancellationTokenSource
var cts2 = new CancellationTokenSource();
cts2.Token.Register(() => Console.WriteLine("cts2 canceled"));

// 创建一个新的 CancellationTokenSource，它在 cts1 或 cts2 取消时取消
var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cts2.Token);
linkedCts.Token.Register(() => Console.WriteLine("linkedCts canceled"));

// 取消其中一个 CancellationTokenSource 对象(我选择cts2)
cts2.Cancel();

// 显示哪个 CancellationTokenSource 对象被取消了
Console.WriteLine("cts1 canceled={0}, cts2 canceled={1}, linkedCts={2}",
    cts1.IsCancellationRequested, cts2.IsCancellationRequested, linkedCts.IsCancellationRequested);
```

运行上述代码得到以下输出：

```cmd
linkedCts canceled
cts2 canceled
cts1 canceled=False, cts2 canceled=True, linkedCts=True
```

在很多情况下，我们需要在过一段时间之后才取消操作。例如，服务器应用程序可能会根据客户端的请求而开始计算。但必须在 2 秒钟之内有响应，无论此时工作是否已经完成。有的时候，与其等待漫长时间获得一个完整的结果，还不如在短时间内报错，或者用部分计算好的结果进行响应。幸好，`CancellationTokenSource` 提供了在指定时间后自动取消的机制。为了利用这个机制，要么用接受延时参数的构造构造一个 `CancellationTokenSource` 对象，要么调用 `CancellationTokenSource` 的 `CancelAfter` 方法。

```C#
public sealed class CancellationTokenSource : IDisposable {
    public CancellationTokenSource(int millisecondsDelay);
    public CancellationTokenSource(TimeSpan delay);
    public void CancelAfter(int millisecondsDelay);
    public void CancelAfter(TimeSpan delay);
    ...
}
```

## <a name="27_5">27.5 任务</a>

很容易调用 `ThreadPool` 的 `QueueUserWorkItem` 方法发起一次异步的计算限制操作。但这个技术有许多限制。最大的问题是没有内建的机制让你知道操作在什么时候完成，也没有机制在操作完成时获得返回值。为了克服这些限制(并解决其他一些问题)，Microsoft 引入了 **任务**的概念。我们通过 `System.Threading.Tasks` 命名空间中的类型来使用任务。

所以，不是调用 `ThreadPool` 的 `QueueUserWorkItem` 方法，而是用任务来做相同的事情：

```C#
ThreadPool.QueueUserWorkItem(ComputeBoundOp, 5);    // 调用 QueueUserWorkItem
new Task(ComputeBoundOp, 5).Start();                // 用 Task 来做相同的事情
Task.Run(() => ComputeBoundOp(5));                  // 另一个等价的写法
```

第二行代码创建 `Task` 对象并立即调用 `Start` 来调度任务。当然，也可先创建好 `Task` 对象再调用 `Start`。例如，可以创建一个 `Task` 对象再调用`Start` 来调度任务。由于创建 `Task` 对象并立即调用 `Start` 是常见的编程模式，所以可以像最后一行代码展示的那样调用 `Task` 的静态 `Run` 方法。

为了创建一个 `Task`，需要调用构造器并传递一个 `Action` 或 `Action<Object>` 委托。这个委托就是你想执行的操作。如果传递的是期待一个`Object` 的方法，还必须向 `Task` 的构造器传递最终要传给操作的实参。调用 `Run` 时可以传递一个 `Action` 或 `Func<TResult>` 委托来指定想要执行的操作。无论调用构造器还是`Run`，都可选择传递一个 `CancellationToken`，它使 `Task` 能在调度前取消(详情参见稍后的 27.5.2 节“取消任务”)。

还可选择向构造器传递一些 `TaskCreationOptions` 标志来控制 `Task` 的执行方式。`TaskCreationOptions` 枚举类型定义了一组可按位 OR 的标志。定义如下：

```C#
[Flags, Serializable]
public enum TaskCreationOptions {
	None           = 0x0000,      // 默认
	
    // 提议 TaskScheduler 你希望该任务尽快执行
	PreferFairness = 0x0001,
	
    // 提议 TaskScheduler 应尽可能地创建线程池线程
	LongRunning    = 0x0002,
	
    // 该提议总是被采纳：将一个 Task 和它的父 Task 关联(稍后讨论)
	AttachedToParent = 0x0004,
	
    // 该提议总是被采纳：如果一个任务试图和这个父任务连接，它就是一个普通任务，而不是子任务
	DenyChildAttach = 0x0008,
	
    // 该提议总是被采纳：强迫子任务使用默认调度器而不是父任务的调度器
	HideScheduler = 0x0010
}
```

有的标志只是“提议”，`TaskScheduler` 在调度一个 `Task` 时，可能会、也可能不会采纳这些提议。不过，`AttachedToParent`，`DenyChildAttach` 和`HideScheduler` 总是得以采纳，因为它们和 `TaskScheduler` 本身无关。`TaskScheduler` 对象的详情将在 27.5.7 节“任务调度器”讨论。

### 27.5.1 等待任务完成并获取结果 

可等待任务完成并获取结果。例如，以下 `Sum` 方法在 `n` 值很大的时候会执行较长时间：

```C#
private static Int32 Sum(Int32 n) {
    Int32 sum = 0;
    for (; n > 0; n--)
        checked { sum += n; }       // 如果 n 太大，会抛出 System.OverflowException
    return sum;
}
```

现在可以构造一个 `Task<TResult>`对象(派生自 `Task`)，并为泛型 `TResult` 参数传递计算限制操作的返回类型。开始任务之后，可等待它完成并获得结果，如以下代码所示：

```C#
// 创建一个 Task(现在还没有开始运行)
Task<Int32> t = new Task<Int32>(n => Sum((Int32)n), 1000000000);

// 可以后再启动任务
t.Start();

// 可选择显式等待任务完成
t.Wait();   // 注意：还有一些重载的版本能接受 timeout/CancellationToken 值

// 可获得结果(Result 属性内部会调用 Wait)
Console.WriteLine("The Sum is: " + t.Result);   // 一个 Int32 值
```

如果计算限制的任务抛出未处理的异常，异常会被“吞噬”并存储到一个集合中，而线程池线程可以返回到线程池中。调用 `Wait` 方法或者 `Result` 属性时，这些成员会抛出一个 `System.AggregateException` 对象。

> 重要提示 线程调用 `Wait` 方法时，系统检查线程要等待的 `Task` 是否已开始执行。如果是，调用 `Wait` 的线程来执行 `Task`。在这种情况下，调用`Wait` 的线程不会阻塞；它会执行 `Task` 并立即返回。好处在于，没有线程会被阻塞，所以减少了对资源的占用(因为不需要创建一个线程来替代被阻塞的线程)，并提升了性能(因为不需要花时间创建线程，也没有上下文切换)。不好的地方在于，假如线程在调用 `Wait` 前已获得了一个线程同步锁，而 `Task` 试图获取同一个锁，就会造成死锁的线程！

`AggregateException` 类型封装了异常对象的一个集合(如果父任务生成了多个子任务，而多个子任务都抛出了异常，这个集合便可能包含多个异常)。该类型的 `InnerExceptions` 属性返回一个 `ReadOnlyCollection<Exception>` 对象。不要混淆 `InnerExceptions` 属性和 `InnerException` 属性，后者是`AggregateException` 类从 `System.Exception` 基类继承的，在本例中，`AggregateException` 的 `InnerExceptions` 属性的元素 `0` 将引用由计算限制方法(`Sum`)抛出的实际 `System.OverflowException` 对象。

为方便编码，`AggregateException` 重写了 `Exception` 的 `GetBaseException` 方法，返回作为问题根源的最内层的 `AggregateException`(假定集合只有一个最内层的异常)。`AggregateException` 还提供了一个 `Flatten` 方法，它创建一个新的 `AggregateException`， 其 `InnerExceptions` 属性包含一个异常列表，其中的异常是通过遍历原始 `AggregateException` 的内层异常层次结构而生成的。最后，`AggregateException` 还提供了一个 `Handle` 方法，它为 `AggregateException` 中包含的每个异常都调用一个回调方法。然后，回调方法可以为每个异常决定如何对其进行处理：回调返回 `true` 表示异常已处理；返回 `false` 表示未处理。调用 `Handle` 后，如果至少有一个异常没有被处理，就创建一个新的 `AggregateException` 对象，其中只包含未处理的异常，并抛出这个新的 `AggregateException` 对象。本章以后会展示使用了 `Flatten` 和 `Handle` 方法的例子。

> 重要提示 如果一致不调用 `Wait` 或 `Result`，或者一直不查询 `Task` 的 `Exception` 属性，代码就一直注意不到这个异常的发生。这当然不好，因为程序遇到了未预料到的问题，而你居然没注意到。为了帮助你检测没有被注意到。为了帮助你检测没有被注意到的异常，可以向 `TaskScheduler` 的静态 `UnobservedTaskException` 事件登记一个回调方法。每次放一个 `Task` 被垃圾回收时，如果存在一个没有被注意到的异常，CLR 的终结器线程就会引发这个事件。一旦引发，就会向你的事件处理方法传递一个 `UnobservedTaskExceptionEventArgs` 对象，其中包含你没有注意到的 `AggregateException`。

除了等待单个任务，`Task` 类还提供了两个静态方法，允许线程等待一个 `Task` 对象数组。其中，`Task` 的静态 `WaitAny` 方法会阻塞调用线程，直到数组中的任何 `Task` 对象完成。方法返回 `Int32` 数组索引值，指明完成的是哪个 `Task` 对象。方法返回后，线程被唤醒并继续运行。如果发生超时，方法将返回 `-1`。如果 `WaitAny` 通过一个 `CancellationToken` 取消，会抛出一个 `OperationCanceledException`。

类似地，`Task` 类还有一个静态 `WaitAll` 方法，它阻塞调用线程，直到数组中的所有 `Task` 对象完成。如果所有 `Task` 对象都完成，`WaitAll` 方法返回`true`。发生超时则返回 `false`。如果 `WaitAll` 通过一个 `CancellationToken`取消，会抛出一个 `OperationCanceledException`。

### 27.5.2 取消任务

可用一个 `CancellationTokenSource` 取消 `Task`。首先必须修订前面的 `Sun` 方法，让它接受一个 `CancellationToken`：

```C#
private static Int32 Sum(CancellationToken ct, Int32 n) {
    Int32 sum = 0;
    for (; n > 0; n--) {

        // 在取消标志引用的 CancellationTokenSource 上调用 Cancel，
        // 下面这行代码就会抛出 OperationCanceledException
        ct.ThrowIfCancellationRequested();

        checked { sum += n; }  // 如果 n 太大，会抛出 System.OverflowException
    }    
    return sum;
}
```

循环(负责执行计算限制的操作)中调用 `CancellationToken` 的 `ThrowIfCancellationRequested` 方法定时检查操作是否已取消。这个方法与`CancellationToken` 的 `IsCancellationRequested` 属性相似(27.4 节“协作式取消和超时”已经讨论过这个属性)。但如果 `CancellationTokenSource` 已经取消，`ThrowIfCancellationRequested` 会抛出一个 `OperationCanceledException`。之所以选择抛出异常，是因为和 `ThreadPool` 的 `QueueUserWorkItem` 方法初始化的工作项不同，任务有办法表示完成，任务甚至能返回一个值。所以，需要采取一种方式将已完成的任务和出错的任务区分开。而让任务抛出异常，就可以知道任务没有一直运行到结束。

现在像下面这样创建 `CancellationTokenSource` 和 `Task` 对象：

```C#
CancellationTokenSource cts = new CancellationTokenSource();
Task<Int32> t = Task.Run(() => Sum(cts.Token, 1000000000), cts.Token);

// 在之后的某个时间，取消 CancellationTokenSource 以取消 Task
cts.Cancel();       // 这是异步请求，Task 可能已经完成了

try {
    // 如果任务已取消，Request 会抛出一个 AggregateException
    Console.WriteLine("The sum is: " + t.Result);   // 一个 Int32 值
}
catch (AggregateException x) {
    // 将任何 OperationCanceledException 对象都视为已处理。
    // 其他任何异常都造成抛出一个新的 AggregateException，
    // 其中只包含未处理的异常
    x.Handle(e => e is OperationCanceledException);
    // 所有异常都处理好之后，执行下面这一行
    Console.WriteLine("Sum was canceled");
}
```

可在创建 `Task` 时将一个 `CancellationToken` 传给构造器(如上例所示)，从而将两者关联。如果 `CancellationToken` 在 `Task` 调度前取消，`Task`会被取消，永远都不执行<sup>①</sup>。但如果 `Task` 已调度(通过调用 `Start` 方法<sup>②</sup>)，那么`Task`的代码只有显示支持取消，其操作才能在执行期间取消。遗憾的是，虽然 `Task` 对象关联了一个 `CancellationToken`，但却没有办法访问它。因此，必须在`Task` 的代码中获得创建`Task` 对象时的同一个`CancellationToken`。为此，最简单的办法就是使用一个 lambda 表达式，将 `CancellationToken` 作为闭包变量“传递”(就像上例那样)。

> ① 顺便说一句，如果一个任务还没有开始就试图取消它，会抛出一个 `InvalidOperationException`。
> ② 调用静态 `Run` 方法会自动创建 `Task`对象并立即调用`Start`。  ———— 译注

### 27.5.3 任务完成时自动启动新任务

伸缩性好的软件不应该使线程阻塞。调用 `Wait`，或者在任务尚未完成时查询任务的 `Result` 属性<sup>③</sup>，极有可能造成线程池创建新线程，这增大了资源的消耗，也不利于性能和伸缩性。幸好，有更好的办法可以知道一个任务在什么时候结束运行。任务完成时可启动另一个任务。下面重写了之前的代码，它不阻塞任何线程：

> ③ `Result` 属性内部会调用 `Wait`。 ———— 译注

```C#
// 创建并启动一个 Task，继续另一个任务
Task<Int32> t = Task.Run(() => Sum(CancellationToken.None, 1000));

// ContinueWith 返回一个 Task，但一般都不需要再使用该对象(下例的 cwt)
Task cwt = t.ContinueWith(task => Concole.WriteLine("The sum is: " + task.Result));
```

现在，执行 `Sum` 的任务完成时会启动另一个任务(也在某个线程池线程上)以显示结果。执行上述代码的线程不会进入阻塞状态并等待这两个任务中的任何一个完成。相反，线程可以执行其他代码。如果线程本身就是一个线程池线程，它可以返回池中以执行其他操作。注意，执行 `Sum` 的任务可能在调用 `ContinueWith` 之前完成。但这不是一个问题，因为 `ContinueWith` 方法会看到 `Sum` 任务已经完成，会立即启动显示结果的任务。

注意， `ContinueWith` 返回对新 `Task` 对象的引用(我的代码是将该引用放到 `cwt` 变量中)。当然，可以用这个 `Task` 对象调用各种成员(比如 `Wait`，`Result`，甚至 `ContinueWith`)，但一般都忽略这个 `Task` 对象，不再用变量保存对它的引用。

另外，`Task` 对象内部包含了 `ContinueWith` 任务的一个集合。所以，实际可以用一个 `Task` 对象来多次调用 `ContinueWith`。任务完成时，所有`ContinueWith` 任务都会进入线程池的队列中。此外，可在调用 `ContinueWith` 时传递对一组 `TaskContinuationOptions` 枚举值进行按位 OR 运算的结果。前 6 个标志(`None`，`PreferFairness`，`LongRunning`，`AttachedToParent`，`DenyChildAttach` 和 `HideScheduler`)与之前描述的 `TaskCreationOptions` 枚举类型提供的标志完全一致。下面是 `TaskContinuationOptions` 类型的定义：

```C#
[Flags, Serializable]
public enum TaskContinuationOptions
{
    None                    = 0x0000,  // 默认

    // 提议 TaskScheduler 你希望该任务尽快执行.
    PreferFairness          = 0x0001,
    // 提议 TaskScheduler 应尽可能地创建池线程线程
    LongRunning             = 0x0002,
    
    // 该提议总是被采纳：将一个 Task 和它的父 Task 关联(稍后讨论) 
    AttachedToParent        = 0x0004,

    // 任务试图和这个父任务连接将抛出一个 InvalidOperationException
    DenyChildAttach         = 0x0008,

    // 强迫子任务使用默认调度器而不是父任务的调度器
    HideScheduler           = 0x0010,

    // 除非前置任务(antecedent task)完成，否则禁止延续任务完成(取消)
    LazyCancellation        = 0x0020,

    // 这个标志指出你希望由执行第一个任务的线程执行
    // ContinueWith 任务。第一个任务完成后，调用
    // ContinueWith 的线程接着执行 ContinueWith 任务 ①
    ExecuteSynchronously    = 0x80000,

    // 这些标志指出在什么情况下运行 ContinueWith 任务
    NotOnRanToCompletion    = 0x10000,
    NotOnFaulted            = 0x20000,
    NotOnCanceled           = 0x40000,

    // 这些标志是以上三个标志的便利组合
    OnlyOnRanToCompletion   = NotOnRanToCompletion | NotOnFaulted, //0x60000,
    OnlyOnFaulted           = NotOnRanToCompletion | NotOnCanceled,// 0x50000,
    OnlyOnCanceled          = NotOnFaulted | NotOnCanceled,// 0x30000
}
```

> ① `ExecuteSynchronously` 是指同步执行。两个任务都在使用同一个线程一前一后地执行，就称为同步执行。 ————译注

调用 `ContinueWith` 时，可用 `TaskContinuationOptions.OnlyOnCanceled` 标志指定新任务只有在第一个任务被取消时才执行。类速地，`TaskContinuationOptions.OnlyOnFaulted` 标志指定新任务只有在第一个任务抛出未处理的异常时才执行。当然，还可使用 `TaskContinuationOptions.OnlyOnRanToCompletion` 标志指定新任务只有在第一个任务顺利完成(中途没有取消，也没有抛出未处理异常)时才执行。默认情况下，如果不指定上述任何标志，则新任务无论如何都会运行，不管第一个任务如何完成。一个 `Task` 完成时，它的所有未运行的延续任务都被自动取消<sup>②</sup>。下面用一个例子来演示所有这些概念。

> ② 未运行是因为不满足前面说的各种条件。 ———— 译注

```C#
// 创建并启动一个 Task，它有多个延续任务 
Task<Int32> t = Task.Run(() => Sum(10000)); 

// 每个 ContinueWith 都返回一个 Task， 但这些 Task 一般都用不着了
t.ContinueWith(task => Console.WriteLine("The sum is: " + task.Result), 
 TaskContinuationOptions.OnlyOnRanToCompletion); 
t.ContinueWith(task => Console.WriteLine("Sum threw: " + task.Exception.InnerException), 
 TaskContinuationOptions.OnlyOnFaulted); 
t.ContinueWith(task => Console.WriteLine("Sum was canceled"), 
 TaskContinuationOptions.OnlyOnCanceled); 
```

### 27.5.4 任务可以启动子任务

最后，任务支持父/子关系，如以下代码所示：

```C#
Task<Int32[]> parent = new Task<int[]>(() => {
    var results = new Int32[3];     // 创建一个数组来存储结果

    // 这个任务创建并启动 3 个子任务
    new Task(() => results[0] = Sum(10000), TaskCreationOptions.AttachedToParent).Start();
    new Task(() => results[1] = Sum(20000), TaskCreationOptions.AttachedToParent).Start();
    new Task(() => results[2] = Sum(30000), TaskCreationOptions.AttachedToParent).Start();

    // 返回对数组的引用(即使数组元素可能还没有初始化)
    return results;
});

// 付任务及其子任务运行完成后，用一个延续任务显示结果
var cwt = parent.ContinueWith(parentTask => Array.ForEach(parentTask.Result, Console.WriteLine));

// 启动父任务，便于它启动它的子任务
parent.Start();
```

在本例中，父任务闯将并启动三个 `Task` 对象。一个任务创建的一个或多个 `Task` 对象默认是顶级任务，它们与创建它们的任务无关。但 `TaskCreationOptions.AttachedToParent` 标志将一个 `Task` 和创建它的 `Task` 关联，结果是除非所有子任务(以及子任务的子任务)结束运行，否则创建任务(父任务)不认为已经结束。调用 `ContinueWith` 方法创建 `Task` 时，可指定 `TaskCreationOptions.AttachedToParent` 标志将延续任务指定成子任务。

### 27.5.5 任务内部揭秘

每个 `Task` 对象都有一组字段，这些字段构成了任务的状态。其中包括一个 `Int32 ID`(参见`Task`的只读`Id`属性)、代表`Task` 执行状态的一个`Int32`、对父任务的引用、对`Task`创建时指定的 `TaskScheduler` 的引用、对回调方法的引用、对要传给回调方法的对象的引用(可通过`Task`的只读`AsyncState`属性查询)、对 `ExecutionContext` 的引用以及对 `ManualResetEventSlim` 对象的引用。另外，每个 `Task` 对象都有对根据需要创建的补充状态的引用。补充状态包含一个 `CancellationToken` 、一个 `ContinueWithTask` 对象集合、为抛出未处理异常的子任务而准备的一个 `Task` 对象集合等。说了这么多，重点不需要任务的附加功能，那么使用 `ThreadPool.QueueUserWorkItem` 能获得更好的资源利用率。

`Task` 和 `Task<TResult>` 类实现了 `IDisposable` 接口，允许在用完 `Task` 对象后调用 `Dispose`。如今，所有 `Dispose` 方法所做的都是关闭 `ManualResetEventSlim` 对象。但可定义从 `Task` 和 `Task<TResult>` 派生的类，在这些类中分配它们自己的资源，并在它们重写的 `Dispose` 方法中释放这些资源。我建议不要在代码中为 `Task` 对象显式调用 `Dispose`；相反，应该让垃圾回收器自己清理任何不再需要的资源。

## <a name="27_6">27.6 `Parallel` 的静态 `For`，`ForEach` 和 `Invoke`方法</a>

## <a name="27_7">27.7 并行语言集成查询(PLINQ)</a>

## <a name="27_8">27.8 执行定时的计算限制操作</a>

## <a name="27_9">27.9 线程池如何管理线程</a>