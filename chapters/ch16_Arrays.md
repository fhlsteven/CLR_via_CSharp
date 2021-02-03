 # 第 16 章 数组

 本章内容

* <a href="#16_1">初始化数组元素</a>
* <a href="#16_2">数组转型</a>
* <a href="#16_3">所有数组都隐式派生自 `System.Array`</a>
* <a href="#16_4">所有数组都隐式实现 `IEnumerable`、`ICollection` 和 `IList`</a>
* <a href="#16_5">数组的传递和返回</a>
* <a href="#16_6">创建下限非零的数组</a>
* <a href="#16_7">数组的内部工作原理</a>
* <a href="#16_8">不安全的数组访问和固定大小的数组</a>

数组是允许将多个数据项作为集合来处理的机制。CLR 支持一维、多维和交错数组(即数组构成的数组)。所有数组类型都隐式地从 `System.Array` 抽象类派生，后者又派生自 `System.Object`。这意味着数组始终是引用类型，是在托管堆上分配的。在应用程序的变量或字段中，包含的是对数组的引用，而不是包含数组本身的元素。下面的代码更清楚地说明了这一点：

```C#
Int32[] myIntegers;                 // 声明一个数组引用
myIntegers = new Int32[100];        // 创建含有 100 个 Int32 的数组
```

第一行代码声明 `myIntegers` 变量，它能指向包含 `Int32` 值的一维数组。`myIntegers` 刚开始设为 `null`，因为当时还没有分配数组。第二行代码分配了含有 100 个 `Int32` 值的数组，所有 `Int32` 都被初始化为 0。由于数组是引用类型，所以会在托管堆上分配容纳 100 个未装箱`Int32`所需的内存块。实际上，除了数组元素，数组对象占据的内存块还包含一个类型对象指针、一个同步块索引和一些额外的成员<sup>①<sup>。该数组的内存块地址被返回并保存到`myIntegers`变量中。

> ① 这些额外的成员称为 overhead 字段或者说“开销字段”。 —— 译注

还可创建引用类型的数组：

```C#
Control[] myControls;               // 声明一个数组引用
myControls = new Control[50];       // 创建含有 50 个 Control 引用的数组
```

第一行代码声明`myControls` 变量，它能指向包含 `Control` 引用的一维数组。`myControls` 刚开始被设为 `null`，因为当时还没有分配数组。第二行代码分配了含有 50 个 `Control` 引用的数组，这些引用全被初始化为`null`。由于 `Control` 是引用类型，所以创建数组只是创建了一组引用，此时没有创建实际的对象。这个内存块的地址被返回并保存到 `myControls` 变量中。

图 16-1 展示了值类型的数组和引用类型的数组在托管堆中的情况。

![16_1](../resources/images/16_1.png)  

图 16-1 值类型和引用类型的数组在托管堆中的情况

图 16—1 中，`Control` 数组显示了执行以下各行代码之后的结果：

```C#
myControls[1] = new Button();
myControls[2] = new TextBox();
myControls[3] = myControls[2];
myControls[46] = new DataGrid();
myControls[48] = new ComboBox();
myControls[49] = new Button();
```

为了符合“公共语言规范”(Common Language Specification， CLS)的要求，所有数组都必须是 0 基数组(即最小索引为 0)。这样就可以用 C# 的方法创建数组，并将该数组的引用传给用其他语言(比如 Microsoft Visual Basic .NET)写的代码。此外，由于 0 基数组是最常用的数组(至少就目前而言)，所以 Microsoft 花了很大力气优化性能。不过， CLR 确实支持非 0 基数组，只是不提倡使用。对于不介意稍许性能下降或者跨语言移植问题的读者，本章后文要介绍如何创建和使用非 0 基数组。

注意在图 16-1 中，每个数组都关联了一些额外的开销信息。这些信息包括数组的秩<sup>①</sup>、数组每一维的下限(几乎总是 0)和每一维的长度。开销信息还包含数组的元素类型。本章后文将介绍查询这种开销信息的方法。

> ① 即 rank，或称数组的维数。 ——译注

前面已通过几个例子演示了如何创建一维数组。应尽可能使用一维 0 基数组，有时也将这种数组称为 SZ<sup>②</sup>数组或向量(vector)。向量的性能是最佳的，因为可以使用一些特殊的 IL 指令(比如 `newarr`，`ldelem`，`ldelema`，`ldlen` 和 `stelem`)来处理。不过，必要时也可使用多维数组。下面展示了几个多维数组的例子：

```C#
// 创建一个二维数组，由 Double 值构成
Double[,] myDoubles = new Double[10, 20];

// 创建一个三维数组，由 String 引用构成
String[,,] myStrings = new String[5, 3, 10];
```

> ② SZ 是 single-dimension, zero-based(一维 0 基)的简称。 ——译注

CLR 还支持交错数组(jagged array)，即数组构成的数组。0 基一维交错数组的性能和普通向量一样好。不过，访问交错数组的元素意味着必须进行两次或更多次数组访问。下例演示了如何创建一个多边形数组，每个多边形都由一个包含 `Point` 实例的数组构成：

```C#
// 创建由多个 Point 数组构成的一维数组
Point[][] myPolygons = new Point[3][];

// myPolygons[0] 引用一个含有 10 个 Point 实例的数组
myPolygons[0] = new Point[10];

// myPolygons[1] 引用一个含有 20 个 Point 实例的数组
myPolygons[1] = new Point[20];

// myPolygons[2] 引用一个含有 30 个 Point 实例的数组
myPolygons[2] = new Point[30];

// 显示第一个多边形中的 Point
for (Int32 x = 0; x < myPolygons[0].Length; x++)
    Console.WriteLine(myPolygons[0][x]);
```

> 注意 CLR 会验证数组索引的有效性。换句话说，不能创建含有 100 个元素的数组(索引编号 0 到 99)，然后试图访问索引为 -5 或 100 的元素。这样做会导致 `System.IndexOutOfRangeException` 异常。允许访问数组范围之外的内存会破坏类型安全性，而且会造成潜在的安全漏洞，所以 CLR 不允许可验证的代码这么做。通常，索引范围检查对性能的影响微乎其微，因为 JIT 编译器通常只在循环开始之前检查一次数组边界，而不是每次循环迭代都检查<sup>①</sup>。不过，如果仍然担心 CLR 索引检查造成的性能损失，可以在 C# 中使用 unsafe 代码来访问数组。16.7 节“数组的内部工作原理”将演示具体做法。

> ① 不要混淆“循环”和“循环迭代”。例如以下代码：
  ```C#
  Int32[] myArray = new Int32[100];
  for (Int32 i = 0; i < myArray.Length; i++) myArray[i] = i;
  ```
> “`for`循环”总共要“循环迭代100次”，有时也简单地说“迭代100次”。 —— 译注

## <a name="16_1">16.1 初始化数组元素</a>

前面展示了如何创建数组对象，如何初始化数组中的元素。C# 允许用一个语句做这两件事情。例如：

`String[] names = new String[] { "Aidan", "Grant" };`

大括号中的以逗号分隔的数据的数据项称为**数组初始化器**(array initializer)。每个数据项都可以是一个任意复杂度的表达式；在多维数组的情况下，则可以是一个嵌套的数组初始化器。上例只使用了两个简单的`String`表达式。

在方法中声明局部变量来引用初始化好的数组时，可利用 C# 的“隐式类型的局部变量”功能来简化一下代码：

```C#
// 利用 C# 的隐式类型的局部变量功能：
var names = new String[] { "Aidan", "Grant" };
```

编译器推断局部变量 `names` 是 `String[]` 类型，因为那是赋值操作符(`=`)右侧的表达式的类型。

可利用 C# 的隐式类型的数组功能让编译器推断数组元素的类型。注意，下面这行代码没有在 `new` 和 `[]`之间指定类型。

```C#
// 利用 C#的隐式类型的局部变量和隐式类型的数组功能:
var names = new[] { "Aidan", "Grant", null }；
```

在上一行中，编译器检查数组中用于初始化数组元素的表达式的类型，并选择所有元素最接近的共同基类来作为数组的类型。在本例中，编译器发现两个 `String` 和一个 `null`。由于 `null` 可隐式转型为任意引用类型(包括 `String`)，所以编译器推断应该创建和初始化一个由 `String` 引用构成的数组。但假如写以下代码：

```C#
// 使用 C# 的隐式类型的局部变量和隐式类型的数组功能：(错误)
var names = new[] { "Aidan", "Grant", 123 }；
```

编译器就会报错：`error Cs0826:找不到隐式类型数组的最佳类型`。这是由于两个 `String` 和一个 `Int32` 的共同基类是 `Object`，意味着编译器不得不创建 `Object`引用的一个数组，然后对 `123` 进行装箱，并让最后一个数组元素引用已装箱的、值为 `123` 的一个 `Int32`。C# 团队认为，隐式对数组元素进行装箱是一个代价高昂的操作，所以要在编译时报错。

作为初始化数组时的一个额外的语法奖励，还可以像下面这样写：

`String[] names = { "Aidan", "Grant" };`

注意，赋值操作符(`=`)右侧只给出了一个初始化器，没有 `new`，没有类型，没有 `[]`。这个语法可读性很好，但遗憾的是，C#编译器不允许在这种语法中使用隐式类型的局部变量：

```C#
// 试图使用隐式类型的局部变量(错误)
var names = { "Aidan", "Grant" };
```

试图编译上面这行代码，编译器会报告以下两条消息。

* error CS0820：无法用数组初始值设定项初始化隐式类型的局部变量。

* error CS0622；只能使用数组初始值设定项表达式为数组类型赋值，请尝试改用 new 表达式。

虽然理论上可以通过编译，但 C#团队认为编译器在这里会为你做太多的工作。它要推断数组类型，新建数组对象那个，初始化数组，还要推断局部变量的类型。

最后讲一下“隐式类型的数组”如何与“匿名类型”和“隐式类型的局部变量”组合使用。10.1.4 “匿名类型”已讨论了匿名类型以及如何保证类型同一性。下面来看看以下代码：

```C#
// 使用 C# 的隐式类型的局部变量、隐式类型的数组和匿名类型功能：
var kids = new[] { new { Name="Aidan" }, new { Name="Grant" }};

// 示例用法(用了另一个隐式类型的局部变量)：
foreach (var kid in kids)
    Console.WriteLine(kid.Name);
```

这个例子在一个数组初始化器中添加了两个用于定义数组元素的表达式。每个表达式都代表一个匿名类型(因为 `new` 操作符后没有提供类型名称)。由于连个匿名类型具有一致的结构(有一个 `String` 类型的 `Name` 字段)，所以编译器知道这两个对象具有相同的类型(类型的同一性)。然后，我使用了 C#的“隐式类型的数组”功能(在 `new` 和 `[]` 之间不指定类型)，让编译器推断数组本身的类型，构造这个数组对象，并初始化它内部的两个引用，指向匿名类型的两个实例。最后，将对这个数组对象的引用赋给 `kids` 局部变量，该变量的类型通过C# 的“隐式类型的局部变量”功能来推断。

第二行代码用 `foreach` 循环演示如何使用刚才创建的、用两个匿名类型实例初始化的数组。注意必须为循环使用隐式类型的局部变量(`kid`)。运行这段代码将得到以下输出：

```cmd
Aidan
Grant
```

## <a name="16_2">16.2 数组转型</a>

对于元素为引用类型的数组，CLR 允许将数组元素从一种类型转型另一种。成功转型要求数组维数相同，而且必须存在从元素源类型到目标类型的隐式或显式转换。CLR 不允许将值类型元素的数组转型为其他任何类型。(不过，可用 `Array.Copy`方法创建新数组并在其中填充元素来模拟这种效果。)<sup>①</sup>以下代码演示了数组转型：

```C#
// 创建二维 FileStream 数组
FileStream[,] fs2dim = new FileStream[5, 10];

// 隐式转型为二维 Object 数组
Object[,] o2dim = fs2dim;

// 二维数组不能转型为一维数组，编译器报错：
// error CS00303: 无法将类型“object[*,*]”转换为“System.IO.Stream[]”
Stream[] sldim = (Stream[]) o2dim;

// 显示转型为二维 Stream 数组
Stream[,] s2dim = (Stream[,]) o2dim;

// 显式转型为二维 String 数组
// 能通过编译，但在运行时抛出 InvalidCastException 异常
String[,] st2dim = (String[,]) o2dim;

// 创建一维 Int32 数组(元素是值类型)
Int32[] ildim = new Int32[5];

// 不能将值类型的数组转型为其他任何类型，编译器报错：
// error CS0030：无法将类型 "int[]" 转换为 "Object[]"
Object[] oldim = (Object[]) ildim;

// 创建一个新数组，使用 Array.Copy 将源数组中的每个元素
// 转型为目标数组中的元素类型，并把它们复制过去。
// 下面的代码创建元素为引用类型的数组，
// 每个元素都是对已装箱 Int32 的引用
Object[] obldim = new Object[ildim.Length];
Array.Copy(ildim, obldim, ildim.Length);
```

`Array.Copy` 的作用不仅仅是将元素从一个数组复制到另一个。`Copy`方法还能正确处理内存的重叠区域，就像 C 的 `memmove` 函数一样。有趣的是， C 的 `memcpy` 函数反而不能正确处理处理重叠的内存区域。`Copy`方法还能在复制每个数组元素时进行必要的类型转换，具体如下所述：

* 将值类型的元素装箱为引用类型的元素，比如将一个 `Int32[]` 复制到一个 `Object[]` 中。

* 将引用类型的元素拆箱为值类型的元素，比如将一个 `Object[]` 复制到一个 `Int32[]` 中。

* 加宽 CLR 基元值类型，比如将一个 `Int32[]` 的元素复制到一个 `Double[]` 中。

* 在两个数组之间复制时，如果仅从数组类型证明不了两者的兼容性，比如从 `Object[]` 转型为 `IFormattable[]`，就根据需要对元素进行向下类型转换。如果`Object[]`中的每个对象都实现了`IFormattable`，`Copy`方法就能成功执行。

下面演示了 `Copy` 方法的另一种用法：

```C#
// 定义实现了一个接口的值类型
internal struct MyValueType : IComparable {
    public Int32 CompareTo(Object obj){
        ...
    }
}

public static class Program {
    public static void Main() {
        // 创建含有 100 个值类型的数组
        MyValueType[] src = new MyValueType[100];

        // 创建 IComparable 引用数组
        IComparable[] dest = new IComparable[src.Length];

        // 初始化 IComparable 数组中的元素，
        // 使它们引用源数组元素的已装箱版本
        Array.Copy(src, dest, src.Length);
    }
}
```

地球人都能猜得到，FCL 频繁运用了 `Array` 的 `Copy` 方法。

有时确实需要将数组从一种类型转换为另一种类型。这种功能称为**数组协变性**(array covariance)。但在利用它时要清楚由此而来的性能损失。假设有以下代码：

```C#
String[] sa = new String[100];
Object[] oa = sa;           // oa 引用一个 String 数组
oa[5] = "Jeff";             // 性能损失：CLR 检查 oa 的元素类型是不是 String；检查通过
oa[3] = 5;                  // 性能损失：CLR 检查 oa 的元素类型是不是 Int32；发现有错，
                            // 抛出 ArrayTypeMismatchException 异常
```

在上述代码中，`oa` 变量被定义为 `Object[]` 类型，但实际引用的是一个 `String[]`。编译器允许代码将 5 放到数组元素中，因为 5 是 `Int32`,而 `Int32` 派生自 `Object`。虽然编译能通过，但 CLR 必须保证类型安全。对数组元素赋值时，它必须保证赋值的合法性。所以，CLR 必须在运行时检查数组包含的是不是 `Int32` 元素。在本例中，在本例中，答案是否定的，所有不允许赋值；CLR 抛出 `ArrayTypeMismatchException` 异常。

