using System.Collections;
using System.IO;
using UnityEngine;

namespace Assets.Scripts
{
	public sealed class GameInstance : MonoBehaviour
	{
		private Coroutine loadCoroutine;

		[HideInInspector]
		public RpgGameData GameData { get; private set; }
		[HideInInspector]
		public GameWorld World { get; private set; }

		internal void Awake()
		{
			this.loadCoroutine = this.StartCoroutine(this.Load());
		}

		public IEnumerator Load()
		{
			Debug.Log("Staring game...");

			// locating game data file in 'StreamingAssets' folder on current platform
			var pathToGameData = Path.Combine(Application.streamingAssetsPath, "RpgGameData.gdjs");
#if UNITY_STANDALONE || UNITY_EDITOR_WIN
			pathToGameData = "file://" + pathToGameData;
#endif

			Debug.Log("Loading game data...");

			// CASE #1: load from data stream (file, network ...)
			using (var loader = new WWW(pathToGameData))
			{
				// loading GameData into memory with WWW class
				yield return loader;

				// creating Stream instance for RpgGameData's constructor, 
				// passing 'writable: false' to prevent extra copying of data in memory
				var gameDataStream = new MemoryStream(loader.bytes, writable: false);

				// reading game data from stream, format is set to JSON
				this.GameData = new RpgGameData(gameDataStream, new Formatters.GameDataLoadOptions { Format = Formatters.Format.Json });
			}

			// CASE #2: load from asset at /Assets/Resources
			//this.GameData = Resources.Load<RpgGameDataAsset>("RpgGameData").GameData;
			

			Debug.Log("Creating game world...");

			// creating game world with loaded game data
			this.World = new GameWorld(this.GameData);
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
