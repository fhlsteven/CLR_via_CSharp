# 第 20 章 异常和状态管理

本章内容

* <a href="#20_1">定义“异常”</a>
* <a href="#20_2">异常处理机制</a>
* <a href="#20_3">`System.Exception` 类</a>
* <a href="#20_4">FCL 定义的异常类</a>
* <a href="#20_5">抛出异常</a>
* <a href="#20_6">定义自己的异常类</a>
* <a href="#20_7">用可靠性换取开发效率</a>
* <a href="#20_8">设计规范和最佳实践</a>
* <a href="#20_9">未处理的异常</a>
* <a href="#20_10">对异常进行调试</a>
* <a href="#20_11">异常处理的性能问题</a>
* <a href="#20_12">约束执行区域(CER)</a>
* <a href="#20_13">代码协定</a>

本章重点在于错误处理，但并非仅限与此。错误处理要分几个部分。首先要定义到底什么是错误。然后要讨论如何判断正在经历一个错误，以及如何从错误中恢复。这个时候，状态就成为一个要考虑的问题，因为错误常常在不恰当的时候发生。代码可能在状态改变的中途发生错误。这时需要将一些状态还原为改变之前的样子。当然，还要讨论代码如何通知调用者有错误发生。

在我看来，异常处理是 CLR 最薄弱的一个环节，造成开发人员在写托管代码时遇到许多问题。经过多年的发展，Microsoft 确实进行了一系列显著的改进来帮助开发人员处理错误。但我认为在获得一个真正良好、可靠的系统之前， Microsoft 仍有大量工作要做。针对未处理的异常、约束执行区域(constraind execution region, CER)、代码协定、运行时包装的异常以及未捕捉的异常，本章要讨论处理它们时的改进。

## <a name="20_1">20.1 定义"异常"</a>

设计类型时要想好各种使用情况。类型名称通常是名词，例如 `FileStream` 或者 `StringBuilder`。 然后要为类型定义属性、方法、事件等。这些成员的定义方式(属性的数据类型、方法的参数、返回值等)就是类型的编程接口。这些成员代表本身或者类型实例能执行的行动。行动成员通常用动词表示，例如 `Read`，`Write`，`Flush`，`Append`，`Insert` 和 `Remove`等。当行动成员不能完成任务时，就应抛出异常。

> 重要提示 异常时指成员没有完成它的名称所宣称的行动。

例如以下类定义：

```C#
internal class Account {
    public static void Transfer(Account from, Account to, Decimal amount) {
        from -= amount;
        to += amount;
    }
}
```

`Transfer` 方法接受两个 `Account` 对象和一个代表账号之间转账金额的 `Decimal` 值。显然，`Transfer` 方法的作用是从一个账户扣除钱，把钱添加到另一个账户中。`Transfer` 方法可能因为多种原因而失败。例如，`from`或`to`实参可能为`null`; `from` 或 `to` 实参引用的可能不是活动账户；`from` 账户可能没有足够的资金；`to`账户的资金可能过多，以至于增加资金时导致账户溢出；`amount` 实参为 `0`、负数或者小数超过两位。

`Transfer` 方法在调用时，它的代码应检查前面描述的种种可能。检测到其中任何一种可能都不能转账，应抛出异常来通知调用者它不能完成任务。事实上，`Transfer` 方法的返回类型为 `void`。 这是由于 `Transfer` 方法没有什么有意义的值需要返回。这一点很容易想得通：方法正常返回<sup>①</sup>表明转账成功，失败就抛出一个有意义转账成功，失败就抛出一个有意义的异常。

> ① 指返回到调用位置，而不是返回一个值。————译注

面向对象编程极大提高了开发人员的开发效率，因为可以写这样的代码：

`Boolean f = "Jeff".Substring(1, 1).ToUpper().EndWith("E");    // true`

这行代码将多个操作链接到一起<sup>②</sup>。我很容易写这行代码，其他人也很容易阅读和维护，因为它的意图很明显：获取字符串，取出一部分，全部大写那个部分，然后检查那个部分是否”E“结尾。出发点不错，但有一个重要的前提：没有操作失败，中途不出错。但错误总是可能发生的，所以需要以一种方式处理错误。事实上，许多面向对象的构造——构造器、获取和设置属性、添加和删除事件、调用操作符重载和调用转换操作符等———都没办法返回错误代码，但它们仍然需要报告错误。Microsoft .NET Framework 和所有编程语言通过**异常处理**来解决这个问题。

> ② 事实上，利用 C# 的”扩展方法“，可以将更多本来不能链接的方法链接到一起。

> 重要提示 许多开发人员都错误地认为异常和某件事件的发生频率有关。例如，一个设计文件 `Read` 方法的开发人员可能会这样想：”读取文件最终会抵达文件尾。由于抵达文件尾部是会发生，所以我设计这个`Read`方法返回一个特殊值来报告抵达了文件尾；我不让它抛出异常。“
问题在于，这是设计 `Read` 方法的开发人员的想法，而非调用 `Read` 方法的开发人员的想法。

> 设计 `Read` 方法的开发人员不可能知道这个方法的所有调用情形。所以，开发人员不可能知道 `Read` 的调用者是不是每次都会一路读取到文件尾。事实上，由于大多数文件包含的都是结构化数据，所以一路读取直至文件尾的情况是很少发生的。

## <a name="20_2">20.2 异常处理机制</a>

本节介绍异常处理机制，以及进行异常处理所需的 C# 构造，但不打算罗列过多的细节。本章旨在提供何时以及如何使用异常处理的设计规范。要更多地了解异常处理机制和相关的 C# 语言结构，请参考文件和 C# 语言规范。另外， .NET Framework 异常处理机制是用 Microsoft Windows 提供的结构化异常处理(Structured Exception Handling, SEH)机制构建的。对 SEH 的讨论有很多，包括我自己的 《Windows 核心编程(第 5 版)》 一书，其中有 3 章内容专门讨论 SEH。

以下 C# 代码展示了异常处理机制的标准用法，可通过它对异常处理代码块及其用途产生初步认识。代码后面的各小节将正式描述 `try`、`catch` 和 `finally` 块及其用途，并提供关于它们的一些注意事项。

```C#
private void SomeMethod() {

    try {
        // 需要得体地进行恢复和/或清理的代码放在这里
    }
    catch (InvalidOperationException) {
        // 从 InvalidOperationException 恢复的代码放在这里
    }
    catch (IOException) {
        // 从 IOException 恢复的代码放在这里
    }
    catch {
        // 从除了上述异常之外的其他所有异常恢复的代码放在这里
        ...
        // 如果什么异常都捕捉，通常要重新抛出异常。本章稍后将详细解释
        throw；
    }
    finally {
        // 这里的代码对始于 try 块的任何操作进行清理
        // 这里的代码总是执行，不管是不是抛出了异常
    }
    // 如果 try 块没有抛出异常，或者某个 catch 块捕捉到异常，但没有抛出或
    // 重新抛出异常，就执行下面的代码
    ...
}
```

这段代码只是使用各种异常处理块的一种可能的方式。不要被这些代码吓到————大多数方法都只有一个 `try` 块和一个匹配 `finally` 块，或者一个 `try` 块和一个匹配 `finally` 块，或者一个 `try` 块和一个匹配的 `catch` 块。像本例那样有这么多 `catch` 块是很少见的，这里列出它们仅仅是为了演示。

### 20.2.1 `try`块

如果代码需要执行一般性的资源清理操作，需要从异常中恢复，或者两者都需要，就可以放到 `try` 块中。负责清理的代码应放到一个 `finally` 块中。 `try` 块还可包含也许会抛出异常的代码。负责异常恢复的代码应放到一个或多个 `catch`块中。针对应用程序能从中安全恢复的每一种异常，都应该创建一个 `catch` 块。一个 `try` 块至少要有一个关联的 `catch` 块或 `finally` 块，单独一个 `try` 块没有意义， C# 也不允许。

> 重要提示 开发人员有时不知道应该在一个 `try` 块中放入多少代码。这据图取决于状态管理。如果在一个 `try` 块中执行多个可能抛出同一个异常类型的操作，但不同的操作有不同的异常恢复措施，就应该将每个操作都放到它自己的 `try`块中，这样才能正确地恢复状态。

### 20.2.2 `catch`块

`catch`块包含的是响应一个异常需要执行的代码。一个 `try` 块可以关联 0 个或多个 `catch`块。如果 `try` 块中的代码没有造成异常的抛出，CLR 永远不会执行它的任何 `catch` 块。线程将跳过所有 `catch`块，直接执行 `finally` 块(如果有的话)。`finally` 块执行完毕后，从 `finally` 块后面的语句继续执行。

`catch` 关键字后的圆括号中的表达式称为**捕捉类型**。C# 要求捕捉类型必须是`System.Exception`或者它的派生类型。例如，上述代码包含用于处理`InvalidOperationException`异常(或者从它派生的任何异常)和`IOException`异常(后者从它派生的任何异常)的`catch`块。最后一个`catch`块没有指定捕捉类型，能处理除了前面的`catch`块指定的之外的其他所有异常；这相当于捕捉`System.Exception`(只是 `catch` 块大括号中的代码访问不了异常信息)。

> 注意 用 Microsoft Visual Studio 调试 `catch` 块时，可在监视窗口中添加特殊变量名称 `$Exception` 来查看当前抛出的异常对象。

CLR 自上而下搜索匹配的 `catch` 块，所以应该将具体的异常放在顶部。也就是说，首先出现的是派生程度最大的异常类型，接着是它们的基类型(如果有的话)，最后是 `System.Exception`(或者没有指定任何捕捉类型的 `catch`块)。事实上，如果弄反了这个顺序，将较具体的 `catch` 块放在靠近底部的位置，C# 编译器会报错，因为这样的 `catch` 块是不可达的。

在 `try` 块的代码(或者从 `try` 块调用的任何方法)中抛出异常，CLR将搜索捕捉类型与抛出的异常相同(或者是它的基类)的 `catch` 块。如果没有任何捕捉类型与抛出异常匹配，CLR 会去调用栈<sup>①</sup>更高的一层搜索与异常匹配的捕捉类型。如果都到了调用栈的顶部，还是没有找到匹配的 `catch` 块，就会发生未处理的异常。本章后面将更深入地探讨未处理的异常。

> ① 文档翻译成“调用堆栈”。 —— 译注

一旦 CLR 找到匹配的 `catch` 块，就会执行内层所有 `finally` 块中的代码。所谓 “内存`finally`块”是指从抛出异常的`try`块开始，到匹配异常的`catch`块之间的所有`finally`块。<sup>②</sup>注意，匹配异常的那个 `catch` 块所关联的 `finally`块尚未执行，该`finally`块中的代码一直要等到这个`catch` 块中的代码执行完毕才会执行。

> ② 前面说过，异常处理设涉及调用栈的知识，内层没有找到合适的 `catch`，就跑去上一层。以此类推，直至栈顶。不要产生“`finally`块怎么跑到 `try` 和 `catch` 之间”的误解。请用”立体“思维来看待寻找合适 `catch` 的过程。 ———译注

所有内层 `finally` 块执行完毕之后，匹配异常的那个 `catch` 块中的代码才开始执行。`catch` 块中的代码通常执行一些对异常进行处理的操作。在 `catch` 块的末尾，我们有以下三个选择。

* 重新抛出相同的异常，向调用栈高一层的代码通知该异常的发生。

* 抛出一个不同的异常，向调用栈高一层的代码提供更丰富的异常信息。

* 让线程从 `catch` 块的底部退出<sup>③</sup>。

> ③ 次退出(fall out of the bottom of the catch block)非彼退出。不是说要终止线程，而是说执行正常地“贯穿” `catch` 块的底部，并执行匹配的 `finally`块。 ——— 译注

本章稍后将针对每一种技术的使用时机提供一些指导方针。选择前两种技术将抛出异常，CLR 的行为和之前说的一样：回溯调用栈，查找捕捉类型与抛出的异常的类型匹配的`catch`块。

选择最后一种技术，当线程从 `catch` 块的底部退出后，它将立即执行包含在 `finally` 块<sup>④</sup>(如果有的话)中的代码。`finally`块的所有代码执行完毕后，线程退出 `finally` 块，执行紧跟在 `finally` 块之后的语句。如果不存在 `finally` 块，线程将从最后一个 `catch` 块之后的语句开始执行。

> ④ 这个才是与 `catch` 关联的 `finally` 块，也就是常说的 `try-catch-finally` 中的 `finally`。

C# 允许在捕捉类型后指定一个变量。捕捉到异常时，该变量将引用抛出的 `System.Exception` 派生对象。`catch` 块的代码可通过引用该变量来访问异常的具体信息(例如异常发生时的堆栈跟踪<sup>⑤</sup>)。虽然这个对象可以修改，但最好不要这么做，而应把它当成是只读的。本章稍后将解释 `Exception` 类型以及可以在该类型上进行哪些操作，

> ⑤ 对方法调用的跟踪称为堆栈跟踪(stack trace)。堆栈跟踪列表提供了一种循着调用序列跟踪到异常发生处的手段。另外要注意，虽然本书大多数时候会将 `stack` 翻译成 ”栈“而不是”堆栈“，但为了保持和文档及习惯说法的一致，偶尔还是不得不将一些 `stack` 翻译成“堆栈”。——译注

> 注意 你的代码可向 `AppDomain` 的 `FirstChanceException` 事件登记，这样只要 AppDomain 中发生异常，就会收到通知。这个通知是在 CLR 开始搜索任何 `catch` 块之前发生的。欲知该事件的详情，请参见第 22 章 ”CLR 寄宿和 AppDomain”。

### 20.2.3 `finally`块

`finally`块包含的是保证会执行的代码<sup>①</sup>。一般在 `finally` 块中执行`try`块的行动所要求的资源清理操作。

> 终止线程或卸载 AppDomain 会造成 CLR 抛出一个 `ThreadAbortException`，使 `finally` 块能够执行。如果直接用 Win32 函数 `TerminateThread` 杀死线程，或者用 Win32 函数 `TerminateProcess` 或 `System.Environment` 的 `FailFast` 方法杀死进程，`finally` 块不会执行。当然，进程终止后， Windows 会清理该进程使用的所有资源。

例如，在 `try` 块中打开了文件，就应该将关闭文件的代码放到`finally`块中。

```C#
private void ReadData(String pathname) {
    FileStream fs = null;
    try {
        fs = new FileStream(pathname, FileMode.Open);
        // 处理文件中的数据 ...
    }
    catch (IOException) {
        // 在此添加 IOException 恢复的代码
    }
    finally {
        // 确保文件被关闭
        if (fs != null) fs.Close();
    }
}
```

在上述代码中，如果`try`块中的代码没有抛出异常，文件保证会被关闭。如果`try`块中的代码抛出异常，文件也保证会被关闭，无论该异常是否被捕捉到。将关闭文件的语句放在`finally` 块之后是不正确的，因为假若异常抛出但未捕捉到，该语句就执行不到，造成文件一直保持打开状态(直到下一次垃圾回收)。

`try` 块并不一定要关联 `finally` 块。 `try` 块的代码有时并不需要任何清理工作。但是，只要有 `finally` 块，它就必须出现在所有 `catch` 块之后，而且一个 `try` 块最多只能够关联一个 `finally` 块。

线程执行完 `finally` 块中的代码后，会执行紧跟在 `finally` 块之后的语句。记住，`finally` 块中的代码是清理代码，这些代码只需对 `try` 块中发起的操作进行清理。`catch` 和 `finally` 块中的代码应该非常短(通常只有一两行)，而且要有非常高的成功率，避免自己又抛出异常。

当然，(`catch`中的)异常恢复代码或(`finally` 中的)清理代码总是有可能失败并抛出异常的。但这个可能性不大。而且如果真的发生，通常意味着某个地方出了很严重的问题。很可能是某些状态在一个地方发生了损坏。即使`catch`或`finally`块内部抛出了异常也不是世界末日———— CLR 的异常机制仍会正常运转，好像异常是在`finally`块之后抛出的第一个异常，关于第一个异常的所有信息(例如堆栈跟踪)都将丢失
。这个新异常可能(而且极有可能)不会由你的代码处理，最终变成一个未处理的异常。在这种情况下，CLR 会终止你的进程。这是件好事情，因为损坏的所有状态现在都会被销毁。相较于让应用程序继续运行，造成不可预知的结果以及可能的安全漏洞，这样处理要好得多！

我个人认为，C# 团队应该为异常处理机制选择一套不同的语言关键字。程序员想做的是尝试(`try`)执行一些代码。如果发生错误，要么处理(`handle`)错误，以便从错误中恢复并继续；要么进行补偿(`compensate`)来撤消一些状态更改，并向调用者上报错误。程序员还希望确保清楚操作(`cleanup`)无论如何都会发生。左边的代码是目前 C# 编译器所支持的方法，右边的是我推荐的可读性更佳的方式：

![20_0](../resources/images/20_0.png)  

> CLS 和非 CLS异常  
> 所有面向 CLR 的编程语言都必须支持抛出从 `Exception` 派生的对象，因为公共语言规范(Common Language Specification, CLS)对此进行了硬性规定。但是，CLR 实际允许抛出任何类型的实例，而且有些编程语言允许代码抛出非 CLS 相容的异常对象，比如一个 `String`，`Int32` 和 `DateTime` 等。C# 编译器只允许代码抛出从 `Exception` 派生的对象，而用其他一些语言写的代码不仅允许抛出 `Exception`派生对象，还允许抛出非 `Exception` 派生对象。

> 许多程序员没有意识到 CLR 允许抛出任何对象来报告异常。大多数开发人员以为只有派生自 `Exception` 的对象才能抛出。在 CLR 的 2.0 版本之前，程序员写 `catch` 块来捕捉异常时，只能捕捉 CLS 相容的异常。如果一个 C# 方法调用了用另一种编程语言写的方法，而且那个方法抛出一个非 CLS 相容的异常，那么 C# 代码根本不能捕捉这个异常，从而造成一些安全隐患。

> 在 CLR 的 2.0 版本中，Microsoft 引入了新的 `RuntimeWrappedException` 类(在命名空间 `System.Runtime.CompilerServices` 中定义)。该类派生自 `Exception`，所以它是一个 CLS 相容的异常类型。`RuntimeWrappedException` 类含有一个 `Object` 类型的一个 CLS 相容的异常类型。`RuntimeWrappedException` 类含有一个 `Object` 类型的私有字段(可通过 `RuntimeWrappedException` 类的只读属性 `WrappedException` 来访问)。在 CLR 2.0 中，非 CLS 相容的一个异常被抛出时，CLR 会自动构造 `RuntimeWrappedException` 类的实例，并初始化该实例的私有字段，使之引用实际抛出的对象。这样 CLR 就将非 CLS 相容的异常转变成了 CLS 相容的异常。所以，任何能捕捉 `Exception` 类型的代码，现在都能捕捉非 CLS 相容的异常，从而消除了潜在的安全隐患。

> 虽然 C# 编译器只允许开发人员抛出派生自 `Exception` 的对象，但在 C#的 2.0 版本之前，C# 编译器确实允许开发人员使用以下形式的代码捕捉非 CLS 相容的异常：

```C#
private void SomeMethod() {
    try {
        // 需要得体地进行恢复和/或清理的代码放在这里
    }
    catch (Exception e) {
        // C# 2.0 以前，这个块只能捕捉 CLS 相容的异常：
        // 而现在，这个块能捕捉 CLS 相容和不相容的异常
        throw;   // 重新抛出捕捉到的任何东西
    }
    catch {
        // 在所有版本的 C# 中，这个块可以捕捉 CLS 相容和不相容的异常
        throw;  // 重新抛出捕捉到的任何东西
    }
}
```

> 现在，一些开发人员注意到 CLR 同时支持相容和不相容于 CLS 的异常，他们可能像上面展示的那样写两个 `catch` 块来捕捉这两种异常。为 CLR 2.0 或更高版本重新编译上述代码，第二个 `catch` 块永远执行不到，C#编译器显示以下警告消息：

> CS1058: 上一个 catch 子句已捕获所有异常。引发的所有非异常<sup>①</sup>均被包装在 `System.Runtime.CompilerServices.RuntimeWrappedException`中。

>> ① “非异常”其实就是“非`System.Excepion`派生的异常”。 ————译注

> 开发人员有两个办法迁移 .NET Framework 2.0 之前的代码。首先，两个 `catch` 块中的代码可以合并到一个 `catch` 块中，并删除其中的一个 `catch` 块中，并删除其中的一个 `catch` 块。这是推荐的办法。另外，还可以向 CLR 说明程序集中的代码想按照旧的规则行事。也就是说，告诉 CLR 你的 `catch(Exception)` 块不应捕捉新的 `RuntimeWrappedException` 类的一个实例。在这种情况下，CLR 不会将非 CLS 相容的对象包装到一个 `RuntimeWrappedException` 实例中，而且只有在你提供了一个没有指定任何类型的 `catch` 块时才调用你的代码。为了告诉 CLR 需要旧的行为，可向你的程序集应用 `RuntimeCompatibilityAttribute` 类的实例：

```C#
using System.Runtime.CompilerServices;
[assembly:RuntimeCompatibility(WrapNonExceptionThrows = false)]
```

> 注意，该特性影响的是整个程序集。在同一个程序集中，包装和不包装异常这两种处理方式不能同时存在。向包含旧代码的程序集(假如 CLR 不支持在其中包装异常)添加新代码(希望CLR 包装异常)时要特别小心。

## <a name="20_3">20.3 `System.Exception` 类</a>

CLR 允许异常抛出任何类型的实例 ———— 从 `Int32` 到 `String` 都可以。但是，Microsoft 决定不强迫所有编程语言都抛出和捕捉任意类型的异常。因此，他们定义了 `System.Exception` 类型，并规定所有 CLS 相容的编程语言都必须能抛出和捕捉派生自该类型的异常。派生自 `System.Exception` 的异常类型被认为是 CLS 相容的。C# 和其他许多语言的编译器都只允许抛出 CLS 相容的异常。

`System.Exception` 是一个很简单的类型，表 20-1 描述了它包含的属性。但一般不要写任何代码以任何方式查询或访问这些属性。相反，当应用程序因为未处理的异常而终止时，可以在调试器中查看这些属性，或者在 Windows 应用程序事件日志或崩溃转储(crash dump)中查看。

表 20-1 `System.Exception` 类型的公共属性
|属性名称|访问|类型|说明|
|:---:|:---:|:---:|:---:|
|`Message`|只读|`String`|包含辅助性文字说明，指出抛出异常的原因。如果抛出的异常未处理，该消息通常被写入日志。由于最终用户一般不看这种消息，所以消息应提供尽可能多的技术细节，方便开发人员在生成新版本程序集时，利用消息所提供的信息来修正代码|
|`Data`|只读|`IDictionary`|引用一个“键/值对”集合。通常，代码在抛出异常前在该集合中添加记录项；捕捉异常的代码可在异常恢复过程中查询记录项；捕捉异常的代码可在异常恢复过程中查询记录项并利用其中的信息|
|`Source`|读/写|`String`|包含生成异常的程序集的名称|
|`StackTrace`|只读|`String`|包含抛出异常之前调用过的所有方法的名称和签名，该属性对调试很有用|
|`TargetSite`|只读|`MethodBase`|包含抛出异常的方法|
|`HelpLink`|只读|`String`|包含帮助用户理解异常的一个文档的 URL(例如 file://C:\MyApp\Help.htm#MyExceptionHelp)。但要注意，健全的编程和安全实践阻止用户查看原始的未处理的异常。因此，除非希望将信息传达给其他程序员，否则不要使用该属性|
|`InnerException`|只读|`Exception`|如果当前异常是在处理一个异常时抛出的，该属性就指出上一个异常是什么。这个只读属性通常为 `null`。`Exception`类型还提供了公共方法`GetBaseException`来遍历由内层异常构成的链表，并返回最初抛出的异常|
|`HResult`|读/写|`Int32`|跨越托管和本机代码边界时使用的一个 32 位值。例如，当 COM API 返回代表失败的 `HRESULT` 值时，CLR 抛出一个 `Exception` 派生对象，并通过该属性来维护 `HRESULT` 值|

这里有必要讲一下 `System.Exception` 类型提供的只读 `StackTrace` 属性。`catch` 块可读取该属性来获取一个堆栈跟踪(stack trace)，它描述了异常发生前调用了哪些方法。检查异常原因并改正代码时，这些信息是很有用的。访问该属性实际会调用 CLR 中的代码；该属性并不是简单地返回一个字符串。构造 `Exception` 派生类型的新对象时，`StackTrace` 属性被初始化为 `null`。如果此时读取该属性，得到的不是堆栈跟踪，而是一个 `null`。

一个异常抛出时，CLR 在内部记录 `throw` 指令的位置(抛出位置)。一个 `catch` 块捕捉到该异常时，CLR 记录捕捉位置。在 `catch` 块内访问被抛出的异常对象的 `StackTrace` 属性，负责实现该属性的代码会调用 CLR 内部的代码，后者创建一个字符串来指出从异常抛出位置到异常捕捉位置的所有方法。

> 重要提示 抛出异常时，CLR 会重置异常起点；也就是说，CLR 只记录最新的异常对象的抛出位置。

以下代码抛出它捕捉到的相同的异常对象，导致 CLR 重置该异常的起点：

```C#
private void SomeMethod() {
    try { ... }
    catch (Exception e) {
        ...
        throw e;        // CLR 认为这是异常的起点， FxCop 报错
    }
}
```

但如果仅仅使用 `throw` 关键字本身(删除后面的 `e`)来重新抛出异常对象，CLR 就不会重置堆栈的起点。以下代码重新抛出它捕捉到的异常，但不会导致 CLR 重置起点：

```C#
private void SomeMethod() {
    try { ... }
    catch (Exception e) {
        ...
        throw;    // 不影响 CLR 对异常起点的认知。 FxCop 不再报错
    }
}
```

实际上，两段代码唯一的区别就是 CLR 对于异常起始抛出位置的认知。遗憾的是，不管抛出还是重新抛出异常，Windows 都会重置栈的起点。因此，如果一个异常成为未处理的异常，那么向 Windows Error Reporting 报告的栈位置就是最后一次抛出或重新抛出的位置(即使 CLR 知道异常的原始抛出位置)。之所以遗憾，是因为假如应用程序在字段那里失败，会使调试工作变得异常困难。有些开发人员无法忍受这一点，于是选择以一种不同的方式实现代码，确保堆栈跟踪能真正反映异常的原始抛出位置：

```C#
private void SomeMethod() {
    Boolean trySucceeds = false;

    try {
        ...
        trySucceeds = true;
    }
    finally {
        if (!trySucceeds) { /*捕捉代码放到这里*/ }
    }
}
```

`StackTrace` 属性返回的字符串不包含调用栈中比较受异常对象的那个 `catch` 块高的任何方法<sup>①</sup>。要获得从线程起始处到异常处理程序(`catch` 块)之间的完整堆栈跟踪，需要使用 `System.Diagnostics.StackTrace` 类型。该类型定义了一些属性和方法，允许开发人员程序化地处理堆栈跟踪以及构成堆栈跟踪的栈桢<sup>②</sup>。

> ① 栈顶移动即“升高”，向栈底移动即“降低”。 —— 译注

> ② 栈桢(stack frame)代表当前线程的调用栈中的一个方法调用。执行线程的过程中进行的每个方法调用都会在调用栈中创建并压入一个 `StackFrame`。 ——译注

可用几个不同的构造器来构造一个 `StackTrace` 对象。一些构造器构造从线程起始处到 `StackTrace` 对象的构造位置的栈桢。另一些使用作为参数传递的一个 `Exception` 派生对象来初始化栈桢。

如果 CLR 能找到你的程序集的调试符号(存储在.pdb 文件中)，那么在`System.Exception`的 `StackTrace` 属性或者 `System.Diagnostics.StackTrace` 的 `ToString` 方法返回的字符串中，将包括源代码文件路径和代码行号，这些信息对于调试是很有用的。

获得堆栈跟踪后，可能发现实际调用栈中的一些方法没有出现在堆栈跟踪字符串中。这可能有两方面的原因。首先，调用栈记录的是线程的返回位置(而非来源位置)。其次， JIT 编译器可能进行了优化，将一些方法内联(inline)，以避免调用单独的方法并从中返回的开销。许多编译器(包括 C#编译器)都支持`/debug`命令行开关。使用这个开关，编译器会在生成的程序集中嵌入信息，告诉 JIT 编译器不要内联程序集的任何方法，确保调试人员获得更完整、更有意义的堆栈跟踪。

> 注意 JIT 编译器会检查应用于程序集的 `System.Diagnostics.Debuggabletrribute` 定制特性。C# 编译器会自动应用该特性。如果该特性指定了 `DisableOptimizations` 标志，JIT 编译器就不会对程序集的方法进行内联。使用 C# 编译器的 `/debug` 开关就会设置这个标志。另外，向方法应用定制特性 `System.Runtime.CompilerServices.MethodImplAttribute` 将禁止 JIT 编译器在调试和发布生成(debug and release build)时对该方法进行内联处理，以下方法定义示范了如何禁止方法内联：

```C#
using System;
using System.Runtime.CompilerServices;

internal sealed class SomeType {
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SomeMethod() {
        ...
    }
}
```

## <a name="20_4">20.4 FCL 定义的异常类</a>

FCL 定义了许多异常类型(它们最终都从 `System.Exception` 类型派生)。以下层次结构展示了 MSCorLib.dll 程序集中定义的异常类型；其他程序集还定义的异常类型。用于获得这个层次结构的应用程序请参见 23.3.3 节“构建`Exception`派生类型的层次结构”。

```C#
System.Exception
 System.AggregateException
 System.ApplicationException
 System.Reflection.InvalidFilterCriteriaException
 System.Reflection.TargetException
 System.Reflection.TargetInvocationException
 System.Reflection.TargetParameterCountException
 System.Threading.WaitHandleCannotBeOpenedException
 System.Diagnostics.Tracing.EventSourceException
 System.InvalidTimeZoneException
 System.IO.IsolatedStorage.IsolatedStorageException
 System.Threading.LockRecursionException
 System.Runtime.CompilerServices.RuntimeWrappedException
 System.SystemException
 System.Threading.AbandonedMutexException
 System.AccessViolationException
 System.Reflection.AmbiguousMatchException
 System.AppDomainUnloadedException
 System.ArgumentException
 System.ArgumentNullException
 System.ArgumentOutOfRangeException
 System.Globalization.CultureNotFoundException
 System.Text.DecoderFallbackException
 System.DuplicateWaitObjectException
 System.Text.EncoderFallbackException
 System.ArithmeticException
 System.DivideByZeroException
 System.NotFiniteNumberException
 System.OverflowException
 System.ArrayTypeMismatchException
 System.BadImageFormatException
 System.CannotUnloadAppDomainException
 System.ContextMarshalException
 System.Security.Cryptography.CryptographicException
 System.Security.Cryptography.CryptographicUnexpectedOperationException
 System.DataMisalignedException
 System.ExecutionEngineException
 System.Runtime.InteropServices.ExternalException
 System.Runtime.InteropServices.COMException
 System.Runtime.InteropServices.SEHException
 System.FormatException
 System.Reflection.CustomAttributeFormatException
 System.Security.HostProtectionException
 System.Security.Principal.IdentityNotMappedException
 System.IndexOutOfRangeException 
 System.InsufficientExecutionStackException
 System.InvalidCastException
 System.Runtime.InteropServices.InvalidComObjectException
 System.Runtime.InteropServices.InvalidOleVariantTypeException
 System.InvalidOperationException
 System.ObjectDisposedException
 System.InvalidProgramException
 System.IO.IOException
 System.IO.DirectoryNotFoundException
 System.IO.DriveNotFoundException
 System.IO.EndOfStreamException
 System.IO.FileLoadException
 System.IO.FileNotFoundException
 System.IO.PathTooLongException
 System.Collections.Generic.KeyNotFoundException
 System.Runtime.InteropServices.MarshalDirectiveException
 System.MemberAccessException
 System.FieldAccessException
 System.MethodAccessException
 System.MissingMemberException
 System.MissingFieldException
 System.MissingMethodException
 System.Resources.MissingManifestResourceException
 System.Resources.MissingSatelliteAssemblyException
 System.MulticastNotSupportedException
 System.NotImplementedException
 System.NotSupportedException
 System.PlatformNotSupportedException
 System.NullReferenceException
 System.OperationCanceledException
 System.Threading.Tasks.TaskCanceledException
 System.OutOfMemoryException
 System.InsufficientMemoryException
 System.Security.Policy.PolicyException
 System.RankException
 System.Reflection.ReflectionTypeLoadException
 System.Runtime.Remoting.RemotingException
 System.Runtime.Remoting.RemotingTimeoutException
 System.Runtime.InteropServices.SafeArrayRankMismatchException
 System.Runtime.InteropServices.SafeArrayTypeMismatchException
 System.Security.SecurityException
 System.Threading.SemaphoreFullException
 System.Runtime.Serialization.SerializationException
 System.Runtime.Remoting.ServerException
 System.StackOverflowException
 System.Threading.SynchronizationLockException
 System.Threading.ThreadAbortException
 System.Threading.ThreadInterruptedException
 System.Threading.ThreadStartException
 System.Threading.ThreadStateException
 System.TimeoutException
 System.TypeInitializationException
 System.TypeLoadException
 System.DllNotFoundException
 System.EntryPointNotFoundException
 System.TypeAccessException
 System.TypeUnloadedException
 System.UnauthorizedAccessException
 System.Security.AccessControl.PrivilegeNotHeldException
 System.Security.VerificationException
 System.Security.XmlSyntaxException
 System.Threading.Tasks.TaskSchedulerException
 System.TimeZoneNotFoundException
```

Microsoft 本来是打算将 `System.Exception` 类型作为所有异常的基类型，而另外两个类型 `System.SystemException` 和 `System.ApplicationException` 是唯一直接从 `Exception` 派生的类型。另外，CLR 抛出的所有异常都从 `SystemException`派生，应用程序抛出的所有异常都从 `ApplicationException` 派生。这样就可以写一个 `catch` 块来捕捉 CLR 抛出的所有异常或者应用程序抛出的所有异常。

但是，正如你看到的那样，规则没有得到严格遵守。有的异常类型直接从 `Exception` 派生(`IsolatedStorageException`)；CLR 抛出的一些异常从 `ApplicationException` 派生 (`TargetInvocationException`)；而应用程序抛出的一些异常从 `SystemException` 派生(`FormatException`)。这根本就是一团糟。结果是 `SystemException` 类型和 `ApplicationException` 类型根本没什么特殊含义。Microsoft 本该及时将它们从异常类的层次结果中移除，但现在已经不能那样做了，因为会破坏现有的代码对这两个类型的引用。

## <a name="20_5">20.5 抛出异常</a>

实现自己的方法时，如果方法无法完成方法名所指明的任务，就应抛出一个异常。抛出异常时要考虑两个问题。

第一个问题是抛出什么 `Exception` 派生类型。应选择一个有意义的类型。要考虑调用栈中位于高处的代码，要知道那些代码如何判断一个方法失败从而执行得体的恢复代码。可直接使用 FCL 定义好的类型，但在 FCL 中也许找不到和你想表达的意思完全匹配的类型。所以可能需要定义自己的类型，只要它最终从 `System.Exception` 派生就好。

强烈建议定义浅而宽的异常类型层次结构<sup>①</sup>，以创建尽量少的基类。原因是基类的主要作用就是将大量错误当作一个错误，而这通常是危险的。基于同样的考虑，永远都不要抛出一个`System.Exception` 对象<sup>②</sup>，抛出其他任何基类异常类型时也要特别谨慎。

> ① ![20_00](../resources/images/20_00.png)= 浅而宽； ![20_000](../resources/images/20_000.png)= 深而窄。 ——— 译注

> ② 事实上，Microsoft 本来就应该将 `System.Exception` 类标记为 `abstract`，在编译时就禁止代码试图抛出它(的实例)。

> 重要提示 还要考虑版本问题。如果定义从现有异常类型派生的一个新异常类型，捕捉现有基类型的所有代码也能捕捉新类型。这有时可能正好是你期望的，但有时也可能不是，具体取决于捕捉基类的代码以什么样的方式响应异常类型及其派生类型。从未预料到会有新异常的代码现在可能出现非预期的行为，并可能留下安全隐患。而定义新异常类型的人一般不知道基异常的所有捕捉位置以及具体处理方式。所以这里事实不可能做出面面俱到的决定。

第二个问题是向异常类型的构造器传递什么字符串消息。抛出异常时应包含一条字符串消息，详细说明方法为什么无法完成任务。如果异常被捕捉到并进行了处理，用户就看不到该字符串消息。但是，如果成为未处理的异常，消息通常会被写入日志。未处理的异常意味着应用程序存在真正的bug，开发人员必须修复该 bug。最终用户没有源代码或能力去修复 bug 并重新编译程序。事实上，这个字符串消息根本不应该向最终用户显示，所以，字符串消息可以包含非常详细的技术细节，以帮助开发人员修正代码。

另外，由于所有开发人员都不得不讲英语(至少要会一点，因为编程语言和 FCL 类/方法都使用英语)，所以通常不必本地化异常字符串消息。但如果要构建由非英语开发人员使用的类库，就可能需要本地化字符串消息。Microsoft 已本地化了 FCL 抛出的异常消息，因为全世界的开发人员都要用这个类库。

## <a name="20_6">20.6 定义自己的异常类</a>

遗憾的是，设计自己的异常不仅繁琐，还容易出错。主要原因是从  `Exception` 派生的所有类型都应该是可序列化的(serializable)，使它们能穿越 AppDomain 边界或者写入日志/数据库。序列化涉及许多问题，详情将在第24章“运行时序列化”讲述。所以，为了简化编码，我写了一个自己的泛型 `Exception<TExceptionArgs>`类，它像下面这样定义：

```C#
[Serializable]
public sealed class Exception<TExceptionArgs> : Exception, ISerializable where TExceptionArgs : ExceptionArgs {

    private const String c_args = "Args";       // 用于(反)序列化
    private readonly TExceptionArgs m_args;

    public TExceptionArgs Args { get { return m_args; } }

    public Exception(String message = null, Exception innerException = null) 
        : this(null, message, innerException) { }

    public Exception(TExceptionArgs args, String message = null, 
      Exception innerException = null) : base(message, innerException) {
        m_args = args;
    }

    // 这个构造器用于反序列化：由于类是密封的，所以构造器是私有的.
    // 如果这个类不是密封的，这个构造器就应该是受保护的.
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    private Exception(SerializationInfo info, StreamingContext context) : base(info, context) {
        m_args = (TExceptionArgs)info.GetValue(c_args, typeof(TExceptionArgs));
    }

    // 这个方法用于序列化：由于 ISerializable 接口，所以它是公共的
    [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
    public override void GetObjectData(SerializationInfo info, StreamingContext context) {
        info.AddValue(c_args, m_args);
        base.GetObjectData(info, context);
    }

    public override string Message {
        get  {
            String baseMsg = base.Message;
            return (m_args == null) ? baseMsg : baseMsg + " (" + m_args.Message + ")";
        }
    }

    public override Boolean Equals(Object obj) {
        Exception<TExceptionArgs> other = obj as Exception<TExceptionArgs>;
        if (other == null) return false;
        return Object.Equals(m_args, other.m_args) && base.Equals(obj);
    }
    public override int GetHashCode() { return base.GetHashCode(); }
}
```

`TExceptionArgs`约束为的`ExceptionArgs`基类非常简单，它看起来像下面这样：

```C#
[Serializable]
public abstract class ExceptionArgs {
    public virtual String Message { get { return String.Empty; } }
}
```

定义好这两个类之后，定义其他异常类就是小事一桩。要定义代表磁盘满的异常类，可以像下面这样写：

```C#
[Serializable]
public sealed class DiskFullExceptionArgs : ExceptionArgs {
    private readonly String m_diskpath;     // 在构造时设置的私有字段

    public DiskFullExceptionArgs(String diskpath) { m_diskpath = diskpath; }

    // 返回字段的公共只读属性
    public String DiskPath { get { return m_diskpath; } }

    // 重写 Message 属性来包含我们的字段(如果设置了的话)
    public override String Message {
        get {
            return (m_diskpath == null) ? base.Message : "DiskPath=" + m_diskpath;
        }
    }
}
```

另外如果没有额外的数据要包含到类中，可以简单地写成：

```C#
[Serializable]
public sealed class DiskFullExceptionArgs : ExceptionArgs { }
```

现在，可以像下面这样写来抛出并捕捉这样的一个异常：

```C#
public static void TestException() {
    try {
        throw new Exception<DiskFullExceptionArgs>(new DiskFullExceptionArgs(@"C:\"), "The disk is full");
    }
    catch (Exception<DiskFullExceptionArgs> e) {
        Console.WriteLine(e.Message);
    }
}
```

> 注意 我的 `Exception<TExceptionArgs>` 类有两个问题需要注意。第一个问题是，用它定义的任何异常类型都总是派生自 `System.Exception`。这在大多数时候都不是问题，而且浅而宽的异常类型层次结构还是一件好事。第二个问题是，Visual Studio 的未处理异常对话框不会显示 `Exception<T>`类型的泛型类型参数，如下图所示。

![20_0_0](../resources/images/20_0_0.png)  

## <a name="20_7">20.7 用可靠性换取开发效率</a>

我从 1975 年开始写软件。首先是进行大量 BASIC 编程。随着我对硬件的兴趣日增，又转向汇编语言。随着时间的推移，我开始转向 C 语言，因为它允许我从更高的抽象层访问硬件，使编程变得更容易。我的资历是写操作系统代码和平台/库代码，所以我总是努力使自己的代码尽量小而快。应用程序写得再好，也不会强过它们赖以生存的操作系统和库吧?

除了创建小而快的代码，我还总是关注错误恢复。分配内存时(使用 C++ 的 `new` 操作符或调用 `malloc`，`HeapAlloc`，`VirtualAlloc` 等)，我总是检查返回值，确保我请求的内存真的给了我。另外，如果内存请求失败，我总是提供一个备选的代码路径，确保剩余的程序状态不会受影响。而且让我的所有调用者都知道我失败了，使调用代码也能采取正确的补救措施。

出于某些我不好解释的原因，为 .NET Framework 写代码时，我没有做到这种对细节的关注。“内存耗尽”总是可能发生的，但我几乎没看到过任何代码包含从 `OutOfMemoryException` 恢复的 `catch` 块。事实上，甚至有的开发人员告诉我 CLR 不让程序捕捉 `OutOfMemoryException`。我在此要郑重声明，绝对不是这样的；你可以捕捉这个异常。事实上，执行托管代码时，有太多的错误都可能发生，但我很少看到开发人员写代码尝试从这些潜在的错误中恢复。本节要指出其中的一些潜在的错误，并解释为什么可以合理地忽略它们。我还要指出忽略了这些错误之后，可能造成什么重大的问题，并推荐了有助于缓解这些问题的一些方式。

面向对象编程极大提升了开发人员的开发效率。开发效率的提升有很大一部分来自可组合性(composability)，它使代码很容易编写、阅读和维护。例如下面这行代码：

`Boolean f = "Jeff".Substring(1, 1).ToUpper().EndsWith("E");`

但上述代码有一个很重要的前提：没有错误发生。而错误总是可能发生的。所以，我们需要一种方式处理错误。这正是异常处理构造<sup>①</sup>和机制的目的，我们不能像 Win32 和 COM 函数那样返回 `true/false` 或者一个 `HRESULT` 来指出成功/失败。

> ① `try-catch-finally` 就是 C# 的异常处理 ”构造“。 ——译注

除了代码的可组合性，开发效率的提升还来自编译器提供的各种好用的功能。例如，编译器能隐式地做下面这些事情。

* 调用方法时插入可选参数。

* 对值类型的实例进行装箱。

* 构造/初始化参数数组。

* 绑定到 `dynamic` 变量/表达式的成员。

* 绑定到扩展方法。

* 绑定/调用重载的操作符(方法)。

* 构造委托对象。

* 在调用泛型方法、声明局部变量和使用 lambda 表达式时推断类型。

* 为 lambda 表达式和迭代器定义/构造闭包类<sup>①</sup>。

> ① 闭包(closure)是由编译器生成的数据结构(一个 C# 类)，其中包含一个表达式以及对表达式进行求值所需的变量(C# 的公共字段)。变量允许在不改变表达式签名的前提下，将数据从表达式的一次调用传递到下一次调用。————译注

* 定义/构造/初始化匿名类型及其实例。

* 重写代码来支持 LINQ 查询表达式和表达式树。

另外，CLR 本身也会提供大量辅助来进一步简化编程。例如，CLR 会隐式做下面这些事情。

* 调用虚方法和接口方法。

* 加载程序集并对方法进行 JIT 编译，可能抛出以下异常：`FileLoadException`，`BadImageFormatException`，`InvalidProgramException`，`FieldAccessException`，`MethodAccessException`，`MissingFieldException`，`MissingMethodException` 和 `VerificationException`。

* 访问 `MarshalByRefObject` 派生类型的对象时穿越 AppDomain 边界(可能抛出 `AppDomainUnloadedException`)。

* 穿越 AppDomain 边界时序列化和反序列化对象。

* 调用 `Thread.Abort` 或 `AppDomain.Unload` 时造成线程抛出 `ThreadAbortException`。

* 垃圾回收之后，在回收对象的内存之前调用 `Finalize` 方法。

* 使用泛型类型时，在 Loader 堆中创建类型对象<sup>②</sup>。

> ② 每个 AppDomain 都有一个自己的托管堆，这个托管堆内部又按照功能进行了不同的划分，其中最重要的就是 GC 堆和 Loader 堆，前者存储引用类型的实例，也就是会被垃圾回收机制”照顾“到的东西。而 Loader 堆负责存储类型的元数据，也就是所谓的“类型对象”。在每个“类型对象”的末尾，都含有一个“方法表”。详情参见 22.2 节和图 22-1。 ———— 译注

* 调用类型的静态构造器<sup>③</sup>(可能抛出 `TypeInitializationException`)。

> ③ 也称为类型构造器，详情参见 8.3 节 “类型构造器”。 ————译注

* 抛出各种异常，包括 `OutOfMemoryException`，`DivideByZeroException`，`NullReferenceException`，`RuntimeWrappedException`，`TargetInvocationException`，`OverflowException`，`NotFiniteNumberException`，`ArrayTypeMismatchException`，`DataMisalignedException`，`IndexOutOfRangeException`，`InvalidCastException`，`RankException`，`SecurityException`等。

另外，理所当然地，.NET Framework 配套提供了一个包罗万象的类库，其中有无数的类型，每个类型都封装了常用的、可重用的功能。可利用这些类型构建 Web 窗体应用程序、 Web 服务和富 GUI 应用程序，可以处理安全性、图像和语音识别等。所有这些代码都可能抛出代表某个地方出错的异常。另外，未来的版本可能引入从现有异常类型派生的新异常类型，而你的 `catch` 块能捕捉未来才会出现的异常类型。

所有这一切————面向对象编程、编译器功能、CLR 功能以及庞大的类库 ——— 使 .NET Framework 成为颇具吸引力的软件开发平台<sup>①</sup>。但我的观点是，所有这些东西都会在代码中引入你没什么控制权的“错误点”(point of failure)。如果所有东西都正确无误地运行，那么一切都很好：可以方便地编写代码，写出来的代码也很容易阅读和维护。但一旦某样东西出了问题，就几乎不可能完全理解哪里出错和为什么出错。下面这个例子可以证明我的观点：

```C#
private static Object OneStatment(Stream stream, Char charToFind) {
    return (charToFind + ": " + stream.GetType() + String.Empty + (strram.Position + 512M)).Where(c=>c == charToFind).ToArray();
}
```

> ① 应该补充的是，Visual Studio 的编辑器、智能感知支持、代码段(code snippet)支持、模板、可扩展系统、调试系统以及其他多种工具也增大了平台对于开发人员的吸引力。但之所以把这些放在讨论主线以外，是因为它们对代码运行时的行为没有任影响。

这个不太自然的方法只包含一个 C#语句，但该语句做了大量工作。下面是 C#编译器为这个方法生成的 IL 代码(一些行加粗并倾斜；由于一些隐式的操作，它们成了潜在的 “错误点”)：

```C#
.method private hidebysig static object OneStatement(
 class [mscorlib]System.IO.Stream stream, char charToFind) cil managed {
 .maxstack 4
 .locals init (
 [0] class Program/<>c__DisplayClass1 V_0,
 [1] object[] V_1)
 L_0000: newobj instance void Program/<>c__DisplayClass1::.ctor()
 L_0005: stloc.0
 L_0006: ldloc.0
 L_0007: ldarg.1
 L_0008: stfld char Program/<>c__DisplayClass1::charToFind
 L_000d: ldc.i4.5
 L_000e: newarr [mscorlib]System.Object
 L_0013: stloc.1
 L_0014: ldloc.1
 L_0015: ldc.i4.0
 L_0016: ldloc.0
 L_0017: ldfld char Program/<>c__DisplayClass1::charToFind
 L_001c: box [mscorlib]System.Char
 L_0021: stelem.ref 
 L_0022: ldloc.1
 L_0023: ldc.i4.1
 L_0024: ldstr ": "
 L_0029: stelem.ref
 L_002a: ldloc.1
 L_002b: ldc.i4.2
 L_002c: ldarg.0
 L_002d: callvirt instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
 L_0032: stelem.ref
 L_0033: ldloc.1
 L_0034: ldc.i4.3
 L_0035: ldsfld string [mscorlib]System.String::Empty
 L_003a: stelem.ref
 L_003b: ldloc.1
 L_003c: ldc.i4.4
 L_003d: ldc.i4 0x200
 L_0042: newobj instance void [mscorlib]System.Decimal::.ctor(int32)
 L_0047: ldarg.0
 L_0048: callvirt instance int64 [mscorlib]System.IO.Stream::get_Position()
 L_004d: call valuetype [mscorlib]System.Decimal
         [mscorlib]System.Decimal::op_Implicit(int64)
 L_0052: call valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::op_Addition
         (valuetype [mscorlib]System.Decimal, valuetype [mscorlib]System.Decimal)
 L_0057: box [mscorlib]System.Decimal
 L_005c: stelem.ref
 L_005d: ldloc.1
 L_005e: call string [mscorlib]System.String::Concat(object[])
 L_0063: ldloc.0
 L_0064: ldftn instance bool Program/<>c__DisplayClass1::<OneStatement>b__0(char)
 L_006a: newobj instance
         void [mscorlib]System.Func`2<char, bool>::.ctor(object, native int)
 L_006f: call class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>
         [System.Core]System.Linq.Enumerable::Where<char>(
         class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>,
         class [mscorlib]System.Func`2<!!0, bool>)
 L_0074: call !!0[] [System.Core]System.Linq.Enumerable::ToArray<char>
         (class [mscorlib]System.Collections.Generic.IEnumerable`1<!!0>)
 L_0079: ret
} 
```

由此可见，构造`<>c__DisplayClass1`类(编译器生成的类型)、`Object[]`数组和`Func`委托，以及对`char`和`Decimal`进行装箱时，可能抛出一个`OutOfMemoryException`。调用`Concat`，`Where` 和 `ToArray`时，也会在内部分配内存。构造 `Decimal` 实例时，可能造成它的类型构造器被调用，并抛出一个 `TypeInitializationException`<sup>①</sup>。还存在对 `Decimal` 的 `op_Implicit` 操作符和 `op_Addition` 操作符方法的隐式调用，这些方法可能抛出一个 `OverflowException`。

> ① 顺便说一句，`System.Char`，`System.String`，`System.Type` 和 `System.IO.Stream` 都定义了类构造器，它们全部都有可能造成在这个应用程序的某个位置抛出一个 `TypeInitializationException`

`Stream` 的 `Position` 属性比较有趣。首先，它是一个虚属性，所以我的 `OneStatement` 方法无法知道实际执行的代码，可能抛出任何异常。其次，`Stream` 从 `MarshalByRefObject` 派生，所以 `stream` 实参可能引用一个代理对象，后者又引用另一个 AppDomain 中的对象。而另一个 AppDomain 可能已经卸载，造成一个 `AppDomainUnloadedException`。

当然，调用的所有方法都是我个人无法控制的，它们都由 Microsoft 创建。Microsoft 将来还可能更改它们的实现，抛出我写 `OneStatement` 方法时不可能预料到的新异常类型。所以，我怎么可能写这个 `OneStatement` 方法来获得完全的“健壮性”来防范所有可能的错误呢？顺便说一句，反过来也存在问题：`catch` 块可捕捉指定异常类型的派生类型，所以是在为一种不同的错误执行恢复代码。

对所有可能的错误有了一个基本认识之后，就能理解为何不去追求完全健壮和可靠的代码了：因为不切实际(更极端的说法是根本不可能)。不去追求完全的健壮性和可靠性，另一个原因是错误不经常发生。由于错误(比如`OutOfMemoryException`)极其罕见，所以开发人员决定不去追求完全可靠的代码，牺牲一定的可靠性来换取程序员开发效率的提升。

异常的好处在于，未处理的异常会造成应用程序终止。之所以是好事，是因为可在测试期间提早发现问题。利用由未处理异常提供的信息(错误消息和堆栈跟踪)，通常足以完成对代码的修正。当然，许多公司不希望应用程序在测试和部署之后还发生意外终止的情况，所以会插入代码来捕捉 `System.Exception`，也就是所有异常类型的基类。但如果捕捉 `System.Exception` 并允许应用程序继续运行，一个很大的问题是状态可能遭受破坏。

本章早些时候展示了一个 `Account` 类，它定义了一个 `Transfer` 方法，用于将钱从一个账户转移到另一个。这个 `Transfer` 方法调用时，如果成功将钱从 `from` 账户扣除，但在将钱添加到 `to` 账户之前抛出异常，那么会发生什么？如果调用代码(调用这个方法的代码)捕捉 `System.Exception` 并继续进行，应用程序的状态的破坏：`from` 和 `to` 账户的钱都会错误地变少。由于涉及到金钱，所以这种对状态的破坏不能被视为简单 bug，而应被看成是一个安全性 bug。应用程序继续进行，会尝试对大量账户执行更多的转账操作，造成状态破坏大量蔓延。

一些人会说，`Transfer` 方法本身应该捕捉 `System.Exception` 并将钱还给 `from` 账户。如果 `Transfer` 方法很简单，这个方案确实可行。但如果 `Transfer` 方法还要确实可行。但如果 `Transfer` 方法还要生成关于取钱的审计记录，或者其他线程要同时操作同一个账户，那么撤销(undo)操作本身就可能失败，造成抛出其他异常。现在，状态破坏将变得更糟而非更好。