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
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Windows
{
	[CustomPropertyDrawer(typeof(GameDataDocumentReference))]
	public class GameDataDocumentReferenceDrawer : PropertyDrawer
	{
		private const int FIELD_HEIGHT = 20;
		[NonSerialized] private string[] schemaNames;
		[NonSerialized] private string[] documentIds;
		[NonSerialized] private GameDataBase lastGameData;
		[NonSerialized] private string lastSchemaName;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var idField = property.FindPropertyRelative(nameof(GameDataDocumentReference.id));
			var schemaNameOrIdField = property.FindPropertyRelative(nameof(GameDataDocumentReference.schemaNameOrId));
			var gameDataField = property.FindPropertyRelative(nameof(GameDataDocumentReference.gameData));

			var foldoutRect = new Rect(position.x, position.y, position.width - 10, FIELD_HEIGHT - 2);
			var gameDataRect = new Rect(position.x + 10, position.y + FIELD_HEIGHT, position.width - 10, FIELD_HEIGHT - 2);
			var schemaRect = new Rect(position.x + 10, position.y + FIELD_HEIGHT * 2, position.width - 10, FIELD_HEIGHT - 2);
			var idRect = new Rect(position.x + 10, position.y + FIELD_HEIGHT * 3, position.width - 10, FIELD_HEIGHT - 2);

			this.UpdateCachedLists(gameDataField, schemaNameOrIdField);

			// ReSharper disable once AssignmentInConditionalExpression
			if (property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, toggleOnLabelClick: true))
			{
				GUI.changed = false;

				EditorGUI.PropertyField(gameDataRect, gameDataField);

				if (GUI.changed)
				{
					this.UpdateCachedLists(gameDataField, schemaNameOrIdField);

					schemaNameOrIdField.stringValue = string.Empty;
				}

				// Schema
				GUI.enabled = this.schemaNames.Length > 0;
				schemaRect = EditorGUI.PrefixLabel(schemaRect, new GUIContent("Schema"));
				var selectedIndex = Array.IndexOf(this.schemaNames, schemaNameOrIdField.stringValue ?? string.Empty);
				var newSelectedIndex = selectedIndex;
				var isInvalid = !string.IsNullOrEmpty(schemaNameOrIdField.stringValue) && selectedIndex < 0;
				if (isInvalid)
				{
					schemaNameOrIdField.stringValue = EditorGUI.TextField(schemaRect, schemaNameOrIdField.stringValue);

					var highlightRect = new Rect(idRect.x - 2, idRect.y - 2, idRect.width + 4, idRect.height + 4);
					EditorGUI.DrawRect(highlightRect, new Color(1f, 0f, 0f, 0.2f)); // Semi-transparent red
				}
				else
				{
					newSelectedIndex = EditorGUI.Popup(schemaRect, selectedIndex, this.schemaNames);
					schemaNameOrIdField.stringValue = this.schemaNames.ElementAtOrDefault(newSelectedIndex);
				}
				GUI.enabled = true;

				if (GUI.changed)
				{
					idField.stringValue = string.Empty;
				}

				// Id
				GUI.enabled = this.documentIds.Length > 0;
				idRect = EditorGUI.PrefixLabel(idRect, new GUIContent("Id"));
				selectedIndex = Array.IndexOf(this.documentIds, idField.stringValue ?? string.Empty);
				isInvalid = !string.IsNullOrEmpty(idField.stringValue) && selectedIndex < 0;
				if (isInvalid)
				{
					idField.stringValue = EditorGUI.TextField(idRect, idField.stringValue);

					var highlightRect = new Rect(idRect.x - 2, idRect.y - 2, idRect.width + 4, idRect.height + 4);
					EditorGUI.DrawRect(highlightRect, new Color(1f, 0f, 0f, 0.2f)); // Semi-transparent red
				}
				else
				{
					newSelectedIndex = EditorGUI.Popup(idRect, selectedIndex, this.documentIds);
					idField.stringValue = this.documentIds.ElementAtOrDefault(newSelectedIndex);
				}
				GUI.enabled = true;
			}
			EditorGUI.EndProperty();
		}
		private void UpdateCachedLists(SerializedProperty gameDataField, SerializedProperty schemaNameOrIdField)
		{
			this.schemaNames ??= Array.Empty<string>();
			this.documentIds ??= Array.Empty<string>();

			if (this.lastGameData?.RevisionHash != (gameDataField.objectReferenceValue as GameDataBase)?.RevisionHash)
			{
				this.lastGameData = (GameDataBase)gameDataField.objectReferenceValue;
				this.schemaNames = this.lastGameData?.GetDocumentSchemaNames().ToArray() ?? Array.Empty<string>();
				this.lastSchemaName = null;
			}
			if (this.lastSchemaName != schemaNameOrIdField.stringValue)
			{
				this.lastSchemaName = schemaNameOrIdField.stringValue;
				if (this.schemaNames.Contains(this.lastSchemaName))
				{
					this.documentIds = this.lastGameData?.GetDocumentIds(this.lastSchemaName).ToArray() ?? Array.Empty<string>();
				}
				else
				{
					this.documentIds ??= Array.Empty<string>();
				}
			}
		}

		/// <inheritdoc />
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (property.isExpanded)
			{
				return FIELD_HEIGHT * 4;
			}
			else
			{
				return FIELD_HEIGHT;
			}
		}
	}
}
