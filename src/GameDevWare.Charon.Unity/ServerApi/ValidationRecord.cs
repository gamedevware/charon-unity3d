using System;
using GameDevWare.Charon.Unity.Json;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[Serializable, UsedImplicitly(ImplicitUseTargetFlags.WithMembers), PublicAPI]
	public class ValidationRecord
	{
		[JsonMember("id")]
		public object Id;
		[JsonMember("entityName")]
		public string EntityName;
		[JsonMember("entityId")]
		public string EntityId;
		[JsonMember("errors")]
		public ValidationError[] Errors;

		public bool HasErrors
		{
			get { return this.Errors == null || this.Errors.Length == 0; }
		}
	}
}