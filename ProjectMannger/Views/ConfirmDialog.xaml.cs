using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProjectMannger.Views
{
    /// <summary>
    /// 确认对话框（无输入框，仅确认/取消）
    /// </summary>
    public class ConfirmDialog : Window
    {
        public ConfirmDialog(string message, string title)
        {
            Title = title;
            Width = 420;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            var mainBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 30, Opacity = 0.08, ShadowDepth = 0, Color = Colors.Black
                }
            };

            var stack = new StackPanel { Margin = new Thickness(24) };

            // 标题
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(titleBlock);

            // 提示信息
            var msgBlock = new TextBlock
            {
                Text = message,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                Margin = new Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(msgBlock);

            // 按钮区
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };

            var cancelBtn = new Button
            {
                Content = "取消",
                Width = 80,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(241, 245, 249)),
                Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            cancelBtn.Click += (s, e) => DialogResult = false;
            btnPanel.Children.Add(cancelBtn);

            var okBtn = new Button
            {
                Content = "确定",
                Width = 80,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            okBtn.Click += (s, e) => DialogResult = true;
            btnPanel.Children.Add(okBtn);

            stack.Children.Add(btnPanel);
            mainBorder.Child = stack;
            Content = mainBorder;

            // 支持 Esc 取消，Enter 确认
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                    DialogResult = false;
                else if (e.Key == Key.Enter)
                    DialogResult = true;
            };
        }
    }
}