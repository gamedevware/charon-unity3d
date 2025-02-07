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

using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GameDevWare.Charon
{
	[ScriptedImporter(1, "gdjs")]
	public class GameDataJsonImporter : ScriptedImporter
	{
		/// <inheritdoc />
		public override void OnImportAsset(AssetImportContext ctx) => ImportAsset(ctx);

		public static void ImportAsset(AssetImportContext ctx)
		{
			var gameDataPath = ctx.assetPath ?? "";
			if (string.IsNullOrEmpty(gameDataPath) || !File.Exists(gameDataPath))
			{
				return;
			}

			var text = File.ReadAllText(gameDataPath);
			var textAsset = new TextAsset(text);

			// Add the TextAsset to the import context
			ctx.AddObjectToAsset("main", textAsset);
			ctx.SetMainObject(textAsset);
		}
	}
}
