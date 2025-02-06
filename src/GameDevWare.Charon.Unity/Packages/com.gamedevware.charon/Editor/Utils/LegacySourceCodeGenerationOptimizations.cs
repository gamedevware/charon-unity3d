using System;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Utils
{
	[Flags]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal enum LegacySourceCodeGenerationOptimizations
	{
		LazyReferences = 0x1 << 0,

		HideReferences = 0x1 << 1,
		HideLocalizedStrings = 0x1 << 2,

		SuppressDocumentClass = 0x1 << 4,
		SuppressApiClass = 0x1 << 5,
		SuppressCollectionClass = 0x1 << 6,
		SuppressLocalizedStringClass = 0x1 << 7,
		SuppressReferenceClass = 0x1 << 8,
		SuppressDataContractAttributes = 0x1 << 9,
		
		DisableFormulas = 0x1 << 10,

		DisableJsonSerialization = 0x1 << 11,
		DisableMessagePackSerialization = 0x1 << 12,
		DisableBsonSerialization = 0x1 << 13,
		DisableXmlSerialization = 0x1 << 14,
		DisablePatching = 0x1 << 15,
	}

}
