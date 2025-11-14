# 2025.4.3

- Use `#if` to hide `SettingsService` to prevent compilation errors in the 2021 editor.
- Update example assets

# 2025.4.1

- Added a plugin version label to the asset Inspector window to assist with troubleshooting and screenshot reporting.
- Ensured that a “Missing .NET SDK” message is logged to the Unity Editor console when the editor fails to start.
- Introduced a Resource Server information endpoint to help identify plugin versions when requesting assets from the Unity Editor.

# 2025.1.2

- Improved example code with additional comments for better clarity.
- Updated `CharonCli.Import/I18NImport` to return an `ImportReport` instance, providing detailed results of the import process.
- Refactored `CharonCli.RunT4Async` by splitting it into two separate methods: `RunT4Async` and `PreprocessT4Async`, improving usability and reducing confusion.
- Fixed an issue in bootstrap `.bat/.sh` scripts where exit codes of invoked tools were not properly checked, causing failures on the first run.
- Enhanced documentation for the `CharonCli` class and reorganized `CharonCliExamples` into multiple files for improved readability.
- Added a `BulkDelete` example in `CharonCliExamples`.
- Updated line endings in generated code to match the default OS format, with customization options available in the `.asset` Inspector window.

# 2025.1.1
## Build Scripts
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

## UI
- Introduced a "Create Game Data" window for naming game data files/classes prior to creation.
- Relocated code generation and import settings from ".gdjs/.gdmp" files to ".asset" files, establishing them as the primary assets for game data. All routines now reference these assets instead of ".gdjs/.gdmp" files.
- Implemented a custom `AssetImporter` for ".gdjs/.gdmp" files, featuring a "Reimport" button for these assets.
- Developed a custom property drawer for the `GameDataDocumentReference` type, enabling precise document reference by schema and ID.

## Unity
- Implemented automatic Charon log archiving on a weekly basis with auto-cleanup after one month.
- Added a local HTTP server to serve the project's C# types to "Formulas" and enabled code generation and asset reimport directly from Charon's UI.
