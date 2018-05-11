rd /S /Q .\bin\Release
rd /S /Q .\Publishing\lib

.nuget\NuGet.exe restore Metrics.sln

"C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MsBuild.exe" Metrics.sln /target:Clean;Rebuild /p:Configuration=Release /verbosity:m /p:zip=false
if %errorlevel% neq 0 exit /b %errorlevel%

md .\Publishing\lib
md .\Publishing\lib\net45

copy .\bin\Release\Metrics.dll .\Publishing\lib\net45\
copy .\bin\Release\Metrics.xml .\Publishing\lib\net45\
copy .\bin\Release\Metrics.pdb .\Publishing\lib\net45\

.\.nuget\NuGet.exe pack .\Publishing\Metrics.Net.nuspec -OutputDirectory .\Publishing
if %errorlevel% neq 0 exit /b %errorlevel%
