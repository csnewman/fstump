export CppCompilerAndLinker=clang
dotnet publish -r linux-x64 -c release
strip bin/release/netcoreapp2.2/linux-x64/publish/FStump
