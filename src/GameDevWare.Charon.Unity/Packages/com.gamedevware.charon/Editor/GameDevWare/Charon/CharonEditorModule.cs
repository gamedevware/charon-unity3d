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
using Editor.GameDevWare.Charon.Services;
using GameDevWare.Charon;
using JetBrains.Annotations;
using UnityEditor;

namespace Editor.GameDevWare.Charon
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
