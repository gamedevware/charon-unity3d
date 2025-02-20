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
using JetBrains.Annotations;
using UnityObject = UnityEngine.Object;

namespace GameDevWare.Charon
{
	[PublicAPI, Serializable]
	public class GameDataSettings
	{
		public string codeGenerationPath;
		public string gameDataFileGuid;
		public string gameDataClassName;
		public string gameDataNamespace;
		public string gameDataDocumentClassName;
		public string defineConstants;
		public string[] publishLanguages;
		public int publishFormat;
		public int optimizations;
		public int lineEnding;
		public int indentation;
		public bool splitSourceCodeFiles;
		public bool clearOutputDirectory;
		public string serverAddress;
		public string projectId;
		public string projectName;
		public string branchName;
		public string branchId;

		public bool IsConnected =>
			string.IsNullOrEmpty(this.serverAddress) == false &&
			string.IsNullOrEmpty(this.projectId) == false &&
			string.IsNullOrEmpty(this.branchId) == false;


		public Uri MakeDataSourceUrl()
		{
			if (!this.IsConnected)
			{
				throw new InvalidOperationException("Data source URL could be created only for connected game data.");
			}

			return new Uri(new Uri(this.serverAddress), $"view/data/{this.projectId}/{this.branchId}/");
		}
	}
}
