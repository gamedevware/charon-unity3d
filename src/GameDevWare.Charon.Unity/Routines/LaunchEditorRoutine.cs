using GameDevWare.Charon.Unity.Async;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using GameDevWare.Charon.Unity.ServerApi;
using UnityEditor;
using UnityEditor.Callbacks;
using GameDevWare.Charon.Unity.ServerApi.KeyStorage;

namespace GameDevWare.Charon.Unity.Routines
{
	internal class LaunchEditorRoutine
	{
		private static Promise loadEditorTask;
		private static int CurrentEditorPid;
		private static string CurrentEditorGameDataPath;

		// ReSharper disable once InconsistentNaming
		// ReSharper disable once UnusedMember.Local
		[OnOpenAsset(0)]
		private static bool OnOpenAsset(int instanceID, int exceptionId)
		{
			var gameDataPath = AssetDatabase.GetAssetPath(instanceID);
			if (GameDataTracker.IsGameDataFile(gameDataPath) == false)
				return false;

			if (loadEditorTask != null && !loadEditorTask.IsCompleted)
			{
				return false;
			}

			var reference = ValidationError.GetReference(exceptionId);
			var cancellation = new Promise();
			var progressCallback = ProgressUtils.ShowCancellableProgressBar(Resources.UI_UNITYPLUGIN_INSPECTOR_LAUNCHING_EDITOR_PREFIX + " ", cancellation: cancellation);
			loadEditorTask = new Coroutine<bool>(LoadEditor(gameDataPath, reference, loadEditorTask, progressCallback, cancellation));
			loadEditorTask.ContinueWith(t => EditorUtility.ClearProgressBar());

			return true;
		}

		private static IEnumerable LoadEditor(string gameDataPath, string reference, Promise waitTask, Action<string, float> progressCallback, Promise cancellation)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (progressCallback == null) throw new ArgumentNullException("progressCallback");

			// un-select gamedata file in Project window
			if (Selection.activeObject != null && AssetDatabase.GetAssetPath(Selection.activeObject) == gameDataPath)
				Selection.activeObject = null;

			if (waitTask != null)
				yield return waitTask.IgnoreFault();

			var gameDataSettings = GameDataSettings.Load(gameDataPath);
			if (gameDataSettings == null)
			{
				throw new InvalidOperationException(string.Format("Unable to start editor for '{0}'. File is not a game data file.", gameDataPath));
			}

			var checkRequirements = CharonCli.CheckRequirementsAsync();
			yield return checkRequirements;

			switch (checkRequirements.GetResult())
			{
				case RequirementsCheckResult.MissingRuntime: yield return UpdateRuntimeWindow.ShowAsync(); break;
				case RequirementsCheckResult.WrongVersion:
				case RequirementsCheckResult.MissingExecutable: yield return CharonCli.DownloadCharon(progressCallback.Sub(0.00f, 0.50f)); break;
				case RequirementsCheckResult.Ok: break;
				default: throw new InvalidOperationException(string.Format("Unexpected Charon check error: {0}.", checkRequirements.GetResult()));
			}

			if (gameDataSettings.IsConnected)
			{
				foreach (var step in RemoteAuthenticateAndOpenWindow(gameDataPath, gameDataSettings, reference, progressCallback.Sub(0.50f, 1.00f), cancellation))
				{
					yield return step;
				}
			}
			else
			{
				foreach (var step in LaunchCharonAndOpenWindow(gameDataPath, reference, progressCallback, cancellation))
				{
					yield return step;
				}
			}
		}
		private static IEnumerable RemoteAuthenticateAndOpenWindow(string gameDataPath, GameDataSettings gameDataSettings, string reference, Action<string, float> progressCallback, Promise cancellation)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (gameDataSettings == null) throw new ArgumentNullException("gameDataSettings");
			if (progressCallback == null) throw new ArgumentNullException("progressCallback");
			if (cancellation == null) throw new ArgumentNullException("cancellation");

			cancellation.ThrowIfCancellationRequested();

			var gameDataEditorUrl = new Uri(gameDataSettings.ServerAddress);

			if (reference == null || string.IsNullOrEmpty(reference))
			{
				reference = gameDataSettings.MakeDataSourceUrl().GetComponents(UriComponents.Path, UriFormat.Unescaped);
			}

			var serverApiClient = new ServerApiClient(gameDataEditorUrl);
			var apiKeyPath = new Uri(gameDataEditorUrl, "/" + gameDataSettings.ProjectId);
			var apiKey = KeyCryptoStorage.GetKey(apiKeyPath);
			var navigateUrl = new Uri(gameDataEditorUrl, reference);
			if (string.IsNullOrEmpty(apiKey))
			{
				serverApiClient.UseApiKey(apiKey);

				progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_AUTHENTICATING, 0.1f);

				var getLoginLinkTask = serverApiClient.GetLoginLink();
				yield return getLoginLinkTask.IgnoreFault();

				if (!getLoginLinkTask.HasErrors && getLoginLinkTask.GetResult() != null)
				{
					var loginParameters = string.Format("?loginLink={0}&returnUrl={1}", Uri.EscapeDataString(getLoginLinkTask.GetResult()), Uri.EscapeDataString(reference));
					navigateUrl = new Uri(gameDataEditorUrl, "view/sign-in" + loginParameters);
				}

				progressCallback(Resources.UI_UNITYPLUGIN_PROGRESS_AUTHENTICATING, 0.3f);
			}

			cancellation.ThrowIfCancellationRequested();

			progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_OPENING_BROWSER, 0.90f);

			NavigateTo(gameDataPath, gameDataEditorUrl, navigateUrl);
		}
		private static IEnumerable LaunchCharonAndOpenWindow(string gameDataPath, string reference, Action<string, float> progressCallback, Promise cancellation)
		{
			var port = Settings.Current.EditorPort;
			var gameDataEditorUrl = new Uri("http://localhost:" + port + "/");

			if (IsCurrentEditorServing(gameDataPath) == false)
			{
				foreach (var step in KillAndReLaunchLocalCharon(gameDataPath, gameDataEditorUrl, progressCallback, cancellation))
				{
					yield return step;
				}
			}

			cancellation.ThrowIfScriptsCompiling();
			cancellation.ThrowIfCancellationRequested();

			var waitForStart = new Async.Coroutine(WaitForStart(gameDataEditorUrl, progressCallback.Sub(0.50f, 1.00f), cancellation));
			yield return waitForStart;

			cancellation.ThrowIfScriptsCompiling();
			cancellation.ThrowIfCancellationRequested();

			var navigateUrl = new Uri(gameDataEditorUrl, reference);
			NavigateTo(gameDataPath, gameDataEditorUrl, navigateUrl);
		}

		private static bool IsCurrentEditorServing(string gameDataPath)
		{
			if (CurrentEditorGameDataPath != gameDataPath || CurrentEditorPid == 0)
			{
				return false;
			}

			try
			{
				var process = Process.GetProcessById(CurrentEditorPid);
				return !process.HasExited;
			}
			catch { return false; }
		}
		private static IEnumerable KillAndReLaunchLocalCharon(string gameDataPath, Uri gameDataEditorUrl, Action<string, float> progressCallback, Promise cancellation)
		{
			CharonCli.FindAndEndGracefully();

			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log("Starting game data editor at " + gameDataEditorUrl + "...");

			progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.10f);

			cancellation.ThrowIfScriptsCompiling();

			var charonRunTask = CharonCli.Listen(gameDataPath, CharonCli.GetDefaultLockFilePath(), gameDataEditorUrl.Port, shadowCopy: true,
				progressCallback: progressCallback.Sub(0.10f, 0.30f));

			if (Settings.Current.Verbose)
				UnityEngine.Debug.Log("Launching game data editor process.");

			// wait until server process start
			var timeoutPromise = Promise.Delayed(TimeSpan.FromSeconds(10));
			var startPromise = (Promise)charonRunTask.IgnoreFault();
			var startCompletePromise = Promise.WhenAny(timeoutPromise, startPromise);

			var timeBasedWait = new Coroutine<bool>(RunTimeBasedProgress(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, TimeSpan.FromSeconds(5),
				progressCallback.Sub(0.35f, 0.50f), cancellation, startCompletePromise));

			yield return Promise.WhenAny(timeoutPromise, startPromise, cancellation);
			yield return timeBasedWait;

			if (timeoutPromise.IsCompleted)
			{
				EditorUtility.ClearProgressBar();
				UnityEngine.Debug.LogWarning(Resources.UI_UNITYPLUGIN_WINDOW_FAILED_TO_START_EDITOR_TIMEOUT);
				yield break; // yield break;
			}
			else if (cancellation.IsCompleted)
			{
				cancellation.ThrowIfScriptsCompiling();
				cancellation.ThrowIfCancellationRequested();
			}
			else if (charonRunTask.HasErrors)
			{
				throw new InvalidOperationException("Failed to start editor.", charonRunTask.Error.Unwrap());
			}

			progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.50f);

			CurrentEditorPid = charonRunTask.GetResult().Id;
			CurrentEditorGameDataPath = gameDataPath;
		}

		private static IEnumerable WaitForStart(Uri gameDataEditorUrl, Action<string, float> progressCallback, Promise cancellation)
		{
			// wait until server start to respond
			var timeout = TimeSpan.FromSeconds(10);
			var downloadStream = new MemoryStream();
			var startTime = DateTime.UtcNow;
			var timeoutDateTime = startTime + timeout;
			var downloadIndexHtmlTask = HttpUtils.DownloadTo(downloadStream, gameDataEditorUrl, timeout: TimeSpan.FromSeconds(1));

			do
			{
				if (DateTime.UtcNow > timeoutDateTime)
				{
					throw new TimeoutException(Resources.UI_UNITYPLUGIN_WINDOW_FAILED_TO_START_EDITOR_TIMEOUT);
				}

				yield return downloadIndexHtmlTask.IgnoreFault();

				if (downloadIndexHtmlTask.HasErrors == false && downloadStream.Length > 0)
					break;

				var launchProgress = Math.Min(1.0f, Math.Max(0.0, (DateTime.UtcNow - startTime).TotalMilliseconds / timeout.TotalMilliseconds));
				progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, (float)launchProgress);

				cancellation.ThrowIfScriptsCompiling();
				cancellation.ThrowIfCancellationRequested();

				downloadStream.SetLength(0);
				downloadIndexHtmlTask = HttpUtils.DownloadTo(downloadStream, gameDataEditorUrl, timeout: TimeSpan.FromSeconds(1));
			} while (true);

			progressCallback(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_OPENING_BROWSER, 0.99f);
		}
		private static IEnumerable RunTimeBasedProgress(string message, TimeSpan expectedTime, Action<string, float> progressCallback, Promise cancellation, Promise completion)
		{
			var startTime = DateTime.UtcNow;
			while (cancellation.IsCompleted == false && completion.IsCompleted == false)
			{
				var timeElapsed = DateTime.UtcNow - startTime;
				var timeElapsedRatio = (float)Math.Min(1.0f, Math.Max(0.0f, timeElapsed.TotalMilliseconds / expectedTime.TotalMilliseconds));

				progressCallback(message, timeElapsedRatio);

				yield return Promise.Delayed(TimeSpan.FromMilliseconds(100));
			}
			yield return false;
		}

		private static void NavigateTo(string gameDataPath, Uri gameDataEditorUrl, Uri navigateUrl)
		{
			var browserType = (BrowserType)Settings.Current.Browser;
			if (browserType == BrowserType.UnityEmbedded && !GameDataEditorWindow.IsWebViewAvailable)
			{
				browserType = BrowserType.SystemDefault;
			}
			switch (browserType)
			{
				case BrowserType.Custom:
					if (string.IsNullOrEmpty(Settings.Current.BrowserPath))
						goto case BrowserType.SystemDefault;
					if (Settings.Current.Verbose)
					{
						UnityEngine.Debug.Log(string.Format("Opening custom browser '{1}' window for '{0}' address.", navigateUrl, Settings.Current.BrowserPath));
					}
					Process.Start(Settings.Current.BrowserPath, navigateUrl.OriginalString);
					break;
				case BrowserType.UnityEmbedded:
				case BrowserType.SystemDefault:
					if (Settings.Current.Verbose)
					{
						UnityEngine.Debug.Log(string.Format("Opening default browser window for '{0}' address.", navigateUrl));
					}
					EditorUtility.OpenWithDefaultApp(navigateUrl.OriginalString);
					break;
				default:
					throw new ArgumentOutOfRangeException(string.Format("Unexpected value '{0}' for '{1}' enum.", browserType, typeof(BrowserType)));
			}
		}

	}
}
