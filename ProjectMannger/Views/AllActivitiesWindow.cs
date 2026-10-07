using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProjectMannger.Models;
using TM = ProjectMannger.Helpers.ThemeManager;

namespace ProjectMannger.Views
{
    /// <summary>
    /// 全部活动记录窗口
    /// </summary>
    public class AllActivitiesWindow : Window
    {
        public AllActivitiesWindow(List<ActivityItem> activities)
        {
            Title = "全部更新记录";
            Width = 700;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            var mainBorder = new Border
            {
                Background = TM.Brush(TM.CardBg),
                CornerRadius = new CornerRadius(8),
                BorderBrush = TM.Brush(TM.CardBorder),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 30, Opacity = 0.08, ShadowDepth = 0, Color = Colors.Black
                }
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 标题栏
            var titleBar = new Grid
            {
                Margin = new Thickness(24, 20, 24, 0)
            };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleBlock = new TextBlock
            {
                Text = "全部更新记录",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = TM.Brush(TM.TextSecondary),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleBlock, 0);
            titleBar.Children.Add(titleBlock);

            var closeBtn = new Button
            {
                Content = "✕",
                Width = 28,
                Height = 28,
                FontSize = 14,
                Background = Brushes.Transparent,
                Foreground = TM.Brush(TM.TextPlaceholder),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            closeBtn.Click += (s, e) => Close();
            Grid.SetColumn(closeBtn, 1);
            titleBar.Children.Add(closeBtn);

            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // 活动列表（带滚动条）
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(24, 16, 24, 24)
            };

            var activityStack = new StackPanel();

            if (activities.Count == 0)
            {
                activityStack.Children.Add(new TextBlock
                {
                    Text = "暂无更新记录",
                    FontSize = 13,
                    Foreground = TM.Brush(TM.TextPlaceholder),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
            }
            else
            {
                foreach (var activity in activities)
                {
                    var cardBorder = new Border
                    {
                        Background = TM.Brush(TM.SectionBg),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 8)
                    };

                    var mainStack = new StackPanel();

                    // 主要信息行
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 80 });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    grid.Children.Add(new TextBlock
                    {
                        Text = activity.Time,
                        FontSize = 12,
                        Foreground = TM.Brush(TM.TextPlaceholder),
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    var nameBlock = new TextBlock
                    {
                        Text = activity.ProjectName,
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = TM.Brush(TM.TextSecondary),
                        Margin = new Thickness(0, 0, 8, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(nameBlock, 1);
                    grid.Children.Add(nameBlock);

                    var contentBlock = new TextBlock
                    {
                        Text = activity.Content,
                        FontSize = 12,
                        Foreground = TM.Brush(TM.TextTertiary),
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(contentBlock, 2);
                    grid.Children.Add(contentBlock);

                    mainStack.Children.Add(grid);

                    // 如果有详情，直接显示在下方
                    if (!string.IsNullOrEmpty(activity.Detail))
                    {
                        var detailBlock = new TextBlock
                        {
                            Text = activity.Detail,
                            FontSize = 11,
                            Foreground = TM.Brush(TM.TextPlaceholder),
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(80, 6, 0, 0),
                            LineHeight = 18
                        };
                        mainStack.Children.Add(detailBlock);
                    }

                    cardBorder.Child = mainStack;
                    activityStack.Children.Add(cardBorder);
                }
            }

            scrollViewer.Content = activityStack;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            mainBorder.Child = mainGrid;
            Content = mainBorder;

            // 支持拖拽移动
            MouseLeftButtonDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                    DragMove();
            };
        }
    }
}
