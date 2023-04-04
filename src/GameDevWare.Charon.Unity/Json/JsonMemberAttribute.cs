using System;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.Json
{
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class JsonMemberAttribute : Attribute
	{
		public readonly string Name;

		public JsonMemberAttribute(string name)
		{
			this.Name = name;
		}
	}
}
