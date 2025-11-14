/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System.IO;
using System.Reflection;
using System.Threading;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.Routines;
using GameDevWare.Charon.Editor.Utils;
using GameDevWare.Charon.Editor.Windows;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

// ReSharper disable UnusedMember.Local
namespace GameDevWare.Charon.Editor
{
	internal static class CharonEditorMenu
	{
		[MenuItem("Tools/Charon/Generate C# Code", false, 1)]
		private static void GenerateCodeAndAssets()
		{
			if (!GenerateCodeAndAssetsCheck()) return;

			var generateCodeTask = GenerateSourceCodeRoutine.ScheduleAsync(
				progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_GENERATING_CODE_AND_ASSETS));

			generateCodeTask.LogFaultAsError();
			generateCodeTask.ContinueWithHideProgressBar();
			FocusConsoleWindow();
		}
		[MenuItem("Tools/Charon/Generate C# Code", true, 1)]
		private static bool GenerateCodeAndAssetsCheck()
		{
			return !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/Charon/Synchronize Assets", false, 2)]
		private static void SynchronizeAssets()
		{
			if (!SynchronizeAssetsCheck()) return;

			var cancellationSource = new CancellationTokenSource();
			var synchronizeAssetsTask = SynchronizeAssetsRoutine.ScheduleAsync(
				progressCallback: ProgressUtils.ShowCancellableProgressBar(Resources.UI_UNITYPLUGIN_GENERATING_CODE_AND_ASSETS, cancellationSource: cancellationSource),
				cancellationToken: cancellationSource.Token
			);
			synchronizeAssetsTask.LogFaultAsError();
			synchronizeAssetsTask.ContinueWithHideProgressBar(CancellationToken.None);

			FocusConsoleWindow();
		}
		[MenuItem("Tools/Charon/Synchronize Assets", true, 2)]
		private static bool SynchronizeAssetsCheck()
		{
			return !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/Charon/Validate Assets", false, 3)]
		private static void ValidateAll()
		{
			if (!ValidateAllCheck()) return;

			var validateTask = ValidateGameDataRoutine.ScheduleAsync(
				progressCallback: ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_VALIDATING_ASSETS)
			);
			validateTask.LogFaultAsError();
			validateTask.ContinueWithHideProgressBar();
			FocusConsoleWindow();
		}
		[MenuItem("Tools/Charon/Validate Assets", true, 3)]
		private static bool ValidateAllCheck()
		{
			return !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/Charon/Advanced/Extract Code Generation Templates...", false, 8)]
		private static void ExtractT4Templates()
		{
			if (!ExtractT4TemplatesCheck())
				return;

			var extractionPath = EditorUtility.OpenFolderPanel(Resources.UI_UNITYPLUGIN_SPECIFY_EXTRACTION_LOC_TITLE, "", "");
			if (string.IsNullOrEmpty(extractionPath))
				return;

			ExtractT4TemplatesRoutine.ScheduleAsync(extractionPath);
			FocusConsoleWindow();
		}
		[MenuItem("Tools/Charon/Advanced/Extract Code Generation Templates...", true, 10)]
		private static bool ExtractT4TemplatesCheck()
		{
			return !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;
		}

		[MenuItem("Tools/Charon/Troubleshooting/Report Issue...", false, 11)]
		private static void SendFeedback()
		{
			Application.OpenURL("https://github.com/gamedevware/charon/issues");
		}

		[MenuItem("Tools/Charon/Troubleshooting/Reset Preferences", false, 14)]
		private static void ResetPreferences()
		{
			var userDataDirectory = CharonFileUtils.CharonAppContentPath;
			if (Directory.Exists(userDataDirectory) == false)
				return;

			CharonEditorModule.Instance.Processes.CloseAll();

			Directory.Delete(userDataDirectory, recursive: true);
		}

		[MenuItem("Tools/Charon/Troubleshooting/Open Logs...", false, 17)]
		private static void OpenLogs()
		{
			if (string.IsNullOrEmpty(CharonFileUtils.LibraryCharonLogsPath) == false)
			{
				EditorUtility.OpenWithDefaultApp(CharonFileUtils.LibraryCharonLogsPath);
			}
		}
		[MenuItem("Tools/Charon/Troubleshooting/Open Logs...", true, 17)]
		private static bool OpenLogsCheck()
		{
			return string.IsNullOrEmpty(CharonFileUtils.LibraryCharonLogsPath) == false && Directory.Exists(CharonFileUtils.LibraryCharonLogsPath) && Directory.GetFiles(CharonFileUtils.LibraryCharonLogsPath).Length > 0;
		}

		[MenuItem("Tools/Charon/Troubleshooting/Verbose Logs", false, 20)]
		private static void VerboseLogs()
		{
			if (CharonEditorModule.Instance.Settings.LogLevel == CharonLogLevel.Verbose)
			{
				CharonEditorModule.Instance.Settings.LogLevel = CharonLogLevel.Normal;
			}
			else
			{
				CharonEditorModule.Instance.Settings.LogLevel = CharonLogLevel.Verbose;
			}
			Menu.SetChecked("Tools/Charon/Troubleshooting/Verbose Logs", CharonEditorModule.Instance.Settings.LogLevel == CharonLogLevel.Verbose);
		}
		[MenuItem("Tools/Charon/Troubleshooting/Verbose Logs", true, 20)]
		private static bool VerboseLogsCheck()
		{
			Menu.SetChecked("Tools/Charon/Troubleshooting/Verbose Logs", CharonEditorModule.Instance.Settings.LogLevel == CharonLogLevel.Verbose);
			return true;
		}

		[MenuItem("Tools/Charon/Open Documentation", false, 25)]
		private static void ShowDocumentation()
		{
			Application.OpenURL("https://gamedevware.github.io/charon/");
		}

		[MenuItem("Tools/Charon/Check for Updates...", false, 28)]
		public static void CheckUpdates()
		{
#if UNITY_2021_3_OR_NEWER
			UnityEditor.PackageManager.UI.Window.Open("com.gamedevware.charon");
#else
			var packageManagerWindowType = typeof(PackageManagerExtensions).Assembly.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow", throwOnError: true);
			var openWindowMethod = packageManagerWindowType.GetMethod("OpenPackageManager", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
			openWindowMethod.Invoke(null, new object[] { "com.gamedevware.charon" });
#endif
		}

		[MenuItem("Tools/Charon/Settings...", false, 31)]
		private static void ShowSettings()
		{
#if UNITY_2022_3_OR_NEWER
			SettingsService.OpenProjectSettings("Project/Charon");
			return;
#else
			var preferencesWindowType = typeof(EditorApplication).Assembly.GetType("UnityEditor.PreferencesWindow", throwOnError: false, ignoreCase: true);
			var settingsWindowType = typeof(EditorApplication).Assembly.GetType("UnityEditor.SettingsWindow", throwOnError: false, ignoreCase: true);
			if (preferencesWindowType != null)
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
				CharonEditorModule.Instance.Logger.Log(LogType.Warning, "Unable to locate preferences window. Please open it manually 'Edit -> Preferences...'.");
			}
#endif
		}

		[MenuItem("Assets/Create/Game Data")]
		private static void CreateGameDataAsset()
		{
			if (!CreateGameDataAssetCheck()) return;

			if (!TryGetActiveFolderPath(out var currentFolderPath))
			{
				currentFolderPath = "Assets";
			}

			var currentFolder = default(UnityObject);
			if (Selection.assetGUIDs.Length > 0)
			{
				currentFolder = AssetDatabase.LoadAssetAtPath<UnityObject>(AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]));
			}
			currentFolder ??= AssetDatabase.LoadAssetAtPath<UnityObject>(currentFolderPath);

			CreateGameDataWindow.ShowAsync(currentFolder).LogFaultAsAssert();
		}
		[MenuItem("Assets/Create/Game Data", true)]
		private static bool CreateGameDataAssetCheck()
		{
			return !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling;
		}

		internal static void FocusConsoleWindow()
		{
			var consoleWindowType = typeof(SceneView).Assembly.GetType("UnityEditor.ConsoleWindow", throwOnError: false);
			if (consoleWindowType == null)
				return;

			var consoleWindow = EditorWindow.GetWindow(consoleWindowType);
			if (consoleWindow != null)
				consoleWindow.Focus();
		}

		private static bool TryGetActiveFolderPath(out string path)
		{
			var tryGetActiveFolderPath = typeof(ProjectWindowUtil).GetMethod( "TryGetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic );
			if (tryGetActiveFolderPath == null)
			{
				path = null;
				return false;
			}

			var args = new object[] { null };
			var found = (bool)tryGetActiveFolderPath.Invoke( null, args );
			path = (string)args[0];
			return found;
		}
	}
}

