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
