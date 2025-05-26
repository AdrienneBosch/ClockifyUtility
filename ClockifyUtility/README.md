
# Clockify Utility

A WPF application for generating developer invoices from Clockify time entries. This tool helps automate invoice creation using your tracked time, with support for custom invoice templates and PDF export.

## Getting Started

### 1. Prerequisites

- .NET 9 SDK
- A Clockify account

### 2. Clone and Build

Clone this repository and open the solution in Visual Studio or your preferred .NET IDE. Build the solution to restore dependencies.

### 3. Configure App Settings

The application requires an `appsettings.json` file in the same directory as the executable. This file should specify the directory containing your invoice configuration files.

**Template files are provided within the `Templates` folder:**

- `appsettings.template.json`
- `AppSettings.template.invoice.json`

Copy and rename a template to `appsettings.json` and edit as needed.

Example `appsettings.json`:

```

{
"InvoiceConfigDirectory": "C:/Path/To/Your/InvoiceConfigs",
"DefaultInvoice": "my-invoice.json",
"InvoiceNumber": "001"
}

```

- `InvoiceConfigDirectory`: Path to the folder containing your invoice config files (one per client/project).
- `DefaultInvoice`: (Optional) The default invoice config file to use.
- `InvoiceNumber`: (Optional) The starting invoice number (auto-incremented).

### 4. Create Invoice Config Files

Each invoice config file (e.g., `my-invoice.json`) should include your Clockify API key, User ID, Workspace ID, client info, and invoice styling.

**A template invoice config is provided:**

- `AppSettings.template.invoice.json`

Copy and rename this file for each client/project, then edit the details.

Example invoice config:

```

{
"Clockify": {
"ClockifyApiKey": "YOUR\_API\_KEY",
"UserId": "YOUR\_USER\_ID",
"WorkspaceId": "YOUR\_WORKSPACE\_ID",
"ClientName": "Client Name",
"ClientEmailAddress": "[client@email.com](mailto:client@email.com)",
"FromName": "Your Name",
"CompanyAddressLine1": "123 Main St",
"ContactEmail": "[your@email.com](mailto:your@email.com)",
"HourlyRate": 100.0,
"CurrencySymbol": "\$"
},
"InvoiceStyle": {
"PrimaryColor": "#2C3E50"
}
}

```

#### How to Get Your Clockify API Key

1. Log in to your Clockify account.
2. Click your profile icon and go to **Preferences**.
3. Go to the **Advanced** tab.
4. Click **Generate** to create your API key.
5. Copy and paste the API key into your invoice config file.

#### How to Get Your User ID and Workspace ID

If you run the app without these values, a dialog will appear showing your User ID and available workspaces. Copy these values into your invoice config file and restart the app.

### 5. Run the Application

- Start the app. On first run, it will validate your config files.
- Select the invoice config and month, then click **Generate Invoice**.
- To generate invoices for all configs in the folder, select **All** and click **Generate Invoice**. The app will process every invoice config in the directory.
- To set a default invoice config, select the desired config and press the **star button**. The selected config will become the default for future runs.
- The app will fetch your time entries, generate an invoice, and save it as a PDF in the output directory.

## Troubleshooting

- If you see a dialog about missing User ID or Workspace ID, follow the instructions to update your config file.
- Make sure your API key is valid and you have internet access.

