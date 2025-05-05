using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MindMap.Behavior
{
    internal class DragThumbBehavior : Behavior<Thumb>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(DragThumbBehavior));

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.DragDelta += OnDragDelta;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.DragDelta -= OnDragDelta;
        }

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            Command?.Execute(new Vector(e.HorizontalChange, e.VerticalChange));
        }
    }
}
