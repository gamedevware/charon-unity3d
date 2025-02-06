/*
 Copyright 2010-2014 Outercurve Foundation

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GameDevWare.Charon.Unity.Utils
{
	/// <summary>
	///     A hybrid implementation of SemVer that supports semantic versioning as described at http://semver.org while not
	///     strictly enforcing it to
	///     allow older 4-digit versioning schemes to continue working.
	/// </summary>
	[Serializable]
	internal sealed class SemanticVersion : IComparable, IComparable<SemanticVersion>, IEquatable<SemanticVersion>
	{
		private const RegexOptions FLAGS = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;
		private static readonly Regex SemanticVersionRegex = new Regex(@"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>-[a-z][0-9a-z-]*)?$", FLAGS);
		private static readonly Regex StrictSemanticVersionRegex = new Regex(@"^(?<Version>\d+(\.\d+){2})(?<Release>-[a-z][0-9a-z-]*)?$", FLAGS);
		private readonly string originalString;
		private string normalizedVersionString;

		/// <summary>
		///     Gets the normalized version portion.
		/// </summary>
		public Version Version { get; private set; }

		/// <summary>
		///     Gets the optional special version.
		/// </summary>
		public string SpecialVersion { get; private set; }

		public SemanticVersion(string version)
			: this(Parse(version))
		{
			// The constructor normalizes the version string so that it we do not need to normalize it every time we need to operate on it. 
			// The original string represents the original form in which the version is represented to be used when printing.
			this.originalString = version;
		}

		public SemanticVersion(int major, int minor, int build, int revision)
			: this(new Version(major, minor, build, revision))
		{
		}

		public SemanticVersion(int major, int minor, int build, string specialVersion)
			: this(new Version(major, minor, build), specialVersion)
		{
		}

		public SemanticVersion(Version version)
			: this(version, string.Empty)
		{
		}

		public SemanticVersion(Version version, string specialVersion)
			: this(version, specialVersion, null)
		{
		}

		private SemanticVersion(Version version, string specialVersion, string originalString)
		{
			if (version == null) throw new ArgumentNullException("version");

			this.Version = NormalizeVersionValue(version);
			this.SpecialVersion = specialVersion ?? string.Empty;
			this.originalString = string.IsNullOrEmpty(originalString) ? version + (!string.IsNullOrEmpty(specialVersion) ? '-' + specialVersion : null) :
				originalString;
		}

		internal SemanticVersion(SemanticVersion semVer)
		{
			this.originalString = semVer.ToString();
			this.Version = semVer.Version;
			this.SpecialVersion = semVer.SpecialVersion;
		}

		public string[] GetOriginalVersionComponents()
		{
			if (!string.IsNullOrEmpty(this.originalString))
			{
				string original;

				// search the start of the SpecialVersion part, if any
				var dashIndex = this.originalString.IndexOf('-');
				if (dashIndex != -1)
				{
					// remove the SpecialVersion part
					original = this.originalString.Substring(0, dashIndex);
				}
				else
					original = this.originalString;

				return SplitAndPadVersionString(original);
			}

			return SplitAndPadVersionString(this.Version.ToString());
		}

		private static string[] SplitAndPadVersionString(string version)
		{
			var a = version.Split('.');
			if (a.Length == 4)
				return a;

			// if 'a' has less than 4 elements, we pad the '0' at the end 
			// to make it 4.
			var b = new string[4] {"0", "0", "0", "0"};
			Array.Copy(a, 0, b, 0, a.Length);
			return b;
		}

		/// <summary>
		///     Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an
		///     optional special version.
		/// </summary>
		public static SemanticVersion Parse(string version)
		{
			if (string.IsNullOrEmpty(version)) throw new ArgumentException("Argument can't be null or empty.", "version");

			SemanticVersion semVer;
			if (!TryParse(version, out semVer))
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Invalid semantic version string '{0}'.", version), "version");

			return semVer;
		}

		/// <summary>
		///     Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an
		///     optional special version.
		/// </summary>
		public static bool TryParse(string version, out SemanticVersion value)
		{
			return TryParseInternal(version, SemanticVersionRegex, out value);
		}

		/// <summary>
		///     Parses a version string using strict semantic versioning rules that allows exactly 3 components and an optional
		///     special version.
		/// </summary>
		public static bool TryParseStrict(string version, out SemanticVersion value)
		{
			return TryParseInternal(version, StrictSemanticVersionRegex, out value);
		}

		private static bool TryParseInternal(string version, Regex regex, out SemanticVersion semVer)
		{
			semVer = null;
			if (string.IsNullOrEmpty(version)) return false;

			var match = regex.Match(version.Trim());
			Version versionValue;
			if (!match.Success || !TryParseVersion(match.Groups["Version"].Value, out versionValue)) return false;

			semVer = new SemanticVersion(NormalizeVersionValue(versionValue), match.Groups["Release"].Value.TrimStart('-'), version.Replace(" ", ""));
			return true;
		}

		private static bool TryParseVersion(string version, out Version value)
		{
			value = null;
			try
			{
				value = new Version(version);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		///     Attempts to parse the version token as a SemanticVersion.
		/// </summary>
		/// <returns>An instance of SemanticVersion if it parses correctly, null otherwise.</returns>
		public static SemanticVersion ParseOptionalVersion(string version)
		{
			SemanticVersion semVer;
			TryParse(version, out semVer);
			return semVer;
		}

		private static Version NormalizeVersionValue(Version version)
		{
			return new Version(version.Major,
				version.Minor,
				Math.Max(version.Build, 0),
				Math.Max(version.Revision, 0));
		}

		public static bool operator ==(SemanticVersion version1, SemanticVersion version2)
		{
			if (ReferenceEquals(version1, null)) return ReferenceEquals(version2, null);

			return version1.Equals(version2);
		}

		public static bool operator !=(SemanticVersion version1, SemanticVersion version2)
		{
			return !(version1 == version2);
		}

		public static bool operator <(SemanticVersion version1, SemanticVersion version2)
		{
			if (version1 == null) throw new ArgumentNullException("version1");

			return version1.CompareTo(version2) < 0;
		}

		public static bool operator <=(SemanticVersion version1, SemanticVersion version2)
		{
			return version1 == version2 || version1 < version2;
		}

		public static bool operator >(SemanticVersion version1, SemanticVersion version2)
		{
			if (version1 == null) throw new ArgumentNullException("version1");

			return version2 < version1;
		}

		public static bool operator >=(SemanticVersion version1, SemanticVersion version2)
		{
			return version1 == version2 || version1 > version2;
		}

		public override string ToString()
		{
			return this.originalString;
		}

		/// <summary>
		///     Returns the normalized string representation of this instance of <see cref="SemanticVersion" />.
		///     If the instance can be strictly parsed as a <see cref="SemanticVersion" />, the normalized version
		///     string if of the format {major}.{minor}.{build}[-{special-version}]. If the instance has a non-zero
		///     value for <see cref="Version.Revision" />, the format is {major}.{minor}.{build}.{revision}[-{special-version}].
		/// </summary>
		/// <returns>The normalized string representation.</returns>
		public string ToNormalizedString()
		{
			if (this.normalizedVersionString == null)
			{
				var builder = new StringBuilder();
				builder
					.Append(this.Version.Major)
					.Append('.')
					.Append(this.Version.Minor)
					.Append('.')
					.Append(Math.Max(0, this.Version.Build));

				if (this.Version.Revision > 0)
				{
					builder.Append('.')
						.Append(this.Version.Revision);
				}

				if (!string.IsNullOrEmpty(this.SpecialVersion))
				{
					builder.Append('-')
						.Append(this.SpecialVersion);
				}

				this.normalizedVersionString = builder.ToString();
			}

			return this.normalizedVersionString;
		}

		public override bool Equals(object obj)
		{
			var semVer = obj as SemanticVersion;
			return !ReferenceEquals(null, semVer) && this.Equals(semVer);
		}

		public override int GetHashCode()
		{
			var hashCode = this.Version.GetHashCode();
			if (this.SpecialVersion != null) hashCode = hashCode * 4567 + this.SpecialVersion.GetHashCode();

			return hashCode;
		}

		public int CompareTo(object obj)
		{
			if (ReferenceEquals(obj, null)) return 1;

			var other = obj as SemanticVersion;
			if (other == null) return 1;

			return this.CompareTo(other);
		}

		public int CompareTo(SemanticVersion other)
		{
			if (ReferenceEquals(other, null)) return 1;

			var result = this.Version.CompareTo(other.Version);

			if (result != 0) return result;

			var empty = string.IsNullOrEmpty(this.SpecialVersion);
			var otherEmpty = string.IsNullOrEmpty(other.SpecialVersion);
			if (empty && otherEmpty)
				return 0;

			if (empty)
				return 1;
			if (otherEmpty) return -1;

			return StringComparer.OrdinalIgnoreCase.Compare(this.SpecialVersion, other.SpecialVersion);
		}

		public bool Equals(SemanticVersion other)
		{
			return !ReferenceEquals(null, other) &&
				this.Version.Equals(other.Version) &&
				this.SpecialVersion.Equals(other.SpecialVersion, StringComparison.OrdinalIgnoreCase);
		}
	}
}