namespace ClockifyUtility.Services
{
	public class MissingClockifyIdException : Exception
	{
		public MissingClockifyIdException ( string apiKey )
			: base ( "UserId or WorkspaceId is missing from config." )
		{
			ApiKey = apiKey;
		}

		public string ApiKey { get; }
	}
}