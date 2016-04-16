using System.Collections;
using System.Text;

namespace Assets.Editor.GameDevWare.Charon.Tasks
{
	public class RunToolsTask : Task
	{
		private readonly ExecuteCommandTask runTools;
		private readonly StringBuilder errorOutput;
		private readonly StringBuilder standartOutput;

		public bool IsSuccessfull { get { return this.runTools.IsRunning == false && this.runTools.ExitCode == 0; } }
		public string Errors { get { return this.errorOutput.ToString(); } }
		public string Output { get { return this.standartOutput.ToString(); } }

		public RunToolsTask(params string[] arguments)
		{
			this.standartOutput = new StringBuilder();
			this.errorOutput = new StringBuilder();
			this.runTools = new ExecuteCommandTask
			(
				Settings.Current.ToolsPath,
				(sender, args) => { if (!string.IsNullOrEmpty(args.Data)) standartOutput.Append(args.Data); },
				(sender, args) => { if (!string.IsNullOrEmpty(args.Data)) errorOutput.Append(args.Data); },
				arguments
			);
			this.runTools.RequireDotNetRuntime();
		}

		protected override IEnumerable InitAsync()
		{
			yield return this.StartedEvent;

			this.runTools.Start();
			yield return this.runTools;

			this.runTools.Close();
		}
	}
}
