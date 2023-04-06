using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public enum SourceCodeIndentation
	{
		Tabs = 0,
		TwoSpaces,
		FourSpaces
	}
}