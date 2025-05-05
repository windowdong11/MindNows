using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MindMap.Behavior
{
    internal class EscKeyToCommandBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(EscKeyToCommandBehavior));

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
            if (e.Key == Key.Escape && Command?.CanExecute(null) == true)
            {
                Command.Execute(null);
                e.Handled = true; // 이벤트 소모

                // 실제 해제
                var parent = Window.GetWindow(AssociatedObject);
                if (parent != null)
                {
                    // 빈 UIElement로 포커스 이동
                    var scope = FocusManager.GetFocusScope(parent);
                    FocusManager.SetFocusedElement(scope, parent);
                }
            }
        }
    }
}
