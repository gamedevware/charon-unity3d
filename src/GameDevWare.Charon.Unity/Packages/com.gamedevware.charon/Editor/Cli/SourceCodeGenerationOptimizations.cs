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
using JetBrains.Annotations;

namespace GameDevWare.Charon.Editor.Cli
{
	/// <summary>
	/// Source code generation optimization for <see cref="CharonCli.GenerateCSharpCodeAsync"/> operation.
	/// See https://gamedevware.github.io/charon/advanced/commands/generate_csharp_code.html for detailed information.
	/// </summary>
	[Flags]
	[PublicAPI]
	public enum SourceCodeGenerationOptimizations
	{
		/// <summary>
		/// Enumeration value - eagerReferenceResolution.
		/// </summary>
		EagerReferenceResolution = 1 << 0,
		/// <summary>
		/// Enumeration value - rawReferences.
		/// </summary>
		RawReferences = 1 << 1,
		/// <summary>
		/// Enumeration value - rawLocalizedStrings.
		/// </summary>
		RawLocalizedStrings = 1 << 2,
		/// <summary>
		/// Enumeration value - disableStringPooling.
		/// </summary>
		DisableStringPooling = 1 << 3,
		/// <summary>
		/// Enumeration value - disableJsonSerialization.
		/// </summary>
		DisableJsonSerialization = 1 << 4,
		/// <summary>
		/// Enumeration value - disableMessagePackSerialization.
		/// </summary>
		DisableMessagePackSerialization = 1 << 5,
		/// <summary>
		/// Enumeration value - disablePatching.
		/// </summary>
		DisablePatching = 1 << 6,
		/// <summary>
		/// Enumeration value - disableFormulas.
		/// </summary>
		DisableFormulaCompilation = 1 << 7,
		/// <summary>
		/// Enumeration value - disableDocumentIdEnums.
		/// </summary>
		DisableDocumentIdEnums = 1 << 8,
	}
}
