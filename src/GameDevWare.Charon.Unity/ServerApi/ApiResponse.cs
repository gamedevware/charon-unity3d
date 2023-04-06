using System;
using System.Linq;
using System.Net;
using GameDevWare.Charon.Unity.Json;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Unity.ServerApi
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class ApiResponse<T>
	{
		[JsonMember("result")]
		public T Result;

		[JsonMember("errors")]
		public ApiError[] Errors;

		public T GetResponseResultOrError()
		{
			if (this.Errors != null)
			{
				throw new WebException(Resources.UI_UNITYPLUGIN_SERVER_ERROR +
					Environment.NewLine + string.Join(Environment.NewLine, this.Errors.Select(error => error.ToString()).ToArray()),
					WebExceptionStatus.UnknownError);
			}

			return this.Result;
		}
	}
}
