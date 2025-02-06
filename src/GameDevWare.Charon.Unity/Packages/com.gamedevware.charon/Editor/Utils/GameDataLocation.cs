using System;
using System.IO;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Utils
{
	/// <summary>
	/// Game data location and optional API key for remote locations.
	/// </summary>
	[PublicAPI]
	public struct GameDataLocation
	{
		/// <summary>
		/// Game data location on file system, or remote location in internet.
		/// </summary>
		[NotNull] public readonly Uri Location;
		/// <summary>
		/// Authentication credentials for remote game data.
		/// </summary>
		[CanBeNull] public readonly string ApiKey;

		/// <summary>
		/// Returns true if location has API key attached.
		/// </summary>
		public bool HasApiKey { get { return string.IsNullOrEmpty(this.ApiKey) == false; } }

		/// <summary>
		/// Returns true if location if file path.
		/// </summary>
		public bool IsFile { get { return this.Location != null && string.Equals(this.Location.Scheme, "file", StringComparison.OrdinalIgnoreCase); } }
		public bool IsFileExists { get { return this.Location != null && this.IsFile && File.Exists(this.Location.LocalPath); } }

		/// <summary>
		/// Constructor for location URL and optional credentials.
		/// </summary>
		public GameDataLocation([NotNull] string gameDataPath)

		{

			if (gameDataPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
				gameDataPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				this.Location = new Uri(gameDataPath, UriKind.Absolute);
				this.ApiKey = default(string);
			}
			else
			{
				this.Location = new Uri(Path.GetFullPath(gameDataPath), UriKind.Absolute);
				this.ApiKey = default(string);
			}
		}

		/// <summary>
		/// Constructor for location URL and optional credentials.
		/// </summary>
		public GameDataLocation([NotNull] Uri location, [CanBeNull] string apiKey)
		{
			if (location == null) throw new ArgumentNullException("location");

			this.Location = location;
			this.ApiKey = apiKey;
		}

		/// <summary>
		/// Convert string path to <see cref="GameDataLocation"/>
		/// </summary>
		public static implicit operator GameDataLocation(string gameDataPath)
		{
			if (gameDataPath == null) return default(GameDataLocation);

			return new GameDataLocation(gameDataPath);
		}

		/// <summary>
		/// Throws if location is file path and <see cref="File.Exists"/> is false.
		/// </summary>
		public void ThrowIfFileNotExists()
		{
			if (this.IsFile && !this.IsFileExists)
			{
				throw new IOException(string.Format("GameData file '{0}' doesn't exists.", this.Location.LocalPath));
			}
		}
		/// <summary>
		/// Throws if location is remote
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		public void ThrowIfRemote()
		{
			if (!this.IsFile)
			{
				throw new InvalidOperationException("Location of game data should be local file system.");
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			if (this.Location == null)
			{
				return "<null>";
			}
			return this.Location.ToString();
		}
	}
}
