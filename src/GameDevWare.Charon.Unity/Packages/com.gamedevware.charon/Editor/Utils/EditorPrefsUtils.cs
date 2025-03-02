using System;
using System.Globalization;
using GameDevWare.Charon.Editor.Services;
using UnityEditor;

namespace GameDevWare.Charon.Editor.Utils
{
	internal static class EditorPrefsUtils
	{
		private static readonly string PreferencesPrefix = typeof(CharonSettings).Assembly.GetName().Name + "::" + nameof(CharonSettings);

		public static T GetPrefsValue<T>(string keyName, T defaultValue, ref T? cachedValue) where T: struct
		{
			if (cachedValue.HasValue)
			{
				return cachedValue.Value;
			}

			var prefsValue = EditorPrefs.GetString(PreferencesPrefix + "::" + keyName);
			if (string.IsNullOrEmpty(prefsValue))
			{
				cachedValue = defaultValue;
				return defaultValue;
			}

			try
			{
				if (typeof(T).IsEnum)
				{
					cachedValue = (T)Enum.Parse(typeof(T), prefsValue, ignoreCase: true);
				}
				else if (typeof(T) == typeof(TimeSpan))
				{
					cachedValue = (T)(object)TimeSpan.Parse(prefsValue, CultureInfo.InvariantCulture);
				}
				else
				{
					cachedValue = (T)Convert.ChangeType(prefsValue, typeof(T), CultureInfo.InvariantCulture);
				}
				return cachedValue.Value;
			}
			catch
			{
				cachedValue = defaultValue;
				return defaultValue;
			}
		}
		public static void SetPrefsValue<T>(string keyName, T value, ref T? cachedValue) where T: struct
		{
			if (cachedValue.HasValue && value.Equals(cachedValue.Value))
			{
				return; // same
			}

			cachedValue = value;
			var stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
			EditorPrefs.SetString(PreferencesPrefix + "::" + keyName, stringValue);
		}
		public static T GetPrefsValueRef<T>(string keyName, T defaultValue, ref T cachedValue) where T: class
		{
			if (cachedValue != null)
			{
				return cachedValue;
			}

			var prefsValue = EditorPrefs.GetString(PreferencesPrefix + "::" + keyName);
			if (string.IsNullOrEmpty(prefsValue))
			{
				cachedValue = defaultValue;
				return defaultValue;
			}

			try
			{
				cachedValue = (T)Convert.ChangeType(prefsValue, typeof(T), CultureInfo.InvariantCulture);
				return cachedValue;
			}
			catch
			{
				cachedValue = defaultValue;
				return defaultValue;
			}
		}
		public static void SetPrefsValueRef<T>(string keyName, T value, ref T cachedValue) where T: class
		{
			if (ReferenceEquals(cachedValue, value) || (value != null && value.Equals(cachedValue)))
			{
				return; // same
			}
			cachedValue = value;
			var stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);
			EditorPrefs.SetString(PreferencesPrefix + "::" + keyName, stringValue);
		}
	}
}
