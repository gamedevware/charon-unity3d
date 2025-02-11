﻿/*
	Copyright (c) 2025 Denis Zykov

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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Cli;
using GameDevWare.Charon.Editor.ServerApi;
using GameDevWare.Charon.Editor.Utils;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityObject = UnityEngine.Object;
using Random = System.Random;

namespace GameDevWare.Charon.Editor.Routines
{
	[PublicAPI]
	internal class LaunchEditorRoutine
	{
		private static Task LoadEditorTask;
		private static CancellationTokenSource LoadEditorTaskCancellationSource;

		[OnOpenAsset(0)]
		private static bool OnOpenAsset(int instanceID, int exceptionId)
		{
			var gameDataAssetPath = AssetDatabase.GetAssetPath(instanceID);

			var asset = AssetDatabase.LoadAssetAtPath<UnityObject>(gameDataAssetPath);
			var gameDataAsset = GameDataAssetUtils.GetAssociatedGameDataAsset(asset);
			if (gameDataAsset == null)
			{
				return false;
			}

			var reference = ValidationError.GetReference(exceptionId);
			LoadEditorTaskCancellationSource?.Cancel();
			var cancellationSource = LoadEditorTaskCancellationSource = new CancellationTokenSource();
			var progressCallback = ProgressUtils.ShowCancellableProgressBar(Resources.UI_UNITYPLUGIN_INSPECTOR_LAUNCHING_EDITOR_PREFIX + " ",
				cancellationSource: cancellationSource);
			progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_CHECKING_TOOLS_VERSION, 0.0f);

			LoadEditorTask = LoadEditorAsync(gameDataAsset, gameDataAssetPath, reference, LoadEditorTask, progressCallback, cancellationSource.Token)
				.ContinueWith(_ => EditorUtility.ClearProgressBar(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
			LoadEditorTask.LogFaultAsError();

			return true;
		}

		private static async Task LoadEditorAsync
		(
			GameDataBase gameDataBase,
			string gameDataAssetPath,
			string reference,
			Task waitTask,
			Action<string, float> progressCallback,
			CancellationToken cancellation)
		{
			if (gameDataBase == null) throw new ArgumentNullException(nameof(gameDataBase));
			if (progressCallback == null) throw new ArgumentNullException(nameof(progressCallback));

			if (waitTask != null)
			{
				await waitTask.IgnoreFault().ConfigureAwait(true);
			}

			cancellation.ThrowIfCancellationRequested();

			var gameDataSettings = gameDataBase.settings;
			if (gameDataSettings == null)
			{
				throw new InvalidOperationException($"Unable to start editor for '{gameDataAssetPath}'. File is not a game data file.");
			}

			if (gameDataSettings.IsConnected)
			{
				await RemoteAuthenticateAndOpenWindowAsync(gameDataSettings, reference, progressCallback.Sub(0.50f, 1.00f), cancellation).ConfigureAwait(true);
			}
			else if (!await TryJoinExistingEditorAsync(gameDataSettings, reference, progressCallback, cancellation).ConfigureAwait(true))
			{
				await LaunchCharonAndOpenWindowAsync(gameDataSettings, reference, progressCallback, cancellation).ConfigureAwait(true);
			}
		}


		private static async Task RemoteAuthenticateAndOpenWindowAsync(GameDataSettings gameDataSettings, string reference, Action<string, float> progressCallback, CancellationToken cancellation)
		{
			if (gameDataSettings == null) throw new ArgumentNullException(nameof(gameDataSettings));
			if (progressCallback == null) throw new ArgumentNullException(nameof(progressCallback));

			cancellation.ThrowIfCancellationRequested();

			var gameDataEditorUrl = new Uri(gameDataSettings.serverAddress);

			if (reference == null || string.IsNullOrEmpty(reference))
			{
				reference = gameDataSettings.MakeDataSourceUrl().GetComponents(UriComponents.Path, UriFormat.Unescaped);
			}

			var serverApiClient = new ServerApiClient(gameDataEditorUrl);
			var apiKeyPath = new Uri(gameDataEditorUrl, "/" + gameDataSettings.projectId);
			var apiKey = CharonEditorModule.Instance.KeyCryptoStorage.GetKey(apiKeyPath);
			var navigateUrl = new Uri(gameDataEditorUrl, reference);

			if (!string.IsNullOrEmpty(apiKey))
			{
				serverApiClient.UseApiKey(apiKey);

				progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_AUTHENTICATING, 0.1f);

				var loginCode = await serverApiClient.GetLoginCodeAsync(apiKey).IgnoreFault().ConfigureAwait(true);

				if (string.IsNullOrEmpty(loginCode) == false)
				{
					var loginParameters = $"?loginCode={Uri.EscapeDataString(loginCode)}&returnUrl={Uri.EscapeDataString(reference)}";
					navigateUrl = new Uri(gameDataEditorUrl, "view/sign-in" + loginParameters);
				}

				progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_AUTHENTICATING, 0.3f);
			}

			cancellation.ThrowIfCancellationRequested();

			progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_OPENING_BROWSER, 0.90f);

			NavigateTo(navigateUrl);
		}
		private static async Task LaunchCharonAndOpenWindowAsync(GameDataSettings gameDataSettings, string reference, Action<string, float> progressCallback, CancellationToken cancellation)
		{
			var randomPort = new Random().Next(10000, 65000);
			var gameDataEditorUrl = new Uri("http://localhost:" + randomPort + "/");

			var gameDataPath = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid) ?? "");
			await LaunchLocalCharon(gameDataPath, gameDataEditorUrl, progressCallback, cancellation).ConfigureAwait(true);

			cancellation.ThrowIfScriptsCompiling();
			cancellation.ThrowIfCancellationRequested();

			await WaitForStart(gameDataEditorUrl, progressCallback.Sub(0.50f, 1.00f), cancellation).ConfigureAwait(true);

			cancellation.ThrowIfScriptsCompiling();
			cancellation.ThrowIfCancellationRequested();

			var navigateUrl = new Uri(gameDataEditorUrl, reference);
			NavigateTo(navigateUrl);
		}

		private static async Task<bool> TryJoinExistingEditorAsync(GameDataSettings gameDataSettings, string reference, Action<string,float> progressCallback, CancellationToken cancellation)
		{
			var gameDataPath = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(gameDataSettings.gameDataFileGuid) ?? "");
			var lockFilePath = Path.Combine(CharonFileUtils.LibraryCharonPath, CharonServerProcess.GetLockFileNameFor(gameDataPath));
			if (!File.Exists(lockFilePath))
			{
				return false;
			}

			// try delete orphained lock
			try { File.Delete(lockFilePath); }
			catch { /* ignore delete attempt errors and continue */ }

			var gameDataEditorUrl = default(Uri);
			try
			{
				using var lockFileStream = new FileStream(lockFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				using var lockFileReader = new StreamReader(lockFileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, 1024, leaveOpen: true);

				var pidStr = await lockFileReader.ReadLineAsync();
				var listenAddressString = await lockFileReader.ReadLineAsync();

				if (!int.TryParse(pidStr, out var pid) ||
					!Uri.TryCreate(listenAddressString, UriKind.Absolute, out gameDataEditorUrl))
				{
					return false;
				}

				using var downloadStream = new MemoryStream();
				var downloadTask = HttpUtils.DownloadToAsync(downloadStream, gameDataEditorUrl, timeout: TimeSpan.FromSeconds(5), cancellation: cancellation);
				await downloadTask.IgnoreFault().ConfigureAwait(true);
				if (downloadTask.IsFaulted || downloadStream.Length == 0)
				{
					return false;
				}
			}
			catch
			{
				return false;
			}

			var navigateUrl = new Uri(gameDataEditorUrl, reference);
			NavigateTo(navigateUrl);
			return true;
		}

		private static async Task LaunchLocalCharon(string gameDataPath, Uri gameDataEditorUrl, Action<string, float> progressCallback, CancellationToken cancellation)
		{
			var logger = CharonEditorModule.Instance.Logger;

			logger.Log(LogType.Assert, "Starting game data editor at " + gameDataEditorUrl + "...");

			progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.10f);

			cancellation.ThrowIfScriptsCompiling();

			var charonRunTask = CharonCli.StartServerAsync(gameDataPath, gameDataEditorUrl.Port, progressCallback: progressCallback.Sub(0.10f, 0.30f));

			logger.Log(LogType.Assert, "Launching game data editor process.");

			// wait until server process start
			var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), cancellation);
			var startTask = charonRunTask.IgnoreFault();
			var startOrTimeoutTask = Task.WhenAny(timeoutTask, startTask);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			RunTimedProgressAsync(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, expectedTime: TimeSpan.FromSeconds(5),
				progressCallback.Sub(0.35f, 0.50f), cancellation, startOrTimeoutTask);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

			await Task.WhenAny(timeoutTask, startTask).ConfigureAwait(true);

			if (timeoutTask.IsCompleted)
			{
				EditorUtility.ClearProgressBar();
				logger.Log(LogType.Warning, Resources.UI_UNITYPLUGIN_WINDOW_FAILED_TO_START_EDITOR_TIMEOUT);
				return;
			}
			else if (cancellation.IsCancellationRequested)
			{
				cancellation.ThrowIfScriptsCompiling();
				cancellation.ThrowIfCancellationRequested();
			}

			progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.50f);

			var editorProcess = await charonRunTask.ConfigureAwait(true);
			CharonEditorModule.Instance.Processes.Add(editorProcess);
		}
		private static async Task WaitForStart(Uri gameDataEditorUrl, Action<string, float> progressCallback, CancellationToken cancellation)
		{
			// wait until server start to respond
			var timeout = CharonEditorModule.Instance.Settings.IdleCloseTimeout;
			var downloadStream = new MemoryStream();
			var startTime = DateTime.UtcNow;
			var timeoutDateTime = startTime + timeout;
			var downloadIndexHtmlTask = HttpUtils.DownloadToAsync(downloadStream, gameDataEditorUrl, timeout: TimeSpan.FromSeconds(1), cancellation: cancellation);

			do
			{
				if (DateTime.UtcNow > timeoutDateTime)
				{
					throw new TimeoutException(Resources.UI_UNITYPLUGIN_WINDOW_FAILED_TO_START_EDITOR_TIMEOUT);
				}

				await downloadIndexHtmlTask.IgnoreFault().ConfigureAwait(true);

				if (!downloadIndexHtmlTask.IsFaulted && downloadStream.Length > 0)
					break;

				var launchProgress = Math.Min(1.0f, Math.Max(0.0, (DateTime.UtcNow - startTime).TotalMilliseconds / timeout.TotalMilliseconds));
				progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, (float)launchProgress);

				cancellation.ThrowIfScriptsCompiling();
				cancellation.ThrowIfCancellationRequested();

				downloadStream.SetLength(0);
				downloadIndexHtmlTask = HttpUtils.DownloadToAsync(downloadStream, gameDataEditorUrl, timeout: TimeSpan.FromSeconds(1), cancellation: cancellation);
			} while (true);

			progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_OPENING_BROWSER, 0.99f);
		}
		private static async Task RunTimedProgressAsync
			(string message, TimeSpan expectedTime, Action<string, float> progressCallback, CancellationToken cancellation, Task completion)
		{
			var startTime = DateTime.UtcNow;
			while (!cancellation.IsCancellationRequested && !completion.IsCompleted)
			{
				var timeElapsed = DateTime.UtcNow - startTime;
				var timeElapsedRatio = (float)Math.Min(1.0f, Math.Max(0.0f, timeElapsed.TotalMilliseconds / expectedTime.TotalMilliseconds));

				progressCallback(message, timeElapsedRatio);

				await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None).ConfigureAwait(true);
			}
		}

		private static void NavigateTo(Uri navigateUrl)
		{
			var logger = CharonEditorModule.Instance.Logger;
			var settings = CharonEditorModule.Instance.Settings;
			switch (settings.EditorApplication)
			{
				case CharonEditorApplication.CustomBrowser:
					if (string.IsNullOrEmpty(settings.CustomEditorApplicationPath))
						goto case CharonEditorApplication.DefaultBrowser;

					logger.Log(LogType.Assert, string.Format("Opening custom browser '{1}' window for '{0}' address.", navigateUrl, settings.CustomEditorApplicationPath));

					Process.Start(settings.CustomEditorApplicationPath, navigateUrl.OriginalString);
					break;
				case CharonEditorApplication.DefaultBrowser:
					logger.Log(LogType.Assert, $"Opening default browser window for '{navigateUrl}' address.");

					EditorUtility.OpenWithDefaultApp(navigateUrl.OriginalString);
					break;
				default:
					throw new ArgumentOutOfRangeException($"Unexpected value '{settings.EditorApplication}' for '{typeof(CharonEditorApplication)}' enum.");
			}
		}
	}
}
