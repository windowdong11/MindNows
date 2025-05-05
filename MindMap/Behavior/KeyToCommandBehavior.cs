using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace MindMap.Behavior
{
    internal class KeyToCommandBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register(nameof(Key), typeof(Key), typeof(KeyToCommandBehavior));

        public Key Key
        {
            get => (Key)GetValue(KeyProperty);
            set => SetValue(KeyProperty, value);
        }
        public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(KeyToCommandBehavior));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key && Keyboard.Modifiers == Modifiers && Command?.CanExecute(null) == true)
            {
                Command.Execute(null);
                e.Handled = true;
            }
        }
    }
}
