using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ClockifyUtility.Views
{
    public partial class ClockifyIdDialog : Window
    {
        public ClockifyIdDialog(string userId, List<Models.WorkspaceInfo> workspaces)
        {
            InitializeComponent();
            UserIdBox.Text = userId;
            WorkspaceList.ItemsSource = workspaces;
        }

        private void CopyUserId_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(UserIdBox.Text);
        }

        private void CopyWorkspaceId_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string id)
                Clipboard.SetText(id);
        }
    }
}
