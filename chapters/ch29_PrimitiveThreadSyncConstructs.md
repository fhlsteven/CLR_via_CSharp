# 第 29 章 基元线程同步构造

本章内容

* <a href="#29_1">类库和线程安全</a>
* <a href="#29_2">基元用户模式和内核模式构造</a>
* <a href="#29_3">用户模式构造</a>
* <a href="#29_4">内核模式构造</a>

一个线程池线程阻塞时，线程池会创建额外的线程，而创建、销毁和调度线程所需的时间和内存资源是相当昂贵的。另外，许多开发人员看见自己程序的线程没有做任何有用的事情时，他们的习惯是创建更多的线程，寄希望于新线程能做有用的事情。为了构建可伸缩的、响应灵敏的应用程序，关键在于不要阻塞你拥有的线程，使它们能用于(和重用于)执行其他任务。第 27 章“计算限制的异步操作”讲述了如何利用线程执行计算限制的操作，第 28 章 “I/O 限制的异步操作” 则讲述了如何利用线程执行 I/O 限制的操作。

本章重点在于线程同步。多个线程**同时**访问共享数据时，线程同步能防止数据损坏。之所以要强调**同时**，是因为线程同步问题其实就是计时问题。如果一些数据由两个线程访问，但那些线程不可能同时接触到数据，就完全用不着线程同步。第 28 章展示了如何通过不同的线程来执行异步函数的不同部分。可能有两个不同的线程访问相同的变量和数据，但根据异步函数的实现方式，不可能有两个线程**同时**访问相同的数据。所以，在代码访问异步函数中包含的数据时不需要线程同步。

不需要线程同步是最理想的情况，因为线程同步存在许多问题。第一个问题是它比较繁琐，而且很容易写错。在你的代码中，必须标识出所有可能由多个线程同时访问的数据。然后，必须用额外的代码将这些代码包围起来，并获取和释放一个线程同步锁。锁的作用是确保一次只有一个线程访问资源。只要有一个代码块忘记用锁包围，数据就会损坏。另外，没有办法证明你已正确添加了所有锁定代码。只能运行应用程序，对它进行大量压力测试，并寄希望于没有什么地方出错。事实上，应该在 CPU (或 CPU 内核)数量尽可能多的机器上测试应用程序。因为 CPU 越多，两个或多个线程同时访问资源的机率越大，越容易检测到问题。

锁的第二个问题在于，它们会损害性能。获取和释放锁是需要时间的，因为要调用一些额外的方法，而且不同的 CPU 必须进行协调，以决定哪个线程先取得锁。让机器中的 CPU 以这种方式相互通信，会对性能造成影响。例如，假定使用以下代码将一个节点添加到链表头：

```C#
// 这个类由 LinkedList 类使用
public class Node {
    internal Node m_next;
    // 其他成员未列出
}

public sealed class LinkedList {
    private Node m_head;

    public void Add(Node newNode) {
        // 以下两行执行速度非常快的引用赋值
        newNode.m_next = m_head;
        m_head = newNode;
    }
}
```

这个 `Add` 方法执行两个速度很快的引用赋值。现在假定要使 `Add` 方法线程安全，使多个线程能同时调用它而不至于损坏链表。这需要让 `Add` 方法获取和释放一个锁：

```C#
public sealed class LinkedList {
    private SomeKindOfLock m_lock = new SomeKindOfLock();
    private Node m_head;

    public void Add(Node newNode) {
        m_lock.Acquire();
        // 以下两行执行速度非常快的引用赋值
        newNode.m_next = m_head;
        m_head = newNode;
        m_lock.Release();
    }
}
```

`Add` 虽然线程安全了，但速度也显著慢下来了。具体慢多少要取决于所选的锁的种类；本章和下一章会对比各种锁的性能。但即便是最快的锁，也会造成 `Add` 方法数倍地慢于没有任何锁的版本。当然，如果代码在一个循环中调用 `Add` 向链表插入几个节点，性能还会变得更差。

线程同步锁的第三个问题在于，它们一次只允许一个线程访问资源。这是锁的全部意义之所在，但也是问题之所在，因为阻塞一个线程会造成更多的线程被创建。例如，假定一个线程池线程试图获取一个它暂时无法获取的锁，线程池就可能创建一个新线程，使 CPU 保持“饱和”。如同第 26 章“线程基础” 讨论的那样，创建线程时一个昂贵的操作，会耗费大量内存和时间。更不妙的是，当阻塞的线程再次运行时，它会和这个新的线程池线程共同运行。也就是说，Windows 现在要调度比 CPU 数量更多的线程，这会增大上下文切换的机率，进一步损害到性能。

综上所述，线程同步是一件不好的事情，所以在设计自己的应用程序时，应该尽可能地避免进行线程同步。具体就是避免使用像静态字段这样的共享数据。线程用 `new` 操作符构造对象时，`new` 操作符会返回对新对象的引用。在这个时刻，只要构造对象的线程才有对它的引用；其他任何线程都不能访问那个对象。如果能避免将这个引用传给可能同时使用对象的另一个线程，就不必同步对该对象的访问。

可试着使用值类型，因为它们总是被复制，每个线程操作的都是它自己的副本。最后，多个线程同时对共享数据进行只读访问是没有任何问题的。例如，许多应用程序都会在它们初始化期间创建一些数据结构。初始化完成后，应用程序就可以创建它希望的任何数量的线程；如果所有线程都只是查询数据，那么所有线程都能同时查询，无需获取或释放一个锁。`String` 类型便是这样一个例子：一旦创建好 `String` 对象，它就是“不可变”(immutable)的。所以，许多线程能同时访问一个 `String` 对象，`String` 对象没有被破坏之虞。

## <a name="29_1">29.1 类库和线程安全</a>

现在，我想简单地谈一谈类库和线程同步。Microsoft 的 Framework Class Library(FCL)保证所有静态方法都是线程安全的。这意味着假如两个线程同时调用一个静态方法，不会发生数据被破坏的情况。FCL 必须在内部做到这一点，因为开发不同程序集的多个公司不可能事先协商好使用一个锁来仲裁对资源的访问。`Console` 类包含了一个静态字段，类的许多方法都要获取和释放这个字段上的锁，确保一次只有一个线程访问控制台。

要郑重声明的是，使一个方法线程安全，并不是说它一定要在内部获取一个线程同步锁。线程安全的方法意味着在两个线程试图同时访问数据时，数据不会被破坏。`System.Math` 类有一个静态 `Max` 方法，它像下面这样实现：

```C#
public static Int32 Max(Int32 val1, Int32 val2) {
    return (val1 < val2) ? val2 : val1;
}
```

这个方法是线程安全的，即使它没有获取任何锁。由于 `Int32` 是值类型，所以传给 `Max` 的两个 `Int32` 值会复制到方法内部。多个线程可以同时调用 `Max` 方法，每个线程处理的都是它自己的数据，线程之间互不干扰。

另一方面，FCL 不保证实例方法是线程安全的，因为假如全部添加锁定，会造成性能的巨大损失。另外，假如每个实例方法都需要获取和释放一个锁，事实上会造成最终在任何给定的时刻，你的应用程序只有一个线程在运行，这对性能的影响是显而易见的。如前所述，调用实例方法时无需线程同步。然而，如果线程随后公开了这个对象引用————把它放到一个静态字段中，把它作为状态实参传给一个 `ThreadPool.QueueUserWorkItem` 或 `Task` ———— 那么在多个线程可能同时进行非读只读访问的前提下，就需要线程同步。

建议你自己的类库也遵循 FCL 的这个模式；也就是说，使自己的所有静态方法都线程安全，使所有实例方法都非线程安全。这个模式有一点要注意：如果实例方法的目的是协调线程，则实例方法应该是线程安全的。例如，一个线程可能调用 `CancellationTokenSource` 的 `Cancel` 方法取消一个操作，另一个线程通过查询对应的 `CancellationToken` 的 `IsCancellationRequested` 属性，检测到它应该停止正在做的事情。这两个实例成员内部通过一些特殊的线程同步代码来协调两个线程。<sup>①</sup>

> ① 具体地说，两个成员访问的字段被标记为 `volatile`，这是本章稍后要讨论的一个概念。

## <a name="29_2">29.2 基元用户模式和内核模式构造</a>

本章将讨论基元线程同步构造。**基元**(primitive)是指可以在代码中使用的最简单的构造。有两种基元构造；用户模式(user-mode)和内核模式(kernel-mode)。应尽量使用基元用户模式构造，它们的速度要显著快于内核模式的构造。这是因为它们使用了特殊 CPU 指令来协调线程。这意味着协调是在硬件中发生的(所以才这么快)。但这也意味着 Microsoft Windows 操作系统永远检测不到一个线程在基元用户模式的构造上阻塞了。由于在用户模式的基元构造上阻塞的线程池线程永远不认为已阻塞，所以线程池不会创建新线程来替换这种临时阻塞的线程。此外，这些 CPU 指令只阻塞线程相当短的时间。

所有这一切听起来真不错，是吧？确实如此，这是我建议尽量使用这些构造的原因。但它们也有一个缺点:只有 Windows 操作系统内核才能停止一个线程的运行(防止它浪费 CPU 时间)。在用户模式中运行的线程可能被系统抢占(preempted)，但线程会以最快的速度再次调度。所以，想要取得资源但暂时取不到的线程会一直在用户模式中“自旋”，这可能浪费大量 CPU 时间，而这些 CPU 时间本可用于执行其他更有用的工作。即便没有其他更有用的工作，更好的做法也是让 CPU 空闲，这至少能省一点电。

这使我们将眼光投向了基元内核模式构造。内核模式的构造是由 Windows 操作系统自身提供的。所以，它们要求在应用程序的线程中调用由操作系统内核实现的函数。将线程从用户模式切换为内核模式(或相反)会招致巨大的性能损失，这正是为什么要避免使用内核模式构造的原因。<sup>①</sup>但它们有一个重要的优点：线程通过内核模式的构造获取其他线程拥有的资源时，Windows 会阻塞线程以避免它浪费 CPU 时间。当资源变得可用时，Windows 会恢复线程，允许它访问资源。

> ① 29.4.1 节 “Event 构造” 最后会通过一个程序来具体测试性能。

对于在一个构造上等待的线程，如果拥有这个构造的线程一直不释放它，前者就可能一直阻塞。如果是用户模式的构造，线程将一直在一个 CPU 上运行，我们称为“活锁”(deadlock)。两种情况都不好。但在两者之间，死锁总是优于活锁，因为活锁既浪费 CPU 时间，又浪费内存(线程栈等)，而死锁只浪费内存。<sup>②</sup>

> ② 之所以说分配给线程的内存被浪费了，是因为在线程没有取得任何进展的前提下，这些内存不会差生任何收益。

我理想中的构造应兼具两者的长处。也就是说，在没有竞争的情况下，这个构造应该快而且不会阻塞(就像用户模式的构造)。但如果存在对构造的竞争，我希望它被操作系统内核阻塞。像这样的构造确实存在；我把它们称为**混合构造**(hybrid construct)，将在第 30 章详细讨论。应用程序使用混合构造是一种很常见的现象，因为在大多数应用程序中，很少会有两个或多个线程同时访问相同的数据。混合构造使你的应用程序在大多数时间都快速运行，偶尔运行得比较慢是为了阻塞线程。但这时慢一些不要紧，因为线程反正都要阻塞。

CLR 的许多线程同步构造实际只是 "Win32 线程同步构造" 的一些面向对象的类包装器。毕竟，CLR 线程就是 Windows 线程，这意味着要由 Windows 调度线程和控制线程同步。Windows 线程同步构造自 1992 年便存在了，人们已就这个主题撰写了大量内容。<sup>①</sup>所以，本章只是稍微提及了一下它。

> ① 事实上，在 Christophe Nasarre 和我合写的 《Windows 核心编程(第 5 版)》中，有几章就是专门讲这个主题的。

## <a name="29_3">29.3 用户模式构造</a>

CLR 保证对以下数据类型的变量的读写是原子性的：`Boolean`，`Char`，`(S)Byte`，`(U)Int16`，`(U)Int32`，`(U)IntPtr`，`Single` 以及引用类型。这意味着变量中的所有字节都一次性读取或写入。假如，假定有以下类：

```C#
internal static class SomeType {
    public static Int32 x = 0;
}
```

然后，如果一个线程执行这一行代码：

`SomeType.x = 0x01234567;`

`x` 变量会一次性(原子性)地从 `0x00000000` 变成 `0x01234567`。另一个线程不可能看到出于中间状态的值。例如，不可能有别的线程查询 `SomeType.x` 并得到值 `0x01230000`。假定上述 `SomeType` 类中的 `x` 字段是一个 `Int64`，那么当一个线程执行以下代码时：

`SomeType.x = 0x0123456789abcdef;`

另一个线程可能查询 `x`，并得到值 `0x0123456700000000` 或 `0x0000000089abcdef` 值，因为读取和写入操作不是原子性的。这称为一次 torn read<sup>②<sup>。

> ② 一次读取被撕成两半。或者说在机器级别上，要分两个 MOV 指令才能读完。 ———— 译注

虽然对变量的原子访问可保证读取或写入操作一次性完成，但由于编译器和 CPU 的优化，不保证操作 **什么时候** 发生。本节讨论的基元用户模式构造用于规划好这些原子性读取/写入 操作的时间。此外，这些构造还可强制对 `(U)Int64` 和 `Double` 类型的变量进行原子性的、规划好了时间的访问。

有两种基元用户模式线程同步构造。

* **易变<sup>③</sup>构造(volatile construct)**  
  在特定的时间，它在包含一个简单数据类型的变量上执行原子性的读或写操作。

* **互锁构造(interlocked construct)**  
  在特定的时间，它在包含一个简单数据类型的变量上执行原子性的读和写操作。

> ③ 文档将 volatile 翻译为 “可变”。其实它是 “短暂存在”、“易变”的意思，因为可能多个线程都想对这种字段进行修改，本书采用“易变”。 ————译注

所有易变和互锁构造都要求传递对包含简单数据类型的一个变量的引用(内存地址)。

### 29.3.1 易变构造

早期软件是用汇编语言写的。汇编语言非常繁琐，程序员要事必躬亲，清楚地指明：将这个 CPU 寄存器用于这个，分支到那里，通过这个来间接调用等。为了简化编程，人们发明个了更高级的语言。这些高级语言引入了一系列常规构造，比如 `if/else`、`switch/case`、各种循环、局部变量、实参、虚方法调用、操作符重载等。最终，这些语言的编译器必须将高级构造转换成低级构造，使计算机能真正做你想做的事情。

换言之，C# 编译器将你的 C# 构造转换成中间语言(IL)。然后，JIT 将 IL 转换成本机 CPU 指令，然后由 CPU 亲自处理这些指令。此外，C# 编译器、JIT编译器、甚至 CPU 本身都可能优化你的代码。例如，下面这个荒谬的方法在编译之后会消失得无影无踪：

```C#
private static void OptimizedAway() {
    // 常量表达式在编译时计算，结果是 0
    Int32 value = (1 * 100) - (50 * 2);

    // 如果 value 是0，循环永远不执行
    for (Int32 x = 0; x < value; x++) {
        // 不需要编译循环中的代码，因为永远都执行不到
        Console.WriteLine("Jeff");
    }
}
```

在上述代码中，编译器发现 `value` 始终是 `0`；所以循环永远不会执行，没有必要编译循环中的代码。换言之，这个方法在编译后会被“优化掉”。事实上，如果一个方法调用了 `OptimizedAway`， 在对那个方法进行 JIT 编译时，JIT 编译器会尝试内联(嵌入)`OptimizedAway` 方法的代码。但由于没有代码，所以 JIT 编译器会删除调用 `OptimizedAway` 的代码。我们喜爱编译器的这个功能。作为开发人员，我们应该以最合理的方式写代码。代码应该容易编写、阅读和维护。然后，编译器将我们的意图转换成机器能理解的代码。在这个过程中，我们希望编译器能有最好的表现。

C# 编译器、JIT 编译器和 CPU 对代码进行优化时，它们保证我们的意图会得到保留。也就是说，从单线程的角度看，方法会做我们希望它做的事情，虽然做的方式可能有别于我们在源代码中描述的方式。但从多线程的角度看，我们的意图并不一定能得到保留。下例演示了在优化之后，程序的工作方式和我们预想的有出入：

```C#
internal static class StrangeBehavior {
    // 以后会讲到，将这个字段标记成 volatile 可修正问题
    private static Boolean s_stopWorker = false;

    public static void Main() {
        Console.WriteLine("Main: letting worker run for 5 seconds");
        Thread t = new Thread(Worker);
        t.Start();
        Thread.Sleep(5000);
        s_stopWorker = true;
        Console.WriteLine("Main: waiting for worker to stop");
        t.Join();
    }

    private static void Worker(Object o) {
        Int32 x = 0;
        while (!s_stopWorker) x++;
        Console.WriteLine("Worker: stopped when x={0}", x);
    }
}
```

在上述代码中，`Main` 方法创建一个新线程来执行 `Worker` 方法。`Worker` 方法会一直数数，直到被告知停止。`Main` 方法允许 `Worker` 线程运行 5 秒，然后将静态 `Boolean` 字段设为 `true` 来告诉它停止。在这个时候，`Worker` 线程应显示它数到多少了，然后线程终止。`Main` 线程通过调用 `Join` 来等待 `Worker` 线程终止，然后 `Main` 线程返回，造成整个进程终止。

看起来很简单，但要注意，由于会对程序执行各种优化，所以它存在一个潜在的问题。当 `Worker` 方法编译时，编译器发现 `s_stopWorker` 要么为 `true`，要么为 `false`。它还发现这个值在 `Worker` 方法本身中永远都不变化。因此，编译器会生成代码先检查 `s_stopWorker`。 如果`s_stopWorker` 为 `false`，编译器就生成代码来进入一个无限循环，并在循环中一直递增 `x`。所以，如你所见，优化导致循环很快就完成，因为对`s_stopWorker`的检查只有循环前发生一次；不会在循环的每一次迭代时都检查。

要想实际体验这一切，请将上述代码放到一个 .cs 文件中，再用 C# 编译器(csc.exe)的 `/platform:x86` 和 `/optimize+` 开关来编译。运行生成的 EXE 程序，会看到程序一直运行。注意，必须针对 x86 平台来编译，确保在运行时使用的是 x86 平台来编译，确保在运行时使用的是 x86 JIT 编译器。x86 JIT 编译器比 x64 编译器更成熟，所以它在执行优化的时候更大胆。其他 JIT 编译器不执行这个特定的优化，所以程序会像预期的那样正常运行到结束。这使我们注意另一个有趣的地方；程序是否如预想的那样工作要取决于大量因素，比如使用的是编译器的什么版本和什么开关，使用的是哪个 JIT 编译器，以及代码在什么 CPU 上运行等。除此之外，要看到上面这个程序进入死循环，一定不能在调试器中运行它，因为调试器会造成 JIT 编译器生成未优化的代码(目的是方便你进行单步调试)。

再来看另一个例子。在这个例子中，有两个字段要由两个线程同时访问：

```C#
internal sealed class ThreadsSharingData {
    private Int32 m_flag = 0;
    private Int32 m_value = 0;

    // 这个方法由一个线程执行
    public void Thread1() {
        // 注意：以下两行代码可以按相反的顺序执行
        m_value = 5;
        m_falg = 1;
    }

    // 这个方法由另一个线程执行
    public void Thread2() {
        // 注意： m_value 可能先于 m_flag 读取
        if (m_flag == 1)
            Console.WriteLine(m_value);
    }
}
```

上述代码的问题在于，编译器和 CPU 在解释代码的时候，可能反转 `Thread1` 方法中的两行代码。毕竟，反转两行代码不会改变方法的意图。方法需要在 `m_value` 中存储 `5`，在 `m_flag` 中存储 `1`。从单线程应用程序的角度说，这两行代码的执行顺序无关紧要。如果这两行代码真的按相反顺序执行，执行 `Thread2` 方法的另一个线程可能看到 `m_flag` 是 `1`，并显示 `0`。

下面从另一个角度研究上述代码。假定 `Thread1` 方法中的代码按照**程序顺序**(就是编码顺序)执行。编译 `Thread2` 方法中的代码时，编译器必须生成代码将 `m_flag` 和 `m_value` 从 RAM 读入 CPU 寄存器。RAM 可能先传递 `m_value` 值，它包含 `0` 值。然后，`Thread1` 方法可能执行，将 `m_value` 更改为 `5`，将 `m_flag` 更改为 `1`。但 `Thread2` 的 CPU 寄存器没有看到 `m_value` 已被另一个线程更改为 `5`。然后，`m_flag` 的值从 RAM 读入 CPU 寄存器。由于 `m_flag` 已变成 `1`，造成 `Thread2` 同样显示 `0`。

这些细微之处很容易被人忽视。由于调试版本不会进行优化，所以等到程序生成发行版本的时候，这些问题才会显现出来，造成很难提前检测到问题并进行纠正。下面讨论如何解决这个问题。

静态 `System.Threading.Volatile` 类提供了两个静态方法，如下所示：<sup>①</sup>

> ① `Read` 和 `Write` 还有一些重载版本可用于操作以下类型：`Boolean`，`(S)Byte`，`(U)Int16`，`UInt32`，`(U)Int64`，`(U)IntPtr`，`Single`，`Double` 和 `T`。其中 `T` 是约束为 `class`(引用类型)的泛型类型。

```C#
public static class Volatile {
    public static void Write(ref Int32 location, Int32 value);
    public static Int32 Read(ref Int32 location);
}
```

这些方法比较特殊。它们事实上会禁止 C# 编译器、JIT 编译器和 CPU 平常执行的一些优化。下面描述了这些方法是如何工作的。

* `Volatile.Write` 方法强迫 `location` 中的值在调用时写入。此外，按照编码顺序，之前的加载和存储操作必须在调用 `Volatile.Write` *之前*发生。

* `Volatile.Write` 方法强迫 `location` 中的值在调用时读取。此外，按照编码顺序，之后的加载和存储操作必须在调用 `Volatile.Read` *之后*发生。

> 重要提示 我知道目前这些概念很容易令人迷惑，所以让我归纳一条简单的规则：当线程通过共享内存相互通信时，调用 `Volatile.Write` 来写入最后一个值，调用 `Volatile.Read` 来读取第一个值。

现在就可以使用上述方法修正 `ThreadsSharingData` 类：

```C#
internal sealed class ThreadsSharingData {
    private Int32 m_falg = 0;
    private Int32 m_value = 0;

    // 这个方法由一个线程执行
    public void Thread1() {
        // 注意：在将 1 写入 m_flag 之前，必须先将 5 写入 m_value
        m_value = 5;
        Volatile.Write(ref m_flag, 1);
    }

    // 这个方法由另一个线程执行
    public void Thread2() {
        // 注意：m_value 必然在读取了 m_flag 之后读取
        if (Volatile.Read(ref m_flag) == 1) 
            Console.WriteLine(m_value);  
    }
}
```

首先，注意我们遵循了规则。`Thread1` 方法将两个值写入多个线程共享的字段。最后一个值的写入(将 `m_flag` 设为 `1`)通过调用 `Volatile.Write` 来进行。`Thread2` 方法从多个线程共享的字段读取两个值，第一个值的读取(读取 `m_flag` 的值)通过调用 `Volatile.Read` 来进行。

但是，这里真正发生了什么事情？对于 `Thread1` 方法，`Volatile.Write` 调用确保在它之前的所有写入操作都在将 `1` 写入 `m_flag` 之前完成。由于在调用 `Volatile.Write` 之前的写入操作是 `m_value = 5`，所以它必须先完成。事实上，如果在调用 `Volatile.Write` 之前要对许多变量进行修改，它们全都必须在将 `1` 写入 `m_flag` 之前完成。注意，`Volatile.Write` 调用之前的写入可能被优化成以任意顺序执行；只是所有这些写入都必须在调用 `Volatile.Write` 之前完成。

对于 `Thread2` 方法，`Volatile.Read` 调用确保在它之后的所有变量读取操作都必须在 `m_flag` 中的值读取之后开始。由于 `Volatile.Read` 调用之后是对 `m_value` 的读取，所以必须在读取了 `m_flag` 之后，才能读取 `m_value`。如果在调用 `Volatile.Read` 之后有许多读取，它们都必须在读取了 `m_flag` 的值之后才能开始。注意，`Volatile.Read` 调用之后的读取可能被优化成以任何顺序执行；只是所有这些读取都必须在调用了 `Volatile.Read` 之后发生。

#### C# 对易变字段的支持

如何确保正确调用 `Volatile.Read` 和 `Volatile.Write` 方法，是程序员最为头疼的问题之一。程序员来很难记住所有这些方法和规则，并搞清楚其他线程会在后台对共享数据进行什么操作。为了简化编程，C# 编译器提供了 `volatile` 关键字，它可应用于以下任何类型的静态或实例字段：`Boolean`，`(S)Byte`，`(U)Int16`，`(U)Int32`,`(U)IntPtr`，`Single` 和 `Char`，还可将 `volatile` 关键字应用于引用类型的字段，以及基础类型为 `(S)Byte`，`(U)Int16`或`(U)Int32`的任何枚举字段。JIT 编译器确保对易变字段的所有访问都是以易变读取或写入的方式执行，不必显示调用 `Volatile` 的静态 `Read` 或 `Write` 方法。另外，`volatile` 关键字告诉 C# 和 JIT 编译器不将字段缓存到 CPU 的寄存器中，确保字段的所有读写操作都在 RAM 中进行。

下面用 `volatile` 关键字重写 `ThreadsSharingData` 类。

```C#
internal sealed class ThreadSharingData {
    private volatile Int32 m_flag = 0;
    private          Int32 m_value = 0;

    // 这个方法由一个线程执行
    public void Thread1() {
        // 注意：将 1 写入 m_flag 之前，必须先将 5 写入 m_value
        m_value = 5;
        m_flag = 1;
    }

    // 这个方法由另一个线程执行
    public void Thread2() {
        // 注意： m_value 必须在读取了 m_flag 之后读取
        if (m_flag == 1)
            Console.WriteLine(m_value);
    }
}
```

一些开发人员(包括我)不喜欢 C# 的 `volatile` 关键字，认为 C# 语言就不该提供这个关键字。<sup>①</sup>大多数算法都不需要对字段进行易变的读取和写入，大多数字段访问都可以按正常方式进行，这样能提高性能。要求对字段的所有访问都是易变的，这种情况极为少见。例如，很难解释如何将易变读取操作应用于下面这样的算法：

> ① 顺便说一句，还好 Microsoft Visual Basic 没有提供什么“易变”语义。

`m_amount = m_amount + m_amount;    // 假定 m_amount 是类中定义的一个 volatile 字段`

通常，要倍增一个整数，只需将它的所有位都左移 1 位，许多编译器都能检测到上述代码的意图，并执行这个优化。如果 `m_amount` 是`volatile` 字段，就不允许执行这个优化。编译器必须生成代码将 `m_amout` 是 `volatile` 字段，就不允许执行这个优化。编译器必须生成代码将 `m_amount` 读入一个寄存器，再把它读入另一个寄存器，将两个寄存器加到一起，再将结果写回 `m_amount` 字段。未优化的代码肯定会更大、更慢；如果它包含在一个循环中，更会成为一个大大的杯具。

另外，C# 不支持以传引用的方式将 `volatile` 字段传给方法。例如，如果将 `m_amount` 定义成一个 `volatile Int32`，那么试图调用 `Int32` 类型的 `TryParse` 方法将导致编译器生成一条如下所示的警告信息：

```C#
Boolean success = Int32.TryParse("123", out m_amount);
// 上一行代码导致 C# 编译器生成一下警告信息：
// CS0420：对 volatile 字段的引用不被视为 volatile
```

### 29.3.2 互锁结构

`Volatile` 的 `Read` 方法执行一次原子性的读取操作，`Write` 方法执行一次原子性的写入操作。也就是说，每个方法执行的是一次原子读取或者原子写入。本节将讨论静态 `System.Threading.Interlocked` 类提供的方法。`Interlocked` 类中的每个方法都执行一次原子读写以及写入操作。此外，`Interlocked` 的所有方法都建立了完整的内存栅栏(memory fence)。换言之，调用某个 `Interlocked` 方法之前的任何变量写入都在这个 `Interlocked` 方法调用之前执行；而这个调用之后的任何变量读取的都在这个调用之后读取。

```C#
public static class Interlocked {
    // return (++location)

    public static Int32 Increment(ref Int32 location);

    // return (--location)
    public static Int32 Decrement(ref Int32 location);

    // return (location += value)
    // 注意： value 可能是一个负数，从而实现减法运算
    public static Int32 Add(ref Int32 location1, Int32 value);

    // Int32 old = location1; location1 = value; return old;
    public static Int32 Exchange(ref Int32 location1, Int32 value);

    // Int32 old = location1;
    // if (location1 == comparand) location1 = value;
    // return old;
    public static Int32 CompareExchange(ref Int32 location1, Int32 value, Int32 comparand);
    ...
}
```

上述方法还有一些重载版本能对 `Int64` 值进行处理。此外，`Interlocked` 类提供了 `Exchange` 和 `CompareExchange` 方法，它们能接收`Object`，`IntPtr`，`Single` 和 `Double` 等类型的参数。这两个方法各自还有一个泛型版本，其泛型类型被约束为 `class`(任意引用类型)。

我个人很喜欢使用 `Interlocked` 的方法，它们相当快，而且能做不少事情，下面用一些代码演示如何使用 `Interlocked` 的方法异步查询几个 Web 服务器，并同时处理返回的数据。代码很短，绝不阻塞任何线程，而且使用线程池线程来实现自动伸缩(根据负荷大小使用最多与 CPU 数量等同的线程数)。此外，代码理论上支持访问最多 2 147 483 674(`Int32.MaxValue`)个 Web 服务器。换言之，在自己进行编程时，这些代码是一个很好的参考模型：

```C#
internal sealed class MultiWebRequests {
    // 这个辅助类用于协调所有异步操作
    private AsyncCoordinator m_ac = new AsyncCoordinator();

    // 这是想要查询的 Web 服务器及其响应(异常或 Int32)的集合
    // 注意：多个线程访问该字典不需要以同步方式进行，
    // 因为构造后键就是只读的
    private Dictionary<String, Object> m_servers = new Dictionary<String, Object> {
        { "http://Wintellect.com/", null },
        { "http://Microsoft.com/", null },
        { "http://1.1.1.1/", null }        
    };

    public MultiWebRequests(Int32 timeout = Timeout.Infinite) {
        // 以异步方式一次性发起所有请求
        var httpClient = new HttpClient();
        foreach (var server in m_servers.Keys) {
            m_ac.AboutToBegin(1);
            httpClient.GetByteArrayAsync(server)
                .ContinueWith(task => ComputeResult(server, task));
        }

        // 告诉 AsyncCoordinator 所有操作都已发起，并在所有操作完成、
        // 调用 Cancel 或者发生超时的时候调用 AllDone
        m_ac.AllBegun(AllDone, timeout);
    }

    private void ComputeResult(String server, Task<Byte[]> task) {
        Object result;
        if (task.Exception != null) {
            result = task.Exception.InnerException;
        } else {
            // 在线程池线程上处理 I/O 完成，
            // 在此添加自己的计算密集型算法...
            result = task.Result.Length;    // 本例只是返回长度
        }

        // 保存结果(exception/sum)，指出 1 个操作完成
        m_servers[server] = result;
        m_ac.JustEnded();
    }

    // 调用这个方法指出结果已无关紧要
    public void Cancel() { m_ac.Cancel(); }

    // 所有 Web 服务器都响应、调用了 Cancel 或者发生超时，就调用该方法
    private void AllDone(CoordinationStatus status) {
        switch (status) {
            case CoordinationStatus.Cancel:
                Console.WriteLine("Operation canceled.");
                break;

            case CoordinationStatus.Timeout:
                Console.WriteLine("Operation timed-out.");
                break;
            case CoordinationStatus.AllDone:
                Console.WriteLine("Operation completed; results below:");
                foreach (var server in m_servers) {
                    Console.Write("{0} ", server.Key);
                    Object result = server.Value;
                    if (result is Exception) {
                        Console.WriteLine("failed due to {0}.", result.GetType().Name);
                    } else {
                        Console.WriteLine("returned {0:N0} bytes.", result);
                    }
                }
                break;
        } 
    }
}
```

可以看出，上述代码并没有直接使用 Interlocked 的任何方法，因为我将所有协调代码都放到可重用的 `AsyncCoordinator` 类中。该类会在以后详细解释。我想先说明以下这个类的作用。构造一个 `MultiWebRequest` 类时，会先初始化一个 `AsyncCoordinator` 和包含了一组服务器 URI(及其将来结果)的字典。然后，它以异步方式一个接一个地发出所有 Web 请求。为此，它首先调用 ` AsyncCoordinator` 的 `AboutToBegin` 方法，向它传递要发出的请求数量。<sup>①</sup>然后，它调用 `HttpClient` 的 `GetByteArrayAsync` 来初始化请求。这会返回一个 `Task`，我随即在这个 `Task`上调用 `ContinueWith`，确保在服务器有了响应之后，我的 `ComputeResult` 方法可通过许多线程池线程并发处理结果。对 Web 服务器的所有请求都发出之后，将调用 `AsyncCoordinator` 的 `AllBegun` 方法，向它传递要在所有操作完成后执行的方法(`AllDone`)以及一个超时值。每收到每一个 Web 服务器响应，线程池线程都会调用 `MultiWebRequests` 的 `ComputeResult` 方法。该方法处理服务器返回的字节(或者发生的任何错误)，将结果存储到字典集合中。存储好每个结果之后，会调用 `AsyncCoordinator` 的 `JustEnded`方法，使 `AsyncCoordinator` 对象知道一个操作已经完成。

> ① 可改写代码，在 for 循环前调用一次 `m_ac.AboutToBegin(m_requests.Count)`，而不是每次循环迭代都调用 `AbountToBegin`。

所有操作完成后，`AsyncCoordinator` 会调用 `AllDone` 方法处理来自所有 Web 服务器的结果。执行 `AllDone` 方法的线程就是获取最后一个Web 服务器响应的那个线程池线程。但如果发生超时或取消，调用 `AllDone` 的线程就是向 `AsyncCoordinator` 通知超时的那个线程池线程，或者是调用 `Cancel` 方法的那个线程。也有可能 `AllDone` 由发出 Web 服务器请求的那个线程调用 ———— 如果最后一个请求在调用 `AllBegun` 之前完成。

注意，这里存在竞态条件，因为以下事情可能恰好同时发生：所有 Web 服务器请求完成、代用 `AllBegun`、发生超时以及调用 `Cancel`。这时，`AsyncCoordinator` 会选择 1 个赢家和 3 个输家，确保 `AllDone` 方法不被多次调用。赢家是通过传给 `AllDone` 的 `status` 实参来识别的，它可以是 `CoordinationStatus` 类型定义的几个符号之一：

`internal enum CoordinationStatus { AllDone, TimeOut, Cancel };`

对发生的事情有一个大致了解之后，接着看看它的具体工作原理。`AsyncCoordinator` 类封装了所有线程协调(合作)逻辑。它用 `Interlocked` 提供的方法来操作一切，确保代码以极快的速度运行，同时没有线程会被阻塞。下面是这个类的代码：

```C#
internal sealed class AsyncCoordinator {
    private Int32 m_opCount = 1;            // AllBegun 内部调用 JustEnded 来递减它
    private Int32 m_statusReported = 0;     // 0=false, 1=true
    private Action<CoordinationStatus> m_callback;
    private Timer m_timer;

    // 该方法必须在发起一个操作之前调用
    public void AboutToBegin(Int32 opsToAdd = 1) {
        Interlocked.Add(ref m_opCount, opsToAdd);
    }

    // 该方法必须在处理好一个操作的结果之后调用
    public void JustEnded() {
        if (Interlocked.Decrement(ref m_opCount) == 0)
            ReportStatus(CoordinationStatus.AllDone);
    }

    // 该方法必须在发起所有操作之后调用
    public void AllBegun(Action<CoordinationStatus> callback,
    Int32 timeout = Timeout.Infinite) {
        m_callback = callback;

        if (timeout != Timeout.Infinite)
            m_timer = new Timer(TimeExpired, null, timeout, Timeout.Infinite);
        JustEnded();
    }

    private void TimeExpired(Object o) { ReportStatus(CoordinationStatus.Timeout); }
    public void Cancel() { ReportStatus(CoordinationStatus.Cancel); }

    private void ReportStatus(CoordinationStatus status) {
        // 如果状态从未报告过，就报告它；否则忽略它
        if (Interlocked.Exchange(ref m_statusReported, 1) == 0)
            m_callback(status);
    }
}
```

这个类最重要的字段就是 `m_opCount` 字段，用于跟踪仍在进行的异步操作的数量。每个异步操作开始前都会调用 `AboutToBegin`。该方法调用`Interlocked.Add` ，以原子方式将传给它的数字加到 `m_opCount` 字段上。`m_opCount` 上的加法运算必须以原子方式进行，因为随着更多的操作开始，可能开始在线程池线程上处理 Web 服务器的响应。处理好 Web 服务器的响应后会调用 `JustEnded`。该方法调用 `Interlocked.Decrement`，以原子方式从 `m_opCount` 上减 `1`。无论哪一个线程恰好将 `m_opCount` 设为 `0`，都由它调用 `ReportStatus`。

> 注意 `m_opCount` 字段初始化为 1 (而非 0)，这一点很重要。执行构造器方法的线程在发出 **Web** 服务器请求期间，由于 `m_opCount` 字段为 1，所以能保证 `AllDone` 不会被调用。构造器调用 `AllBegun` 之前，`m_opCount` 永远不不可能变成 0 。构造器调用 `AllBegun` 时，`AllBegun` 内部调用 `JustEnded` 来递减 `m_opCount`，所以事实上撤销(`undo`)了把它初始化成 1 的效果。现在 `m_opCount` 能变成 0了，但只能是在发起了所有 **Web** 服务器请求之后。 

`ReportStatus` 方法对全部操作结束、发生超时和调用 `Cancel` 时可能发生的竞态条件进行仲裁。`ReportStatus` 必须确保其中只有一个条件胜出，确保 `m_callback` 方法只被调用一次。为了仲裁赢家，要调用 `Interlocked.Exchange` 方法，向它传递对 `m_statusReported` 字段的引用。这个字段实际是作为一个 `Boolean` 变量使用的；但不能真的把它写成一个 `Boolean` 变量，因为没有任何 `Interlocked` 方法能接受 `Boolean` 变量。因此，我用一个 `Int32` 变量来代替，`0` 意味着 `false`，`1` 意味着 `true`。

在 `ReportStatus` 内部，`Interlocked.Exchange` 调用会将 `m_statusReported` 更改为 `1`。但只有做这个事情的第一个线程才会看到`Interlocked.Exchange` 返回 `0`，只有这个线程才能调用回调方法。调用 `Interlocked.Exchange` 的其他任何线程都会得到返回值 `1`，相当于告诉这些线程：回调方法已被调用，你不要再调用了。

### 29.3.3 实现简单的自旋锁<sup>①</sup>

> ① 即 spin lock。spin 顾名思义确实是不停旋转的意思。在多线程处理中，它意味着让一个线程暂时“原地打转”，以免它跑去跟另一个线程竞争资源。注意其中的关键字是 spin，表明线程将一直运行，占用宝贵的 CPU 时间。————译注

`Interlocked` 的方法很好用，但主要用于操作 `Int32` 值。如果需要原子性地操作类对象中的一组字段，又该怎么办呢？在这种情况下，需要采取一个办法阻止所有线程，只允许其中一个进入对字段进行操作的代码区域。可以使用 `Interlocked` 的方法构造一个线程同步块：

```C#
internal struct SimpleSpinLock {
    private Int32 m_ResourceInUse;      // 0=false(默认), 1=true

    public void Enter() {
        while (true) {
            // 总是将资源设为 "正在使用" (1)，
            // 只有从 “未使用” 变成 “正在使用” 才会返回 ①
            if (Interlocked.Exchange(ref m_ReourceInUse, 1) == 0) return;
            // 在这里添加“黑科技” ② ...
        }
    }

    public void Leave() {
        // 将资源标记为 “未使用”
        Volatile.Write(ref m_ResourceInUse, 0);
    }
}
```

> ① 从 0 变成 1 才返回(结束自旋)，从 1 变成 1 不返回(继续自旋)。 ———— 译注  

> ② 本节稍后会在正文中描述 “黑科技”(Black Magic)。 ———— 译注

下面这个类展示了如何使用 `SimpleSpinLock`：

```C#
public sealed class SomeResource {
    private SimpleSpinLock m_sl = new SimpleSpinLock();

    public void AccessResource() {
        m_sl.Enter();
        // 一次只有一个线程才能进入这里访问资源...
        m_sl.Leave();
    }
}
```

`SimpleSpinLock` 的实现很简单。如果两个线程同时调用 `Enter`，那么 `Interlocked.Exchange` 会确保一个线程将 `m_resourceInUse` 从 0 变成 1，并发现 `m_resourceInUse` 为 0<sup>③</sup>，然后这个线程从 `Enter` 返回，使它能继续执行 `AccessResource` 方法中的代码。另一个线程会将 `m_resourceInUse` 从 1 变成 1。由于不是从 0 变成 1，所以会不停地调用 `Exchange` 进行“自旋”，直到第一个线程调用 `Leave`。

> ③ `Interlocked.Exchange` 方法将一个存储位置设为指定值，并返回该存储位置的原始值。详情请参考文档。 ———— 译注

第一个线程完成对 `SomeResource` 对象的字段的处理之后会调用 `Leave`。`Leave` 内部调用 `Volatile.Write`，将 `m_resourceInUse` 更改回 0。这造成正在“自旋”的线程能够将 `m_resourceInUse` 从 0 变为 1，所以终于能从 `Enter` 返回，终于可以开始访问 `SomeReource` 对象的字段。

这就是线程同步锁的一个简单实现。这种锁最大的问题在于，在存在对锁的竞争的前提下，会造成线程“自旋”。这个“自旋”会浪费宝贵的 CPU 时间，阻止 CPU 做其他更有用的工作。因此，自旋锁只应该用于保护那些会执行得非常快的代码区域。

自旋锁一般不要在单 CPU 机器上使用，因为在这种机器上，一方面是希望获得锁的线程自旋，一方面是占有锁的线程不能快速释放锁。如果占有锁的线程的优先级低于想要获取锁的线程(自旋线程)，局面还会变得糟糕，因为占有所得线程可能根本没有机会运行 。这会造成“活锁”情形<sup>④</sup>。Windows 有时会短时间地动态提升一个线程的优先级。因此，对于正在使用自旋锁的线程，应该禁止像这样的优先级提升；请参考 `System.Diagnostics.Process` 和 `System.Diagnostics.ProcessThread` 的 `PriorityBoostEnabled` 属性。超线程机器同样存在自旋锁的问题。为了解决这些问题，许多自旋锁内部都有一些额外的逻辑；我将这些额外的逻辑称为“黑科技”(Black Magic)。这里不打算过多讲解其中的细节，因为随着越来越多的人开始研究锁及其性能，这些逻辑也可能发生变化。但我可以告诉你的是：FCL 提供了一个名为 `System.Threading.SpinWait` 的结构，它封装了人们关于这种“黑科技”的最新研究成果。

> ④ 活锁和死锁的区别请参见 29.2 节“基元用户模式和内核模式构造”。 ———— 译注

FCL 还包含了一个 `System.Threading.SpinLock` 结构，它和前面展示的 `SimpleSpinLock` 类相似，只是使用了 `SpinWait` 结构来增强性能。`SpinLink` 结构还提供了超时支持。很有器的一点是，我的 `SimpleSpinLock` 和 FCL 的 `SpinLink` 都是值类型。这意味着它们是轻量级的、内存友好的对象。例如，如果需要将一个锁同集合中的每一项关联，`SpinLock` 就是很好的选择。但一定不要到底传递 `SpinLock` 实例，否则它们会被复制，而你会失去所有同步。虽然可以定义实例 `SpinLock` 字段，但不要将字段标记为 `readonly`，因为在操作锁的时候，它的内部状态必须改变。

> 在线程处理中引入延迟  
> “黑科技”旨在让希望获得资源的线程暂停执行，使当前拥有资源的线程能执行它的代码并让出资源。为此，`SpinWait` 结构内部调用 `Thread` 的静态 `Sleep`，`Yield` 和 `SpinWait` 方法。在这里的补充内容中，我想简单解释一下这些方法。

> 线程可告诉系统它在指定时间内不想被调度，这是调用 `Thread` 的静态 `Sleep` 方法来实现的：

```C#
public static void Sleep(Int32 millisecondsTimeout);
public static void Sleep(TimeSpan timeout);
```

> 这个方法导致线程在指定时间内挂起。调用 `Sleep` 允许线程自愿放弃它的时间片的剩余部分。系统会使线程在大致指定的时间里不被调度。没有错————如果告诉系统你希望一个线程睡眠 100 毫秒，那么会睡眠大致那么长的时间，但也有可能会多睡眠几秒、甚至几分钟的时间。记住，Windows 不是实时操作系统。你的线程可能在正确的时间唤醒，但具体是否这样，要取决于系统中正在发生的别的事情。

> 可以调用 `Sleep`，并为 `millisecondsTimeout` 参数传递 `System.Threading.Timeout.Infinite` 中的值(定义为 `-1`)。这告诉系统永远不调度线程，但这样做没什么意义。更好的做法是让线程退出，回收它的栈和内核对象。可以向 `Sleep` 传递 `0`，告诉系统调用线程放弃了它当前时间片的剩余部分，强迫系统调度另一个线程。但系统可能重新调度刚才调用了 `Sleep` 的线程(如果没有相同或更高优先级的其他可调度线程，就会发生这种情况)。

> 线程可要求 Windows 在当前 CPU 上调度另一个线程，这是通过 `Thread` 的 `Yield` 方法来实现的：

```C#
public static Boolean Yield();
```

> 如果 Windows 发现有另一个线程准备好在当前处理器上运行，`Yield` 就会返回 `true` ，调用 `Yield` 的线程会提前结束它的时间片<sup>①</sup>，所选的线程得以运行一个时间片。然后，调用 `Yield` 的线程被再次调度，开始用一个全新的时间片运行。如果 Windows 发现没有其他线程准备在当前处理器上运行，`Yield` 就会返回 `false`，调用 `Yield` 的线程继续运行它的时间片。

>> ① 这正是 yield 一词在当前上下文中的含义，即 放弃 或 叫停；而不是文档中翻译的 “生成” 或 “产生”。例如。文档将 “If this method succeeds, the rest of the thread's current time slice is yielded.” 这句话翻译成“如果此方法成功，则生成该线程当前时间片的其余部分。”(参见 MSDN 的 `Thread.Yield` 方法)。我不得不说，“翻译记忆”害死人，因为它不区分上下文。相应地，C# 的 `yield` 关键字就确实有“生成”的意思。 ———— 译注

> `Yield` 方法旨在使 “饥饿” 状态的、具有相等或更低优先级的线程有机会运行。如果一个线程希望获得当前另一个线程拥有的资源，就调用这个方法。如果运气好，Windows 会调度当前拥有资源的线程，而那个线程会让出资源。然后，当调用 `Yield` 的线程再次运行时就会拿到资源。

> 调用 `Yield` 的效果介于调用 `Thread.Sleep(0)` 和 `Thread.Sleep(1)` 之间。`Thread.Sleep(0)`不允许较低优先级的线程运行，而`Thread.Sleep(1)` 总是强迫进行上下文切换，而由于内部系统计时器的解析度的问题， Windows 总是强迫线程睡眠超过 1 毫秒的时间。

> 事实上，超线程 CPU 一次只允许一个线程运行。所以，在这些 CPU 上执行“自旋”循环时，需要强迫当前线程暂停，使 CPU 有机会切换到另一个线程并允许它运行。线程可调用 `Thread` 的 `SpinWait` 方法强迫它自身暂停，允许超线程 CPU 切换到另一线程：

```C#
public static void SpinWait(Int32 iterations);
```

> 调用这个方法实际会执行一个特殊的 CPU 指令；它不告诉 Windows 做任何事情(因为 Windows 已经认为它在 CPU 上调度了两个线程)。在非超线程 CPU 上，这个特殊 CPU 指令会被忽略。

> 要更多地了解这些方法，请参见它们的 Win32 等价函数：`Sleep`，`SwitchToThread` 和 `YieldProcessor`。另外，要想进一步了解如何调整系统计时器的解析度，请参考 Win32 `timeBeginPeriod` 和 `timeEndPeriod` 函数。

### 29.3.4 Interlocked Anything 模式

许多人在查看 `Interlocked` 的方法时，都好奇 **Microsoft** 为什么不创建一组更丰富的 `Interlocked` 方法，使它们适用于更广泛的情形。例如，如果 `Interlocked` 类能提供 `Multiple`，`Divide`，`Minimum`，`Maximum`，`And`，`Or`，`Xor`等方法，那么不是更好吗？虽然 `Interlocked` 类没有提供这些方法，但一个已知的模式允许使用 `Interlocked.CompareExchange` 方法以原子方式在一个 `Int32` 上执行任何操作。事实上，由于 `Interlocked.ComoareExchange` 提供了其他重载版本，能操作 `Int64`，`Single`，`Double`，`Object` 和泛型引用类型，所以该模式适合所有这些类型。

该模型类似于在修改数据库记录时使用的乐观并发模式<sup>①</sup>。下例使用该模式创建一个原子 `Maximum` 方法。

> ① 乐观并发控制(又名“乐观锁”，Optimistic Concurrency Control，缩写“OCC”)是一种并发控制的方法。它假设多用户并发的事务在处理时不会彼此互相影响，各事务能够在不产生锁的情况下处理各自影响的那部分数据。在提交数据更新之前，每个事务会先检查在该事务读取数据后，有没有其他事务又修改了该数据。如果其他事务有更新的话，正在提交的事务会进行回滚。乐观事务控制最早是由孔祥重(H.T.Kung)教授提出。乐观并发控制多数用于数据争用不大、冲突较少的环境中，这种环境中，偶尔回滚事务的成本会低于读取数据时锁定数据的成本，因此可以获得比其他并发控制方法更高的吞吐量。 ———— 维基百科

```C#
public static Int32 Maximum(ref Int32 target, Int32 value) {
    Int32 currentVal = target, startVal, desiredVal;

    // 不要在循环中访问目标(target)，除非是想要改变它时另一个线程也在动它
    do {
        // 记录这一次循环迭代的起始值(startVal)
        startVal = currentVal;

        // 基于 startVal 和 value 计算 desiredVal
        desiredVal = Math.Max(startVal, value);

        // 注意：线程在这里可能被 “抢占”，所以以下代码不是原子性的：
        // if (target == startVal) target = desiredVal;

        // 而应该使用以下原子性的 CompareExchange 方法，它
        // 返回在 target 在(可能)被方法修改之前的值
        currentVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);

        // 如果 target 的值在这一次循环迭代中被其他线程改变，就重复
    } while (startVal != currentVal);

    // 在这个线程尝试设置它之前返回最大值
    return desiredVal;
}
```

现在解释一下实际发生的事情。进入方法后，`currentVal` 被初始化为方法开始执行时的 `target` 值。然后，在循环内部，`startVal` 被初始化为同一个值。可用 `startVal` 执行你希望的任何操作。这个操作可以非常复杂，可以包含成千上万行代码。但最终要得到一个结果，并将结果放到 `desiredVal` 中。本例判断 `startVal` 和 `value` 哪个最大。

现在，当这个操作进行时，其他线程可能更改 `target`。虽然几率很小，但仍是有可能发生的。如果真的发生，`desiredVal` 的值就是基于存储在`startVal` 中的旧值而获得的，而非基于 `target` 的新值。这时就不应更改 `target`。我们用 `Interlocked.ComapreExchange` 方法确保在没有其他线程更改 `target` 的前提下将 `target` 的值更改为 `desiredVal`。该方法验证 `target` 值和 `startVal` 值匹配(`startVal` 代表操作开始前的 `target` 值)。如果 `target` 值没有改变，`CompareExchange` 就把它更改为 `desiredVal` 中的新值。如果 `target` 的值被(另一个线程)改变了，`CompareExchange` 就不更改 `target`.

`CompareExchange` 在调用的同时会返回 `target` 中的值<sup>①</sup>，我将该值存储到 `currentVal` 中。然后比较 `startVal` 和 `currentVal`。两个值相等，表明没有其他线程更改 `target`，`target`现在包含 `desiredVal` 中的值，`while` 循环不再继续，方法返回。相反，如果 `startVal` 不等于 `currentVal`，表明有其他线程更改了`target`，`target` 没有变成 `desiredVal` 中的值，所以 `while` 循环继续下一次迭代，再次尝试相同的操作，这一次用 `currentVal` 中的新值来反映其他线程的更改。

> ① 更准确地说，是返回 `target`(第 1 个参数)的原始值。`CompareExchange` 方法比较第 1 个和第 3 个参数，相等就将用第 2 个参数的值替换第 1 个参数的值。与此同时，方法返回第 1 个参数的原始值。———— 译注

我个人在自己的大量代码中用的都是这个模式。甚至专门写了一个泛型方法 `Morph` 来封装这个模式：<sup>②</sup>

> ② `Morph` 方法由于调用了 `morpher` 回调方法，所以肯定会招致一定的性能惩罚。要想获得最佳性能，只能以内联或嵌入(inline) 方式执行操作，就像 `Maximum` 的例子一样。

```C#
delegate Int32 Morpher<TResult, TArgument>(Int32 startValue, TArgument argument,
    out TResult morphResult);

static TResult Morph<TResult, TArgument>(ref Int32 target, TArgument argument,
    Morpher<TResult, TArgument> morpher) {

    TResult morphResult;
    Int32 currentVal = target, startVal, desiredVal;
    do {
        startVal = currentVal;
        desiredVal = morpher(startVal, argument, out morphResult);
        currentVal = Interlocked.CompareExchange(ref target, desiredVal, startVal);
    } while (startVal != currentVal);
    return morphResult;
}
```

## <a name="29_4">29.4 内核模式构造</a>

Windows 提供了几个内核模式的构造来同步线程。内核模式的构造比用户模式的构造慢得多，一个原因是它们要求 Windows 操作系统自身的配合，另一个原因是在内核对象上调用的每个方法都造成调用线程从托管代码转换为本机(native)用户模式代码，再转换为本机(native)内核模式代码。然后，还要朝相反的方向一路返回。这些转换需要大量 CPU 时间；经常执行会对应用程序的总体性能造成负面影响。

但内核模式的构造具备基元用户模式构造所不具备的优点。

* 内核模式的构造检测到在一个资源上的竞争时，Windows 会阻塞输掉的线程，使它不占用一个 CPU “自旋”(spinning)，无谓地浪费处理器资源。

* 内核模式的构造可实现本机(native)和托管(managed)线程相互之间的同步。

* 内核模式的构造可同步在同一台机器的不同进程中运行的线程。

* 内核模式对的构造可应用安全性设置，防止未经授权的账户访问它们。

* 线程可一直阻塞，直到集合中的所有内核模式构造都可用，或直到集合中的任何内核模式构造可用。

* 在内核模式的构造上阻塞的线程可指定超时值；指定时间内访问不到希望的资源，线程就可以解除阻塞并执行其他任务。

事件和信号量是两种基元内核模式线程同步构造。至于其他内核模式构造，比如互斥体，则是在这两个基计构造上构建的。<sup>①</sup>欲知 Windows 内核模式构造的详情，请参考我和 Christophe Nasarre 合著的 《Windows 核心编程(第 5 版)》。

> ① 在文档中，semaphores 翻译成“信号量”，mutex 翻译成“互斥体”。本书采用了文档的译法。 ———— 译注

`System.Threading` 命名空间提供了一个名为 `WaitHandle` 抽象基类。`WaitHandle` 类是一个很简单的类，它唯一的作用就是包装一个 Windows 内核对象句柄。FCL 提供了几个从 `WaitHandle` 派生的类。所有类都在 `System.Threading` 命名空间中定义。类层次结构如下所示：

```doc
WaitHandle
    EventWaitHandle
        AutoResetEvent
        ManualResetEvent
    Semaphore
    Mutex 
```

`WaitHandle` 基类内部有一个 `SafeWaitHandle` 字段，它容纳了一个 Win32 内核对象句柄。这个字段是在构造一个具体的 `WaitHandle` 派生类时初始化的。除此之外，`WaitHandle` 类公开了由所有派生类继承的方法。在一个内核模式的构造上调用的每个方法都代表一个完整的内存栅栏<sup>②</sup>。下面是 `WaitHandle` 的一些有意思的公共方法(未列出某些方法的某些重载版本)：

> ② 之所以用栅栏这个词，是表明掉调用这个方法之前的任何变量写入都必须在这个方法调用之前发生；而这个调用之后的任何变量读取都必须在这个调用之后发生。————译注

```C#
public abstract class WaitHandle : MarshalByRefObject, IDisposable {
    // WaitOne 内部调用 Win32 WaitForSingleObjectEx 函数
    public virtual Boolean WaitOne();
    public virtual Boolean WaitOne(Int32 millisecondsTimeout);
    public virtual Boolean WaitOne(TimeSpan timeout);

    // WaitAll 内部调用 Win32 WaitForMultipleObjectsEx 函数
    public static Boolean WaitAll(WaitHandle[] waitHandles);
    public static Boolean WaitAll(WaitHandle[] waitHandles, Int32 millisecondsTimeout);
    public static Boolean WaitAll(WaitHandle[] waitHandles, TimeSpan timeout);

    // WaitAny 内部调用 Win32 WaitForMultipleObjectsEx 函数
    public static Int32 WaitAny(WaitHandle[] waitHandles);
    public static Int32 WaitAny(WaitHandle[] waitHandles, Int32 millisecondsTimeout); 
    public static Int32 WaitAny(WaitHandle[] waitHandles, TimeSpan timeout);
    public const Int32 WaitTimeout = 258; // 超时就从 WaitAny 返回这个

    // Dispose 内部调用 Win32 CloseHandle 函数 – 自己不要调用
    public void Dispose();
}
```

这些方法有几点需要注意。

* 可以调用 `WaitHandle` 的 `WaitOne` 方法让调用线程等待底层内核对象收到信号。这个方法在内部调用 Win32 `WaitForSingleObjectEx`函数。如果对象收到信号，返回的 `Boolean` 是 `true`；超时就返回 `false`。

* 可以调用 `WaitHandle` 的静态 `WaitAll` 方法，让调用线程等待 `WaitHandle[]` 中指定的所有内核对象都收到信号。如果所有对象都收到信号，返回的 `Boolean` 是 `true`；超时则返回 `false`。这个方法在内部调用 Win32 `WaitForMultipleObjectsEx` 函数，为`bWaitAll` 参数传递`TRUE`。

* 可以调用 `WaitHandle` 的静态 `WaitAny` 方法让调用线程等待 `WaitHandle[]` 中指定的任何内核对象收到信号。返回的 `Int32` 是与收到信号的内核对象对应的数组元素索引；如果在等待期间没有对象收到信号，则返回 `WaitHandle.WaitTimeout`。这个方法在内部调用 Win32 `WaitForMultipleObjectsEx` 函数，为 `bWaitALl` 参数传递 `FALSE`。

* 再传给 `WaitAny` 和 `WaitAll` 方法的数组中，包含的元素数不能超过 64 个，否则方法会抛出一个 `System.NotSupportedException`。

* 可以调用 `WaitHandle` 的 `Dispose` 方法来关闭底层内核对象句柄。这个方法在内部调用 Win32 `CloseHandle` 函数。只有确定没有别的线程要使用内核对象才能显式调用 `Dispose`。 你需要写代码并进行测试，这是一个巨大的负担。所以我强烈反对显式调用 `Dispose`；相反，让垃圾回收器(GC)去完成清理工作。GC 知道什么时候没有线程使用对象，会自动进行清理。从某个角度看，GC 是在帮你进行线程同步！

> 注意 在某些情况下，当一个 COM 单线程套间线程<sup>①</sup>阻塞时，线程可能在内部醒来以 pump 消息。例如，阻塞的线程会醒来处理发自另一个线程的 Windows 消息。这个设计是为了支持 COM 互操作性。对于大多数应用程序，这都不是一个问题 ———— 事实上，反而是一件好事。然而，如果你的代码在处理消息期间获得另一个线程同步锁，就可能发生死锁。如第 30 章所述，所有混合锁都在内部调用这些方法。所以，使用混合锁存在相同的利与弊。

不接受超时参数的那些版本的 `WaitOne` 和 `WaitAll` 方法应返回 `void` 而不是 `Boolean`。原因是隐含的超时时间是无限长(`System.Threading.Timeout.Infinite`)，所以它们只会返回 `true`。调用任何这些方法时都不需要检查返回值。

如前所述，`AutoResetEvent`，`ManualResetEvent`，`Semaphore` 和 `Mutex` 类都派生自 `WaitHandle`，因此它们继承了 `WaitHandle` 的方法和行为。但这些类还引入了一些自己的方法，下面将进行解释。

首先，所有这些类的构造器都在内部调用 Win32 `CreateEvent`(为 `BManualReset` 参数传递 `FALSE` 或 `TRUE`)、`CreateSemaphore`或`CreateMutex` 函数。从所有这些调用返回的句柄值都保存在 `WaitHandle` 基类内部定义的一个私有 `SafeWaitHandle` 字段中。

其次，`EventWaitHandle`，`Semaphore` 和 `Mutex` 类都提供了静态 `OpenExisting` 方法，它们在内部调用 Win32 `OpenEvent`，`OpenSemaphore` 或 `OpenMutex` 函数，并传递一个 `String` 实参(标识现有的一个具名内核对象)。所有函数返回的句柄值都保存到从 `OpenExisting` 方法返回的一个新构造的对象中。如果指定名称的内核对象不存在，就抛出一个 `WaitHandleCannotBeOpenedException` 异常。

内核模式构建的一个常见用途是创建在任何时刻只允许它的一个实例运行的应用程序。这种单实例应用程序的例子包括 Microsoft Office Outlook，Windows Live Messenger，Windows Media Player 和 Windows Media Center。下面展示了如何实现一个单实例应用程序：

```C#
using System;
using System.Threading;

public static class Program {
    public static void Main() {
        Boolean createdNew;

        // 尝试创建一个具有指定名称的内核对象
        using (new Semaphore(0, 1, "SomeUniqueStringIdentifyingMyApp", out createdNew)) {
            if (createdNew) {
                // 这个线程创建了内核对象，所以肯定没有这个应用程序
                // 的其他实例正在运行。在这里运行应用程序的其余部分...
            } else {
                // 这个线程打开了一个具有相同字符串名称的、现有的内核对象；
                // 表明肯定正在运行这个应用程序的另一个实例。
                // 这里没什么可以做的事情，所以从 Main 返回，终止应用程序
                // 的这个额外的实例。
            }
        }
    }
}
```

上述代码使用的是 `Semaphore`，但换成 `EventWaitHandle` 或 `Mutex` 一样可以，因为我并没有真正使用对象提供的线程同步行为。但我利用了在创建任何种类的内核对象时由 Windows 内核提供的一些线程同步行为。下面解释一下代码是如何工作的。假定这个进程的两个实例同时启动。每个进程都有自己的线程，两个线程都尝试创建具有相同字符串名称(本例是 "`SomeUniqueStringIdentifyingMyApp`")的一个`Semaphore`。Windows 内核确保只有一个线程实际地创建具有指定名称的内核对象；创建对象的线程会将它的 `createdNew` 变量设为 `true`。

对于第二个线程，Windows 发现具有指定名称的内核对象已经存在了。因此，不允许第二个线程创建另一个同名的内核对象。不过，如果这个线程继续运行的话，它能访问和第一个进程的线程所访问的一样的内核对象。不同进程中的线程就是这样通过内核对象来通信的。但在本例中，第二个进程的线程看到它的 `createdNew` 变量设为 `false`，所以知道有进程的另一个实例正在运行，所以进程的第二个实例立即退出。

### 29.4.1 `Event` 构造

事件(event)其实只是由内核维护的 `Boolean` 变量。事件为 `false`，在事件上等待的线程就阻塞；事件为 `true`，就解除阻塞。有两种事件，即自动重置事件和手动重置事件。当一个自动重置事件为 `true` 时，它只唤醒一个阻塞的线程，因为在解除第一个线程的阻塞后，内核将事件*自动重置*回`false`，造成其余线程继续阻塞。而当一个手动重置事件为 `true` 时，它解除正在等待它的所有线程的阻塞，因为内核不将事件自动重置回 `false`；现在，你的代码必须将事件*手动重置*回`false`。下面是与事件相关的类：

```C#
public class EventWaitHandle : WaitHandle {
    public Boolean Set();       // 将 Boolean 设为 true；总是返回 true
    public Boolean Reset();     // 将 Boolean 设为 fasle；总是返回 false
}

public sealed class AutoResetEvent : EventWaitHandle {
    public AutoResetEvent(Boolean initialState);
}

public sealed class ManualResetEvent : EventWaitHandle {
    public ManualResetEvent(Boolean initialState);
}
```

可用自动重置事件轻松创建线程同步锁，它的行为和前面展示的 `SimpleSpinLock` 类相似：

```C#
internal sealed class SimpleWaitLock : IDisposable {
    private readonly AutoResetEvent m_available;

    public SimpleWaitLock() {
        m_available = new AutoResetEvent(true);     // 最开始可自由使用
    }

    public void Enter() {
        // 在内核中阻塞 ①，直到资源可用
        m_available.WaitOne();
    }

    public void Leave() {
        // 让另一个线程访问资源
        m_available.Set();
    }

    public void Dispose() {
        m_available.Dispose();
    }
}
```

> ① 正是因为发生竞争时，没有竞争赢的线程会阻塞，所以这种方法能最有效地节省资源。 ———— 译注

可采取和使用 `SimpleSpinLock` 时完全一样的方式使用这个 `SimpleWaitLock`。事实上，外部行为是完全相同的；不过，两个锁的性能截然不同。锁上面灭有竞争的时候，`SimpleWaitLock` 比 `SimpleSpinLock` 慢得多，因为对 `SimpleWaitLock` 的 `Enter` 和 `Leave` 方法的每一个调用都强迫调用线程从托管代码转换为内核代码，再转换回来————这是不好的地方。但在存在竞争的时候，输掉的线程会被内核阻塞，不会在那里“自旋”，从而浪费 CPU 时间————这是好的地方。还要注意，构造 `AutoResetEvent` 对象并在它上面调用 `Dispose` 也会造成从托管向内核的转换，对性能造成负面影响。这些调用一般很少发生，所以一般不必过于关心它们。

为了更好地理解性能上的差异，我写了一些代码：

```C#
public static void Main() {
    Int32 x = 0;
    const Int32 iterations = 10000000;  // 1000万

    // x 递增 1000万次，要花多长时间？
    Stopwatch sw = Stopwatch.StartNew();
    for (Int32 i = 0; i < iterations; i++) {
        x++;
    }
    Console.WriteLine("Incrementing x: {0:N0}", sw.ElapsedMilliseconds);

    // x 递增 1000万次，加上调用一个什么都不做的方法的开销，要花多长时间？
    sw.Restart();
    for (Int32 i = 0; i < iterations; i++) {
        M(); x++; M();
    }
    Console.WriteLine("Incrementing x in M: {0:N0}", sw.ElapsedMilliseconds);

    // x 递增 1000 万次，加上调用一个无竞争的 SpinLock 的开销，要花多长时间？
    SpinLock sl = new SpinLock(false);
    sw.Restart();
    for (int i = 0; i < iterations; i++) {
        Boolean taken = false; sl.Enter(ref taken); x++; sl.Exit();
    }
    Console.WriteLine("Incrementing x in SpinLock: {0:N0}", sw.ElapsedMilliseconds);

    // x 递增 1000 万次，加上调用一个无竞争的 SimpleWaitLock 的开销，要花多长时间？
    using(SimpleWaitLock swl = new SimpleWaitLock()) {
        sw.Restart();
        for (int i = 0; i < iterations; i++) {
            swl.Enter(); x++; swl.Leave();
        }
        Console.WriteLine("Incrementing x in SimpleWaitLock: {0:N0}", sw.ElapsedMilliseconds);
    }
}

[MethodImpl(MethodImplOptions.NoInlining)]
private static void M() { /* 这个方法什么都不做，直接返回 */ }
```

在我的机器上运行上述代码，得到以下输出：

```cmd
Incrementing x: 8                       最快
Incrementing x in M: 69                 慢约 9 倍
Incrementing x in SpinLock: 164         慢约 21 倍
Incrementing x in SimpleWaitLock: 8854  慢约 1107 倍
```

我的 MAC 本上的执行结果：

```cmd
Incrementing x: 20
Incrementing x in M: 51
Incrementing x in SpinLock: 236
Incrementing x in SimpleWaitLock: 5,989
```

由此可以看到，单出递增 `x` 只需 8 毫秒。递增前后多调用一个方法，就要多花约 9 倍的时间。然后，在用户模式的构造中执行递增，代码变慢了 21 倍(164/8)。最后，如果使用内核模式的构造，程序更是慢的可怕，慢了大约 1107 倍(8864/8)！所以，线程同步能避免就尽量避免。如果一定要进行线程同步，就尽量使用用户模式的构造。内核模式的构造要尽量避免。

### 29.4.2 `Semaphore` 构造

信号量(semaphore)其实就是由内核维护的 `Int32` 变量。信号量为 0 时，在信号量上等待的线程会阻塞；信号量大于 0 时解除阻塞。在信号量上等待的线程解除阻塞时，内核自动从信号量的计数中减 1。信号量还关联了一个最大 `Int32` 值，当前技术绝不允许超过最大计数。下面展示了 `Semaphore` 类的样子：

```C#
public sealed class Semaphore : WaitHandle {
    public Semaphore(Int32 initialCount, Int32 maximumCount);
    public Int32 Release();         // 调用 Release(1); 返回上一个计数
    public Int32 Release(Int32 releaseCount);   // 返回上一个计数
}
```

下面总结一下这三种内核模式基元的行为。

* 多个线程在一个自动重置事件上等待时，设置事件只导致一个线程被解除阻塞。

* 多个线程在一个手动重置事件上等待时，设置事件导致所有线程被解除阻塞。

* 多个线程在一个信号量上等待时，释放信号量导致 `releaseCount` 个线程被解除阻塞(`releaseCount` 是传给 `Semaphore` 的 `Release`方法的实参)。

因此，自动重置事件在行为上和最大计数为 1 的信号量非常相似。两者的区别在于，可以在一个自动重置事件上连续多次调用 `Set`，同时仍然只有一个线程解除阻塞。相反，在一个信号量上连续多次调用 `Release`，会使它的内部计数一直递增，这可能解除大量线程的阻塞。顺便说一句，如果在一个信号量上多次调用 `Release`，会导致它的计数超过最大计数，这时 `Release` 会抛出一个 `SemaphoreFullException`。

可像下面这样用信号量重新实现 `SimpleWaitLock`，允许多个线程并发访问一个资源(如果所有线程以只读方式访问资源，就是安全的)：

```C#
internal sealed class SimpleWaitLock : IDisposable {
    private Semaphore m_available;

    public SimpleWaitLock(Int32 maxConcurrent) {
        m_available = new Semaphore(maxConcurrent, maxConcurrent);
    }

    public void Enter() {
        // 在内核中阻塞直到资源可用
        m_available.WaitOne();
    }

    public void Leave() {
        // 让其他线程访问资源
        m_available.Release();
    }

    public void Dispose() { m_available.Close(); }
}
```

### 29.4.3 `Mutex` 构造

互斥体(mutex)代表一个互斥的锁。它的工作方式和 `AutoResetEvent` 或者计数为 1 的 `Semaphore` 相似，三者都是一次只释放一个正在等待的线程。下面展示了 `Mutex` 类的样子：

```C#
public sealed class Mutex : WaitHandle {
    public Mutex();
    public void ReleaseMutex();
}
```

互斥体有一些额外的逻辑，这造成它们比其他构造更复杂。首先，`Mutex` 对象会查询调用线程的 `Int32` ID，记录是哪个线程获得了它。一个线程调用`ReleaseMutex` 时，`Mutex` 确保调用线程就是获取 `Mutex` 的那个线程。如若不然，`Mutex` 对象的状态就不会改变，而 `ReleaseMutex` 会抛出一个 `System.ApplicationException`。另外，拥有 `Mutex` 的线程因为任何原因而终止，在 `Mutex` 上等待的某个线程会因为抛出 `System.Threading.AbandonedMutexException` 异常而被唤醒。该异常通常会成为未处理的异常，从而终止整个进程。这是好事，因为线程在获取了一个 `Mutex` 之后，可能在更新完 `Mutex` 所保护的数据之前终止。如果其他线程捕捉了 `AbandonedMutexException`，就可能视图访问损坏的数据，造成无法预料的结果和安全隐患。

其次， `Mutex` 对象维护着一个递归计数(recursion count)，指出拥有该 `Mutex` 的线程拥有了它多少次。如果一个线程当前拥有一个 `Mutex`，而后该线程再次在 `Mutex` 上等待，计数就会递增，这个线程允许继续进行。线程调用 `ReleaseMutex` 将导致计数递减。只有计数变成 0，另一个线程才能成为该 `Mutex` 的所有者。

大多数人都不喜欢这个额外的逻辑。这些“功能”是有代价的。`Mutex` 对象需要更多的内存来容纳额外的线程 ID 和计数信息。更要紧的是，`Mutex` 代码必须维护这些信息，使锁变得更慢。如果应用程序需要(或希望)这些额外的功能，那么应用程序的代码可以自己实现；代码不一定要放到 `Mutex` 对象中。因此，许多人都会避免使用 `Mutex` 对象。

通常，当一个方法获取了一个锁，然后调用也需要锁的另一个方法，就需要一个递归锁。如以下代码所示：

```C#
internal class SomeClass : IDisposable {
    private readonly Mutex m_lock = new Mutex();

    public void Method1() {
        m_lock.WaitOne();
        // 随便做什么事情...
        Method2(); // Method2 递归地获取锁
        m_lock.ReleaseMutex();
    }

    public void Method2() {
        m_lock.WaitOne();
        // 随便做什么事情...
        m_lock.ReleaseMutex();
    }

    public void Dispose() { m_lock.Dispose(); }
} 
```

在上述代码中，使用一个 `SomeClass` 对象的代码可以调用 `Method1`，它获取 `Mutex`，执行一些线程安全的操作，然后调用 `Method2`，它也执行一些线程安全的操作。由于 `Mutex` 对象支持递归，所以线程会获取两次锁，然后释放它两次。在此之后，另一个线程才能拥有这个 `Mutex`。如果 `SomeClass` 使用一个 `AutoResetEvent` 而不是 `Mutex`，线程在调用 `Method2` 的 `WaitOne` 方法时会阻塞。

如果需要递归锁，可以使用一个 `AutoResetEvent` 来简单地创建一个：

```C#
internal sealed class RecursiveAutoResetEvent : IDisposable {
    private AutoResetEvent m_lock = new AutoResetEvent(true);
    private Int32 m_owningThreadId = 0;
    private Int32 m_recursionCount = 0;

    public void Enter() {
        // 获取调用线程的唯一 Int32 ID
        Int32 currentThreadId = Thread.CurrentThread.ManagedThreadId;

        // 如果调用线程拥有锁，就递增递归计数
        if (m_owningThreadId == currentThreadId) {
            m_recursionCount++;
            return;
        }

        // 调用线程不拥有锁，等待它
        m_lock.WaitOne();
        
        // 调用线程现在拥有了锁，初始化拥有线程的 ID 和递归计数
        m_owningThreadId = currentThreadId;
        m_recursionCount = 1;
    }

    public void Leave() {
        // 如果调用线程不拥有锁，就出错了
        if (m_owningThreadId != Thread.CurrentThread.ManagedThreadId)
        throw new InvalidOperationException();

        // 从递归计数中减1
        if (--m_recursionCount == 0) {
            // 如果递归计数为 0，表明没有线程拥有锁
            m_owningThreadId = 0;
            m_lock.Set(); // 唤醒一个正在等待的线程(如果有的话)
        }
    }

    public void Dispose() { m_lock.Dispose(); }
} 
```

虽然 `RecursiveAutoResetEvent` 类的行为和 `Mutex` 类完全一样，但在一个线程试图递归地获取锁时，它的性能会好得多，因为现在跟踪线程所有权和递归的都是托管代码。只有在第一次获取 `AutoResetEvent`，或者最后把它放弃给其他线程时，线程才需要从托管代码转换为内核代码。