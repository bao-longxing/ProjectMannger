using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using ProjectMannger.Helpers;
using ProjectMannger.Models;
using ProjectMannger.ViewModels;
using ProjectMannger.Views;
using TM = ProjectMannger.Helpers.ThemeManager;
using System.IO;
using Forms = System.Windows.Forms;

namespace ProjectMannger
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private Forms.NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // 订阅 ViewModel 事件
            _viewModel.ShowMessageRequested += OnShowMessage;
            _viewModel.ShowInputDialogRequested += OnShowInputDialog;
            _viewModel.ShowMultiFieldDialogRequested += OnShowMultiFieldDialog;
            _viewModel.ShowConfirmDialogRequested += OnShowConfirmDialog;
            _viewModel.ShowAllActivitiesRequested += OnShowAllActivities;
            _viewModel.ShowProjectEditDialogRequested += OnShowProjectEditDialog;
            _viewModel.RefreshUIRequested += OnRefreshUI;

            // 订阅主题变更事件
            ProjectMannger.Helpers.ThemeManager.ThemeChanged += OnThemeChanged;

            // 初始化 UI
            DateText.Text = _viewModel.CurrentDate;
            BuildTodoList();
            BuildActivityList();
            BuildProjectList();
            UpdateProjectDetailPanel();
            UpdateStatisticsCards();

            // 绑定事件
            SetupDarkModeToggle();

            // 初始化系统托盘图标
            InitNotifyIcon();
        }

        #region 窗口控制

        private void DragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
                ToggleMaximize();
            else
                this.DragMove();
        }

        private void DragArea_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) { }
        private void DragArea_MouseMove(object sender, MouseEventArgs e) { }
        private void DragArea_MouseDoubleClick(object sender, MouseButtonEventArgs e) => ToggleMaximize();
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => MinimizeToTray();
        private void BtnMaximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Views.SettingsWindow
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }

        private void ViewHistoryBtn_Click(object sender, MouseButtonEventArgs e)
        {
            var historyWindow = new Views.HistoryProjectsWindow(_viewModel.Projects.ToList())
            {
                Owner = this
            };
            historyWindow.ShowDialog();
        }

        private void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = !this.Topmost;
            // 更新图标填充状态
            IconPin.Fill = this.Topmost
                ? (System.Windows.Media.Brush)FindResource("Theme.TextMuted")
                : System.Windows.Media.Brushes.Transparent;
            BtnPin.ToolTip = this.Topmost ? "取消置顶" : "置顶窗口";
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                WindowBorder.CornerRadius = new CornerRadius(8);
                // 切换到最大化图标
                IconMaximize.Data = (System.Windows.Media.Geometry)FindResource("IconMaximize");
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                WindowBorder.CornerRadius = new CornerRadius(0);
                // 切换到还原图标
                IconMaximize.Data = (System.Windows.Media.Geometry)FindResource("IconRestore");
            }
        }

        #region 系统托盘

        private void InitNotifyIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Text = "研发工作台";

            // 加载图标：优先使用嵌入资源 Logo.ico
            try
            {
                var iconUri = new Uri("pack://application:,,,/Logo.ico", UriKind.Absolute);
                var resourceInfo = System.Windows.Application.GetResourceStream(iconUri);
                if (resourceInfo != null)
                {
                    using (var stream = resourceInfo.Stream)
                    {
                        _notifyIcon.Icon = new System.Drawing.Icon(stream);
                    }
                }
                else
                {
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            // 右键菜单
            var menu = new Forms.ContextMenuStrip();
            var showItem = new Forms.ToolStripMenuItem("显示主窗口");
            showItem.Click += (s, args) => RestoreFromTray();
            var exitItem = new Forms.ToolStripMenuItem("退出");
            exitItem.Click += (s, args) => ExitApplication();
            menu.Items.Add(showItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(exitItem);
            _notifyIcon.ContextMenuStrip = menu;

            // 双击托盘图标恢复窗口
            _notifyIcon.DoubleClick += (s, args) => RestoreFromTray();

            _notifyIcon.Visible = false;
        }

        private void MinimizeToTray()
        {
            this.Hide();
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(1500, "研发工作台", "程序已最小化到系统托盘", Forms.ToolTipIcon.Info);
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            _notifyIcon.Visible = false;
        }

        private void ExitApplication()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        #endregion
        #endregion

        #region 窗口边缘拖拽缩放

        private const int EdgeThickness = 6; // 边缘可拖拽区域宽度（像素）

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_NCHITTEST = 0x0084;
            const int WM_GETMINMAXINFO = 0x0024;

            if (msg == WM_NCHITTEST)
            {
                // 最大化时不处理边缘拖拽
                if (WindowState == WindowState.Maximized)
                    return IntPtr.Zero;

                Point mousePos = PointFromScreen(new Point(
                    (short)(lParam.ToInt32() & 0xFFFF),
                    (short)(lParam.ToInt32() >> 16)));

                double w = ActualWidth;
                double h = ActualHeight;

                bool left = mousePos.X < EdgeThickness;
                bool right = mousePos.X > w - EdgeThickness;
                bool top = mousePos.Y < EdgeThickness;
                bool bottom = mousePos.Y > h - EdgeThickness;

                // 排除标题栏按钮区域（右上角 46*3 按钮区域）
                if (top && right && mousePos.Y < 36)
                {
                    // 不处理，让按钮正常工作
                    return IntPtr.Zero;
                }

                // 排除圆角区域
                double cornerRadius = 8;
                if ((left && top) || (left && bottom) || (right && top) || (right && bottom))
                {
                    double dx = left ? mousePos.X : (w - mousePos.X);
                    double dy = top ? mousePos.Y : (h - mousePos.Y);
                    double dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist < cornerRadius)
                        return IntPtr.Zero;
                }

                int hit = 0;
                if (left && top) hit = 13;        // HTTOPLEFT
                else if (left && bottom) hit = 16; // HTBOTTOMLEFT
                else if (right && top) hit = 14;   // HTTOPRIGHT
                else if (right && bottom) hit = 17;// HTBOTTOMRIGHT
                else if (left) hit = 10;           // HTLEFT
                else if (right) hit = 11;          // HTRIGHT
                else if (top) hit = 12;            // HTTOP
                else if (bottom) hit = 15;         // HTBOTTOM

                if (hit != 0)
                {
                    handled = true;
                    return new IntPtr(hit);
                }
            }
            else if (msg == WM_GETMINMAXINFO)
            {
                // 确保最大化时不会被任务栏遮挡
                var monitor = MonitorFromWindow(hwnd, 2 /* MONITOR_DEFAULTTONEAREST */);
                if (monitor != IntPtr.Zero)
                {
                    var mi = new MONITORINFO();
                    mi.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(mi);
                    GetMonitorInfo(monitor, ref mi);
                    var rc = mi.rcWork;
                    var info = System.Runtime.InteropServices.Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    info.ptMaxPosition.X = Math.Abs(rc.Left);
                    info.ptMaxPosition.Y = Math.Abs(rc.Top);
                    info.ptMaxSize.X = Math.Abs(rc.Right - rc.Left);
                    info.ptMaxSize.Y = Math.Abs(rc.Bottom - rc.Top);
                    System.Runtime.InteropServices.Marshal.StructureToPtr(info, lParam, true);
                }
            }

            return IntPtr.Zero;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct POINT
        {
            public int X, Y;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMaxTrackSize;
            public POINT ptMinTrackSize;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        #endregion

        #region ViewModel 事件处理

        private void OnShowMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string OnShowInputDialog(string message, string title, string defaultValue)
        {
            var dialog = new InputDialog(message, title, defaultValue)
            {
                Owner = this
            };
            if (dialog.ShowDialog() == true)
                return dialog.InputText;
            return null;
        }

        private System.Collections.Generic.Dictionary<string, string> OnShowMultiFieldDialog(string title, System.Collections.Generic.List<FieldDefinition> fields)
        {
            var dialog = new MultiFieldDialog(title, fields)
            {
                Owner = this
            };
            if (dialog.ShowDialog() == true)
                return dialog.Results;
            return null;
        }

        private bool OnShowConfirmDialog(string message, string title)
        {
            var dialog = new ConfirmDialog(message, title)
            {
                Owner = this
            };
            return dialog.ShowDialog() == true;
        }

        private void OnShowAllActivities(System.Collections.Generic.List<ActivityItem> activities)
        {
            var window = new AllActivitiesWindow(activities)
            {
                Owner = this
            };
            window.ShowDialog();
        }

        private ProjectEditDialog OnShowProjectEditDialog(ProjectModel project)
        {
            return new ProjectEditDialog(project)
            {
                Owner = this
            };
        }

        private void OnRefreshUI()
        {
            BuildTodoList();
            BuildActivityList();
            BuildProjectList();
            UpdateProjectDetailPanel();
            UpdateStatisticsCards();
        }

        #endregion

        #region 深色模式切换

        private void SetupDarkModeToggle()
        {
            if (DarkModeToggle != null)
            {
                DarkModeToggle.MouseLeftButtonDown += (s, e) =>
                {
                    _viewModel.ToggleDarkModeCommand.Execute(null);
                };
            }
        }

        private void OnThemeChanged()
        {
            // 更新深色模式按钮文字
            if (DarkModeText != null)
                DarkModeText.Text = TM.IsDarkMode ? "浅色" : "深色";

            // DynamicResource 会自动更新 XAML 中使用了 Theme.xxx 的元素
            // 这里只需要重建动态生成的 UI 内容
            BuildTodoList();
            BuildActivityList();
            BuildProjectList();
            UpdateProjectDetailPanel();
            UpdateStatisticsCards();
        }

        #endregion

        #region 待办事项

        private void BuildTodoList()
        {
            var todoStack = TodoListPanel;
            if (todoStack == null) return;

            // 清除旧的待办项（保留标题行）
            while (todoStack.Children.Count > 1)
                todoStack.Children.RemoveAt(todoStack.Children.Count - 1);

            // 重建待办项
            foreach (var todo in _viewModel.Todos)
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // 左侧：CheckBox + 文本
                var leftStack = new StackPanel { Orientation = Orientation.Horizontal };
                var checkBox = new CheckBox
                {
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    IsChecked = todo.IsCompleted,
                    Tag = todo.Id
                };
                checkBox.Checked += TodoCheckBox_Changed;
                checkBox.Unchecked += TodoCheckBox_Changed;
                leftStack.Children.Add(checkBox);

                var textBlock = new TextBlock
                {
                    Text = todo.Title,
                    FontSize = 13,
                    Foreground = todo.IsCompleted
                        ? TM.Brush(TM.TextPlaceholder)
                        : TM.Brush(TM.TextTertiary),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                if (todo.IsCompleted)
                    textBlock.TextDecorations = TextDecorations.Strikethrough;
                leftStack.Children.Add(textBlock);

                Grid.SetColumn(leftStack, 0);
                grid.Children.Add(leftStack);

                // 右侧：优先级 + 时间
                var rightStack = new StackPanel { Orientation = Orientation.Horizontal };

                var priorityBg = GetPriorityBackground(todo.Priority);
                var priorityFg = GetPriorityForeground(todo.Priority);
                var priorityBorder = new Border
                {
                    Background = priorityBg,
                    CornerRadius = new CornerRadius(4),
                    Width = 24,
                    Height = 24,
                    Margin = new Thickness(0, 0, 16, 0),
                    Child = new TextBlock
                    {
                        Text = todo.Priority,
                        FontSize = 11,
                        Foreground = priorityFg,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                rightStack.Children.Add(priorityBorder);

                var timeBlock = new TextBlock
                {
                    Text = todo.DueTime,
                    FontSize = 12,
                    Foreground = TM.Brush(TM.TextPlaceholder),
                    Width = 70,
                    TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                rightStack.Children.Add(timeBlock);

                // 删除按钮
                var deleteBtn = new Button
                {
                    Content = "✕",
                    Width = 20,
                    Height = 20,
                    FontSize = 10,
                    Background = Brushes.Transparent,
                    Foreground = TM.Brush(TM.TextPlaceholder),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(4, 0, 0, 0),
                    Tag = todo.Id,
                    ToolTip = "删除待办",
                    VerticalAlignment = VerticalAlignment.Center
                };
                deleteBtn.Click += TodoDelete_Click;
                rightStack.Children.Add(deleteBtn);

                Grid.SetColumn(rightStack, 1);
                grid.Children.Add(rightStack);

                todoStack.Children.Add(grid);
            }

            // 添加 "+ 添加待办" 按钮
            var addPanel = new StackPanel
            {
                Margin = new Thickness(0, 6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var addBtn = new TextBlock
            {
                Text = "+ 添加待办",
                FontSize = 12,
                Foreground = TM.Brush(TM.Accent),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            addBtn.MouseLeftButtonDown += (s, e) => _viewModel.AddTodoCommand.Execute(null);
            addPanel.Children.Add(addBtn);
            todoStack.Children.Add(addPanel);
        }

        private void TodoCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is string todoId)
            {
                _viewModel.ToggleTodoCommand.Execute(todoId);
                BuildTodoList(); // 刷新UI
            }
        }

        private void TodoDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string todoId)
            {
                _viewModel.DeleteTodoCommand.Execute(todoId);
            }
        }

        private Border FindTodoBorder()
        {
            // 遍历找到"今日待办"边框
            var centerPanel = RootGrid.Children.OfType<Border>()
                .FirstOrDefault(b => Grid.GetRow(b) == 1 && Grid.GetColumn(b) == 0);
            if (centerPanel == null) return null;

            return FindBorderByText(centerPanel, "今日待办");
        }

        #endregion

        #region 最近活动

        private void BuildActivityList()
        {
            var activityStack = ActivityListPanel;
            if (activityStack == null) return;

            // 清除旧内容
            activityStack.Children.Clear();

            // 标题
            activityStack.Children.Add(new TextBlock
            {
                Text = "最近更新",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = TM.Brush(TM.TextSecondary),
                Margin = new Thickness(0, 0, 0, 16)
            });

            // 活动项
            foreach (var activity in _viewModel.Activities.Take(5))
            {
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 14) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, MinWidth = 80 });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                grid.Children.Add(new TextBlock
                {
                    Text = activity.Time,
                    FontSize = 12,
                    Foreground = TM.Brush(TM.TextPlaceholder)
                });

                var nameBlock = new TextBlock
                {
                    Text = activity.ProjectName,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = TM.Brush(TM.TextSecondary),
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(nameBlock, 1);
                grid.Children.Add(nameBlock);

                var contentBlock = new TextBlock
                {
                    Text = activity.Content,
                    FontSize = 12,
                    Foreground = TM.Brush(TM.TextMuted),
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(contentBlock, 2);
                grid.Children.Add(contentBlock);

                activityStack.Children.Add(grid);
            }

            // 查看全部
            var viewAll = new TextBlock
            {
                Text = "查看全部 >",
                FontSize = 12,
                Foreground = TM.Brush(TM.Accent),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 4, 0, 0)
            };
            viewAll.MouseLeftButtonDown += (s, e) => _viewModel.ViewAllActivitiesCommand.Execute(null);
            activityStack.Children.Add(viewAll);
        }

        private Border FindActivityBorder()
        {
            var centerPanel = RootGrid.Children.OfType<Border>()
                .FirstOrDefault(b => Grid.GetRow(b) == 1 && Grid.GetColumn(b) == 0);
            if (centerPanel == null) return null;

            return FindBorderByText(centerPanel, "最近更新");
        }

        #endregion

        #region 项目列表

        private void BuildProjectList()
        {
            var projectPanel = FindProjectListPanel();
            if (projectPanel == null) return;

            projectPanel.Children.Clear();

            // 项目表头
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            string[] headers = { "项目", "阶段", "进度", "截止日期", "优先级", "操作" };
            for (int i = 0; i < headers.Length; i++)
            {
                headerGrid.Children.Add(new TextBlock
                {
                    Text = headers[i],
                    FontSize = 12,
                    Foreground = TM.Brush(TM.TextPlaceholder),
                    VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn((UIElement)headerGrid.Children[headerGrid.Children.Count - 1], i);
            }
            projectPanel.Children.Add(headerGrid);

            // 分割线
            projectPanel.Children.Add(new Border
            {
                Height = 1,
                Background = TM.Brush(TM.Divider),
                Margin = new Thickness(0, 0, 0, 10)
            });

            // 项目行（排除已完成的项目）
            foreach (var project in _viewModel.Projects.Where(p => p.Stage != "已完成"))
            {
                var row = CreateProjectRow(project);
                projectPanel.Children.Add(row);
            }
        }

        private Border CreateProjectRow(ProjectModel project)
        {
            var border = new Border
            {
                Background = _viewModel.SelectedProject?.Id == project.Id
                    ? TM.Brush(TM.SectionBg)
                    : Brushes.Transparent,
                Padding = new Thickness(12, 10, 12, 10),
                CornerRadius = new CornerRadius(6),
                Cursor = Cursors.Hand,
                Tag = project.Id
            };
            border.MouseLeftButtonDown += (s, e) =>
            {
                _viewModel.SelectProjectCommand.Execute(project.Id);
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });

            // 项目名 + 副标题
            var nameStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var iconGrid = new Grid { Width = 24, Height = 24, Margin = new Thickness(0, 0, 10, 0) };

            var iconColor = (SolidColorBrush)new BrushConverter().ConvertFromString(project.IconColor);
            iconGrid.Children.Add(new Ellipse { Fill = iconColor, Width = 24, Height = 24 });
            iconGrid.Children.Add(new TextBlock
            {
                Text = project.Name.Substring(0, 1),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            nameStack.Children.Add(iconGrid);

            var namePanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            namePanel.Children.Add(new TextBlock
            {
                Text = project.Name,
                FontSize = 13,
                FontWeight = FontWeights.Medium,
                Foreground = TM.Brush(TM.TextTertiary)
            });
            namePanel.Children.Add(new TextBlock
            {
                Text = project.Subtitle,
                FontSize = 11,
                Foreground = TM.Brush(TM.TextPlaceholder)
            });
            nameStack.Children.Add(namePanel);
            grid.Children.Add(nameStack);

            // 阶段
            var stageBg = GetStageBackground(project.Stage);
            var stageFg = GetStageForeground(project.Stage);
            var stageBorder = new Border
            {
                Background = stageBg,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = project.Stage, FontSize = 11, Foreground = stageFg }
            };
            Grid.SetColumn(stageBorder, 1);
            grid.Children.Add(stageBorder);

            // 进度圆环
            var progressStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 0) };
            var ringSize = 32.0;
            var ringStroke = 3.0;
            // WPF StrokeDashArray 的值是相对于 StrokeThickness 的倍数，按描边中心线半径计算
            var circumference = (2 * Math.PI * (ringSize - ringStroke) / 2.0) / ringStroke;
            var progressValue = project.Progress / 100.0;

            var ringGrid = new Grid { Width = ringSize, Height = ringSize, Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center };
            // 轨道圆环
            ringGrid.Children.Add(new Ellipse
            {
                Stroke = TM.Brush(TM.ProgressTrack),
                StrokeThickness = ringStroke,
                Width = ringSize,
                Height = ringSize
            });
            // 进度圆环
            var progressEllipse = new Ellipse
            {
                Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235)), // #2563EB
                StrokeThickness = ringStroke,
                Width = ringSize,
                Height = ringSize,
                StrokeDashArray = new DoubleCollection
                {
                    circumference * progressValue,
                    circumference * (1 - progressValue)
                },
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(-90)
            };
            ringGrid.Children.Add(progressEllipse);
            // 中心百分比文字
            ringGrid.Children.Add(new TextBlock
            {
                Text = $"{project.Progress}%",
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Foreground = TM.Brush(TM.TextTertiary),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            progressStack.Children.Add(ringGrid);
            Grid.SetColumn(progressStack, 2);
            grid.Children.Add(progressStack);

            // 截止日期
            grid.Children.Add(new TextBlock
            {
                Text = project.PlanEndDate,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn((UIElement)grid.Children[grid.Children.Count - 1], 3);

            // 优先级
            var priorityBg = GetPriorityBackground(project.Priority);
            var priorityFg = GetPriorityForeground(project.Priority);
            var priorityBorder = new Border
            {
                Background = priorityBg,
                CornerRadius = new CornerRadius(4),
                Width = 24,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock { Text = project.Priority, FontSize = 11, Foreground = priorityFg, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };
            Grid.SetColumn(priorityBorder, 4);
            grid.Children.Add(priorityBorder);

            // 删除按钮
            var deleteButton = new Button
            {
                Content = "✕",
                FontSize = 12,
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                Foreground = TM.Brush(TM.TextPlaceholder),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "删除项目"
            };
            deleteButton.MouseEnter += (s, e) =>
            {
                deleteButton.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // 红色悬停效果保持不变
                deleteButton.Background = TM.Brush(TM.SectionBg);
            };
            deleteButton.MouseLeave += (s, e) =>
            {
                deleteButton.Foreground = TM.Brush(TM.TextPlaceholder);
                deleteButton.Background = Brushes.Transparent;
            };
            deleteButton.Click += (s, e) =>
            {
                _viewModel.DeleteProjectCommand.Execute(project.Id);
            };
            Grid.SetColumn(deleteButton, 5);
            grid.Children.Add(deleteButton);

            border.Child = grid;
            return border;
        }

        private StackPanel FindProjectListPanel()
        {
            return ProjectListPanel;
        }

        #endregion

        #region 统计卡片

        private void UpdateStatisticsCards()
        {
            // 直接使用 x:Name 引用更新统计数字
            StatValue1.Text = _viewModel.TotalProjects.ToString();
            StatValue2.Text = _viewModel.TodoCount.ToString();
        }

        #endregion

        #region 项目详情面板

        private void UpdateProjectDetailPanel()
        {
            var project = _viewModel.SelectedProject;
            if (project == null)
            {
                DetailContentPanel.Visibility = Visibility.Collapsed;
                return;
            }
            DetailContentPanel.Visibility = Visibility.Visible;

            // 更新标题
            try
            {
                var iconColor = (SolidColorBrush)new BrushConverter().ConvertFromString(project.IconColor);
                DetailIconEllipse.Fill = iconColor;
            }
            catch { }
            DetailIconLetter.Text = project.Name.Length > 0 ? project.Name.Substring(0, 1) : "?";
            DetailProjectName.Text = project.Name;
            DetailProjectSubtitle.Text = project.Subtitle;

            // 更新基本信息
            DetailProjectType.Text = project.ProjectType;
            DetailStartDate.Text = project.StartDate;
            DetailPlanEndDate.Text = project.PlanEndDate;
            DetailPriorityText.Text = project.Priority;
            DetailPriorityBorder.Background = GetPriorityBackground(project.Priority);
            DetailPriorityText.Foreground = GetPriorityForeground(project.Priority);
            DetailDescription.Text = project.Description;

            // 更新任务和计划
            DetailCurrentTask.Text = project.CurrentTask;
            DetailNextPlan.Text = project.NextPlan;

            // 更新进度
            // WPF StrokeDashArray 的值是相对于 StrokeThickness 的倍数，需要除以 StrokeThickness
            double progress = project.Progress / 100.0;
            // 描边中心线半径 = (Width - StrokeThickness) / 2 = (80 - 8) / 2 = 36
            double perimeter = 2 * Math.PI * 36;
            double strokeThickness = 8;
            double circumference = perimeter / strokeThickness; // 换算为 StrokeThickness 相对单位
            DetailProgressEllipse.StrokeDashArray = new DoubleCollection
            {
                circumference * progress,
                circumference * (1 - progress)
            };
            DetailProgressPercent.Text = $"{project.Progress}%";
            DetailStage.Text = project.Stage;
            DetailElapsedDays.Text = _viewModel.SelectedProjectElapsedDays;
            DetailRemainingDays.Text = _viewModel.SelectedProjectRemainingDays;

            // 更新标签页显示
            UpdateTabHeaders();
            UpdateTabContentVisibility();
        }

        private void UpdateTabHeaders()
        {
            string selected = _viewModel.SelectedTab;
            bool isOverview = selected == "概览";
            bool isLogs = selected == "日志";
            bool isIssues = selected == "问题";

            TabOverviewBorder.BorderBrush = isOverview ? TM.Brush(TM.Accent) : Brushes.Transparent;
            TabOverviewBorder.Background = Brushes.Transparent;
            TabOverviewText.Foreground = isOverview ? TM.Brush(TM.Accent) : TM.Brush(TM.TextMuted);
            TabOverviewText.FontWeight = isOverview ? FontWeights.Medium : FontWeights.Normal;

            TabLogsBorder.BorderBrush = isLogs ? TM.Brush(TM.Accent) : Brushes.Transparent;
            TabLogsText.Foreground = isLogs ? TM.Brush(TM.Accent) : TM.Brush(TM.TextMuted);
            TabLogsText.FontWeight = isLogs ? FontWeights.Medium : FontWeights.Normal;

            TabIssuesBorder.BorderBrush = isIssues ? TM.Brush(TM.Accent) : Brushes.Transparent;
            TabIssuesText.Foreground = isIssues ? TM.Brush(TM.Accent) : TM.Brush(TM.TextMuted);
            TabIssuesText.FontWeight = isIssues ? FontWeights.Medium : FontWeights.Normal;
        }

        private void UpdateTabContentVisibility()
        {
            var project = _viewModel.SelectedProject;
            string selected = _viewModel.SelectedTab;

            OverviewPanel.Visibility = selected == "概览" ? Visibility.Visible : Visibility.Collapsed;
            LogsPanel.Visibility = selected == "日志" ? Visibility.Visible : Visibility.Collapsed;
            IssuesPanel.Visibility = selected == "问题" ? Visibility.Visible : Visibility.Collapsed;

            if (selected == "日志" && project != null)
            {
                LogsPanel.Children.Clear();
                BuildLogsTab(LogsPanel, project);
            }
            else if (selected == "问题" && project != null)
            {
                IssuesPanel.Children.Clear();
                BuildIssuesTab(IssuesPanel, project);
            }
        }

        private void Tab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string tab)
            {
                _viewModel.SwitchTabCommand.Execute(tab);
            }
        }

        private void BtnEditProject_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.EditProjectAllCommand.Execute(null);
        }

        private void BuildLogsTab(StackPanel mainStack, ProjectModel project)
        {
            mainStack.Children.Add(new TextBlock
            {
                Text = "项目日志",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = TM.Brush(TM.TextSecondary),
                Margin = new Thickness(0, 0, 0, 16)
            });

            if (project.Logs.Count == 0)
            {
                mainStack.Children.Add(new TextBlock
                {
                    Text = "暂无日志记录",
                    FontSize = 12,
                    Foreground = TM.Brush(TM.TextPlaceholder)
                });
            }
            else
            {
                foreach (var log in project.Logs)
                {
                    var logBorder = new Border
                    {
                        Background = TM.Brush(TM.SectionBg),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 8)
                    };
                    var logStack = new StackPanel();
                    logStack.Children.Add(new TextBlock
                    {
                        Text = $"{log.Date} - {log.Author}",
                        FontSize = 11,
                        Foreground = TM.Brush(TM.TextPlaceholder),
                        Margin = new Thickness(0, 0, 0, 4)
                    });
                    logStack.Children.Add(new TextBlock
                    {
                        Text = log.Content,
                        FontSize = 12,
                        Foreground = TM.Brush(TM.TextTertiary)
                    });
                    logBorder.Child = logStack;
                    mainStack.Children.Add(logBorder);
                }
            }
        }

        private void BuildIssuesTab(StackPanel mainStack, ProjectModel project)
        {
            mainStack.Children.Add(new TextBlock
            {
                Text = "项目问题",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = TM.Brush(TM.TextSecondary),
                Margin = new Thickness(0, 0, 0, 16)
            });

            if (project.Issues == null || project.Issues.Count == 0)
            {
                mainStack.Children.Add(new TextBlock
                {
                    Text = "暂无问题记录",
                    FontSize = 12,
                    Foreground = TM.Brush(TM.TextPlaceholder)
                });
            }
            else
            {
                foreach (var issue in project.Issues)
                {
                    var issueBorder = new Border
                    {
                        Background = TM.Brush(TM.SectionBg),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 0, 0, 6)
                    };
                    var issueText = new TextBlock
                    {
                        Text = issue.Title,
                        FontSize = 13,
                        Foreground = TM.Brush(TM.TextPrimary),
                        TextWrapping = TextWrapping.Wrap
                    };
                    issueBorder.Child = issueText;
                    mainStack.Children.Add(issueBorder);
                }
            }
        }

        #endregion

        #region 新建项目按钮

        private void CreateProject_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CreateProjectCommand.Execute(null);
        }

        #endregion

        #region 辅助方法

        private SolidColorBrush GetPriorityBackground(string priority)
        {
            switch (priority)
            {
                case "高": return TM.Brush(TM.PriorityHighBg);
                case "中": return TM.Brush(TM.PriorityMedBg);
                case "低": return TM.Brush(TM.PriorityLowBg);
                default: return TM.Brush(TM.PriorityDefaultBg);
            }
        }

        private SolidColorBrush GetPriorityForeground(string priority)
        {
            switch (priority)
            {
                case "高": return TM.Brush(TM.PriorityHighFg);
                case "中": return TM.Brush(TM.PriorityMedFg);
                case "低": return TM.Brush(TM.PriorityLowFg);
                default: return TM.Brush(TM.PriorityDefaultFg);
            }
        }

        private SolidColorBrush GetStageBackground(string stage)
        {
            switch (stage)
            {
                case "开发中": return TM.Brush(TM.StageDevBg);
                case "测试中": return TM.Brush(TM.StageTestBg);
                case "已完成": return TM.Brush(TM.StageDoneBg);
                case "需求确认": return TM.Brush(TM.StageDefaultBg);
                default: return TM.Brush(TM.StageDefaultBg);
            }
        }

        private SolidColorBrush GetStageForeground(string stage)
        {
            switch (stage)
            {
                case "开发中": return TM.Brush(TM.StageDevFg);
                case "测试中": return TM.Brush(TM.StageTestFg);
                case "已完成": return TM.Brush(TM.StageDoneFg);
                case "需求确认": return TM.Brush(TM.StageDefaultFg);
                default: return TM.Brush(TM.StageDefaultFg);
            }
        }

        private Border FindBorderByText(DependencyObject parent, string text)
        {
            var borders = new List<Border>();
            FindAllChildren<Border>(parent, borders);
            return borders.FirstOrDefault(b =>
            {
                var tb = FindChild<TextBlock>(b);
                return tb != null && tb.Text == text;
            });
        }

        private UIElement FindChildByPredicate(DependencyObject parent, Func<DependencyObject, bool> predicate)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (predicate(child)) return child as UIElement;
                var result = FindChildByPredicate(child, predicate);
                if (result != null) return result;
            }
            return null;
        }

        private void FindAllChildren<T>(DependencyObject parent, List<T> results) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    results.Add(typed);
                FindAllChildren(child, results);
            }
        }

        private T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed)
                    return typed;
                var result = FindChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private T FindChildByName<T>(string name) where T : FrameworkElement
        {
            return FindName(name) as T;
        }

        protected override void OnClosed(EventArgs e)
        {
            ThemeManager.ThemeChanged -= OnThemeChanged;
            _viewModel.SaveAll();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnClosed(e);
        }

        #endregion
    }
}
