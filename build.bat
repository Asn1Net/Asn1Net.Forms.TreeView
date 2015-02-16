

@rem build
setlocal
@rem preparing environment
call "C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\Tools\vsvars32.bat" || exit /b 1
set SLNPATH=src\Asn1Net.Forms.TreeView.sln

@rem cleanin sln
msbuild %SLNPATH% /p:Configuration=Release /target:Clean || exit /b 1
@rem build version (.NET 4.0)
msbuild %SLNPATH% /p:Configuration=Release;TargetFrameworkVersion=v4.0 /toolsversion:4.0 /target:Build || exit /b 1


@rem set variables
set OUTDIR=build\Asn1Net.Forms.TreeView\lib
set SRCDIR=src\Asn1Net.Forms.TreeView\bin\Release

@rem prepare output directory
rmdir /S /Q %OUTDIR%
mkdir %OUTDIR%\net40 || exit /b 1

@rem copy files to output directory
copy %SRCDIR%\Asn1Net.Forms.TreeView.dll %OUTDIR%\net40\ || exit /b 1
copy %SRCDIR%\Asn1Net.Forms.TreeView.xml %OUTDIR%\net40\ || exit /b 1

@rem set license variables
set LICENSEDIR=build\Asn1Net.Forms.TreeView

@rem copy licenses to output directory
copy LICENSE %LICENSEDIR%\license.txt || exit /b 1
copy agpl-3.0.txt %LICENSEDIR% || exit /b 1
copy README.md %LICENSEDIR%\Readme.txt || exit /b 1

@rem copy 3rd party licences
pushd src\packages\Asn1Net.Reader.* || exit /b 1
copy 3rd-party-license.txt ..\..\..\%LICENSEDIR%\3rd-party-license.txt || exit /b 1
copy license.txt ..\..\..\%LICENSEDIR%\asn1net.reader-license.txt || exit /b 1
popd

@rem copy make_nuget.bat and nuspec file
set BUILDDIR=build
copy make_nuget.bat %BUILDDIR% || exit /b 1
copy Asn1Net.Forms.TreeView.nuspec %BUILDDIR%\Asn1Net.Forms.TreeView\ || exit /b 1

endlocal

@echo BUILD SUCCEEDED !!!
@exit /b 0