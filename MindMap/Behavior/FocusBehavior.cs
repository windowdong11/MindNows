using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using System.Windows;

namespace MindMap.Behavior
{
    internal class FocusBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.Register(nameof(IsFocused), typeof(bool), typeof(FocusBehavior),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsFocusedChanged));

        public bool IsFocused
        {
            get => (bool)GetValue(IsFocusedProperty);
            set => SetValue(IsFocusedProperty, value);
        }

        private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FocusBehavior behavior && behavior.AssociatedObject != null)
            {
                var textBox = behavior.AssociatedObject;
                bool isEditMode = behavior.IsFocused;
                textBox.IsReadOnly = !isEditMode;
                textBox.Focusable = isEditMode;
                textBox.IsHitTestVisible = isEditMode;
                if (isEditMode)
                {
                    behavior.AssociatedObject.Focus();
                    //behavior.AssociatedObject.SelectAll(); // 전체 선택
                }
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            OnIsFocusedChanged(this, new DependencyPropertyChangedEventArgs(IsFocusedProperty, null, IsFocused));
        }
    }
}
