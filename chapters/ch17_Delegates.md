# 第 17 章 委托

本章内容：

* <a href="#17_1">初始委托</a>
* <a href="#17_2">用委托回调静态方法</a>
* <a href="#17_3">用委托回调实例方法</a>
* <a href="#17_4">委托揭秘</a>
* <a href="#17_5">用委托回调许多方法(委托链)</a>
* <a href="#17_6">委托定义不要太多(泛型委托)</a>
* <a href="#17_7">C# 为委托提供的简化语法</a>
* <a href="#17_8">委托和反射</a>

本章要讨论回调函数。回调函数式一种非常有用的编程机制，它的存在已经有很多年了。Microsoft .NET Framework 通过 `委托`来提供回调函数机制。不同于其他平台(比如非托管C++)的回调机制，委托的功能要多得多。例如，委托确保回调方法是类型安全的(这是 CLR 最重要的目标之一)。委托还允许顺序调用多个方法，并支持调用静态方法和实例方法。

## <a name="17_1">17.1 初始委托</a>

C “运行时”的 `qsort` 函数获取指向一个回调函数的指针，以便对数组中的元素进行排序。在 Microsoft Windows 中，窗口过程、钩子过程和异步过程调用等都需要回调函数。在 .NET Framework 中，回调方法的应用更是广泛。例如，可以登记回调方法来获得各种各样的通知，例如未处理的异常、窗口状态变化、菜单项选择、文件系统变化、窗体控件事件和异步操作已完成等。

在非托管 C/C++ 中，非成员函数的地址只是一个内存地址。这个地址不携带任何额外的信息，比如函数期望收到的参数个数、参数类型、函数返回值类型以及函数的调用协定。简单地说，非托管 C/C++ 回调函数不是类型安全的(不过它们确实是一种非常轻量级的机制)。

.NET Framework 的回调函数和非托管 Windows 编程环境的回调函数一样有用，一样普遍。但是，.NET Framework 提供了称为**委托**的类型安全机制。为了理解委托，先来看看如何使用它。以下代码<sup>①</sup>演示了如何声明、创建和使用委托：

> ① 这个程序最好不要通过在 Visual Studio 中新建 “Windows 窗体应用程序”项目来生成。用文本编辑器输入代码，另存为 *name.cs*。启动“VS2013 开发人员命令提示”，输入 `csc name.cs` 生成，输入 `name` 执行。这样可同时看到控制台和消息框的输出。 —— 译注

```C#
using System;
using System.Windows.Forms;
using System.IO;

// 声明一个委托类型，它的实例引用一个方法，
// 该方法获取一个 Int32 参数，返回 void
internal delegate void Feedback(Int32 value);
public sealed class Program {
    public static void Main() {
        StaticDelegateDemo();
        InstanceDelegateDemo();
        ChainDelegateDemo1(new Program());
        ChainDelegateDemo2(new Program());
    }

    private static void StaticDelegateDemo() {
        Console.WriteLine("----- Static Delegate Demo -----");
        Counter(1, 3, null);
        Counter(1, 3, new Feedback(Program.FeedbackToConsole));
        Counter(1, 3, new Feedback(FeedbackToMsgBox));          // 前缀 "Program." 可选
        Console.WriteLine();
    }

    private static void InstanceDelegateDemo() {
        Console.WriteLine("----- Instance Delegate Demo -----");
        Program p = new Program();
        Counter(1, 3, new Feedback(p.FeedbackToFile));
        Console.WriteLine();
    }

    private static void ChainDelegateDemo1(Program p) {
        Console.WriteLine("----- Chain Delegate Demo 1 -----");
        Feedback fb1 = new Feedback(FeedbackToConsole);
        Feedback fb2 = new Feedback(FeedbackToMsgBox);
        Feedback fb3 = new Feedback(p.FeedbackToFile);

        Feedback fbChain = null;
        fbChain = (Feedback) Delegate.Combine(fbChain, fb1);
        fbChain = (Feedback) Delegate.Combine(fbChain, fb2);
        fbChain = (Feedback) Delegate.Combine(fbChain, fb3);
        Counter(1, 2, fbChain);

        Console.WriteLine();
        fbChain = (Feedback)
            Delegate.Remove(fbChain, new Feedback(FeedbackToMsgBox));
        Counter(1, 2, fbChain);
    }

    private static void ChainDelegateDemo2(Program p) {
        Console.WriteLine("----- Chain Delegate Demo 2 -----");
        Feedback fb1 = new Feedback(FeedbackToConsole);
        Feedback fb2 = new Feedback(FeedbackToMsgBox);
        Feedback fb3 = new Feedback(p.FeedbackToFile);

        Feedback fbChain = null;
        fbChain += fb1;
        fbChain += fb2;
        fbChain += fb3;
        Counter(1, 2, fbChain);

        Console.WriteLine();
        fbChain -= new Feedback(FeedbackToMsgBox);
        Counter(1, 2, fbChain);
    }

    private static void Counter(Int32 from, Int32 to, Feedback fb) {
        for (Int32 val = from; val <= to; val++) {
            // 如果指定了任何回调，就调用它们
            if (fb != null)
                fb(val);
        }
    }

    private static void FeedbackToConsole(Int32 value) {
        Console.WriteLine("Item=" + value);
    }

    private static void FeedbackToMsgBox(Int32 value) {
        MessageBox.Show("Item=" + value);
    }

    private void FeedbackToFile(Int32 value) {
        using (StreamWriter sw = new StreamWriter("Status", true)) {
            sw.WriteLine("Item=" + value);
        }
    }
}
```

下面来看看代码做的事情。在顶部，注意看`internal`委托`Feedback`的声明。委托要指定一个回调方法签名。在本例中，`Feedback`委托指定的方法要获取一个`Int32`参数，返回`void`。在某种程度上，委托和非托管 C/C++ 中代表函数地址的`typedef`很相似。

`Program`类定义了私有静态方法`Counter`，它从整数`from`计数到整数`to`。方法的`fb`参数代表`Feedback`委托对象引用。方法遍历所有整数。对于每个整数，如果`fb`变量不为`null`，就调用由`fb`变量指定的回调方法。传入这个回调方法的是正在处理的那个数据项的值，也就是数据项的编号。设计和实现回调方法时，可选择任何恰当的方式处理数据项。

## <a name="17_2">17.2 用委托回调静态方法</a>

理解`Counter`方法的设计及其工作方式之后，再来看看如何利用委托回调静态方法。本节重点是上一节示例代码中的`StaticDelegateDemo`方法。

在`StaticDelegateDemo`方法中第一次调用`Counter`方法时，为第三个参数(对应于 `Counter` 的 `fb`参数)传递的是`null`。由于`Counter`的`fb`参数收到的是`null`，所以处理每个数据项时都不调用回调方法。

`StaticDelegateDemo`方法再次调用`Counter`，为第三个参数传递新构成的`Feedback`委托对象。委托对象是方法的包装器(wrapper)，使方法能通过包装器来间接回调。在本例中，静态方法的完整名称`Program.FeedbackToConsole` 被传给 `Feedback` 委托类型的构造器，这就是要包装的方法。`new`操作符返回的引用作为`Counter`的第三个参数来传递。现在，当`Counter`执行时，会为序列中的每个数据项调用`Program`类型的静态方法`FeedbackToConsole`。`FeedbackToConsole`方法本身的作用很简单，就是向控制台写一个字符串，显示正在进行处理的数据项。

> 注意 `FeedbackToConsole`方法被定义成`Program`类型内部的私有方法，但`Counter`方法能调用 `Program` 的私有方法，这明显没有问题，因为`Counter`和`FeedbackToConsole`在同一个类型中定义。但即使`Counter`方法在另一个类型中定义，也不会出问题！简单地说，在一个类型中通过委托来调用另一个类型的私有成员，只要委托对象是由具有足够安全性/可访问性的代码创建的，便没有问题。

在 `StaticDelegateDemo` 方法中，对 `Counter` 方法的第三个调用和第二个调用几乎完全一致。唯一的区别在于 `Feedback` 委托对象包装的是静态方法 `Program.FeedbackToMsgBox`。`FeedbackToMsgBox`构造一个字符串来指出正在处理的数据项，然后在消息框中显示该字符串。

这个例子中的所有操作都是都是类型安全的。例如，在构造`Feedback`委托对象时，编译器确保`Program`的 `FeedbackToConsole` 和`FeedbackToMsgBox`方法的签名兼容于`Feedback`委托定义的签名。具体地说，两个方法都要获取一个参数(一个`Int32`)，而且两者都要有相同的返回类型(`void`)。将`FeedbackToConsole`的定义改为下面这样：

```C#
private static Boolean FeedbackToConsole(String value) {
    ...
}
```

C#编译器将不会编译以上代码，并报告以下错误：`error CS0123:"FeedbackToConsole"的重载均与委托Feedback"不匹配`。

将方法绑定到委托时，C# 和 CLR 都允许引用类型的**协变性**(covariance)和**逆变性**(contravariance)。协变性是指方法能返回从委托的返回类型派生的一个类型。逆变性是指方法获取的参数可以是委托的参数类型的基类。例如下面这个委托：

`delegate Object MyCallback(FileStream s);`

完全可以构造该委托类型的一个实例并绑定具有以下原型的方法：

`String SomeMethod(Stream s);`

在这里，`SomeMethod` 的返回类型(`String`)派生自委托的返回类型(`Object`)；这种协变性是允许的。`SomeMethod`的参数类型(`Stream`)是委托的参数类型(`FileStream`)的基类；这种逆变性是允许的。

注意，只有引用类型才支持协变性与逆变性，值类型或`void`不支持。所以，不能把下面的方法绑定到`MyCallback`委托：

`Int32 SomeOtherMethod(Stream s);`

虽然`SomeOtherMethod` 的返回类型(`Int32`)派生自(`MyCallback`)的返回类型(`Object`)，但这种形式的协变性是不允许的，因为`Int32`是值类型。显然，值类型和 `void` 之所以不支持，是因为它们的存储结构是变化的，而引用类型的存储结构始终是一个指针。幸好，视图执行不支持的操作，C#编译器会报错。

## <a name="17_3">17.3 用委托回调实例方法</a>

委托除了能调用静态方法，还能为具体的对象调用实例方法。为了理解如何回调实例方法，先来看看 17.1 节的示例代码中的 `InstanceDelegateDemo` 方法。

注意`InstanceDelegateDemo`方法构造了名为`p`的`Program`对象。这个`Program`对象没有定义任何实例字段或属性；创建它纯粹是为了演示。在`Counter`方法调用中构造新的`Feedack`委托对象时，向`Feedback` 委托类型的构造函数传递的是`p.FeedbackToFile`。这导致委托包装对`FeedbackToFile`方法的引用，这是一个实例方法(而不是静态方法)。当`Counter`调用由其`fb`实参标识的回调方法时，会调用`FeedbackToFile`实例方法，新构造的对象`p`的地址作为隐式的 `this`参数传给这个实例方法。

`FeedbackToFile`方法的工作方法类似于`FeedbackToConsole`和`FeedbackToMsgBox`，不同的是它会打开一个文件，并将字符串附加到文件末尾。(方法创建的 Status 文件可在与可执行程序相同的目录中找到。)

再次声明，本例旨在演示委托可以包装对实例方法和静态方法的调用。如果是实例方法，委托要知道方法操作的是具体哪个对象实例。包装实例方法很有用，因为对象内部的代码可以访问对象的实例成员。这意味着对象可以维护一些状态，并在回调方法执行期间利用这些状态信息。

## <a name="17_4">17.4 委托揭秘</a>

从表面看，委托似乎很容易使用：用 C#的`delegate`关键字定义，用熟悉的`new`操作符构造委托实例，用熟悉的方法调用语法来调用回调函数(用引用了委托对象的变量替代方法名)。

但实际情况比前几个例子演示的要复杂一些。编译器和 CLR 在幕后做了大量工作来隐藏复杂性。本节要解释编译器和 CLR 如何协同工作来实现委托。掌握这些知识有助于加深对委托的理解，并学会如何更高效地使用。另外，还要介绍通过委托来实现的一些附加功能。

首先重新审视这一行代码：

`internal delegate void Feedback(Int32 value);`

看到这行代码后，编译器实际会像下面这样定义一个完整的类：

```C#
internal class Feedback : System.MulticastDelegate {
    // 构造器
    public Feedback(Object @object, IntPtr method);

    // 这个方法的原型和源代码指定的一样
    public virtual void Invoke(Int32 value);

    // 以下方法实现对回调方法的异步问题
    public virtual IAsyncResult BeginInvoke(Int32 value, AsyncCallback callback, Object @object);
    public virtual void EndInvoke(IAsyncResult result);
}
```

编译器定义的类有 4 个方法：一个构造器、`Invoke`、`BeginInvoke`和`EndInvoke`。本章重点解释构造器和`Invoke`。`BeginInvoke`和`EndInvoke`方法将留到第 27 章讨论。

事实上，可用 ILDasm.exe 查看生成的程序集，验证编译器真的会自动生成这个类，如果 17-1 所示。

![17_1](../resources/images/17_1.png)  

图 17-1 ILDasm.exe 显示了编译器为委托生成的元数据

在本例中，编译器定义了 `Feedback` 类，它派生自 FCL 定义的`System.MulticastDelegate` 类型(所有委托类型都派生自`MulticastDelegate`)。

> 重要提示 `System.MulticastDelegate`派生自`System.Delegate`，后者又派生自 `System.Object`。是历史原因造成有两个委托类。这实在是令人遗憾———— FCL 本该只有一个委托类。没有办法，我们对这两个类都要有所了解。即使创建的所有委托类型都将`MulticastDelegate`作为基类，个别情况下仍会使用 `Delegate` 类(而非`MulticastDelegate`类)定义的方法处理自己的委托类型。例如，`Delegate`类的两个静态方法`Combine`和`Remove`(后文将解释其用途)的签名都指出要获取`Delegate`参数。由于你创建的委托类型派生自`MulticastDelegate`，后者又派生自`Delegate`，所以你的委托类型的实例是可以传给这两个方法的。

这个类的可访问性是`private`，因为委托在源代码中声明为`internal`。如果源代码改成使用`public`可见性，编译器生成的`Feedback`类也会变成公共类。要注意的是，委托类既可嵌套在一个类型中定义，也可在全局范围中定义。简单地说，由于委托是类，所以凡是能够定义类的地方，都能定义委托。

由于所有委托类型都派生自`MulticastDelegate`，所以它们继承了`MulticastDelegate`的字段、属性和方法。在所有这些成员中，有三个非公共字段是最重要的。表 17-1 总结了这些重要字段。

表 17-1 `MulticastDelegate` 的三个重要的非公共字段  
|字段|类型|说明|
|:---:|:---:|:---:|
|`_target`|`System.Object`|当委托对象包装一个静态方法时，这个字段为`null`。当委托对象包装一个实例方法时，这个字段引用的是回调方法要操作的对象。换言之，这个字段指出要传给实例方法的隐式参数 `this` 的值|
|`_methodPtr`|`System.IntPtr`|一个内部的整数值，CLR用它标识要回调的方法|
|`_invocationList`|`System.Object`|该字段通常为 `null`。构造委托链时它引用一个委托数组(详情参见下一节)|  

注意，所有委托都有一个构造器，它获取两个参数：一个是对象引用，另一个是引用了回调方法的整数。但如果仔细查看前面的源代码，会发现传递的是`Program.FeedbackToConsole`或`p.FeedbackToFile`这样的值。根据迄今为止学到的编程知识，似乎没有可能通过编译！

然而，C# 编译器知道要构造的是委托，所以会分析源代码来确定引用的是哪个对象和方法。对象引用被传给构造器的 `object` 参数，标识了方法的一个特殊 `IntPtr` 值(从 `MethodDef` 或 `MemberRef` 元数据 token 获得)被传给构造器的 `method` 参数。对于静态方法，会为 `object` 参数传递 `null` 值。在构造器内部，这两个实参分别保存在 `_target` 和 `_methodPtr` 私有字段中。除此以外，构造器还将 `_invocationList` 字段设为`null`，对这个字段的讨论将推迟到 17.5 节 “用委托回调多个方法(委托链)”进行。

所以，每个委托对象实际都是一个包装器，其中包装了一个方法和调用该方法时要操作的对象。例如，在执行以下两行代码之后：

```C#
Feedback fbStatic = new Feedback(Program.FeedbackToConsole);
Feedback fbInstance = new Feedback(new Program().FeedbackToFile);
```

`fbStatic` 和 `fbInstance` 变量将引用两个独立的、初始化好的 `Feedback` 委托对象，，如图 17-2 所示。

![17_2](../resources/images/17_2.png)  

图 17-2 在两个变量引用的委托中，一个包装静态方法，另一个包装实例方法

知道委托对象如何构造并了解其内部结构之后，再来看看回调方法时如何调用的。为方便讨论，下面重复了 `Counter` 方法的定义：

```C#
private static void Counter(Int32 from, Int32 to, Feedback fb) {
    for (Int32 val = from; val <= to; val++) {
        // 如果指定了任何回调，就调用它们
        if (fb != null)
            fb(val);
    }
}
```

注意`if`语句首先检查`fb`是否为`null`。不为`null`就调用<sup>①</sup>回调方法。`null`检查必不可少，因为`fb`只是*可能*引用了`Feedback`委托对象的变量；它也可能为`null`。这段代码看上去像是调用了一个名为`fb`的函数，并向它传递一个参数(`val`)。但事实上，这里没有名为 `fb` 的函数。再次提醒你注意注意，因为编译器知道 `fb` 是引用了委托对象的变量，所以会生成代码调用该委托对象的`Invoke` 方法。也就是说，编译器在看到以下代码时：

> ① 这里的“调用”是 `invoke`，参考 8.6.2 节的译注对 `invoke` 和 `call` 的解释。 ———— 译注

`fb(val);`

它将生成以下代码，好像源代码本来就是这么写的一样：

`fb.Invoke(val);`

为了验证编译器生成代码来调用委托类型的 `Invoke` 方法，可利用 ILDasm.exe 检查为 `Counter` 方法创建的 IL 代码。下面列出了 `Counter` 方法的 IL 代码。IL_0009 处的指令就是对 `Feedback` 的 `Invoke` 方法的调用。

```C#
.method private hidebysig static void Counter( int32 from,
                                               int32 'to',
                                               class Feedback fb) cil managed
{
    // Code size 23 (0x17)
    .maxstack 2
    .locals init (int32 val)
    IL_0000: ldarg.0
    IL_0001: stloc.0
    IL_0002: br.s IL_0012
    IL_0004: ldarg.2
    IL_0005: brfalse.s IL_000e
    IL_0007: ldarg.2
    IL_0008: ldloc.0
    IL_0009: callvirt instance void Feedback::Invoke(int32)
    IL_000e: ldloc.0
    IL_000f: ldc.i4.1
    IL_0010: add
    IL_0011: stloc.0
    IL_0012: ldloc.0
    IL_0013: ldarg.1
    IL_0014: ble.s IL_0004
    IL_0016: ret
} // end of method Program::Counter
```

其实，完全可以修改 `Counter` 方法来显式调用 `Invoke` 方法，如下所示：

```C#
private static void Counter(Int32 from, Int32 to, Feedback fb) {
    for (Int32 val = from; val <= to; val++){
        // 如果指定了任何回调，就调用它们
        if (fb != null)
            fb.Invoke(val);
    }
}
```

前面说过，编译器是在定义 `Feedback` 类的时候定义 `Invoke` 的。在 `Invoke` 被调用时，它使用私有字段 `_target` 和 `_methodPtr` 在指定对象上调用包装好的回调方法。注意，`Invoke`方法的签名和委托的签名匹配。由于`Feedback`委托要获取一个`Int32`参数并返回`void`，所以编译器生成的`Invoke`方法也要获取一个`Int32`并返回`void`。

## <a name="17_5">17.5 用委托回调多个方法(委托连)</a>

委托本身就很有用，再加上对委托链的支持，用处就更大了！委托链是委托对象的集合。可利用委托链调用集合中的委托所代表的全部方法。为了理解这一点，请参考 17.1 节的实例代码中的 `ChainDelegateDemo1` 方法。在 `Console.WriteLine` 语句之后，我构造了三个委托对象并让变量 `fb1`，`fb2` 和 `fb3` 分别引用每个对象，如图 17-3 所示。

![17_3](../resources/images/17_3.png)  
图 17-3 fb1，fb2 和 fb3 变量引用的委托对象的初始状态

指向`Feedback`委托对象的引用变量`fbChain`旨在引用委托链(或者说委托对象集合)，这些对象包装了可回调的方法。`fbChain`初始化为`null`，表明目前没有要回调的方法。使用`Delegate`类的公共静态方法`Combine`将委托添加到链中：

`fbChain = (Feedback) Delegate.Combine(fbChain, fb1);`

执行这行代码时，`Combine` 方法发现试图合并的是`null`和`fb1`。在内部，`Combine`直接返回`fb1`中的值，所以`fbChain`变量现在引用`fb1`变量所引用的委托对象，如果 17-4 所示。

![17_4](../resources/images/17_4.png)  
图 17-4 在委托链中插入第一个委托后委托对象的状态  

再次调用 `Combine` 方法在链中添加第二个委托：

`fbChain = (Feedback) Delegate.Combine(fbChain, fb2);`

在内部，`Combine`方法发现`fbChain` 已引用了一个委托对象，所以 `Combine` 会构造一个新的委托对象。新委托对象对它的私有字段 `_target` 和 `_methodPtr` 进行初始化，具体的值对于目前的讨论来说并不重要。重要的是，`_invocationList`字段被初始化为引用一个委托对象数组。数组的第一个元素(索引0)被初始化引用包装了 `FeedbackToConsole` 方法的委托(也就是 `fbChain` 目前引用的委托)。数组的第二个元素(索引 1)被初始化为引用包装了`FeedbackToMsgBox`方法的委托(也就是 `fb2` 引用的委托)。最后，`fbChain`被设为引用新建的委托对象，如果 17-5 所示。

![17_5](../resources/images/17_5.png)  
图 17-5 在委托链中插入第二个委托之后委托对象的状态

为了在链中添加第三个委托，我再次调用 `Combine` 方法。

`fbChain = (Feedback) Delegate.Combine(fbChain, fb3);`

同样地，`Combine`方法发现`fbChain`已引用了一个委托对象，因而又构造一个新的委托对象，如果 17-6 所示。和前面一样，新委托对象对私有字段 `_target`和`_methodPtr`进行初始化，具体的值就目前来说并不重要。`_invocationList`字段被初始化为引用一个委托对象数组。该数组的第一个元素和第二个元素(索引 0 和 1)被初始化为引用 `fb1` 和 `fb2` 所引用的委托。数组的第三个元素(索引 2)被初始化为引用包装了`FeedbackToFile`方法的委托(这是`fb3`所引用的委托)。最后，`fbChain`被设为引用这个新建的委托对象。注意，之前新建的委托及其`_invocationList`字段引用的数组现在可以进行垃圾回收。

在`ChainDelegateDemo1`方法中，用于设置委托链的所有代码执行完毕之后，我将`fbChain`变量传给 `Counter` 方法：

`Counter(1, 2, fbChain);`

`Counter`方法内部的代码会在`Feedback`委托对象上隐式调用`Invoke`方法，具体已在前面讲述过了。在 `fbChain`引用的委托上调用`Invoke`时，该委托发现私有字段`_invocationList`不为`null`，所以会执行一个循环来遍历数组中的所有元素，并依次调用每个委托包装的方法。在本例中，`FeedbackToConsole`首先被调用，随后是`FeedbackToMsgBox`，最后是`FeedbackToFile`。

![17_6](../resources/images/17_6.png)  

图 17-6 委托链完成后委托对象的最终状态

以伪代码的形式，`Feedback` 的 `Invoke` 方法基本上是像下面这样实现的：

```C#
public void Invoke(Int32 value) {
    Delegate[] delegateSet = _invocationList as Delegate[];
    if (delegateSet != null ) {
        // 这个委托数组指定了应该调用的委托
        foreach (Feedback d in delegates)
            d(value);   // 调用每个委托
    } else {  // 否则就不是委托链
        // 该委托标识了要回调的单个方法，
        // 在指定的目标对象上调用这个回调方法
        _methodPtr.Invoke(_target, value);
        // 上面这行代码接近实际的代码，
        // 实际发生的事情用 C# 是表示不出来的
    }
}
```

注意，还可调用 `Delegate` 的公共静态方法 `Remove` 从链中删除委托。`ChainDelegateDemo1` 方法在结尾处对此进行了演示。

`fbChain = (Feedback) Delegate.Remove(fbChain, new Feedback(FeedbackToMsgBox));`

`Remove` 方法被调用时，它扫描第一个实参(本例是`fbChain`)所引用的那个委托对象内部维护的委托数组(从末尾向索引 0 扫描)。`Remove`查找的是其`_target` 和 `_methodPtr` 字段与第二个实参(本例是新建的`Feedback`委托)中的字段匹配的委托。如果找到匹配的委托，并且(在删除之后)数组中只剩余一个数据项，就返回那个数据项。如果找到匹配的委托，并且(在删除之后)数组中只剩余一个数据项，就返回那个数据项。如果找到匹配的委托，并且数组中还剩余多个数据项，就新建一个委托对象————其中创建并初始化的 `_invocationList` 数组中还剩余多个数据项，当然被删除的数据项除外————并返回对这个新建委托对象的引用。如果从链中删除了仅有的一个元素，`Remove`会返回`null`。注意，每次 `Remove` 方法调用只能从链中删除一个委托，它不会删除有匹配的`_target` 和 `_methodPtr` 字段的所有委托。

前面展示的例子中，委托类型 `Feedback` 的返回值都是 `void`。但完全可以像下面这样定义 `Feedback` 委托：

`public delegate Int32 Feedback(Int32 value);`



