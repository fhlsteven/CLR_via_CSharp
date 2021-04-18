# 第 30 章 混合线程同步构造

本章内容

* <a href="#30_1">一个简单的混合锁</a>
* <a href="#30_2">自旋、线程所有权和递归</a>
* <a href="#30_3">FCL 中的混合结构</a>
* <a href="#30_4">著名的双检锁技术</a>
* <a href="#30_5">条件变量模式</a>
* <a href="#30_6">异步的同步构造</a>
* <a href="#30_7">并发集合类</a>

第 29 章“基元线程同步构造”讨论了基元用户模式和内核模式线程同步构造。其他所有线程同步构造都基于它们而构建，而且一般都合并了用户模式和内核模式构造，我们称为**混合线程同步构造**。没有线程竞争时，混合构造提供了基元用户模式构造所具有的性能优势。多个线程竞争一个构造时，混合构造通过基元内核模式的构造来提供不“自旋”的优势(避免浪费 CPU 时间)。由于大多数应用程序的线程都很少同时竞争一个构造，所以性能上的增强可以使你的应用程序表现得更出色。

本章首先展示了如何基于构造来构建混合构造。然后展示了 FCL 自带的许多混合构造，描述了它们的行为，并介绍了如何正确使用它们。我还提供了一些我自己创建的构造，它们通过 Wintellect 的 Power Threading 库免费提供给大家使用，请从 *[http://wintellect.com/Resource-Power-Collections-Library](http://wintellect.com/Resource-Power-Collections-Library)* 下载。

本章末尾展示了如何使用 FCL 的并发集合类来取代混合构造，从而最小化资源使用并提升性能。最后讨论了异步的同步构造，允许以同步方式访问资源，同时不造成任何线程的阻塞，从而减少了资源消耗，并提高了伸缩性。

## <a name="30_1">30.1 一个简单的混合锁</a>

言归正传，下面是一个混合线程同步锁的例子：

```C#
internal sealed class SimpleHybridLock : IDisposable {
    // Int32 由基元用户模式构造 (Interlocked 的方法)使用
    private Int32 m_waiters = 0;

    // AutoResetEvent 是基元内核模式构造
    private readonly AutoResetEvent m_waiterLock = new AutoResetEvent(false);

    public void Enter() {
        // 指出这个线程想要获得锁
        if (Interlocked.Increment(ref m_waiters) == 1)
            return; // 锁可自由使用，无竞争，直接返回
                    
        // 另一个线程拥有锁(发生竞争)，使这个线程等待
        m_waiterLock.WaitOne(); // 这里产生较大的性能影响
        // WaitOne 返回后，这个线程拿到锁了
    }

    public void Leave() {
        // 这个线程准备释放锁
        if (Interlocked.Decrement(ref m_waiters) == 0)
            return; // 没有其他线程正在等待，直接返回

        // 有其他线程正在阻塞，唤醒其中一个
        m_waiterLock.Set(); // 这里产生较大的性能影响
    }

    public void Dispose() { m_waiterLock.Dispose(); }
}
```

`SimpleHybridLock` 包含两个字段：一个 `Int32`，由基元用户模式的构造来操作；以及一个 `AutoResetEvent`，它是一个基元内核模式的构造。为了获得出色的性能，锁要尽量操作 `Int32`，尽量少操作 `AutoResetEvent`。每次构造 `SimpleHybridLock` 对象就会创建 `AutoResetEvent`；和 `Int32` 字段相比，它对性能的影响大得多。本章以后会展示混合构造 `AutoResetEventSlim`；多个线程同时访问锁时，只有在第一次检测到竞争时才会创建 `AutoResetEvent`，这样就避免了性能损失。`Dispose` 方法关闭 `AutoResetEvent`，这也会对性能造成大的影响。

`SimpleHybridLock` 对象在构造和 dispose 时的性能能提升当然很好，但我们应该将更多精力放在它的 `Enter` 和 `Leave` 方法的性能上，因为在对象生存期内，这两个方法要被大量地调用。下面让我们重点关注这些方法。

调用 `Enter` 的第一个线程造成 `Interlocked.Increment` 在 `m_waiters` 字段上加 `1`，使它的值变成 `1`。这个线程发现以前有零个线程正在等待这个锁，所以线程从它的 `Enter` 调用中返回。值得欣赏的是，线程获得锁的速度非常快。现在，如果另一个线程介入并调用`Enter`，这个线程将 `m_waiters` 递增到 `2`，发现锁在另一个线程那里。所以，这个线程会使用 `AutoResetEvent` 对象来调用 `WaitOne`，从而阻塞自身。调用 `WaitOne` 造成线程的代码转变成内核模式的代码，这会对性能产生巨大影响。但线程反正都要停止运行，所以让线程花点时间来完全停止，似乎也不是太坏。好消息是，线程现在会阻塞，不会因为在 CPU 上“自旋”而浪费 CPU 时间。(29.3.3 节“实现简单的自旋锁”引入的 `SimpleSpinLock` 的 `Enter` 方法就会这样“自旋”。)

再来看看 `Leave` 方法。一个线程调用 `Leave` 时，会调用 `Interlocked.Decrement` 从 `m_waiters` 字段减 1。 如果 `m_waiters` 现在是 `0`，表明没有其他线程在调用 `Enter` 时发生阻塞，调用 `Leave` 的线程可以直接返回。同样地，想象以下这有多快：离开一个锁意味着线程从一个 `Int32` 中减 1，执行快速的 `if` 测试，然后返回！另一方面，如果调用 `Leave` 的线程发现 `m_waiters` 不为 0，线程就知道现在存在一个竞争，另外至少有一个线程在内核中阻塞。这个线程必须唤醒一个(而且只能是一个)阻塞的线程。唤醒线程是通过在 `AutoResetEvent` 上调用 `Set` 来实现的。这会造成性能上的损失，因为线程必须转换成内核模式代码，再转换回来。但这个转换只有在发生竞争时才会发生。当然，`AutoResetEvent` 确保只有一个阻塞的线程被唤醒；在 `AutoResetEvent` 上阻塞的其他所有线程会继续阻塞，直到新的、解除了阻塞的线程最终调用 `Leave`。

> 注意 在实际应用中，任何线程可以在任何时间调用 `Leave`， 因为 `Enter` 方法没有记录哪一个线程成功获得了锁。很容易添加字段和代码来维护这种信息，但会增大锁对象自身需要的内存，并损害 `Enter` 和 `Leave` 方法的性能，因为它们现在必须操作这个字段。我情愿有一个性能高超的锁，并确保我的代码以正确方式使用它。你会注意到，事件和信号量都没有维护这种信息，只有互斥体才有维护。

## <a name="30_2">30.2 自旋、线程所有权和递归</a>

由于转换为内核模式会造成巨大的性能损失，而且线程占有锁的时间通常都很短，所以为了提升应用程序的总体性能，可以让一个线程在用户模式中“自旋”一小段时间，再让线程转换为内核模式。如果线程正在等待的锁在线程“自旋”期间变得可用，就能避免向内核模式的转换了。

此外，有的锁限制只能由获得锁的线程释放锁。有的锁允许当前拥有它的线程递归地拥有锁(多次拥有)，`Mutex` 锁就是这样一个例子。<sup>①</sup>可通过一些别致的逻辑构建支持自旋、线程所有权和递归的一个混合锁，如下所示：

> ① 线程在 `Mutex` 对象上等待时不会“自旋”，因为 `Mutex` 的代码在内核中。这意味着线程必须转换成内核模式才能检查 `Mutex` 的状态。

```C#
internal sealed class AnotherHybridLock : IDisposable {
    // Int32 由基元用户模式构造 (Interlocked 的方法)使用
    private Int32 m_waiters = 0;

    // AutoResetEvent 是基元内核模式构造
    private AutoResetEvent m_waiterLock = new AutoResetEvent(false);

    // 这个字段控制自旋，希望能提升性能
    private Int32 m_spincount = 4000; // 随便选择的一个计数

    // 这些字段指出哪个线程拥有锁，以及拥有了它多少次
    private Int32 m_owningThreadId = 0, m_recursion = 0;

    public void Enter() {
        // 如果调用线程已经拥有锁，递增递归计数并返回
        Int32 threadId = Thread.CurrentThread.ManagedThreadId;
        if (threadId == m_owningThreadId) { m_recursion++; return; }

        // 调用线程不拥有锁，尝试获取它
        SpinWait spinwait = new SpinWait();
        for (Int32 spinCount = 0; spinCount < m_spincount; spinCount++) {
            // 如果锁可以自由使用了，这个线程就获得它；设置一些状态并返回
            if (Interlocked.CompareExchange(ref m_waiters, 1, 0) == 0) goto GotLock;

            // 黑科技：给其他线程运行的机会，希望锁会被释放
            spinwait.SpinOnce();
        }

        // 自旋结束，锁仍未获得，再试一次
        if (Interlocked.Increment(ref m_waiters) > 1) {
            // 仍然是竞态条件，这个线程必须阻塞
            m_waiterLock.WaitOne(); // 等待锁：性能有损失
            // 等这个线程醒来时，它拥有锁；设置一些状态并返回
        }

    GotLock:
        // 一个线程获得锁时，我们记录它的 ID，并
        // 指出线程拥有锁一次
        m_owningThreadId = threadId; m_recursion = 1;
    }

    public void Leave() {
        // 如果调用线程不拥有锁，表明存在 bug
        Int32 threadId = Thread.CurrentThread.ManagedThreadId;
        if (threadId != m_owningThreadId)
            throw new SynchronizationLockException("Lock not owned by calling thread");
        
        // 递减递归技术。如果这个线程仍然拥有锁，那么直接返回
        if (--m_recursion > 0) return;

        m_owningThreadId = 0; // 现在没有线程拥有锁
                              
        // 如果没有其他线程在等待，直接返回
        if (Interlocked.Decrement(ref m_waiters) == 0)
            return;

        // 有其他线程正在等待，唤醒其中 1 个
        m_waiterLock.Set(); // 这里有较大的性能损失
    }
    
    public void Dispose() { m_waiterLock.Dispose(); }
}
```

可以看出，为锁添加了额外的行为之后，会增大它拥有的字段数量，进而增大内存消耗。代码还变得更复杂了，而且这些代码必须执行，造成锁的性能的下降。29.4.1 节“Event构造”比较了各种情况下对一个 `Int32` 进行递增的性能，这些情况分别是：无任何锁，使用基元用户模式构造，以及使用内核模式构造。这里重复了哪些性能测试的结果，并添加了使用 `SimpleHybridlock` 和 `AnotherHybridLock` 的结果。结果从快到慢依次是：

```cmd
Incrementing x: 8                           最快
Incrementing x in M: 69                     慢约 9 倍
Incrementing x in SpinLock: 164             慢约 21 倍
Incrementing x in SimpleHybridlock: 164     慢约 21 倍(类似于 SpinLock)
Incrementing x in AnotherHybridLock: 230    慢约 29 倍(因为所有权/递归)
Incrementing x in SimpleWaitLock: 8854      慢约 1107 倍
```

注意，`AnotherHybridLock` 的性能不如 `SimpleHybridlock`。这是因为需要额外的逻辑和错误检查来管理线程所有权和递归行为。如你所见，在锁中添加的每一个行为都会影响它的性能。

## <a name="30_3">30.3 FCL 中的混合结构</a>

FCL 自带了许多混合构造，它们通过一些别致的逻辑将你的线程保持在用户模式，从而增应用程序的性能。有的混合构造直到首次有线程在一个构造上发生竞争时，才会创建内核模式的构造。如果线程一直不在构造说上发生竞争，应用程序就可避免因为创建对象而产生的性能损失，同时避免为对象分配内存。许多构造还支持使用一个 `Cancellation Token`(参见第 27 章“计算限制的异步操作”)，使一个线程强迫解除可能正在构造上等待的其他线程的阻塞。本节将向你介绍这些混合构造。

### 30.3.1 `ManualResetEventSlim`类和 `SemaphoreSlim`类

先来看看 `System.Threading.ManualResetEventSlim` 和 `System.Threading.SemaphoreSlim` 这两个类。<sup>①</sup>这两个构造的工作方式和对应的内核模式构造完全一致，只是它们都在用户模式中“自旋”，而且都推迟到发生第一次竞争时，才创建内核模式的构造。它们的 `Wait` 方法允许传递一个超时值和一个 `CancellationToken`。下面展示了这些类(未列出部分方法的重载版本)：

> ①  虽然没有一个 `AutoResetEventSlim` 类，但许多时候都可以构造一个 `SemaphoreSlim` 对象，并将 `maxCount` 设为 1.

```C#
public class ManualResetEventSlim : IDisposable {
    public ManualResetEventSlim(Boolean initialState, Int32 spinCount);
    public void Dispose();
    public void Reset();
    public void Set();
    public Boolean Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken);

    public Boolean IsSet { get; }
    public Int32 SpinCount { get; }
    public WaitHandle WaitHandle { get; }
}

public class SemaphoreSlim : IDisposable {
    public SemaphoreSlim(Int32 initialCount, Int32 maxCount);
    public void Dispose();
    public Int32 Release(Int32 releaseCount);
    public Boolean Wait(Int32 millisecondsTimeout, CancellationToken cancellationToken);

    // 该特殊的方法用于 async 和 await(参见第 28 章)
    public Task<Boolean> WaitAsync(Int32 millisecondsTimeout, CancellationToken cancellationToken);
    public Int32 CurrentCount { get; }
    public WaitHandle AvailableWaitHandle { get; }
}
```

### 30.3.2 `Monitor`类和同步块

或许最常用的混合型线程同步构造就是 `Monitor` 类，它提供了支持自旋、线程所有权和递归和互斥锁。之所以最常用，是因为它资格最老，C# 有内建的关键字支持它，JIT 编译器对它知之甚详，而且 CLR 自己也在代表你的应用程序使用它。但正如稍后就要讲到的那样，这个构造存在许多问题，用它很容易造成代码中出现 bug。我先解释这个构造，然后指出问题以及解决问题的方法。

堆中的每个对象都可关联一个名为 **同步块** 的数据结构。同步块包含字段，这些字段和本章前面展示的 `AnotherHybridLock` 类的字段相似。具体地说，它对内核对象、拥有线程(owning thread)的 ID、递归计数(recursion count)以及等待线程(waiting thread)计数提供了相应的字段。`Monitor` 是静态类，它的方法接收对任何堆对象的引用。这些方法对指定对象的同步块中的字段进行操作。以下是 `Monitor` 类最常用的方法：

```C#
public static class Monitor {
    public static void Enter(Object obj);
    public static void Exit(Object obj);

    // 还可指定尝试进入锁时的超时值(不常用):
    public static Boolean TryEnter(Object obj, Int32 millisecondsTimeout);

    // 稍后会讨论 lockTaken 实参
    public static void Enter(Object obj, ref Boolean lockTaken);
    public static void TryEnter(Object obj, Int32 millisecondsTimeout, ref Boolean lockTaken);
}
```

显然，为堆中每个对象都关联一个同步块数据结构显得很浪费，尤其是考虑到大多数对象的同步块都从不使用。为节省内存，CLR 团队采用一种更经济的方式提供刚才描述的功能。它的工作原理是：CLR 初始化时在堆中分配一个同步块数组。本书第 4 章说过，每当一个对象在堆中创建的时候，都有两个额外的开销字段与它关联。第一个“类型对象指针”，包含类型的“类型对象”的内存地址。第二个是“同步块索引”，包含同步块数组中的一个整数索引。

一个对象在构造时，它的同步块索引初始化为 -1，表明不引用任何同步块。然后，调用 `Monitor.Enter` 时，CLR 在数组中找到一个空白同步块，并设置对象的同步块索引，让它引用该同步块。换言之，同步块和对象是动态关联的。调用 `Exit` 时，会检查是否有其他任何线程正在等待使用对象的同步块。如果没有线程在等待它，同步块就自由了，`Exit` 将对象的同步块索引设回 `-1`，自由的同步块将来可以和另一个对象关联。

图 30-1 展示了堆中的对象、它们的同步块索引以及 CLR 的同步块数组元素之间的关系。Object-A，Object-B 和 Object-C 都将它们的类型对象指针成员设为引用 Type-T(一个类型对象)。这意味着三个对象全都具有相同的类型。如第 4 章所述，类型对象本身也是堆中的一个对象。和其他所有对象一样，类型对象有两个开销成员：同步块索引和类型对象指针。这意味着同步块可以和类型对象关联，而且可以将一个类型对象引用传给 `Monitor` 的方法。顺便说一句，如有必要，同步块数组能创建更多的同步块。所以，同时同步大量对象时，不必担心系统会用光同步块。

![30_1](../resources/images/30_1.png)  

图 30-1 堆中的对象(包括类型对象)可使其中同步块索引引用 CLR 同步块数组中的记录项

以下代码演示了 `Monitor` 类原本的使用方式：

```C#
internal sealed class Transaction {
    private DateTime m_timeOfLastTrans;

    public void PerformTransaction() {
        Monitor.Enter(this);
        // 以下代码拥有对数据的独占访问权...
        m_timeOfLastTrans = DateTime.Now;
        Monitor.Exit(this);
    }

    public DateTime LastTransaction {
        get {
            Monitor.Enter(this);
            // 以下代码拥有对数据的独占访问权...
            DateTime temp = m_timeOfLastTrans;
            Monitor.Exit(this);
            return temp;
        }
    }
}
```

表面看很简单，但实际存在问题。现在的问题是，每个对象的同步块索引都隐式为公共的。以下代码演示了这可能造成的影响。

```C#
public static void SomeMethod() {
    var t = new Transaction();
    Monitor.Enter(t); // 这个线程获取对象的公共锁

    // 让一个线程池线程显示 LastTransaction 时间
    // 注意：线程线程会阻塞，直到 SomeMethod 调用了 Monitor.Exit！
    ThreadPool.QueueUserWorkItem(o => Console.WriteLine(t.LastTransaction));

    // 这里执行其他一些代码...
    Monitor.Exit(t);
}
```

在上述代码中，执行 `SomeMethod` 的线程调用 `Monitor.Enter`，获取由 `Transaction` 对象公开的锁。线程池线程查询 `LastTransaction` 属性时，这个属性也调用 `Monitor.Enter` 来获取同一个锁，造成线程池线程阻塞，直到执行 `SomeMethod` 的线程调用`Monitor.Exit`。有调试器可发现线程池线程在 `LastTransaction` 属性内部阻塞。但很难判断是另外哪个线程拥有锁。即使真的弄清楚了是哪个线程拥有锁，还必须弄清楚是什么代码造成它取得锁，这就更难了。更糟的是，即使历经千辛万苦，终于搞清楚了是什么带按摩造成线程取得锁，最后却发现那些代码不在你的控制范围之内，或者无法修改它们来修正问题。因此，我的建议是始终坚持使用私有锁。下面展示了如何修正 `Transaction` 类：

```C#
internal sealed class Transaction {
    private readonly Object m_lock = new Object(); // 现在每个 Transaction 对象都有私有锁
    private DateTime m_timeOfLastTrans;

    public void PerformTransaction() {
        Monitor.Enter(m_lock);  // 进入私有锁
        // 以下代码拥有对数据的独占访问权...
        m_timeOfLastTrans = DateTime.Now;
        Monitor.Exit(m_lock);  // 退出私有锁
    }

    public DateTime LastTransaction {
        get {
            Monitor.Enter(m_lock);  // 进入私有锁
            // 以下代码拥有对数据的独占访问权...
            DateTime temp = m_timeOfLastTrans;
            Monitor.Exit(m_lock);   // 退出私有锁
            return temp;
        }
    }
}
```

如果 `Transaction` 的成员是静态的，只需将 `m_lock` 字段也变成静态字段，即可确保静态成员的线程安全性。

通过以上讨论，````````````

## <a name="30_4">30.4 著名的双检锁技术</a>

## <a name="30_5">30.5 条件变量模式</a>

## <a name="30_6">30.6 异步的同步构造</a>

## <a name="30_7">30.7 并发集合类</a>