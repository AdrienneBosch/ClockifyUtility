using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using DinkToPdf;
using DinkToPdf.Contracts;

namespace ClockifyUtility.Services
{
    public class PdfService : IPdfService
    {
        private static bool _dllLoaded = false;
        private static readonly object _lock = new();
        private readonly IConverter _converter;

        public PdfService()
        {
            EnsureNativeLibraryLoaded();
            _converter = new SynchronizedConverter(new PdfTools());
        }

        public Task GeneratePdfAsync(string html, string outputPath)
        {
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = new GlobalSettings
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait,
                    Out = outputPath
                },
                Objects =
                {
                    new ObjectSettings
                    {
                        HtmlContent = html
                    }
                }
            };
            _converter.Convert(doc);
            return Task.CompletedTask;
        }

        private void EnsureNativeLibraryLoaded()
        {
            if (_dllLoaded) return;
            lock (_lock)
            {
                if (_dllLoaded) return;
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dllPath = Path.Combine(baseDir, "NativeLibs", "libwkhtmltox.dll");
                if (!File.Exists(dllPath))
                    throw new FileNotFoundException($"libwkhtmltox.dll not found at {dllPath}");
                var context = new CustomAssemblyLoadContext();
                context.LoadUnmanagedLibrary(dllPath);
                _dllLoaded = true;
            }
        }
    }

    public class CustomAssemblyLoadContext : AssemblyLoadContext
    {
        public IntPtr LoadUnmanagedLibrary(string absolutePath)
        {
            return LoadUnmanagedDll(absolutePath);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            return LoadUnmanagedDllFromPath(unmanagedDllName);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
