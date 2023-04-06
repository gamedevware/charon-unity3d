using System;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Utils
{
	internal class EditorLayoutUtils
	{
		public static void BeginPaddings(Vector2 size, Rect padding)
		{
			GUILayout.BeginHorizontal(GUILayout.Width(size.y - padding.x - padding.width));
			GUILayout.Space(padding.x);
			GUILayout.BeginVertical(GUILayout.Width(size.x - padding.y - padding.height));
			GUILayout.Space(padding.y);
		}
		public static void EndPaddings()
		{
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}
		public static void AutoSize(EditorWindow editorWindow)
		{
			if (ReferenceEquals(editorWindow, null)) throw new ArgumentNullException("editorWindow");
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (editorWindow == null) return;

			GUILayoutUtility.GetRect(1, 1, 1, 1);
			if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().y > 0)
			{
				var newRect = GUILayoutUtility.GetLastRect();
				editorWindow.position = new Rect(editorWindow.position.position, new Vector2(editorWindow.position.width, newRect.y + 7));
				editorWindow.minSize = new Vector2(editorWindow.minSize.x, editorWindow.position.height);
				editorWindow.maxSize = new Vector2(editorWindow.maxSize.x, editorWindow.position.height);
			}
		}
	}
}
