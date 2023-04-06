/*
	Copyright (c) 2023 Denis Zykov

	This is part of "Charon: Game Data Editor" Unity Plugin.

	Charon Game Data Editor Unity Plugin is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see http://www.gnu.org/licenses.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using GameDevWare.Charon.Unity.Utils;

namespace GameDevWare.Charon.Unity.Async
{
	[Serializable, DebuggerDisplay("Count = {InnerExceptions.Count}")]
	internal class AggregateException : Exception
	{
		private const string DEFAULT_MESSAGE = "One or more error occured";
		public AggregateException()
			: base(DEFAULT_MESSAGE)
		{
			this.InnerExceptions = new ReadOnlyCollection<Exception>(new Exception[0]);
		}
		public AggregateException(IEnumerable<Exception> innerExceptions)
			: this(DEFAULT_MESSAGE, innerExceptions)
		{
		}
		public AggregateException(params Exception[] innerExceptions)
			: this(DEFAULT_MESSAGE, innerExceptions)
		{
		}
		public AggregateException(string message)
			: base(message)
		{
			this.InnerExceptions = new ReadOnlyCollection<Exception>(new Exception[0]);
		}
		protected AggregateException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			var list = info.GetValue("InnerExceptions", typeof(Exception[])) as Exception[];
			if (list == null)
				throw new SerializationException("DeserializationFailure");
			this.InnerExceptions = new ReadOnlyCollection<Exception>(list);
		}
		public AggregateException(string message, IEnumerable<Exception> innerExceptions)
			: this(message, (innerExceptions as IList<Exception>) ?? ((innerExceptions == null) ? null : new List<Exception>(innerExceptions)))
		{
		}
		public AggregateException(string message, params Exception[] innerExceptions)
			: this(message, (IList<Exception>)innerExceptions)
		{
		}
		public AggregateException(string message, Exception innerException)
			: base(message, innerException)
		{
			if (innerException == null) throw new ArgumentNullException("innerException");

			this.InnerExceptions = new ReadOnlyCollection<Exception>(new[] { innerException });
		}
		private AggregateException(string message, IList<Exception> innerExceptions)
			: base(message, ((innerExceptions != null) && (innerExceptions.Count > 0)) ? innerExceptions[0] : null)
		{
			if (innerExceptions == null) throw new ArgumentNullException("innerExceptions");

			var list = new Exception[innerExceptions.Count];
			for (var i = 0; i < list.Length; i++)
			{
				list[i] = innerExceptions[i];
				if (list[i] == null)
					throw new ArgumentException("Inner exception is null");
			}
			this.InnerExceptions = new ReadOnlyCollection<Exception>(list);
		}
		public ReadOnlyCollection<Exception> InnerExceptions { get; private set; }
		public AggregateException Flatten()
		{
			var innerExceptions = new List<Exception>();
			var procList = new List<AggregateException> { this };
			var procListIdx = 0;
			while (procList.Count > procListIdx)
			{
				var list3 = procList[procListIdx++].InnerExceptions;
				for (var i = 0; i < list3.Count; i++)
				{
					var item = list3[i];
					if (item != null)
					{
						var aggException = item as AggregateException;
						if (aggException != null)
							procList.Add(aggException);
						else
							innerExceptions.Add(item);
					}
				}
			}
			return new AggregateException(this.Message, innerExceptions);
		}
		public override Exception GetBaseException()
		{
			Exception innerException = this;
			for (var aggrException = this; (aggrException != null) && (aggrException.InnerExceptions.Count == 1); aggrException = innerException as AggregateException)
				innerException = innerException.InnerException;
			return innerException;
		}
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null) throw new ArgumentNullException("info");

			base.GetObjectData(info, context);
			var array = new Exception[this.InnerExceptions.Count];
			this.InnerExceptions.CopyTo(array, 0);
			info.AddValue("InnerExceptions", array, typeof(Exception[]));
		}
		public void Handle(Predicate<Exception> predicate)
		{
			if (predicate == null) throw new ArgumentNullException("predicate");

			var innerExceptions = default(List<Exception>);
			for (var i = 0; i < this.InnerExceptions.Count; i++)
			{
				if (!predicate(this.InnerExceptions[i]))
				{
					if (innerExceptions == null)
						innerExceptions = new List<Exception>();
					innerExceptions.Add(this.InnerExceptions[i]);
				}
			}
			if (innerExceptions != null)
				throw new AggregateException(this.Message, innerExceptions);
		}
		public Exception Unwrap()
		{
			return ExceptionUtils.Unwrap(this);
		}

		public override string ToString()
		{
			var str = base.ToString();
			for (var i = 0; i < this.InnerExceptions.Count; i++)
				str = string.Format(CultureInfo.InvariantCulture, "{0}{1}---> (Inner Exception #{2}) {3}{4}{5}", str, Environment.NewLine, i, this.InnerExceptions[i], "<---", Environment.NewLine);
			return str;
		}
	}
}
