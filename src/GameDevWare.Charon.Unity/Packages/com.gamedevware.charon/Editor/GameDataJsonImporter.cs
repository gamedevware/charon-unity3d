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

using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace GameDevWare.Charon.Editor
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
