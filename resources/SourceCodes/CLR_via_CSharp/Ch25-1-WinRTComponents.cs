/******************************************************************************
Module:  WinRTComponents.cs
Notices: Copyright (c) 2012 by Jeffrey Richter
******************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Metadata;

// The namespace MUST match the assembly name and cannot be "Windows"
namespace Wintellect.WinRTComponents {
   // [Flags]  // Must not be present if enum is int; required if enum is uint
   public enum WinRTEnum : int {    // Enums must be backed by int or uint
      None,
      NotNone
   }


   // Structures can only contain core data types, String, & other structures
   // No constructors or methods are allowed
   public struct WinRTStruct {
      public Int32 ANumber;
      public String AString;
      public WinRTEnum AEnum;    // Really just a 32-bit integer
   }


   // Delegates must have WinRT-compatible types in the signature (no BeginInvoke/EndInvoke)
   public delegate String WinRTDelegate(Int32 x);


   // Interfaces can have methods, properties, & events but cannot be generic.
   public interface IWinRTInterface {
      // Nullable<T> marshals as IReference<T>
      Int32? InterfaceProperty { get; set; }
   }

   // Members without a [Version(#)] attribute default to the class's 
   // version (1) and are part of the same underlying COM interface
   // produced by WinMDExp.exe.
   [Version(1)]
   // Class must be derived from Object, sealed, not generic, 
   // implement only WinRT interfaces, & public members must be WinRT types
   public sealed class WinRTClass : IWinRTInterface {
      // Public fields are not allowed 

      #region Class can expose static methods, properties, and events
      public static String StaticMethod(String s) { return "Returning " + s; }
      public static WinRTStruct StaticProperty { get; set; }

      // In JavaScript 'out' parameters are returned as objects with each 
      // parameter becoming a property along with the return value
      public static String OutParameters(out WinRTStruct x, out Int32 year) {
         x = new WinRTStruct { AEnum = WinRTEnum.NotNone, ANumber = 333, AString = "Jeff" };
         year = DateTimeOffset.Now.Year;
         return "Grant";
      }
      #endregion

      // Constructor can take arguments but not out/ref arguments
      public WinRTClass(Int32? number) { InterfaceProperty = number; }

      public Int32? InterfaceProperty { get; set; }

      // Only ToString is allowed to be overridden
      public override String ToString() {
         return String.Format("InterfaceProperty={0}",
            InterfaceProperty.HasValue ? InterfaceProperty.Value.ToString() : "(not set)");
      }

      public void ThrowingMethod() {
         throw new InvalidOperationException("My exception message");

         // To throw a specific HRESULT, use COMException instead
         //const Int32 COR_E_INVALIDOPERATION = unchecked((Int32)0x80131509);
         //throw new COMException("Invalid Operation", COR_E_INVALIDOPERATION);
      }

      #region Arrays are passed, returned OR filled; never a combination
      public Int32 PassArray([ReadOnlyArray] /* [In] implied */ Int32[] data) {
         // NOTE: Modified array contents MAY not be marshaled out; do not modify the array
         return data.Sum();
      }

      public Int32 FillArray([WriteOnlyArray] /* [Out] implied */ Int32[] data) {
         // NOTE: Original array contents MAY not be marshaled in; 
         // write to the array before reading from it
         for (Int32 n = 0; n < data.Length; n++) data[n] = n;
         return data.Length;
      }

      public Int32[] ReturnArray() {
         // Array is marshaled out upon return
         return new Int32[] { 1, 2, 3 };
      }
      #endregion

      // Collections are passed by reference
      public void PassAndModifyCollection(IDictionary<String, Object> collection) {
         collection["Key2"] = "Value2";  // Modifies collection in place via interop
      }

      #region Method overloading
      // Overloads with same # of parameters are considered identical to JavaScript
      public void SomeMethod(Int32 x) { }

      [Windows.Foundation.Metadata.DefaultOverload]  // Attribute makes this method the default overload
      public void SomeMethod(String s) { }
      #endregion

      #region Automatically implemented event
      public event WinRTDelegate AutoEvent;

      public String RaiseAutoEvent(Int32 number) {
         WinRTDelegate d = AutoEvent;
         return (d == null) ? "No callbacks registered" : d(number);
      }
      #endregion

      #region Manually implemented event
      // Private field that keeps track of the event's registered delegates
      private EventRegistrationTokenTable<WinRTDelegate> m_manualEvent = null;

      // Manual implementation of the event's add and remove methods
      public event WinRTDelegate ManualEvent {
         add {
            // Gets the existing table, or creates a new one if the table is not yet initialized
            return EventRegistrationTokenTable<WinRTDelegate>
               .GetOrCreateEventRegistrationTokenTable(ref m_manualEvent).AddEventHandler(value);
         }
         remove {
            EventRegistrationTokenTable<WinRTDelegate>
               .GetOrCreateEventRegistrationTokenTable(ref m_manualEvent).RemoveEventHandler(value);
         }
      }

      public String RaiseManualEvent(Int32 number) {
         WinRTDelegate d = EventRegistrationTokenTable<WinRTDelegate>
            .GetOrCreateEventRegistrationTokenTable(ref m_manualEvent).InvocationList;
         return (d == null) ? "No callbacks registered" : d(number);
      }
      #endregion

      #region Asynchronous methods
      // Async methods MUST return IAsync[Action|Operation](WithProgress)
      // NOTE: Other languages see the DataTimeOffset as Windows.Foundation.DateTime
      public IAsyncOperationWithProgress<DateTimeOffset, Int32> DoSomethingAsync() {
         // Use the System.Runtime.InteropServices.WindowsRuntime.AsyncInfo's Run methods to 
         // invoke a private method written entirely in managed code
         return AsyncInfo.Run<DateTimeOffset, Int32>(DoSomethingAsyncInternal);
      }

      // Implement the async operation via a private method using normal .NET technologies
      private async Task<DateTimeOffset> DoSomethingAsyncInternal(
         CancellationToken ct, IProgress<Int32> progress) {

         for (Int32 x = 0; x < 10; x++) {
            // This code supports cancellation and progress reporting
            ct.ThrowIfCancellationRequested();
            if (progress != null) progress.Report(x * 10);
            await Task.Delay(1000); // Simulate doing something asynchronously
         }
         return DateTimeOffset.Now; // Ultimate return value
      }

      public IAsyncOperation<DateTimeOffset> DoSomethingAsync2() {
         // If you don't need cancellation & progress, use 
         // System.WindowsRuntimeSystemExtensions' AsAsync[Action|Operation] Task 
         // extension methods (these call AsyncInfo.Run internally)
         return DoSomethingAsyncInternal(default(CancellationToken), null).AsAsyncOperation();
      }
      #endregion

      // After you ship a version, mark new members with a [Version(#)] attribute
      // so that WinMDExp.exe puts the new members in a different underlying COM 
      // interface. This is required since COM interfaces are supposed to be immutable.
      [Version(2)]
      public void NewMethodAddedInV2() { }
   }
}

// In VS, set project property to WinMD file: Runs CSC /t:winmdobj & spawns WinMDExp.exe /modulepdb:<symbolfile> *.WinMDObj 
// WinMDExp.exe adds metadata it does not change the IL at all. ILDasm has new /Project switch.
// Creating components: http://msdn.microsoft.com/en-us/library/windows/apps/hh441572(v=vs.110).aspx

// Extensions SDK install dir: %ProgramFiles%\Microsoft SDKs\Windows\v8.0\Extension SDKs 
// Creating Extension SDK: http://msdn.microsoft.com/library/hh768146(v=VS.110).aspx
// VS SDK to make VSIX files: http://www.microsoft.com/visualstudio/11/en-us/downloads#vs-sdk

#if false
namespace WinRTComponent.Generated {
   using System.Runtime.CompilerServices;
   using System.Runtime.InteropServices;
   using Windows.Foundation.Metadata;
   using GuidA = Windows.Foundation.Metadata.GuidAttribute;

#if !EnableComposition
   [Version(0x1000000), Activatable(0x1000000), Activatable(typeof(IWinRTTypeFactory), 0x1000000), Static(typeof(IWinRTTypeStatic), 0x1000000)]
   internal sealed class _CLR_WinRTType : IWinRTTypeClass {
#else
   [EnableComposition, Version(0x1000000), Composable(typeof(IWinRTTypeFactory), CompositionType.Public, 0x1000000), Static(typeof(IWinRTTypeStatic), 0x1000000)]
   internal class <CLR>WinRTType : IWinRTTypeClass
#endif
      // NOTE: This type contains the IL originally produced by the compiler
      private WinRTDelegate m_cb;

      public _CLR_WinRTType();
      public _CLR_WinRTType(int value);
      public string RaiseCallback(int x);
      public void SetCallback(WinRTDelegate cb);
      public static string StaticMethod();

      public int InstanceProperty { get; set; }
      /*public*/ int WinRTComponent.Generated.IWinRTTypeClass.InstanceProperty { get; set; }
   }

   [CompilerGenerated, GuidA(0xd377195f, 0xe945, 0x5b43, 0x5d, 0x22, 0x13, 160, 0x52, 0x25, 0x86, 0x47), Version(0x1000000), ExclusiveTo(typeof(WinRTType))]
   internal interface IWinRTTypeClass {
      // NOTE: This interface has the instance members (except for ctors) defined by the original class
      [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/)]
      string RaiseCallback([In] int x);
      [MethodImpl(/*0/ MethodCodeType=MethodCodeType.Runtime*/)]
      void SetCallback([In] WinRTDelegate cb);

      // Properties
      int InstanceProperty { [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/), CompilerGenerated] get; [param: In] [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/), CompilerGenerated] set; }
   }

   [CompilerGenerated, GuidA(0xa954d174, 0x59c7, 0x5248, 0x61, 0xda, 0xf6, 0x8a, 60, 0x84, 0x67, 0xf8), Version(0x1000000), ExclusiveTo(typeof(WinRTType))]
   internal interface IWinRTTypeFactory {
      // NOTE: This interface has a factory that is the equivalent of a ctor with arguments
#if EnableComposition
      [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/)]
      WinRTType CreateWinRTType([In] int value);
#else
    [MethodImpl(0, MethodCodeType=MethodCodeType.Runtime)]
    WinRTType CreateWinRTType([In] object outerInspectable, out object innerInspectable);
  
    [MethodImpl(0, MethodCodeType=MethodCodeType.Runtime)]
    WinRTType CreateWinRTType([In] int value, [In] object outerInspectable, out object innerInspectable);
#endif
   }

   [CompilerGenerated, GuidA(0x3b0e2b52, 0xeace, 0x501b, 0x40, 0x87, 0x34, 0x94, 0xf4, 0x92, 0x8d, 0x55), Version(0x1000000), ExclusiveTo(typeof(WinRTType))]
   internal interface IWinRTTypeStatic {
      // NOTE: This interface has the sttaic members defined by the original class
      [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/)]
      string StaticMethod();
   }

   [GuidA(0xb56fb6d3, 0xb2a2, 0x52c3, 0x4b, 0xd6, 0x10, 0x69, 0x1a, 0xfe, 0xb3, 220), Version(0x1000000)]
   public delegate string WinRTDelegate([In] int x);

   [StructLayout(LayoutKind.Sequential), Version(0x1000000)]
   public struct WinRTStruct {
      public string foo;
   }

#if !EnableComposition
   [Version(0x1000000), CompilerGenerated, Activatable(0x1000000), Activatable(typeof(IWinRTTypeFactory), 0x1000000), Static(typeof(IWinRTTypeStatic), 0x1000000)]
   public sealed class WinRTType : IWinRTTypeClass {
#else
   [EnableComposition, Version(0x1000000), CompilerGenerated, Composable(typeof(IWinRTTypeFactory), CompositionType.Public, 0x1000000), Static(typeof(IWinRTTypeStatic), 0x1000000)]
   public class WinRTType : IWinRTTypeClass
#endif
      // NOTE: These members have no IL; THE CLR's JITter implements the implementation automatically on 1st call
      // Methods
      [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/)]
      public WinRTType();
      [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/)]
      public WinRTType([In] int value);
      [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/)]
      public string RaiseCallback([In] int x);
      [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/)]
      public void SetCallback([In] WinRTDelegate cb);
      [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/)]
      public static string StaticMethod();

      // Properties
      public int InstanceProperty { [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/), CompilerGenerated] get; [param: In] [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/), CompilerGenerated] set; }
      /*public*/ int WinRTComponent.Generated.IWinRTTypeClass.InstanceProperty { [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/), CompilerGenerated] get; [param: In] [MethodImpl(/*0, MethodCodeType=MethodCodeType.Runtime*/), CompilerGenerated] set; }
   }
}
#endif


/*
Set project output type to WinMD which builds as follows:
Csc.exe /noconfig /nowarn:2008,1701,1702 /pdb:obj\Debug\WinRTComponent.compile.pdb /nostdlib+ /errorreport:prompt /warn:4 /define:DEBUG;TRACE /errorendlocation 
   /debug+ /debug:full /filealign:512 /optimize- /out:obj\Debug\WinRTComponent.intermediate.winmdobj /target:winmdobj /utf8output WinRTComponent.cs Properties\AssemblyInfo.cs obj\Debug\\TemporaryGeneratedFile_E7A71F73-0F8D-4B9B-B56E-8E70B10BC5D3.cs
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\mscorlib.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\Microsoft.CSharp.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\Microsoft.VisualBasic.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\mscorlib.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Collections.Concurrent.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Collections.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Collections.ObjectModel.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.AttributedModel.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.Hosting.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.Primitives.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.EventBasedAsync.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Core.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Contracts.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Debug.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Tools.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Tracing.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Dynamic.Runtime.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Globalization.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.IO.Compression.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.IO.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.Expressions.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.Parallel.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.Queryable.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.Http.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.NetworkInformation.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.Primitives.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.Requests.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Numerics.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Reflection.Context.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Reflection.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Reflection.Extensions.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Resources.ResourceManager.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.Extensions.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.InteropServices.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.InteropServices.WindowsRuntime.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.Serialization.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.UI.Xaml.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Security.Principal.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.DataContract.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.DataContract.JsonSerializer.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.DataContract.Serializer.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.Xml.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Duplex.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Http.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.NetTcp.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Primitives.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Security.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.XmlSerializer.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Text.Encoding.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Text.RegularExpressions.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Threading.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Threading.Tasks.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Threading.Tasks.Parallel.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.Linq.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.ReaderWriter.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.Serialization.dll" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Activation.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Background.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Contacts.provider.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Contacts.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Core.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Datatransfer.sendtarget.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Datatransfer.sharetarget.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Datatransfer.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Infrastructure.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.resources.core.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.resources.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.search.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.store.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.data.json.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.data.xml.dom.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.data.xml.xsl.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.enumeration.pnp.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.enumeration.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.geolocation.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.input.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.portable.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.Devices.Printers.Extensions.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.sensors.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.sms.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.foundation.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.collation.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.datetimeformatting.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.fonts.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.numberformatting.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.display.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.imaging.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.printing.advanced.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.printing.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.internal.security.authentication.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.management.core.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.management.deployment.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.capture.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.devices.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.playlists.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.playto.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.protection.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.transcoding.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.videoeffects.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.backgroundtransfer.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.connectivity.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.networkoperators.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.proximity.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.pushnotifications.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.sockets.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.authentication.live.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.authentication.web.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.credentials.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.certificates.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.core.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.dataprotection.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.accesscache.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.bulkaccess.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.compression.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.fileproperties.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.pickers.provider.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.pickers.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.search.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.streams.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.display.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.threading.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.userprofile.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.applicationsettings.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.core.animationmetrics.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.core.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.input.inking.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.input.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.notifications.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.popups.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.startscreen.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.viewmanagement.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.webui.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.peers.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.provider.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.text.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.controls.primitives.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.controls.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.data.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.documents.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.input.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.interop.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.markup.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.animation.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.imaging.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.media3d.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.navigation.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.printing.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.resources.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.shapes.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.web.atompub.winmd" 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.web.syndication.winmd" 

Csc.exe /noconfig /nowarn:2008,1701,1702,1701,1702 /pdb:obj\Debug\ClassLibrary1.compile.pdb /nostdlib+ /errorreport:prompt /warn:4 /define:DEBUG;TRACE /errorendlocation /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\mscorlib.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\Microsoft.CSharp.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\Microsoft.VisualBasic.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\mscorlib.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Collections.Concurrent.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Collections.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Collections.ObjectModel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.AttributedModel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.Hosting.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.Primitives.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.EventBasedAsync.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Core.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Contracts.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Debug.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Tools.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Tracing.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Dynamic.Runtime.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Globalization.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.IO.Compression.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.IO.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.Expressions.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.Parallel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.Queryable.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.Http.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.NetworkInformation.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.Primitives.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.Requests.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Numerics.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Reflection.Context.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Reflection.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Reflection.Extensions.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Resources.ResourceManager.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.Extensions.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.InteropServices.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.InteropServices.WindowsRuntime.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.Serialization.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.UI.Xaml.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Security.Principal.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.DataContract.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.DataContract.JsonSerializer.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.DataContract.Serializer.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.Xml.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Duplex.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Http.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.NetTcp.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Primitives.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Security.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.XmlSerializer.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Text.Encoding.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Text.RegularExpressions.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Threading.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Threading.Tasks.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Threading.Tasks.Parallel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.Linq.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.ReaderWriter.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.Serialization.dll" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Activation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Background.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Contacts.provider.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Contacts.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Datatransfer.sendtarget.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Datatransfer.sharetarget.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Datatransfer.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Infrastructure.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.resources.core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.resources.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.search.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.store.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.data.json.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.data.xml.dom.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.data.xml.xsl.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.enumeration.pnp.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.enumeration.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.geolocation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.input.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.portable.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.Devices.Printers.Extensions.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.sensors.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.sms.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.foundation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.collation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.datetimeformatting.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.fonts.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.numberformatting.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.display.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.imaging.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.printing.advanced.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.printing.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.internal.security.authentication.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.management.core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.management.deployment.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.capture.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.devices.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.playlists.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.playto.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.protection.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.transcoding.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.videoeffects.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.backgroundtransfer.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.connectivity.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.networkoperators.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.proximity.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.pushnotifications.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.sockets.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.authentication.live.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.authentication.web.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.credentials.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.certificates.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.dataprotection.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.accesscache.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.bulkaccess.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.compression.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.fileproperties.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.pickers.provider.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.pickers.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.search.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.streams.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.display.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.threading.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.userprofile.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.applicationsettings.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.core.animationmetrics.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.input.inking.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.input.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.notifications.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.popups.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.startscreen.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.viewmanagement.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.webui.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.peers.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.provider.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.text.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.controls.primitives.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.controls.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.data.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.documents.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.input.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.interop.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.markup.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.animation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.imaging.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.media3d.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.navigation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.printing.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.resources.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.shapes.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.web.atompub.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.web.syndication.winmd" /debug+ /debug:full /filealign:512 /optimize- /out:obj\Debug\ClassLibrary1.winmdobj /target:winmdobj /utf8output WinRTComponent.cs Properties\AssemblyInfo.cs obj\Debug\\TemporaryGeneratedFile_E7A71F73-0F8D-4B9B-B56E-8E70B10BC5D3.cs "C:\Users\Jeffrey\AppData\Local\Temp\.NETCore,Version=v4.5.AssemblyAttributes.cs"
winmdexp.exe 
 * /md:bin\Debug\WinRTComponent.XML 
 * /mp:obj\Debug\WinRTComponent.compile.pdb 
 * /pdb:obj\Debug\WinRTComponent.pdb 
 * /out:obj\Debug\WinRTComponent.winmd 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\mscorlib.dll" 
   /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\Microsoft.CSharp.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\Microsoft.VisualBasic.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\mscorlib.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Collections.Concurrent.dll" 
 * /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Collections.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Collections.ObjectModel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.AttributedModel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.Hosting.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.Composition.Primitives.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ComponentModel.EventBasedAsync.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Core.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Contracts.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Debug.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Tools.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Diagnostics.Tracing.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Dynamic.Runtime.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Globalization.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.IO.Compression.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.IO.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.Expressions.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.Parallel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Linq.Queryable.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.Http.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.NetworkInformation.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.Primitives.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Net.Requests.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Numerics.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Reflection.Context.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Reflection.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Reflection.Extensions.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Resources.ResourceManager.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.Extensions.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.InteropServices.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.InteropServices.WindowsRuntime.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.Serialization.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.UI.Xaml.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Security.Principal.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.DataContract.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.DataContract.JsonSerializer.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.DataContract.Serializer.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Serialization.Xml.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Duplex.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Http.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.NetTcp.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Primitives.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.Security.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.ServiceModel.XmlSerializer.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Text.Encoding.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Text.RegularExpressions.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Threading.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Threading.Tasks.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Threading.Tasks.Parallel.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.Linq.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.ReaderWriter.dll" /reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Xml.Serialization.dll" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Activation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Background.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Contacts.provider.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Contacts.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Datatransfer.sendtarget.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Datatransfer.sharetarget.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Datatransfer.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\Windows.Applicationmodel.Infrastructure.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.resources.core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.resources.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.search.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.store.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.applicationmodel.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.data.json.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.data.xml.dom.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.data.xml.xsl.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.enumeration.pnp.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.enumeration.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.geolocation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.input.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.portable.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.Devices.Printers.Extensions.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.sensors.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.devices.sms.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.foundation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.collation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.datetimeformatting.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.fonts.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.numberformatting.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.globalization.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.display.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.imaging.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.printing.advanced.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.printing.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.graphics.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.internal.security.authentication.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.management.core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.management.deployment.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.capture.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.devices.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.playlists.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.playto.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.protection.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.transcoding.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.videoeffects.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.media.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.backgroundtransfer.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.connectivity.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.networkoperators.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.proximity.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.pushnotifications.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.sockets.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.networking.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.authentication.live.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.authentication.web.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.credentials.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.certificates.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.dataprotection.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.security.cryptography.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.accesscache.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.bulkaccess.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.compression.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.fileproperties.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.pickers.provider.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.pickers.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.search.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.streams.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.storage.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.display.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.threading.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.userprofile.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.system.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.applicationsettings.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.core.animationmetrics.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.core.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.input.inking.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.input.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.notifications.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.popups.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.startscreen.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.viewmanagement.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.webui.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.peers.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.provider.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.text.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.automation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.controls.primitives.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.controls.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.data.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.documents.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.input.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.interop.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.markup.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.animation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.imaging.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.media3d.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.media.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.navigation.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.printing.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.resources.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.shapes.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.ui.xaml.winmd" /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.web.atompub.winmd" 
 * 
 * /reference:"C:\Program Files (x86)\Windows Kits\8.0\Windows Metadata\windows.web.syndication.winmd" 
  obj\Debug\WinRTComponent.winmdobj 

1>  Copying file from "obj\Debug\WinRTComponent.winmd" to "bin\Debug\WinRTComponent.winmd".
1>  Copying file from "obj\Debug\WinRTComponent.pdb" to "bin\Debug\WinRTComponent.pdb".
*/
