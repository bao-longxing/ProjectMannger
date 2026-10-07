using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ProjectMannger.Models;
using TM = ProjectMannger.Helpers.ThemeManager;

namespace ProjectMannger.Views
{
    /// <summary>
    /// 历史项目窗口 - 显示已完成的项目
    /// </summary>
    public class HistoryProjectsWindow : Window
    {
        private ListBox _projectList;
        private StackPanel _detailPanel;
        private List<ProjectModel> _completedProjects;

        public HistoryProjectsWindow(List<ProjectModel> allProjects)
        {
            _completedProjects = allProjects.Where(p => p.Stage == "已完成").ToList();
            InitializeUI();
        }

        private void InitializeUI()
        {
            Title = "历史项目";
            Width = 800;
            Height = 500;
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
                Text = $"历史项目 ({_completedProjects.Count})",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleContent.Children.Add(titleText);

            titleBar.Children.Add(titleContent);
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // === 内容区：左侧项目列表 + 右侧详情 ===
            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 左侧项目列表
            var listBorder = new Border
            {
                Background = TM.Brush(TM.SectionBg),
                BorderBrush = TM.Brush(TM.Divider),
                BorderThickness = new Thickness(0, 0, 1, 0)
            };

            _projectList = new ListBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 12, 8, 12)
            };

            if (_completedProjects.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "暂无已完成的项目",
                    FontSize = 13,
                    Foreground = TM.Brush(TM.TextPlaceholder),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                };
                _projectList.Items.Add(emptyText);
            }
            else
            {
                foreach (var project in _completedProjects)
                {
                    var item = new ListBoxItem
                    {
                        Padding = new Thickness(12, 10, 12, 10),
                        Margin = new Thickness(0, 0, 0, 4),
                        Cursor = Cursors.Hand,
                        Tag = project,
                        Content = CreateProjectListItem(project)
                    };
                    _projectList.Items.Add(item);
                }
            }

            _projectList.SelectionChanged += ProjectList_SelectionChanged;
            listBorder.Child = _projectList;
            Grid.SetColumn(listBorder, 0);
            contentGrid.Children.Add(listBorder);

            // 右侧详情面板
            var detailScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(20, 16, 20, 16)
            };

            _detailPanel = new StackPanel();
            detailScroll.Content = _detailPanel;
            Grid.SetColumn(detailScroll, 1);
            contentGrid.Children.Add(detailScroll);

            Grid.SetRow(contentGrid, 1);
            mainGrid.Children.Add(contentGrid);

            mainBorder.Child = mainGrid;
            Content = mainBorder;

            // 默认选中第一个
            if (_projectList.Items.Count > 0 && _completedProjects.Count > 0)
                _projectList.SelectedIndex = 0;
        }

        private StackPanel CreateProjectListItem(ProjectModel project)
        {
            var stack = new StackPanel();

            var nameText = new TextBlock
            {
                Text = project.Name,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = TM.Brush(TM.TextPrimary),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            stack.Children.Add(nameText);

            var dateText = new TextBlock
            {
                Text = $"完成于 {project.PlanEndDate}",
                FontSize = 11,
                Foreground = TM.Brush(TM.TextPlaceholder),
                Margin = new Thickness(0, 2, 0, 0)
            };
            stack.Children.Add(dateText);

            return stack;
        }

        private void ProjectList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_projectList.SelectedItem is ListBoxItem item && item.Tag is ProjectModel project)
            {
                ShowProjectDetail(project);
            }
        }

        private void ShowProjectDetail(ProjectModel project)
        {
            _detailPanel.Children.Clear();

            // 项目标题
            var headerStack = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 20)
            };

            var nameBlock = new TextBlock
            {
                Text = project.Name,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = TM.Brush(TM.TextPrimary)
            };
            headerStack.Children.Add(nameBlock);

            var subtitleBlock = new TextBlock
            {
                Text = project.Subtitle,
                FontSize = 13,
                Foreground = TM.Brush(TM.TextPlaceholder),
                Margin = new Thickness(0, 4, 0, 0)
            };
            headerStack.Children.Add(subtitleBlock);

            _detailPanel.Children.Add(headerStack);

            // 基本信息卡片
            var infoCard = new Border
            {
                Background = TM.Brush(TM.CardBg),
                CornerRadius = new CornerRadius(8),
                BorderBrush = TM.Brush(TM.CardBorder),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var infoStack = new StackPanel();
            infoStack.Children.Add(new TextBlock
            {
                Text = "基本信息",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = TM.Brush(TM.TextSecondary),
                Margin = new Thickness(0, 0, 0, 10)
            });
            var infoGrid = new Grid();
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            infoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            infoGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddInfoRow(infoGrid, 0, "项目类型", project.ProjectType);
            AddInfoRow(infoGrid, 1, "优先级", project.Priority);
            AddInfoRow(infoGrid, 2, "开始日期", project.StartDate);
            AddInfoRow(infoGrid, 3, "完成日期", project.PlanEndDate);

            infoStack.Children.Add(infoGrid);
            infoCard.Child = infoStack;
            _detailPanel.Children.Add(infoCard);

            // 描述卡片
            if (!string.IsNullOrEmpty(project.Description))
            {
                var descCard = new Border
                {
                    Background = TM.Brush(TM.CardBg),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = TM.Brush(TM.CardBorder),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                var descStack = new StackPanel();
                descStack.Children.Add(new TextBlock
                {
                    Text = "项目描述",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TM.Brush(TM.TextSecondary),
                    Margin = new Thickness(0, 0, 0, 10)
                });
                descStack.Children.Add(new TextBlock
                {
                    Text = project.Description,
                    FontSize = 13,
                    Foreground = TM.Brush(TM.TextTertiary),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20
                });
                descCard.Child = descStack;
                _detailPanel.Children.Add(descCard);
            }

            // 进度卡片
            var progressCard = new Border
            {
                Background = TM.Brush(TM.CardBg),
                CornerRadius = new CornerRadius(8),
                BorderBrush = TM.Brush(TM.CardBorder),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var progressStack = new StackPanel();
            progressStack.Children.Add(new TextBlock
            {
                Text = "完成进度",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = TM.Brush(TM.TextSecondary),
                Margin = new Thickness(0, 0, 0, 10)
            });

            var progressBar = new Grid
            {
                Height = 8,
                Margin = new Thickness(0, 0, 0, 8)
            };
            progressBar.Children.Add(new Border
            {
                Background = TM.Brush(TM.ProgressTrack),
                CornerRadius = new CornerRadius(4)
            });
            progressBar.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(16, 185, 129)),
                CornerRadius = new CornerRadius(4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 200 * project.Progress / 100
            });
            progressStack.Children.Add(progressBar);

            var progressText = new TextBlock
            {
                Text = $"{project.Progress}% ({project.CompletedTasks}/{project.TotalTasks} 任务)",
                FontSize = 12,
                Foreground = TM.Brush(TM.TextSecondary)
            };
            progressStack.Children.Add(progressText);

            progressCard.Child = progressStack;
            _detailPanel.Children.Add(progressCard);

            // 问题列表卡片
            if (project.Issues != null && project.Issues.Count > 0)
            {
                var issuesCard = new Border
                {
                    Background = TM.Brush(TM.CardBg),
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = TM.Brush(TM.CardBorder),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(16, 12, 16, 12),
                    Margin = new Thickness(0, 0, 0, 12)
                };
                var issuesStack = new StackPanel();
                issuesStack.Children.Add(new TextBlock
                {
                    Text = $"问题列表 ({project.Issues.Count})",
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TM.Brush(TM.TextSecondary),
                    Margin = new Thickness(0, 0, 0, 10)
                });

                foreach (var issue in project.Issues)
                {
                    var issueBorder = new Border
                    {
                        Background = TM.Brush(TM.InputBg),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(10, 6, 10, 6),
                        Margin = new Thickness(0, 0, 0, 4)
                    };
                    var issueText = new TextBlock
                    {
                        Text = issue.Title,
                        FontSize = 12,
                        Foreground = TM.Brush(TM.TextTertiary)
                    };
                    issueBorder.Child = issueText;
                    issuesStack.Children.Add(issueBorder);
                }

                issuesCard.Child = issuesStack;
                _detailPanel.Children.Add(issuesCard);
            }
        }

        private void AddInfoRow(Grid grid, int row, string label, string value)
        {
            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = TM.Brush(TM.TextPlaceholder),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);

            var valueText = new TextBlock
            {
                Text = value ?? "-",
                FontSize = 13,
                Foreground = TM.Brush(TM.TextPrimary),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);
        }
    }
}
