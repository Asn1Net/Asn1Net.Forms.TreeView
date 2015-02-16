@rem Add path to MSBuild Binaries
setlocal
call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\vsvars32.bat" || @goto :error

set NUGETDIR=Asn1Net.Forms.TreeView

nuget pack %NUGETDIR%\Asn1Net.Forms.TreeView.nuspec || exit /b 1

endlocal

@echo NUGET BUILD SUCCEEDED !!!
@exit /b 0
