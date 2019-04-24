namespace GameDevWare.Charon.Unity.Updates.Packages.Zip
{
	/// <summary>
	/// The strategy to apply to testing.
	/// </summary>
	internal enum TestStrategy
	{
		/// <summary>
		/// Find the first error only.
		/// </summary>
		FindFirstError,

		/// <summary>
		/// Find all possible errors.
		/// </summary>
		FindAllErrors,
	}
}