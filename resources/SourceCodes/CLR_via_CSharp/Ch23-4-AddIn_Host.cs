using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Wintellect.HostSDK;

public sealed class Program {
   public static void Main() {
      // Find the directory that contains the Host exe
      String AddInDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

      // Assume AddIn assemblies are in same directory as host's EXE file
      var AddInAssemblies = Directory.EnumerateFiles(AddInDir, "*.dll");

      // Create a collection of Add-In Types usable by the host
      var AddInTypes =
         from file in AddInAssemblies
         let assembly = Assembly.Load(file)
         from t in assembly.ExportedTypes // Publicly-exported types
         // Type is usable if it is a class that implements IAddIn 
         where t.IsClass && typeof(IAddIn).GetTypeInfo().IsAssignableFrom(t.GetTypeInfo())
         select t;
      // Initialization complete: the host has discovered the usable Add-Ins

      // Here's how the host can construct Add-In objects and use them
      foreach (Type t in AddInTypes) {
         IAddIn ai = (IAddIn) Activator.CreateInstance(t);
         Console.WriteLine(ai.DoSomething(5));
      }
   }
}