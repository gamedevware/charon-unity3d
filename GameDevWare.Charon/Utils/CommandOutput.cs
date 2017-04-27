using GameDevWare.Charon.Async;

namespace GameDevWare.Charon.Utils
{
	public class CommandOutput
	{
		public const string FORMAT_JSON = "json";

		public string Type { get; set; }
		public string Format { get; set; }
		public string[] FormattingOptions { get; set; }

		public Promise<RunResult> Capture(Promise<RunResult> runTask)
		{
			throw new System.NotImplementedException();
		}

		public static CommandOutput JsonObject()
		{
			throw new System.NotImplementedException();
		}

		public T ReadAs<T>()
		{
			throw new System.NotImplementedException();
		}
	}
}