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
using System.Collections.Generic;
using System.Linq;
using GameDevWare.Charon.Editor.Services.ResourceServerApi;
using WeightedFormulaType = System.Collections.Generic.KeyValuePair<GameDevWare.Charon.Editor.Services.ResourceServerApi.FormulaType, float>;

namespace GameDevWare.Charon.Editor.Services
{
	public class FormulaTypeIndexer
	{
		private readonly IComparer<WeightedFormulaType> weightedTypeInformationComparer;
		private readonly Dictionary<string, string> aliases;
		private FormulaType[] types;

		public FormulaTypeIndexer()
		{
			this.weightedTypeInformationComparer = Comparer<WeightedFormulaType>.Create((x, y) => y.Value.CompareTo(x.Value));

			// ReSharper disable AssignNullToNotNullAttribute
			this.aliases = new Dictionary<string, string> {
#pragma warning disable CS8604 // Possible null reference argument.
				{ typeof(void).FullName, "void" },
				{ typeof(byte).FullName, "byte" },
				{ typeof(sbyte).FullName, "sbyte" },
				{ typeof(short).FullName, "short" },
				{ typeof(ushort).FullName, "ushort" },
				{ typeof(int).FullName, "int" },
				{ typeof(uint).FullName, "uint" },
				{ typeof(long).FullName, "long" },
				{ typeof(ulong).FullName, "ulong" },
				{ typeof(float).FullName, "float" },
				{ typeof(double).FullName, "double" },
				{ typeof(decimal).FullName, "decimal" },
				{ typeof(char).FullName, "char" },
				{ typeof(string).FullName, "string" },
				{ typeof(object).FullName, "object" },
#pragma warning restore CS8604 // Possible null reference argument.
			};

			this.types = Array.Empty<FormulaType>();
		}

		public ListFormulaTypesResponse ListFormulaTypes
		(
			int skip,
			int take,
			string query
		)
		{
			this.FillTypes();

			if (string.IsNullOrEmpty(query))
			{
				return new ListFormulaTypesResponse {
					Types = this.types.Skip(skip).Take(take).ToArray(),
					Total = this.types.Length
				};
			}

			var foundTypes = new List<WeightedFormulaType>(take + 1);
			var total = 0;
			foreach (var typeInfo in this.types)
			{
				var weight = query == null ? 1 : Math.Max(Match(typeInfo.Name, query), Match(typeInfo.FullName, query));
				if (weight <= 0)
				{
					continue;
				}

				total++;

				var weightedTypeInfo = new WeightedFormulaType(typeInfo, weight);
				var index = foundTypes.BinarySearch(weightedTypeInfo, this.weightedTypeInformationComparer);
				if (index >= 0 && index < take)
					foundTypes.Insert(index, weightedTypeInfo);
				else if (index < 0 && ~index < take)
					foundTypes.Insert(~index, weightedTypeInfo);

				while (foundTypes.Count > take)
				{
					foundTypes.RemoveAt(take);
				}
			}

			var list = new List<FormulaType>(take);
			foreach (var foundType in foundTypes)
			{
				// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
				if (foundType.Key != null)
				{
					list.Add(foundType.Key);
				}
			}

			return new ListFormulaTypesResponse { Types = list.ToArray(), Total = total };
		}

		private void FillTypes()
		{
			if (this.types.Length > 0)
			{
				return;
			}

			var formulaTypes = new List<FormulaType>();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				var assemblyName = assembly.GetName().Name ?? string.Empty;
				foreach (var type in assembly.GetTypes())
				{
					if (type.DeclaringType != null ||
						type.IsNotPublic ||
						type.IsGenericType ||
						type.IsNested ||
						type.IsPointer ||
						type.IsArray ||
						type.IsByRef ||
						type.IsSZArray ||
						type.IsCOMObject ||
						type.IsSpecialName ||
						type.IsByRefLike)
					{
						continue;
					}

					formulaTypes.Add(ToFormulaType(type, assemblyName));
				}
			}
			this.types = formulaTypes.ToArray();
		}

		private static FormulaType ToFormulaType(Type type, string assemblyName)
		{
			var kind = type.IsEnum ? FormulaTypeKind.Enum :
				type.IsInterface ? FormulaTypeKind.Interface :
				typeof(MulticastDelegate).IsAssignableFrom(type) ? FormulaTypeKind.Delegate :
				type.IsValueType ? FormulaTypeKind.Structure : FormulaTypeKind.Class;

			var formulaType = new FormulaType {
				Kind = kind,
				SourceCodeLanguage = "cSharp73",
				Name = type.Name,
				PackageOrNamespaceName = type.Namespace ?? string.Empty,
				ModuleName = assemblyName,
				FullName = type.FullName ?? string.Empty
			};
			return formulaType;
		}
		private static float Match(string value, string pattern)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			if (pattern == null) throw new ArgumentNullException(nameof(pattern));

			const float NO_MATCH = 0.0f;
			const float FULL_MATCH = 1.0f;
			const float FULL_MATCH_WRONG_CASE = 0.99f;
			const float MATCH = 0.9f;
			const float MATCH_RANGE = FULL_MATCH_WRONG_CASE - MATCH - 0.001f;
			const float MATCH_WRONG_CASE = 0.8f;
			const float MATCH_WRONG_CASE_RANGE = MATCH - MATCH_WRONG_CASE - 0.001f;
			const float CAPITALS_MATCH = 0.5f;
			const float CAPITALS_MATCH_RANGE = MATCH_WRONG_CASE - CAPITALS_MATCH - 0.001f;
			const float PARTIAL_MATCH = 0.3f;
			const float PARTIAL_MATCH_POS_RANGE = 0.05f;
			const float PARTIAL_MATCH_LEN_RANGE = CAPITALS_MATCH - PARTIAL_MATCH - PARTIAL_MATCH_POS_RANGE - 0.001f;
			const float PARTIAL_MATCH_WRONG_CASE = 0.1f;
			const float PARTIAL_MATCH_WRONG_CASE_POS_RANGE = 0.05f;
			const float PARTIAL_MATCH_WRONG_CASE_LEN_RANGE = PARTIAL_MATCH - PARTIAL_MATCH_WRONG_CASE - PARTIAL_MATCH_WRONG_CASE_POS_RANGE - 0.001f;

			if (value.Length == 0 || pattern.Length == 0) return NO_MATCH;

			var indexOf = value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
			var indexOfCaseSensitive = indexOf < 0 ? indexOf : value.IndexOf(pattern, StringComparison.Ordinal);
			var indexOfPartialPart = 0.0f;
			var indexOfPartial = indexOf >= 0 ? -1 : IndexOfPartial(value, pattern, ref indexOfPartialPart, StringComparison.OrdinalIgnoreCase);
			var indexOfPartialCaseSensitivePart = 0.0f;
			var indexOfPartialCaseSensitive = indexOf >= 0
				? -1 : IndexOfPartial(value, pattern, ref indexOfPartialCaseSensitivePart, StringComparison.Ordinal);
			var matchCapitals = MatchCapitals(value, pattern);
			var lengthRatio = Math.Min(1.0f, Math.Max(0.0f, (float)pattern.Length / value.Length));

			var positionRange = value.Length - pattern.Length;

			if (indexOfCaseSensitive >= 0 && indexOfCaseSensitive == 0 && pattern.Length == value.Length)
				return FULL_MATCH;

			if (indexOf >= 0 && indexOf == 0 && pattern.Length == value.Length && pattern.Length == value.Length)
				return FULL_MATCH_WRONG_CASE;

			if (indexOfCaseSensitive >= 0)
				return MATCH + (MATCH_RANGE - MATCH_RANGE * ((float)indexOfCaseSensitive / positionRange)) * lengthRatio;

			if (indexOf >= 0)
				return MATCH_WRONG_CASE + (MATCH_WRONG_CASE_RANGE - MATCH_WRONG_CASE_RANGE * ((float)indexOf / positionRange)) * lengthRatio;

			if (matchCapitals > 0.0f)
				return CAPITALS_MATCH + CAPITALS_MATCH_RANGE * matchCapitals;

			if (indexOfPartialCaseSensitive > 0 && indexOfPartialPart >= 0.3f)
			{
				return PARTIAL_MATCH +
					(PARTIAL_MATCH_POS_RANGE - PARTIAL_MATCH_POS_RANGE * ((float)indexOfCaseSensitive / indexOfPartialCaseSensitive)) +
					PARTIAL_MATCH_LEN_RANGE * indexOfPartialPart;
			}

			if (indexOfPartial >= 0 && indexOfPartialCaseSensitivePart >= 0.3f)
			{
				return PARTIAL_MATCH_WRONG_CASE +
					(PARTIAL_MATCH_WRONG_CASE_POS_RANGE - PARTIAL_MATCH_WRONG_CASE_POS_RANGE * ((float)indexOfCaseSensitive / indexOfPartial)) +
					PARTIAL_MATCH_WRONG_CASE_LEN_RANGE * indexOfPartialCaseSensitivePart;
			}

			return NO_MATCH;
		}
		private static int IndexOfPartial(string value, string pattern, ref float part, StringComparison comparison)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			if (pattern == null) throw new ArgumentNullException(nameof(pattern));

			var isCaseInsensitive = comparison == StringComparison.OrdinalIgnoreCase ||
				comparison == StringComparison.CurrentCultureIgnoreCase ||
				comparison == StringComparison.InvariantCultureIgnoreCase;
			var patternIdx = 0;
			var bestMatchStart = -1;
			var patternChar = isCaseInsensitive ? char.ToLowerInvariant(pattern[patternIdx]) : pattern[patternIdx];
			for (var i = 0; i < value.Length; i++)
			{
				var valueChar = isCaseInsensitive ? char.ToUpperInvariant(value[i]) : value[i];
				if (valueChar == patternChar)
				{
					if ((patternIdx + 1) / (float)pattern.Length >= part)
					{
						bestMatchStart = i - patternIdx;
						part = (patternIdx + 1) / (float)pattern.Length;
					}

					patternIdx++;
					if (patternIdx >= pattern.Length) // full match
						return bestMatchStart;

					patternChar = isCaseInsensitive ? char.ToLowerInvariant(pattern[patternIdx]) : pattern[patternIdx];
				}
				else if (patternIdx != 0)
				{
					patternIdx = 0;
					patternChar = isCaseInsensitive ? char.ToLowerInvariant(pattern[patternIdx]) : pattern[patternIdx];
				}
			}

			return bestMatchStart;
		}
		private static float MatchCapitals(string value, string pattern)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));
			if (pattern == null) throw new ArgumentNullException(nameof(pattern));

			const float NO_MATCH = 0.0f;
			const float FULL_MATCH = 1.0f;
			const float MATCH = 0.5f;
			const float MATCH_RANGE = 0.499f;

			var patternCapitals = CountCapitals(pattern);
			if (patternCapitals != pattern.Length)
				return NO_MATCH;

			var valueCapitals = CountCapitals(value);
			if (valueCapitals < patternCapitals)
				return NO_MATCH;

			var matchStartIndex = -1;
			var patternIdx = 0;
			var patternChar = pattern[patternIdx];
			for (var i = 0; i < value.Length; i++)
			{
				var valueChar = value[i];
				if (char.IsUpper(valueChar) == false) continue;

				if (valueChar == patternChar)
				{
					if (patternIdx == 0) matchStartIndex = i;
					patternIdx++;
					if (patternIdx >= pattern.Length)
					{
						return valueCapitals == patternCapitals
							? FULL_MATCH
							: MATCH + (MATCH_RANGE - MATCH_RANGE * (matchStartIndex / (float)value.Length));
					}

					patternChar = pattern[patternIdx];
				}
				else if (patternIdx != 0)
				{
					patternIdx = 0;
					patternChar = pattern[patternIdx];
				}
			}

			return NO_MATCH;
		}
		private static int CountCapitals(string value)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));

			var count = 0;
			foreach (var valueChar in value)
			{
				if (char.IsUpper(valueChar))
					count++;
			}

			return count;
		}
	}
}
