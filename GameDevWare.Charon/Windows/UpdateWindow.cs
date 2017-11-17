/*
	Copyright (c) 2017 Denis Zykov

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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using GameDevWare.Charon.Async;
using GameDevWare.Charon.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Windows
{
	internal class UpdateWindow : EditorWindow
	{
		public const string ACTION_SKIP = Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_SKIP_ACTION;
		public const string ACTION_UPDATE = Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_UPDATE_ACTION;
		public const string ACTION_REPAIR = Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_REPAIR_ACTION;
		public const string ACTION_DOWNLOAD = Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_DOWNLOAD_ACTION;

		private class ProductRow
		{
			public readonly string Id;
			public readonly string Name;
			public readonly bool Disabled;
			public Promise<Version> CurrentVersion;
			public Version SelectedVersion;
			public Version[] AllVersions;
			public Version ExpectedVersion;
			public Promise<BuildInfo[]> AllBuilds;
			public string Location;
			public string Action;
			public int ActionMask;

			public ProductRow(string id, string name, bool disabled)
			{
				if (id == null) throw new ArgumentNullException("id");
				if (name == null) throw new ArgumentNullException("name");

				this.Id = id;
				this.Name = name;
				this.Disabled = disabled;
				this.CurrentVersion = Promise.FromResult<Version>(null);
				this.AllBuilds = Promise.FromResult<BuildInfo[]>(null);
				this.Action = ACTION_SKIP;
			}
		}
		private class ProductColumn
		{
			public readonly string Title;
			public readonly Action<ProductRow, float> Renderer;
			public float Width;
			public bool Flex;

			public ProductColumn(string title, Action<ProductRow, float> renderer)
			{
				if (title == null) throw new ArgumentNullException("title");
				if (renderer == null) throw new ArgumentNullException("renderer");

				this.Title = title;
				this.Renderer = renderer;
			}
		}

		private static readonly string[] Actions = new[] { ACTION_SKIP, ACTION_UPDATE, ACTION_REPAIR, ACTION_DOWNLOAD };

		private readonly ProductRow[] rows;
		private readonly ProductColumn[] columns;
		private readonly Rect padding;
		private Promise updatePromise;
		private string updateStatus;

		public UpdateWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_TITLE);
			this.minSize = new Vector2(380, 290);
			this.position = new Rect(
				(Screen.width - this.minSize.x) / 2,
				(Screen.height - this.minSize.y) / 2,
				this.minSize.x,
				this.minSize.y
			);
			this.padding = new Rect(10, 10, 10, 10);

			this.rows = new[] {
				new ProductRow(UpdateServerCli.PRODUCT_CHARON, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_CHARON_NAME, disabled: false) {
					CurrentVersion = CharonCli.GetVersionAsync().IgnoreFault(),
					AllBuilds = UpdateServerCli.GetBuilds(UpdateServerCli.PRODUCT_CHARON),
					Location = Path.GetFullPath(Settings.CharonPath),
					ExpectedVersion = string.IsNullOrEmpty(Settings.Current.EditorVersion) ? default(Version) : new Version(Settings.Current.EditorVersion)
				},
				new ProductRow(UpdateServerCli.PRODUCT_CHARON_UNITY, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_CHARON_UNITY_PLUGIN_NAME, disabled: !IsAssemblyLoaded(UpdateServerCli.PRODUCT_CHARON_UNITY_ASSEMBLY)) {
					CurrentVersion = Promise.FromResult(GetAssemblyVersion(UpdateServerCli.PRODUCT_CHARON_UNITY_ASSEMBLY)),
					AllBuilds = UpdateServerCli.GetBuilds(UpdateServerCli.PRODUCT_CHARON_UNITY),
					Location = GetAssemblyLocation(UpdateServerCli.PRODUCT_CHARON_UNITY_ASSEMBLY)
				},
				new ProductRow(UpdateServerCli.PRODUCT_EXPRESSIONS, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_EXPRESSIONS_PLUGIN_NAME, disabled: !IsAssemblyLoaded(UpdateServerCli.PRODUCT_EXPRESSIONS_ASSEMBLY)) {
					CurrentVersion = Promise.FromResult(GetAssemblyVersion(UpdateServerCli.PRODUCT_EXPRESSIONS_ASSEMBLY)),
					AllBuilds = UpdateServerCli.GetBuilds(UpdateServerCli.PRODUCT_EXPRESSIONS),
					Location = GetAssemblyLocation(UpdateServerCli.PRODUCT_EXPRESSIONS_ASSEMBLY)
				},
				new ProductRow(UpdateServerCli.PRODUCT_TEXT_TEMPLATES, Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_TEXT_TRANSFORM_PLUGIN_NAME, disabled: !IsAssemblyLoaded(UpdateServerCli.PRODUCT_TEXT_TEMPLATES_ASSEMBLY)) {
					CurrentVersion = Promise.FromResult(GetAssemblyVersion(UpdateServerCli.PRODUCT_TEXT_TEMPLATES_ASSEMBLY)),
					AllBuilds = UpdateServerCli.GetBuilds(UpdateServerCli.PRODUCT_TEXT_TEMPLATES),
					Location = GetAssemblyLocation(UpdateServerCli.PRODUCT_TEXT_TEMPLATES_ASSEMBLY)
				}
			};
			this.columns = new[] {
				new ProductColumn(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_PRODUCT_COLUMN_NAME, RenderProductCell) { Flex = true, Width = 10 },
				new ProductColumn(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_CURRENT_VERSION_COLUMN_NAME, RenderCurrentVersionCell ) { Width = 100 },
				new ProductColumn(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_AVAILABLE_VERSION_COLUMN_NAME, RenderSelectedVersionCell) { Width = 100 },
				new ProductColumn(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_ACTION_COLUMN_NAME, RenderActionCell) { Width = 80 }
			};

			this.UpdateColumnWidths();

			for (var i = 0; i < this.rows.Length; i++)
			{
				var row = this.rows[i];
				if (row.CurrentVersion.IsCompleted == false)
					row.CurrentVersion.ContinueWith(this.ContinueWithRepaint);
				if (row.AllBuilds.IsCompleted == false)
					row.AllBuilds.ContinueWith(this.ContinueWithRepaint);

				Promise.WhenAll(row.CurrentVersion, row.AllBuilds)
					.ContinueWith(p => ChooseAction(row))
					.ContinueWith(this.ContinueWithRepaint);
			}
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		protected void OnGUI()
		{
			if (this.columns == null) return;

			if (Event.current.type == EventType.repaint)
				this.UpdateColumnWidths();

			GUI.enabled = this.updatePromise == null && EditorApplication.isCompiling == false;

			var headerStyle = new GUIStyle(GUIStyle.none)
			{
				clipping = TextClipping.Clip,
				fontStyle = FontStyle.Bold,
				normal = {
					background = Texture2D.whiteTexture
				}
			};

			var cellStyle = new GUIStyle(GUIStyle.none)
			{
				clipping = TextClipping.Clip,
				normal = {
					background = Texture2D.whiteTexture
				}
			};

			// paddings
			GUILayout.BeginHorizontal(GUILayout.Width(this.position.width - this.padding.x - this.padding.width));
			GUILayout.Space(this.padding.x);
			GUILayout.BeginVertical(GUILayout.Width(this.position.height - this.padding.y - this.padding.height));
			GUILayout.Space(this.padding.y);

			// render headers
			GUILayout.BeginHorizontal(headerStyle);
			foreach (var column in this.columns)
			{
				GUILayout.Label(column.Title, GUILayout.Width(column.Width));
			}
			GUILayout.EndHorizontal();
			EditorGUILayout.Separator();
			// render rows
			foreach (var row in this.rows)
			{
				if (row.Disabled)
					continue;

				GUILayout.BeginHorizontal(cellStyle);
				foreach (var column in this.columns)
				{
					column.Renderer(row, column.Width);
				}
				GUILayout.EndHorizontal();
			}

			EditorGUILayout.Separator();
			GUILayout.BeginHorizontal();
			GUILayout.Label(this.updateStatus ?? string.Empty);
			EditorGUILayout.Space();
			var wasEnabled = GUI.enabled;
			GUI.enabled = wasEnabled && this.rows.All(r => r.CurrentVersion.IsCompleted && r.AllBuilds.IsCompleted);
			var actionText = this.rows.Any(r => r.Action != ACTION_SKIP) ? Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_UPDATE_BUTTON : Resources.UI_UNITYPLUGIN_ABOUT_CLOSE_BUTTON;
			if (this.updatePromise == null && GUILayout.Button(actionText, GUILayout.Width(80)))
			{
				this.updatePromise = new Coroutine<object>(this.PerformUpdateAsync());
				this.updatePromise.ContinueWith(p =>
				{
					if (p.HasErrors)
					{
						this.updateStatus = Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_ERROR_MESSAGE + ": " + p.Error.Unwrap().Message;
						Debug.LogError("An update process has ended with error." + Environment.NewLine + p.Error.Unwrap());
						Menu.FocusConsoleWindow();
					}
					this.Repaint();
				});
			}
			GUI.enabled = wasEnabled;
			GUILayout.EndHorizontal();

			// paddings
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();

			GUILayoutUtility.GetRect(1, 1, 1, 1);
			if (Event.current.type == EventType.repaint && GUILayoutUtility.GetLastRect().y > 0)
			{
				var newRect = GUILayoutUtility.GetLastRect();
				this.position = new Rect(this.position.position, new Vector2(this.position.width, newRect.y + 7));
				this.minSize = new Vector2(this.minSize.x, this.position.height);
				this.maxSize = new Vector2(this.maxSize.x, this.position.height);
			}

			GUI.enabled = true;
		}

		private void ContinueWithRepaint(Promise promise)
		{
			this.Repaint();
		}

		private static Version GetAssemblyVersion(string assemblyName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.GetName(copiedName: false).Name == assemblyName)
					return assembly.GetName().Version;
			}

			return null;
		}
		private static bool IsAssemblyLoaded(string assemblyName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.GetName(copiedName: false).Name == assemblyName)
					return true;
			}

			return false;
		}
		private static string GetAssemblyLocation(string assemblyName)
		{
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.GetName(copiedName: false).Name == assemblyName)
					return assembly.Location;
			}

			return null;
		}
		private static void RenderActionCell(ProductRow row, float width)
		{
			var completed = row.CurrentVersion.IsCompleted &&
				!row.CurrentVersion.HasErrors &&
				row.AllBuilds.IsCompleted &&
				!row.AllBuilds.HasErrors;

			var oldEnabled = GUI.enabled;
			GUI.enabled = oldEnabled && row.AllBuilds.IsCompleted && !row.AllBuilds.HasErrors && row.AllBuilds.GetResult() != null && row.SelectedVersion != null;
			if (completed)
			{
				var actions = new List<string>();
				for (var i = 0; i < Actions.Length; i++)
				{
					if ((row.ActionMask & (1 << i)) != 0)
						actions.Add(Actions[i]);
				}
				if (actions.Count == 0)
					actions.Add(ACTION_SKIP);
				var actionIndex = EditorGUILayout.Popup(actions.IndexOf(row.Action), actions.ToArray(), GUILayout.Width(width));
				if (actionIndex < 0)
					actionIndex = 0;
				row.Action = actions[actionIndex];
			}
			GUI.enabled = oldEnabled;
		}
		private static void RenderCurrentVersionCell(ProductRow row, float width)
		{
			var promise = row.CurrentVersion;
			if (promise.IsCompleted == false)
				GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_CHECKING_MESSAGE, GUILayout.Width(width));
			else if (promise.HasErrors)
				GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_ERROR_MESSAGE, GUILayout.Width(width));
			else
				GUILayout.Label(Convert.ToString(promise.GetResult()), GUILayout.Width(width));

		}
		private static void RenderSelectedVersionCell(ProductRow row, float width)
		{
			if (row.SelectedVersion != null)
			{
				if ((row.Action == ACTION_DOWNLOAD || row.Action == ACTION_UPDATE) && row.AllVersions != null)
				{
					var versionNames = Array.ConvertAll(row.AllVersions, v => v.ToString());
					var selectedVersion = Array.IndexOf(row.AllVersions, row.SelectedVersion);
					if (selectedVersion < 0)
						selectedVersion = 0;
					selectedVersion = EditorGUILayout.Popup(selectedVersion, versionNames, GUILayout.Width(width));
					if (selectedVersion < 0)
						selectedVersion = 0;
					row.SelectedVersion = row.AllVersions[selectedVersion];
				}
				else if (row.Action == ACTION_REPAIR)
				{
					GUILayout.Label(string.Empty, GUILayout.Width(width));
				}
				else if (row.Action == ACTION_SKIP && row.AllVersions != null)
				{
					GUILayout.Label(Convert.ToString(row.AllVersions.FirstOrDefault()), GUILayout.Width(width));
				}
				else
				{
					GUILayout.Label(row.SelectedVersion.ToString(), GUILayout.Width(width));
				}
			}
			else if (row.AllBuilds.IsCompleted == false)
			{
				GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_CHECKING_MESSAGE, GUILayout.Width(width));
			}
			else if (row.AllBuilds.IsCompleted && row.AllBuilds.HasErrors)
			{
				GUILayout.Label(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_ERROR_MESSAGE, GUILayout.Width(width));
			}
			else
			{
				GUILayout.Label(Convert.ToString(row.SelectedVersion), GUILayout.Width(width));
			}
		}

		private static void RenderProductCell(ProductRow row, float width)
		{
			GUILayout.Label(row.Name, GUILayout.Width(width));
		}
		private static void ChooseAction(ProductRow row)
		{
			var currentVersion = row.CurrentVersion.GetResult();
			var builds = row.AllBuilds.HasErrors ? default(BuildInfo[]) : row.AllBuilds.GetResult();
			if (builds == null || builds.Length == 0)
				return;

			var versions = Array.ConvertAll(builds, b => b.Version);
			Array.Sort(versions);
			Array.Reverse(versions);

			row.AllVersions = versions;
			var lastVersion = versions.FirstOrDefault();

			if (string.IsNullOrEmpty(row.Location) || lastVersion == null)
			{
				// no local artifacts
				row.SelectedVersion = null;
				row.Action = ACTION_SKIP;
				row.ActionMask = (1 << Array.IndexOf(Actions, ACTION_SKIP));
				return;
			}

			var expectedBuild = builds.FirstOrDefault(b => b.Version == row.ExpectedVersion);
			var currentBuild = builds.FirstOrDefault(b => b.Version == currentVersion);
			if (expectedBuild == null && currentVersion != null && currentVersion > lastVersion)
			{
				// current installed build is the last one
				row.SelectedVersion = currentVersion;
				row.Action = ACTION_SKIP;
				row.ActionMask = (1 << Array.IndexOf(Actions, ACTION_SKIP));
			}
			else if (File.Exists(row.Location) == false || (expectedBuild != null && currentBuild != expectedBuild))
			{
				// missing file or invalid version is installed
				row.SelectedVersion = expectedBuild != null ? expectedBuild.Version : currentVersion ?? lastVersion;
				row.Action = ACTION_DOWNLOAD;
				row.ActionMask = (1 << Array.IndexOf(Actions, ACTION_DOWNLOAD)) | (1 << Array.IndexOf(Actions, ACTION_SKIP));
			}
			else if (currentBuild == null && currentVersion != null)
			{
				// current installed build is not found
				row.SelectedVersion = lastVersion;
				row.Action = ACTION_DOWNLOAD;
				row.ActionMask = (1 << Array.IndexOf(Actions, ACTION_DOWNLOAD)) | (1 << Array.IndexOf(Actions, ACTION_SKIP));
			}
			else
			{
				var actualHash = FileAndPathUtils.ComputeHash(row.Location, currentBuild.HashAlgorithm);
				if (string.Equals(currentBuild.Hash, actualHash, StringComparison.OrdinalIgnoreCase) == false)
				{
					if (Settings.Current.Verbose)
						Debug.LogWarning(string.Format("File's '{0}' {1} hash '{2}' differs from expected '{3}'. File should be repaired.", row.Location, currentBuild.HashAlgorithm, actualHash, currentBuild.Hash));

					// corrupted file
					row.SelectedVersion = lastVersion;
					row.Action = ACTION_REPAIR;
					row.ActionMask = (1 << Array.IndexOf(Actions, ACTION_REPAIR)) | (1 << Array.IndexOf(Actions, ACTION_UPDATE)) | (1 << Array.IndexOf(Actions, ACTION_SKIP));
				}
				else if (currentVersion < lastVersion)
				{
					// outdated version
					row.SelectedVersion = lastVersion;
					row.Action = ACTION_UPDATE;
					row.ActionMask = (1 << Array.IndexOf(Actions, ACTION_UPDATE)) | (1 << Array.IndexOf(Actions, ACTION_SKIP));
				}
				else
				{
					// actual version
					row.SelectedVersion = currentVersion ?? lastVersion;
					row.Action = ACTION_SKIP;
					row.ActionMask = (1 << Array.IndexOf(Actions, ACTION_SKIP));
				}
			}
		}
		public IEnumerable PerformUpdateAsync()
		{
			var artifacts = new string[this.rows.Length];
			try
			{
				// downloading
				for (var i = 0; i < this.rows.Length; i++)
				{
					var row = this.rows[i];
					if (row.Disabled || row.Action == ACTION_SKIP)
						continue;

					var downloadProgress = this.GetProgressReportFor(row, i + 1, this.rows.Length);
					var downloadAsync = new Coroutine<string>(DownloadAsync(row, downloadProgress));
					yield return downloadAsync.IgnoreFault();
					if (downloadAsync.HasErrors)
					{
						Debug.LogWarning(string.Format("Failed to download build version '{1}' of '{0}' product. Error: {2}", row.Name, row.SelectedVersion, downloadAsync.Error.Unwrap()));
						continue;
					}
					artifacts[i] = downloadAsync.GetResult();
				}

				// cleanup
				for (var i = 0; i < this.rows.Length; i++)
				{
					var row = this.rows[i];
					if (row.Disabled || row.Action == ACTION_SKIP || artifacts[i] == null)
						continue;

					if (row.Id == UpdateServerCli.PRODUCT_CHARON)
						PreCharonDeploy(row.Location);
				}

				// deploy
				for (var i = 0; i < this.rows.Length; i++)
				{
					var row = this.rows[i];
					if (row.Disabled || row.Action == ACTION_SKIP || artifacts[i] == null)
						continue;

					try
					{
						File.Delete(row.Location);
						File.Move(artifacts[i], row.Location);
					}
					catch (Exception moveError)
					{
						Debug.LogError(string.Format("Failed to move downloaded file to new location '{0}'.", row.Location));
						Debug.LogError(moveError.Unwrap());
					}

					var deployProgress = this.GetProgressReportFor(row, i + 1, this.rows.Length);
					if (row.Id == UpdateServerCli.PRODUCT_CHARON && artifacts[i] != null)
						yield return new Coroutine<object>(PostCharonDeployAsync(row.Location, deployProgress)).IgnoreFault();
				}

				// post deploy
				for (var i = 0; i < this.rows.Length; i++)
				{
					var row = this.rows[i];
					if (row.Disabled || row.Action == ACTION_SKIP || artifacts[i] == null)
						continue;
					var assetLocation = FileAndPathUtils.MakeProjectRelative(row.Location);
					if (string.IsNullOrEmpty(assetLocation))
						continue;
					AssetDatabase.ImportAsset(assetLocation, ImportAssetOptions.ForceUpdate);
				}

				this.updateStatus = Resources.UI_UNITYPLUGIN_PROGRESS_DONE;
			}
			finally
			{
				foreach (var tempFile in artifacts)
				{
					try
					{
						File.Delete(tempFile);
					}
					catch {/*ignore delete error*/}
				}
			}

			this.Close();

			// re-load window if not 'Close' button was pressed
			if (this.rows.All(r => r.Action == ACTION_SKIP) == false)
				Promise.Delayed(TimeSpan.FromSeconds(1)).ContinueWith(_ => EditorWindow.GetWindow<UpdateWindow>(utility: true));
		}
		private static void PreCharonDeploy(string charonPath)
		{
			if (charonPath == null) throw new ArgumentNullException("charonPath");

			GameDataEditorWindow.FindAllAndClose();

			try
			{
				if (File.Exists(charonPath))
					File.Delete(charonPath);
				if (Directory.Exists(charonPath))
					Directory.Delete(charonPath);

				var charonDirectory = Path.GetDirectoryName(charonPath) ?? "";
				if (Directory.Exists(charonDirectory) == false)
					Directory.CreateDirectory(charonDirectory);
			}
			catch (Exception cleanupError)
			{
				Debug.LogWarning(string.Format("Failed to cleanup before '{0}' deployment to '{1}'. {2}.", Path.GetFileName(charonPath), Path.GetDirectoryName(charonPath), cleanupError.Message));
				Debug.LogError(cleanupError);
			}
		}
		private static IEnumerable PostCharonDeployAsync(string charonPath, Action<string, float> progressCallback = null)
		{
			// ensure config file
			var charonConfigPath = charonPath + ".config";
			if (File.Exists(charonConfigPath) == false)
			{
				var embeddedConfigStream = typeof(Menu).Assembly.GetManifestResourceStream("GameDevWare.Charon.Charon.exe.config");
				if (embeddedConfigStream != null)
				{
					using (embeddedConfigStream)
					using (var configFileStream = File.Create(charonConfigPath, 8 * 1024, FileOptions.SequentialScan))
					{
						var buffer = new byte[8 * 1024];
						var read = 0;
						while ((read = embeddedConfigStream.Read(buffer, 0, buffer.Length)) > 0)
							configFileStream.Write(buffer, 0, read);
					}
				}
			}

			var currentVersion = default(Version);
			if (File.Exists(charonPath))
			{
				if (progressCallback != null) progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.95f);

				var checkToolsVersion = CharonCli.GetVersionAsync();
				yield return checkToolsVersion.IgnoreFault();
				currentVersion = checkToolsVersion.HasErrors ? default(Version) : checkToolsVersion.GetResult();
			}

			Settings.Current.EditorVersion = currentVersion != null ? currentVersion.ToString() : null;
			Settings.Current.Save();
		}
		private static IEnumerable DownloadAsync(ProductRow row, Action<string, float> progressCallback = null)
		{

			var downloadVersion = default(Version);
			if (row.Action == ACTION_DOWNLOAD || row.Action == ACTION_UPDATE)
				downloadVersion = row.SelectedVersion;
			else if (row.Action == ACTION_REPAIR)
				downloadVersion = row.CurrentVersion.GetResult();

			if (downloadVersion == null)
				yield break;

			var downloadPath = Path.GetTempFileName();
			var downloadSuccess = false;
			try
			{
				var downloadAsync = UpdateServerCli.DownloadBuild(row.Id, downloadVersion, downloadPath, progressCallback);
				yield return downloadAsync;
				downloadSuccess = true;
				yield return downloadPath;
			}
			finally
			{
				if (downloadSuccess == false && File.Exists(downloadPath))
				{
					try
					{
						File.Delete(downloadPath);
					}
					catch { /* ignore delete error */ }
				}
			}

		}
		private Action<string, float> GetProgressReportFor(ProductRow row, int number, int total)
		{
			return (msg, progress) =>
			{
				this.updateStatus = string.Format("{0} [{1}/{2}]: {3} ({4:F2}%)", row.Name, number, total, msg, progress * 100);
				this.Repaint();
			};
		}

		private void UpdateColumnWidths()
		{
			if (this.columns == null) return;

			var nonFlexWidth = 0.0f;
			var flexCount = 0;
			foreach (var column in this.columns)
			{
				if (column.Flex)
					flexCount++;
				else
					nonFlexWidth += column.Width;
			}

			var flexWidth = this.position.width - nonFlexWidth - this.padding.x - this.padding.width - 20;
			foreach (var column in this.columns)
			{
				if (column.Flex == false)
					continue;

				column.Width = flexWidth / flexCount;
			}
		}
	}
}
