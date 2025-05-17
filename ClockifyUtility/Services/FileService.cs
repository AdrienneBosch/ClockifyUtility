using System.IO;

namespace ClockifyUtility.Services
{
	public class FileService : IFileService
	{
		public async Task SaveHtmlAsync ( string html, string filePath )
		{
			string? dir = Path.GetDirectoryName ( filePath );
			if ( !Directory.Exists ( dir ) )
			{
				_ = Directory.CreateDirectory ( dir );
			}

			await File.WriteAllTextAsync ( filePath, html );
		}
	}
}