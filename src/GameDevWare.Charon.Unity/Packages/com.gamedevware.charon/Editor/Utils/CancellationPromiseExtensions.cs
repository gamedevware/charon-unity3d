using System;
using GameDevWare.Charon.Unity.Async;
using UnityEditor;

namespace GameDevWare.Charon.Unity.Utils
{
	internal static class CancellationPromiseExtensions
	{
		public static void ThrowIfCancellationRequested(this Promise promise)
		{
			if (promise == null)
			{
				return;
			}
			if (promise.IsCompleted)
			{
				throw new InvalidOperationException(Resources.UI_UNITYPLUGIN_ERROR_CANCELLED);
			}
		}
		public static void ThrowIfScriptsCompiling(this Promise promise)
		{
			if (EditorApplication.isCompiling)
			{
				promise.TrySetCompleted();
				throw new InvalidOperationException(Resources.UI_UNITYPLUGIN_ERROR_SCRIPTS_COMPILING);
			}
		}
	}
}
