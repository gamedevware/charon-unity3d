/*
	Copyright (c) 2025 Denis Zykov

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
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Editor.ServerApi
{
	[Serializable, UsedImplicitly(ImplicitUseTargetFlags.WithMembers), PublicAPI]
	public class ValidationRecord
	{
		[DataMember(Name = "id")]
		public object Id;
		[DataMember(Name = "entityName"), Obsolete]
		public string EntityName;
		[DataMember(Name = "entityId"), Obsolete]
		public string EntityId;
		[DataMember(Name = "schemaName")]
		public string SchemaName;
		[DataMember(Name = "schemaId")]
		public string SchemaId;
		[DataMember(Name = "errors")]
		public ValidationError[] Errors;

		public bool HasErrors => this.Errors == null || this.Errors.Length == 0;
	}
}
