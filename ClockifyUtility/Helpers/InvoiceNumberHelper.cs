using System;
using System.IO;
using Newtonsoft.Json;
using ClockifyUtility.ViewModels;

namespace ClockifyUtility.Helpers
{
    public static class InvoiceNumberHelper
    {
        private static readonly object _lock = new object();

        public static string GetAndIncrementInvoiceNumber(string appSettingsPath)
        {
            lock (_lock)
            {
                AppSettings? settings = null;
                if (File.Exists(appSettingsPath))
                {
                    var json = File.ReadAllText(appSettingsPath);
                    settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    settings = new AppSettings();
                }
                string current = settings.InvoiceNumber;
                if (string.IsNullOrWhiteSpace(current))
                    current = "000";
                if (!int.TryParse(current, out int num))
                    num = 0;
                num++;
                string next = num.ToString("D3");
                settings.InvoiceNumber = next;
                File.WriteAllText(appSettingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
                return next;
            }
        }
    }
}
