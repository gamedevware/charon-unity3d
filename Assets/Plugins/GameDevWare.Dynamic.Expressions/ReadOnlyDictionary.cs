/*
	Copyright (c) 2015 Denis Zykov

	This is part of Charon Game Data Editor Unity Plugin.

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
using System.Collections;
using System.Collections.Generic;

namespace GameDevWare.Dynamic.Expressions
{
	public class ReadOnlyDictionary<KeyT, ValueT> : IDictionary<KeyT, ValueT>
	{
		private static readonly IDictionary<KeyT, ValueT> EmptyDictionary = new Dictionary<KeyT, ValueT>();
		public static readonly ReadOnlyDictionary<KeyT, ValueT> Empty = new ReadOnlyDictionary<KeyT, ValueT>();

		private readonly IDictionary<KeyT, ValueT> innerDictionary;

		public ReadOnlyDictionary()
		{
			this.innerDictionary = EmptyDictionary;
		}

		public ReadOnlyDictionary(IDictionary<KeyT, ValueT> innerDictionary)
		{
			if (innerDictionary == null)
				throw new ArgumentNullException("innerDictionary");

			this.innerDictionary = innerDictionary;
		}

		#region IDictionary<KeyT,ValueT> Members

		void IDictionary<KeyT, ValueT>.Add(KeyT key, ValueT value)
		{
			throw new NotSupportedException();
		}

		public bool ContainsKey(KeyT key)
		{
			return this.innerDictionary.ContainsKey(key);
		}

		public ICollection<KeyT> Keys
		{
			get { return this.innerDictionary.Keys; }
		}

		bool IDictionary<KeyT, ValueT>.Remove(KeyT key)
		{
			throw new NotSupportedException();
		}

		public bool TryGetValue(KeyT key, out ValueT value)
		{
			return this.innerDictionary.TryGetValue(key, out value);
		}

		public ICollection<ValueT> Values
		{
			get { return this.innerDictionary.Values; }
		}

		public ValueT this[KeyT key]
		{
			get { return this.innerDictionary[key]; }
			set { throw new NotSupportedException(); }
		}

		#endregion

		#region ICollection<KeyValuePair<KeyT,ValueT>> Members

		void ICollection<KeyValuePair<KeyT, ValueT>>.Add(KeyValuePair<KeyT, ValueT> item)
		{
			throw new NotSupportedException();
		}

		void ICollection<KeyValuePair<KeyT, ValueT>>.Clear()
		{
			throw new NotSupportedException();
		}

		bool ICollection<KeyValuePair<KeyT, ValueT>>.Contains(KeyValuePair<KeyT, ValueT> item)
		{
			return ((ICollection<KeyValuePair<KeyT, ValueT>>)this.innerDictionary).Contains(item);
		}

		void ICollection<KeyValuePair<KeyT, ValueT>>.CopyTo(KeyValuePair<KeyT, ValueT>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<KeyT, ValueT>>)this.innerDictionary).CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return this.innerDictionary.Count; }
		}

		bool ICollection<KeyValuePair<KeyT, ValueT>>.IsReadOnly
		{
			get { return ((ICollection<KeyValuePair<KeyT, ValueT>>)this.innerDictionary).IsReadOnly; }
		}

		bool ICollection<KeyValuePair<KeyT, ValueT>>.Remove(KeyValuePair<KeyT, ValueT> item)
		{
			return ((ICollection<KeyValuePair<KeyT, ValueT>>)this.innerDictionary).Remove(item);
		}

		#endregion

		#region IEnumerable<KeyValuePair<KeyT,ValueT>> Members

		IEnumerator<KeyValuePair<KeyT, ValueT>> IEnumerable<KeyValuePair<KeyT, ValueT>>.GetEnumerator()
		{
			return ((ICollection<KeyValuePair<KeyT, ValueT>>)this.innerDictionary).GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)this.innerDictionary).GetEnumerator();
		}

		#endregion

		public override bool Equals(object obj)
		{
			return this.innerDictionary.Equals(obj);
		}

		public override int GetHashCode()
		{
			return this.innerDictionary.GetHashCode();
		}

		public override string ToString()
		{
			return this.innerDictionary.ToString();
		}
	}

	internal static class ReadOnlyDictionaryExtentions
	{
		public static ReadOnlyDictionary<KeyT, ValueT> AsReadOnly<KeyT, ValueT>(this Dictionary<KeyT, ValueT> dictionary)
		{
			if (dictionary == null) throw new ArgumentNullException("dictionary");

			return new ReadOnlyDictionary<KeyT, ValueT>(dictionary);
		}
	}
}
