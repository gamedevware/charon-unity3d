namespace GameDevWare.Charon.Models
{
	internal sealed class AccountRef
	{
		public string Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }

		public override string ToString()
		{
			return string.Format("{0} {1}, id: {2}", this.FirstName, this.LastName, this.Id);
		}
	}
}