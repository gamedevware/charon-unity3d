using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity.Windows
{
	internal class UpdateAvailableWindow : EditorWindow
	{
		private readonly Rect padding;
		private Vector2 scrollPosition;

		public string ReleaseNotes;

		public UpdateAvailableWindow()
		{
			this.titleContent = new GUIContent(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_TITLE);
			this.minSize = new Vector2(500, 400);
			this.position = new Rect(
				(Screen.width - this.minSize.x) / 2,
				(Screen.height - this.minSize.y) / 2,
				this.minSize.x,
				this.minSize.y
			);
			this.padding = new Rect(10, 10, 10, 10);
		}

		[SuppressMessage("ReSharper", "InconsistentNaming")]
		protected void OnGUI()
		{
			// paddings
			GUILayout.BeginHorizontal(GUILayout.Width(this.position.width - this.padding.x - this.padding.width));
			GUILayout.Space(this.padding.x);
			GUILayout.BeginVertical(GUILayout.Height(this.position.height - this.padding.y - this.padding.height));
			GUILayout.Space(this.padding.y);

			this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition, false, true);
			var style = new GUIStyle { richText = true };
			GUILayout.Label(this.ReleaseNotes ?? string.Empty, style);
			GUILayout.EndScrollView();

			
			GUILayout.Space(5);
			GUILayout.BeginHorizontal();
			EditorGUILayout.Space();
			if (GUILayout.Button(Resources.UI_UNITYPLUGIN_WINDOW_UPDATE_UPDATE_BUTTON, GUILayout.Width(80)))
			{
				EditorWindow.GetWindow<UpdateWindow>(utility: true);
				this.Close();
			}
			GUILayout.EndHorizontal();

			// paddings
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}
	}
}
