# 删除旧版并重新构建

在项目根目录运行：

```powershell
Get-Process RealtimeTranslate.App -ErrorAction SilentlyContinue | Stop-Process
dotnet clean .\RealtimeTranslateCodex.sln -c Release
dotnet build .\RealtimeTranslateCodex.sln -c Release
Start-Process .\src\RealtimeTranslate.App\bin\Release\net8.0-windows10.0.19041.0\RealtimeTranslate.App.exe
```

