# Build Scripts
- Updated `CharonCli` to return `Task` instead of `Promise` for improved asynchronous handling.
- Transitioned `CharonCli` to utilize the auto-updated `dotnet charon` tool, replacing the `GameDevWare.Charon` NuGet package.
- Introduced the `CharonCli.InitGameDataAsync` method for initializing game data files.
- Incorporated a `logsVerbosity` parameter across all `CharonCli` methods to enable log verbosity control.
- Modified all input and output models in `CharonCli` to utilize `JsonObject` or `JObject` when the JSON.NET library is included in the project.
- Expanded `CharonCli` with methods `ImportFromFileAsync`, `ExportToFileAsync`, `I18NImportFromFileAsync`, `I18NExportToFileAsync`, `CreatePatchToFileAsync`, `ApplyPatchFromFileAsync`, `BackupToFileAsync`, and `RestoreFromFileAsync` to facilitate file-based operations.
- Added `CharonCli.I18NImportAsync` and `I18NExportAsync` methods to support handling of localizable data within Unity's build scripts.
- Implemented the `I18NAddLanguageAsync` method to simplify the addition of new languages to game data via Unity's build scripts.
- Introduced a `clearOutputDirectory` flag in `CharonCli.GenerateCSharpCodeAsync` to enable the removal of outdated generated files before adding new ones.
- Added `CharonCli.RunCharonAsync` to execute any command supported by the `dotnet charon` tool.
- Added `CharonCli.RunT4Async` to execute the `dotnet t4` tool.
- Structured all Charon-related activities within `CharonEditorModule`, allowing subscription to `OnGameDataPreSourceCodeGeneration` and `OnGameDataPreSynchronization` events for extending functionality.

# UI
- Introduced a "Create Game Data" window for naming game data files/classes prior to creation.
- Relocated code generation and import settings from ".gdjs/.gdmp" files to ".asset" files, establishing them as the primary assets for game data. All routines now reference these assets instead of ".gdjs/.gdmp" files.
- Implemented a custom `AssetImporter` for ".gdjs/.gdmp" files, featuring a "Reimport" button for these assets.
- Developed a custom property drawer for the `GameDataDocumentReference` type, enabling precise document reference by schema and ID.

# Unity
- Implemented automatic Charon log archiving on a weekly basis with auto-cleanup after one month.
- Added a local HTTP server to serve the project's C# types to "Formulas" and enabled code generation and asset reimport directly from Charon's UI.
