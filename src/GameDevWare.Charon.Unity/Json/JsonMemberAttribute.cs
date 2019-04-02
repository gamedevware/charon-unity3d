using System;

namespace GameDevWare.Charon.Unity.Json
{
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
