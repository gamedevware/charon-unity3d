﻿/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

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
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Utils
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
			if (ReferenceEquals(editorWindow, null)) throw new ArgumentNullException(nameof(editorWindow));
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
