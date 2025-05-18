using System.IO;

namespace ClockifyUtility.Services
{
	public class FileService : IFileService
	{
		public async Task SaveHtmlAsync ( string html, string filePath )
		{


			await File.WriteAllTextAsync ( filePath, html );
		}
	}
}