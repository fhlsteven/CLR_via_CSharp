# 第 11 章 事  件

本章内容：
* <a href="#11_1">设计公开事件的类型</a>
* <a href="#11_2">编译器如何实现事件</a>
* <a href="#11_3">设计侦听事件的类型</a>
* <a href="#11_4">显式实现事件</a>

本章讨论可以在类型中定义的最后一种成员：事件。定义了事件成员的类型允许类型(或类型的实例)通知其他对象发生了特定的事情。例如，`Button` 类提供了 `Click` 事件。应用程序中的一个或多个对象可接收关于该事件的通知，以便在`Button`被单击之后采用特定操作。我们用事件这种类型成员来实现这种类型成员来实现这种交互。具体地说，定义了事件成员成员的类型能提供以下功能。

* 方法能登记它对事件的关注。
* 方法能注销它对事件的关注。
* 事件发生时，登记了的方法将收到通知。

类型之所以能提供事件通知功能，是因为类型维护了一个已登记方法的列表。事件发生后，类型将通知列表中所有已登记的方法。

CLR 事件模型以**委托**为基础。委托是调用<sup>①</sup>回调方法的一种类型安全的方式。对象凭借回调方法接收它们订阅的通知。本章会开始使用委托，但委托的完整细节是在第 17 章"委托"中讲述的。
> 这个“调用”(invoke)理解为“唤出”更恰当。它和普通的“调用”(call)稍有不同。在英语的语境中，invoke 和 call 的区别在于，在执行一个所有信息都已知的方法时，用 call 比较恰当。这些信息包括要引用的类型、方法的签名以及方法名。但是，在需要先“唤出”某个东西来帮你调用一个信息不明的方法时，用 invoke 就比较恰当。但是，由于两者均翻译为“调用”不会对读者的理解造成太大的困扰，所以本书仍然采用约定俗成的方式来进行翻译，只是在必要的时候附加英文原文提醒你区分。 —— 译注

为了帮你完整地理解事件在 CLR 中的工作机制，先来描述事件很有用的一个场景。假定要设计一个电子邮件应用程序。电子邮件到达时，用户可能希望将该邮件转发给传真机或寻呼机。先设计名为 `MailManager` 的类型来接收传入的电子邮件，它公开 `NewMail` 事件。

其他类型(如 `Fax` 和 `Pager`)的对象登记对于该事件的关注。`MailManager` 收到新电子邮件会引发该事件，造成邮件分发给每一个已登记的对象。每个对象都用它们自己的方式处理邮件。

应用程序初始化时只实例化一个 `MailManager` 实例，然后可以实例化任意数量的 `Fax` 和 `Pager`
对象。图 11-1 展示了应用程序如何初始化，以及新电子邮件到达时发生的事情。

![11_1](../resources/images/11_1.png)  
图 11-1 设计使用了事件的应用程序 

图 11-1 的应用程序首先构造 `MailManager` 的一个实例。 `MailManager` 提供了 `NewMail` 事件。构造 `Fax` 和 `Pager` 对象时，它们向 `MailManager` 的 `NewMail` 事件登记自己的一个实例方法。这样当新邮件到达时， `MailManager` 就知道通知 `Fax` 和 `Pager` 对象。`MailManager` 将来收到新邮件时会引发 `NewMail` 事件，使自己登记的方法都有机会以自己的方式处理邮件。

## <a name="11_1">11.1 设计要公开事件的类型</a>

开发人员通过连续几个步骤定义公开了一个或多个事件成员的类型。本节详细描述了每个必要的步骤。`MailManager` 示例应用程序(可从 *http://wintellect.com* 下载)展示了 `MailManager` 类型、`Fax` 类型和 `Pager` 类型的所有源代码。注意，`Pager`类型和`Fax`类型几乎完全性相同。

### 11.1.1 第一步：定义类型来容纳所有需要发送给事件通知接收者的附加信息

事件引发时，引发事件的对象可能希望向接收事件通知的对象传递一些附件信息。这些附加信息需要封装到它自己的类中，该类通常包含一组私有字段，以及一些用于公开这些字段的只读公共属性。根据约定，这种类应该从 `System.EventArgs` 派生，而且类名以 `EventArgs` 结束。本例将该类命名为 `NewMailEventArgs` 类，它的各个字段分别标识了发件人(`m_from`)、收件人(`m_to`)和主题(`m_subject`)。

```C#
// 第一步：定义一个类型来容纳所有应该发送给事件通知接受者的附加信息
internal class NewMailEventArgs : EventArgs {
    private readonly String m_from, m_to, m_subject;

    public NewMailEventArgs(String from, String to, String subject) {
        m_from = from; m_to = to; m_subject = subject;
    }

    public String From { get { return m_from; } }
    public String To { get { return m_to; } }
    public String Subject { get { return m_subject; } }
}
```

> 注意 `EventArgs` 类在 Microsoft .NET Framework 类库(FCL)中定义，其实现如下：

```C#
[ComVisible(true), Serializable]
public class EventArgs {
	public static readonly EventArgs Empty = new EventArgs();
	public EventArgs() { }
}
```

> 可以看出，该类型的实现非常简单，就是一个让其他类型继承的基类型。许多事件都没有附加信息需要传递。例如，当一个 `Button` 向已登记的接收者通知自己被单击时，调用回调方法就可以了。定义不需要传递附加数据的事件时，可直接使用 `EventArgs.Empty`，不用构造新的 `EventArgs` 对象。

11.1.2 第二步：定义事件成员

事件成员使用 C# 关键字 `event` 定义。每个事件成员都要指定以下内容：可访问性标识符(几乎肯定是 `pulbic`，这样其他代码才能访问该事件成员)；委托类型，指出要调用的方法的原型；以及名称(可以是任何有效的标识符)。以下是我们的 `MailManager` 类中的事件成员：

```C#
internal class MailManager {

    // 第二部：定义事件成员
    public event EventHandler<NewMailEventArgs> NewMail;
    ...
}
```

`NewMail` 是事件名称。事件成员的类型是 `EventHandler<NewMailEventArgs>`，意味着“事件通知”的所有接收者都必须提供一个原型和 `EventHandler<NewMailEventArgs>`委托类型匹配的回调方法。由于泛型 `System.EventHandler` 委托类型的定义如下：

```C#
public delegate void EventHandler<TEventArgs>(Object sender, TEventArgs e);
```

所以方法原型必须具有以下形式：

```C#
void MethodName(Object sender, NewMailEventArgs e);
```

> 注意 许多人奇怪事件模式为什么要求 `sender` 参数是 `Object` 类型。毕竟，只有 `MailManager` 才会引发传递了 `NewMailEventArgs` 对象的事件，所以回调方法更合适的原型似乎是下面这个：  
`void MethodName(MailManager sender, NewMailEventArgs e);`  
要求 `sender` 是 `Object` 主要是因为继承。例如，假定````
