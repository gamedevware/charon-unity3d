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

namespace GameDevWare.Charon.Editor.Cli
{
	/// <summary>
	/// Represents the logging level during the execution of FCharonCli operations.
	/// This enumeration defines the different levels of logging that can be set for the operations
	/// performed by FCharonCli. It allows control over the amount and detail of log information
	/// produced during these operations.
	/// </summary>
	public enum CharonLogLevel
	{
		/// <summary>
		/// Indicates no logging, except for fatal errors.
		///
		/// When set to None, the logging system will only output messages that are classified as fatal errors,
		/// which are typically those that cause the operation to terminate unexpectedly.
		/// </summary>
		None,

		/// <summary>
		/// Indicates normal logging level with informational messages.
		///
		/// In this mode, the logging system outputs regular information messages. This is typically used for
		/// standard operational logging where routine information is logged.
		/// </summary>
		Normal,

		/// <summary>
		/// Indicates verbose logging with detailed messages.
		///
		/// When set to Verbose, the logging system produces a detailed log output, including debug messages.
		/// This level is typically used for troubleshooting and debugging purposes, as it provides in-depth
		/// details about the operation's progress and state.
		/// </summary>
		Verbose,
	};
}
