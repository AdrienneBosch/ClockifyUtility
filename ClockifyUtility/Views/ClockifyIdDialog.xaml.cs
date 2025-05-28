using System.Windows;
using System.Windows.Controls;

namespace ClockifyUtility.Views
{
	public partial class ClockifyIdDialog : Window
	{
		public ClockifyIdDialog ( string userId, List<Models.WorkspaceInfo> workspaces, string invoiceFileName, Window? owner = null )
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

			if (owner != null)
			{
				this.Owner = owner;
				this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				this.Topmost = owner.IsActive; // Initialize Topmost based on owner's current activity state
				// Only set Topmost to true while the owner window is active
				owner.Activated += (s, e) => this.Topmost = true;
				owner.Deactivated += (s, e) => this.Topmost = false;
				this.Closed += (s, e) => {
					owner.Activated -= (s2, e2) => this.Topmost = true;
					owner.Deactivated -= (s2, e2) => this.Topmost = false;
				};
			}
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