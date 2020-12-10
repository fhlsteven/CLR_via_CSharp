@echo off
Rem %1="$(DevEnvDir)", %2="$(SolutionDir)", %3="$(OutDir)"

rem Set all the VS environment variables
pushd %1
call ..\Tools\VSVars32.bat
popd

rem Change to the solution directory
cd %2

REM There are two ways to build this multi-file assembly
REM The line below picks one of those ways
Goto Way1

:Way1
csc /t:module  /debug:full /out:Ch02-3-RUT.netmodule Ch02-3-RUT.cs
csc /t:library /debug:full /out:Ch02-3-MultiFileLibrary.dll /addmodule:Ch02-3-RUT.netmodule Ch02-3-FUT.cs Ch02-3-AssemblyVersionInfo.cs
md %3
move /Y Ch02-3-RUT.netmodule %3
move /Y Ch02-3-RUT.pdb %3
move /Y Ch02-3-MultiFileLibrary.dll %3
move /Y Ch02-3-MultiFileLibrary.pdb %3
goto Exit

:Way2
csc /t:module /debug:full /out:Ch02-3-RUT.netmodule Ch02-3-RUT.cs
csc /t:module /debug:full /out:Ch02-3-FUT.netmodule Ch02-3-FUT.cs Ch02-3-AssemblyVersionInfo.cs
al  /out:Ch02-3-MultiFileLibrary.dll /t:library Ch02-3-RUT.netmodule Ch02-3-FUT.netmodule
md %3
move /Y Ch02-3-RUT.netmodule %3
move /Y Ch02-3-RUT.pdb %3
move /Y Ch02-3-FUT.netmodule %3
move /Y Ch02-3-FUT.pdb %3
move /Y Ch02-3-MultiFileLibrary.dll %3
move /Y Ch02-3-MultiFileLibrary.pdb %3
goto Exit

:Exit

