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
using JetBrains.Annotations;

namespace GameDevWare.Charon.Editor.Cli
{
	/// <summary>
	/// Document validation options for <see cref="CharonCli.ValidateAsync"/> operation.
	/// See https://gamedevware.github.io/charon/advanced/commands/data_validate.html for detailed information.
	/// </summary>
	[Flags]
	[PublicAPI]
	public enum ValidationOptions
	{
		None = 0x0,
		Repair = 0x1 << 0,
		CheckTranslation = 0x1 << 1,
		DeduplicateIds = 0x1 << 2,
		RepairRequiredWithDefaultValue = 0x1 << 3,
		EraseInvalidValue = 0x1 << 4,
		CheckRequirements = 0x1 << 5,
		CheckFormat = 0x1 << 6,
		CheckUniqueness = 0x1 << 7,
		CheckReferences = 0x1 << 8,
		CheckSpecification = 0x1 << 9,
		CheckConstraints = 0x1 << 10,

		AllIntegrityChecks = CheckRequirements | CheckFormat | CheckUniqueness | CheckReferences | CheckSpecification | CheckConstraints,
		Default = Repair | DeduplicateIds | RepairRequiredWithDefaultValue | EraseInvalidValue | CheckRequirements | CheckFormat | CheckUniqueness | CheckReferences | CheckConstraints,
		DefaultForCreate = Repair | RepairRequiredWithDefaultValue | AllIntegrityChecks,
		DefaultForUpdate = DefaultForCreate,
		DefaultForBulkChange = Repair | DeduplicateIds | RepairRequiredWithDefaultValue,
		DefaultForInternalChange = Repair | DeduplicateIds | RepairRequiredWithDefaultValue,
		DefaultForBackgroundValidation = AllIntegrityChecks | CheckTranslation
	}
}
