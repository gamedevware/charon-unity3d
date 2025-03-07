/*
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Services.ServerApi;
using GameDevWare.Charon.Editor.Utils;
using JetBrains.Annotations;
using UnityEngine;

#if JSON_NET_3_0_2_OR_NEWER
using Newtonsoft.Json;
using JsonObject = Newtonsoft.Json.Linq.JObject;
using JsonValue = Newtonsoft.Json.Linq.JToken;
#else
using GameDevWare.Charon.Editor.Json;
#endif

// ReSharper disable UseAwaitUsing

namespace GameDevWare.Charon.Editor.Cli
{
	/// <summary>
	/// Provides a convenient interface for running Charon.exe command line operations.
	/// This class encapsulates functionality for creating, updating, deleting, importing, exporting, and finding documents
	/// within a specified GameData URL, either file-based or server-based. It simplifies interactions
	/// with the Charon command line tool, offering methods that return tasks representing the operations.
	/// For more detailed documentation of each method, refer to the Charon command line documentation at
	/// https://gamedevware.github.io/charon/advanced/command_line.html
	/// </summary>
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public static class CharonCli
	{
		private static readonly string[] EmptyParameters = Array.Empty<string>();
		private static bool ScriptFilesAreCopied;

		public const string FORMAT_JSON = "json";
		public const string FORMAT_BSON = "bson";
		public const string FORMAT_MESSAGE_PACK = "msgpack";
		public const string FORMAT_XLSX = "xlsx";
		public const string FORMAT_XLIFF2 = "xliff2";
		public const string FORMAT_XLIFF1 = "xliff1";

		private const string SOURCE_STANDARD_INPUT = "in";
		private const string TARGET_STANDARD_OUTPUT = "out";
		private const string TARGET_STANDARD_ERROR = "err";
		private const string TARGET_NULL = "null";

		internal static async Task<CharonServerProcess> StartServerAsync
		(
			string gameDataPath,
			int port,
			string lockFilePath = null,
			CharonLogLevel? logsVerbosity = null,
			Action<string, float> progressCallback = null
		)
		{
			if (string.IsNullOrEmpty(gameDataPath)) throw new ArgumentException("Value cannot be null or empty.", nameof(gameDataPath));
			if (port <= 0 || port > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(port));

			logsVerbosity ??= CharonEditorModule.Instance.Settings.LogLevel;
			gameDataPath = Path.GetFullPath(gameDataPath);
			if (File.Exists(gameDataPath) == false) throw new IOException($"File '{gameDataPath}' doesn't exists.");

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 0.30f);

			var listenAddress = new Uri("http://localhost:" + port);
			lockFilePath ??= Path.Combine(CharonFileUtils.LibraryCharonPath, CharonServerProcess.GetLockFileNameFor(gameDataPath));

			var settings = CharonEditorModule.Instance.Settings;

			var charonPath = EnsureCharonRunScript("RunCharon");
			var idleTimeout = settings.IdleCloseTimeout;
			var unityPid = Process.GetCurrentProcess().Id;
			var runResult = await CommandLineUtils.RunAsync(
				new ToolRunOptions
				(
					charonPath,
					ToolRunOptions.FlattenArguments(
						"SERVER", "START",
						"--dataBase", Path.GetFullPath(gameDataPath),
						"--port", port.ToString(),
						"--watchPid", unityPid.ToString(),
						"--lockFile", Path.GetFullPath(lockFilePath),
						"--maxIdleTime", idleTimeout.ToString(), // auto-close idle editor
						"--log", logsVerbosity == CharonLogLevel.None ? TARGET_NULL : TARGET_STANDARD_OUTPUT,
						logsVerbosity == CharonLogLevel.Verbose ? "--verbose" : null
					)
				) {
					CaptureStandardError = false,
					CaptureStandardOutput = false,
					ExecutionTimeout = TimeSpan.Zero,
					WaitForExit = false,
					StartInfo = {
						EnvironmentVariables = {
							{ "DOTNET_CONTENTROOT", CharonFileUtils.CharonAppContentPath },

							//
							{ "CHARON_API_SERVER", settings.GetServerAddressUrl().OriginalString },
							{ "CHARON_API_KEY", "" },

							{ "SERILOG__WRITETO__0__NAME", "File" }, {
								"SERILOG__WRITETO__0__ARGS__PATH", Path.GetFullPath(Path.Combine(CharonFileUtils.LibraryCharonLogsPath,
									$"{DateTime.UtcNow:yyyy_MM_dd_hh}.charon.unity.log"))
							},
						}
					}
				}
			);

			progressCallback?.Invoke(Resources.UI_UNITYPLUGIN_WINDOW_EDITOR_LAUNCHING_EXECUTABLE, 1.0f);

			return new CharonServerProcess(runResult, gameDataPath, listenAddress, lockFilePath);
		}

		/// <summary>
		/// Init the specified GameData file.
		/// https://gamedevware.github.io/charon/advanced/commands/init.html
		/// </summary>
		/// <param name="gameDataPath">The path of the GameData file.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The created document.</returns>
		public static async Task InitGameDataAsync
		(
			string gameDataPath,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataPath == null) throw new ArgumentNullException(nameof(gameDataPath));

			using var _ = await RunCharonAsync
			(
				string.Empty, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"INIT", gameDataPath
				),
				configureTool
			);
		}

		/// <summary>
		/// Creates a document in the specified GameData URL.
		/// https://gamedevware.github.io/charon/advanced/commands/data_create.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNameOrId">The schema name or ID of the document.</param>
		/// <param name="document">The document to create as a shared reference to a JsonObject.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The created document.</returns>
		public static async Task<JsonObject> CreateDocumentAsync
		(
			string gameDataUrl,
			string apiKey,
			string schemaNameOrId,
			JsonObject document,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNameOrId == null) throw new ArgumentNullException(nameof(schemaNameOrId));
			if (document == null) throw new ArgumentNullException(nameof(document));

			var inputFileName = WriteJsonInput(document);
			var outputFileName = CreateTemporaryFile("json");

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "CREATE", gameDataUrl,
					"--schema", schemaNameOrId,
					"--input", inputFileName,
					"--inputFormat", "json",
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Updates a document in the specified GameData URL.
		/// https://gamedevware.github.io/charon/advanced/commands/data_update.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNameOrId">The schema name or ID of the document.</param>
		/// <param name="document">The document to update as a shared reference to a JsonObject.</param>
		/// <param name="id">Optional ID of the document to update if not present in the Document object.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The updated document.</returns>
		public static async Task<JsonObject> UpdateDocumentAsync
		(
			string gameDataUrl,
			string apiKey,
			string schemaNameOrId,
			JsonObject document,
			string id = null,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNameOrId == null) throw new ArgumentNullException(nameof(schemaNameOrId));
			if (document == null) throw new ArgumentNullException(nameof(document));

			var inputFileName = WriteJsonInput(document);
			var outputFileName = CreateTemporaryFile("json");

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "UPDATE", gameDataUrl,
					"--schema", schemaNameOrId,
					string.IsNullOrEmpty(id) ? EmptyParameters : new[] { "--id", id },
					"--input", inputFileName,
					"--inputFormat", "json",
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Deletes a document in the specified GameData URL.
		/// https://gamedevware.github.io/charon/advanced/commands/data_delete.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNameOrId">The schema name or ID of the document.</param>
		/// <param name="document">The document to delete, only the ID is used for deletion.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The deleted document or null in case of failure.</returns>
		public static async Task<JsonObject> DeleteDocumentAsync
		(
			string gameDataUrl,
			string apiKey,
			string schemaNameOrId,
			JsonObject document,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNameOrId == null) throw new ArgumentNullException(nameof(schemaNameOrId));
			if (document == null) throw new ArgumentNullException(nameof(document));

			var outputFileName = CreateTemporaryFile("json");

			var id = document["Id"]?.ToString();
			if (string.IsNullOrEmpty(id))
			{
				throw new ArgumentException("Document missing Id property.", nameof(document));
			}

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "DELETE", gameDataUrl,
					"--schema", schemaNameOrId,
					"--id", id,
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Deletes a document in the specified GameData URL by ID.
		/// https://gamedevware.github.io/charon/advanced/commands/data_delete.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNameOrId">The schema name or ID of the document.</param>
		/// <param name="id">The ID of the document to delete.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The deleted document or null in case of failure.</returns>
		public static async Task<JsonObject> DeleteDocumentAsync
		(
			string gameDataUrl,
			string apiKey,
			string schemaNameOrId,
			string id,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNameOrId == null) throw new ArgumentNullException(nameof(schemaNameOrId));
			if (id == null) throw new ArgumentNullException(nameof(id));

			var outputFileName = CreateTemporaryFile("json");

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "DELETE", gameDataUrl,
					"--schema", schemaNameOrId,
					"--id", id,
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Finds a document in the specified GameData URL by ID.
		/// https://gamedevware.github.io/charon/advanced/commands/data_find.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNameOrId">The schema name or ID of the document.</param>
		/// <param name="id">The ID of the document to find.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The found document or null in case of failure.</returns>
		public static async Task<JsonObject> FindDocumentAsync
		(
			string gameDataUrl,
			string apiKey,
			string schemaNameOrId,
			string id,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNameOrId == null) throw new ArgumentNullException(nameof(schemaNameOrId));
			if (id == null) throw new ArgumentNullException(nameof(id));

			var outputFileName = CreateTemporaryFile("json");

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "FIND", gameDataUrl,
					"--schema", schemaNameOrId,
					"--id", id,
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Lists documents in the specified GameData URL.
		/// https://gamedevware.github.io/charon/advanced/commands/data_list.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNameOrId">The schema name or ID of the document.</param>
		/// <param name="filters">Filters for documents to list.</param>
		/// <param name="sorters">Sorters for documents to list.</param>
		/// <param name="path">Limit search only to embedded documents with specified path.</param>
		/// <param name="skip">Number of documents to skip before writing to output.</param>
		/// <param name="take">Number of documents to take after 'skip' for output.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The found documents.</returns>
		public static async Task<JsonObject> ListDocumentsAsync
		(
			string gameDataUrl,
			string apiKey,
			string schemaNameOrId,
			IReadOnlyList<ListFilter> filters = null,
			IReadOnlyList<ListSorter> sorters = null,
			string path = null,
			uint? skip = null,
			uint? take = null,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNameOrId == null) throw new ArgumentNullException(nameof(schemaNameOrId));

			var outputFileName = CreateTemporaryFile("json");

			var filtersList = new List<string>();
			if (filters != null && filters.Count > 0)
			{
				filtersList.Add("--filters");
				foreach (var listFilter in filters)
				{
					filtersList.Add(listFilter.PropertyName);
					filtersList.Add(listFilter.GetOperationName());
					filtersList.Add(listFilter.GetValueQuoted());
				}
			}

			var sortersList = new List<string>();
			if (sorters != null && sorters.Count > 0)
			{
				sortersList.Add("--sorters");
				foreach (var listSorter in sorters)
				{
					sortersList.Add(listSorter.PropertyName);
					sortersList.Add(listSorter.GetDirectionName());
				}
			}

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "LIST", gameDataUrl,
					"--schema", schemaNameOrId,
					filtersList,
					sortersList,
					string.IsNullOrEmpty(path) ? EmptyParameters : new[] { "--path", path },
					skip.GetValueOrDefault() == 0 ? EmptyParameters : new[] { "--skip", skip.ToString() },
					take == null ? EmptyParameters : new[] { "--take", take.ToString() },
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Imports documents grouped by schema into a specified GameDataUrl file or server.
		/// https://gamedevware.github.io/charon/advanced/commands/data_import.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNamesOrIds">Names or IDs of schemas to import from DocumentsBySchemaNameOrId. Can be empty or '*' to import all documents.</param>
		/// <param name="documentsBySchemaNameOrId">The documents to be imported, grouped by schema name or ID.</param>
		/// <param name="importMode">The mode of import operation, see ImportMode for details.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task ImportAsync
		(
			string gameDataUrl,
			string apiKey,
			IReadOnlyList<string> schemaNamesOrIds,
			JsonObject documentsBySchemaNameOrId,
			ImportMode importMode,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNamesOrIds == null) throw new ArgumentNullException(nameof(schemaNamesOrIds));
			if (documentsBySchemaNameOrId == null) throw new ArgumentNullException(nameof(documentsBySchemaNameOrId));

			var inputFileName = WriteJsonInput(documentsBySchemaNameOrId);

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "IMPORT", gameDataUrl,
					"--schemas", schemaNamesOrIds,
					"--mode", (int)importMode,
					"--input", inputFileName,
					"--inputFormat", "json"
				),
				configureTool
			);
		}

		/// <summary>
		/// Imports documents from a file into a specified GameDataUrl file or server.
		/// https://gamedevware.github.io/charon/advanced/commands/data_import.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNamesOrIds">Names or IDs of schemas to import. Can be empty or '*' to import all documents.</param>
		/// <param name="importMode">The mode of import operation, see ImportMode for details.</param>
		/// <param name="documentsBySchemaNameOrIdFilePath">File path to the documents to import.</param>
		/// <param name="format">The format of the imported documents ('json', 'bson', 'msgpack', 'xlsx').</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task ImportFromFileAsync
		(
			string gameDataUrl,
			string apiKey,
			IReadOnlyList<string> schemaNamesOrIds,
			ImportMode importMode,
			string documentsBySchemaNameOrIdFilePath,
			ExportFormat format = ExportFormat.Json,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNamesOrIds == null) throw new ArgumentNullException(nameof(schemaNamesOrIds));
			if (documentsBySchemaNameOrIdFilePath == null) throw new ArgumentNullException(nameof(documentsBySchemaNameOrIdFilePath));

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "IMPORT", gameDataUrl,
					"--schemas", schemaNamesOrIds,
					"--mode", (int)importMode,
					"--input", documentsBySchemaNameOrIdFilePath,
					"--inputFormat", format.GetFormatName()
				),
				configureTool
			);
		}

		/// <summary>
		/// Exports documents from a GameDataUrl file or server.
		/// https://gamedevware.github.io/charon/advanced/commands/data_export.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNamesOrIds">Names or IDs of schemas to export. Can be empty or '*' to export all documents.</param>
		/// <param name="properties">Names, IDs, types of properties in schemas to include in the export. Can be empty or '*' to export all properties.</param>
		/// <param name="languages">Language tags (BCP 47) to include in the export of localized text. Can be empty or '*' to export all languages.</param>
		/// <param name="exportMode">The mode of export operation, see ExportMode for details.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The exported documents.</returns>
		public static async Task<JsonObject> ExportAsync
		(
			string gameDataUrl,
			string apiKey,
			IReadOnlyList<string> schemaNamesOrIds,
			IReadOnlyList<string> properties,
			IReadOnlyList<string> languages,
			ExportMode exportMode,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNamesOrIds == null) throw new ArgumentNullException(nameof(schemaNamesOrIds));
			if (properties == null) throw new ArgumentNullException(nameof(properties));
			if (languages == null) throw new ArgumentNullException(nameof(languages));

			var outputFileName = CreateTemporaryFile("json");

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "EXPORT", gameDataUrl,
					schemaNamesOrIds.Count == 0 ? EmptyParameters : "--schemas", schemaNamesOrIds,
					properties.Count == 0 ? EmptyParameters : "--properties", properties,
					languages.Count == 0 ? EmptyParameters : "--languages", languages,
					"--mode", (int)exportMode,
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Exports documents from a GameDataUrl file or server to a file.
		/// https://gamedevware.github.io/charon/advanced/commands/data_export.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNamesOrIds">Names or IDs of schemas to export. Can be empty or '*' to export all documents.</param>
		/// <param name="properties">Names, IDs, types of properties in schemas to include in the export. Can be empty or '*' to export all properties.</param>
		/// <param name="languages">Language tags (BCP 47) to include in the export of localized text. Can be empty or '*' to export all languages.</param>
		/// <param name="exportMode">The mode of export operation, see ExportMode for details.</param>
		/// <param name="exportedDocumentsFilePath">File path where the exported documents will be saved.</param>
		/// <param name="format">The format in which to save the exported data ('json', 'bson', 'msgpack', 'xlsx').</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task ExportToFileAsync
		(
			string gameDataUrl,
			string apiKey,
			IReadOnlyList<string> schemaNamesOrIds,
			IReadOnlyList<string> properties,
			IReadOnlyList<string> languages,
			ExportMode exportMode,
			string exportedDocumentsFilePath,
			ExportFormat format = ExportFormat.Json,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNamesOrIds == null) throw new ArgumentNullException(nameof(schemaNamesOrIds));
			if (properties == null) throw new ArgumentNullException(nameof(properties));
			if (languages == null) throw new ArgumentNullException(nameof(languages));
			if (exportedDocumentsFilePath == null) throw new ArgumentNullException(nameof(exportedDocumentsFilePath));

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "EXPORT", gameDataUrl,
					schemaNamesOrIds.Count == 0 ? EmptyParameters : "--schemas", schemaNamesOrIds,
					properties.Count == 0 ? EmptyParameters : "--properties", properties,
					languages.Count == 0 ? EmptyParameters : "--languages", languages,
					"--mode", (int)exportMode,
					"--output", exportedDocumentsFilePath,
					"--outputFormat", format.GetFormatName()
				),
				configureTool
			);
		}

		/// <summary>
		/// Imports translated documents grouped by schema into a specified GameDataUrl file or server.
		/// https://gamedevware.github.io/charon/advanced/commands/data_i18n_import.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNamesOrIds">Names or IDs of schemas to import from DocumentsBySchemaNameOrId. Can be empty or '*' to import all documents.</param>
		/// <param name="languages">Language tags (BCP 47) to import into localized text. Can be empty or '*' to import all languages.</param>
		/// <param name="documentsBySchemaNameOrId">The documents to be imported, grouped by schema name or ID.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task I18NImportAsync
		(
			string gameDataUrl,
			string apiKey,
			IReadOnlyList<string> schemaNamesOrIds,
			IReadOnlyList<string> languages,
			JsonObject documentsBySchemaNameOrId,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNamesOrIds == null) throw new ArgumentNullException(nameof(schemaNamesOrIds));
			if (languages == null) throw new ArgumentNullException(nameof(languages));
			if (documentsBySchemaNameOrId == null) throw new ArgumentNullException(nameof(documentsBySchemaNameOrId));

			var inputFileName = WriteJsonInput(documentsBySchemaNameOrId);

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "I18N", "IMPORT", gameDataUrl,
					"--schemas", schemaNamesOrIds,
					languages.Count == 0 ? EmptyParameters : "--languages", languages,
					"--input", inputFileName,
					"--inputFormat", "json"
				),
				configureTool
			);
		}

		/// <summary>
		/// Imports documents from a file into a specified GameDataUrl file or server.
		/// https://gamedevware.github.io/charon/advanced/commands/data_i18n_import.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNamesOrIds">Names or IDs of schemas to import. Can be empty or '*' to import all documents.</param>
		/// <param name="languages">Language tags (BCP 47) to import into localized text. Can be empty or '*' to import all languages.</param>
		/// <param name="documentsBySchemaNameOrIdFilePath">File path to the documents to import.</param>
		/// <param name="format">The format of the imported documents ('xliff', 'xliff2', 'xliff1', 'xlsx', 'json').</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task I18NImportFromFileAsync
		(
			string gameDataUrl,
			string apiKey,
			IReadOnlyList<string> schemaNamesOrIds,
			IReadOnlyList<string> languages,
			string documentsBySchemaNameOrIdFilePath,
			ExportFormat format = ExportFormat.Json,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNamesOrIds == null) throw new ArgumentNullException(nameof(schemaNamesOrIds));
			if (languages == null) throw new ArgumentNullException(nameof(languages));
			if (documentsBySchemaNameOrIdFilePath == null) throw new ArgumentNullException(nameof(documentsBySchemaNameOrIdFilePath));

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "I18N", "IMPORT", gameDataUrl,
					"--schemas", schemaNamesOrIds,
					languages.Count == 0 ? EmptyParameters : "--languages", languages,
					"--input", documentsBySchemaNameOrIdFilePath,
					"--inputFormat", format.GetFormatName()
				),
				configureTool
			);
		}

		/// <summary>
		/// Exports documents from a GameDataUrl file or server.
		/// https://gamedevware.github.io/charon/advanced/commands/data_i18n_export.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNamesOrIds">Names or IDs of schemas to export. Can be empty or '*' to export all documents.</param>
		/// <param name="sourceLanguage">Language tag (BCP 47) to include in the export as source of translation.</param>
		/// <param name="targetLanguage">Language tag (BCP 47) to include in the export as target of translation.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The exported documents.</returns>
		public static async Task<JsonObject> I18NExportAsync
		(
			string gameDataUrl,
			string apiKey,
			IReadOnlyList<string> schemaNamesOrIds,
			string sourceLanguage,
			string targetLanguage,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNamesOrIds == null) throw new ArgumentNullException(nameof(schemaNamesOrIds));
			if (sourceLanguage == null) throw new ArgumentNullException(nameof(sourceLanguage));
			if (targetLanguage == null) throw new ArgumentNullException(nameof(targetLanguage));

			var outputFileName = CreateTemporaryFile("json");
			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "I18N", "EXPORT", gameDataUrl,
					schemaNamesOrIds.Count == 0 ? EmptyParameters : "--schemas", schemaNamesOrIds,
					"--sourceLanguage", sourceLanguage,
					"--targetLanguage", targetLanguage,
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Exports documents from a GameDataUrl file or server to a file.
		/// https://gamedevware.github.io/charon/advanced/commands/data_i18n_export.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="schemaNamesOrIds">Names or IDs of schemas to export. Can be empty or '*' to export all documents.</param>
		/// <param name="sourceLanguage">Language tag (BCP 47) to include in the export as source of translation.</param>
		/// <param name="targetLanguage">Language tag (BCP 47) to include in the export as target of translation.</param>
		/// <param name="exportedDocumentsFilePath">File path where the exported documents will be saved.</param>
		/// <param name="format">The format in which to save the exported data ('xliff', 'xliff2', 'xliff1', 'xlsx', 'json').</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task I18NExportToFileAsync
		(
			string gameDataUrl,
			string apiKey,
			IReadOnlyList<string> schemaNamesOrIds,
			string sourceLanguage,
			string targetLanguage,
			string exportedDocumentsFilePath,
			ExportFormat format = ExportFormat.Json,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (schemaNamesOrIds == null) throw new ArgumentNullException(nameof(schemaNamesOrIds));
			if (sourceLanguage == null) throw new ArgumentNullException(nameof(sourceLanguage));
			if (targetLanguage == null) throw new ArgumentNullException(nameof(targetLanguage));
			if (exportedDocumentsFilePath == null) throw new ArgumentNullException(nameof(exportedDocumentsFilePath));

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "I18N", "EXPORT", gameDataUrl,
					schemaNamesOrIds.Count == 0 ? EmptyParameters : "--schemas", schemaNamesOrIds,
					"--sourceLanguage", sourceLanguage,
					"--targetLanguage", targetLanguage,
					"--output", exportedDocumentsFilePath,
					"--outputFormat", format.GetFormatName()
				),
				configureTool
			);
		}

		/// <summary>
		/// Add translation language to a GameDataUrl file or server.
		/// https://gamedevware.github.io/charon/advanced/commands/data_i18n_add_language.html
		/// </summary>
		/// <param name="gameDataUrl">The URL of the GameData file or server.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="languages">Language tags (BCP 47) to add in project's translation language list.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task I18NAddLanguageAsync
		(
			string gameDataUrl,
			string apiKey,
			string[] languages,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (languages == null) throw new ArgumentNullException(nameof(languages));

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "I18N", "ADDLANGUAGE", gameDataUrl,
					"--languages", languages
				),
				configureTool
			);
		}

		/// <summary>
		/// Compares all documents in two GameData URLs and creates a patch representing the difference.
		/// https://gamedevware.github.io/charon/advanced/commands/data_create_patch.html
		/// </summary>
		/// <param name="gameDataUrl1">The first GameData URL for comparison.</param>
		/// <param name="gameDataUrl2">The second GameData URL for comparison.</param>
		/// <param name="apiKey">Authentication credentials if the GameData URLs are servers, otherwise empty.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The patch data.</returns>
		public static async Task<JsonObject> CreatePatchAsync
		(
			string gameDataUrl1,
			string gameDataUrl2,
			string apiKey,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl1 == null) throw new ArgumentNullException(nameof(gameDataUrl1));
			if (gameDataUrl2 == null) throw new ArgumentNullException(nameof(gameDataUrl2));

			var outputFileName = CreateTemporaryFile("json");

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				arguments: ToolRunOptions.FlattenArguments(
					"DATA", "CREATEPATCH", gameDataUrl1, gameDataUrl2,
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Compares all documents in two GameData URLs and creates a patch representing the difference.
		/// https://gamedevware.github.io/charon/advanced/commands/data_create_patch.html
		/// </summary>
		/// <param name="gameDataUrl1">The first GameData URL for comparison.</param>
		/// <param name="gameDataUrl2">The second GameData URL for comparison.</param>
		/// <param name="apiKey">Authentication credentials if the GameData URLs are servers, otherwise empty.</param>
		/// <param name="exportedDocumentsFilePath">File path where the path will be saved.</param>
		/// <param name="format">The format in which to save the patch ('json', 'msgpack').</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The patch data.</returns>
		public static async Task CreatePatchToFileAsync
		(
			string gameDataUrl1,
			string gameDataUrl2,
			string apiKey,
			string exportedDocumentsFilePath,
			BackupFormat format = BackupFormat.Json,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl1 == null) throw new ArgumentNullException(nameof(gameDataUrl1));
			if (gameDataUrl2 == null) throw new ArgumentNullException(nameof(gameDataUrl2));
			if (exportedDocumentsFilePath == null) throw new ArgumentNullException(nameof(exportedDocumentsFilePath));

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				arguments: ToolRunOptions.FlattenArguments(
					"DATA", "CREATEPATCH", gameDataUrl1, gameDataUrl2,
					"--output", exportedDocumentsFilePath,
					"--outputFormat", format.GetFormatName()
				),
				configureTool
			);
		}

		/// <summary>
		/// Applies a patch created by CreatePatch to a specified GameData URL.
		/// https://gamedevware.github.io/charon/advanced/commands/data_apply_patch.html
		/// </summary>
		/// <param name="gameDataUrl">The GameData URL to apply the patch to.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="gameDataPatch">The patch document created by CreatePatch.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task ApplyPatchAsync
		(
			string gameDataUrl,
			string apiKey,
			JsonObject gameDataPatch,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (gameDataPatch == null) throw new ArgumentNullException(nameof(gameDataPatch));

			var inputFileName = WriteJsonInput(gameDataPatch);

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "APPLYPATCH", gameDataUrl,
					"--input", inputFileName,
					"--inputFormat", "json"
				),
				configureTool
			);
		}

		/// <summary>
		/// Applies a patch created by CreatePatch to a specified GameData URL.
		/// https://gamedevware.github.io/charon/advanced/commands/data_apply_patch.html
		/// </summary>
		/// <param name="gameDataUrl">The GameData URL to apply the patch to.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="gameDataPatchFilePath">The patch file created by CreatePatch.</param>
		/// <param name="format">The format of the imported patch ('json', 'msgpack' or 'auto').</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task ApplyPatchFromFileAsync
		(
			string gameDataUrl,
			string apiKey,
			string gameDataPatchFilePath,
			BackupFormat format = BackupFormat.Json,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (gameDataPatchFilePath == null) throw new ArgumentNullException(nameof(gameDataPatchFilePath));

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "APPLYPATCH", gameDataUrl,
					"--input", gameDataPatchFilePath,
					"--inputFormat", format.GetFormatName()
				),
				configureTool
			);
		}

		/// <summary>
		/// Backups game data with all documents and their metadata.
		/// https://gamedevware.github.io/charon/advanced/commands/data_backup.html
		/// </summary>
		/// <param name="gameDataUrl">The GameData URL to backup.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The backup data.</returns>
		public static async Task<JsonObject> BackupAsync
		(
			string gameDataUrl,
			string apiKey,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));

			var outputFileName = CreateTemporaryFile("json");

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "Backup", gameDataUrl,
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return (JsonObject)ReadOutputJson(outputFileName);
		}

		/// <summary>
		/// Backups game data to a file with all documents and their metadata.
		/// https://gamedevware.github.io/charon/advanced/commands/data_backup.html
		/// </summary>
		/// <param name="gameDataUrl">The GameData URL to backup.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="gameDataFilePath">File path where the backup will be saved.</param>
		/// <param name="format">The format for saving data ('json', 'msgpack').</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task BackupToFileAsync
		(
			string gameDataUrl,
			string apiKey,
			string gameDataFilePath,
			BackupFormat format = BackupFormat.Json,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (gameDataFilePath == null) throw new ArgumentNullException(nameof(gameDataFilePath));

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "Backup", gameDataUrl,
					"--output", gameDataFilePath,
					"--outputFormat", format.GetFormatName()
				),
				configureTool
			);
		}

		/// <summary>
		/// Restores game data with all documents and their metadata.
		/// https://gamedevware.github.io/charon/advanced/commands/data_restore.html
		/// </summary>
		/// <param name="gameDataUrl">The GameData URL to restore.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="gameData">Previously backed up data.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task RestoreAsync
		(
			string gameDataUrl,
			string apiKey,
			JsonObject gameData,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (gameData == null) throw new ArgumentNullException(nameof(gameData));

			var inputFileName = WriteJsonInput(gameData);

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "RESTORE", gameDataUrl,
					"--input", inputFileName,
					"--inputFormat", "json"
				),
				configureTool
			);
		}

		/// <summary>
		/// Restores game data from a file with all documents and their metadata.
		/// https://gamedevware.github.io/charon/advanced/commands/data_restore.html
		/// </summary>
		/// <param name="gameDataUrl">The GameData URL to restore.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="gameDataFilePath">File path with previously backed up data.</param>
		/// <param name="format">The format for the backed up data ('json', 'msgpack').</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task RestoreFromFileAsync
		(
			string gameDataUrl,
			string apiKey,
			string gameDataFilePath,
			BackupFormat format = BackupFormat.Json,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (gameDataFilePath == null) throw new ArgumentNullException(nameof(gameDataFilePath));

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "RESTORE", gameDataUrl,
					"--input", gameDataFilePath,
					"--inputFormat", format.GetFormatName()
				),
				configureTool
			);
		}

		/// <summary>
		/// Checks all documents in the specified GameData URL and returns a report with any issues.
		/// https://gamedevware.github.io/charon/advanced/commands/data_validate.html
		/// </summary>
		/// <param name="gameDataUrl">The GameData URL to validate.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="validationOptions">A list of checks to perform during validation, see ValidationOption for details.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The report of issues.</returns>
		public static async Task<ValidationReport> ValidateAsync
		(
			string gameDataUrl,
			string apiKey,
			ValidationOptions validationOptions,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));

			var outputFileName = CreateTemporaryFile("json");

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments(
					"DATA", "VALIDATE", gameDataUrl,
					"--validationOptions", ((int)validationOptions).ToString(),
					"--output", outputFileName,
					"--outputFormat", "json"
				),
				configureTool
			);

			return ReadOutputJson(outputFileName).ToObject<ValidationReport>();
		}

		/// <summary>
		/// Generates C# source code for loading game data from a GameDataUrl into a game's runtime.
		/// https://gamedevware.github.io/charon/advanced/commands/generate_csharp_code.html
		/// </summary>
		/// <param name="gameDataUrl">The GameData URL from which to generate source code.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="outputDirectory">Directory to place generated files, preferably empty.</param>
		/// <param name="documentClassName">Name for the base class for all documents.</param>
		/// <param name="gameDataClassName">Name for the main class from which all documents are accessible.</param>
		/// <param name="gameDataNamespace">Namespace for generated code.</param>
		/// <param name="defineConstants">Additional defines for all generated files.</param>
		/// <param name="sourceCodeGenerationOptimizations">List of enabled optimizations in the generated code, see SourceCodeGenerationOptimizations for details.</param>
		/// <param name="sourceCodeIndentation">Indentation style for the generated code.</param>
		/// <param name="sourceCodeLineEndings">Line endings for the generated code.</param>
		/// <param name="clearOutputDirectory">Whether to clear the output directory from generated code before generating files.</param>
		/// <param name="splitFiles">Split code into multiple files instead of keeping one huge file.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task GenerateCSharpCodeAsync
		(
			string gameDataUrl,
			string apiKey,
			string outputDirectory,
			string documentClassName = "Document",
			string gameDataClassName = "GameData",
			string gameDataNamespace = "GameParameters",
			string defineConstants = null,
			SourceCodeGenerationOptimizations sourceCodeGenerationOptimizations = default,
			SourceCodeIndentation sourceCodeIndentation = SourceCodeIndentation.Tabs,
			SourceCodeLineEndings sourceCodeLineEndings = SourceCodeLineEndings.OsDefault,
			bool clearOutputDirectory = true,
			bool splitFiles = false,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));
			if (outputDirectory == null) throw new ArgumentNullException(nameof(outputDirectory));
			if (documentClassName == null) throw new ArgumentNullException(nameof(documentClassName));
			if (gameDataClassName == null) throw new ArgumentNullException(nameof(gameDataClassName));
			if (gameDataNamespace == null) throw new ArgumentNullException(nameof(gameDataNamespace));

			sourceCodeGenerationOptimizations |= SourceCodeGenerationOptimizations.DisableFormulaCompilation;

			if (sourceCodeLineEndings == SourceCodeLineEndings.OsDefault)
			{
				sourceCodeLineEndings = SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows ?
					SourceCodeLineEndings.Windows : SourceCodeLineEndings.Unix;
			}

			var optimizationsList = new List<string>();
			foreach (SourceCodeGenerationOptimizations optimization in Enum.GetValues(typeof(SourceCodeGenerationOptimizations)))
			{
				if ((sourceCodeGenerationOptimizations & optimization) != 0)
				{
					optimizationsList.Add(optimization.ToString());
				}
			}
			if (optimizationsList.Count > 0)
			{
				optimizationsList.Insert(0, "--optimizations");
			}

			using var _ = await RunCharonAsync
			(
				apiKey, logsVerbosity,
				ToolRunOptions.FlattenArguments
				(
					"GENERATE", "CSHARPCODE", gameDataUrl,
					"--outputDirectory", outputDirectory,
					"--documentClassName", documentClassName,
					"--gameDataClassName", gameDataClassName,
					"--namespace", gameDataNamespace,
					string.IsNullOrEmpty(defineConstants) ? EmptyParameters : new[] { "--defineConstants", defineConstants },
					clearOutputDirectory ? "--clearOutputDirectory" : null,
					"--indentation", sourceCodeIndentation.ToString(),
					"--lineEndings", sourceCodeLineEndings.ToString(),
					optimizationsList,
					splitFiles ? "--splitFiles" : null
				),
				configureTool
			);
		}

		/// <summary>
		/// Dumps T4 code generation templates used to generate source code into a specified directory.
		/// https://gamedevware.github.io/charon/advanced/commands/generate_templates.html
		/// </summary>
		/// <param name="outputDirectory">The directory where the templates will be dumped.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		public static async Task DumpTemplatesAsync(string outputDirectory, Action<ToolRunOptions> configureTool = null)
		{
			if (outputDirectory == null) throw new ArgumentNullException(nameof(outputDirectory));

			using var _ = await RunCharonAsync
			(
				apiKey: string.Empty, CharonLogLevel.Normal,
				arguments: ToolRunOptions.FlattenArguments
				(
					"GENERATE", "TEMPLATES",
					"--outputDirectory", outputDirectory
				),
				configureTool
			);
		}

		/// <summary>
		/// Gets the version number of the charon tool executable.
		/// https://gamedevware.github.io/charon/advanced/commands/version.html
		/// </summary>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The version number as a string.</returns>
		public static async Task<string> GetVersionAsync(Action<ToolRunOptions> configureTool = null)
		{
			using var runResult = await RunCharonAsync(
				apiKey: string.Empty, CharonLogLevel.None,
				arguments: ToolRunOptions.FlattenArguments
				(
					"VERSION"
				),
				configureTool
			);

			var versionString = runResult.GetOutputData();
			return string.IsNullOrEmpty(versionString) ? "0.0.0.0" : versionString;
		}

		/// <summary>
		/// Gets the version of the charon tool executable used to create the specified GameData URL.
		/// </summary>
		/// <param name="gameDataUrl">The GameData URL to check.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is a server, otherwise empty.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The version number as a <see cref="Version"/>.</returns>
		public static async Task<Version> GetGameDataToolVersionAsync
		(
			string gameDataUrl,
			string apiKey,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (gameDataUrl == null) throw new ArgumentNullException(nameof(gameDataUrl));

			using var runResult = await RunCharonAsync(
				apiKey: apiKey, logsVerbosity,
				arguments: ToolRunOptions.FlattenArguments
				(
					"DATA", "VERSION", gameDataUrl
				),
				configureTool
			);
			return new Version(runResult.GetOutputData());
		}

		/// <summary>
		/// Run specified command with charon tool.
		/// https://gamedevware.github.io/charon/advanced/command_line.html
		/// Example:
		/// <code>
		/// var runResult = await CharonCli.RunAsync
		/// (
		///   new[] { "DATA", "BACKUP", "--dataBase", "/var/gamedata.json", "--output", "/var/gamedata_BACKUP.bson"},
		///   logsVerbosity: CharonLogLevel.Verbose
		/// );
		/// // runResult.ExitCode == 0 -> success
		/// </code>
		/// </summary>
		/// <param name="commandsAndOptions">List of commands and options to pass to charon tool.</param>
		/// <param name="apiKey">Authentication credentials if GameDataUrl is used, and it is a server, otherwise empty.</param>
		/// <param name="logsVerbosity">The verbosity level of logs. Defaults to CharonLogLevel.Normal.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>The version number as a <see cref="Version"/>.</returns>
		public static Task<ToolRunResult> RunCharonAsync
		(
			string[] commandsAndOptions,
			string apiKey,
			CharonLogLevel? logsVerbosity = null,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (commandsAndOptions == null) throw new ArgumentNullException(nameof(commandsAndOptions));

			return RunCharonAsync
			(
				apiKey: apiKey, logsVerbosity,
				arguments: commandsAndOptions,
				configureTool
			);
		}

		/// <summary>
		/// Run dotnet-t4 command-line tool for processing T4 templates. It is a general-purpose way to generate text or code files using C#.
		/// https://github.com/mono/t4/blob/main/dotnet-t4/readme.md
		/// </summary>
		/// <param name="templateFile">The path of the template .tt file.</param>
		/// <param name="referencedAssemblies">An assembly reference by path or assembly name. It will be resolved from the framework and assembly directories.</param>
		/// <param name="usings">A namespace imports which generate a using statement in template source code.</param>
		/// <param name="includeDirectories">A directory to be searched when resolving included files.</param>
		/// <param name="assemblyLookupDirectories">A directory to be searched when resolving assemblies.</param>
		/// <param name="parameters">Set session parameter <see cref="KeyValuePair{TKey,TValue}.Key"/> to <see cref="KeyValuePair{TKey,TValue}.Value"/>.
		/// The value is accessed from the template's Session dictionary, or from a property declared with a parameter directive: &lt;#@ parameter name='[name]' type='[type]' #&gt;.
		/// If the name matches a parameter with a non-string type, the value will be converted to that type.
		/// </param>
		/// <param name="useRelativeLinePragmas">Use relative paths in line pragmas.</param>
		/// <param name="debugMode">Generate debug symbols and keep temporary files.</param>
		/// <param name="verboseLogs">Output additional diagnostic information to stdout.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>Instance of running tool process. Check <see cref="ToolRunResult.ExitCode"/> for 0 to assess results.</returns>
		// ReSharper disable once FunctionComplexityOverflow
		public static Task<ToolRunResult> RunT4Async
		(
			string templateFile,
			IEnumerable<string> referencedAssemblies = null,
			IEnumerable<string> usings = null,
			IEnumerable<string> includeDirectories = null,
			IEnumerable<string> assemblyLookupDirectories = null,
			IEnumerable<KeyValuePair<string, string>> parameters = null,
			bool useRelativeLinePragmas = false,
			bool debugMode = false,
			bool verboseLogs = false,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (templateFile == null) throw new ArgumentNullException(nameof(templateFile));

			var t4Path = EnsureCharonRunScript("RunT4");

			var arguments = new List<string>();
			foreach (var referencedAssembly in referencedAssemblies ?? Array.Empty<string>())
			{
				arguments.Add("-r=" + referencedAssembly);
			}
			foreach (var usingStatement in usings ?? Array.Empty<string>())
			{
				arguments.Add("-u=" + usingStatement);
			}
			foreach (var includeDirectory in includeDirectories ?? Array.Empty<string>())
			{
				arguments.Add("-I=" + includeDirectory);
			}
			foreach (var assemblyLookupDirectory in assemblyLookupDirectories ?? Array.Empty<string>())
			{
				arguments.Add("-P=" + assemblyLookupDirectory);
			}
			foreach (var parameter in parameters ?? Array.Empty<KeyValuePair<string, string>>())
			{
				arguments.Add($"-p={parameter.Key}={parameter.Value}");
			}
			if (useRelativeLinePragmas)
			{
				arguments.Add("-l");
			}
			if (debugMode)
			{
				arguments.Add("--debug");
			}
			if (verboseLogs)
			{
				arguments.Add("--verbose");
			}
			arguments.Add(templateFile);

			var runOptions = new ToolRunOptions(t4Path, arguments.ToArray()) {
				CaptureStandardOutput = true,
				CaptureStandardError = true,
				ExecutionTimeout = TimeSpan.FromSeconds(30),
				WaitForExit = true
			};
			configureTool?.Invoke(runOptions);
			return CommandLineUtils.RunAsync(runOptions);
		}

		/// <summary>
		/// Run dotnet-t4 command-line tool for processing T4 templates. It is a general-purpose way to generate text or code files using C#.
		/// https://github.com/mono/t4/blob/main/dotnet-t4/readme.md
		/// </summary>
		/// <param name="templateFile">The path of the template .tt file.</param>
		/// <param name="outputFile">The name or path of the output file. It defaults to the input filename with its extension changed to .txt, or to match the generated code when preprocessing, and may be overridden by template settings. Use - instead of a filename to write to stdout.</param>
		/// <param name="usings">A namespace imports which generate a using statement in template source code.</param>
		/// <param name="templateClassName">Preprocess the template into class name for use as a runtime template. The class name may include a namespace.</param>
		/// <param name="useRelativeLinePragmas">Use relative paths in line pragmas.</param>
		/// <param name="debugMode">Generate debug symbols and keep temporary files.</param>
		/// <param name="verboseLogs">Output additional diagnostic information to stdout.</param>
		/// <param name="configureTool">Optional configuration delegate for tool process.</param>
		/// <returns>Instance of running tool process. Check <see cref="ToolRunResult.ExitCode"/> for 0 to assess results.</returns>
		// ReSharper disable once FunctionComplexityOverflow
		public static Task<ToolRunResult> PreprocessT4Async
		(
			string templateFile,
			string outputFile,
			string templateClassName,
			IEnumerable<string> usings = null,
			bool useRelativeLinePragmas = false,
			bool debugMode = false,
			bool verboseLogs = false,
			Action<ToolRunOptions> configureTool = null
		)
		{
			if (templateFile == null) throw new ArgumentNullException(nameof(templateFile));

			var t4Path = EnsureCharonRunScript("RunT4");

			var arguments = new List<string>();
			if (!string.IsNullOrEmpty(outputFile))
			{
				arguments.Add("--out=" + outputFile);
			}
			foreach (var usingStatement in usings ?? Array.Empty<string>())
			{
				arguments.Add("-u=" + usingStatement);
			}
			if (!string.IsNullOrEmpty(templateClassName))
			{
				arguments.Add("-c=" + templateClassName);
			}
			if (useRelativeLinePragmas)
			{
				arguments.Add("-l");
			}
			if (debugMode)
			{
				arguments.Add("--debug");
			}
			if (verboseLogs)
			{
				arguments.Add("--verbose");
			}
			arguments.Add(templateFile);

			var runOptions = new ToolRunOptions(t4Path, arguments.ToArray()) {
				CaptureStandardOutput = true,
				CaptureStandardError = true,
				ExecutionTimeout = TimeSpan.FromSeconds(30),
				WaitForExit = true
			};
			configureTool?.Invoke(runOptions);
			return CommandLineUtils.RunAsync(runOptions);
		}

		internal static void CleanUpLogsDirectory()
		{
			if (string.IsNullOrEmpty(CharonFileUtils.LibraryCharonLogsPath) || Directory.Exists(CharonFileUtils.LibraryCharonLogsPath) == false)
			{
				return;
			}

			var logger = CharonEditorModule.Instance.Logger;
			var logsRetentionTime = TimeSpan.FromDays(2);
			foreach (var logFile in Directory.GetFiles(CharonFileUtils.LibraryCharonLogsPath))
			{
				if (DateTime.UtcNow - File.GetLastWriteTimeUtc(logFile) <= logsRetentionTime)
				{
					continue; // not old enough
				}

				try
				{
					logger.Log(LogType.Assert, $"Deleting old log file at '{logFile}'.");

					File.Delete(logFile);
				}
				catch (Exception deleteError)
				{
					logger.Log(LogType.Warning, $"Failed to delete log file at '{logFile}'.");
					logger.Log(LogType.Warning, deleteError);
				}
			}
		}

		private static async Task<ToolRunResult> RunCharonAsync(string apiKey, CharonLogLevel? logsVerbosity, string[] arguments, Action<ToolRunOptions> configureTool)
		{
			if (arguments == null) throw new ArgumentNullException(nameof(arguments));

			var charonPath = EnsureCharonRunScript("RunCharon");

			logsVerbosity ??= CharonEditorModule.Instance.Settings.LogLevel;
			arguments = arguments.Concat(new[] { logsVerbosity == CharonLogLevel.Verbose ? "--verbose" : "" }).ToArray();

			var runOptions = new ToolRunOptions(charonPath, arguments) {
				CaptureStandardOutput = true,
				CaptureStandardError = true,
				ExecutionTimeout = TimeSpan.FromSeconds(30),
				WaitForExit = true,
				StartInfo = {
					EnvironmentVariables = {
						{ "DOTNET_CONTENTROOT", CharonFileUtils.CharonAppContentPath },
						{ "CHARON_API_KEY", apiKey ?? string.Empty },

						{ "SERILOG__WRITETO__0__NAME", "File" }, {
							"SERILOG__WRITETO__0__ARGS__PATH", Path.GetFullPath(Path.Combine(CharonFileUtils.LibraryCharonLogsPath,
								$"{DateTime.UtcNow:yyyy_MM_dd_hh}.charon.unity.log"))
						},
					}
				}
			};

			configureTool?.Invoke(runOptions);

			var runResult = await CommandLineUtils.RunAsync(runOptions);

			if (runResult.ExitCode != 0)
			{
				throw new InvalidOperationException((runResult.GetErrorData() ??
						"An error occurred.") +
					$" Process exit code: {runResult.ExitCode}.");
			}

			return runResult;
		}

		private static string WriteJsonInput(JsonValue jsonValue, [CallerMemberName] string memberName = "Command")
		{
			var tempFilePath = CreateTemporaryFile("json", memberName);
			using var fileStream = File.Create(tempFilePath);
			using var textWriter = new StreamWriter(fileStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 4096, leaveOpen: true);
#if JSON_NET_3_0_2_OR_NEWER
			using var jsonWriter = new JsonTextWriter(textWriter);
			jsonWriter.CloseOutput = false;
			jsonWriter.Formatting = Formatting.Indented;
			jsonValue.WriteTo(jsonWriter);
#else
			jsonValue.Save(textWriter, pretty: true);
#endif
			return tempFilePath;
		}
		private static JsonValue ReadOutputJson(string filePath)
		{
			using var textReader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);

#if JSON_NET_3_0_2_OR_NEWER
			using var jsonReader = new JsonTextReader(textReader);
			jsonReader.CloseInput = false;
			return JsonValue.ReadFrom(jsonReader);
#else
			return JsonValue.Load(textReader);
#endif

		}
		private static string CreateTemporaryFile(string extension, [CallerMemberName] string memberName = "Command")
		{
			var tempFileName = Path.GetFullPath(Path.Combine(Path.Combine(CharonFileUtils.TempPath, "charoncli"),
				memberName + "_" + Guid.NewGuid().ToString().Replace("-", "") + "." + extension));

			if (!Directory.Exists(Path.GetDirectoryName(tempFileName)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(tempFileName)!);
			}

			return tempFileName;
		}

		private static string EnsureCharonRunScript(string scriptName)
		{
			if (scriptName == null) throw new ArgumentNullException(nameof(scriptName));

			if (!ScriptFilesAreCopied)
			{
				CopyScriptFiles(scriptName);
				ScriptFilesAreCopied = true;
			}

			foreach (var scriptFilePath in Directory.GetFiles(CharonFileUtils.LibraryCharonPath))
			{
				if (string.Equals(Path.GetFileNameWithoutExtension(Path.GetFileName(scriptFilePath)), scriptName, StringComparison.OrdinalIgnoreCase))
				{
					return scriptFilePath;
				}
			}
			throw new InvalidOperationException($"Unable to find '{scriptName}' script in '{CharonFileUtils.LibraryCharonPath}' directory.");
		}
		private static void CopyScriptFiles(string scriptName)
		{
			var oldExtension = "windows";
			var newExtension = default(string);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				oldExtension = "windows";
				newExtension = "bat";
			}
			else
			{
				oldExtension = "unix";
				newExtension = "sh";
			}

			foreach (var sourceFilePath in Directory.GetFiles(Path.Combine(CharonFileUtils.PluginBasePath, "Scripts"), "*." + oldExtension))
			{
				var targetFilePath = Path.Combine(CharonFileUtils.LibraryCharonPath, Path.ChangeExtension(Path.GetFileName(sourceFilePath), newExtension));
				var targetDirectoryPath = Path.GetDirectoryName(targetFilePath) ?? "";

				if (CharonFileUtils.HasSameContent(sourceFilePath, targetFilePath))
				{
					continue;
				}

				if (File.Exists(targetFilePath))
				{
					try { File.Delete(targetFilePath); }
					catch { continue; /* skip busy file */ }
				}

				if (!Directory.Exists(targetDirectoryPath))
				{
					Directory.CreateDirectory(targetDirectoryPath);
				}

				File.Copy(sourceFilePath, targetFilePath);

				if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					Chmod(targetFilePath, "+x", deleteFileOnFail: true);
				}
			}
		}

		private static void Chmod(string filePath, string permissions, bool deleteFileOnFail)
		{
			try
			{
				var processStartInfo = new ProcessStartInfo
				{
					FileName = "chmod",
					Arguments = $"\"{permissions}\" \"{filePath}\"",
					UseShellExecute = true,
					CreateNoWindow = true,
				};

				using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
				using var process = Process.Start(processStartInfo)!;
				timeout.Token.Register(process.EndGracefully);
				process.WaitForExit();
			}
			catch
			{
				CharonFileUtils.SafeFileDelete(filePath); // delete file because it doesn't have proper permissions
				throw;
			}
		}
	}
}
