using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
    public class ClockifyService : IClockifyService
    {
        public async Task<List<TimeEntryModel>> FetchTimeEntriesAsync(DateTime start, DateTime end, ConfigModel config)
        {
            // TODO: Implement real API call. For now, return dummy data.
            await Task.Delay(100); // Simulate async
            return new List<TimeEntryModel>
            {
                new TimeEntryModel { ProjectName = "Project A", Hours = 10, Start = start, End = end, Description = "Development" },
                new TimeEntryModel { ProjectName = "Project B", Hours = 5, Start = start, End = end, Description = "Testing" }
            };
        }
    }
}
