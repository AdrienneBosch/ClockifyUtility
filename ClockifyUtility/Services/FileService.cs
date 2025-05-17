using System;
using System.IO;
using System.Threading.Tasks;

namespace ClockifyUtility.Services
{
    public class FileService : IFileService
    {
        public async Task SaveHtmlAsync(string html, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(filePath, html);
        }
    }
}
