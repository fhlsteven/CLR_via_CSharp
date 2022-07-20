# CLR_via_CSharp

CLR Via C# 第四版
(美) Jeffrey Richter 著 周靖

本书源代码来自[Click Me](https://github.com/cuicheng11165/clr-via-csharp-4th-edition-code)  
看书 记录 差不多就是照样子搬运工

md文件上传到语雀了 那个看书也可以的 我觉得挺好使的 [Click Me](https://www.yuque.com/fhlsteven/clr_via_csharp)  

养成每天看书的习惯，每天更新github的习惯，练习markdown语法，练习打字速度，后期之后再做些想做的事情，没有想好做什么，大家要是有啥建议也可以留言。

[前言](./chapters/foreword.md)  
[序言](./chapters/introduction.md)

---
第一部分 CLR 基础  

[第 1 章 CLR的执行模型](./chapters/chapter1_TheCLRSExecutionMode.md)  
[第 2 章 生成、打包、部署和管理应用程序及类型](./chapters/chapter2_Building.md)  
[第 3 章 共享程序集和强命名程序集](./chapters/chapter3_SharedAssemblies.md)

---
第二部分 设计类型  

[第 4  章 类型基础](./chapters/ch4_TypeFundamentals.md)  
[第 5  章 基元类型、引用类型和值类型](./chapters/ch5_PrimitiveRefValType.md)  **※**  
[第 6  章 类型和成员基础](./chapters/ch6_TypeAndMemberBasics.md)  
[第 7  章 常量和字段](./chapters/ch7_ConstantsAndFields.md)  
[第 8  章 方法](./chapters/ch8_Methods.md)  
[第 9  章 参数](./chapters/ch9_Parameters.md)  
[第 10 章 属性](./chapters/ch10_Properties.md)  
[第 11 章 事件](./chapters/ch11_Events.md)  
[第 12 章 泛型](./chapters/ch12_Generics.md)  
[第 13 章 接口](./chapters/ch13_Interfaces.md)

---
第三部分 基本类型  

[第 14 章 字符、字符串和文本处理](./chapters/ch14_CharStringText.md)  
[第 15 章 枚举类型和位标志](./chapters/ch15_EnumeratedTypes.md)  
[第 16 章 数组](./chapters/ch16_Arrays.md)  
[第 17 章 委托](./chapters/ch17_Delegates.md)  
[第 18 章 定制特性](./chapters/ch18_CustomAttributes.md)  
[第 19 章 可空值类型](./chapters/ch19_NullableValueTypes.md)  

---
第四部分 核心机制  

[第 20 章 异常和状态管理](./chapters/ch20_ExceptionsAndStateManae.md)  
[第 21 章 托管堆和垃圾回收](./chapters/ch21_ManagedHeapGarbage.md)  
[第 22 章 CLR 寄宿和 AppDomain](./chapters/ch22_CLRHostingAndAppDomain.md)  
[第 23 章 程序集加载和反射](./chapters/ch23_AssemblyLoaingReflection.md)  
[第 24 章 运行时序列化](./chapters/ch24_RuntimeSerialization.md)  
[第 25 章 与 WinRT 组件互操作](./chapters/ch25_WinRTComponents.md)

---
第五部分 线程处理

[第 26 章 线程基础](./chapters/ch26_ThreadBasics.md)  
[第 27 章 计算限制的异步操作](./chapters/ch27_ComputeBoundAsync.md)  
[第 28 章 I/O 限制的异步操作](./chapters/ch28_IOBoundAsyncOperations.md)  
[第 29 章 基元线程同步构造](./chapters/ch29_PrimitiveThreadSyncConstructs.md)  
[第 30 章 混合线程同步构造](./chapters/ch30_hybridThreadSyncConst.md)  

---
[译者后记](./chapters/Postscript.md)  

---
---

终于撕完这本书了，上图  

![final](./resources/images/final.JPG)  

看的过程有一些之前改代码的时候看别人用的一些技巧，当时不懂还问了，现在看书的时候有看到而且讲的很详细，一种特别神奇的感觉(只可意会不可言传)，你遇到了就会有体会。还有通看了书之后也会对你看源代码有帮助的，毕竟现在人家都开源了是吧，不看白不看。看书是一会儿事，写代码时另外一会儿事，多看优秀的代码，学习优秀的思想，最后还是的多动手写代码，在工作中，尽可能用吧。

做事情坚持不下去的时候，就找个方式让自己坚持下去，持之以恒总会有点料的；”在深入了解情况之前，不要看不起任何人。有些人，它的人生落在了跑步机上“————向前看

---
---
阅读策略

作者：赵劼 来源：知乎 (https://www.zhihu.com/question/27283360/answer/36182906)

* 细读：都要读懂，要都理解了，读不懂反复读，找额外资料读。

* 通读：大致都了解可以干嘛，尽量看懂。

* 粗读：随手翻下，读不懂可以跳过，时不时回头看看。

Ch1通读。

Ch2和3粗读。

Ch4到19：细读，全是基础内容。

Ch20细读，最后两节（CER和Code Contract）可以粗读。

Ch21细读，讲GC的，比较重要。

Ch22粗读。

Ch23到25通读。

Ch26细读。

Ch27到30通读。

30章，每2天1章，大概需要2个月
