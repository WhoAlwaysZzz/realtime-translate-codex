# Contributing

Thanks for helping improve Realtime Translate Codex.

## Development setup

- Windows 10 or 11
- .NET 8 SDK
- Windows English OCR language support for live OCR testing

```powershell
dotnet restore .\RealtimeTranslateCodex.sln
dotnet build .\RealtimeTranslateCodex.sln --no-restore
dotnet run --project .\tests\RealtimeTranslate.Tests\RealtimeTranslate.Tests.csproj
dotnet format .\RealtimeTranslateCodex.sln --verify-no-changes --verbosity minimal
```

Open an issue before large architecture changes. Keep commits focused, test logic changes, and describe manual WPF verification in the pull request. Never commit API keys, local settings, screenshots, OCR archives, or user dictionaries.

By contributing, you agree that your contribution is licensed under the MIT License.
