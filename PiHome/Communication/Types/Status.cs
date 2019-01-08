namespace Communication.Types
{
	public class StatusResponse
	{
		public Status status { get; set; }
	}

	public enum Status
	{
		success,
		failed,
		refused,
		accepted
	}
}