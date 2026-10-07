using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using ProjectMannger.Models;
using TM = ProjectMannger.Helpers.ThemeManager;

namespace ProjectMannger.Views
{
    /// <summary>
    /// 项目编辑对话框 - 统一编辑所有项目属性
    /// 重新设计：支持主题、双列布局、圆环进度预览
    /// </summary>
    public class ProjectEditDialog : Window
    {
        public ProjectModel EditedProject { get; private set; }
        public List<ProjectIssue> EditedIssues { get; private set; }

        private ProjectModel _originalProject;

        // 基本信息控件
        private TextBox _subtitleBox;
        private ComboBox _projectTypeCombo;
        private TextBox _startDateBox;
        private TextBox _planEndDateBox;
        private ComboBox _priorityCombo;
        private TextBox _descriptionBox;

        // 任务和计划控件
        private TextBox _currentTaskBox;
        private TextBox _nextPlanBox;

        // 进度控件
        private ComboBox _stageCombo;
        private Slider _progressSlider;
        private TextBlock _progressPercentText;
        private Ellipse _progressTrackEllipse;
        private Ellipse _progressFillEllipse;
        private TextBlock _progressCenterText;

        // 问题列表
        private ItemsControl _issuesList;
        private List<IssueItem> _issueItems = new List<IssueItem>();

        // 主题相关
        private Border _mainBorder;
        private Border _titleBarBorder;
        private TextBlock _titleText;

        // 用于跟踪需要主题更新的控件
        private readonly List<Action> _themeUpdateActions = new List<Action>();

        // 问题数据项
        private class IssueItem
        {
            public string Id { get; set; }
            public TextBox TitleBox { get; set; }
            public Border CardBorder { get; set; }
            public string Status { get; set; } = "待处理";
        }

        public ProjectEditDialog(ProjectModel project)
        {
            _originalProject = project;
            InitializeUI();
            LoadProjectData();
            TM.ThemeChanged += OnThemeChanged;
            Closed += (s, e) => TM.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged()
        {
            foreach (var action in _themeUpdateActions)
                action.Invoke();
        }

        private void InitializeUI()
        {
            Title = $"编辑项目 - {_originalProject.Name}";
            Width = 720;
            Height = 780;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            // 主容器
            _mainBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };
            _themeUpdateActions.Add(() =>
            {
                _mainBorder.Background = TM.Brush(TM.DetailBg);
                _mainBorder.BorderBrush = TM.Brush(TM.CardBorder);
            });

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 标题栏
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 内容
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 按钮

            // === 标题栏（可拖拽）===
            _titleBarBorder = new Border
            {
                Height = 48,
                Padding = new Thickness(20, 0, 12, 0)
            };
            _themeUpdateActions.Add(() =>
            {
                _titleBarBorder.Background = TM.Brush(TM.Accent);
            });
            _titleBarBorder.MouseLeftButtonDown += (s, e) => DragMove();

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _titleText = new TextBlock
            {
                Text = $"编辑项目 - {_originalProject.Name}",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(_titleText, 0);
            titleGrid.Children.Add(_titleText);

            // 关闭按钮
            var closeBtn = CreateWindowButton("✕");
            closeBtn.MouseLeftButtonDown += (s, e) => Close();
            closeBtn.MouseEnter += (s, e) =>
            {
                ((Border)s).Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            };
            closeBtn.MouseLeave += (s, e) =>
            {
                ((Border)s).Background = Brushes.Transparent;
            };
            Grid.SetColumn(closeBtn, 1);
            titleGrid.Children.Add(closeBtn);

            _titleBarBorder.Child = titleGrid;
            Grid.SetRow(_titleBarBorder, 0);
            mainGrid.Children.Add(_titleBarBorder);

            // === 内容区 ===
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(24, 20, 24, 12)
            };
            _themeUpdateActions.Add(() =>
            {
                scrollViewer.Background = Brushes.Transparent;
            });

            var contentStack = new StackPanel();

            // 基本信息卡片
            contentStack.Children.Add(CreateCard("基本信息", BuildBasicInfoContent));

            // 任务与计划卡片
            contentStack.Children.Add(CreateCard("任务与计划", BuildTaskPlanContent));

            // 进度卡片（特殊：带圆环预览）
            contentStack.Children.Add(CreateCard("进度", BuildProgressContent));

            // 问题卡片
            contentStack.Children.Add(CreateCard("问题列表", BuildIssuesContent));

            scrollViewer.Content = contentStack;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // === 底部按钮区 ===
            var btnBar = new Border
            {
                Padding = new Thickness(24, 12, 24, 20)
            };
            _themeUpdateActions.Add(() =>
            {
                btnBar.Background = TM.Brush(TM.DetailBg);
                btnBar.BorderBrush = TM.Brush(TM.Divider);
            });
            btnBar.BorderThickness = new Thickness(0, 1, 0, 0);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelBtn = new Button
            {
                Content = "取消",
                Width = 90,
                Height = 36,
                Margin = new Thickness(0, 0, 12, 0),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                FontSize = 13
            };
            _themeUpdateActions.Add(() =>
            {
                cancelBtn.Background = TM.Brush(TM.SectionBg);
                cancelBtn.Foreground = TM.Brush(TM.TextSecondary);
                cancelBtn.BorderBrush = TM.Brush(TM.BorderDefault);
            });
            cancelBtn.Click += (s, e) => DialogResult = false;
            btnPanel.Children.Add(cancelBtn);

            var okBtn = new Button
            {
                Content = "保存更改",
                Width = 100,
                Height = 36,
                Cursor = Cursors.Hand,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                BorderThickness = new Thickness(0)
            };
            _themeUpdateActions.Add(() =>
            {
                okBtn.Background = TM.Brush(TM.Accent);
                okBtn.Foreground = Brushes.White;
            });
            okBtn.Click += SaveButton_Click;
            btnPanel.Children.Add(okBtn);

            btnBar.Child = btnPanel;
            Grid.SetRow(btnBar, 2);
            mainGrid.Children.Add(btnBar);

            _mainBorder.Child = mainGrid;
            Content = _mainBorder;

            // 应用初始主题
            foreach (var action in _themeUpdateActions)
                action.Invoke();

            Loaded += (s, e) => _projectTypeCombo?.Focus();
        }

        private Border CreateWindowButton(string symbol)
        {
            var btn = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 6, 0, 6),
                Child = new TextBlock
                {
                    Text = symbol,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            btn.MouseLeftButtonDown += (s, e) =>
            {
                if (symbol == "✕") Close();
            };
            return btn;
        }

        // ===================== 卡片容器 =====================

        private Border CreateCard(string header, Func<UIElement> buildContent)
        {
            var cardBorder = new Border
            {
                CornerRadius = new CornerRadius(8),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 16),
                Padding = new Thickness(20, 16, 20, 20)
            };
            _themeUpdateActions.Add(() =>
            {
                cardBorder.Background = TM.Brush(TM.CardBg);
                cardBorder.BorderBrush = TM.Brush(TM.CardBorder);
            });

            var stack = new StackPanel();

            // 卡片标题
            var headerText = new TextBlock
            {
                Text = header,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 14)
            };
            _themeUpdateActions.Add(() =>
            {
                headerText.Foreground = TM.Brush(TM.TextPrimary);
            });
            stack.Children.Add(headerText);

            // 分隔线
            var divider = new Border { Height = 1, Margin = new Thickness(0, 0, 0, 14) };
            _themeUpdateActions.Add(() =>
            {
                divider.Background = TM.Brush(TM.Divider);
            });
            stack.Children.Add(divider);

            // 内容
            stack.Children.Add(buildContent());

            cardBorder.Child = stack;
            return cardBorder;
        }

        // ===================== 基本信息 =====================

        private UIElement BuildBasicInfoContent()
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 0)
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) }); // 间距
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 左列
            var leftStack = new StackPanel();
            Grid.SetColumn(leftStack, 0);

            // 项目名称（只读）
            leftStack.Children.Add(CreateFieldLabel("项目名称"));
            var nameDisplay = new TextBox
            {
                Text = _originalProject.Name,
                IsReadOnly = true,
                FontSize = 13,
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 0, 0, 14),
                BorderThickness = new Thickness(1)
            };
            _themeUpdateActions.Add(() =>
            {
                nameDisplay.Background = TM.Brush(TM.SectionBg);
                nameDisplay.Foreground = TM.Brush(TM.TextMuted);
                nameDisplay.BorderBrush = TM.Brush(TM.Divider);
            });
            leftStack.Children.Add(nameDisplay);

            // 项目副标题
            leftStack.Children.Add(CreateFieldLabel("项目副标题"));
            _subtitleBox = CreateStyledTextBox("输入副标题...");
            leftStack.Children.Add(_subtitleBox);

            // 项目类型
            leftStack.Children.Add(CreateFieldLabel("项目类型"));
            _projectTypeCombo = CreateStyledComboBox(
                new List<string> { "软件开发", "硬件开发", "视觉检测", "数据分析", "其他" });
            leftStack.Children.Add(_projectTypeCombo);

            // 优先级
            leftStack.Children.Add(CreateFieldLabel("优先级"));
            _priorityCombo = CreateStyledComboBox(new List<string> { "高", "中", "低" });
            leftStack.Children.Add(_priorityCombo);

            grid.Children.Add(leftStack);

            // 右列
            var rightStack = new StackPanel();
            Grid.SetColumn(rightStack, 2);

            // 开始日期
            rightStack.Children.Add(CreateFieldLabel("开始日期"));
            _startDateBox = CreateStyledTextBox("例：2024-01-15");
            rightStack.Children.Add(_startDateBox);

            // 计划完成日期
            rightStack.Children.Add(CreateFieldLabel("计划完成日期"));
            _planEndDateBox = CreateStyledTextBox("例：2024-06-30");
            rightStack.Children.Add(_planEndDateBox);

            // 描述
            rightStack.Children.Add(CreateFieldLabel("项目描述"));
            _descriptionBox = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(12, 9, 12, 9),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                MinHeight = 88,
                MaxHeight = 88,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 4),
                BorderThickness = new Thickness(1)
            };
            _themeUpdateActions.Add(() =>
            {
                _descriptionBox.Background = TM.Brush(TM.InputBg);
                _descriptionBox.Foreground = TM.Brush(TM.TextPrimary);
                _descriptionBox.BorderBrush = TM.Brush(TM.BorderDefault);
            });
            rightStack.Children.Add(_descriptionBox);

            grid.Children.Add(rightStack);
            return grid;
        }

        // ===================== 任务与计划 =====================

        private UIElement BuildTaskPlanContent()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 当前任务
            var leftStack = new StackPanel();
            Grid.SetColumn(leftStack, 0);
            leftStack.Children.Add(CreateFieldLabel("当前任务"));
            _currentTaskBox = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(12, 9, 12, 9),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                MinHeight = 100,
                MaxHeight = 100,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 4),
                BorderThickness = new Thickness(1)
            };
            _themeUpdateActions.Add(() =>
            {
                _currentTaskBox.Background = TM.Brush(TM.InputBg);
                _currentTaskBox.Foreground = TM.Brush(TM.TextPrimary);
                _currentTaskBox.BorderBrush = TM.Brush(TM.BorderDefault);
            });
            leftStack.Children.Add(_currentTaskBox);
            grid.Children.Add(leftStack);

            // 下一步计划
            var rightStack = new StackPanel();
            Grid.SetColumn(rightStack, 2);
            rightStack.Children.Add(CreateFieldLabel("下一步计划"));
            _nextPlanBox = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(12, 9, 12, 9),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                MinHeight = 100,
                MaxHeight = 100,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 4),
                BorderThickness = new Thickness(1)
            };
            _themeUpdateActions.Add(() =>
            {
                _nextPlanBox.Background = TM.Brush(TM.InputBg);
                _nextPlanBox.Foreground = TM.Brush(TM.TextPrimary);
                _nextPlanBox.BorderBrush = TM.Brush(TM.BorderDefault);
            });
            rightStack.Children.Add(_nextPlanBox);
            grid.Children.Add(rightStack);

            return grid;
        }

        // ===================== 进度 =====================

        private UIElement BuildProgressContent()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // 圆环区域
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 控件区域

            // 左侧：圆环进度预览
            var ringPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(ringPanel, 0);

            var ringSize = 90.0;
            var ringStroke = 8.0;
            var ringGrid = new Grid
            {
                Width = ringSize,
                Height = ringSize,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // 轨道
            _progressTrackEllipse = new Ellipse
            {
                StrokeThickness = ringStroke,
                Width = ringSize,
                Height = ringSize
            };
            _themeUpdateActions.Add(() =>
            {
                _progressTrackEllipse.Stroke = TM.Brush(TM.ProgressTrack);
            });
            ringGrid.Children.Add(_progressTrackEllipse);

            // 进度弧
            _progressFillEllipse = new Ellipse
            {
                Stroke = TM.Brush(TM.Accent),
                StrokeThickness = ringStroke,
                Width = ringSize,
                Height = ringSize,
                StrokeDashArray = new DoubleCollection { 0, 32.201 },
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(-90)
            };
            ringGrid.Children.Add(_progressFillEllipse);

            // 中心百分比
            _progressCenterText = new TextBlock
            {
                Text = "0%",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            _themeUpdateActions.Add(() =>
            {
                _progressCenterText.Foreground = TM.Brush(TM.TextPrimary);
            });
            ringGrid.Children.Add(_progressCenterText);

            ringPanel.Children.Add(ringGrid);

            // 进度文字标签
            var progressLabel = new TextBlock
            {
                Text = "完成进度",
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _themeUpdateActions.Add(() =>
            {
                progressLabel.Foreground = TM.Brush(TM.TextMuted);
            });
            ringPanel.Children.Add(progressLabel);
            grid.Children.Add(ringPanel);

            // 右侧：阶段 + 滑块
            var controlsStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(controlsStack, 2);

            // 当前阶段
            controlsStack.Children.Add(CreateFieldLabel("当前阶段"));
            _stageCombo = CreateStyledComboBox(
                new List<string> { "需求确认", "开发中", "测试中", "已完成" });
            controlsStack.Children.Add(_stageCombo);

            // 进度滑块
            var sliderLabel = new DockPanel
            {
                Margin = new Thickness(0, 14, 0, 8)
            };
            var sliderLabelText = new TextBlock
            {
                Text = "进度百分比",
                FontSize = 12
            };
            _themeUpdateActions.Add(() =>
            {
                sliderLabelText.Foreground = TM.Brush(TM.TextMuted);
            });
            DockPanel.SetDock(sliderLabelText, Dock.Left);
            sliderLabel.Children.Add(sliderLabelText);

            _progressPercentText = new TextBlock
            {
                Text = "0%",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            _themeUpdateActions.Add(() =>
            {
                _progressPercentText.Foreground = TM.Brush(TM.Accent);
            });
            sliderLabel.Children.Add(_progressPercentText);
            controlsStack.Children.Add(sliderLabel);

            _progressSlider = new Slider
            {
                Minimum = 0,
                Maximum = 100,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Value = 0,
                Height = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            _progressSlider.ValueChanged += ProgressSlider_ValueChanged;
            controlsStack.Children.Add(_progressSlider);

            // 快捷按钮
            var quickBtnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 6, 0, 0)
            };
            foreach (var pct in new[] { 0, 25, 50, 75, 100 })
            {
                var btn = new Button
                {
                    Content = $"{pct}%",
                    Width = 42,
                    Height = 26,
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 6, 0),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Tag = pct
                };
                var capturedPct = pct;
                _themeUpdateActions.Add(() =>
                {
                    btn.Background = TM.Brush(TM.SectionBg);
                    btn.Foreground = TM.Brush(TM.TextTertiary);
                    btn.BorderBrush = TM.Brush(TM.BorderDefault);
                });
                btn.Click += (s, e) => _progressSlider.Value = capturedPct;
                quickBtnPanel.Children.Add(btn);
            }
            controlsStack.Children.Add(quickBtnPanel);

            grid.Children.Add(controlsStack);
            return grid;
        }

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var val = (int)e.NewValue;
            _progressPercentText.Text = $"{val}%";
            _progressCenterText.Text = $"{val}%";

            // 更新圆环
            // 描边中心线半径 = (90 - 8) / 2 = 41
            double perimeter = 2 * Math.PI * 41;
            double strokeThickness = 8;
            double circumference = perimeter / strokeThickness;
            double progress = val / 100.0;
            _progressFillEllipse.StrokeDashArray = new DoubleCollection
            {
                circumference * progress,
                circumference * (1 - progress)
            };
        }

        // ===================== 问题列表 =====================

        private UIElement BuildIssuesContent()
        {
            var stack = new StackPanel();

            // 问题列表
            _issuesList = new ItemsControl
            {
                Margin = new Thickness(0, 0, 0, 12)
            };
            stack.Children.Add(_issuesList);

            // 添加问题按钮
            var addBtn = new Button
            {
                Content = "＋ 添加问题",
                Height = 34,
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _themeUpdateActions.Add(() =>
            {
                addBtn.Background = TM.Brush(TM.AccentLightBg);
                addBtn.Foreground = TM.Brush(TM.Accent);
                addBtn.BorderBrush = TM.Brush(TM.Accent, 60);
            });
            addBtn.Click += (s, e) => AddIssueItem("", "", "待处理");
            stack.Children.Add(addBtn);

            return stack;
        }

        private void AddIssueItem(string title, string description, string status)
        {
            var item = new IssueItem { Status = status };

            var cardBorder = new Border
            {
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };
            _themeUpdateActions.Add(() =>
            {
                cardBorder.Background = TM.Brush(TM.SectionBg);
                cardBorder.BorderBrush = TM.Brush(TM.Divider);
            });
            item.CardBorder = cardBorder;

            var cardGrid = new Grid();
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 问题标题输入
            item.TitleBox = new TextBox
            {
                Text = title,
                FontSize = 13,
                Padding = new Thickness(0, 2, 0, 2),
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            _themeUpdateActions.Add(() =>
            {
                item.TitleBox.Background = Brushes.Transparent;
                item.TitleBox.Foreground = TM.Brush(TM.TextPrimary);
            });
            Grid.SetColumn(item.TitleBox, 0);
            cardGrid.Children.Add(item.TitleBox);

            // 删除按钮
            var deleteBtn = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "✕",
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            _themeUpdateActions.Add(() =>
            {
                ((TextBlock)deleteBtn.Child).Foreground = TM.Brush(TM.TextPlaceholder);
            });
            deleteBtn.MouseEnter += (s, e) =>
            {
                deleteBtn.Background = new SolidColorBrush(Color.FromArgb(20, 239, 68, 68));
                ((TextBlock)deleteBtn.Child).Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            };
            deleteBtn.MouseLeave += (s, e) =>
            {
                deleteBtn.Background = Brushes.Transparent;
                ((TextBlock)deleteBtn.Child).Foreground = TM.Brush(TM.TextPlaceholder);
            };
            deleteBtn.MouseLeftButtonDown += (s, e) =>
            {
                _issueItems.Remove(item);
                _issuesList.Items.Remove(cardBorder);
            };
            Grid.SetColumn(deleteBtn, 1);
            cardGrid.Children.Add(deleteBtn);

            cardBorder.Child = cardGrid;
            item.CardBorder = cardBorder;

            _issueItems.Add(item);
            _issuesList.Items.Add(cardBorder);
        }

        // ===================== 通用控件工厂 =====================

        private TextBlock CreateFieldLabel(string text)
        {
            var label = new TextBlock
            {
                Text = text,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _themeUpdateActions.Add(() =>
            {
                label.Foreground = TM.Brush(TM.TextMuted);
            });
            return label;
        }

        private TextBox CreateStyledTextBox(string placeholder = "")
        {
            var textBox = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 0, 0, 14),
                BorderThickness = new Thickness(1)
            };
            _themeUpdateActions.Add(() =>
            {
                textBox.Background = TM.Brush(TM.InputBg);
                textBox.Foreground = TM.Brush(TM.TextPrimary);
                textBox.BorderBrush = TM.Brush(TM.BorderDefault);
            });

            if (!string.IsNullOrEmpty(placeholder))
            {
                // 简单的 placeholder 效果
                textBox.Text = placeholder;
                textBox.Foreground = TM.Brush(TM.TextPlaceholder);
                textBox.GotFocus += (s, e) =>
                {
                    if (textBox.Text == placeholder && textBox.Foreground == TM.Brush(TM.TextPlaceholder))
                    {
                        textBox.Text = "";
                        textBox.Foreground = TM.Brush(TM.TextPrimary);
                    }
                };
                textBox.LostFocus += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(textBox.Text))
                    {
                        textBox.Text = placeholder;
                        textBox.Foreground = TM.Brush(TM.TextPlaceholder);
                    }
                };
            }

            return textBox;
        }

        private ComboBox CreateStyledComboBox(List<string> items)
        {
            var combo = new ComboBox
            {
                ItemsSource = items,
                Style = (Style)FindResource("ModernComboBox"),
                ItemContainerStyle = (Style)FindResource("ModernComboBoxItem")
            };
            return combo;
        }

        // ===================== 数据加载与保存 =====================

        private void LoadProjectData()
        {
            _subtitleBox.Text = _originalProject.Subtitle ?? "";
            _projectTypeCombo.SelectedItem = _originalProject.ProjectType;
            _startDateBox.Text = _originalProject.StartDate;
            _planEndDateBox.Text = _originalProject.PlanEndDate;
            _priorityCombo.SelectedItem = _originalProject.Priority;
            _descriptionBox.Text = _originalProject.Description;

            // 清除 placeholder 样式（因为有实际数据）
            if (!string.IsNullOrWhiteSpace(_originalProject.Subtitle))
                _subtitleBox.Foreground = TM.Brush(TM.TextPrimary);
            if (!string.IsNullOrWhiteSpace(_originalProject.StartDate))
                _startDateBox.Foreground = TM.Brush(TM.TextPrimary);
            if (!string.IsNullOrWhiteSpace(_originalProject.PlanEndDate))
                _planEndDateBox.Foreground = TM.Brush(TM.TextPrimary);

            _currentTaskBox.Text = _originalProject.CurrentTask;
            _nextPlanBox.Text = _originalProject.NextPlan;

            _stageCombo.SelectedItem = _originalProject.Stage;
            _progressSlider.Value = _originalProject.Progress;

            // 加载问题
            if (_originalProject.Issues != null)
            {
                foreach (var issue in _originalProject.Issues)
                {
                    AddIssueItem(issue.Title ?? "", issue.Description ?? "", issue.Status ?? "待处理");
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取实际文本（处理 placeholder）
            string subtitle = GetActualText(_subtitleBox, "输入副标题...");
            string startDate = GetActualText(_startDateBox, "例：2024-01-15");
            string planEndDate = GetActualText(_planEndDateBox, "例：2024-06-30");

            if (string.IsNullOrWhiteSpace(startDate))
            {
                MessageBox.Show("请输入开始日期", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                _startDateBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(planEndDate))
            {
                MessageBox.Show("请输入计划完成日期", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                _planEndDateBox.Focus();
                return;
            }

            int progress = (int)_progressSlider.Value;

            // 收集问题
            var issues = new List<ProjectIssue>();
            foreach (var item in _issueItems)
            {
                var title = item.TitleBox.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    issues.Add(new ProjectIssue
                    {
                        Id = item.Id ?? Guid.NewGuid().ToString("N"),
                        Title = title ?? "",
                        Description = "",
                        Status = item.Status,
                        Priority = "中",
                        CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                        Assignee = ""
                    });
                }
            }

            EditedProject = new ProjectModel
            {
                Id = _originalProject.Id,
                Name = _originalProject.Name,
                Subtitle = subtitle,
                IconColor = _originalProject.IconColor,
                IconKey = _originalProject.IconKey,
                ProjectType = _projectTypeCombo.SelectedItem?.ToString() ?? "软件开发",
                StartDate = startDate,
                PlanEndDate = planEndDate,
                Priority = _priorityCombo.SelectedItem?.ToString() ?? "中",
                Description = _descriptionBox.Text,
                Stage = _stageCombo.SelectedItem?.ToString() ?? "需求确认",
                Progress = progress,
                CurrentTask = _currentTaskBox.Text,
                NextPlan = _nextPlanBox.Text,
                TotalTasks = _originalProject.TotalTasks,
                CompletedTasks = _originalProject.CompletedTasks,
                Logs = _originalProject.Logs,
                Issues = issues
            };

            EditedIssues = issues;
            DialogResult = true;
        }

        private string GetActualText(TextBox box, string placeholder)
        {
            if (box.Foreground == TM.Brush(TM.TextPlaceholder) && box.Text == placeholder)
                return "";
            return box.Text;
        }
    }

    /// <summary>
    /// 问题编辑对话框
    /// </summary>
    public class IssueEditDialog : Window
    {
        public ProjectIssue EditedIssue { get; private set; }

        private TextBox _titleBox;
        private TextBox _descriptionBox;
        private Border _mainBorder;

        private readonly List<Action> _themeUpdateActions = new List<Action>();

        public IssueEditDialog(ProjectIssue issue = null)
        {
            InitializeUI(issue);
            if (issue != null)
            {
                LoadIssueData(issue);
            }
            TM.ThemeChanged += OnThemeChanged;
            Closed += (s, e) => TM.ThemeChanged -= OnThemeChanged;
        }

        private void OnThemeChanged()
        {
            foreach (var action in _themeUpdateActions)
                action.Invoke();
        }

        private void InitializeUI(ProjectIssue issue)
        {
            Title = issue == null ? "添加问题" : "编辑问题";
            Width = 460;
            Height = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;

            _mainBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };
            _themeUpdateActions.Add(() =>
            {
                _mainBorder.Background = TM.Brush(TM.DetailBg);
                _mainBorder.BorderBrush = TM.Brush(TM.CardBorder);
            });

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 标题栏
            var titleBar = new Border
            {
                Height = 48,
                Padding = new Thickness(20, 0, 12, 0)
            };
            _themeUpdateActions.Add(() =>
            {
                titleBar.Background = TM.Brush(TM.Accent);
            });
            titleBar.MouseLeftButtonDown += (s, e) => DragMove();

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = issue == null ? "添加问题" : "编辑问题",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);
            titleGrid.Children.Add(titleText);

            var closeBtn = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(4),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 6, 0, 6),
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
            closeBtn.MouseEnter += (s, e) => closeBtn.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            closeBtn.MouseLeave += (s, e) => closeBtn.Background = Brushes.Transparent;
            Grid.SetColumn(closeBtn, 1);
            titleGrid.Children.Add(closeBtn);

            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // 内容
            var contentStack = new StackPanel { Margin = new Thickness(24, 20, 24, 12) };

            contentStack.Children.Add(CreateFieldLabel("问题标题"));
            _titleBox = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 0, 0, 16),
                BorderThickness = new Thickness(1)
            };
            _themeUpdateActions.Add(() =>
            {
                _titleBox.Background = TM.Brush(TM.InputBg);
                _titleBox.Foreground = TM.Brush(TM.TextPrimary);
                _titleBox.BorderBrush = TM.Brush(TM.BorderDefault);
            });
            contentStack.Children.Add(_titleBox);

            contentStack.Children.Add(CreateFieldLabel("问题描述"));
            _descriptionBox = new TextBox
            {
                FontSize = 13,
                Padding = new Thickness(12, 9, 12, 9),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                MinHeight = 120,
                MaxHeight = 120,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderThickness = new Thickness(1)
            };
            _themeUpdateActions.Add(() =>
            {
                _descriptionBox.Background = TM.Brush(TM.InputBg);
                _descriptionBox.Foreground = TM.Brush(TM.TextPrimary);
                _descriptionBox.BorderBrush = TM.Brush(TM.BorderDefault);
            });
            contentStack.Children.Add(_descriptionBox);

            var scrollContent = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = contentStack
            };
            Grid.SetRow(scrollContent, 1);
            mainGrid.Children.Add(scrollContent);

            // 按钮区
            var btnBar = new Border
            {
                Padding = new Thickness(24, 12, 24, 20)
            };
            _themeUpdateActions.Add(() =>
            {
                btnBar.Background = TM.Brush(TM.DetailBg);
                btnBar.BorderBrush = TM.Brush(TM.Divider);
            });
            btnBar.BorderThickness = new Thickness(0, 1, 0, 0);

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var cancelBtn = new Button
            {
                Content = "取消",
                Width = 90,
                Height = 36,
                Margin = new Thickness(0, 0, 12, 0),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                FontSize = 13
            };
            _themeUpdateActions.Add(() =>
            {
                cancelBtn.Background = TM.Brush(TM.SectionBg);
                cancelBtn.Foreground = TM.Brush(TM.TextSecondary);
                cancelBtn.BorderBrush = TM.Brush(TM.BorderDefault);
            });
            cancelBtn.Click += (s, e) => DialogResult = false;
            btnPanel.Children.Add(cancelBtn);

            var okBtn = new Button
            {
                Content = issue == null ? "添加" : "保存",
                Width = 90,
                Height = 36,
                Cursor = Cursors.Hand,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                BorderThickness = new Thickness(0)
            };
            _themeUpdateActions.Add(() =>
            {
                okBtn.Background = TM.Brush(TM.Accent);
                okBtn.Foreground = Brushes.White;
            });
            okBtn.Click += SaveButton_Click;
            btnPanel.Children.Add(okBtn);

            btnBar.Child = btnPanel;
            Grid.SetRow(btnBar, 2);
            mainGrid.Children.Add(btnBar);

            _mainBorder.Child = mainGrid;
            Content = _mainBorder;

            // 应用初始主题
            foreach (var action in _themeUpdateActions)
                action.Invoke();

            Loaded += (s, e) => _titleBox?.Focus();
        }

        private void LoadIssueData(ProjectIssue issue)
        {
            _titleBox.Text = issue.Title;
            _descriptionBox.Text = issue.Description;
        }

        private TextBlock CreateFieldLabel(string text)
        {
            var label = new TextBlock
            {
                Text = text,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 6)
            };
            _themeUpdateActions.Add(() =>
            {
                label.Foreground = TM.Brush(TM.TextMuted);
            });
            return label;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_titleBox.Text))
            {
                MessageBox.Show("请输入问题标题", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                _titleBox.Focus();
                return;
            }

            EditedIssue = new ProjectIssue
            {
                Id = Guid.NewGuid().ToString(),
                Title = _titleBox.Text,
                Description = _descriptionBox.Text,
                Status = "待处理",
                Priority = "中",
                CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Assignee = ""
            };

            DialogResult = true;
        }
    }
}
