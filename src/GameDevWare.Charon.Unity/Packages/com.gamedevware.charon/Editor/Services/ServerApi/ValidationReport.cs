﻿/*
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
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace GameDevWare.Charon.Editor.Services.ServerApi
{
	[Serializable, PublicAPI]
	public class ValidationReport
	{
		[DataMember(Name = "records")]
		public ValidationRecord[] Records;
		[DataMember(Name = "metadataHash")]
		public string MetadataHash;
		[DataMember(Name = "revisionHash")]
		public string RevisionHash;

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
