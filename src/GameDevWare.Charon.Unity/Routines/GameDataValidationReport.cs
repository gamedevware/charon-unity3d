using System;
using System.Linq;
using GameDevWare.Charon.Unity.ServerApi;

namespace GameDevWare.Charon.Unity.Routines
{
	public class GameDataValidationReport
	{
		public readonly string GameDataPath;
		public readonly ValidationReport Report;

		public GameDataValidationReport(string gameDataPath, ValidationReport report)
		{
			if (gameDataPath == null) throw new ArgumentNullException("gameDataPath");
			if (report == null) throw new ArgumentNullException("report");

			this.GameDataPath = gameDataPath;
			this.Report = report;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0}, Has Errors: {1}", this.GameDataPath, this.Report.HasErrors);
		}
	}
}
