using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Editor.Services;
using Editor.Services.ResourceServerApi;
using GameDevWare.Charon.Editor.Json;
using GameDevWare.Charon.Editor.Routines;
using GameDevWare.Charon.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Services
{
	internal class UnityResourceServer : IDisposable
	{
		private readonly HttpListener listener;
		private readonly FormulaTypeIndexer formulaTypeIndexer;
		private readonly CancellationTokenSource cancellationTokenSource;
		private readonly CharonSettings settings;
		private readonly CharonLogger logger;
		private Task receiveTask;

		public int Port { get; }
		public Task Completion => this.receiveTask.IgnoreFault();

		public UnityResourceServer(CharonLogger logger)
		{
			if (logger == null) throw new ArgumentNullException(nameof(logger));

			this.listener = new HttpListener();
			this.cancellationTokenSource = new CancellationTokenSource();
			this.formulaTypeIndexer = new FormulaTypeIndexer();
			this.Port = 10000 + Process.GetCurrentProcess().Id % 65000;
			this.logger = logger;
			this.receiveTask = Task.CompletedTask;
		}

		public void Initialize()
		{
			this.receiveTask = this.ReceiveRequestsAsync(this.cancellationTokenSource.Token);
		}

		private async Task ReceiveRequestsAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					this.listener.Prefixes.Clear();
					this.listener.Prefixes.Add("http://127.0.0.1:" + this.Port + "/");

					this.listener.Start();

					this.logger.Log(LogType.Assert, $"Resource server is listening at '{this.listener.Prefixes.FirstOrDefault()}'.");
				}
				catch (Exception startError)
				{
					this.logger.Log(LogType.Assert, $"Failed to start resource server at '{this.listener.Prefixes.FirstOrDefault()}' due to an error.");
					this.logger.Log(LogType.Assert, startError.Unwrap());

					await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
					continue; // retry
				}

				await Task.Yield();

				while (!cancellationToken.IsCancellationRequested)
				{
					var context = await this.listener.GetContextAsync().ConfigureAwait(false);
					this.DispatchRequest(context);
				}

				try
				{
					this.listener.Stop();
				}
				catch (Exception stopError)
				{
					this.logger.Log(LogType.Assert, $"Failed to stop resource server at '{this.listener.Prefixes.FirstOrDefault()}' due to an error.");
					this.logger.Log(LogType.Assert, stopError.Unwrap());
				}
			}
		}

		private async void DispatchRequest(HttpListenerContext context)
		{
			try
			{
				using var _ = context.Response;
				context.Response.StatusCode =(int)HttpStatusCode.OK;

				var stopwatch = Stopwatch.StartNew();
				this.logger.Log(LogType.Assert, $"Received [{context.Request.HttpMethod.ToUpperInvariant()}] {context.Request.Url.LocalPath} request.");

				var localPath = context.Request.Url.LocalPath;
				if (context.Request.HttpMethod == "OPTIONS")
				{
					await this.OnOptionsRequestAsync(context.Request, context.Response).ConfigureAwait(false);
				}
				else if (localPath.StartsWith("/api/commands/generate-code", StringComparison.OrdinalIgnoreCase))
				{
					await this.OnGenerateCodeAsync(context.Request, context.Response).ConfigureAwait(false);
				}
				else if (localPath.StartsWith("/api/commands/publish", StringComparison.OrdinalIgnoreCase))
				{
					await this.OnPublishAsync(context.Request, context.Response).ConfigureAwait(false);
				}
				else if (localPath.StartsWith("/api/formula-types/list", StringComparison.OrdinalIgnoreCase))
				{
					await this.OnListFormulaTypesAsync(context.Request, context.Response).ConfigureAwait(false);
				}

				this.logger.Log(LogType.Assert, $"Finished [{context.Request.HttpMethod.ToUpperInvariant()}] {context.Request.Url.LocalPath} request in {stopwatch.ElapsedMilliseconds:F0}ms with {context.Response.StatusCode} status code.");
			}
			catch (Exception requestError)
			{
				try
				{
					this.logger.Log(LogType.Assert, $"Failed to process request to '{context.Request.Url}' due to an error.");
					this.logger.Log(LogType.Assert, requestError.Unwrap());

					context.Response.Headers.Clear();
					context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
					context.Response.Close();
				}
				catch { /* failed to finish request */ }
			}
		}

		private Task OnOptionsRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
		{
			this.AddCorsHeaders(request, response);

			response.StatusCode = (int)HttpStatusCode.NoContent;

			return Task.CompletedTask;
		}
		private async Task OnPublishAsync(HttpListenerRequest request, HttpListenerResponse response)
		{
			if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("POST method is expected for this endpoint.");

			this.AddCorsHeaders(request, response);

			var publishRequest = ReadRequestBody<PublishRequest>(request);
			var paths = new[] { AssetDatabase.GUIDToAssetPath(publishRequest.GameDataAssetId) };
			await SynchronizeAssetsRoutine.ScheduleAsync(paths).ConfigureAwait(false);
			response.StatusCode = (int)HttpStatusCode.NoContent;
		}
		private async Task OnGenerateCodeAsync(HttpListenerRequest request, HttpListenerResponse response)
		{
			if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("POST method is expected for this endpoint.");

			this.AddCorsHeaders(request, response);

			var publishRequest = ReadRequestBody<GenerateSourceCodeRequest>(request);
			var paths = new[] { AssetDatabase.GUIDToAssetPath(publishRequest.GameDataAssetId) };
			await GenerateSourceCodeRoutine.ScheduleAsync(paths).ConfigureAwait(false);
			response.StatusCode = (int)HttpStatusCode.NoContent;
		}
		private Task OnListFormulaTypesAsync(HttpListenerRequest request, HttpListenerResponse response)
		{
			if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("POST method is expected for this endpoint.");

			this.AddCorsHeaders(request, response);

			var listFormulaTypesRequest = ReadRequestBody<ListFormulaTypesRequest>(request);
			var listFormulaTypesResponse = this.formulaTypeIndexer.ListFormulaTypes
			(
				listFormulaTypesRequest.Skip,
				listFormulaTypesRequest.Take,
				listFormulaTypesRequest.Query
			);

			response.StatusCode = (int)HttpStatusCode.OK;
			this.WriteResponseBody(listFormulaTypesResponse, response);
			return Task.CompletedTask;
		}

		private static T ReadRequestBody<T>(HttpListenerRequest request)
		{
			var jsonValue = JsonValue.Load(request.InputStream);
			return jsonValue.ToObject<T>();
		}
		private void WriteResponseBody<T>(T responseObject, HttpListenerResponse response)
		{
			response.ContentType = "application/json";
			response.SendChunked = true;

			JsonObject.From(responseObject).Save(response.OutputStream);
		}

		private void AddCorsHeaders(HttpListenerRequest request, HttpListenerResponse response)
		{
			var origin = request.Headers["Origin"];
			if (string.IsNullOrEmpty(origin))
			{
				origin = "*";
			}
			response.Headers.Add("Access-Control-Allow-Origin", origin);
			response.Headers.Add("Access-Control-Allow-Methods", "*");
			response.Headers.Add("Access-Control-Allow-Headers", "*, Authorization");
			response.Headers.Add("Access-Control-Max-Age", "60000");
		}

		/// <inheritdoc />
		public void Dispose()
		{
			this.cancellationTokenSource.Cancel();
			((IDisposable)this.listener)?.Dispose();
		}
	}
}
