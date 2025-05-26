using System.Windows;
using System.Windows.Controls;

namespace ClockifyUtility.Views
{
	public partial class ClockifyIdDialog : Window
	{
		public ClockifyIdDialog ( string userId, List<Models.WorkspaceInfo> workspaces, string invoiceFileName )
		{
			InitializeComponent ( );
			UserIdBox.Text = userId;
			WorkspaceList.ItemsSource = workspaces;

			// Set InvoiceFileNameText after the window is loaded to ensure the element exists
			this.Loaded += (s, e) => {
				var invoiceTextBlock = this.FindName("InvoiceFileNameText") as TextBlock;
				if (invoiceTextBlock != null)
					invoiceTextBlock.Text = $"Invoice config: {invoiceFileName}";
			};
		}

		private void CopyUserId_Click ( object sender, RoutedEventArgs e )
		{
			Clipboard.SetText ( UserIdBox.Text );
		}

		private void CopyWorkspaceId_Click ( object sender, RoutedEventArgs e )
		{
			if ( sender is Button btn && btn.CommandParameter is string id )
			{
				Clipboard.SetText ( id );
			}
		}
	}
}