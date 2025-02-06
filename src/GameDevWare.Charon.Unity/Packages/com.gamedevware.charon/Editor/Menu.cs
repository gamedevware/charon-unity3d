/*
	Copyright (c) 2023 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
*/

using System;
using System.IO;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Routines;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

// ReSharper disable UnusedMember.Local
namespace GameDevWare.Charon.Unity
{
	internal static class Menu
	{
		public const string TOOLS_PREFIX = "Tools/Charon/";
		public const string ASSETS_CREATE_PREFIX = "Assets/Create/";
		public const string TROUBLESHOOTING_PREFIX = TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_TROUBLESHOOTING + "/";
		public const string ADVANCED_PREFIX = TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_ADVANCED + "/";

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_GENERATE_CODE_AND_ASSETS, false, 1)]
		private static void GenerateCodeAndAssets()
		{
			if (!GenerateCodeAndAssetsCheck()) return;

			var generateCoroutine = GenerateCodeAndAssetsRoutine.Schedule(
				progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_GENERATING_CODE_AND_ASSETS));
			generateCoroutine.ContinueWith(ProgressUtils.HideProgressBar);
			FocusConsoleWindow();
		}
		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_GENERATE_CODE_AND_ASSETS, true, 1)]
		private static bool GenerateCodeAndAssetsCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_SYNCHRONIZE_ASSETS, false, 2)]
		private static void SynchronizeAssets()
		{
			if (!SynchronizeAssetsCheck()) return;

			var cancellation = new Promise();
			var generateCoroutine = SynchronizeAssetsRoutine.Schedule(
				force: true,
				progressCallback: ProgressUtils.ShowCancellableProgressBar(Resources.UI_UNITYPLUGIN_GENERATING_CODE_AND_ASSETS, cancellation: cancellation),
				cancellation: cancellation);
			generateCoroutine.ContinueWith(ProgressUtils.HideProgressBar);

			FocusConsoleWindow();
		}
		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_SYNCHRONIZE_ASSETS, true, 2)]
		private static bool SynchronizeAssetsCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VALIDATE_ASSETS, false, 3)]
		private static void ValidateAll()
		{
			if (!ValidateAllCheck()) return;

			var validateCoroutine = ValidateGameDataRoutine.Schedule(
				progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_VALIDATING_ASSETS)
			);
			validateCoroutine.ContinueWith(ProgressUtils.HideProgressBar);
			FocusConsoleWindow();
		}
		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VALIDATE_ASSETS, true, 3)]
		private static bool ValidateAllCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(ADVANCED_PREFIX + Resources.UI_UNITYPLUGIN_MENU_EXTRACT_T4_TEMPLATES, false, 8)]
		private static void ExtractT4Templates()
		{
			if (!ExtractT4TemplatesCheck())
				return;

			var extractionPath = EditorUtility.OpenFolderPanel(Resources.UI_UNITYPLUGIN_SPECIFY_EXTRACTION_LOC_TITLE, "", "");
			if (string.IsNullOrEmpty(extractionPath))
				return;

			ExtractT4TemplatesRoutine.Schedule(extractionPath);
			FocusConsoleWindow();
		}
		[MenuItem(ADVANCED_PREFIX + Resources.UI_UNITYPLUGIN_MENU_EXTRACT_T4_TEMPLATES, true, 10)]
		private static bool ExtractT4TemplatesCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(ADVANCED_PREFIX + Resources.UI_UNITYPLUGIN_MENU_USE_BETA_FEED, false, 20)]
		private static void UseCharonFeed()
		{
			Settings.Current.UseBetaFeed = !Settings.Current.UseBetaFeed;
			UnityEditor.Menu.SetChecked(ADVANCED_PREFIX + Resources.UI_UNITYPLUGIN_MENU_USE_BETA_FEED, Settings.Current.UseBetaFeed);
			Settings.Current.Save();
		}
		[MenuItem(ADVANCED_PREFIX + Resources.UI_UNITYPLUGIN_MENU_USE_BETA_FEED, true, 20)]
		private static bool UseCharonFeedCheck()
		{
			UnityEditor.Menu.SetChecked(ADVANCED_PREFIX + Resources.UI_UNITYPLUGIN_MENU_USE_BETA_FEED, Settings.Current.UseBetaFeed);
			return true;
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_SEND_FEEDBACK, false, 11)]
		private static void SendFeedback()
		{
			Application.OpenURL("https://github.com/gamedevware/charon/issues");
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_RESET_PREFERENCES, false, 14)]
		private static void ResetPreferences()
		{
			var userDataDirectory = Settings.UserDataPath;
			if (Directory.Exists(userDataDirectory) == false)
				return;

			GameDataEditorWindow.FindAllAndClose();

			Directory.Delete(userDataDirectory, recursive: true);
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_OPEN_LOGS, false, 17)]
		private static void OpenLogs()
		{
			if (string.IsNullOrEmpty(Settings.LibraryCharonLogsPath) == false)
			{
				EditorUtility.OpenWithDefaultApp(Settings.LibraryCharonLogsPath);
			}
		}
		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_OPEN_LOGS, true, 17)]
		private static bool OpenLogsCheck()
		{
			return string.IsNullOrEmpty(Settings.LibraryCharonLogsPath) == false && Directory.Exists(Settings.LibraryCharonLogsPath) && Directory.GetFiles(Settings.LibraryCharonLogsPath).Length > 0;
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, false, 20)]
		private static void VerboseLogs()
		{
			Settings.Current.Verbose = !Settings.Current.Verbose;
			UnityEditor.Menu.SetChecked(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, Settings.Current.Verbose);
			Settings.Current.Save();
		}
		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, true, 20)]
		private static bool VerboseLogsCheck()
		{
			UnityEditor.Menu.SetChecked(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_VERBOSE_LOGS, Settings.Current.Verbose);
			return true;
		}

		[MenuItem(TROUBLESHOOTING_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CHECK_RUNTIME, false, 22)]
		private static void CheckRuntime()
		{
			UpdateRuntimeWindow.ShowAsync(autoClose: false);
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_DOCUMENTATION, false, 25)]
		private static void ShowDocumentation()
		{
			Application.OpenURL("https://gamedevware.github.io/charon/");
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES, false, 28)]
		private static void CheckUpdates()
		{
			if (!CheckUpdatesCheck()) return;

			EditorWindow.GetWindow<UpdateWindow>(utility: true);
		}
		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CHECK_UPDATES, true, 28)]
		private static bool CheckUpdatesCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(TOOLS_PREFIX + Resources.UI_UNITYPLUGIN_MENU_SETTINGS, false, 31)]
		private static void ShowSettings()
		{
			var settingsService = typeof(EditorApplication).Assembly.GetType("UnityEditor.SettingsService", throwOnError: false, ignoreCase: true);
			var preferencesWindowType = typeof(EditorApplication).Assembly.GetType("UnityEditor.PreferencesWindow", throwOnError: false, ignoreCase: true);
			var settingsWindowType = typeof(EditorApplication).Assembly.GetType("UnityEditor.SettingsWindow", throwOnError: false, ignoreCase: true);
			if (settingsService != null)
			{
				settingsService.Invoke("OpenUserPreferences", "Preferences/Charon");
			}
			else if (preferencesWindowType != null)
			{
				var settingsWindow = EditorWindow.GetWindow(preferencesWindowType);
				settingsWindow.Show();
				settingsWindow.Focus();
			}
			else if (settingsWindowType != null)
			{
				var settingsWindow = EditorWindow.GetWindow(settingsWindowType);
				settingsWindow.Show();
				settingsWindow.Focus();
			}
			else
			{
				Debug.LogWarning("Unable to locate preferences window. Please open it manually 'Edit -> Preferences...'.");
			}
		}

		[MenuItem(ASSETS_CREATE_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_JSON)]
		private static void CreateGameDataJsonAsset()
		{
			if (!CreateGameDataAssetJsonCheck()) return;

			CreateGameData(GameDataStoreFormat.Json);
		}
		[MenuItem(ASSETS_CREATE_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_JSON, true)]
		private static bool CreateGameDataAssetJsonCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem(ASSETS_CREATE_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_MESSAGEPACK)]
		private static void CreateGameDataMsgPackAsset()
		{
			if (!CreateGameDataAssetMsgPackCheck()) return;

			CreateGameData(GameDataStoreFormat.MessagePack);
		}
		[MenuItem(ASSETS_CREATE_PREFIX + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA + "/" + Resources.UI_UNITYPLUGIN_MENU_CREATE_GAMEDATA_MESSAGEPACK, true)]
		private static bool CreateGameDataAssetMsgPackCheck()
		{
			return !CoroutineScheduler.IsRunning && !EditorApplication.isCompiling;
		}

		private static void CreateGameData(GameDataStoreFormat format)
		{
			var location = Path.GetFullPath("./Assets");
			if (Selection.activeObject != null)
			{
				location = Path.GetFullPath(AssetDatabase.GetAssetPath(Selection.activeObject));
				if (File.Exists(location))
				{
					location = Path.GetDirectoryName(location);
				}
			}

			if (string.IsNullOrEmpty(location) || Directory.Exists(location) == false)
			{
				Debug.LogWarning("Unable to create GameData file because 'Selection.activeObject' is null or wrong asset. Select Folder in Project window and try again.");
				return;
			}

			var i = 1;
			var extension = StorageFormats.GetStoreFormatExtension(format);
			var gameDataPath = Path.Combine(location, "GameData" + extension);

			while (File.Exists(gameDataPath))
				gameDataPath = Path.Combine(location, "GameData" + (i++) + extension);


			gameDataPath = FileHelper.MakeProjectRelative(gameDataPath);

			File.WriteAllText(gameDataPath, "");
			AssetDatabase.Refresh();

			var gameDataAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(gameDataPath);

			EditorGUIUtility.PingObject(gameDataAsset);
		}

		public static void FocusConsoleWindow()
		{
			var consoleWindowType = typeof(SceneView).Assembly.GetType("UnityEditor.ConsoleWindow", throwOnError: false);
			if (consoleWindowType == null)
				return;

			var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
			if (consoleWindow != null)
				consoleWindow.Focus();
		}
	}
}

