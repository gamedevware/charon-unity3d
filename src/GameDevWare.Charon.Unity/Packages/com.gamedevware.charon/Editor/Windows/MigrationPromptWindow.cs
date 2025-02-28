/*
	Copyright (c) 2025 Denis Zykov

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

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

// ReSharper disable Unity.RedundantSerializeFieldAttribute
namespace GameDevWare.Charon.Editor.Windows
{
	internal class MigrationPromptWindow : EditorWindow
	{
		private static readonly Rect Padding = new Rect(10, 10, 10, 10);

		[NonSerialized] private string lastError;
		[NonSerialized] private string progressStatus;
		[NonSerialized] private Task migrationTask;
		[NonSerialized] private readonly ILogger logger;

		[SerializeField] private bool autoClose;

		private event EventHandler Done;
		private event EventHandler<ErrorEventArgs> Cancel;

		public MigrationPromptWindow()
		{
			this.titleContent = new GUIContent("Charon Plugin Migration");
			this.minSize = new Vector2(480, 230);
			this.position = new Rect(
				(Screen.width - this.minSize.x) / 2,
				(Screen.height - this.minSize.y) / 2,
				this.minSize.x,
				this.minSize.y
			);
			this.lastError = string.Empty;
			this.progressStatus = string.Empty;
			this.logger = CharonEditorModule.Instance.Logger;
		}

		[SuppressMessage("ReSharper", "InconsistentNaming"), UsedImplicitly]
		protected void OnGUI()
		{
			var migrator = CharonEditorModule.Instance.LegacyPluginMigrator;
			var someOperationPending = (this.migrationTask != null && !this.migrationTask.IsCompleted);
			var legacyPluginPresent = migrator.IsLegacyPluginExists();

			EditorLayoutUtils.BeginPaddings(this.position.size, Padding);

			EditorGUILayout.BeginVertical();
			{
				var boxStyle = new GUIStyle(GUI.skin.label) {
					wordWrap = true
				};

				if (legacyPluginPresent)
				{
					GUILayout.Box("An old version of the plugin was found [<2025.1.0]. Do you want to automatically migrate your data to the new version?", boxStyle);
				}
				else
				{
					GUILayout.Box("The old version of the plugin has been removed. You can Close this window now.", boxStyle);
				}
			}
			EditorGUILayout.EndVertical();

			GUILayout.Space(5);
			this.HorizontalLine(1);
			GUILayout.Space(5);

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.Label(this.progressStatus, EditorStyles.label, GUILayout.MaxWidth(Math.Max(this.position.width - 60, 60)));
				EditorGUILayout.Space();

				GUI.enabled = !someOperationPending && !CharonEditorModule.Instance.Routines.IsRunning && !EditorApplication.isCompiling && legacyPluginPresent;

				if (GUILayout.Button("Migrate", GUI.skin.button, GUILayout.Width(80), GUILayout.Height(25)))
				{
					this.migrationTask = migrator.MigrateAsync(this.GetProgressReport());
				}

				GUI.enabled = !GUI.enabled;

				if (GUILayout.Button(legacyPluginPresent ? "Cancel" : "Close", GUI.skin.button, GUILayout.Width(80), GUILayout.Height(25)))
				{
					this.RaiseCancel();
					this.Close();
				}

				GUILayout.Space(5);
			}
			EditorGUILayout.EndHorizontal();

			EditorLayoutUtils.EndPaddings();

			EditorLayoutUtils.AutoSize(this);
		}

		private void HorizontalLine(int height = 1)
		{
			var rect = EditorGUILayout.GetControlRect(false, height);

			rect.height = height;

			EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
		}

		private void RaiseDone()
		{
			if (this.Done != null)
				this.Done(this, EventArgs.Empty);
			this.Done = null;
			this.Cancel = null;

			if (!this.autoClose)
				return;

			this.Close();
		}
		private void RaiseCancel()
		{
			if (this.Cancel != null)
			{
				this.Cancel(this, new ErrorEventArgs(new InvalidOperationException(Resources.UI_UNITYPLUGIN_OPERATION_CANCELLED)));
			}
			this.Cancel = null;
			this.Done = null;
		}
		private void OnDestroy()
		{
			this.RaiseCancel();
		}

		private Action<string, float> GetProgressReport()
		{
			return (msg, progress) =>
			{
				this.progressStatus = $"{msg} ({progress * 100:F2}%)";
				this.Repaint();
			};
		}

		public static Task ShowAsync(bool autoClose = true)
		{
			var taskCompletionSource = new TaskCompletionSource<object>();
			var window = GetWindow<MigrationPromptWindow>(utility: true);

			window.Done += (_, _) => taskCompletionSource.TrySetResult(null);
			window.Cancel += (_, args) => taskCompletionSource.TrySetException(args.GetException());

			window.autoClose = autoClose;

			window.Focus();

			return taskCompletionSource.Task;
		}
	}
}
