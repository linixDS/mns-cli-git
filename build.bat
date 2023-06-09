@echo off

if "%1" == "install" (
     if not exist ".\build" mkdir .\build

     if not exist ".\build\modules" mkdir .\build\modules
     if not exist ".\build\.cache" mkdir .\build\.cache

     if not exist ".\build\modules\cache" mkdir .\build\modules\cache
     copy .\cache\bin\Debug\net7.0\cache.dll .\.\build\modules\cache\
     copy .\cache\package.json .\.\build\modules\cache\
     copy .\cache\bin\Debug\net7.0\core.dll .\.\build\modules\cache\


     if not exist ".\build\modules\fortigate" mkdir .\build\modules\fortigate
     copy .\fortigate\bin\Debug\net7.0\fortigate.dll .\.\build\modules\fortigate\
     copy .\fortigate\package.json .\.\build\modules\fortigate\
     copy .\fortigate\bin\Debug\net7.0\core.dll .\.\build\modules\fortigate\

     if not exist ".\build\modules\network" mkdir .\build\modules\network

     copy .\network\bin\Debug\net7.0\network.dll .\.\build\modules\network\
     copy .\network\package.json .\.\build\modules\network\
     copy .\network\bin\Debug\net7.0\core.dll .\.\build\modules\network\

     if not exist ".\build\modules\module" mkdir .\build\modules\module

     copy .\module\bin\Debug\net7.0\network.dll .\.\build\modules\module\
     copy .\module\package.json .\.\build\modules\module\
     copy .\module\bin\Debug\net7.0\core.dll .\.\build\modules\module\

     if not exist ".\build\modules\remote" mkdir .\build\modules\remote

     copy .\remote\bin\Debug\net7.0\remote.dll .\.\build\modules\remote\
     copy .\remote\package.json .\.\build\modules\remote\
     copy .\remote\vnc.exe .\.\build\modules\remote\
     copy .\remote\bin\Debug\net7.0\core.dll .\.\build\modules\remote\

     if not exist ".\build\modules\mikrotik" mkdir .\build\modules\mikrotik

     copy .\mikrotik\bin\Debug\net7.0\remote.dll .\.\build\modules\mikrotik\
     copy .\mikrotik\package.json .\.\build\modules\mikrotik\
     copy .\mikrotik\winbox64.exe .\.\build\modules\mikrotik\
     copy .\mikrotik\bin\Debug\net7.0\core.dll .\.\build\modules\mikrotik\


     copy .\mns-cli\bin\Debug\net7.0\mns-cli.dll .\.\build\
     copy .\mns-cli\bin\Debug\net7.0\mns-cli.exe .\.\build\
     copy .\mns-cli\bin\Debug\net7.0\core.dll .\.\build\
     copy .\mns-cli\bin\Debug\net7.0\runtime.dll .\.\build\

     copy .\mns-cli\bin\Debug\net7.0\mns-cli.runtimeconfig.json .\.\build\

    exit 0
)

if "%1" == "build" (
    echo "----------------------------------------------"
    echo "::Compiling library core.dll"
    echo "----------------------------------------------"
    cd .\core\
    dotnet build
    cd ..

    echo "----------------------------------------------"
    echo "::Compiling library runtime.dll"
    echo "----------------------------------------------"
    cd .\runtime\
    dotnet build
    cd ..

    echo "----------------------------------------------"
    echo "::Compiling library cache.dll"
    echo "----------------------------------------------"
    cd .\cache\
    dotnet build
    cd ..

    echo "----------------------------------------------"
    echo "::Compiling library mikrotik.dll"
    echo "----------------------------------------------"
    cd .\mikrotik\
    dotnet build
    cd ..

    echo "----------------------------------------------"
    echo "::Compiling module network.dll"
    echo "----------------------------------------------"
    cd .\network\
    dotnet build
    cd ..

    echo "----------------------------------------------"
    echo "::Compiling module remote.dll"
    echo "----------------------------------------------"
    cd .\remote\
    dotnet build
    cd ..

    echo "----------------------------------------------"
    echo "::Compiling module fortigate.dll"
    echo "----------------------------------------------"
    cd .\fortigate\
    dotnet build
    cd ..

    echo "----------------------------------------------"
    echo "::Compiling module module.dll"
    echo "----------------------------------------------"
    cd .\module\
    dotnet build
    cd ..

    echo "----------------------------------------------"
    echo "::Compiling program mns-cli"
    echo "----------------------------------------------"
    cd .\mns-cli\
    dotnet build
    cd ..

    exit 0
)

echo "build.bat build"
echo "build.bat install"



