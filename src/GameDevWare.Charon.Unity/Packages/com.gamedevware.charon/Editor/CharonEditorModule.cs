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

using GameDevWare.Charon.Editor.Services;
using JetBrains.Annotations;
using UnityEditor;

namespace GameDevWare.Charon.Editor
{
	[InitializeOnLoad, UsedImplicitly]
	public class CharonEditorModule
	{
		public static readonly CharonEditorModule Instance;

		static CharonEditorModule()
		{
			Instance = new CharonEditorModule();
		}

		internal readonly RoutineQueue Routines;
		internal readonly CharonProcessList Processes;
		internal readonly DeferredAssetImporter AssetImporter;
		internal readonly CharonLogger Logger;
		public readonly KeyCryptoStorage KeyCryptoStorage;
		public readonly CharonSettings Settings;

		private CharonEditorModule()
		{
			EditorApplication.update += this.Initialize;

			this.Settings = new CharonSettings();
			this.Logger = new CharonLogger(this.Settings);
			this.KeyCryptoStorage = new KeyCryptoStorage(this.Logger);
			this.Routines = new RoutineQueue();
			this.Processes = new CharonProcessList();
			this.AssetImporter = new DeferredAssetImporter(this.Logger);
		}

		private void Initialize()
		{
			if (EditorApplication.isCompiling)
				return;

			EditorApplication.update -= this.Initialize;

			this.AssetImporter.Initialize();
		}
	}
}
