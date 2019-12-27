using System;
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Updates;
using GameDevWare.Charon.Unity.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Windows
{
	public sealed class CharonSettingsWindow
	{
		private static string editorVersion = Resources.UI_UNITYPLUGIN_WINDOW_CHECKING_VERSION;
		private static string assetVersion = (Settings.GetCurrentAssetVersion() ?? new Version()).ToString();

		private static Promise<SemanticVersion> checkToolsVersion;
		private static Promise<RequirementsCheckResult> checkRequirements;
		private static bool isUpdateSubscribed;

		private static bool HasNewCharonVersion
		{
			get
			{
				return checkToolsVersion != null &&
					checkToolsVersion.IsCompleted &&
					!checkToolsVersion.HasErrors &&
					checkToolsVersion.GetResult() != null &&
					UpdateChecker.GetLastCharonVersion() != null &&
					UpdateChecker.GetLastCharonVersion() > checkToolsVersion.GetResult();
			}
		}

		[PreferenceItem("Charon")]
		public static void PreferencesGUI()
		{
			if (!isUpdateSubscribed)
			{
				EditorApplication.update += Update;
				isUpdateSubscribed = true;
			}

			GUILayout.Space(10);
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOW_INFO_GROUP, EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_ABOUT_EDITOR_VERSION_LABEL, editorVersion);
			if (HasNewCharonVersion)
			{
				if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_AVAILABLE_TITLE, EditorStyles.toolbarButton, GUILayout.Width(120), GUILayout.Height(18)))
				{
					EditorWindow.GetWindow<UpdateWindow>(utility: true);
					GUI.changed = true;
				}
				GUILayout.Space(5);
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOW_ASSET_VERSION_LABEL, assetVersion);
			EditorGUILayout.LabelField(Resources.UI_UNITYPLUGIN_WINDOW_EXTENSIONS_LABEL, string.Join(", ", Settings.SupportedExtensions));
			GUI.enabled = true;
			GUILayout.Space(10);
			GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOW_SETTINGS_GROUP, EditorStyles.boldLabel);
			Settings.Current.EditorPort = EditorGUILayout.IntField(Resources.UI_UNITYPLUGIN_ABOUT_EDITOR_PORT, Settings.Current.EditorPort);
			Settings.Current.Browser = Convert.ToInt32(EditorGUILayout.EnumPopup(Resources.UI_UNITYPLUGIN_WINDOW_BROWSER, (BrowserType)Settings.Current.Browser));
			if (Settings.Current.Browser == (int)BrowserType.Custom)
			{
				EditorGUILayout.BeginHorizontal();
				{
					Settings.Current.BrowserPath = EditorGUILayout.TextField(Resources.UI_UNITYPLUGIN_WINDOW_BROWSER_PATH, Settings.Current.BrowserPath);
					if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOW_BROWSE_BUTTON, EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(18)))
					{
						Settings.Current.BrowserPath = EditorUtility.OpenFilePanel(Resources.UI_UNITYPLUGIN_WINDOW_BROWSER_PATH_TITLE, "", "");
						GUI.changed = true;
					}
					GUILayout.Space(5);
				}
				EditorGUILayout.EndHorizontal();
			}
			else
				GUILayout.Space(18);

			GUILayout.Space(18);

			if (GUI.changed)
				Settings.Current.Save();
		}

		public static void Update()
		{
			if (checkRequirements == null)
				checkRequirements = CharonCli.CheckRequirementsAsync();
			if (checkRequirements.IsCompleted == false)
				return;

			if (checkRequirements.HasErrors)
			{
				editorVersion = checkRequirements.Error.Unwrap().Message;
				return;
			}

			var result = checkRequirements.GetResult();
			// ReSharper disable once SwitchStatementMissingSomeCases REASON: Other cases are irrelevant for display purposes
			switch (result)
			{
				case RequirementsCheckResult.MissingRuntime:
					editorVersion = Resources.UI_UNITYPLUGIN_WINDOW_CHECK_RESULT_MISSING_MONO_OR_DOTNET;
					break;
				case RequirementsCheckResult.MissingExecutable:
					editorVersion = Resources.UI_UNITYPLUGIN_WINDOWCHECK_RESULT_MISSING_TOOLS;
					break;
				case RequirementsCheckResult.Ok:
					if (checkToolsVersion == null)
					{
						editorVersion = Resources.UI_UNITYPLUGIN_WINDOW_CHECKING_VERSION;
						checkToolsVersion = CharonCli.GetVersionAsync();
						checkToolsVersion.ContinueWith(r =>
						{
							if (r.HasErrors)
								editorVersion = r.Error.Unwrap().Message;
							else
								editorVersion = r.GetResult().ToString();
						});
					}
					break;

			}
		}
	}
}
