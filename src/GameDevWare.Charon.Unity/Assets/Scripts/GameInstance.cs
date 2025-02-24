using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Assets.Scripts
{
	public sealed class GameInstance : MonoBehaviour
	{
		public RpgGameDataAsset gameDataAsset;

		public GameWorld World { get; private set; }
		public RpgGameData GameData { get; private set; }

		internal void Awake()
		{
			DontDestroyOnLoad(this);

			this.CreateGameWorldAsync().ContinueWith(t =>
				Debug.LogException(t.Exception),
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
				TaskScheduler.Current
			);
		}

		private async Task<RpgGameData> LoadGameDataFromStreamingAssetsAsync()
		{
			await Task.Yield();

			var gameDataLoadOptions = new Formatters.GameDataLoadOptions
			{
				Format = Formatters.GameDataFormat.Json,
				LeaveStreamsOpen = true
			};

			// locating game data file in 'StreamingAssets' folder on current platform
			var gameDataPath = Path.Combine(Application.streamingAssetsPath, "RpgGameData.gdjs");
#if UNITY_STANDALONE || UNITY_EDITOR_WIN
			gameDataPath = "file://" + gameDataPath;
#endif
			if (gameDataPath.Contains("://")) // Android & WebGL
			{
				using var request = UnityWebRequest.Get(gameDataPath);
				var operation = request.SendWebRequest();

				while (!operation.isDone)
				{
					await Task.Yield(); // Await until done
				}

				if (request.result == UnityWebRequest.Result.Success)
				{

					await using var gameDataStream = new NativeArrayStream(request.downloadHandler.nativeData);
					return new RpgGameData(gameDataStream, gameDataLoadOptions);
				}
				else
				{
					throw new IOException($"Failed to download game data file: {request.error}");
				}
			}
			else // Standalone, Editor, iOS
			{
				await using var gameDataStream = File.OpenRead(gameDataPath);
				return new RpgGameData(gameDataStream, gameDataLoadOptions);
			}
		}

		private async Task CreateGameWorldAsync()
		{
			await Task.Yield();

			if (this.gameDataAsset == null)
			{
				Debug.Log("Downloading game data from streaming assets...");

				this.GameData = await this.LoadGameDataFromStreamingAssetsAsync();
			}
			else
			{
				Debug.Log("Using game data from pre-defined asset.");

				this.GameData = this.gameDataAsset.GameData;
			}

			Debug.Log("Creating game world...");

			// creating game world with loaded game data
			this.World = new GameWorld(this.GameData);

			Debug.Log("Game is ready!");
		}

		public void OnGUI()
		{

			var width = 500.0f;
			var height = Screen.height - 40.0f;

			GUILayout.BeginArea(new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height));
			GUILayout.BeginVertical();

			if (this.World == null)
			{
				GUILayout.Label("Loading...");
			}
			else
			{
				GUILayout.Label("Location: " + this.World.Dungeon.Name);
				GUILayout.Label("");
				GUILayout.Label("Inventory:");
				foreach (var item in this.World.Inventory)
					GUILayout.Label(item.ToString());
				GUILayout.Label("");
				GUILayout.Label("Characters:");
				foreach (var character in this.World.Characters)
					GUILayout.Label(character.ToString());


			}

			GUILayout.EndVertical();
			GUILayout.EndArea();
		}
	}
}
