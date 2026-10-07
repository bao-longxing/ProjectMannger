using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProjectMannger.Views
{
    /// <summary>
    /// 字段定义
    /// </summary>
    public class FieldDefinition
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public string Value { get; set; }
        public FieldType Type { get; set; } = FieldType.Text;
        public List<string> Options { get; set; } // 用于 ComboBox 类型
        public int Lines { get; set; } = 1; // 用于多行文本
    }

    public enum FieldType
    {
        Text,
        ComboBox,
        MultiLine
    }

    /// <summary>
    /// 多字段编辑对话框
    /// </summary>
    public class MultiFieldDialog : Window
    {
        public Dictionary<string, string> Results { get; private set; } = new Dictionary<string, string>();
        private Dictionary<string, Control> _fieldControls = new Dictionary<string, Control>();

        public MultiFieldDialog(string title, List<FieldDefinition> fields)
        {
            Title = title;
            Width = 460;
            SizeToContent = SizeToContent.Height;
            MaxHeight = 600;
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

            var mainStack = new StackPanel { Margin = new Thickness(24) };

            // 标题
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainStack.Children.Add(titleBlock);

            // 字段
            foreach (var field in fields)
            {
                var fieldPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };

                // 标签
                var label = new TextBlock
                {
                    Text = field.Label,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                fieldPanel.Children.Add(label);

                Control inputControl;

                switch (field.Type)
                {
                    case FieldType.ComboBox:
                        var comboBox = new ComboBox
                        {
                            ItemsSource = field.Options,
                            SelectedItem = field.Value,
                            Style = (Style)FindResource("ModernComboBox"),
                            ItemContainerStyle = (Style)FindResource("ModernComboBoxItem")
                        };
                        inputControl = comboBox;
                        break;

                    case FieldType.MultiLine:
                        var multiTextBox = new TextBox
                        {
                            Text = field.Value,
                            FontSize = 13,
                            Padding = new Thickness(10, 8, 10, 8),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                            BorderThickness = new Thickness(1),
                            TextWrapping = TextWrapping.Wrap,
                            AcceptsReturn = true,
                            MinHeight = 60,
                            MaxHeight = 120,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                        };
                        inputControl = multiTextBox;
                        break;

                    default: // Text
                        var textBox = new TextBox
                        {
                            Text = field.Value,
                            FontSize = 13,
                            Padding = new Thickness(10, 8, 10, 8),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(226, 232, 240)),
                            BorderThickness = new Thickness(1)
                        };
                        inputControl = textBox;
                        break;
                }

                _fieldControls[field.Key] = inputControl;
                fieldPanel.Children.Add(inputControl);
                mainStack.Children.Add(fieldPanel);
            }

            // 按钮区
            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 8, 0, 0)
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
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            okBtn.Click += (s, e) =>
            {
                Results = CollectValues(fields);
                DialogResult = true;
            };
            btnPanel.Children.Add(okBtn);

            mainStack.Children.Add(btnPanel);
            mainBorder.Child = mainStack;
            Content = mainBorder;

            // 焦点设到第一个输入框
            Loaded += (s, e) =>
            {
                if (_fieldControls.Count > 0)
                {
                    var firstControl = _fieldControls.Values.GetEnumerator();
                    firstControl.MoveNext();
                    firstControl.Current?.Focus();
                }
            };
        }

        private Dictionary<string, string> CollectValues(List<FieldDefinition> fields)
        {
            var results = new Dictionary<string, string>();
            foreach (var field in fields)
            {
                if (_fieldControls.TryGetValue(field.Key, out var control))
                {
                    switch (control)
                    {
                        case ComboBox combo:
                            results[field.Key] = combo.SelectedItem?.ToString() ?? "";
                            break;
                        case TextBox text:
                            results[field.Key] = text.Text;
                            break;
                    }
                }
            }
            return results;
        }
    }
}
