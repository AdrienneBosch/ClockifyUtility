using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClockifyUtility.Models
{
    public class InvoiceConfigValidator
    {
        private static readonly Regex HexColorRegex = new Regex("^#([A-Fa-f0-9]{6})$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new Regex(@"^[\d\-\+\(\)\s]{7,}$", RegexOptions.Compiled);

        public static List<string> Validate(InvoiceConfig config)
        {
            var errors = new List<string>();
            if (config == null)
            {
                errors.Add("Config is null.");
                return errors;
            }
            if (config.Clockify == null)
                errors.Add("Missing required section: Clockify");
            if (config.InvoiceStyle == null)
                errors.Add("Missing required section: InvoiceStyle");
            if (config.Clockify != null)
            {
                var c = config.Clockify;
                if (string.IsNullOrWhiteSpace(c.ClockifyApiKey)) errors.Add("ClockifyApiKey is required.");
                if (string.IsNullOrWhiteSpace(c.UserId)) errors.Add("UserId is required.");
                if (string.IsNullOrWhiteSpace(c.WorkspaceId)) errors.Add("WorkspaceId is required.");
                if (string.IsNullOrWhiteSpace(c.FromName)) errors.Add("FromName is required.");
                if (string.IsNullOrWhiteSpace(c.ContactEmail) || !EmailRegex.IsMatch(c.ContactEmail)) errors.Add("ContactEmail is required and must be a valid email.");
                if (string.IsNullOrWhiteSpace(c.ContactPhone) || !PhoneRegex.IsMatch(c.ContactPhone)) errors.Add("ContactPhone is required and must be a valid phone number.");
                if (string.IsNullOrWhiteSpace(c.BankName)) errors.Add("BankName is required.");
                if (string.IsNullOrWhiteSpace(c.BankAccountNumber)) errors.Add("BankAccountNumber is required.");
                if (string.IsNullOrWhiteSpace(c.BankAccountHolder)) errors.Add("BankAccountHolder is required.");
                if (string.IsNullOrWhiteSpace(c.BankRoutingNumber)) errors.Add("BankRoutingNumber is required.");
                if (string.IsNullOrWhiteSpace(c.ClientName)) errors.Add("ClientName is required.");
                if (string.IsNullOrWhiteSpace(c.ClientEmailAddress) || !EmailRegex.IsMatch(c.ClientEmailAddress)) errors.Add("ClientEmailAddress is required and must be a valid email.");
                if (string.IsNullOrWhiteSpace(c.ClientNumber)) errors.Add("ClientNumber is required.");
                if (string.IsNullOrWhiteSpace(c.CurrencySymbol)) errors.Add("CurrencySymbol is required.");
                if (c.HourlyRate <= 0) errors.Add("HourlyRate must be greater than 0.");
                if (string.IsNullOrWhiteSpace(c.OutputPath)) errors.Add("OutputPath is required.");
                if (string.IsNullOrWhiteSpace(c.InvoiceNumber)) errors.Add("InvoiceNumber is required.");
            }
            if (config.ConstantLineItems != null)
            {
                for (int i = 0; i < config.ConstantLineItems.Count; i++)
                {
                    var item = config.ConstantLineItems[i];
                    if (string.IsNullOrWhiteSpace(item.Description)) errors.Add($"ConstantLineItems[{i}]: Description is required.");
                    if (item.Amount < 0) errors.Add($"ConstantLineItems[{i}]: Amount must be >= 0.");
                }
            }
            if (config.InvoiceStyle != null)
            {
                var s = config.InvoiceStyle;
                ValidateColor(s.PrimaryColor, "PrimaryColor", errors);
                ValidateColor(s.SecondaryColor, "SecondaryColor", errors);
                ValidateColor(s.AccentColor, "AccentColor", errors);
                ValidateColor(s.BackgroundColor, "BackgroundColor", errors);
                ValidateColor(s.TextColor, "TextColor", errors);
                ValidateColor(s.TableHeaderBg, "TableHeaderBg", errors);
                ValidateColor(s.TableBorder, "TableBorder", errors);
                ValidateColor(s.SoftHeadingBg, "SoftHeadingBg", errors);
                ValidateColor(s.SoftAltRowBg, "SoftAltRowBg", errors);
                ValidateColor(s.SectionBg, "SectionBg", errors);
                ValidateColor(s.SectionText, "SectionText", errors);
            }
            return errors;
        }

        private static void ValidateColor(string value, string field, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(value) || !HexColorRegex.IsMatch(value))
                errors.Add($"{field} must be a valid hex color (e.g. #AABBCC).");
        }
    }
}
