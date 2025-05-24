using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClockifyUtility.Controls
{
    public class PaletteComboBox : ComboBox
    {
        static PaletteComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PaletteComboBox), new FrameworkPropertyMetadata(typeof(PaletteComboBox)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("SelectedItemBackground") is Border border)
            {
                border.PreviewMouseLeftButtonDown -= ContentSite_PreviewMouseLeftButtonDown;
                border.PreviewMouseLeftButtonDown += ContentSite_PreviewMouseLeftButtonDown;
            }
            if (GetTemplateChild("ContentSite") is ContentPresenter presenter)
            {
                presenter.PreviewMouseLeftButtonDown -= ContentSite_PreviewMouseLeftButtonDown;
            }
        }

        private void ContentSite_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsEnabled)
            {
                Focus();
                IsDropDownOpen = !IsDropDownOpen;
                e.Handled = true;
            }
        }
    }
}
