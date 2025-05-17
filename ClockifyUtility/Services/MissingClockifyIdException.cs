using System;

namespace ClockifyUtility.Services
{
    public class MissingClockifyIdException : Exception
    {
        public string ApiKey { get; }
        public MissingClockifyIdException(string apiKey)
            : base("UserId or WorkspaceId is missing from config.")
        {
            ApiKey = apiKey;
        }
    }
}
