/*
	Copyright (c) 2025 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see http://www.gnu.org/licenses.
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
