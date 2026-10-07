using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Win32;
using ProjectMannger.Services;
using TM = ProjectMannger.Helpers.ThemeManager;

namespace ProjectMannger.Views
{
    /// <summary>
    /// 设置窗口 - 数据配置界面
    /// </summary>
    public class SettingsWindow : Window
    {
        private TextBox _dataPathBox;
        private readonly DataService _dataService = DataService.Instance;

        public SettingsWindow()
        {
            InitializeUI();
            LoadSettings();
        }

        private void InitializeUI()
        {
            Title = "设置";
            Width = 520;
            Height = 280;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            var mainBorder = new Border
            {
                Background = TM.Brush(TM.DetailBg),
                CornerRadius = new CornerRadius(8),
                BorderBrush = TM.Brush(TM.CardBorder),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 30, Opacity = 0.08, ShadowDepth = 0, Color = Colors.Black
                }
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 标题栏
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 内容
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 底部按钮

            // === 标题栏 ===
            var titleBar = new Grid
            {
                Height = 48
            };
            titleBar.MouseLeftButtonDown += (s, e) => DragMove();

            var titleBarBg = new Border
            {
                Background = TM.Brush(TM.Accent),
                CornerRadius = new CornerRadius(8, 8, 0, 0)
            };
            titleBar.Children.Add(titleBarBg);

            var titleContent = new DockPanel
            {
                Margin = new Thickness(20, 0, 12, 0)
            };

            var closeBtn = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = new TextBlock
                {
                    Text = "✕",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            closeBtn.MouseLeftButtonDown += (s, e) => Close();
            closeBtn.MouseEnter += (s, e) =>
            {
                ((Border)s).Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            };
            closeBtn.MouseLeave += (s, e) =>
            {
                ((Border)s).Background = Brushes.Transparent;
            };
            DockPanel.SetDock(closeBtn, Dock.Right);
            titleContent.Children.Add(closeBtn);

            var titleText = new TextBlock
            {
                Text = "设置",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleContent.Children.Add(titleText);

            titleBar.Children.Add(titleContent);
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // === 内容区：数据路径设置 ===
            var contentPanel = new StackPanel
            {
                Margin = new Thickness(24, 20, 24, 12)
            };

            // 标题
            var sectionTitle = new TextBlock
            {
                Text = "数据存储",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = TM.Brush(TM.TextPrimary),
                Margin = new Thickness(0, 0, 0, 16)
            };
            contentPanel.Children.Add(sectionTitle);

            // 路径设置卡片
            var pathCard = new Border
            {
                Background = TM.Brush(TM.CardBg),
                CornerRadius = new CornerRadius(8),
                BorderBrush = TM.Brush(TM.CardBorder),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12)
            };

            var pathStack = new StackPanel();

            var pathLabel = new TextBlock
            {
                Text = "数据存储路径",
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = TM.Brush(TM.TextPrimary),
                Margin = new Thickness(0, 0, 0, 4)
            };
            pathStack.Children.Add(pathLabel);

            var pathDesc = new TextBlock
            {
                Text = "项目数据、待办事项和活动记录的存储位置",
                FontSize = 12,
                Foreground = TM.Brush(TM.TextPlaceholder),
                Margin = new Thickness(0, 0, 0, 10)
            };
            pathStack.Children.Add(pathDesc);

            var pathRow = new Grid();
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            pathRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _dataPathBox = new TextBox
            {
                FontSize = 12,
                Padding = new Thickness(10, 6, 10, 6),
                VerticalAlignment = VerticalAlignment.Center,
                IsReadOnly = true,
                Background = TM.Brush(TM.InputBg),
                Foreground = TM.Brush(TM.TextSecondary),
                BorderBrush = TM.Brush(TM.BorderDefault),
                BorderThickness = new Thickness(1)
            };
            Grid.SetColumn(_dataPathBox, 0);
            pathRow.Children.Add(_dataPathBox);

            var browseBtn = new Button
            {
                Content = "更改路径",
                Width = 80,
                Height = 30,
                Margin = new Thickness(8, 0, 0, 0),
                FontSize = 12,
                Cursor = Cursors.Hand,
                Background = TM.Brush(TM.Accent),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            browseBtn.Click += BrowseButton_Click;
            Grid.SetColumn(browseBtn, 1);
            pathRow.Children.Add(browseBtn);

            pathStack.Children.Add(pathRow);
            pathCard.Child = pathStack;
            contentPanel.Children.Add(pathCard);

            Grid.SetRow(contentPanel, 1);
            mainGrid.Children.Add(contentPanel);

            // === 底部按钮 ===
            var bottomBar = new Border
            {
                Padding = new Thickness(24, 12, 24, 16),
                BorderBrush = TM.Brush(TM.Divider),
                BorderThickness = new Thickness(0, 1, 0, 0)
            };

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var closeBtnBottom = new Button
            {
                Content = "关闭",
                Width = 80,
                Height = 32,
                Cursor = Cursors.Hand,
                FontSize = 13,
                BorderThickness = new Thickness(1),
                Background = TM.Brush(TM.SectionBg),
                Foreground = TM.Brush(TM.TextSecondary),
                BorderBrush = TM.Brush(TM.BorderDefault)
            };
            closeBtnBottom.Click += (s, e) => DialogResult = true;
            btnPanel.Children.Add(closeBtnBottom);

            bottomBar.Child = btnPanel;
            Grid.SetRow(bottomBar, 2);
            mainGrid.Children.Add(bottomBar);

            mainBorder.Child = mainGrid;
            Content = mainBorder;
        }

        private void LoadSettings()
        {
            _dataPathBox.Text = _dataService.DataPath;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择数据存储路径",
                SelectedPath = _dataService.DataPath,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var newPath = dialog.SelectedPath;
                
                try
                {
                    _dataService.UpdateDataPath(newPath);
                    _dataPathBox.Text = newPath;
                    
                    MessageBox.Show("数据路径已更新，重启应用后生效。", "设置已保存", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"更新路径失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
