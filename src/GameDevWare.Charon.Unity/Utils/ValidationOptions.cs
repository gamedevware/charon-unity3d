/*
	Copyright (c) 2023 Denis Zykov

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

using System;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Utils
{
	[Flags]
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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