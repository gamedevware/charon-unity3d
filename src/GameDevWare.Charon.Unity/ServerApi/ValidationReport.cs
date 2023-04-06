using System;
using System.Linq;
using GameDevWare.Charon.Unity.Json;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[Serializable, UsedImplicitly(ImplicitUseTargetFlags.WithMembers), PublicAPI]
	public class ValidationReport
	{
		[JsonMember("records")]
		public ValidationRecord[] Records;

		public bool HasErrors
		{
			get { return this.Records != null && this.Records.Length > 0 && this.Records.Any(r => !r.HasErrors); }
		}

		public static ValidationReport CreateErrorReport(string message)
		{
			return new ValidationReport
			{
				Records = new ValidationRecord[] {
					new ValidationRecord {
						Errors = new ValidationError[] {
							new ValidationError {
								Message = message
							}
						}
					}
				}
			};
		}
	}
}
