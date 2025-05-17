using System;

namespace ClockifyUtility.Models
{
    public class TimeEntryModel
    {
        public string ProjectName { get; set; }
        public double Hours { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Description { get; set; }
    }
}
