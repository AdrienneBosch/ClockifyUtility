using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClockifyUtility.Controls
{
    public class CustomComboBox : ComboBox
    {
        static CustomComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CustomComboBox), new FrameworkPropertyMetadata(typeof(CustomComboBox)));
        }

        public Brush DropDownBackground
        {
            get => (Brush)GetValue(DropDownBackgroundProperty);
            set => SetValue(DropDownBackgroundProperty, value);
        }
        public static readonly DependencyProperty DropDownBackgroundProperty =
            DependencyProperty.Register(nameof(DropDownBackground), typeof(Brush), typeof(CustomComboBox), new PropertyMetadata(Brushes.White));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(CustomComboBox), new PropertyMetadata(new CornerRadius(3)));

        public Brush MouseOverBackground
        {
            get => (Brush)GetValue(MouseOverBackgroundProperty);
            set => SetValue(MouseOverBackgroundProperty, value);
        }
        public static readonly DependencyProperty MouseOverBackgroundProperty =
            DependencyProperty.Register(nameof(MouseOverBackground), typeof(Brush), typeof(CustomComboBox), new PropertyMetadata(Brushes.LightGray));

        public Brush PressedBackground
        {
            get => (Brush)GetValue(PressedBackgroundProperty);
            set => SetValue(PressedBackgroundProperty, value);
        }
        public static readonly DependencyProperty PressedBackgroundProperty =
            DependencyProperty.Register(nameof(PressedBackground), typeof(Brush), typeof(CustomComboBox), new PropertyMetadata(Brushes.Gray));

        public Brush DisabledBackground
        {
            get => (Brush)GetValue(DisabledBackgroundProperty);
            set => SetValue(DisabledBackgroundProperty, value);
        }
        public static readonly DependencyProperty DisabledBackgroundProperty =
            DependencyProperty.Register(nameof(DisabledBackground), typeof(Brush), typeof(CustomComboBox), new PropertyMetadata(Brushes.DarkGray));
    }
}
