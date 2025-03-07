/*
	Copyright (c) 2025 GameDevWare, Denis Zykov

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Editor.Services.ServerApi
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	internal class ApiResponse<T>
	{
		[DataMember(Name = "result")]
		public T Result;

		[DataMember(Name = "errors")]
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
