using System.Collections.Generic;
using Newtonsoft.Json;

namespace ClockifyUtility.Models
{
    public class InvoiceConfig
    {
        [JsonProperty("Clockify", Required = Required.Always)]
        public required ClockifySection Clockify { get; set; }

        [JsonProperty("ConstantLineItems")]
        public List<ConstantLineItem>? ConstantLineItems { get; set; } = new();

        [JsonProperty("InvoiceStyle", Required = Required.Always)]
        public required InvoiceStyle InvoiceStyle { get; set; }

        [JsonProperty("InvoiceFontFamily")]
        public string? InvoiceFontFamily { get; set; }

        [JsonProperty("InvoiceFontWeight")]
        public string? InvoiceFontWeight { get; set; }

        [JsonProperty("InvoiceMapMode")]
        public bool? InvoiceMapMode { get; set; }
    }

    public class ClockifySection
    {
        [JsonProperty("ClockifyApiKey", Required = Required.Always)]
        public required string ClockifyApiKey { get; set; }
        [JsonProperty("UserId", Required = Required.Always)]
        public required string UserId { get; set; }
        [JsonProperty("WorkspaceId", Required = Required.Always)]
        public required string WorkspaceId { get; set; }
        [JsonProperty("FromName", Required = Required.Always)]
        public required string FromName { get; set; }
        [JsonProperty("CompanyAddressLine1")]
        public string? CompanyAddressLine1 { get; set; }
        [JsonProperty("CompanyAddressLine2")]
        public string? CompanyAddressLine2 { get; set; }
        [JsonProperty("CompanyAddressLine3")]
        public string? CompanyAddressLine3 { get; set; }
        [JsonProperty("ContactEmail", Required = Required.Always)]
        public required string ContactEmail { get; set; }
        [JsonProperty("ContactPhone", Required = Required.Always)]
        public required string ContactPhone { get; set; }
        [JsonProperty("BankName", Required = Required.Always)]
        public required string BankName { get; set; }
        [JsonProperty("BankAccountNumber", Required = Required.Always)]
        public required string BankAccountNumber { get; set; }
        [JsonProperty("BankAccountHolder", Required = Required.Always)]
        public required string BankAccountHolder { get; set; }
        [JsonProperty("BankRoutingNumber", Required = Required.Always)]
        public required string BankRoutingNumber { get; set; }
        [JsonProperty("BankSwift")]
        public string? BankSwift { get; set; }
        [JsonProperty("ClientName", Required = Required.Always)]
        public required string ClientName { get; set; }
        [JsonProperty("ClientAddress1")]
        public string? ClientAddress1 { get; set; }
        [JsonProperty("ClientAddress2")]
        public string? ClientAddress2 { get; set; }
        [JsonProperty("ClientAddress3")]
        public string? ClientAddress3 { get; set; }
        [JsonProperty("ClientEmailAddress", Required = Required.Always)]
        public required string ClientEmailAddress { get; set; }
        [JsonProperty("ClientNumber", Required = Required.Always)]
        public required string ClientNumber { get; set; }
        [JsonProperty("CurrencySymbol", Required = Required.Always)]
        public required string CurrencySymbol { get; set; }
        [JsonProperty("HourlyRate", Required = Required.Always)]
        public required float HourlyRate { get; set; }
        [JsonProperty("OutputPath", Required = Required.Always)]
        public required string OutputPath { get; set; }
        [JsonProperty("InvoiceNumber")]
        public string? InvoiceNumber { get; set; }
    }

// Removed duplicate ConstantLineItem class. Use the one in ConstantLineItem.cs

    public class InvoiceStyle
    {
        [JsonProperty("PrimaryColor", Required = Required.Always)]
        public required string PrimaryColor { get; set; }
        [JsonProperty("SecondaryColor", Required = Required.Always)]
        public required string SecondaryColor { get; set; }
        [JsonProperty("AccentColor", Required = Required.Always)]
        public required string AccentColor { get; set; }
        [JsonProperty("BackgroundColor", Required = Required.Always)]
        public required string BackgroundColor { get; set; }
        [JsonProperty("TextColor", Required = Required.Always)]
        public required string TextColor { get; set; }
        [JsonProperty("TableHeaderBg", Required = Required.Always)]
        public required string TableHeaderBg { get; set; }
        [JsonProperty("TableBorder", Required = Required.Always)]
        public required string TableBorder { get; set; }
        [JsonProperty("SoftHeadingBg", Required = Required.Always)]
        public required string SoftHeadingBg { get; set; }
        [JsonProperty("SoftAltRowBg", Required = Required.Always)]
        public required string SoftAltRowBg { get; set; }
        [JsonProperty("SectionBg", Required = Required.Always)]
        public required string SectionBg { get; set; }
        [JsonProperty("SectionText", Required = Required.Always)]
        public required string SectionText { get; set; }
    }
}
