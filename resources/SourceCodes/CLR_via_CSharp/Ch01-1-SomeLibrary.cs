#if !DEBUG
#pragma warning disable 3002, 3005
#endif
using System;

// Tell compiler to check for CLS compliance
[assembly: CLSCompliant(true)]

namespace SomeLibrary {
   // Warnings appear because the class is public
   public sealed class SomeLibraryType {

      // Warning: Return type of 'SomeLibrary.SomeLibraryType.Abc()' 
      // is not CLS-compliant
      public UInt32 Abc() { return 0; }

      // Warning: Identifier 'SomeLibrary.SomeLibraryType.abc()' 
      // differing only in case is not CLS-compliant
      public void abc() { }

      // No warning: Method is private
      private UInt32 ABC() { return 0; }
   }
}
