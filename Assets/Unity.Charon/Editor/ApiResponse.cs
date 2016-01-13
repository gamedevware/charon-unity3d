namespace Assets.Unity.Charon.Editor
{
	public class ApiResponse<ResultT>
	{
		public string Message { get; set; }

		public ResultT Result { get; set; }

		public bool IsOk()
		{
			return string.IsNullOrEmpty(this.Message);
		}
	}
}
