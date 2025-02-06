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

using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// The operation in progress reported by a <see cref="ZipTestResultHandler"/> during testing.
	/// </summary>
	/// <seealso cref="ZipFile.TestArchive(bool)">TestArchive</seealso>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal enum TestOperation
	{
		/// <summary>
		/// Setting up testing.
		/// </summary>
		Initialising,

		/// <summary>
		/// Testing an individual entries header
		/// </summary>
		EntryHeader,

		/// <summary>
		/// Testing an individual entries data
		/// </summary>
		EntryData,

		/// <summary>
		/// Testing an individual entry has completed.
		/// </summary>
		EntryComplete,

		/// <summary>
		/// Running miscellaneous tests
		/// </summary>
		MiscellaneousTests,

		/// <summary>
		/// Testing is complete
		/// </summary>
		Complete,
	}
}