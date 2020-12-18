# 第 4 章 类型基础

本章内容：
* <a href="#4_1">所有类型都从 System.Object</a>
* <a href="#4_2">类型转换</a>
* <a href="#4_3">命名空间和程序集</a>
* <a href="#4_4">运行时的相互关系</a>

本章讲述使用类型和 CLR 时需掌握的基础知识。具体地说，要讨论所有类型都具有对的一组基本行为。还将讨论类型安全性、命名空间、程序集以及如何将对象从一种类型转换成另一种类型。本章最后会解释类型、对象、线程栈和托管堆在运行时的相互关系。

## <a name="4_1">4.1 所有类型都从 System.Object 派生</a>

“运行时”要求每个类型最终都从 `System.Object` 类型派生。也就是说，以下两个类型定义完全一致：

```C#
// 隐式派生自 Object
class Emplpyee {
    …
} 

// 显示派生自 Object
class Emplpyee : System.Object {
    …
} 
```

由于所有类型最终都从 `System.Object` 派生，所以每个类型的每个对象都保证了一组最基本的方法。具体地说， `System.Object` 类提供了如表 4-1 所示的公共实例方法。

表 4-1 System.Object 的公共方法
|公共方法|说明|
|:---:|:---:|
|`Equals`|如果两个对象具有相同的值，就返回 `true`。欲知该方法的详情，请参见 5.3.2 节“对象相等性和同一性”|
|`GetHashCode`|返回对象的值的哈希码。如果某个类型的对象要在哈希表集合(比如 `Dictionary`)中作为建使用，类型应重写该方法。方法应该为不同对象提供 *良好分布* 。将这个方法设计到 `Object` 中并不恰当。大多数类型永远不会在哈希表中作为键使用；该方法本该在接口中定义。欲知该方法的详情，请参见 5.4 节“对象哈希码”|
|`ToString`|默认返回类型的完整名称(`this.GetType().FullName`)。但经常重写该方法来返回包含对象状态表示的 `String` 对象。例如，核心类型(如 `Boolean` 和 `Int32`)重写该方法来返回它们的值的字符串表示。另外，经常出于调试的目的而重写该方法；调用后获得一个字符串，显示对象各字段的值。事实上，Microsoft Visual Studio 的调试器会自动调用该函数来显示对象的字符串表示。注意，`ToString` 理论上应察觉与调用线程关联的 `CultureInfo` 并采用相应行动。第 14 章“字符、字符串和文本处理”将更详细地讨论 `ToString`|
|`GetType`|返回从 `Type` 派生的一个类型的实例，指出调用 `GetType` 的那个对象是什么类型。返回的 `Type` 对象可以和反射类配合，获取与对象的类型有关的元数据信息。反射将在第 23 章“程序集加载和反射”讨论。 `GetType` 是非虚方法，目的是防止类重写该方法，隐瞒其类型，进而破坏类型安全性|
> 所谓“良好分布”，是指针对所有输入，`GetHashCode` 生成的哈希值应该在所有整数中产生一个随机的分布。——译注

此外，从 `System.Object` 派生的类型能访问如表 4-2 所示的受保护方法。

表 4-2 System.Object 的受保护方法 
|受保护方法|说明|
|:---:|:---:|
|`MemberwiseClone`|这个非虚方法创建类型的新实例，并将新对象的实例字段设与 `this` 对象的实例字段完全一致。返回对新 *实例* 的引用|
|`Finalize`|在垃圾回收器判断对象应该作为垃圾被回收之后，在对象的内存被实际回收之前，会调用这个虚方法。需要在回收内存前执行清理工作的类型应重写该方法。第 21 章“托管堆和垃圾回收”会更详细地讨论这个重要的方法|
> 作者在这段话里引用了两种不同的“实例”。一种是类的实例，也就是对象；另一种是类中定义的实例字段。所谓“实例字段”，就是指非静态字段，有时也称为“实例成员”。简单地说，实例成员属于类的对象，静态成员属于类。——译注

CLR 要求所有对象都用 `new` 操作符创建。以下代码展示了如何创建一个 `Employee` 对象：

```C#
Employee e = new Employee("ConstructorParam1");
```

以下是 `new` 操作符所做的事情。

1. 计算类型及其所有基类型(一直到 `System.Object`，虽然它没有定义自己的实例字段)中定义的所有实例字段需要的字节数。堆上每个对象都需要一些额外的成员，包括“类型对象指针”(type object pointer)和“同步块索引”(sync block index)。CLR 利用这些成员管理对象。额外成员的字节数要计入对象大小。
> 称为 overhead 成员，或者说“开销成员”。——译注

2. 从托管堆中分配类型要求的字节数，从而分配对象的内存，分配的所有字节都设为零(0).
3. 初始化对象的“类型对象指针”和“同步块索引”成员。
4. 调用类型的实例构造器，传递在 `new` 调用中指定的实参(上例就是字符串"**ConstructorParam1**")。大多数编译器都在构造器中自动生成代码来调用基类构造器。每个类型的构造器都负责初始化该类型定义的实例字段。最终调用 `System.Object` 的构造器，该构造器什么都不做，简单地返回。

`new` 执行了所有这些操作之后，返回指向新建对象一个引用(或指针)。在前面的示例代码中，该引用保存到变量 `e` 中，后者具有 `Employee` 类型。

顺便说一句，没有和 `new` 操作符对应的 `delete` 操作符；换言之，没有办法显示释放为对象分配的内存。 CLR 采用了垃圾回收机制(详情在第 21 章讲述)，能自动检测到一个对象不再被使用或访问，并自动释放对象的内存。

## <a name="4_2">4.2 类型转换</a>

CLR 最重要的特性之一就是类型安全。在运行时，CLR 总是知道对象的类型是什么。调用 `GetType` 方法即可知道对象的确切类型。由于它是非虚方法，所以一个类型不可能伪装成另一个类型。例如， `Employee` 类型不能重写 `GetType` 方法并返回一个 `SuperHero` 类型。

开发人员经常需要将对象从一种类型转换为另种类型。CLR 允许将对象转换为它的(实际)类型或者它的任何基类型。每种编程语言都规定了开发人员具体如何进行这种转型操作。例如，C# 不要求任何特殊语法即可将对象转换为它的任何基类型，因为向基类型的转换被认为是一种安全的隐式转换。然而，将对象转换为它的某个派生类型时， C# 要求开发人员只能进行显示转换，因为这种转换可能在运行时失败。以下代码演示了向基类型和派生类型的转换：

```C#
// 该类型隐式派生自 System.Object
    internal class Employee {
        ···
    }

    public sealed class Program {
        public static void Main(){
            // 不需要转型，因为 new 返回一个 Employee对象，
            // 而 Object 是 Employee 的基类型
            Object o = new Employee();

            // 需要转型，因为 Employee 派生自 Object 
            // 其他语言(比如 Visual Basic)也许不要求像
            // 这样进行强制类型转换
            Employee e = (Employee) o;
        }
    }
```

这个例子展示了你需要做什么，才能让编译器顺利编译这些代码。接着，让我们看看运行时发生的事情。在运行时，CLR 检查转型操作，确定总是转换为对象的实际类型或者它的任何基类型。例如，以下代码虽然能通过编译，但会在运行时抛出 `InvalidCastException` 异常：

```C#
    internal class Employee {
        ···
    }

    internal class Manager : Employee {
        ···
    }

    public sealed class Program {
        public static void Main(){
            // 构造一个 Manager 对象，把它传给 PromoteEmployee，
            // Manager “属于”(IS-A) Employee，所以 PromoteEmployee 能成功运行
            Manager m = new Manager();

            // 构造一个 DateTime 对象，把它传给 PromoteEmployee。
            // DateTime 不是从 Employee 派生的，所以 PromoteEmployee
            // 抛出 System.InvalidCastException 异常
            DateTime newYears = new DateTime(2020, 1, 1);
            PromoteEmployee(newYears);
        }

        public static void PromoteEmployee(Object o){
            // 编译器在编译时无法准确地获知对象 o
            // 引用的是什么类型，因此编译器允许代码
            // 通过编译。但在运行时，CLR 知道了 o 引用
            // 的是什么类型(在每次执行转型的时候)，
            // 所以它会核实对象的类型是不是 Employee 或者
            // 从 Employee 派生的任何类型
            Employee  e = (Employee) o;
        }
    }
```

`Main` 构造一个 `Manager` 对象并将其传给 `PromoteEmployee`。这些代码能成功编译并运行，因为 `Manager` 最终从 `Object` 派生，而 `PromoteEmployee` 期待的正是一个 `Object`。进入 `PromoteEmployee` 内部之后， CLR 核实对象 `o` 引用的就是一个 `Employee` 对象，或者是从 `Employee` 派生的一个类型的对象。由于 `Manager` 从 `Employee` 派生，所以 CLR 执行类型转换，允许 `PromoteEmployee` 继续执行。

`PromoteEmployee` 返回后，`Main` 构造一个 `DateTime` 对象，将其传给 `PromoteEmployee`。