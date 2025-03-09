using JetBrains.Annotations;

namespace GameDevWare.Charon.Editor.Services.ServerApi
{
	[PublicAPI]
	public enum ImportReportDocumentChangeStatus
	{
		Created = 0,
		Updated = 1,
		Deleted = 2,
		Skipped = 3,
		Unchanged = 4,
		Error = 5,
	}
}
