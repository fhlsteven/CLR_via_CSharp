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

多次调用 `Register`，多个回调方法都会调用。这些回调方法可能抛出未处理的异常。如果调用 `CancellationTokenSource` 的 `Cancel` 方法，向它传递 `true`，那么抛出了未处理异常的第一个回调方法会阻止其他回调方法的执行，抛出的异常也会从 `Cancel` 中抛出。如果调用 `Cancel` 并向它传递 `false`，那么登记的所有回调方法都会调用。所有未处理的异常都会添加到一个集合中。所有回调方法都执行好后，其中任何一个抛出了未处理的异常，`Cancel` 就会抛出一个 ``

## <a name="27_5">27.5 任务</a>


## <a name="27_6">27.6 `Parallel` 的静态 `For`，`ForEach` 和 `Invoke`方法</a>
## <a name="27_7">27.7 并行语言集成查询(PLINQ)</a>
## <a name="27_8">27.8 执行定时的计算限制操作</a>
## <a name="27_9">27.9 线程池如何管理线程</a>