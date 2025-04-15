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
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GameDevWare.Charon.Editor.Utils;
using UnityEngine;

namespace GameDevWare.Charon.Editor.Services.Http
{
	public class HttpServer : IDisposable
	{
		private const int DEFAULT_BUFFER_SIZE = 1024 * 64;
		private const int MAX_CONNECTION_REUSE_COUNT = 200;

#if UNITY_EDITOR_WIN
		// P/Invoke declarations for SetHandleInformation
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetHandleInformation(IntPtr hObject, int dwMask, int dwFlags);
		private const int HANDLE_FLAG_INHERIT = 0x00000001;
		private const int HANDLE_FLAG_PROTECT_FROM_CLOSE = 0x00000002;
#else
    [DllImport("libc", SetLastError = true)]
    private static extern int fcntl(int fd, int cmd, int arg);

		private const int F_GETFD = 1;
		private const int F_SETFD = 2;
		private const int FD_CLOEXEC = 1;
#endif

		private readonly HttpResponseHeaders defaultHeaders;
		private readonly ConcurrentBag<MemoryStream> memoryStreams;
		private readonly ConcurrentBag<byte[]> buffers;
		private readonly HttpMessageInvoker httpMessageInvoker;
		private readonly ILogger logger;
		private readonly Uri baseAddress;
		private readonly TcpListener listener;

		public Task Completion { get; }

		public HttpServer(IPEndPoint ipEndPoint, HttpMessageHandler httpMessageHandler, ILogger logger, CancellationToken cancellationToken)
		{
			this.httpMessageInvoker = new HttpMessageInvoker(httpMessageHandler, disposeHandler: false);
			this.logger = logger;

			this.baseAddress = new Uri($"http://{ipEndPoint.Address}:{ipEndPoint.Port}/");
			this.defaultHeaders = new HttpResponseMessage {
				Headers = {
					{ "Server", HttpUtils.UserAgentHeaderValue }
				}
			}.Headers;

			this.memoryStreams = new ConcurrentBag<MemoryStream>();
			this.buffers = new ConcurrentBag<byte[]>();
			this.listener = new TcpListener(ipEndPoint);
			this.listener.ExclusiveAddressUse = true;
			this.DisableSocketHandleInheritance(this.listener.Server);

			this.listener.Start();

			this.Completion = this.StartAcceptingClientsAsync(cancellationToken);
		}
		private void DisableSocketHandleInheritance(Socket socket)
		{
#if UNITY_EDITOR_WIN
			var handle = socket.Handle;
			// Disable handle inheritance
			if (!SetHandleInformation(handle, HANDLE_FLAG_INHERIT, 0))
			{
				throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
			}
#else
	        var fd = socket.Handle.ToInt32();
	        var flags = fcntl(fd, F_GETFD, 0);
	        if (flags == -1)
	        {
	            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
	        }

	        if (fcntl(fd, F_SETFD, flags | FD_CLOEXEC) == -1)
	        {
	            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
	        }
#endif
		}
		private async Task StartAcceptingClientsAsync(CancellationToken cancellationToken)
		{
			await Task.Yield();

			while (!cancellationToken.IsCancellationRequested)
			{
				var requestSocket = await this.listener.AcceptSocketAsync().IgnoreFault();
				if (requestSocket == null)
				{
					break; // stopped
				}
				this.ProcessIncomingRequestAsync(requestSocket, cancellationToken).LogFaultAsError();
			}
		}
		private async Task ProcessIncomingRequestAsync(Socket requestSocket, CancellationToken cancellationToken)
		{
			const int STAGE_RECEIVING_REQUEST_HEADERS = 0;
			const int STAGE_RECEIVING_REQUEST_BODY = 1;
			const int STAGE_WAITING_RESPONSE = 2;
			const int STAGE_BUFFERING_RESPONSE_BODY = 3;
			const int STAGE_SENDING_RESPONSE_HEADERS = 4;
			const int STAGE_SENDING_RESPONSE_BODY = 5;
			const int STAGE_SENDING_RESPONSE_DONE = 6;

			await Task.Yield();

			await using var httpStream = new NetworkStream(requestSocket, ownsSocket: true);
			await using var _ = cancellationToken.Register(state => ((IDisposable)state).Dispose(), requestSocket);
			var remoteEndpoint = GetRemoteEndpoint(requestSocket);

			var stage = STAGE_RECEIVING_REQUEST_HEADERS;
			var incomingRequestStream = this.GetMemoryStream();
			var requestBuffer = this.GetBuffer();
			var responseBuffer = this.GetBuffer();
			var requestNumber = 0;
			try
			{
				var requestBufferAvailable = 0;
				while (true)
				{
					var read = 0;
					var headersEndIndex = -1;
					stage = STAGE_RECEIVING_REQUEST_HEADERS;
					do
					{
						requestBufferAvailable += read;

						headersEndIndex = Http11Protocol.GetHeadersEndIndex(requestBuffer.AsSpan(0, requestBufferAvailable), skipStartingNewLines: true);
						if (headersEndIndex < 0)
						{
							if (requestBufferAvailable == requestBuffer.Length)
							{
								throw new WebException($"HTTP request line and headers too long (> {requestBuffer.Length} bytes).",
									WebExceptionStatus.ReceiveFailure);
							}

							continue;
						}

						break;
					} while ((read = await httpStream.ReadAsync(requestBuffer, requestBufferAvailable, requestBuffer.Length - requestBufferAvailable, CancellationToken.None)) > 0);

					if (headersEndIndex < 0)
					{
						if (requestBufferAvailable > 0)
						{
							throw new WebException("Connection is closed while receiving next request.", WebExceptionStatus.ConnectionClosed);
						}

						break;
					}

					requestNumber++;

					var requestMessage = this.ReadRequestHead(requestBuffer.AsSpan(0, headersEndIndex), out var requestBody);

					stage = STAGE_RECEIVING_REQUEST_BODY;

					requestBufferAvailable = await ReadRequestBodyAsync(httpStream, requestBody, requestBuffer, headersEndIndex, requestBufferAvailable - headersEndIndex);

					stage = STAGE_WAITING_RESPONSE;

					var responseMessage = await this.httpMessageInvoker.SendAsync(requestMessage, CancellationToken.None).ConfigureAwait(false);

					stage = STAGE_BUFFERING_RESPONSE_BODY;

					var responseContentStream = default(Stream);
					if (responseMessage.Content != null)
					{
						responseContentStream = this.GetMemoryStream();
						await responseMessage.Content.CopyToAsync(responseContentStream).ConfigureAwait(false);
						responseContentStream.Position = 0;
					}

					stage = STAGE_SENDING_RESPONSE_HEADERS;

					var isLast = requestNumber == MAX_CONNECTION_REUSE_COUNT ||
						IsLastRequest(requestMessage.Headers, responseMessage.Headers);
					var connectionHeader = isLast ? "close" : "keep-alive";
					var keepAliveHeader = requestNumber == 1 && !isLast ? $"timeout=30, max={MAX_CONNECTION_REUSE_COUNT}" : null;

					await Http11Protocol.WriteResponseHeadAsync(
						httpStream, responseBuffer,
						(int)responseMessage.StatusCode,
						responseMessage.StatusCode.ToString(),
						this.defaultHeaders,
						responseMessage.Headers,
						responseMessage.Content?.Headers,
						(responseContentStream?.Length ?? 0).ToString(),
						transferEncoding: null,
						connectionHeader: connectionHeader,
						keepAliveHeader: keepAliveHeader
					);

					stage = STAGE_SENDING_RESPONSE_BODY;

					if (responseContentStream != null)
					{
						await responseContentStream.CopyToAsync(httpStream, 4096, CancellationToken.None).ConfigureAwait(false);
					}

					await httpStream.FlushAsync(CancellationToken.None).ConfigureAwait(false);

					stage = STAGE_SENDING_RESPONSE_DONE;

					if (isLast)
					{
						break;
					}
				}

				requestSocket.Shutdown(SocketShutdown.Send);
				while (await httpStream.ReadAsync(responseBuffer, 0, responseBuffer.Length, CancellationToken.None) > 0)
				{
					// drain socket
				}
			}
			catch (Exception processError)
			{
				this.logger.Log(LogType.Assert, $"Failed to process incoming HTTP request from '{remoteEndpoint}' due to an error.");
				this.logger.Log(LogType.Assert, processError);

				// send default error response if response is not ready
				if (stage < STAGE_SENDING_RESPONSE_HEADERS)
				{
					await this.WriteStatusCodeAsync
					(
						requestSocket,
						responseBuffer,
						new HttpResponseMessage(HttpStatusCode.InternalServerError)
					).IgnoreFault().ConfigureAwait(false);
				}
			}
			finally
			{
				this.ReturnMemoryStream(incomingRequestStream);
				this.ReturnBuffer(responseBuffer);
				this.ReturnBuffer(requestBuffer);
			}

			static string GetRemoteEndpoint(Socket socket)
			{
				try
				{
					return socket.RemoteEndPoint.ToString();
				}
				catch
				{
					return "<unknown>";
				}
			}
		}

		private HttpRequestMessage ReadRequestHead(Span<byte> requestLine, out MemoryStream requestBody)
		{
			var parseResult = Http11Protocol.ParseHttpRequestLine(ref requestLine, out var httpVersion, out var method, out var url);
			if (parseResult != HttpParseResult.Ok)
			{
				throw ProtocolErrorWebException(parseResult);
			}

			if (url == "*")
			{
				url = "/";
			}

			var httpMethod = Http11Protocol.GetHttpMethod(method);
			var requestMessage = new HttpRequestMessage(httpMethod, new Uri(this.baseAddress, url)) {
				Version = httpVersion
			};

			var headers = ReadRequestHeaders(requestLine);
			foreach (string headerName in headers.Keys)
			{
				if (IsContentHeader(headerName)) continue;
				var headerValue = headers.GetValues(headerName);
				if (headerValue == null || headerValue.Length == 0) continue;

				requestMessage.Headers.TryAddWithoutValidation(headerName, headerValue);
			}

			if (!string.IsNullOrEmpty(headers["Content-Length"]))
			{
				if (!long.TryParse(headers["Content-Length"], out var contentLength))
				{
					throw new WebException("Invalid 'Content-Length' header value.", WebExceptionStatus.ProtocolError);
				}

				requestBody = this.GetMemoryStream();
				requestBody.SetLength(contentLength);

				requestMessage.Content = new StreamContent(requestBody, 4096);

				foreach (string headerName in headers.Keys)
				{
					if (!IsContentHeader(headerName)) continue;
					var headerValue = headers.GetValues(headerName);
					if (headerValue == null || headerValue.Length == 0) continue;

					requestMessage.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
				}
			}
			else
			{
				requestBody = null;
			}

			return requestMessage;


			static bool IsContentHeader(string headerName)
			{
				return string.Equals(headerName, "Allow", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Content-Disposition", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Content-Encoding", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Content-Language", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Content-Length", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Content-Location", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Content-MD5", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Content-Range", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Content-Type", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Expires", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Last-Modified", StringComparison.OrdinalIgnoreCase);
			}
		}

		private static NameValueCollection ReadRequestHeaders(Span<byte> requestLine)
		{
			var headers = new NameValueCollection();
			HttpParseResult parseResult;
			while (true)
			{
				var headerName = default(string);
				var headerValue = default(string);

				parseResult = Http11Protocol.ParseHttpHeader(ref requestLine, ref headerName, ref headerValue);
				if (parseResult != HttpParseResult.ContinuedHeaderValue && parseResult != HttpParseResult.Ok)
				{
					break;
				}

				if (string.IsNullOrEmpty(headerName) || string.IsNullOrEmpty(headerValue))
				{
					continue; // skip empty header
				}

				if (IsDoNotSplit(headerName))
				{
					headers[headerName] = headerValue;
				}
				else
				{
					foreach (var value in headerValue.Split(","))
					{
						var trimmedValue = value.Trim();
						if (string.IsNullOrEmpty(trimmedValue))
						{
							continue;
						}

						headers.Add(headerName, trimmedValue);
					}
				}
			}

			if (parseResult != HttpParseResult.EndOfHeaders)
			{
				throw ProtocolErrorWebException(parseResult);
			}

			return headers;


			static bool IsDoNotSplit(string headerName)
			{
				return string.Equals(headerName, "Authorization", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Cookie", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "From", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Host", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "If-Modified-Since", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "If-Unmodified-Since", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Max-Forwards", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Referer", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Content-Disposition", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "X-Requested-With", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "DNT", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "X-Forwarded-Proto", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "Front-End-Https", StringComparison.OrdinalIgnoreCase) ||
					string.Equals(headerName, "X-Csrf-Token", StringComparison.OrdinalIgnoreCase);
			}
		}
		private static async Task<int> ReadRequestBodyAsync(Stream requestStream, MemoryStream requestBody, byte[] buffer, int offset, int count)
		{
			if (requestBody == null)
			{
				// move to the start of buffer
				Buffer.BlockCopy(buffer, offset, buffer, 0, count);
				return count; // new count
			}

			var bodyLength = (int)requestBody.Length;

			if (count >= requestBody.Length)
			{
				// whole request body is already buffered

				requestBody.Write(buffer, offset, bodyLength);
				requestBody.Position = 0;

				Buffer.BlockCopy(buffer, offset + bodyLength, buffer, 0, count - bodyLength);
				return count - bodyLength; // new count
			}

			var toWrite = bodyLength - (int)requestBody.Position;
			int read;
			while (toWrite > 0 && (read = await requestStream.ReadAsync(buffer, 0, toWrite).ConfigureAwait(false)) > 0)
			{
				requestBody.Write(buffer, 0, read);
				toWrite = bodyLength - (int)requestBody.Position;
			}

			if (requestBody.Position != bodyLength)
			{
				throw new WebException("Connection has been closed before whole HTTP request body was received.", WebExceptionStatus.ReceiveFailure);
			}

			requestBody.Position = 0;
			return 0; // new count
		}
		private async Task WriteStatusCodeAsync(Socket requestSocket, byte[] buffer, HttpResponseMessage responseMessage)
		{
			{
				await using var responseStream = new NetworkStream(requestSocket, ownsSocket: false);
				await Http11Protocol.WriteResponseHeadAsync(
					responseStream, buffer,
					(int)responseMessage.StatusCode,
					responseMessage.StatusCode.ToString(),
					this.defaultHeaders,
					responseMessage.Headers,
					null,
					"0",
					transferEncoding: null,
					connectionHeader: "close",
					keepAliveHeader: null
				);
			}
			requestSocket.Shutdown(SocketShutdown.Both);
		}
		private static WebException ProtocolErrorWebException(HttpParseResult result)
		{
			return result switch {
				HttpParseResult.MalformedRequestLine => new WebException("Malformed request line.", WebExceptionStatus.ProtocolError),
				HttpParseResult.UnknownHttpVersion => new WebException("Unknown HTTP version specified.", WebExceptionStatus.ProtocolError),
				HttpParseResult.MissingMethod => new WebException("Missing method in request line.", WebExceptionStatus.ProtocolError),
				HttpParseResult.MissingRequestUrl => new WebException("Missing url in request line.", WebExceptionStatus.ProtocolError),
				HttpParseResult.MissingHttpVersion => new WebException("Missing HTTP version in request line.", WebExceptionStatus.ProtocolError),
				HttpParseResult.MissingHeaderValue => new WebException("One of header missing value.", WebExceptionStatus.ProtocolError),
				HttpParseResult.MissingHeaderValueEnd => new WebException("One of header missing value.", WebExceptionStatus.ProtocolError),
				HttpParseResult.MalformedHeader => new WebException("One of headers is malformed.", WebExceptionStatus.ProtocolError),
				HttpParseResult.EndOfHeaders => throw new ArgumentException("Invalid parse result value.", nameof(result)),
				HttpParseResult.ContinuedHeaderValue => throw new ArgumentException("Invalid parse result value.", nameof(result)),
				HttpParseResult.Ok => throw new ArgumentException("Invalid parse result value.", nameof(result)),
				_ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
			};
		}
		private static bool IsLastRequest(HttpRequestHeaders requestMessageHeaders, HttpResponseHeaders responseMessageHeaders)
		{
			if (requestMessageHeaders.Contains("Connection"))
			{
				foreach (var connectionOption in requestMessageHeaders.GetValues("Connection"))
				{
					if (string.Equals(connectionOption, "close", StringComparison.OrdinalIgnoreCase))
					{
						return true; // Connection: close in Request
					}
					else if (string.Equals(connectionOption, "upgrade", StringComparison.OrdinalIgnoreCase))
					{
						return true; // Connection: Upgrade in Request
					}
				}
			}

			if (responseMessageHeaders.Contains("Connection"))
			{
				foreach (var connectionOption in responseMessageHeaders.GetValues("Connection"))
				{
					if (string.Equals(connectionOption, "close", StringComparison.OrdinalIgnoreCase))
					{
						return true; // Connection: close in Response
					}
				}
			}


			return false;
		}

		private MemoryStream GetMemoryStream()
		{
			while (this.memoryStreams.TryTake(out var requestStream))
			{
				if (!requestStream.CanWrite || !requestStream.CanRead || !requestStream.CanSeek)
				{
					continue;
				}

				requestStream.SetLength(0);
				return requestStream;
			}

			return new MemoryStream();
		}
		private void ReturnMemoryStream(MemoryStream memoryStream)
		{
			if (memoryStream == null) throw new ArgumentNullException(nameof(memoryStream));

			if (!memoryStream.CanWrite || !memoryStream.CanRead || !memoryStream.CanSeek || this.memoryStreams.Count > 100)
			{
				memoryStream.Dispose();
				return;
			}

			memoryStream.SetLength(0);
			this.memoryStreams.Add(memoryStream);
		}

		private byte[] GetBuffer()
		{
			if (this.buffers.TryTake(out var buffer))
			{
				return buffer;
			}

			return new byte[DEFAULT_BUFFER_SIZE];
		}
		private void ReturnBuffer(byte[] buffer)
		{
			if (buffer == null) throw new ArgumentNullException(nameof(buffer));

			if (this.buffers.Count > 1000)
			{
				return;
			}

			Array.Clear(buffer, 0, buffer.Length);
			this.buffers.Add(buffer);
		}

		public void Dispose()
		{
			this.listener.Stop();
		}
	}
}
