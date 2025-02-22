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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Editor.Services.Http
{
	internal class Http11Protocol
	{
		// ReSharper disable InconsistentNaming
		private static readonly HttpContentHeaders EmptyContentHeader = new StreamContent(Stream.Null).Headers;
		private static readonly Dictionary<string, HttpMethod> KnownMethods;

		private static readonly byte[] CRLFCRLF = { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };
		private static readonly byte[] CRLF = { (byte)'\r', (byte)'\n' };
		private static readonly byte[] SP = { (byte)' ' };
		private static readonly byte[] HS = { (byte)':' };
		// ReSharper restore InconsistentNaming

		static Http11Protocol()
		{
			KnownMethods = new Dictionary<string, HttpMethod>(StringComparer.OrdinalIgnoreCase) {
				{ HttpMethod.Delete.Method, HttpMethod.Delete },
				{ HttpMethod.Get.Method, HttpMethod.Get },
				{ HttpMethod.Head.Method, HttpMethod.Head },
				{ HttpMethod.Options.Method, HttpMethod.Options },
				{ HttpMethod.Post.Method, HttpMethod.Post },
				{ HttpMethod.Put.Method, HttpMethod.Put },
				{ HttpMethod.Trace.Method, HttpMethod.Trace },
			};
		}

		public static HttpMethod GetHttpMethod(string methodName)
		{
			if (!KnownMethods.TryGetValue(methodName, out var httpMethod))
			{
				httpMethod = new HttpMethod(methodName);
			}
			return httpMethod;
		}

		public static int GetHeadersEndIndex(Span<byte> requestLineBuffer, bool skipStartingNewLines, int scanOffset = 0)
		{
			const int CRLF_CRLF_LENGTH = 4;

			var offset = 0;
			while (skipStartingNewLines && TryConsume(ref requestLineBuffer, CRLF))
			{
				/* In the interest of robustness, servers SHOULD ignore any empty
				line(s) received where a Request-Line is expected. In other words, if
				the server is reading the protocol stream at the beginning of a
				message and receives a CRLF first, it should ignore the CRLF.*/
				offset += 2 /* CRLF */;
			}

			if (scanOffset > offset + CRLF_CRLF_LENGTH)
			{
				requestLineBuffer = requestLineBuffer.Slice(scanOffset - offset - CRLF_CRLF_LENGTH);
				offset = scanOffset - CRLF_CRLF_LENGTH;
			}

			var lastHeadersIndex = Find(requestLineBuffer, CRLFCRLF);
			if (lastHeadersIndex < 0)
			{
				return lastHeadersIndex;
			}
			else
			{
				return offset + lastHeadersIndex + CRLF_CRLF_LENGTH;
			}
		}

		public static HttpParseResult ParseHttpRequestLine(
			ref Span<byte> requestLine,
			out Version httpVersion,
			out string method,
			out string requestUrl
		)
		{
			httpVersion = HttpVersion.Unknown;
			method = null;
			requestUrl = null;

			while (TryConsume(ref requestLine, CRLF))
			{
				/* In the interest of robustness, servers SHOULD ignore any empty
				line(s) received where a Request-Line is expected. In other words, if
				the server is reading the protocol stream at the beginning of a
				message and receives a CRLF first, it should ignore the CRLF.*/
			}

			var end = FindAny(requestLine, SP);
			if (end < 0)
			{
				return HttpParseResult.MissingMethod;
			}

			// parse method
			method = Encoding.ASCII.GetString(requestLine.Slice(0, end));

			requestLine = requestLine.Slice(end);

			if (TryConsume(ref requestLine, SP) == false)
			{
				return HttpParseResult.MalformedRequestLine;
			}

			// parse request url
			end = FindAny(requestLine, SP);
			if (end < 0)
			{
				return HttpParseResult.MissingRequestUrl;
			}

			requestUrl = Encoding.ASCII.GetString(requestLine.Slice(0, end));
			requestLine = requestLine.Slice(end);

			if (TryConsume(ref requestLine, SP) == false)
			{
				return HttpParseResult.MalformedRequestLine;
			}

			// parse http version
			end = Find(requestLine, CRLF);
			if (end < 0)
			{
				return HttpParseResult.MissingHttpVersion;
			}

			httpVersion = Encoding.ASCII.GetString(requestLine.Slice(0, end)) switch
			{
				"HTTP/1.0" => HttpVersion.Version10,
				"HTTP/1.1" => HttpVersion.Version11,
				_ => null
			};
			requestLine = requestLine.Slice(end);

			if (httpVersion == null)
			{
				return HttpParseResult.UnknownHttpVersion;
			}

			if (TryConsume(ref requestLine, CRLF) == false)
			{
				return HttpParseResult.MalformedRequestLine;
			}

			return HttpParseResult.Ok;
		}

		public static HttpParseResult ParseHttpHeader(ref Span<byte> headerLine, ref string headerName, ref string headerValue)
		{

			var result = PeekHttpHeader(ref headerLine, out var headerNameSequence, out var headerValueSequence);
			if (!headerNameSequence.IsEmpty)
			{
				headerName = Encoding.ASCII.GetString(headerNameSequence);

				if (string.IsNullOrEmpty(headerName))
				{
					return HttpParseResult.MalformedHeader;
				}
			}

			headerValue = null;

			if (!headerValueSequence.IsEmpty)
			{
				headerValue = Encoding.ASCII.GetString(headerValueSequence);
			}
			return result;
		}
		private static HttpParseResult PeekHttpHeader(ref Span<byte> headerLine, out Span<byte> headerNameSequence, out Span<byte> headerValueSequence)
		{
			headerNameSequence = default;
			headerValueSequence = default;

			if (TryConsume(ref headerLine, CRLF))
			{
				return HttpParseResult.EndOfHeaders;
			}

			var isContinuation = TryConsumeAnyWs(ref headerLine);

			var end = 0;
			if (!isContinuation)
			{
				end = FindAny(headerLine, HS);
				if (end < 0)
				{
					return HttpParseResult.MissingHeaderValue;
				}

				// parse header name
				headerNameSequence = headerLine.Slice(0, end);
				TrimWs(ref headerNameSequence);
				headerLine = headerLine.Slice(end + 1);
			}

			end = Find(headerLine, CRLF);
			if (end < 0)
			{
				return HttpParseResult.MissingHeaderValueEnd;
			}

			headerValueSequence = headerLine.Slice(0, end);
			TrimWs(ref headerValueSequence);
			headerLine = headerLine.Slice(end + 2);

			return isContinuation ? HttpParseResult.ContinuedHeaderValue : HttpParseResult.Ok;
		}

		// ReSharper disable once FunctionComplexityOverflow
		public static async Task WriteResponseHeadAsync
		(
			Stream responseStream,
			byte[] buffer,
			int statusCode,
			string statusDescription,
			HttpResponseHeaders defaultResponseHeaders,
			HttpResponseHeaders responseHeaders,
			HttpContentHeaders responseContentHeaders,
			string contentLength,
			string transferEncoding,
			string connectionHeader,
			string keepAliveHeader
		)
		{
			if (statusDescription == null) throw new ArgumentNullException(nameof(statusDescription));
			if (responseHeaders == null) throw new ArgumentNullException(nameof(responseHeaders));

			var bufferOffset = 0;

			PutHttpVersion(HttpVersion.Version11, buffer, ref bufferOffset); // HTTP/1.1
			buffer[bufferOffset++] = (byte)' '; // SP
			PutResponseStatusCode(statusCode, buffer, ref bufferOffset); // status code
			buffer[bufferOffset++] = (byte)' '; // SP
			foreach (var reasonChar in statusDescription)
			{
				if (buffer.Length - bufferOffset < 8)
				{
					await responseStream.WriteAsync(buffer, 0, bufferOffset).ConfigureAwait(false);
					bufferOffset = 0;
				}

				buffer[bufferOffset++] = (byte)reasonChar; // reason phrase
			}

			buffer[bufferOffset++] = (byte)'\r'; // CR
			buffer[bufferOffset++] = (byte)'\n'; // LF

			// write content length
			if (!string.IsNullOrEmpty(contentLength))
			{
				if (buffer.Length - bufferOffset < "Content-Length".Length + contentLength.Length + 8)
				{
					await responseStream.WriteAsync(buffer, 0, bufferOffset).ConfigureAwait(false);
					bufferOffset = 0;
				}
				foreach (var headerNameChar in "Content-Length")
				{
					buffer[bufferOffset++] = (byte)headerNameChar; // header name
				}

				buffer[bufferOffset++] = (byte)':'; // :
				buffer[bufferOffset++] = (byte)' '; // SP
				foreach (var headerValueChar in contentLength)
					buffer[bufferOffset++] = (byte)headerValueChar; // header name
				buffer[bufferOffset++] = (byte)'\r'; // CR
				buffer[bufferOffset++] = (byte)'\n'; // LF
			}

			// write connection
			if (!string.IsNullOrEmpty(connectionHeader))
			{
				if (buffer.Length - bufferOffset < "Connection".Length + connectionHeader.Length + 8)
				{
					await responseStream.WriteAsync(buffer, 0, bufferOffset).ConfigureAwait(false);
					bufferOffset = 0;
				}

				foreach (var headerNameChar in "Connection")
				{
					buffer[bufferOffset++] = (byte)headerNameChar; // header name
				}

				buffer[bufferOffset++] = (byte)':'; // :
				buffer[bufferOffset++] = (byte)' '; // SP
				foreach (var headerValueChar in connectionHeader)
					buffer[bufferOffset++] = (byte)headerValueChar; // header name
				buffer[bufferOffset++] = (byte)'\r'; // CR
				buffer[bufferOffset++] = (byte)'\n'; // LF

				// write keep alive header
				if (!string.IsNullOrEmpty(keepAliveHeader))
				{
					if (buffer.Length - bufferOffset < "Keep-Alive".Length + keepAliveHeader.Length + 10)
					{
						await responseStream.WriteAsync(buffer, 0, bufferOffset).ConfigureAwait(false);
						bufferOffset = 0;
					}

					foreach (var headerNameChar in "Keep-Alive")
					{
						buffer[bufferOffset++] = (byte)headerNameChar; // header name
					}

					buffer[bufferOffset++] = (byte)':'; // :
					buffer[bufferOffset++] = (byte)' '; // SP
					foreach (var headerValueChar in keepAliveHeader)
						buffer[bufferOffset++] = (byte)headerValueChar; // header name
					buffer[bufferOffset++] = (byte)'\r'; // CR
					buffer[bufferOffset++] = (byte)'\n'; // LF
				}
			}

			if (!string.IsNullOrEmpty(transferEncoding))
			{
				if (buffer.Length - bufferOffset < "Transfer-Encoding".Length + transferEncoding.Length + 8)
				{
					await responseStream.WriteAsync(buffer, 0, bufferOffset).ConfigureAwait(false);
					bufferOffset = 0;
				}

				foreach (var headerNameChar in "Transfer-Encoding")
				{
					buffer[bufferOffset++] = (byte)headerNameChar; // header name
				}

				buffer[bufferOffset++] = (byte)':'; // :
				buffer[bufferOffset++] = (byte)' '; // SP
				foreach (var headerValueChar in transferEncoding)
					buffer[bufferOffset++] = (byte)headerValueChar; // header name
				buffer[bufferOffset++] = (byte)'\r'; // CR
				buffer[bufferOffset++] = (byte)'\n'; // LF
			}

			// write rest of response headers
			var allHeaders = responseHeaders
				.Concat(defaultResponseHeaders.Where(h => !responseHeaders.Contains(h.Key)))
				.Concat(responseContentHeaders ?? EmptyContentHeader);

			foreach (var header in allHeaders)
			{
				var headerName = header.Key;
				var headerValues = header.Value;
				if (headerValues == null)
					continue;

				if (headerName.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase) ||
					headerName.Equals("TE", StringComparison.OrdinalIgnoreCase) ||
					headerName.Equals("Trailer", StringComparison.OrdinalIgnoreCase) ||
					headerName.Equals("Upgrade", StringComparison.OrdinalIgnoreCase) ||
					headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
					headerName.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
					headerName.Equals("Proxy-Authenticate", StringComparison.OrdinalIgnoreCase) ||
					headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
					headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				foreach (var headerNameChar in headerName)
				{
					if (buffer.Length - bufferOffset <  8)
					{
						await responseStream.WriteAsync(buffer, 0, bufferOffset).ConfigureAwait(false);
						bufferOffset = 0;
					}

					if (headerNameChar == '\r' || headerNameChar == '\n')
						buffer[bufferOffset++] = (byte)'_'; // replace invalid chars with underscore
					else
						buffer[bufferOffset++] = (byte)headerNameChar; // header name
				}

				buffer[bufferOffset++] = (byte)':'; // :
				buffer[bufferOffset++] = (byte)' '; // SP

				foreach (var headerValue in headerValues)
				{
					if (headerValue == null)
					{
						continue;
					}

					foreach (var headerValueChar in headerValue)
					{
						if (buffer.Length - bufferOffset <  8)
						{
							await responseStream.WriteAsync(buffer, 0, bufferOffset).ConfigureAwait(false);
							bufferOffset = 0;
						}

						if (headerValueChar == '\r' || headerValueChar == '\n')
							buffer[bufferOffset++] = (byte)' '; // replace invalid chars with space
						else
							buffer[bufferOffset++] = (byte)headerValueChar; // header value
					}

					buffer[bufferOffset++] = (byte)','; // SEP
				}

				bufferOffset--;
				buffer[bufferOffset++] = (byte)'\r'; // CR
				buffer[bufferOffset++] = (byte)'\n'; // LF
			}

			if (buffer.Length - bufferOffset <  2)
			{
				await responseStream.WriteAsync(buffer, 0, bufferOffset).ConfigureAwait(false);
				bufferOffset = 0;
			}

			buffer[bufferOffset++] = (byte)'\r'; // CR
			buffer[bufferOffset++] = (byte)'\n'; // LF

			if (bufferOffset > 0)
			{
				await responseStream.WriteAsync(buffer, 0, bufferOffset).ConfigureAwait(false);
				bufferOffset = 0;
			}

			await responseStream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
		}

		private static void PutHttpVersion(Version protocolVersion, byte[] buffer, ref int offset)
		{
			Debug.Assert(offset >= 0 && offset + 8 < buffer.Length, "offset >= 0 && offset + 8 < buffer.Length");

			if (protocolVersion == HttpVersion.Version10)
			{
				foreach (var charValue in "HTTP/1.0")
				{
					buffer[offset++] = (byte)charValue;
				}
			}
			if (protocolVersion == HttpVersion.Version11)
			{
				foreach (var charValue in "HTTP/1.1")
				{
					buffer[offset++] = (byte)charValue;
				}
			}
			else
			{
				throw new ArgumentException($"Unknown protocol version '{protocolVersion}' is specified.");
			}
		}
		private static void PutResponseStatusCode(int statusCode, byte[] buffer, ref int offset)
		{
			Debug.Assert(offset >= 0 && offset + 3 < buffer.Length, "offset >= 0 && offset + 3 < buffer.Length");
			Debug.Assert(statusCode >= 0, "statusCode >= 0");

			const byte ZERO = (byte)'0';

			var idx = offset;

			// Take care of sign
			var unsignedValue = (uint)statusCode;

			// Conversion. Number is reversed.
			do
			{
				buffer[idx++] = (byte)(ZERO + unsignedValue % 10);
			} while ((unsignedValue /= 10) != 0);

			var length = idx - offset;

			// Reverse string
			buffer.AsSpan(offset, length).Reverse();

			offset += length;
		}

		private static bool TryConsume(ref Span<byte> bufferSequence, byte[] tokenToConsume)
		{
			if (tokenToConsume == null) throw new ArgumentNullException(nameof(tokenToConsume));

			var offset = 0;
			foreach (var bufferByte in bufferSequence)
			{
				if (offset >= tokenToConsume.Length)
				{
					break;
				}
				else if (tokenToConsume[offset] != bufferByte)
				{
					return false;
				}
				else
				{
					offset++;
				}
			}

			if (offset == tokenToConsume.Length)
			{
				bufferSequence = bufferSequence.Slice(tokenToConsume.Length);
				return true;
			}
			else
			{
				return false;
			}
		}
		private static bool TryConsumeAnyWs(ref Span<byte> bufferSequence)
		{
			var offset = 0;
			foreach (var bufferByte in bufferSequence)
			{
				if (IsMatchingWs(bufferByte))
				{
					offset++;
					continue;
				}

				goto slice;
			}

			slice:
			if (offset > 0)
			{
				bufferSequence = bufferSequence.Slice(offset);
			}

			return offset > 0;
		}

		private static int FindAny(in Span<byte> bufferSequence, byte[] tokensToFind)
		{
			return bufferSequence.IndexOfAny(tokensToFind);
		}
		private static int Find(in Span<byte> bufferSequence, byte[] tokensToFind)
		{
			var sequenceMatch = 0;
			for (var i = 0; i < bufferSequence.Length; i++)
			{
				var byteValue = bufferSequence[i];
				if (byteValue != tokensToFind[sequenceMatch])
				{
					sequenceMatch = 0;
				}
				else
				{
					sequenceMatch++;

					if (sequenceMatch >= tokensToFind.Length)
					{
						return i - tokensToFind.Length + 1;
					}
				}
			}
			return -1;
		}

		private static void TrimWs(ref Span<byte> bufferSequence)
		{
			const int PART_START = 0;
			const int PART_BODY = 1;
			const int PART_END = 2;

			var part = PART_START;
			var start = 0;
			var end = 0;
			var i = -1;

			foreach (var bufferByte in bufferSequence)
			{
				i++;
				// ReSharper disable once SwitchStatementMissingSomeCases
				switch (part)
				{
					case PART_START:
						if (IsMatchingWs(bufferByte))
						{
							start++;
							end = start + 1;
						}
						else
						{
							part = PART_BODY;
							end = i + 1;
						}

						break;
					case PART_BODY:
						if (IsMatchingWs(bufferByte))
						{
							part = PART_END;
							end = i;
						}
						else
						{
							end = i + 1;
						}

						break;
					case PART_END:
						if (!IsMatchingWs(bufferByte))
						{
							part = PART_BODY;
							end = i + 1;
						}

						break;
				}
			}

			if (start > 0 || end != bufferSequence.Length)
			{
				bufferSequence = bufferSequence.Slice(start, end - start);
			}
		}
		private static bool IsMatchingWs(byte value)
		{
			return value == ' ' || value == '\t';
		}
	}
}
