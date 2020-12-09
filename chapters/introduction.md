# 前言
1999年10月，Microsoft的一些人首次向我展示了Microsoft .NET Framework、公共语言运行时(CLR)和C#编程语言。看到这一切时，我惊呆了，我知道我写软件的方式要发生非常大的变化了。他们请我为团队做一些顾问工作，我当即同意了。刚开始，我以为.NET Framework 是 Win32 API 和 COM 上的一个抽象层。但随着我投入越来越多的时间研究，我意思到它是一个更宏伟的项目。某种程度上，它是自己的操作系统。有自己的内存管理器，自己的安全系统，自己的文件加载器，自己的错误处理机制，自己的应用程序隔离边界(AppDomain)、自己的线程处理模型等。本书解释了所有这些主题，帮你为这个平台高效地设计和实现应用程序和组件。

我写这本书是2012年10月，距离首次接触.NET Framework 和 C#正好13年。13年来，我以Microsoft顾问身份开发过各式各样的应用程序，为.NET Framework本身也贡献良多。作为我自己公司[Wintellect](http://Wintellect.com)的合伙人，我还要为大量客户工作，帮他们设计、调试、优化软件以及解决使用.NET Framework进行高效率编程。贯穿本书所有主题，你都会看到我的经验之谈。

## 本书面向的读者
本书旨在解释如何为.NET Framework开发应用程序和可重用的类。具体地说，我要解释CLR的工作原理及其提供的功能，还要讨论Framework Class  Libarary(FCL)的各个部分。没有一本书能完整地解释FCL——其中含有数以千计的类型，而且这个数字正在以惊人速度增长。所以，我准备将重点放在每个开发人员都需要注意的核心类型上面。另外，虽然不会专门讲Windows窗体、Windows Presentation Foundation(WPF)、Microsoft Silverlight、XML Web服务、Web窗体、Microsoft ASP.NET MVC、Windows Store应用等，但本书描述的技术适用于所有这些应用程序类型。

本书围绕Microsoft Visual Studio 2012/2013，.NET Framework 4.5.x 和 C# 5.0 展开。由于Microsoft在发布这些技术的新版本时，会试图保持很大程度的向后兼容性，所以本书描述的许多内容也适合之前的版本。所有示例代码都用C#编程语言写成。但由于CLR可由许多编程语言使用，所以本书内容也适合非C#程序员。

> **注意** 本书代码可从Wintellect网站下载:*[http://wintellect.com/Resource-CLR-Via-CSharp-Fourth-Edition](http://wintellect.com/Resource-CLR-Via-CSharp-Fourth-Edition)*,也可从译者博客下载：[http://transbot.blog.163.com](http://transbot.blog.163.com).

我和我的编辑进行了艰苦卓绝的工作，试图为你提供最准确、最新、最深入、最容易阅读和理解、没有错误的信息。但是，即便有如此完美的团队协作，疏漏和错误也在所难免。如果你发现了本书的任何错误或者想提出一些建设性的意见，请发送邮件到*JeffreyR@Wintellect.com*

## 致谢
没有别人的帮助和技术援助，我是不可能写好这本书的。尤其要感谢我的家人。写好一本书所投入的时间和精力无法衡量。我只知道，没有我的妻子Kristin和两个儿子Aidan和Grant的支持，根本不可能有这本书的面世。多少次想花些时间一家人小聚，都因为本书而放弃。现在，本书总算告一段落，终于有时间做自己喜欢做的事情了。

本书的修订得到了一些“高人”的协助。.NET Framework团体队的一些人(其中许多都是我的朋友)审阅了其中的章节，我和他们进行了许多发人深省的对话。Christophe Nasarre 参与了我几本书的出版，在审阅本书并确保我能以最恰当的方式来表达的过程中，表现出了非凡的才能。他对本书的品质有至关重要的影响。和往常一样，我和Microsoft Press的团体进行了令人愉快的合作。特别感谢Ben Ryan，Devon Musgrave和Carol Dillingham。另外，感谢Susie Carr和Candace Sinclair提供的编辑和制作支持。

## 勘误和支持
我们尽最大努力保证本书的准确性。勘误或更改会添加到以下网页:  
[http://www.oreilly.com/catalog/errata.csp?isbn=0790145353665](http://www.oreilly.com/catalog/errata.csp?isbn=0790145353665)  
[http://go.microsoft.com/FWLink/?Linkid=266601](http://go.microsoft.com/FWLink/?Linkid=266601)
如果发现未列出的错误，可通过相同的网页报告。  
如需其他支持，请致函Microsoft Press Book Support部门:  
mspinput@microsoft.com  
注意，上述邮件地址不提供产品支持。  
最后，本书中文版的支持（勘误和资源下载）请访问译者博客：  
[http://transbot.blog.163.com](http://transbot.blog.163.com)


---
---
---

因为……所以……

保持好奇心

坚持本心