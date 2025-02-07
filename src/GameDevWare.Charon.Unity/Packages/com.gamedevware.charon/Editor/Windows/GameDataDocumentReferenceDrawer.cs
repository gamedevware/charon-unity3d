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
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor
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
				this.documentIds = this.lastGameData?.GetDocumentIds(this.lastSchemaName).ToArray() ?? Array.Empty<string>();
			}

			// ReSharper disable once AssignmentInConditionalExpression
			if (property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, toggleOnLabelClick: true))
			{
				idRect = EditorGUI.PrefixLabel(idRect, new GUIContent("Id"));
				var selectedIndex = Array.IndexOf(this.documentIds, idField.stringValue ?? string.Empty);
				selectedIndex = EditorGUI.Popup(idRect, selectedIndex, this.documentIds);
				idField.stringValue = this.documentIds.ElementAtOrDefault(selectedIndex);

				schemaRect = EditorGUI.PrefixLabel(schemaRect, new GUIContent("Schema"));
				selectedIndex = Array.IndexOf(this.schemaNames, schemaNameOrIdField.stringValue ?? string.Empty);
				selectedIndex = EditorGUI.Popup(schemaRect, selectedIndex, this.schemaNames);
				schemaNameOrIdField.stringValue = this.schemaNames.ElementAtOrDefault(selectedIndex);

				EditorGUI.PropertyField(gameDataRect, gameDataField);
			}
			EditorGUI.EndProperty();
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
