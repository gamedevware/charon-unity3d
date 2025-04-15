using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using GameDevWare.Charon.Editor.Services.ResourceServerApi;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace GameDevWare.Charon.Editor.Services
{
	internal class AssetIndexer
	{
		private readonly ConditionalWeakTable<Texture2D, byte[]> thumbnailByTexture = new ConditionalWeakTable<Texture2D, byte[]>();
		private readonly ConcurrentDictionary<string, byte[]> thumbnails = new ConcurrentDictionary<string, byte[]>();

		public ListAssetsResponse ListAssets(int skip, int take, string query, string[] types)
		{
			if (types.Length > 0)
			{
				query = string.Join(", ", types.Select(type => "t:" + type)) + " " + query;
			}

			var foundAssets = new List<GameAsset>();
			var allAssets = AssetDatabase.FindAssets(query);
			foreach (var assetGuid in allAssets)
			{
				while (skip > 0)
				{
					skip--;
					continue;
				}

				var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
				var unityObject = AssetDatabase.LoadAssetAtPath<UnityObject>(assetPath);
				if (unityObject == null)
				{
					continue;
				}

				foundAssets.Add(new GameAsset {
					Name = unityObject.name,
					HasThumbnail = AssetPreview.GetMiniThumbnail(unityObject) != null,
					Path =  assetPath,
					Type = unityObject.GetType().Name
				});

				if (foundAssets.Count >= take)
				{
					break;
				}
			}

			return new ListAssetsResponse { Assets = foundAssets.ToArray(), Total = allAssets.Length };
		}
		public byte[] GetAssetThumbnail(string path, int? size)
		{
			if (this.thumbnails.TryGetValue(path, out var thumbnailBytes))
			{
				return thumbnailBytes;
			}
			var unityObject = AssetDatabase.LoadAssetAtPath<UnityObject>(path);
			if (unityObject == null)
			{
				return null;
			}

			var thumbnail = AssetPreview.GetAssetPreview(unityObject);
			if (thumbnail == null)
			{
				thumbnail = AssetPreview.GetMiniThumbnail(unityObject);
			}

			if (thumbnail == null)
			{
				return null;
			}

			if (this.thumbnailByTexture.TryGetValue(thumbnail, out thumbnailBytes))
			{
				return thumbnailBytes;
			}

			thumbnailBytes = SaveTextureToPngInMemory(thumbnail, size);
			this.thumbnails[path] = thumbnailBytes;
			this.thumbnailByTexture.AddOrUpdate(thumbnail, thumbnailBytes);
			return thumbnailBytes;
		}

		private static byte[] SaveTextureToPngInMemory(Texture2D texture, int? size)
		{
			if (texture == null) return null;

			var width = size ?? texture.width;
			var height = size ?? texture.height;
			var rt = RenderTexture.GetTemporary(
				width,
				height,
				0,
				RenderTextureFormat.ARGB32,
				RenderTextureReadWrite.sRGB
			);

			Graphics.Blit(texture, rt);

			var previous = RenderTexture.active;
			RenderTexture.active = rt;

			var readableTexture = new Texture2D(
				width,
				height,
				TextureFormat.ARGB32,
				false,
				true // linear
			);

			readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			readableTexture.Apply();

			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(rt);

			var pngBytes = readableTexture.EncodeToPNG();
			UnityObject.DestroyImmediate(readableTexture);
			return pngBytes;
		}
	}
}
