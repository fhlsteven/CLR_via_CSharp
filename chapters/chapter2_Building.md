# 第 2 章 生成、打包、部署和管理应用程序及类型

本章内容：
* <a href="#2_1">.NET Framework 部署目标</a>
* <a href="#2_2">将类型生成到模块中</a>
* <a href="#2_3">元数据概述</a>
* <a href="#2_4">将模块合并成程序集</a>
* <a href="#2_5">程序集版本资源信息</a>
* <a href="#2_6">语言文化</a>
* <a href="#2_7">简单应用程序部署(私有部署的程序集)</a>
* <a href="#2_8">简单管理控制(配置)</a>

在解释如何为 Microsoft .Net Framework 开发程序之前，首先讨论一下生成、打包和部署应用程序及其类型的步骤。本章重点解释如何生成仅供自己的应用程序使用的程序集。第 3 章“共享程序集和强命名程序集”将讨论更高级的概念，包括如何生成和使用程序集，使其中包含的类型能由多个应用程序共享。这两章会谈及管理员能采用什么方式来影响应用程序及其类型的执行。

当今的应用程序都由多个类型构成，这些类型通常是由你的和 Microsoft 创建的。除此之外，作为一个新兴产业，组件厂商们也纷纷着手构建一些专用类型，并将其出售给各大公司，以缩短软件项目的开发时间。开发这些类型时，如果使用的语言是面向 CLR 的，这些类型就能无缝地共同工作。换言之，用一种语言写的类型可以将另一个类型作为自己的基类使用，不用关心基类用什么语言开发。

本章将解释如何生成这些类型，并将其打包到文件中以进行部署。另外，还会提供一个简短的历史回顾，帮助开发人员理解 .NET Framework 希望解决的某些问题。

## <a name="2_1">2.1 .NET Framework 部署目标</a>

Windows 多年来一直因为不稳定和过于复杂而口碑不佳。不管对它的评价对不对，之所以造成这种状况，要归咎于几方面的原因。首先，所有应用程序都使用来自 Microsoft 或其他厂商的动态链接库(Dynamic-Link Library,DLL)，由于应用程序要执行多个厂商的代码，所以任何一段代码的开发人员都不能百分之百保证别人以什么方式使用这段代码。虽然这种交互可能造成各种各样的麻烦，但实际一般不会出太大问题，因为应用程序在部署前会进行严格测试和调试。

但对于用户，当一家公司决定更新其软件产品的代码，并将新文件发送给他们时，就可能出问题。新文件理论上应该向后兼容以前的文件，但谁能对此保证呢？事实上，一家厂商更新代码时，经常都不可能重新测试和调试之前发布的所有应用程序，无法保证自己的更改不会造成不希望的结果。

很多人都可能遭遇过这样的问题：安装新应用程序时，它可能莫名其妙破坏了另一个已经安装好的应用程序。这就是所谓的“DLL hell”。这种不稳定会对普通计算机用户带来不小的困扰。最终结果是用户必须慎重考虑是否安装新软件。就像我个人来说，有一些重要的应用程序是平时经常都要用到的，为了避免对它们产生不好的影响，我不会冒险去”尝鲜“。

造成 Windows 口碑不佳的第二个原因是安装的复杂性，如今，大多数应用程序在安装时都会影响到系统的全部组件。例如，安装一个应用程序会将文件复制到多个目录，更新注册表设置，并在桌面和”开始“菜单上安装快捷方式。问题是，应用程序不是一个孤立的实体。应用程序备份不易，因为必须复制应用程序的全部文件以及注册表中的相关部分。除此之外，也不能轻松地将应用程序从一台机器移动到另一台机器。只有再次运行安装程序，才能确保所有文件和注册表设置的正确性。最后，即使卸载或移除了应用程序，也免不了担心它的一部分内容仍潜伏在我们的机器中。

第三个原因涉及到安全性。应用程序安装时会带来各种文件，其中许多是由不用的公司开发的。此外， Web 应用程序经常悄悄下载一些代码(比如 ActiveX 控件)，用户根本注意不到自己打的机器上安装了这些代码。如今，这种代码能够执行任何操作，包括删除文件或者发送电子邮件。用户完全有理由害怕安装新的应用程序，因为它们可能造成各种各样的危害。考虑到用户的感受，安全性必须集成到系统中，使用户能够明确允许或禁止各个公司开发的代码访问自己的系统资源。

阅读本章和下一章可以知道， .NET Framework 正常尝试彻底解决 DLL hell 的问题。另外， .NET Framework 还在很大程度上解决了应用程序状态在用户硬盘上四处分散的问题。例如，和 COM 不同，类型不再需要注册表中的设置。但遗憾的是，应用程序还是需要快捷方式。安全性方面，.NET Framework 包含称为”代码访问安全性“(Code Access Security)的安全模型。 Windows 安全性基于用户身份，而代码访问安全性允许宿主设置权限，控制加载的组件能做的事情。像 Microsoft SQL Server 这样的宿主应用程序只能将少许权限授予代码，而本地安装的(自宿主)应用程序可获得完全信任(全部权限)。以后会讲到，.NET Framework 允许用户灵活地控制那些东西能够安装，那些东西能够运行。他们对自己机器的控制上升到一个前所未有的高度。

## <a name="2_2">2.2 将类型生成到模块中</a>

本节讨论如何将包含多个类型的源代码文件转变为可以部署的文件。先看下面这个简单的应用程序。

```C#
public sealed class Program {
  public static void Main(){
      System.Console.WriteLine("Hi");
  }
}
```

该应用程序定义了 `Program` 类型，其中有名为 `Main` 的 `public static` 方法。`Main` 中引用了另一个类型 `System.Console`。 `System.Console` 是 Microsoft 实现好的类型，用于实现这个类型的各个方法的IL 代码存储在 `MSCorLib.dll` 文件中。总之，应用程序定义了一个类型，还使用了其他公司提供的类型。

为了生成这个示例应用程序，请将上述代码放到一个源代码文件中(假定为Program.cs)，然后再命令行执行以下命令： 

>`csc.exe /out:Program.exe /t:exe /r:MSCorLib.dll Program.cs`  

这个命令行指示 C# 编译器生成名为 Program.exe 的可执行文件 (`/out:Prpgram.exe`)。生成的文件是 Win32 控制台应用程序类型 (`/t\[arget\]:exe`)。  

C# 编译器处理源文件时，发现代码引用了 `System.Console`类型的 `WriteLine` 方法。此时，编译器要核实该类型确实存在，它确实有 `WriteLine` 方法，而且传递的实参与方法形参匹配。由于该类型在 C# 源代码中没有定义，所以要顺利通过编译，必须向 C# 编译器提供一组程序集，使它能解析对外部类型的引用。在上述命令行中，我添加了 `/r[eference]:MSCorLib.dll` 开关，告诉编译器在 `MSCorLib.dll` 程序集中查找外部类型。

`MSCorLib.dll` 是特殊文件，它包含所有核心类型，包括 `Byte`，`Char`，`String`，`Int32` 等等。事实上，由于这些类型使用得如此频繁，以至于 C# 编译器会自动引用 `MSCorLib.dll` 程序集。换言之，命令行其实可以简化成下面这样(省略`/r`开关)：  

>`csc.exe /out:Program.exe /t:exe Program.cs`  

此外，由于`/out:Program.exe` 和 `/t:exe` 开关是 C# 编译器的默认规定，所以能继续简化成以下形式：  

>`csc.exe Program.cs`

如果因为某个原因不想 C# 编译器自动引用 `MSCorLib.dll` 程序集，可以使用`/nostdlib` 开关。 Microsoft 生成`MSCorLib.dll` 程序集自身时便使用了这个开关。例如，用以下命令行编译 `Program.cs` 会报错，因为它使用的 `System.Console` 类型是在`MSCorLib.dll`中定义的：  

>`csc.exe /out:Program.exe /t:exe /nostdlib Program.cs`

现在更深入地思考一下 C# 编译器生成的 `Program.exe` 文件。这个文件到底是什么？首先，它是标准PE (可移植执行体，Potable Executable)文件。这意味着运行 32 位或 64 位 Windows 的计算机能加载它，并通过执行某些操作。 Windows 文件支持三种应用程序。生成控制台用户界面(Console User Interface，CUI)应用程序使用`/t:exe` 开关；生成图形用户界面(Graphical User Interface，GUI)应用程序使用`/t:winexe` 开关；生成 Windows Store 应用使用 `/t:appcontainerexe` 开关。

### 响应文件

结束对编译器开关的讨论之前，让我们花点时间了解一下**响应文软**，响应文件是包含一组编译器命令行开关的文本文件。执行 CSC.exe 时，编译器打开响应文件，并使用其中包含的所有开关，感觉就像是这些开关直接在命令行上传递给 CSC.exe。要告诉编译器使用响应文件，在命令行中，请在 @ 符号之后指定响应文件的名称。例如，假定响应文件 MyProject.rsp 包含以下文本：  

```cmd
/out:MyProject.exe
/tartget:winexe
```

 为了让 CSC.exe 使用这些设置，可以像下面这样调用它：

 >`csc.exe @MyProject.rsp CodeFile1.cs CodeFile2.cs`

这就告诉了C# 编译器输出文件的名称和要创建哪种类型的应用程序。可以看出，响应文件能带来一些便利，不必每次编译项目时都手动指定命令行参数。

C# 编译器支持多个响应文件。除了在命令行上显式指定的文件，编译器还会自动查找名为 CSC.rsp 的文件。 CSC.exe运行时，会在 CSC.exe 所在的目录查找全局 CSC.rsp文件。想应用于自己所有项目的设置应放到其中。编译器汇总并使用所有响应文件中的设置。本地和全局响应文件中的某个设置发生冲突，将以本地设置为准。类似地，命令行上显示指定的设置将覆盖本地响应文件中的设置。

.NET Framework 安装时会在`%SystemRoot%\Microsoft.NET\Framework(64)\vX.X.X` 目录中安装默认全局 CSC.rsp 文件(X.X.X是你安装的 .NET Framework 的版本号)。这个文件的最新版本包含以下开关:

> 我搜的`C:\Program Files (x86)\MSBuild\14.0\Bin`

```rsp
# Copyright (c)  Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

# This file contains command-line options that the C#
# command line compiler (CSC) will process as part
# of every compilation, unless the "/noconfig" option
# is specified. 

# Reference the common Framework libraries
/r:Accessibility.dll
/r:Microsoft.CSharp.dll
/r:System.Configuration.dll
/r:System.Configuration.Install.dll
/r:System.Core.dll
/r:System.Data.dll
/r:System.Data.DataSetExtensions.dll
/r:System.Data.Linq.dll
/r:System.Data.OracleClient.dll
/r:System.Deployment.dll
/r:System.Design.dll
/r:System.DirectoryServices.dll
/r:System.dll
/r:System.Drawing.Design.dll
/r:System.Drawing.dll
/r:System.EnterpriseServices.dll
/r:System.Management.dll
/r:System.Messaging.dll
/r:System.Runtime.Remoting.dll
/r:System.Runtime.Serialization.dll
/r:System.Runtime.Serialization.Formatters.Soap.dll
/r:System.Security.dll
/r:System.ServiceModel.dll
/r:System.ServiceModel.Web.dll
/r:System.ServiceProcess.dll
/r:System.Transactions.dll
/r:System.Web.dll
/r:System.Web.Extensions.Design.dll
/r:System.Web.Extensions.dll
/r:System.Web.Mobile.dll
/r:System.Web.RegularExpressions.dll
/r:System.Web.Services.dll
/r:System.Windows.Forms.dll
/r:System.Workflow.Activities.dll
/r:System.Workflow.ComponentModel.dll
/r:System.Workflow.Runtime.dll
/r:System.Xml.dll
/r:System.Xml.Linq.dll
```

由于全局 CSC.rsp 文件引用了列出的所有程序集，所以不必使用 C# 编译器的 `/reference` 开关显示引用这些程序集。这个响应文件为开发人员带来了极大的方便，因为可以直接使用 Microsoft 发布的各个程序集中定义的类型和命名空间，不必每次编译时都指定 `/reference` 编译器开关。

引用所有这些程序集对编译器的速度有一点影响。但是，如果源代码没有引用上述任何程序集定义的类型和成员，就不会影响最终的程序集文件，也不会影响程序的执行性能。

当然，要进一步简化操作，还可在全局 CSC.rsp 文件中添加自己的开关。但这样一来，在其他机器上重现代码的生成环境就比较困难了：在每台用于生成的机器上，都必须以相同方式更新 CSC.rsp。 另外，指定 `/noconfig` 命令开关，编译器将忽略本地和全局 CSC.rsp 文件。

## <a name="2_3">元数据概述</a>

现在，我们知道了创建的是什么类型的 PE 文件。但是， Program.exe 文件中到底有什么？托管 PE 文件由 4 部分构成：PE32(+)头、CLR头、元数据以及 IL。PE32(+)头是 Windows 要求的标准信息。CLR 头是一个小的信息块，是需要 CLR 的模块(托管模块)特有的。这个头包含模块生成时所面向的 CLR 的 major(主)和 minor(次)版本号；一些标志(flag)；一个 **MethodDef** token(稍后详述)，该 token 指定了模块的入口方法(前提是该模块是CUI、GUI 或 Windows Store 执行体)；一个可选的强名称数字签名(将在第3章讨论)。最后，CLR 头还包含模块内部的一些元数据表的大小和偏移量。可以查看 `CorHdr.h` 头文件定义的 `IMAGE_COR20_HEADER` 来了解 CLR 头的具体格式。

元数据是有几个表构成的二级制数据块。有三种表，分别是定义表(definition table)、引用表(reference table)和清单表(manifest table)。表 2-1 总结了模块元数据块中常用的定义表。  

  表 2-1 常用的数据定义表
|元数据定义表名称|说明|
|:---:|:---:|
|ModuleDef|总是包含对模块进行标识的一个记录项。该记录项包含模块文件名和扩展名(不含路径)，以及模块版本ID(形式为编译器创建的GUID)。这样可在保留原始名称记录的前提下自由重命名文件。但强烈反对重命名文件，因为可能妨碍 CLR 在运行时正确定位程序集|
|TypeDef|模块定义的每个类型在这个表中都有一个记录项。每个记录项都包含类型的名称、基类型、一些标志(`public`，`private`等)以及一些索引，这些索引指向 MethodDef 表中该类型的方法、FieldDef 表中该类型的字段、PropertyDef 表中该类型的属性以及 EventDef 表中该类型的事件|
|MethodDef|模块定义的每个方法在这个表中都有一个记录项。每个记录项都包含方法的名称、一些标志(`private`，`public`，`virtual`，`abstract`，`static`，`final` 等)、签名以及方法的 IL 代码在模块中的偏移量。每个记录项还引用了 ParamDef 表中的一个记录项，后者包括与方法参数有关的更多信息|
|FieldDef|模块定义的每个字段在这个表中都有一个记录项。每个记录项都包含标志(`private`，`public`等)、类型和名称|
|ParamDef|模块定义的每个参数在这个表中都有一个记录项。每个记录项都包含标志(`in`，`out`，`retval` 等)、类型和名称|
|EventDef|模块定义的每个事件在这个表中都有一个记录项。每个记录项都包含标志和名称|

编译器编译源代码时，代码定义的任何东西都导致在表 2-1 列出的某个表中创建一个记录项。此外，编译器还会检测源代码引用的类型、字段、方法、属性和事件，并创建相应的元数据表记录项。在创建的元数据中包含一组引用表，它们记录了所引用的内容。表 2-2 总结了常用的引用元数据表。

表 2-2 常用的引用元数据表
|引用元数据表名称|说明|
|:---:|:---:|
|AssemblyRef|模块引用的每个程序集在这个表中都有一个记录项。每个记录项都包含*绑定*该程序集所需的信息：程序集名称(不含路径和扩展名)、版本号、语言文化(culture)以及公钥 token(根据发布者的公钥生成的一个小的哈希值，标识了所引用程序集的发布者)。每个记录项还包含一些标志(flag)和一个哈希值。该哈希值本应作为所引用程序集的二进制数据的校验和来使用。但是，目前 CLR 完全忽略该哈希值，未来的 CLR 可能同样如此 (bind 在文档中有时翻译成“联编“，binder 有时翻译成”联编程序“，本书采用”绑定“和”绑定器“。——译注)|
|ModuleRef|实现该模块所引用的类型的每个 PE 模块在这个表中都有一个记录项。每个记录项都包含模块的文件名和扩展名(不含路径)。可能是别的模块实现了你需要的类型，这个表的作用便是建立同那些类型的绑定关系|
|TypeRef|模块引用的每个类型在这个表中都有一个记录项。每个记录项都包含类型的名称和一个引用(指向类型的位置)。如果类型在另一个类型中实现，引用指向一个 TypeRef 记录项。如果类型在同一个模块中实现，引用指向一个 ModuleDef 记录项。如果类型在调用程序集内的另一个模块中实现，引用指向一个 ModuleRef 记录项。如果类型在不同的程序集中实现，引用指向一个 AssemblyRef 记录项|
|MemberRef|模块引用的每个成员(字段和方法，以及属性方法和事件方法)在这个表中都有一个记录项。每个记录项都包含成员的名称和签名，并指向对成员进行定义的那个类型的 TypeRef 记录项|

除了表 2-1 和表 2-2 所列的，还有其他许多定义表和引用表。但是，我的目的只是让你体会一下编译器在生成的元数据中添加的各种信息。前面提到还有清单(manifest)元数据表，它们将于本章稍后讨论。

可用多种工具检查托管 PE 文件中的元数据。我个人喜欢使用 ILDasm.exe ，即 IL Disassembler(IL 反汇编器)。要查看元数据表，请执行以下命令行：  

> `ILDasm Program.exe`

ILDasm.exe 将运行并加载 Program.exe 程序集。要采用一种美观的、容易阅读的方式查看元数据，请选择”视图“|”元信息“|”显示!“菜单项(或直接按 Ctrl+M 组合键)。随后会显示以下信息：  

```txt
===========================================================
ScopeName : Ch02-1-SimpleProgram.exe
MVID      : {EE1268E9-583A-4AE3-9404-B3439DA8AEB8}
===========================================================
Global functions
-------------------------------------------------------

Global fields
-------------------------------------------------------

Global MemberRefs
-------------------------------------------------------

TypeDef #1 (02000002)
-------------------------------------------------------
	TypDefName: Program  (02000002)
	Flags     : [Public] [AutoLayout] [Class] [Sealed] [AnsiClass] [BeforeFieldInit]  (00100101)
	Extends   : 01000006 [TypeRef] System.Object
	Method #1 (06000001) [ENTRYPOINT]
	-------------------------------------------------------
		MethodName: Main (06000001)
		Flags     : [Public] [Static] [HideBySig] [ReuseSlot]  (00000096)
		RVA       : 0x00002050
		ImplFlags : [IL] [Managed]  (00000000)
		CallCnvntn: [DEFAULT]
		ReturnType: Void
		No arguments.

	Method #2 (06000002) 
	-------------------------------------------------------
		MethodName: .ctor (06000002)
		Flags     : [Public] [HideBySig] [ReuseSlot] [SpecialName] [RTSpecialName] [.ctor]  (00001886)
		RVA       : 0x0000205e
		ImplFlags : [IL] [Managed]  (00000000)
		CallCnvntn: [DEFAULT]
		hasThis 
		ReturnType: Void
		No arguments.


TypeRef #1 (01000001)
-------------------------------------------------------
Token:             0x01000001
ResolutionScope:   0x23000001
TypeRefName:       System.Runtime.CompilerServices.CompilationRelaxationsAttribute
	MemberRef #1 (0a000001)
	-------------------------------------------------------
		Member: (0a000001) .ctor: 
		CallCnvntn: [DEFAULT]
		hasThis 
		ReturnType: Void
		1 Arguments
			Argument #1:  I4

TypeRef #2 (01000002)
-------------------------------------------------------
Token:             0x01000002
ResolutionScope:   0x23000001
TypeRefName:       System.Runtime.CompilerServices.RuntimeCompatibilityAttribute
	MemberRef #1 (0a000002)
	-------------------------------------------------------
		Member: (0a000002) .ctor: 
		CallCnvntn: [DEFAULT]
		hasThis 
		ReturnType: Void
		No arguments.

TypeRef #3 (01000003)
-------------------------------------------------------
Token:             0x01000003
ResolutionScope:   0x23000001
TypeRefName:       System.Diagnostics.DebuggableAttribute
	MemberRef #1 (0a000003)
	-------------------------------------------------------
		Member: (0a000003) .ctor: 
		CallCnvntn: [DEFAULT]
		hasThis 
		ReturnType: Void
		1 Arguments
			Argument #1:  ValueClass DebuggingModes

TypeRef #4 (01000004)
-------------------------------------------------------
Token:             0x01000004
ResolutionScope:   0x01000003
TypeRefName:       DebuggingModes

TypeRef #5 (01000005)
-------------------------------------------------------
Token:             0x01000005
ResolutionScope:   0x23000001
TypeRefName:       System.Runtime.Versioning.TargetFrameworkAttribute
	MemberRef #1 (0a000004)
	-------------------------------------------------------
		Member: (0a000004) .ctor: 
		CallCnvntn: [DEFAULT]
		hasThis 
		ReturnType: Void
		1 Arguments
			Argument #1:  String

TypeRef #6 (01000006)
-------------------------------------------------------
Token:             0x01000006
ResolutionScope:   0x23000001
TypeRefName:       System.Object
	MemberRef #1 (0a000006)
	-------------------------------------------------------
		Member: (0a000006) .ctor: 
		CallCnvntn: [DEFAULT]
		hasThis 
		ReturnType: Void
		No arguments.

TypeRef #7 (01000007)
-------------------------------------------------------
Token:             0x01000007
ResolutionScope:   0x23000001
TypeRefName:       System.Console
	MemberRef #1 (0a000005)
	-------------------------------------------------------
		Member: (0a000005) WriteLine: 
		CallCnvntn: [DEFAULT]
		ReturnType: Void
		1 Arguments
			Argument #1:  String

Assembly
-------------------------------------------------------
	Token: 0x20000001
	Name : Ch02-1-SimpleProgram
	Public Key    :
	Hash Algorithm : 0x00008004
	Version: 0.0.0.0
	Major Version: 0x00000000
	Minor Version: 0x00000000
	Build Number: 0x00000000
	Revision Number: 0x00000000
	Locale: <null>
	Flags : [none] (00000000)
	CustomAttribute #1 (0c000001)
	-------------------------------------------------------
		CustomAttribute Type: 0a000001
		CustomAttributeName: System.Runtime.CompilerServices.CompilationRelaxationsAttribute :: instance void .ctor(int32)
		Length: 8
		Value : 01 00 08 00 00 00 00 00                          >                <
		ctor args: (8)

	CustomAttribute #2 (0c000002)
	-------------------------------------------------------
		CustomAttribute Type: 0a000002
		CustomAttributeName: System.Runtime.CompilerServices.RuntimeCompatibilityAttribute :: instance void .ctor()
		Length: 30
		Value : 01 00 01 00 54 02 16 57  72 61 70 4e 6f 6e 45 78 >    T  WrapNonEx<
                      : 63 65 70 74 69 6f 6e 54  68 72 6f 77 73 01       >ceptionThrows   <
		ctor args: ()

	CustomAttribute #3 (0c000003)
	-------------------------------------------------------
		CustomAttribute Type: 0a000003
		CustomAttributeName: System.Diagnostics.DebuggableAttribute :: instance void .ctor(value class DebuggingModes)
		Length: 8
		Value : 01 00 07 01 00 00 00 00                          >                <
		ctor args: ( <can not decode> )

	CustomAttribute #4 (0c000004)
	-------------------------------------------------------
		CustomAttribute Type: 0a000004
		CustomAttributeName: System.Runtime.Versioning.TargetFrameworkAttribute :: instance void .ctor(class System.String)
		Length: 73
		Value : 01 00 1a 2e 4e 45 54 46  72 61 6d 65 77 6f 72 6b >   .NETFramework<
                      : 2c 56 65 72 73 69 6f 6e  3d 76 34 2e 35 01 00 54 >,Version=v4.5  T<
                      : 0e 14 46 72 61 6d 65 77  6f 72 6b 44 69 73 70 6c >  FrameworkDispl<
                      : 61 79 4e 61 6d 65 12 2e  4e 45 54 20 46 72 61 6d >ayName .NET Fram<
                      : 65 77 6f 72 6b 20 34 2e  35                      >ework 4.5       <
		ctor args: (".NETFramework,Version=v4.5")


AssemblyRef #1 (23000001)
-------------------------------------------------------
	Token: 0x23000001
	Public Key or Token: b7 7a 5c 56 19 34 e0 89 
	Name: mscorlib
	Version: 4.0.0.0
	Major Version: 0x00000004
	Minor Version: 0x00000000
	Build Number: 0x00000000
	Revision Number: 0x00000000
	Locale: <null>
	HashValue Blob:
	Flags: [none] (00000000)


User Strings
-------------------------------------------------------
70000001 : ( 2) L"Hi"


Coff symbol name overhead:  0
===========================================================
===========================================================
===========================================================
```

幸好 ILDasm 处理元数据表，恰当合并了信息，避免我们分析原始表。例如，可以看到当 ILDasm 显示一个 TypeDef 记录项时，会在第一个 TypeRef 记录项时，会在第一个 TypeDef 项之前显示对应的成员定义信息。

不用完全理解上面显示的一切。重点是 Program.exe 包含名为 **Program** 的 TypeDef。 **Program**是公共密封类，从 `System.Object` 派生(`System.Object`是引用的另一个程序集中类型)。**Program** 类型还定义了两个方法：`Main` 和 `.ctor`(构造器)。

`Main` 是公共静态方法，用 IL 代码实现(有的方法可能用本机 CPU 代码实现，比如 x86 代码)。**Main**返回类型是 **void**，无参。构造器(名称始终是`.ctor`)是公共方法，也用 IL 代码实现。构造器返回类型是 `void`，无参，有一个 `this`指针(指向调用方法时构造对象的内存)。

强烈建议多试验一下 ILDasm。它提供了丰富的信息。你对自己看到的东西理解得越多，对 CLR 及其功能的理解就越好。本书后面会大量地用到 ILDasm。

为增加趣味性，来看看 Program.exe 程序集的统计信息。在 ILDasm 中选择”视图“|”统计“，会显示以下信息：

```file
 File size            : 4096
 PE header size       : 512 (496 used)    (12.50%)
 PE additional info   : 1491              (36.40%)
 Num.of PE sections   : 3
 CLR header size     : 72                 ( 1.76%)
 CLR meta-data size  : 872                (21.29%)
 CLR additional info : 0                  ( 0.00%)
 CLR method headers  : 2                  ( 0.05%)
 Managed code         : 21                ( 0.51%)
 Data                 : 2048              (50.00%)
 Unaccounted          : -922              (-22.51%)

 Num.of PE sections   : 3
   .text    - 1536
   .rsrc    - 1536
   .reloc   - 512

 CLR meta-data size  : 872
   Module        -    1 (10 bytes)
   TypeDef       -    2 (28 bytes)      0 interfaces, 0 explicit layout
   TypeRef       -    7 (42 bytes)
   MethodDef     -    2 (28 bytes)      0 abstract, 0 native, 2 bodies
   MemberRef     -    6 (36 bytes)
   CustomAttribute-    4 (24 bytes)
   Assembly      -    1 (22 bytes)
   AssemblyRef   -    1 (20 bytes)
   Strings       -   307 bytes
   Blobs         -   164 bytes
   UserStrings   -     8 bytes
   Guids         -    16 bytes
   Uncategorized -   167 bytes

 CLR method headers : 2
   Num.of method bodies  - 2
   Num.of fat headers    - 0
   Num.of tiny headers   - 2

 Managed code : 21
   Ave method size - 10
```

从中可以看出文件大小(字节数)以及文件各部分大小(字节数和百分比)。对于这个如此小的 Program.exe 应用程序，PE 头和元数据占了相当大的比重。事实上，IL 代码只有区区 20 个字节。当然，随着应用程序规模的增大，它会重用大多数类型以及对其他类型和程序集的引用，元数据和头信息在整个文件中的比重越来越小。  

> 注意 顺便说一下， ILDasm.exe 的一个 bug  会影响显示的文件长度，尤其不要相信 Unaccounted 信息。

## <a name="2_4">将模块合并成程序集</a>

上一节讨论的Program.exe 并非只是含有元数据的 PE 文件，它还是 **程序集(assembly)**。程序集是一个或多个类型定义文件及资源文件的集合。在程序集的所有文件中，有一个文件容纳了**清单**(manifest)。清单也是一个元数据表集合，表中主要包含作为程序组成部分的那些文件的名称。此外，还描述了程序集的版本、语言文化、发布者、公开导出的类型以及构成程序集的所有文件。

CLR 操作的是程序集。换言之， CLR 总是首先加载包含”清单“元数据表的文件，再根据”清单“来获取程序集中的其他文件的名称。下面列出了程序集的重要特点。

* 程序集定义了可重用的类型。
* 程序集用一个版本号标记。
* 程序集可以关联安全信息。

除了包含清单元数据表的文件，程序集其他单独的文件并不具备上述特点。

类型为了顺利地进行打包、版本控制、安全保护以及使用，必须放在作为程序集一部分的模块中。程序集大多数时候只有一个文件，就像前面的Program.exe 那样。然后，程序集还可以多个文件构成：一些是含有元数据的 PE 文件，另一些是.gif或.jpg 这样的资源文件。为便于理解，可将程序集视为一个逻辑 EXE 或 DLL。

Microsoft 为什么引入 ”程序集“ 的概念？这是因为使用程序集，可重用类型的逻辑表示与物理表示就可以分开。例如，程序集可能包含多个类型。可以将常用类型放到一个文件中，不常用类型放到另外一个文件中。如果程序集要从 Internet 下载并部署，那么对于含有不常用类型的文件，假如客户端永远不使用那些类型，该文件就永远不会下载到客户端。例如，擅长制作 UI 控件的一家独立软件开发商(Independent Software Vendor， ISV)可选择在单独的模块中实现 Active Accessibility 类型(以满足 Microsoft 徽标认证授权要求)。这样一来，只要需要额外”无障碍访问“功能的用户才需要下载该模块。