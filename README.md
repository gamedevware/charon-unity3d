[![openupm](https://img.shields.io/npm/v/com.gamedevware.charon?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.gamedevware.charon/)

# Introduction

Charon is a versatile plugin tailored for Unity, designed to facilitate data-driven game design
by allowing both developers and game designers to efficiently manage static game data,
like units, items, missions, quests, and other. It provides a user-friendly interface that requires no special skills
for game designers, simplifying the process of data manipulation. For programmers, Charon streamlines
development workflows by generating c# code to load game data seamlessly into the game.

<img width="800" alt="editor ui" src="https://raw.githubusercontent.com/gamedevware/charon/refs/heads/main/docs/assets/editor_screenshot.png"/>  

# Installation

Prerequisites
---------------
Unity Editor plugin uses `dotnet charon` tool, which is a .NET Core application built for [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) and it runs wherever Unity Editor can run.  

The generated code and data do not require any additional dependencies for your game's runtime.  

Installation from OpenUPM (recommended)
---------------------------------------

1. Install the [.NET 8 or later](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) software for your operating system (Windows, MacOS, Linux).
2. Ensure your Unity version is 2021.3 or later.
3. Open the [OpenUPM](https://openupm.com/packages/com.gamedevware.charon/) page for the plugin.
4. Click the **Manual Installation** button in the upper right corner and follow the instructions.


Installation from Unity Asset Store
-----------------------------------

1. Install the [.NET 8 or later](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) software for your operating system (Windows, MacOS, Linux).
2. Ensure your Unity version is 2021.3 or later.
3. Open the [Charon plugin](https://assetstore.unity.com/packages/tools/visual-scripting/game-data-editor-charon-95117) in the Unity Asset Store.
4. Click **Add To My Assets**.
5. Open the Unity Package Manager by navigating to **Window → Package Manager**.
6. Wait for the package manager to populate the list.
7. Select **My Assets** from the dropdown in the top left corner.
8. Select **Charon** from the list and click **Download**. If it’s already downloaded, you will see an **Import** option.

# How to Use

- [Working with the Plugin](https://gamedevware.github.io/charon/unity/overview.html#working-with-the-plugin)

# Documentation

[Charon Documentation](https://gamedevware.github.io/charon/) • [Unity Plugin Documentation](https://gamedevware.github.io/charon/unity/overview.html)

# Examples
- [Example Project](https://github.com/gamedevware/charon-unity3d/tree/master/src/GameDevWare.Charon.Unity)
- [Editor Extensions](https://github.com/gamedevware/charon-unity3d/tree/master/src/GameDevWare.Charon.Unity/Assets/Editor)
  - [CharonCli -> Bulk Create Documents](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.BulkCreateHeroes.cs)
  - [CharonCli -> Bulk Delete Documents](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.BulkDeleteHeroes.cs)
  - [CharonCli -> Find Document](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.FindHeroById.cs)
  - [CharonCli -> Update Document](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.UpdateHero.cs)
  - [CharonCli -> Create Document](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.CreateHero.cs)
  - [CharonCli -> Delete Document](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.DeleteHero.cs)
  - [CharonCli -> Delete Document By Id](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.DeleteHeroById.cs)
  - [CharonCli -> Export Documents](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.ExportHeroes.cs)
  - [CharonCli -> List All Documents](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.ListItems.cs)
  - [CharonCli -> List Documents With Criteria](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.ListReligiousHeroes.cs)
  - [CharonCli -> Validating Game Data File](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.ValidateGameData.cs)  
  - [I18N -> List Translation Languages](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.ListTranslationLanguages.cs)
  - [I18N -> Export Localizable Data](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.ExportLocalizableData.cs)
  - [I18N -> Add Translation Language](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.AddTranslationLanguage.cs)
  - [I18N -> Import Localizable Data](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.ImportLocalizableData.cs)
  - [T4 -> Preprocess Template](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.PreprocessTemplateIntoGenerator.cs)
  - [T4 -> Run Text Template](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.RunT4Template.cs)
  - [T4 -> Run C# Code Template](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Assets/Editor/CharonExamples/CharonCliExamples.RunT4Template2.cs)

# Change Log
[Change Log](https://github.com/gamedevware/charon-unity3d/blob/master/src/GameDevWare.Charon.Unity/Packages/com.gamedevware.charon/CHANGELOG.md)

# Collaboration

[Discord](https://discord.gg/2quB5vXryd) • [Plugin Issues](https://github.com/gamedevware/charon-unity3d/issues) • [support@gamedevware.com](mailto:support@gamedevware.com)

# License

MIT
