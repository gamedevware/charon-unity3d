namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// Delegate invoked during <see cref="ZipFile.TestArchive(bool, TestStrategy, ZipTestResultHandler)">testing</see> if supplied indicating current progress and status.
	/// </summary>
	/// <remarks>If the message is non-null an error has occured.  If the message is null
	/// the operation as found in <see cref="TestStatus">status</see> has started.</remarks>
	internal delegate void ZipTestResultHandler(TestStatus status, string message);
}