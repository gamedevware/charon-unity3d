using System;
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity
{
	[InitializeOnLoad]
	internal static class PluginInitializer
	{
		private static readonly EditorApplication.CallbackFunction InitializeCallback = Initialize;

		static PluginInitializer()
		{
			EditorApplication.update += InitializeCallback;
		}

		private static void Initialize()
		{			
			EditorApplication.update -= InitializeCallback;

			try
			{
				System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(GameDataInspector).TypeHandle);
				System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Settings).TypeHandle);
				System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Menu).TypeHandle);

				CharonCli.CleanUpLogsDirectory();
			}
			catch (Exception initializationError)
			{
				Debug.LogError("Failed to initialize Charon plugin. Look for exception below.");
				Debug.LogError(initializationError);
			}
		}
	}
}
