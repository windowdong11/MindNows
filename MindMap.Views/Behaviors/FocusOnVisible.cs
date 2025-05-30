using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Threading;

namespace MindMap.Views.Behaviors;

public static class FocusOnVisible
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(FocusOnVisible),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(UIElement element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static bool GetIsEnabled(UIElement element) =>
        (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBox tb && (bool)e.NewValue)
        {
            tb.IsVisibleChanged += (s, ev) =>
            {
                if (tb.IsVisible && tb.IsEnabled)
                {
                    // Dispatcher로 한 프레임 뒤에 포커스 (UI 렌더 후)
                    tb.Dispatcher.BeginInvoke(() =>
                    {
                        tb.Focus();
                        tb.SelectAll(); // 전체 선택(선택적)
                    }, DispatcherPriority.Input);
                }
            };
        }
    }
}
