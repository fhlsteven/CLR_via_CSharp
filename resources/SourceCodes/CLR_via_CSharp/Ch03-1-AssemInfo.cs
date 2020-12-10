/******************************************************************************
Module:  AssemInfo.cs
Notices: Copyright (c) 2013 Jeffrey Richter
******************************************************************************/


using System.Reflection;


///////////////////////////////////////////////////////////////////////////////


// Set the version CompanyName, LegalCopyright and LegalTrademarks fields
[assembly:AssemblyCompany("The Jeffrey Richter Company")]
[assembly:AssemblyCopyright("Copyright (c) 2013 Jeffrey Richter")]
[assembly:AssemblyTrademark(
   "JeffTypes is a registered trademark of the Richter Company")]


///////////////////////////////////////////////////////////////////////////////


// Set the version ProductName and ProductVersion fields
[assembly:AssemblyProduct("Jeffrey Richter Type Library")]
[assembly:AssemblyInformationalVersion("2.0.0.0")]


///////////////////////////////////////////////////////////////////////////////


// Set the version FileVersion, AssemblyVersion, 
// FileDescription, and Comments fields.
[assembly:AssemblyFileVersion("1.0.0.0")]
[assembly:AssemblyVersion("3.0.0.0")]
[assembly:AssemblyTitle("Jeff's type assembly")]
[assembly:AssemblyDescription("This assembly contains Jeff's types")]


///////////////////////////////////////////////////////////////////////////////


// Set the assembly's culture (""=neutral).
[assembly:AssemblyCulture("")]


///////////////////////////////////////////////////////////////////////////////


#if !StronglyNamedAssembly

// Weakly named assemblies are never signed
[assembly:AssemblyDelaySign(false)]

#else

// Strongly named assemblies are usually delay signed while building and 
// completely signed using SN.exe's -R or -Rc switch.
[assembly:AssemblyDelaySign(true)]

   #if !SignedUsingACryptoServiceProvider

   // Give the name of the file that contains the public/private key pair.
   // If delay signing, only the public key is used.
   [assembly:AssemblyKeyFile("MyCompany.keys")]

   // Note: If AssemblyKeyFile and AssemblyKeyName are both specified, 
   // here's what happens...
   // 1) If the container exists, the key file is ignored.
   // 2) If the container doesn't exist, the keys from the key 
   //    file are copied into the container and the assembly is signed.

   #else

   // Give the name of the cryptographic service provider (CSP) container
   // that contains the public/private key pair. 
   // If delay signing, only the public key is used.
   [assembly:AssemblyKeyName("")]

   #endif

#endif


//////////////////////////////// End of File //////////////////////////////////
