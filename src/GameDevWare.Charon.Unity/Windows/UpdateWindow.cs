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
using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Updates;
using GameDevWare.Charon.Unity.Updates.Packages.Deployment;
using GameDevWare.Charon.Unity.Utils;
using UnityEditor;
using UnityEngine;
using PackageInfo = GameDevWare.Charon.Unity.Utils.PackageInfo;

namespace GameDevWare.Charon.Unity.Windows
{
	internal class UpdateWindow : EditorWindow
	{
		private class ProductRow
		{
			public readonly string Id;
			public readonly string Name;
			public readonly bool Disabled;
			public Promise<SemanticVersion> CurrentVersion;
			public SemanticVersion SelectedVersion;
			public SemanticVersion[] AllVersions;
			public SemanticVersion ExpectedVersion;
			public Promise<PackageInfo[]> AllBuilds;
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
				this.CurrentVersion = Promise.FromResult<SemanticVersion>(null);
				this.AllBuilds = Promise.FromResult<PackageInfo[]>(null);
				this.Action = DeploymentAction.ACTION_SKIP;
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

		private static readonly string[] Actions = new[] { DeploymentAction.ACTION_SKIP, DeploymentAction.ACTION_UPDATE, DeploymentAction.ACTION_REPAIR, DeploymentAction.ACTION_DOWNLOAD };

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

			this.rows = Array.ConvertAll(ProductInformation.GetKnownProducts(), p => new ProductRow(p.Id, p.Name, p.Disabled) {
				CurrentVersion = p.CurrentVersion,
				AllBuilds = p.AllBuilds,
				Location = p.Location,
				ExpectedVersion = p.ExpectedVersion
			});

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

			if (Event.current.type == EventType.Repaint)
				this.UpdateColumnWidths();

			GUI.enabled = this.updatePromise == null && EditorApplication.isCompiling == false;

			var headerStyle = new GUIStyle(GUIStyle.none)
			{
				clipping = TextClipping.Clip,
				fontStyle = FontStyle.Bold,
				normal = EditorStyles.toolbar.normal
			};
			

			var cellStyle = new GUIStyle(GUIStyle.none)
			{
				clipping = TextClipping.Clip,
				normal = EditorStyles.whiteLabel.normal
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
			GUI.enabled = wasEnabled && this.rows.All(r => r.CurrentVersion.IsCompleted && r.AllBuilds.IsCompleted) && !EditorApplication.isCompiling;
			var actionText = this.rows.Any(r => r.Action != DeploymentAction.ACTION_SKIP) ? Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_UPDATE_BUTTON : Resources.UI_UNITYPLUGIN_ABOUT_CLOSE_BUTTON;
			if (this.updatePromise == null && GUILayout.Button(actionText, GUILayout.Width(80)))
			{
				if (actionText == Resources.UI_UNITYPLUGIN_ABOUT_CLOSE_BUTTON)
				{
					this.Close();
					return;
				}

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
			if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().y > 0)
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
					actions.Add(DeploymentAction.ACTION_SKIP);
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
				if ((row.Action == DeploymentAction.ACTION_DOWNLOAD || row.Action == DeploymentAction.ACTION_UPDATE) && row.AllVersions != null)
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
				else if (row.Action == DeploymentAction.ACTION_REPAIR)
				{
					GUILayout.Label(string.Empty, GUILayout.Width(width));
				}
				else if (row.Action == DeploymentAction.ACTION_SKIP && row.AllVersions != null)
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
			var builds = row.AllBuilds.HasErrors ? default(PackageInfo[]) : row.AllBuilds.GetResult();
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
				row.Action = DeploymentAction.ACTION_SKIP;
				row.ActionMask = (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_UPDATE)) | (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_SKIP));
				return;
			}

			var expectedBuild = builds.FirstOrDefault(b => b.Version == row.ExpectedVersion);
			var currentBuild = builds.FirstOrDefault(b => b.Version == currentVersion);
			if (expectedBuild == null && currentVersion != null && currentVersion > lastVersion)
			{
				// current installed build is the last one
				row.SelectedVersion = currentVersion;
				row.Action = DeploymentAction.ACTION_SKIP;
				row.ActionMask = (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_SKIP));
			}
			else if (File.Exists(row.Location) == false || (expectedBuild != null && !ReferenceEquals(currentBuild, expectedBuild)))
			{
				// missing file or invalid version is installed
				row.SelectedVersion = expectedBuild != null ? expectedBuild.Version : currentVersion ?? lastVersion;
				row.Action = DeploymentAction.ACTION_DOWNLOAD;
				row.ActionMask = (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_DOWNLOAD)) | (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_SKIP));
			}
			else if (currentBuild == null && currentVersion != null)
			{
				// current installed build is not found
				row.SelectedVersion = lastVersion;
				row.Action = DeploymentAction.ACTION_UPDATE;
				row.ActionMask = (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_UPDATE)) | (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_SKIP));
			}
			else
			{
				var hashAlgorithm = "SHA1";
				var expectedHashFile = row.Location + ".sha1";
				var expectedHash = File.Exists(expectedHashFile) == false ? null : File.ReadAllText(expectedHashFile);
				var actualHash = FileAndPathUtils.ComputeHash(row.Location, hashAlgorithm);
				if (expectedHash != null && string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase) == false)
				{
					if (Settings.Current.Verbose)
						Debug.LogWarning(string.Format("File's '{0}' {1} hash '{2}' differs from expected '{3}'. File should be repaired.", row.Location, hashAlgorithm, actualHash, expectedHash));

					// corrupted file
					row.SelectedVersion = lastVersion;
					row.Action = DeploymentAction.ACTION_REPAIR;
					row.ActionMask = (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_REPAIR)) | (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_UPDATE)) | (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_SKIP));
				}
				else if (currentVersion < lastVersion)
				{
					// outdated version
					row.SelectedVersion = lastVersion;
					row.Action = DeploymentAction.ACTION_UPDATE;
					row.ActionMask = (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_UPDATE)) | (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_SKIP));
				}
				else
				{
					// actual version
					row.SelectedVersion = currentVersion ?? lastVersion;
					row.Action = DeploymentAction.ACTION_SKIP;
					row.ActionMask = (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_UPDATE)) | (1 << Array.IndexOf(Actions, DeploymentAction.ACTION_SKIP));
				}
			}
		}
		public IEnumerable PerformUpdateAsync()
		{
			var deploymentActions = new List<DeploymentAction>();
			try
			{
				// downloading
				for (var i = 0; i < this.rows.Length; i++)
				{
					var row = this.rows[i];
					if (row.Disabled || row.Action == DeploymentAction.ACTION_SKIP)
						continue;

					var progressCallback = this.GetProgressReportFor(row, i + 1, this.rows.Length);
					var downloadVersion = default(SemanticVersion);
					if (row.Action == DeploymentAction.ACTION_DOWNLOAD || row.Action == DeploymentAction.ACTION_UPDATE)
					{
						downloadVersion = row.SelectedVersion;
					}
					else if (row.Action == DeploymentAction.ACTION_REPAIR)
					{
						downloadVersion = row.CurrentVersion.GetResult();
					}

					if (row.Id == ProductInformation.PRODUCT_CHARON)
					{
						deploymentActions.Add(new CharonDeploymentAction(downloadVersion, progressCallback));
					}
					else
					{
						deploymentActions.Add(new LibraryDeploymentAction(row.Id, downloadVersion, row.Location, progressCallback));
					}
				}

				if (deploymentActions.Count == 0)
				{
					yield break;
				}

				// call prepare
				yield return Promise.WhenAll(deploymentActions.ConvertAll(a => a.Prepare()).ToArray()).IgnoreFault();

				// call deploy
				yield return Promise.WhenAll(deploymentActions.ConvertAll(a => a.Complete()).ToArray()).IgnoreFault();

				foreach (var forceReImportPath in deploymentActions.SelectMany(a => a.ChangedAssets))
					AssetDatabase.ImportAsset(forceReImportPath, ImportAssetOptions.ForceUpdate);

				this.updateStatus = Resources.UI_UNITYPLUGIN_PROGRESS_DONE;
			}
			finally
			{
				foreach (var deploymentAction in deploymentActions)
				{
					deploymentAction.CleanUp();
				}
			}

			this.Close();

			// re-load window if not 'Close' button was pressed
			if (this.rows.All(r => r.Action == DeploymentAction.ACTION_SKIP) == false)
				Promise.Delayed(TimeSpan.FromSeconds(1)).ContinueWith(_ => EditorWindow.GetWindow<UpdateWindow>(utility: true));
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
