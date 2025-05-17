using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
    public interface IClockifyService
    {
        Task<List<TimeEntryModel>> FetchTimeEntriesAsync(DateTime start, DateTime end, ConfigModel config, Action<string>? log = null);
    }

    public interface IInvoiceService
    {
        Task<string> GenerateInvoiceAsync(DateTime start, DateTime end, ConfigModel config, Action<string>? log = null);
    }

    public interface IConfigService
    {
        ConfigModel LoadConfig();
    }

    public interface IFileService
    {
        Task SaveHtmlAsync(string html, string filePath);
    }
}
