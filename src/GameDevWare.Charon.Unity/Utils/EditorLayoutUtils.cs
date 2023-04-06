/*
	Copyright (c) 2023 Denis Zykov

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
