# 第 23 章 程序集加载和反射  

本章内容

* <a href="#23_1">程序集加载</a>
* <a href="#23_2">使用反射构建动态可扩展应用程序</a>
* <a href="#23_3">反射的性能</a>
* <a href="#23_4">设计支持加载项的应用程序</a>
* <a href="#23_5">使用反射发现类型的成员</a>

本章讨论了在编译时对一个类型一无所知的情况下，如何在运行时发现类型的信息、创建类型的实例以及访问类型的成员。可利用本章讲述的内容创建动态可扩展应用程序。在这种情况下，一般是由一家公司创建宿主应用程序，其他公司创建加载项(add-in)来扩展宿主应用程序。宿主不能基于一些具体的加载项来构建和测试，因为加载项由不同公司创建，而且极有可能是在宿主应用程序发布之后才创建的。这是宿主为什么要在运动时发现加载项的原因。

动态可扩展应用程序可利用第 22 章讲述的 CLR 寄宿和 AppDomain。宿主可以在一个 AppDomain 中运行加载项代码，这个 AppDomain 有它自己的安全性和配置设置。宿主还可通过卸载 AppDomain 来卸载加载项。在本章末尾，将花费一点时间来讨论如何将所有这些功能组合到一起————包括 CLR 寄宿、AppDomain、程序集加载、类型发现、实例实例构造和反射————从而构建健壮、安全而且可以动态扩展的应用程序。

> 重要提示 .NET Framework 4.5 引入了新的反射 API。旧的 API 缺点太多。例如，它对 LINQ 的支持不好，内建的策略对某些语言来说不正确，有时不必要地强制加载程序集，而且为很少遇到的问题提供了过于复杂的 API。新 API 解决了所有这些问题。但至少就 .NET 4.5 来说，新的反射 API 还不如旧 API 完整。利用新 API 和 `System.Reflection.RuntimeReflcetionExtensions` 类中的扩展方法，现在所有事情都可以做到。希望在 .NET Framework 未来的版本中为新 API 添加更多的方法。当然，对于桌面应用程序，旧 API 是仍然存在的，重新编译现有的代码不会出任何问题。但新 API 是未来的发展方向，这正是本章要全面讨论新 API 的原因。Windows Store 应用由于不用考虑向后兼容，所以必须使用新 API。

## <a name="23_1">23.1 程序集加载</a>

我们知道，JIT 编译器将方法的 IL 代码编译成本机代码时，会查看 IL 代码中引用了哪些类型。在运行时，JIT 编译器利用程序集的 TypeRef 和 AssemblyRef 元数据表来确定哪一个程序集定义了所引用的类型。在 AssemblyRef 元数据表的记录项中，包含了构成程序集强名称的各个部分。JIT 编译器尝试将与该标识匹配的程序集加载到 AppDomain 中(如果还没有加载的话)。如果被加载的程序集是弱命名的，那么标识中就只包含程序集的名称(不包含版本、语言文化及公钥标记信息)。<sup>①</sup>

> ① 强命名程序集和弱命名程序集的区别请参见 3.1 节 “两种程序集，两种部署”。 ———— 译注

在内部，CLR 使用 `System.Reflection.Assembly` 类的静态 `Load` 方法尝试加载这个程序集。该方法在 .NET Framework SDK 文档中是公开的，可调用它显式地将程序集加载到 AppDomain 中。该方法是 CLR 的与 Win32 `LoadLibrary` 函数等价的方法。`Assembly` 的 `Load` 方法实际有几个重载版本。以下是最常用的重载的原型：

```C#
public class Assembly {
    public static Assembly Load(AssemblyName assemblyRef);
    public static Assembly Load(String assemblyString);
    // 未列出不常用的 Load 重载
}
```

在内部，`Load` 导致 CLR 向程序集应用一个版本绑定重定向策略，并在 GAC(全局程序集缓存)中查找程序集。如果没找到，就接着去应用程序的基目录、私有路径子目录和 `codebase`<sup>②</sup>位置查找。如果调用 `Load` 时传递的是弱命名程序集，`Load` 就不会向程序集应用版本绑定重定向策略，CLR 也不会去 GAC 查找程序集。如果 `Load` 找到指定的程序集，会返回对代表已加载的那个程序集的一个 `Assembly` 对象的引用。如果 `Load` 没有找到指定程序集，会抛出一个 `System.IO.FileNotFoundException` 异常。

>> ② 要了解 `codeBase` 元素指定的位置，请参见 3.9 节“高级管理控制(配置)”。 ———— 译注

> 注意 一些极罕见的情况可能需要加载为特定 CPU 架构生成的程序集。这时在指定程序集的标识时，还可包括一个进程架构部分。例如，假定 GAC 中同时包含了一个程序集的 IL 中立版本和 x86 专用版本，CLR 会默认选择 x86 专用版本(参见 第 3 章 “共享程序集和强命名程序集”)。但是，为了强迫 CLR 加载 IL 中立的版本，可以向 `Assembly` 的 `Load` 方法传递以下字符串：

> `"SomeAssembly, Version=2.0.0.0, Culture=neutral, PublicKeyToken=01234567890abcde, ProcessorArchitecture=MSIL"`

> CLR 目前允许 `ProcessorArchitecture` 取 5 个值之一： `MSIL`(Microsoft IL)、`X86`、`IA64`，`AMD64` 以及 `Arm`。

> 重要提示 一些开发人员可能注意到 `System.AppDomain` 提供了 `Load` 方法。和 `Assembly` 的静态 `Load` 方法不同，`AppDomain` 的`Load` 是实例方法，它允许将程序集加载到指定的 AppDomain 中。该方法设计由非托管调用，允许宿主将程序集“注入” 特定 AppDomain 中。托管代码的开发人员一般情况下不应调用它，因为调用 `AppDomain` 的 `Load` 方法时需要传递一个标识了程序集的字符串。该方法随后会应用策略，并在一些常规位置搜索程序集。我们知道，AppDomain 关联了一些告诉 CLR 如何查找程序集的设置。为了加载这个程序集，CLR 将使用与指定 AppDomain 关联的设置，而非与发出调用之 AppDomain 关联的设置。

> 但 `AppDomain` 的 `Load` 方法会返回对程序集的引用。由于 `System.Assembly` 类不是从 `System.MarshalByRefObject` 派生的，所以程序集对象必须按值封送回发出调用的那个 AppDomain。但是，现在 CLR 就会用发出调用的那个 AppDomain 的设置来定位并加载程序集。如果使用发出调用的那个 AppDomain 的策略和搜索位置找不到指定的程序集，就会抛出一个 `FileNotFoundException`。 这个行为一般不是你所期望的，所以应该避免使用 `AppDomain` 的 `Load` 方法。

在大多数动态可扩展应用程序中，`Assembly` 的 `Load` 方法是将程序集加载到 AppDomain 的首选方法。但它要求事先掌握构成程序集标识的各个部分。开发人员经常需要写一些工具或实用程序(例如 ILDasm.exe，PEVerify.exe，CorFlags.exe，GACUtil.exe，SGen.exe，SN.exe 和 XSD.exe 等)来操作程序集，它们都要获取引用了程序集文件路径名(包括文件扩展名)的命令行实参。

调用 `Assembly` 的 `LoadFrom` 方法加载指定了路径名的程序集：

```C#
public class Assembly {
    public static Assembly LoadFrom(String path);
    // 未列出不常用的 LoadFrom 重载
}
```

在内部 `LoadFrom` 首先调用 `System.Refection.AssemblyName` 类的静态 `GetAssemblyName` 方法，该方法打开指定的文件，找到 `AssemblyRef` 元数据表的记录项，提取程序集标识信息，然后以一个 `System.Reflection.AssemblyName` 对象的形式返回这些信息(文件同时会关闭)。随后，`LoadFrom` 方法在内部调用 `Assembly` 的 `Load` 方法，将 `AssemblyName` 对象传给它。然后，CLR 应用版本绑定重定向策略，并在各个位置查找匹配的程序集。`Load` 找到匹配程序集会加载它，并返回代表已加载程序集的 `Assembly` 对象；`LoadFrom` 方法将返回这个值。如果 `Load` 没有找到匹配的程序集，`LoadFrom` 会加载通过 `LoadFrom` 的实参传递的路径中的程序集。当然，如果已加载了具有相同标识的程序集，`LoadFrom` 方法就会直接返回代表已加载程序集的 `Assembly` 对象。

顺便说一句，`LoadFrom` 方法允许传递一个 URL 作为实参，下面是一个例子：

`Assembly a = Assembly.LoadFrom(@"http://Wintellect.com/SomeAssembly.dll");`

如果传递的是一个 Internet 位置，CLR 会下载文件，把它安装到用户的下载缓存中，再从哪儿加载文件。注意，当前必须联网，否则会抛出异常。但如果文件之前已下载过，而且 Microsoft Internet Explorer 被设为脱机工作(在 Internet Explorer 中单击 “文件”|“脱机工作”)，就会使用以前下载的文件，不会抛出异常。还可以调用 `UnsafeLoadFrom`，它能够加载从网上下载的程序集，同时绕过一些安全检查。

> 重要提示 一台机器可能同时存在具有相同标识的多个程序集。由于 `LoadFrom` 会在内部调用 `Load`，所以 CLR 有可能不是加载你指定的文件，而是加载一个不同的文件，从而造成非预期的行为。强烈建议每次生成程序集时都更改版本号，确保每个版本都有自己的唯一性标识，确保 `LoadFrom` 方法的行为符合预期。

Microsoft Visual Studio 的 UI 设计人员和其他工具一般用的是 `Assembly` 的 `LoadFile` 方法。这个方法可从任意路径加载程序集，而且可以将具有相同标识的程序集多次加载到一个 AppDomain 中。在设计器/工具中对应用程序的 UI 进行了修改，而且用户重新生成了程序集时，便有可能发生这种情况。通过 `LoadFile` 加载程序集时，CLR 不会自动解析任何依赖性问题；你的代码必须向`AppDomain` 的 `AssemblyResolve` 事件登记，并让事件回调方法显式地加载任何依赖的程序集。

如果你构建的一个工具只想通过反射(本章稍后进行讨论)来分析程序集的元数据，并希望确保程序集中的任何代码都不会执行，那么加载程序集的最佳方式就是使用 `Assembly` 的 `ReflectionOnlyLoadFrom` 方法或者使用 `Assembly` 的 `ReflectionOnlyLoad` 方法(后者比较少见)。下面是这两个方法的原型：

```C#
public class Assembly {
    public static Assembly ReflectionOnlyLoadFrom(String assemblyFile);
    public static Assembly ReflectionOnlyLoad(String assemblyString);
    // 未列出不常用的 ReflectionOnlyLoad 重载
}
```

`ReflectionOnlyLoadFrom` 方法加载由路径指定的文件；文件的强名称标识不会获取，也不会在 GAC 和其他位置搜索文件。 `ReflectionOnlyLoad` 方法会在 GAC、应用程序基目录、私有路径和 `codebase` 指定的位置搜索指定的程序集。但和 `Load` 方法不同的是，`ReflectionOnlyLoad` 方法不会应用版本控制策略，所以你指定的是哪个版本，获得的就是哪个版本。要自行向程序集标识应用版本控制策略，可将字符串传给 `AppDomain` 的 `ApplyPolicy` 方法。

用 `ReflectionOnlyLoadFrom` 或 `ReflectionOnlyLoad` 方法加载程序集时，CLR 禁止程序集中的任何代码执行；试图执行由这两个方法加载的程序集中的代码，会导致 CLR 抛出一个 `InvalidOperationException` 异常。这两个方法允许工具加载延迟签名的程序集<sup>①</sup>，这种程序集正常情况下会因为安全权限不够而无法加载。另外，这种程序集也可能是为不同的 CPU 架构而创建的。

> ① 要进一步了解延迟签名的程序集，请参见 3.6 节“延迟签名”。 ———— 译注

利用反射来分析由这两个方法之一加载的程序集时，代码经常需要向 `AppDomain` 的 `ReflectionOnlyAssemblyResolve` 事件注册一个回调方法，以便手动加载任何引用的程序集(如果必要，还需要调用 `AppDomain` 的 `ApplyPolicy` 方法)；CLR 不会自动帮你做这个事情。回调方法被调用(invoke)时，它必须调用(call) `Assembly` 的 `ReflectionOnlyLoadFrom` 或 `ReflectionOnlyLoad` 方法来显式加载引用的程序集，并返回对该程序集的引用。

> 注意 经常有人问到程序集卸载的问题。遗憾的是，CLR 不提供卸载单独程序集的能力。如果 CLR 允许这样做，那么一旦线程从某个方法返回至已卸载的一个程序集中的代码，如果允许应用程序以这样的一种方式崩溃，就和它的设计初衷背道而驰了。卸载程序集必须卸载包含它的整个 AppDomain。这方面的详情已在第 22 章“CLR 寄宿和 AppDomain”进行了讨论。

> 使用 `ReflectionOnlyLoadFrom` 或 `ReflectionOnlyLoad` 方法加载的程序集表面上是可以卸载的。毕竟，这些程序集中的代码是不允许执行的。但 CLR 一样不允许执行的。但 CLR 一样不允许卸载用这两个方法加载的程序集。因为用这两个方法加载了程序集之后，仍然可以利用反射来创建对象，以便引用这些程序集中定义的元数据。如果卸载程序集，就必须通过某种方式使这些对象，以便引用这些程序集中定义的元数据。仍然可以利用反射来创建对象，以便引用这些程序集中定义的元数据。如果卸载程序集，就必须通过某种方式使这些对象失效。无论是实现的复杂性，还是执行速度，跟踪这些对象的状态都是得不偿失的。

许多应用程序都由一个要依赖于众多 DLL 文件的 EXE 文件构成。部署应用程序时，所有文件都必须部署。但有一个技术允许只部署一个 EXE 文件。首先标识出 EXE 文件。首先标识出 EXE 文件要依赖的、不是作为 Microsoft .NET Framework 一部分发布的所有 DLL 文件。然后将这些 DLL 添加到 Visual Studio 项目中。对于添加的每个 DLL，都显示它的属性，将它的“生成操作“更改为”嵌入的资源“。这会导致 C# 编译器将 DLL 文件嵌入 EXE  文件中，以后就只需部署这个 EXE。

在运行时，CLR 会找不到依赖的 DLL 程序集。为了解决这个问题，当应用程序初始化时，向 `AppDomain` 的 `ResolveAssembly` 事件登记一个回调方法。代码大致如下：

```C#
private static Assembly ResolveEventHandler(Object sender, ResolveEventArgs args) {
    String dllName = new AssemblyName(args.Name).Name + ".dll";

    var assem = Assembly.GetExecutingAssembly();
    String resourceName = assem.GetManifestResourceNames().FirstOrDefault(rn => rn.EndsWith(dllName));
    if(resourceName == null) return;    // Not found, maybe another handler will find it 
    using (var stream = assem.GetManifestResourceStream(resourceName)) {
        Byte[] assemblyData = new Byte[stream.Length];
        stream.Read(assemblyData, 0, assemblyData.Length);
        return Assembly.Load(assemblyData);
    }
}
```

现在，线程首次调用一个方法时，如果发现该方法引用了依赖 DLL 文件中的类型，就会引发一个 `AssemblyResolve` 事件，而上述回调代码会找到所需的嵌入 DLL 资源，并调用 `Assembly` 的 `Load` 方法获取一个 `Byte[]` 实参的重载版本来加载所需的资源。虽然我喜欢将依赖 DLL 程序集的技术，但要注意这会增大应用程序在运行时的内存消耗。

## <a name="23_2">23.2 使用反射构建动态可扩展应用程序</a>

众所知周，元数据是用一系列表来存储的。生成程序集或模块时，编译器会创建一个类型定义表、一个字段定义表、一个方法定义表以及其他表。利用 `System.Reflection` 命名空间中的其他类型，可以写代码来反射(或者说”解析“)这些元数据表。实际上，这个命名空间中类型为程序集或模块中包含的元数据提供了一个对象模型。

利用对象模型中的类型，可以轻松枚举类型定义元数据表中的所有类型，而针对每个类型都可获取它的基类型、它实现的接口以及与类型关联的标志(flag)。利用 `System.Reflection` 命名空间中的其他类型，还可解析对应的元数据表来查询类型的字段、方法、属性和事件。还可发现应用于任何元数据实体的定制特性(详情参见第 18 章”定制特性“)。甚至有些类允许判断引用的程序集；还有一些方法能返回一个方法的 IL 字节流。利用所有这些信息，很容易构建出与 Microsoft 的 ILDasm.exe 相似的工具。

> 注意 有的反射类型及其成员时专门由 CLR 编译器的开发人员使用的。应用程序的开发人员一般用不着。FCL 文档没有明确指出哪些类型和成员供编译器开发人员(而非应用程序开发人员)使用，但只要意识到有些反射类型及其成员不适合所有人使用，该文档时就会更清醒一些。

事实上，只有极少数应用程序才需使用反射类型。如果类库需要理解类型的定义才能提供丰富的功能，就适合使用反射。例如，FCL 的序列化机制(详情参见第 24 章”运行时序列化”)就是利用反射来判断类型定义了哪些字段。然后，序列化格式器(serialization formatter)可获取这些字段的值，把它们写入字节流以便通过 Internet 传送、保存到文件或复制到剪贴板。类似地，在设计期间，Microsoft Visual Studio 设计器在 Web 窗体或 Windows 窗体上放置控件时，也利用反射来决定要向开发人员显示的属性。

在运行时，当应用程序需要从特定程序集中加载特定类型以执行特定任务时，也要使用反射。例如，应用程序可要求用户提供程序集和类别名。然后，应用程序可显式加载程序集，构造类型的实例，再调用类型中定义的方法。这种用法在概念上类似于调用 Win32 `LoadLibrary` 和 `GetProcAddress` 函数。以这种用法在概念上类似于调用 Win32 `LoadLibrary` 和 `GetProAddress` 函数。以这种方式绑定到类型并调用方法称为**晚期绑定**。(对应的，早期绑定是指在编译时就确定应用程序要使用的类型和方法。)

## <a name="23_3">23.3 反射的性能</a>

反射是相当强大的机制，允许在运行发现并使用编译时还不了解的类型及其成员。但是，它也有下面两个缺点。

* 反射造成编译时无法保证类型安全性。由于反射严重依赖字符串，所以会丧失编译时的类型安全性。例如，执行 `Type.GetType("int")`；要求通过反射在程序集中查找名为“int”的类型，代码会通过编译，但在运行时会返回 `null`，因为 CLR 只知 `"System.Int32"`，不知道`"int"`。

* 反射速度慢。使用反射时，类型及其成员的名称在编译时未知；你要用字符串名称标识每个类型及其成员，然后在运行时发现它们。也就是说，使用 `System.Reflection` 命名空间中的类型扫描程序集的元数据时，反射机制会不停地执行字符串搜索。通常，字符串搜索执行的是不区分大小写的比较，这会进一步影响速度。

使用反射调用成员也会影响性能。用反射调用方法时，首先必须将实参打包(pack)成数组；在内部，反射必须将这些实参解包(unpack)到线程栈上。此外，在调用方法前，CLR 必须检查实参具有正确的数据类型。最后，CLR 必须确保调用者有正确的安全权限来访问被调用的成员。

