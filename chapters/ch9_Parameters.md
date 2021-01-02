# 第 9 章 参数

本章内容：

* <a href="#9_1">可选参数和命名参数</a>
* <a href="#9_2">隐式类型的局部变量</a>
* <a href="#9_3">以传引用的方式向方法传递参数</a>
* <a href="#9_4">向方法传递可变数量的参数</a>
* <a href="#9_5">参数和返回类型的设计规范</a>
* <a href="#9_6">常量性</a>

本章重点在于向方法传递的各种方式，包括如何可选地指定参数，按名称指定参数，按名称指定参数，按引用传递参数，以及如何定义方法来接受可变数量的参数。

## <a name="9_1">9.1 可选参数和命名参数</a>

设计方法的参数时，可为部分或全部参数分配默认值。然后，调用这些方法的代码可以选择不提供部分实参，使用其默认值。此外，调用方法时刻通过指定参数名称来传递参数。以下代码演示了可选参数和命名参数的用法：

```C#
using System;
public static class Program {
    private static Int32 s_n = 0;

    private static void M(Int32 x = 9, String s = "A", 
        DateTime dt = default(DateTime), Guid guid = new Guid()) {

        Console.WriteLine("x={0}, s={1}, dt={2}, guid={3}", x, s, dt, guid);
    }

    public static void Main() {
        // 1. 等同于M(9, "A", default(DateTime), new Guid());
        M();

        // 2. 等同于M(8, "X", default(DateTime), new Guid());
        M(8, "X");

        // 3. 等同于M(5, "A", default(DateTime),Guid.NewGuid());
        M(5, guid: Guid.NewGuid(), dt: DateTime.Now);

        // 4. 等同于M(0, "1", default(DateTime), new Guid());
        M(s_n++, s_n++.ToString());

        // 5. 等同于以下两行代码：
        // String t1 = "2"; Int32 t2 = 3;
        // M(t2, t1, default(DateTime), new Guid());
        M(s: (s_n++).ToString(), x: s_n++);
    }
}
```

运行程序得到以下输出：

```cmd
x=9, s=A, dt=1/1/0001 12:00:00 AM, guid=00000000-0000-0000-0000-000000000000
x=8, s=X, dt=1/1/0001 12:00:00 AM, guid=00000000-0000-0000-0000-000000000000
x=5, s=A, dt=1/1/2021 3:34:58 PM, guid=372cba1a-01db-4570-945e-33e066882919
x=0, s=1, dt=1/1/0001 12:00:00 AM, guid=00000000-0000-0000-0000-000000000000
x=3, s=2, dt=1/1/0001 12:00:00 AM, guid=00000000-0000-0000-0000-000000000000
```

如你所见，如果调用时省略了一个实参，C#编译器会自动嵌入参数的默认值。对 `M` 的第 3 个和第 5 个调用使用了C# 的命名参数功能。在这两个调用中，我为 `x` 显式传递了值，并指出要为名为 `guid` 和 `dt` 的参数传递实参。

向方法传递实参时，编译器按从做到右的顺序对实参进行求值。在对 `M` 的第 4 个调用中，`s_n` 的当前值`(0)`传给`x`，然后`s_n`递增。随后，`s_n` 的当前值`(1)`作为字符串传给`s`，然后继续递增到`2`。使用命名参数传递实参时，编译器仍然按从左到右的顺序对实参进行求值。在对`M` 的第5 个调用中，`s_n`中当前值`(2)`被转换成字符串，并保存到编译器创建的临时变量`(t1)`中。接着，`s_n`递增到`3`，这个值保存到编译器创建的另一个临时变量`(t2)`中。然后，`s_n`继续递增到`4`。最后在调用`M`时，向它传递的实参依次是`t2`，`t1`，一个默认`DateTime`和一个新建的`Guid`。

### 9.1.1 规则和原则

如果在方法中为部分参数指定了默认值，请注意以下附加的规则和原则。

* 可为方法、构造器方法和有参属性(C#索引器)的参数指定默认值。还可以属于委托定义一部分的参数指定默认值。以后调用该委托类型的变量时可省略实参来接受默认值。

* 有默认值的参数必须放在没有默认值的所有参数之后。换言之，一旦定义了有默认值的参数，它右边的所有参数也必须有默认值。例如在前面的`M`方法定义中，如果删除`s`的默认值`("A")`，就会出现编译错误。但这个规则有一个例外：“参数数组”<sup>①</sup>这种参数必须放在所有参数(包括有默认值的这些)之后，而且数组本身不能有一个默认值。
> ① 在本章后面 9.4 节“向方法传递可变数量的参数”详细讨论。

* 默认值必须是编译时能确定的常量值。那么，哪些参数能设置默认值？这些参数的类型可以是C# 认定的基元类型(参见第 5 章的表 5-1)。还包括枚举类型，以及能设为`null`的任何引用类型。值类型的参数可将默认值设为值类型的实例，并让它的所有字段都包含零值。可以用 `default` 关键字或者 `new`关键字来表达这个意思；两种语法将生成完全一致的 IL 代码。在 `M` 方法中设置 `dt`参数和 `guid` 参数的默认值时，分别使用的就是这两种语法。

* 不要重命名参数变量，否则任何调用者以传参数名的方式传递实参，它们的代码也必须修改。例如，在前面的`M`方法声明中，如果将`dt`变量重命名为`dateTime`，对`M`的第三个调用就会造成编译器显示以下消息：`error CS1739:"M"的最佳重载没有名为"dt"的参数`。

* 如果方法从模块外部调用，更改参数的默认值具有潜在的危险性。call site<sup>①</sup>在它的调用中嵌入默认值。如果以后更改了参数的默认值，但没有重新编译包含 call site 的代码，它在调用你的方法时就会传递旧的默认值。可考虑将默认值`0/null`作为哨兵值使用，从而指出默认行为。这样一来，即使更改了默认值，也不必重新编译包含了 call site 的全部代码。下面是一个例子：
  > ① call site 是发出调用的地方，可理解成调用了一个目标方法的表达式或代码行。 ——译注
```C#
// 不要这样做：
private static String MakePath(String filename = "Untitled") {
    return String.Format(@"C:\{0}.txt", filename);
}

// 而要这样做：
private static String MakePath(String filename = null) {
    // 这里使用了空接合操作符(??)；详情参见第 19 章
    return String.Format(@"C:\{0}.txt", filename ?? "Untitled");
}
```

* 如果参数用`ref`或`out`关键字进行了标识，就不能设置默认值。因为没有办法为这些参数传递有意义的默认值。

使用可选或命名参数调用方法时，还要注意以下附加的规则和原则。

* 实参可按任意顺序传递，但命名实参只能出现在实参列表的尾部。

* 可按名称将实参传给没有默认值的参数，但所有必须的实参都必须传递(无论按位置还是按名称)，编译器才能编译代码。

* C# 不允许省略逗号之间的实参，比如 `M(1, , DateTime.Now)`。因为这会造成对可读性的影响，程序员将被迫去数逗号。对于有默认值的参数，如果想省略它们的实参，以传参数名的方式传递实参即可。

* 如果参数要求 `ref/out`，为了以传参数名的方式传递实参，请使用下面这样的语法：

```C#
// 方法声明
private static void M(ref Int32 x) { ... }

// 方法调用：
Int32 a = 5;
M(x: ref a);
```

> 注意 写 C# 代码和 Microsoft Office 的 COM 对象模型进行互操作性时，C# 的可选参数和命名参数功能非常好用。另外，调用 COM 组件时，如果是以传引用的方式传递实参，C# 还允许省略 `ref/out`，进一步简化编码。但如果调用的不是 COM 组件，C# 就要求必须向实参应用 `ref/out` 关键字。

### 9.1.2 `DefaultParameterValueAttribute` 和 `OptionalAttribute`

默认和可选参数的概念要不是 C# 特有的就好了！具有地说，我们希望程序员能用一种编程语言定义一个方法，指出哪些参数是可选的，以及它们的默认值是什么。然后，另一种语言的程序员可以调用该方法。要实现这一点，所选的编译器必须允许调用者忽略一些实参，还必须能确实这些实参的默认值。

在 C# 中，一旦为参数分配了默认值，编译器就会在内部向该参数应用定制特性`System.Runtime.InteropServices.OptionalAttribute`。该特性会在最终生成的文件的元数据中持久性地存储下来。此外，编译器向参数应用 `System.Runtime.InteropServices.DefaultParameterValueAttribute` 特性，并将该属性持久性存储到生成的文件的元数据中。然后，会向 `DefaultParameterValueAttribute` 的构造器传递你在源代码中指定的常量值。

之后，一旦编译器发现某个方法调用缺失了部分实参，就可以确定省略的是可选的实参，并从元数据中提取默认值，将值自动嵌入调用中。

## <a name="9_2">9.2 隐式类型的局部变量</a>

C# 能根据初始化表达式的类型推断方法中的局部变量的类型，如下所示：

```C#
private static void ImplicitlyTypeLocalVariables() {
    var name = "Jeff";
    ShowVariableType(name);                         // 显示：System.String

    // var n = null;                                // 错误，不能将 null 赋给隐式类型的局部变量
    var x = (String)null;                           // 可以这样写，但愿意不大
    ShowVariableType(x);                            // 显示：System.String

    var numbers = new Int32[] { 1, 2, 3, 4 };
    ShowVariableType(numbers);                      // 显示：System.Int32[]

    // 复杂类型能少打一些字
    var collection = new Dictionary<String, Single>() { { "Grant", 4.0f } };

    // System.Collections.Generic.Dictionary`2[System.String,System.Single]
    ShowVariableType(collection);

    foreach (var item in collection) {
        // 显示：System.Collections.Generic.KeyValuePair`2[System.String,System.Single]
        ShowVariableType(item);
    }
}

private static void ShowVariableType<T>(T t) {
    Console.WriteLine(typeof(T));
}
```

`ImplicitlyTypeLocalVariables` 方法中的第一行代码使用C#的`var`关键字引入了一个新的局部变量。为了确定`name`变量的类型，编译器要检查赋值操作符(=)右侧的表达式的类型。由于`"Jeff"`是字符串，所以编译器推断`name`的类型是`String`。为了证明编译器正确推断出类型，我写了`ShowVariableType` 方法。这个泛型方法推断它的实参的类型并在控制台上显示。为方便阅读，我在 `ImplicitlyTypeLocalVariables` 方法内以注释形式列出了每次调用`ShowVariableType`方法的显示结果。

`ImplicitlyTypeLocalVariables` 方法内部的第二个赋值(被注释掉了)会产生编译错误`:error CS0815:无法将“<null>”赋予隐式类型的局部变量`。这是由于 `null` 能隐式转型为任何引用类型或可空值类型。因此，编译器不能推断它的确切类型。但在第三个赋值语句中，我证明只要显式指定了类型(本例是 `String`)，就可以将隐式类型的局部变量初始化为 `null`。这样做虽然可行，但意义不大，因为可以写 `String x = null;`来获得同样效果。

第 4 个赋值语句反映了 C# “隐式类型局部变量”功能的真正价值。没有这个功能，就不得不在赋值操作符的左右两侧指定 `Dictionary<String, Single>`。这不仅需要打更多的字，而且以后修改了集合类型或者任何泛型参数类型，赋值操作符两侧的代码也要修改。

在`foreach`循环中，我用`var`让编译器自动推断集合中的元素的类型。这证明了`var`能很好地用于`foreach`，`using`和`for`语句。还可在验证代码时利用它。例如，可以用方法的返回值初始化隐式类型的局部变量。开发方式时可以灵活更改返回类型。编译器能察觉到返回类型的变化，并自动更改变量的类型！当然，如果使用变量的代码没有相应地进行修改，还是像使用旧类型那样使用它，就可能无法编译。

在 Microsoft Visual Studio 中，鼠标放到 `var` 上将显示一条“工具提示”，指出编译器根据表达式推断出来的类型。在方法中使用匿名类型时必须用到C#的隐式类型局部变量，详情参见第 10 章“属性”。

不能用`var`声明方法的参数类型。原因显而易见，因为编译器必须根据在call site传递的实参来推断参数类型，但 call site 可能一个都没有，也可能有好多个<sup>①</sup>。除此之外，不能用`var`声明类型中的字段。C# 的这个限制是出于多方面的考虑。一个原因是字段可以被多个方法访问，而 C# 团队认为这个协定(变量的类型)应该显式陈述。另一个原因是一旦允许，匿名类型(第 10 章)就会泄露到方法的外部。
> ① 要么一个类型都推断不出来，要么多个推断发生冲突。 ——译注

>重要提示 不要混淆`dynamic`和`var`。用`var`声明局部变量只有一种简化语法，它要求编译器根据表达式推断具体数据类型。`var`关键字只能声明方法内部的局部变量，而`dynamic`关键字适用于局部变量、字段和参数。表达式不能转型为`var`，但能转型为 `dynamic`。必须显式初始化用 `var`声明的变量，但无需初始化用 `dynamic` 声明的变量。欲知 C# `dynamic` 类型的详情，请参见 5.5 节 “`dynamic`基元类型"。

## <a name="9_3">9.3 以传引用的方式向方法传递参数</a>

CLR 默认所有方法参数都传值。传递引用类型的对象时，对象引用(或者说指向对象的指针)被传给方法。注意引用(或指针)本身是传值的，意味着方法能修改对象，而调用者能看到这些修改。对于值类型的实例，传给方法的是实例的一个副本，意味着方法将获得它专用的一个值类型实例副本，调用者中的实例不受影响。
>重要提示 在方法中，必须知道传递的每个参数是引用类型还是值类型，处理参数的代码显著有别。

CLR 允许以传引用而非传值的方式传递参数。C# 用关键字 `out` 或 `ref` 支持这个功能。连个关键字都告诉C# 编译器生成元数据来指明该参数是传引用的。编译器将生成代码来传递参数的地址，而非传递参数本身。

CLR 不区分 `out` 和 `ref`，意味着无论用哪个关键字，都会生成相同的 IL 代码。另外，与那数据也几乎完全一致，只有一个 bit 除外，它用于记录声明方法时指定的是 `out` 还是 `ref`。但C# 编译器是将这两个关键字区别对待的，而且这个区别决定了由哪个方法负责初始化所引用的对象。如果方法的参数用 `out` 来标记，表明不指望调用者在调用方法之前初始化好了对象。被调用的方法不能读取参数的值，而且在返回前必须向这个值写入。相反，如果方法的参数用 `ref` 来标记，调用者就必须在调用该方法前初始化参数的值，被调用的方法可以读取值以及/或者向值写入。

对于 `out` 和 `ref`，引用类型和值类型的行为迥然有异。先看一看为值类型使用 `out` 和 `ref`:

```C#
public sealed class Program {
    public static void Main() {
        Int32 x;                    // x 没有初始化
        GetVal(out x);              // x 不必初始化
        Console.WriteLine(x);       // 显示 "10"
    }

    private static void GetVal(out Int32 v) {
        v = 10;             // 该方法必须初始化 V
    }
}
```

在上述代码中，`x` 在 `Main` 的栈桢<sup>①</sup>中声明。然后，`x`的地址传递给`GetVal`。`GetVal`的`v`是一个指针，指向`Main` 栈桢中的`Int32` 值。在`GetVal`内部，`v`指向的那个`Int32`被更改为`10`。`GetVal`返回时，`Main`的`x`就有了一个为`10`的值，控制台上回显示`10`。为大的值类型使用`out`，可提升代码的执行效率，因为它避免了在进行方法调用时复制值类型实例的字段。
> ① 栈桢(stack frame)代表当前线程的调用栈中的一个方法调用。在执行线程的过程中进行的每个方法调用都会在调用栈中创建并压入一个 `StackFrame`。——译注

下例用`ref`代替了`out`：

```C#
public sealed class Progarm {
    public static void Main() {
        Int32 x = 5;                // x 已经初始化
        AddVal(ref x);              // x 必须初始化
        Console.WriteLine(x);       // 显示"15"
    }

    private static void AddVal(ref Int32 v) {
        v += 10;        // 该方法可使用 v 的已初始化的值
    }
}
```

在上述代码中，`x`也在`Main`的栈桢中声明，并初始化为`5`。然后，`x`的地址传给`AddVal`。`AddVal`的`v`是一个指针，指向`Main`栈桢中的`Int32`值。在`AddVal`内部，`v`指向的那个`Int32`要求必须是已经初始化的。因此，`AddVal`可在任何表达式中使用该初始值。`AddVal`还可更改这个值，新值会“返回”调用者。在本例中，`AddVal`将`10`加到初始值上。`AddVal`返回时，`Main`的`x`将包含`15`，这个值会在控制台上显示出来。

综上所述，从 IL 和 CLR 的角度看，`out`和`ref`是同一码事：都导致传递指向实例的一个指针。但从编译器的角度看，两者是有区别的。根据是`out`还是`ref`，编译器会按照不同的标准来验证你写的代码是否正确。以下代码试图向要求 `ref` 参数的方法传递未初始化的值，结果是编译器报告以下错误：`error CS0165：使用了未赋值的局部变量“x”`。

```C#
public sealed class Program {
    public static void Main() {
        Int32 x;                   // x 内有初始化

        // 下一行代码无法通过编译，编译器将报告：
        // error CS1065：使用了未赋值的局部变量 "x"
        AddVal(ref x);

        Console.WriteLine(x);
    }

    private static void AddVal(ref Int32 v) {
        v += 10;                    // 该方法可使用 v 的已初始化的值
    }
}
```

> 重要提示 经常有人问我，为什么 C# 要求必须在调用方法时指定 `out`或`ref`？毕竟，编译器知道被调用的方法需要的是`out`还是`ref`，所以应该能正确编译代码。事实上，C# 编译器确实能自动采用正确的操作。但 C# 语言的设计者认为调用者的方法是否需要对传递的变量值进行更改。

> 另外，CLR 允许根据使用的是 `out` 还是 `ref`参数对方法进行重载。例如，在 C# 中，以下代码是合法的，可以通过编译：

```C#
public sealed class Point {
    static void Add(Point p) { ... }
    static void Add(ref Point p) { ... }
}
```

> 两个重载方法只有`out`还是`ref`的区别则不合法，因为两个签名的元数据形式完全相同。所以，不能在上述 `Point` 类型中再定义以下方法：

```C#
static void Add(out Point p) { ... }
```

> 试图在 `Point` 类型中添加这个 `Add` 方法，C# 编译器会显示以下消息：`CS0663:"Add" 不能定义仅在ref和out上有差别的重载方法`。

为值类型使用 `out` 和 `ref`，效果等同与以传值的方式传递引用类型。对于值类型，`out` 和 `ref` 允许方法操纵单一的值类型实例。调用者必须为实例分配内存(中的内容)。对于引用类型，调用代码为一个指针分配内存(该指针指向一个引用类型的对象)，被调用者则操纵这个指针。正因为如此，仅当方法“返回”对“方法知道的一个对象”的引用时，为引用类型使用 `out` 和 `ref`才有意义。以下代码对此进行了演示。

```C#
using System;
using System.IO;

public sealed class Program {
    public static void Main() {
        FileStream fs;                  // fs 没初始化

        // 打开第一个待处理的文件
        StartProcessingFiles(out fs);

        // 如果有更多需要处理的文件，就继续
        for (; fs != null; ContinueProcssingFiles(ref fs)) {
            
            // 处理一个文件
            fs.Read(...);
        }
    }

    private static void StartProcessingFiles(out FileStream fs) {
        fs = new FileStream(...);          // fs 必须在这个方法中初始化
    }

    private static void ContinueProcssingFiles(ref FileStream fs) {
        fs.Close();                      // 关闭最后一个操作的文件

        // 打开下一个文件；如果没有更多的文件，就“返回” null
        if (noMoreFilesToProcess) fs = null;
        else fs = new FileStream (...);
    }
}
```

可以看出，上述代码最大的不同就是定义了一些使用了`out`或`ref`引用类型参数的方法，并用这些方法构造对象。新对象的指针将“返回”给调用者。还要注意，`ContinueProcessingFiles` 方法可对传给它的对象进行处理，再“返回”一个新对象。之所以能这样做，是因为参数标记了 `ref`。上述代码可简化为以下形式：

```C#
using System;
using System.IO;

public sealed class Program {
    public static void Main() {
        FileStream fs = null;    // 初始化为 null (必要的操作)

        // 打开第一个待处理的文件
        ProcessFiles(ref fs);

        // 如果有更多需要处理的文件，就继续
        for (; fs != null; ProcessFiles(ref fs)) {
            
            // 处理文件
            fs.Read(...);
        }
    }

    private static void ProcessFiles(ref FileStream fs) {
        // 如果先前的文件是打开的，就将其关闭
        if (fs != null) fs.Close();   // 关闭最后一个操作的文件

        // 打开下一个文件；如果没有更多的文件，就“返回” null
        if (noMoreFilesToProcess) fs = null;
        else fs = new FileStream (...);
    }
}
```

下例演示了如何用 `ref` 关键字实现一个用于交换两个引用类型的方法：

```C#
public static void Swap(ref Object a, ref Object b) {
    Object t = b;
    b = a;
    a = t;
}
```

为了交换对两个 `String` 对象的引用，你或许以为代码能像下面这样写：

```C#
public static void SomeMethod() {
    String s1 = "Jeffrey";
    String s2 = "Richter";

    Swap(ref s1, ref s2);
    Console.WriteLine(s1);  // 显示 “Richter”
    Console.WriteLine(s2);  // 显示 “Jeffrey”
}
```