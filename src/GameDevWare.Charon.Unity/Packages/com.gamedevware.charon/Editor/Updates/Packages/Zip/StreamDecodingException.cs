/*
	Copyright © 2000-2018 SharpZipLib Contributors

	Permission is hereby granted, free of charge, to any person obtaining a copy of this
	software and associated documentation files (the "Software"), to deal in the Software
	without restriction, including without limitation the rights to use, copy, modify, merge,
	publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
	to whom the Software is furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all copies or
	substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
	INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
	PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
	FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
	OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
	DEALINGS IN THE SOFTWARE.
 */

using System;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// Indicates that an error occured during decoding of a input stream due to corrupt
	/// data or (unintentional) library incompability.
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class StreamDecodingException : SharpZipBaseException
	{
		private const string GenericMessage = "Input stream could not be decoded";

		/// <summary>
		/// Initializes a new instance of the StreamDecodingException with a generic message
		/// </summary>
		public StreamDecodingException() : base(GenericMessage) { }

		/// <summary>
		/// Initializes a new instance of the StreamDecodingException class with a specified error message.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		public StreamDecodingException(string message) : base(message) { }

		/// <summary>
		/// Initializes a new instance of the StreamDecodingException class with a specified
		/// error message and a reference to the inner exception that is the cause of this exception.
		/// </summary>
		/// <param name="message">A message describing the exception.</param>
		/// <param name="innerException">The inner exception</param>
		public StreamDecodingException(string message, Exception innerException) : base(message, innerException) { }
	}
}
