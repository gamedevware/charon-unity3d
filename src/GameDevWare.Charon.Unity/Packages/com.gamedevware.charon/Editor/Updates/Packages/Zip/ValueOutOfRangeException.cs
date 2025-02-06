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
	/// Indicates that a value was outside of the expected range when decoding an input stream
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class ValueOutOfRangeException : StreamDecodingException
	{
		/// <summary>
		/// Initializes a new instance of the ValueOutOfRangeException class naming the the causing variable
		/// </summary>
		/// <param name="nameOfValue">Name of the variable, use: nameof()</param>
		public ValueOutOfRangeException(string nameOfValue)
			: base(string.Format("{0} out of range", nameOfValue)) { }

		/// <summary>
		/// Initializes a new instance of the ValueOutOfRangeException class naming the the causing variable,
		/// it's current value and expected range.
		/// </summary>
		/// <param name="nameOfValue">Name of the variable, use: nameof()</param>
		/// <param name="value">The invalid value</param>
		/// <param name="maxValue">Expected maximum value</param>
		/// <param name="minValue">Expected minimum value</param>
		public ValueOutOfRangeException(string nameOfValue, long value, long maxValue, long minValue = 0)
			: this(nameOfValue, value.ToString(), maxValue.ToString(), minValue.ToString()) { }

		/// <summary>
		/// Initializes a new instance of the ValueOutOfRangeException class naming the the causing variable,
		/// it's current value and expected range.
		/// </summary>
		/// <param name="nameOfValue">Name of the variable, use: nameof()</param>
		/// <param name="value">The invalid value</param>
		/// <param name="maxValue">Expected maximum value</param>
		/// <param name="minValue">Expected minimum value</param>
		public ValueOutOfRangeException(string nameOfValue, string value, string maxValue, string minValue = "0") :
			base(string.Format("{0} out of range: {1}, should be {2}..{3}", nameOfValue, value, minValue, maxValue))
		{ }

		private ValueOutOfRangeException()
		{
		}

		private ValueOutOfRangeException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
