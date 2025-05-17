namespace ClockifyUtility.Models
{
	public class TimeEntryModel
	{
		public string? Description { get; set; }
		public DateTime End { get; set; }
		public double Hours { get; set; }
		public string? ProjectId { get; set; }
		public string? ProjectName { get; set; }
		public DateTime Start { get; set; }
	}
}