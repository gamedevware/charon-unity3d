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
using GameDevWare.Charon.Unity.Utils;
using GameDevWare.Charon.Unity.Windows;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Unity
{
	[InitializeOnLoad, UsedImplicitly]
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
