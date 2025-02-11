using System;
using GameDevWare.Charon.Editor.Cli;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameDevWare.Charon.Editor
{
	internal class CharonLogger : ILogger
	{
		private readonly CharonSettings settings;
		private readonly Logger logger;

		/// <inheritdoc />
		public ILogHandler logHandler { get => this.logger.logHandler; set => this.logger.logHandler = value; }
		/// <inheritdoc />
		public bool logEnabled { get => this.logger.logEnabled; set => this.logger.logEnabled = value; }
		/// <inheritdoc />
		public LogType filterLogType { get => this.logger.filterLogType; set => this.logger.filterLogType = value; }

		public CharonLogger(CharonSettings settings)
		{
			this.settings = settings;
			this.logger = new Logger(Debug.unityLogger);
		}

		/// <inheritdoc />
		public void LogFormat(LogType logType, Object context, string format, params object[] args)
		{
			this.logger.LogFormat(logType, context, format, args);
		}
		/// <inheritdoc />
		public void LogException(Exception exception, Object context)
		{
			this.logger.LogException(exception, context);
		}
		/// <inheritdoc />
		public bool IsLogTypeAllowed(LogType logType)
		{
			return logType switch {
				LogType.Error => this.settings.LogLevel != CharonLogLevel.None,
				LogType.Assert => this.settings.LogLevel == CharonLogLevel.Verbose,
				LogType.Log => this.settings.LogLevel != CharonLogLevel.None,
				LogType.Warning => this.settings.LogLevel != CharonLogLevel.None,
				LogType.Exception => this.settings.LogLevel != CharonLogLevel.None,
				_ => throw new ArgumentOutOfRangeException(nameof(logType), logType, null)
			};
		}
		/// <inheritdoc />
		public void Log(LogType logType, object message)
		{
			if (!this.IsLogTypeAllowed(logType))
			{
				return;
			}
			if (logType == LogType.Assert)
			{
				logType = LogType.Log;
			}

			this.logger.Log(logType, message);
		}
		/// <inheritdoc />
		public void Log(LogType logType, object message, Object context)
		{
			if (!this.IsLogTypeAllowed(logType))
			{
				return;
			}
			if (logType == LogType.Assert)
			{
				logType = LogType.Log;
			}

			this.logger.Log(logType, message, context);
		}
		/// <inheritdoc />
		public void Log(LogType logType, string tag, object message)
		{
			if (!this.IsLogTypeAllowed(logType))
			{
				return;
			}
			if (logType == LogType.Assert)
			{
				logType = LogType.Log;
			}

			this.logger.Log(logType, tag, message);
		}
		/// <inheritdoc />
		public void Log(LogType logType, string tag, object message, Object context)
		{
			if (!this.IsLogTypeAllowed(logType))
			{
				return;
			}
			if (logType == LogType.Assert)
			{
				logType = LogType.Log;
			}

			this.logger.Log(logType, tag, message, context);
		}
		/// <inheritdoc />
		public void Log(object message)
		{
			this.logger.Log(message);
		}
		/// <inheritdoc />
		public void Log(string tag, object message)
		{
			this.logger.Log(tag, message);
		}
		/// <inheritdoc />
		public void Log(string tag, object message, Object context)
		{
			this.logger.Log(tag, message, context);
		}
		/// <inheritdoc />
		public void LogWarning(string tag, object message)
		{
			this.logger.LogWarning(tag, message);
		}
		/// <inheritdoc />
		public void LogWarning(string tag, object message, Object context)
		{
			this.logger.LogWarning(tag, message, context);
		}
		/// <inheritdoc />
		public void LogError(string tag, object message)
		{
			this.logger.LogError(tag, message);
		}
		/// <inheritdoc />
		public void LogError(string tag, object message, Object context)
		{
			this.logger.LogError(tag, message, context);
		}
		/// <inheritdoc />
		public void LogFormat(LogType logType, string format, params object[] args)
		{
			if (!this.IsLogTypeAllowed(logType))
			{
				return;
			}
			if (logType == LogType.Assert)
			{
				logType = LogType.Log;
			}

			this.logger.LogFormat(logType, format, args);
		}
		/// <inheritdoc />
		public void LogException(Exception exception)
		{
			this.logger.LogException(exception);
		}
	}
}
