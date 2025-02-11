# Build Scripts
- refactored `CharonCli` to provide `Task` instead of `Promise` as results.
- refactored `CharonCli` to use auto-updated `dotnet charon` tool instead of `GameDevWare.Charon` Nuget package.
- Added `CharonCli.InitGameDataAsync` method of game data file initialization.
- Added `logsVerbosity` parameter to all `CharonCli` as method of log verbosity control.
- Changed all input and output models in `CharonCli` to use `JsonObject` or `JObject` if JSON.NET library is added to the project.
- Added `CharonCli.ImportFromFileAsync`, `ExportToFileAsync`, `I18NImportFromFileAsync`, `I18NExportToFileAsync`, `CreatePatchToFileAsync`, `ApplyPatchFromFileAsync`, `BackupToFileAsync` and `RestoreFromFileAsync` methods to allow perform operations on files.
- Added `CharonCli.I18NImportAsync` and `I18NExportAsync` methods to provide means to work with localizable data from Unity's  build scripts.
- Added `I18NAddLanguageAsync` method to ease of addition of new language in game data from Unity's build scripts.
- Added `clearOutputDirectory` flag in `CharonCli.GenerateCSharpCodeAsync` to allow cleanup output directory from old generated files before adding new ones.
- Added `CharonCli.RunAsync` which allows to run any command supported by `dotnet charon` tool.
- Model all Charon related activities in `CharonEditorModule`. You can subscribe on `OnGameDataPreSourceCodeGeneration`, `OnGameDataPreSynchronization`, events there to extend existing functionality.

# UI
- Added "Create Game Data" window where you can name game data file/class before creation.
- Moved code generation, import settings from ".gdjs/.gdmp" file to ".asset". Now it is main asset regarding the game data. Every routine refers this asset input parameter instead of ".gdjs/.gdmp" file.
- Added custom `AssetImporter` for ".gdjs/.gdmp" files. Now these assets have "Reimport" button.
- Added custom property drawer for `GameDataDocumentReference` type which allows to refer exact document by schema and it's Id.
