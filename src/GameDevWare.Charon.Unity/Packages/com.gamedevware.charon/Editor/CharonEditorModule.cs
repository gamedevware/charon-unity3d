/*
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
using Editor.Services;
using GameDevWare.Charon.Editor.Services;
using JetBrains.Annotations;
using UnityEditor;

namespace GameDevWare.Charon.Editor
{
	/// <summary>
	/// Module governing Charon editor activities. Here you can subscribe on events and change settings.
	/// </summary>
	[InitializeOnLoad, PublicAPI]
	public class CharonEditorModule: IDisposable
	{
		/// <summary>
		/// Singleton instance of <see cref="CharonEditorModule"/>.
		/// </summary>
		public static readonly CharonEditorModule Instance;

		static CharonEditorModule()
		{
			Instance = new CharonEditorModule();
		}

		internal readonly LogArchiver LogArchiver;
		internal readonly RoutineQueue Routines;
		internal readonly CharonProcessList Processes;
		internal readonly DeferredAssetImporter AssetImporter;
		internal readonly CharonLogger Logger;
		internal readonly UnityResourceServer ResourceServer;

		/// <summary>
		/// API key storage of current user.
		/// </summary>
		public readonly KeyCryptoStorage KeyCryptoStorage;
		/// <summary>
		/// Settings of this module.
		/// </summary>
		public readonly CharonSettings Settings;

		/// <summary>
		/// Point of extension of code generation process. Tasks added in this event are executed before source code generation.
		/// </summary>
		public event EventHandler<GameDataSourceCodeGenerationEventArgs> OnGameDataPreSourceCodeGeneration;
		/// <summary>
		/// Point of extension of code generation process. Tasks added in this event are executed after source code generation.
		/// </summary>
		public event EventHandler<GameDataSourceCodeGenerationEventArgs> OnGameDataPostSourceCodeGeneration;
		/// <summary>
		/// Point of extension of asset synchronization process. Tasks added in this event are executed before asset synchronization.
		/// </summary>
		public event EventHandler<GameDataSynchronizationEventArgs> OnGameDataPreSynchronization;
		/// <summary>
		/// Point of extension of asset synchronization process. Tasks added in this event are executed after asset synchronization.
		/// </summary>
		public event EventHandler<GameDataSynchronizationEventArgs> OnGameDataPostSynchronization;

		private CharonEditorModule()
		{
			EditorApplication.update += this.Initialize;

			this.Settings = new CharonSettings();
			this.Logger = new CharonLogger(this.Settings);
			this.LogArchiver = new LogArchiver(this.Logger);
			this.KeyCryptoStorage = new KeyCryptoStorage(this.Logger);
			this.Routines = new RoutineQueue();
			this.Processes = new CharonProcessList();
			this.ResourceServer = new UnityResourceServer(this.Logger);
			this.AssetImporter = new DeferredAssetImporter(this.Logger);
		}

		private void Initialize()
		{
			if (EditorApplication.isCompiling)
				return;

			EditorApplication.update -= this.Initialize;

			this.Settings.Initialize();
			this.KeyCryptoStorage.Initialize();
			this.AssetImporter.Initialize();
			this.ResourceServer.Initialize();
			this.LogArchiver.Initialize();

			AssemblyReloadEvents.beforeAssemblyReload += this.Dispose;
			AppDomain.CurrentDomain.DomainUnload += (_, _) => this.Dispose();
			EditorApplication.quitting += this.Dispose;
		}


		internal GameDataSourceCodeGenerationEventArgs RaiseOnGameDataPreSourceCodeGeneration(GameDataBase sender, string sourceCodeDirectory)
		{
			if (sender == null) throw new ArgumentNullException(nameof(sender));

			var args = new GameDataSourceCodeGenerationEventArgs(sourceCodeDirectory);
			this.OnGameDataPreSourceCodeGeneration?.Invoke(sender, args);
			return args;
		}
		internal GameDataSourceCodeGenerationEventArgs RaiseOnGameDataPostSourceCodeGeneration(GameDataBase sender, string sourceCodeDirectory)
		{
			if (sender == null) throw new ArgumentNullException(nameof(sender));

			var args = new GameDataSourceCodeGenerationEventArgs(sourceCodeDirectory);
			this.OnGameDataPostSourceCodeGeneration?.Invoke(sender, args);
			return args;
		}
		internal GameDataSynchronizationEventArgs RaiseOnGameDataPreSynchronization(GameDataBase sender, string publishedGameDataFilePath)
		{
			if (sender == null) throw new ArgumentNullException(nameof(sender));

			var args = new GameDataSynchronizationEventArgs(publishedGameDataFilePath);
			this.OnGameDataPreSynchronization?.Invoke(sender, args);
			return args;
		}
		internal GameDataSynchronizationEventArgs RaiseOnGameDataPostSynchronization(GameDataBase sender, string publishedGameDataFilePath)
		{
			if (sender == null) throw new ArgumentNullException(nameof(sender));

			var args = new GameDataSynchronizationEventArgs(publishedGameDataFilePath);
			this.OnGameDataPostSynchronization?.Invoke(sender, args);
			return args;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			this.ResourceServer?.Dispose();
			this.Routines?.Dispose();
			this.Processes?.Dispose();
		}
	}
}
