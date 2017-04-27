using System;

namespace GameDevWare.Charon.Utils
{
	[Flags]
	public enum ValidationOptions
	{
		None = 0x0,
		Default = Repair | DeduplicateIds | RepairRequiredWithDefaultValue | EraseInvalidValue,
		Repair = 0x1 << 0,
		CheckTranslation = 0x1 << 1,
		DeduplicateIds = 0x1 << 2,
		RepairRequiredWithDefaultValue = 0x1 << 3,
		EraseInvalidValue = 0x1 << 4
	}
}