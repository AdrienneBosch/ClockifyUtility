using System.Windows;
using System.Windows.Controls;

namespace ClockifyUtility.Controls
{
    public class PaletteComboBox : ComboBox
    {
        static PaletteComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PaletteComboBox), new FrameworkPropertyMetadata(typeof(PaletteComboBox)));
        }
    }
}
