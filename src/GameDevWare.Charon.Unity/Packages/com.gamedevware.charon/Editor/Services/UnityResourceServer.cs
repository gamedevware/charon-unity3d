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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Json;
using GameDevWare.Charon.Editor.Routines;
using GameDevWare.Charon.Editor.Services.Http;
using GameDevWare.Charon.Editor.Services.ResourceServerApi;
using GameDevWare.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Services
{
	internal class UnityResourceServer : HttpMessageHandler
	{
		private readonly FormulaTypeIndexer formulaTypeIndexer;
		private readonly AssetIndexer assetIndexer;
		private readonly CancellationTokenSource cancellationTokenSource;
		private readonly CharonSettings settings;
		private readonly CharonLogger logger;
		private readonly TaskScheduler uiTaskScheduler;
		private Task receiveTask;
		private int isDisposed;

		public int Port { get; }
		public Task Completion => this.receiveTask.IgnoreFault();

		public UnityResourceServer(CharonLogger logger)
		{
			if (logger == null) throw new ArgumentNullException(nameof(logger));

			this.cancellationTokenSource = new CancellationTokenSource();
			this.formulaTypeIndexer = new FormulaTypeIndexer();
			this.assetIndexer = new AssetIndexer();
			this.Port = 10000 + Process.GetCurrentProcess().Id % 55000;
			this.logger = logger;
			this.receiveTask = Task.CompletedTask;
			this.uiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
		}

		public void Initialize()
		{
			this.receiveTask = this.ReceiveRequestsAsync(this.cancellationTokenSource.Token);
		}

		private async Task ReceiveRequestsAsync(CancellationToken cancellationToken)
		{
			var listenEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), this.Port);

			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					using var httpServer = new HttpServer(listenEndPoint, this, this.logger, cancellationToken);

					cancellationToken.ThrowIfCancellationRequested();
					cancellationToken.Register(state => ((IDisposable)state).Dispose(), httpServer);

					this.logger.Log(LogType.Assert, $"Resource server is listening at '{listenEndPoint}'.");

					await httpServer.Completion.ConfigureAwait(false);
				}
				catch (Exception startError)
				{
					if (cancellationToken.IsCancellationRequested)
					{
						break;
					}

					this.logger.Log(LogType.Assert, $"Failed to start resource server at '{listenEndPoint}' due to an error.");
					this.logger.Log(LogType.Assert, startError.Unwrap());

					await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
					continue; // retry
				}

				await Task.Yield();
			}

			this.Dispose();
		}

		/// <inheritdoc />
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			using var _ = request;
			var response = default(HttpResponseMessage);

			var stopwatch = Stopwatch.StartNew();
			this.logger.Log(LogType.Assert, $"Received [{request.Method}] {request.RequestUri.LocalPath} request.");

			var localPath = request.RequestUri.LocalPath;
			if (string.Equals(request.Method.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
			{
				response = await this.OnOptionsRequestAsync(request).ConfigureAwait(false);
			}
			else if (localPath.StartsWith("/api/commands/generate-code", StringComparison.OrdinalIgnoreCase))
			{
				response = await this.OnGenerateCodeAsync(request).ConfigureAwait(false);
			}
			else if (localPath.StartsWith("/api/commands/publish", StringComparison.OrdinalIgnoreCase))
			{
				response = await this.OnPublishAsync(request).ConfigureAwait(false);
			}
			else if (localPath.StartsWith("/api/formula-types/list", StringComparison.OrdinalIgnoreCase))
			{
				response = await this.OnListFormulaTypesAsync(request).ConfigureAwait(false);
			}
			else if (localPath.StartsWith("/api/assets/list", StringComparison.OrdinalIgnoreCase))
			{
				response = await this.OnListAssetsAsync(request).ConfigureAwait(false);
			}
			else if (localPath.StartsWith("/api/assets/thumbnail", StringComparison.OrdinalIgnoreCase))
			{
				response = await this.OnGetAssetThumbnailAsync(request).ConfigureAwait(false);
			}
			else
			{
				response = new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
			}

			this.logger.Log(LogType.Assert, $"Finished [{request.Method}] {request.RequestUri.LocalPath} request in {stopwatch.ElapsedMilliseconds:F0}ms with {response.StatusCode} status code.");

			return response;
		}

		private Task<HttpResponseMessage> OnOptionsRequestAsync(HttpRequestMessage request)
		{
			var response = new HttpResponseMessage(HttpStatusCode.NoContent);
			this.AddCorsHeaders(request, response);

			return Task.FromResult(response);
		}
		private async Task<HttpResponseMessage> OnPublishAsync(HttpRequestMessage request)
		{
			if (!string.Equals(request.Method.Method, "POST", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException($"POST method is expected for [{request.Method}]{request.RequestUri} endpoint.");

			var publishRequest = await ReadRequestBodyAsync<PublishRequest>(request).ConfigureAwait(false);

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			this.AddCorsHeaders(request, response);

			await this.uiTaskScheduler.SwitchTo();

			var gameDataAssetPath = AssetDatabase.GUIDToAssetPath(publishRequest.GameDataAssetId ??
#pragma warning disable CS0612 // Type or member is obsolete
				publishRequest.UnityAssetId
#pragma warning restore CS0612 // Type or member is obsolete
			);
			var gameDataAsset = AssetDatabase.LoadAssetAtPath<GameDataBase>(gameDataAssetPath);
			if (gameDataAsset == null)
			{
				response.StatusCode = HttpStatusCode.NotFound;
				return response;
			}

			gameDataAsset.settings.publishFormat = (int)(FormatsExtensions.GetGameDataFormatForContentType(publishRequest.Format) ??
				(GameDataFormat)gameDataAsset.settings.publishFormat);
			if (publishRequest.Languages == null || (publishRequest.Languages.Length == 1 && publishRequest.Languages[0] == "*"))
			{
				gameDataAsset.settings.publishLanguages = null;
			}
			else
			{
				gameDataAsset.settings.publishLanguages = publishRequest.Languages;
			}
			EditorUtility.SetDirty(gameDataAsset);
			AssetDatabase.SaveAssetIfDirty(gameDataAsset);

			var reportCallback = ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_PROGRESS_IMPORTING);
			var synchronizeAssetsTask = SynchronizeAssetsRoutine.ScheduleAsync(new[] { gameDataAssetPath }, reportCallback);
			synchronizeAssetsTask.ContinueWithHideProgressBar();

			await synchronizeAssetsTask.ConfigureAwait(false);

			response.StatusCode = HttpStatusCode.NoContent;
			return response;
		}
		private async Task<HttpResponseMessage> OnGenerateCodeAsync(HttpRequestMessage request)
		{
			if (!string.Equals(request.Method.Method, "POST", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException($"POST method is expected for [{request.Method}]{request.RequestUri} endpoint.");

			var generateSourceCodeRequest = await ReadRequestBodyAsync<GenerateSourceCodeRequest>(request).ConfigureAwait(false);

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			this.AddCorsHeaders(request, response);

			await this.uiTaskScheduler.SwitchTo();

			var gameDataAssetPath = AssetDatabase.GUIDToAssetPath(generateSourceCodeRequest.GameDataAssetId ??
#pragma warning disable CS0612 // Type or member is obsolete
				generateSourceCodeRequest.UnityAssetId
#pragma warning restore CS0612 // Type or member is obsolete

			);
			var gameDataAsset = AssetDatabase.LoadAssetAtPath<GameDataBase>(gameDataAssetPath);
			if (gameDataAsset == null)
			{
				response.StatusCode = HttpStatusCode.NotFound;
				return response;
			}

			var reportCallback = ProgressUtils.ShowProgressBar(Resources.UI_UNITYPLUGIN_GENERATING_SOURCE_CODE);
			var generateSourceCodeTask = GenerateSourceCodeRoutine.ScheduleAsync(new[] { gameDataAssetPath }, reportCallback);
			generateSourceCodeTask.ContinueWithHideProgressBar();
			await generateSourceCodeTask.ConfigureAwait(false);

			response.StatusCode = HttpStatusCode.NoContent;
			return response;
		}
		private async Task<HttpResponseMessage> OnListFormulaTypesAsync(HttpRequestMessage request)
		{
			if (!string.Equals(request.Method.Method, "POST", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException($"POST method is expected for [{request.Method}]{request.RequestUri} endpoint.");

			var listFormulaTypesRequest = await ReadRequestBodyAsync<ListFormulaTypesRequest>(request).ConfigureAwait(true);

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			this.AddCorsHeaders(request, response);

			var listFormulaTypesResponse = this.formulaTypeIndexer.ListFormulaTypes
			(
				listFormulaTypesRequest.Skip,
				listFormulaTypesRequest.Take,
				listFormulaTypesRequest.Query

			);

			response.StatusCode = HttpStatusCode.OK;
			this.WriteResponseBody(listFormulaTypesResponse, response);
			return response;
		}
		private async Task<HttpResponseMessage> OnGetAssetThumbnailAsync(HttpRequestMessage request)
		{
			if (!string.Equals(request.Method.Method, "POST", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException($"POST method is expected for [{request.Method}]{request.RequestUri} endpoint.");

			var getAssetThumbnailRequest = await ReadRequestBodyAsync<GetAssetThumbnailRequest>(request).ConfigureAwait(true);

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			this.AddCorsHeaders(request, response);

			var thumbnailBytes = this.assetIndexer.GetAssetThumbnail
			(
				getAssetThumbnailRequest.Path,
				getAssetThumbnailRequest.Size
			);

			if (thumbnailBytes == null)
			{
				response.StatusCode = HttpStatusCode.NotFound;
			}
			else
			{
				response.StatusCode = HttpStatusCode.OK;
				response.Content = new ByteArrayContent(thumbnailBytes) {
					Headers = {
						ContentType = MediaTypeHeaderValue.Parse("image/png")
					}
				};
			}

			return response;
		}
		private async Task<HttpResponseMessage> OnListAssetsAsync(HttpRequestMessage request)
		{
			if (!string.Equals(request.Method.Method, "POST", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException($"POST method is expected for [{request.Method}]{request.RequestUri} endpoint.");

			var listAssetsRequest = await ReadRequestBodyAsync<ListAssetsRequest>(request).ConfigureAwait(true);

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			this.AddCorsHeaders(request, response);

			var listAssetsResponse = this.assetIndexer.ListAssets
			(
				listAssetsRequest.Skip,
				listAssetsRequest.Take,
				listAssetsRequest.Query,
				listAssetsRequest.Types
			);

			response.StatusCode = HttpStatusCode.OK;
			this.WriteResponseBody(listAssetsResponse, response);
			return response;
		}

		private static async Task<T> ReadRequestBodyAsync<T>(HttpRequestMessage request)
		{
			if(request.Content == null) throw new InvalidOperationException($"Request to [{request.Method}]{request.RequestUri} doesn't have body.");

			await using var stream = await request.Content.ReadAsStreamAsync().ConfigureAwait(true);

			var jsonValue = JsonValue.Load(stream);
			return jsonValue.ToObject<T>();
		}
		private void WriteResponseBody<T>(T responseObject, HttpResponseMessage response)
		{
			var responseStream = new MemoryStream();
			JsonObject.From(responseObject).Save(responseStream);
			responseStream.Position = 0;

			response.Content = new StreamContent(responseStream) {
				Headers = {
					ContentType = MediaTypeHeaderValue.Parse("application/json")
				}
			};
		}

		private void AddCorsHeaders(HttpRequestMessage request, HttpResponseMessage responseMessage)
		{
			var origin = request.Headers.GetValues("Origin").FirstOrDefault();
			if (string.IsNullOrEmpty(origin))
			{
				origin = "*";
			}
			responseMessage.Headers.Add("Access-Control-Allow-Origin", origin);
			responseMessage.Headers.Add("Access-Control-Allow-Methods", "*");
			responseMessage.Headers.Add("Access-Control-Allow-Headers", "*, Authorization");
			responseMessage.Headers.Add("Access-Control-Max-Age", "60000");
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (Interlocked.Exchange(ref this.isDisposed, 1) != 0)
			{
				return;
			}
			this.cancellationTokenSource.Cancel();
			//this.cancellationTokenSource.Dispose();
			base.Dispose(disposing);
		}
	}
}
