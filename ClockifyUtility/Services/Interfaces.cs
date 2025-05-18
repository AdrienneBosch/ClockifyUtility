using ClockifyUtility.Models;

namespace ClockifyUtility.Services
{
	public interface IClockifyService
	{
   Task<List<TimeEntryModel>> FetchTimeEntriesAsync ( DateTime start, DateTime end, InvoiceConfig config );
	}

	public interface IConfigService
	{
		ConfigModel LoadConfig ( );
	}

	public interface IFileService
	{
		Task SaveHtmlAsync ( string html, string filePath );
	}

	public interface IInvoiceService
	{
   Task<string> GenerateInvoiceAsync ( DateTime start, DateTime end, InvoiceConfig config );
	}
}