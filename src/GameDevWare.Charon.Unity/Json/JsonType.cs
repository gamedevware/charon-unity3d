using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Json
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal enum JsonType
	{
		String,
		Number,
		Object,
		Array,
		Boolean
	}
}
