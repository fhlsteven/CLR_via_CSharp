# 第 26 章 线程基础

本章内容：

* <a href="#26_1">Windows 为什么要支持线程</a>
* <a href="#26_2">线程开销</a>
* <a href="#26_3">停止疯狂</a>
* <a href="#26_4">CPU 发展趋势</a>
* <a href="#26_5">CLR 线程和 Windows 线程</a>
* <a href="#26_6">使用专用线程执行异步的计算限制操作</a>
* <a href="#26_7">使用线程的理由</a>
* <a href="#26_8">线程调度和优先级</a>
* <a href="#26_9">前台线程和后台线程</a>
* <a href="#26_10">继续学习</a>

本章将介绍线程的基本概念，帮助开发人员理解线程及其使用。我将解释 Microsoft Windows 为什么引入线程的概念、CPU 发展趋势、CLR 线程和 Windows 线程的关系、线程开销、Windows 如何调度线程以及公开了线程属性的 Microsoft .NET Framework 类。

本书第 V 部分“线程处理” 的各个章节将解释 Windows 和 CLR 如何协同提供一个线程处理架构