using System;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// Indicates that a value was outside of the expected range when decoding an input stream
	/// </summary>
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
