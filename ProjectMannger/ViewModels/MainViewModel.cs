using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProjectMannger.Commands;
using ProjectMannger.Models;
using ProjectMannger.Services;
using ProjectMannger.Views;

namespace ProjectMannger.ViewModels
{
    /// <summary>
    /// 主窗口 ViewModel
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly DataService _dataService = DataService.Instance;

        #region 私有字段

        private string _currentPage = "工作台";
        private ProjectModel _selectedProject;
        private string _selectedTab = "概览";
        private string _currentDate;
        private bool _isDarkMode = false;

        // 统计数据
        private int _inProgressCount;
        private int _completedCount;
        private int _pendingCount;
        private int _totalProjects;
        private int _todoCount;
        private int _overdueCount;

        #endregion

        #region 集合

        public ObservableCollection<ProjectModel> Projects { get; set; }
        public ObservableCollection<TodoItem> Todos { get; set; }
        public ObservableCollection<ActivityItem> Activities { get; set; }

        #endregion

        #region 属性

        public string CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    OnPropertyChanged(nameof(IsWorkbenchVisible));
                    OnPropertyChanged(nameof(IsProjectsVisible));
                    OnPropertyChanged(nameof(IsSettingsVisible));
                }
            }
        }

        public bool IsWorkbenchVisible => _currentPage == "工作台";
        public bool IsProjectsVisible => _currentPage == "项目";
        public bool IsSettingsVisible => _currentPage == "设置";

        public ProjectModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (SetProperty(ref _selectedProject, value))
                {
                    OnPropertyChanged(nameof(HasSelectedProject));
                    OnPropertyChanged(nameof(SelectedProjectProgress));
                    OnPropertyChanged(nameof(SelectedProjectRemainingDays));
                    OnPropertyChanged(nameof(SelectedProjectElapsedDays));
                }
            }
        }

        public bool HasSelectedProject => _selectedProject != null;

        public string SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set => SetProperty(ref _isDarkMode, value);
        }

        // 统计属性
        public int InProgressCount
        {
            get => _inProgressCount;
            set => SetProperty(ref _inProgressCount, value);
        }

        public int CompletedCount
        {
            get => _completedCount;
            set => SetProperty(ref _completedCount, value);
        }

        public int PendingCount
        {
            get => _pendingCount;
            set => SetProperty(ref _pendingCount, value);
        }

        public int TotalProjects
        {
            get => _totalProjects;
            set => SetProperty(ref _totalProjects, value);
        }

        public int TodoCount
        {
            get => _todoCount;
            set => SetProperty(ref _todoCount, value);
        }

        public int OverdueCount
        {
            get => _overdueCount;
            set => SetProperty(ref _overdueCount, value);
        }

        // 选中项目的计算属性
        public string SelectedProjectProgress => SelectedProject != null ? $"{SelectedProject.Progress}%" : "0%";

        public string SelectedProjectRemainingDays
        {
            get
            {
                if (SelectedProject == null || string.IsNullOrEmpty(SelectedProject.PlanEndDate))
                    return "未知";
                if (DateTime.TryParse(SelectedProject.PlanEndDate, out DateTime endDate))
                {
                    var remaining = (endDate - DateTime.Now).Days;
                    return remaining > 0 ? $"{remaining} 天" : "已逾期";
                }
                return "未知";
            }
        }

        public string SelectedProjectElapsedDays
        {
            get
            {
                if (SelectedProject == null || string.IsNullOrEmpty(SelectedProject.StartDate))
                    return "未知";
                if (DateTime.TryParse(SelectedProject.StartDate, out DateTime startDate))
                {
                    var elapsed = (DateTime.Now - startDate).Days;
                    return $"{elapsed} 天";
                }
                return "未知";
            }
        }

        #endregion

        #region 命令

        // 导航命令
        public ICommand NavigateWorkbenchCommand { get; }
        public ICommand NavigateProjectsCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand ToggleDarkModeCommand { get; }

        // 待办命令
        public ICommand AddTodoCommand { get; }
        public ICommand ToggleTodoCommand { get; }
        public ICommand DeleteTodoCommand { get; }

        // 项目命令
        public ICommand CreateProjectCommand { get; }
        public ICommand SelectProjectCommand { get; }
        public ICommand EditProjectCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand ViewAllActivitiesCommand { get; }

        // 详情面板命令
        public ICommand SwitchTabCommand { get; }
        public ICommand EditBasicInfoCommand { get; }
        public ICommand EditCurrentTaskCommand { get; }
        public ICommand EditNextPlanCommand { get; }
        public ICommand EditProgressCommand { get; }
        public ICommand EditProjectAllCommand { get; }

        // 统计卡片命令
        public ICommand ViewInProgressCommand { get; }
        public ICommand ViewCompletedCommand { get; }
        public ICommand ViewPendingCommand { get; }

        #endregion

        #region 事件

        /// <summary>
        /// 请求显示消息对话框
        /// </summary>
        public event Action<string, string> ShowMessageRequested;

        /// <summary>
        /// 请求显示输入对话框，返回用户输入的文本
        /// </summary>
        public event Func<string, string, string, string> ShowInputDialogRequested;

        /// <summary>
        /// 请求显示多字段编辑对话框，返回字段值字典
        /// </summary>
        public event Func<string, System.Collections.Generic.List<FieldDefinition>, System.Collections.Generic.Dictionary<string, string>> ShowMultiFieldDialogRequested;

        /// <summary>
        /// 请求显示项目编辑对话框
        /// </summary>
        public event Func<ProjectModel, ProjectEditDialog> ShowProjectEditDialogRequested;

        /// <summary>
        /// 请求显示确认对话框，返回用户是否确认
        /// </summary>
        public event Func<string, string, bool> ShowConfirmDialogRequested;

        /// <summary>
        /// 请求显示全部活动记录窗口
        /// </summary>
        public event Action<List<ActivityItem>> ShowAllActivitiesRequested;

        /// <summary>
        /// 请求 UI 刷新
        /// </summary>
        public event Action RefreshUIRequested;

        #endregion

        public MainViewModel()
        {
            CurrentDate = DateTime.Now.ToString("yyyy年MM月dd日 dddd");

            // 初始化集合
            Projects = new ObservableCollection<ProjectModel>(_dataService.LoadProjects());
            Todos = new ObservableCollection<TodoItem>(_dataService.LoadTodos());
            Activities = new ObservableCollection<ActivityItem>(_dataService.LoadActivities());

            // 默认选中第一个项目
            if (Projects.Count > 0)
                SelectedProject = Projects[0];

            UpdateStatistics();

            // 初始化命令
            NavigateWorkbenchCommand = new RelayCommand(() => CurrentPage = "工作台");
            NavigateProjectsCommand = new RelayCommand(() => CurrentPage = "项目");
            NavigateSettingsCommand = new RelayCommand(() => CurrentPage = "设置");
            ToggleDarkModeCommand = new RelayCommand(ToggleDarkMode);

            AddTodoCommand = new RelayCommand(AddTodo);
            ToggleTodoCommand = new RelayCommand(ToggleTodo);
            DeleteTodoCommand = new RelayCommand(DeleteTodo);

            CreateProjectCommand = new RelayCommand(CreateProject);
            SelectProjectCommand = new RelayCommand(SelectProject);
            EditProjectCommand = new RelayCommand(EditProject);
            DeleteProjectCommand = new RelayCommand(DeleteProject);
            ViewAllActivitiesCommand = new RelayCommand(ViewAllActivities);

            SwitchTabCommand = new RelayCommand(SwitchTab);
            EditBasicInfoCommand = new RelayCommand(EditBasicInfo);
            EditCurrentTaskCommand = new RelayCommand(EditCurrentTask);
            EditNextPlanCommand = new RelayCommand(EditNextPlan);
            EditProgressCommand = new RelayCommand(EditProgress);
            EditProjectAllCommand = new RelayCommand(EditProjectAll);

            ViewInProgressCommand = new RelayCommand(() => CurrentPage = "项目");
            ViewCompletedCommand = new RelayCommand(() => CurrentPage = "项目");
            ViewPendingCommand = new RelayCommand(() => CurrentPage = "项目");
        }

        #region 命令实现

        private void ToggleDarkMode()
        {
            IsDarkMode = !IsDarkMode;
            // 通知全局 ThemeManager 并刷新 UI
            ProjectMannger.Helpers.ThemeManager.SetDarkMode(IsDarkMode);
            RefreshUIRequested?.Invoke();
        }

        private void AddTodo()
        {
            var fields = new System.Collections.Generic.List<FieldDefinition>
            {
                new FieldDefinition { Key = "Title", Label = "待办内容", Value = "" },
                new FieldDefinition
                {
                    Key = "Priority",
                    Label = "优先级",
                    Value = "中",
                    Type = FieldType.ComboBox,
                    Options = new System.Collections.Generic.List<string> { "高", "中", "低" }
                }
            };

            var results = ShowMultiFieldDialogRequested?.Invoke("添加待办", fields);
            if (results == null) return;

            if (!results.TryGetValue("Title", out var title) || string.IsNullOrWhiteSpace(title)) return;
            results.TryGetValue("Priority", out var priority);
            if (string.IsNullOrWhiteSpace(priority)) priority = "中";

            var todo = new TodoItem
            {
                Title = title,
                Priority = priority,
                DueTime = DateTime.Now.ToString("HH:mm")
            };
            Todos.Add(todo);
            _dataService.SaveTodos(Todos.ToList());
            AddActivity("待办", $"添加了待办「{title}」（{priority}）");
            UpdateStatistics();
            RefreshUIRequested?.Invoke();
        }

        private void ToggleTodo(object parameter)
        {
            if (parameter is string todoId)
            {
                var todo = Todos.FirstOrDefault(t => t.Id == todoId);
                if (todo != null)
                {
                    todo.IsCompleted = !todo.IsCompleted;
                    _dataService.SaveTodos(Todos.ToList());
                    UpdateStatistics();
                    RefreshUIRequested?.Invoke();
                }
            }
        }

        private void DeleteTodo(object parameter)
        {
            if (parameter is string todoId)
            {
                var todo = Todos.FirstOrDefault(t => t.Id == todoId);
                if (todo != null)
                {
                    var todoTitle = todo.Title;
                    Todos.Remove(todo);
                    _dataService.SaveTodos(Todos.ToList());
                    AddActivity("待办", $"删除了待办「{todoTitle}」");
                    UpdateStatistics();
                    RefreshUIRequested?.Invoke();
                }
            }
        }

        private void CreateProject()
        {
            var name = ShowInputDialogRequested?.Invoke("请输入项目名称：", "新建项目", "");
            if (string.IsNullOrWhiteSpace(name)) return;

            var project = new ProjectModel
            {
                Name = name,
                Subtitle = "新建项目",
                StartDate = DateTime.Now.ToString("yyyy-MM-dd"),
                PlanEndDate = DateTime.Now.AddMonths(3).ToString("yyyy-MM-dd"),
                Priority = "中",
                Stage = "需求确认",
                Progress = 0,
                Description = "",
                CurrentTask = "待规划",
                NextPlan = "待规划",
                TotalTasks = 0,
                CompletedTasks = 0
            };
            Projects.Add(project);
            _dataService.SaveProjects(Projects.ToList());
            SelectedProject = project;
            UpdateStatistics();
            RefreshUIRequested?.Invoke();

            // 添加活动记录
            AddActivity("系统", $"创建了新项目「{name}」");

            // 创建后立即弹出编辑项目窗口，方便完善项目信息
            var editDialog = ShowProjectEditDialogRequested?.Invoke(project);
            if (editDialog != null && editDialog.ShowDialog() == true)
            {
                var editedProject = editDialog.EditedProject;
                if (editedProject != null)
                {
                    // 将编辑结果写回新项目
                    project.ProjectType = editedProject.ProjectType;
                    project.StartDate = editedProject.StartDate;
                    project.PlanEndDate = editedProject.PlanEndDate;
                    project.Priority = editedProject.Priority;
                    project.Description = editedProject.Description;
                    project.CurrentTask = editedProject.CurrentTask;
                    project.NextPlan = editedProject.NextPlan;
                    project.Stage = editedProject.Stage;
                    project.Progress = editedProject.Progress;
                    project.Subtitle = editedProject.Subtitle;
                    project.Issues = editedProject.Issues;
                }

                _dataService.SaveProjects(Projects.ToList());
                UpdateStatistics();
                RefreshUIRequested?.Invoke();
            }
        }

        private void SelectProject(object parameter)
        {
            if (parameter is string projectId)
            {
                var project = Projects.FirstOrDefault(p => p.Id == projectId);
                if (project != null)
                {
                    SelectedProject = project;
                    RefreshUIRequested?.Invoke();
                }
            }
        }

        private void EditProject(object parameter)
        {
            if (SelectedProject == null) return;
            var field = parameter as string;

            switch (field)
            {
                case "Name":
                    var name = ShowInputDialogRequested?.Invoke("请输入新的项目名称：", "编辑项目名称", SelectedProject.Name);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        SelectedProject.Name = name;
                        AddActivity(SelectedProject.Name, "修改了项目名称");
                    }
                    break;
                default:
                    ShowMessageRequested?.Invoke("请使用详情面板中的编辑按钮修改具体字段。", "编辑项目");
                    break;
            }
            _dataService.SaveProjects(Projects.ToList());
            RefreshUIRequested?.Invoke();
        }

        private void DeleteProject(object parameter)
        {
            if (parameter is string projectId)
            {
                var project = Projects.FirstOrDefault(p => p.Id == projectId);
                if (project != null)
                {
                    var confirmed = ShowConfirmDialogRequested?.Invoke(
                        $"确定要删除项目「{project.Name}」吗？此操作不可恢复。",
                        "确认删除") ?? false;

                    if (confirmed)
                    {
                        Projects.Remove(project);
                        if (SelectedProject?.Id == projectId)
                            SelectedProject = Projects.FirstOrDefault();
                        _dataService.SaveProjects(Projects.ToList());
                        UpdateStatistics();
                        RefreshUIRequested?.Invoke();
                        AddActivity("系统", $"删除了项目「{project.Name}」");
                    }
                }
            }
        }

        private void ViewAllActivities()
        {
            ShowAllActivitiesRequested?.Invoke(Activities.ToList());
        }

        private void SwitchTab(object parameter)
        {
            if (parameter is string tab)
            {
                SelectedTab = tab;
                RefreshUIRequested?.Invoke();
            }
        }

        private void EditBasicInfo()
        {
            if (SelectedProject == null) return;

            var fields = new System.Collections.Generic.List<FieldDefinition>
            {
                new FieldDefinition { Key = "ProjectType", Label = "项目类型", Value = SelectedProject.ProjectType,
                    Type = FieldType.ComboBox, Options = new System.Collections.Generic.List<string> { "软件开发", "硬件开发", "视觉检测", "数据分析", "其他" } },
                new FieldDefinition { Key = "StartDate", Label = "开始日期", Value = SelectedProject.StartDate },
                new FieldDefinition { Key = "PlanEndDate", Label = "计划完成日期", Value = SelectedProject.PlanEndDate },
                new FieldDefinition { Key = "Priority", Label = "优先级", Value = SelectedProject.Priority,
                    Type = FieldType.ComboBox, Options = new System.Collections.Generic.List<string> { "高", "中", "低" } },
                new FieldDefinition { Key = "Description", Label = "描述", Value = SelectedProject.Description, Type = FieldType.MultiLine }
            };

            var results = ShowMultiFieldDialogRequested?.Invoke(
                $"编辑 {SelectedProject.Name} - 基本信息", fields);

            if (results == null) return;

            bool changed = false;
            var changes = new System.Collections.Generic.List<string>();

            if (results.TryGetValue("ProjectType", out var pt) && pt != SelectedProject.ProjectType)
            {
                SelectedProject.ProjectType = pt; changed = true; changes.Add($"项目类型→{pt}");
            }
            if (results.TryGetValue("StartDate", out var sd) && sd != SelectedProject.StartDate)
            {
                SelectedProject.StartDate = sd; changed = true; changes.Add($"开始日期→{sd}");
            }
            if (results.TryGetValue("PlanEndDate", out var ped) && ped != SelectedProject.PlanEndDate)
            {
                SelectedProject.PlanEndDate = ped; changed = true; changes.Add($"计划完成→{ped}");
            }
            if (results.TryGetValue("Priority", out var pri) && pri != SelectedProject.Priority)
            {
                SelectedProject.Priority = pri; changed = true; changes.Add($"优先级→{pri}");
            }
            if (results.TryGetValue("Description", out var desc) && desc != SelectedProject.Description)
            {
                SelectedProject.Description = desc; changed = true; changes.Add("描述已更新");
            }

            if (changed)
            {
                _dataService.SaveProjects(Projects.ToList());
                AddActivity(SelectedProject.Name, $"更新了基本信息：{string.Join("，", changes)}");
                RefreshUIRequested?.Invoke();
            }
        }

        private void EditCurrentTask()
        {
            if (SelectedProject == null) return;

            var task = ShowInputDialogRequested?.Invoke(
                "请输入当前任务：",
                $"编辑 {SelectedProject.Name} - 当前任务",
                SelectedProject.CurrentTask);

            if (!string.IsNullOrWhiteSpace(task) && task != SelectedProject.CurrentTask)
            {
                SelectedProject.CurrentTask = task;
                _dataService.SaveProjects(Projects.ToList());
                AddActivity(SelectedProject.Name, $"更新了当前任务：{task}");
                RefreshUIRequested?.Invoke();
            }
        }

        private void EditNextPlan()
        {
            if (SelectedProject == null) return;

            var plan = ShowInputDialogRequested?.Invoke(
                "请输入下一步计划：",
                $"编辑 {SelectedProject.Name} - 下一步计划",
                SelectedProject.NextPlan);

            if (!string.IsNullOrWhiteSpace(plan) && plan != SelectedProject.NextPlan)
            {
                SelectedProject.NextPlan = plan;
                _dataService.SaveProjects(Projects.ToList());
                AddActivity(SelectedProject.Name, $"更新了下一步计划：{plan}");
                RefreshUIRequested?.Invoke();
            }
        }

        private void EditProgress()
        {
            if (SelectedProject == null) return;

            var stageOptions = new System.Collections.Generic.List<string> { "需求确认", "开发中", "测试中", "已完成" };

            var fields = new System.Collections.Generic.List<FieldDefinition>
            {
                new FieldDefinition { Key = "Stage", Label = "当前阶段", Value = SelectedProject.Stage,
                    Type = FieldType.ComboBox, Options = stageOptions },
                new FieldDefinition { Key = "Progress", Label = "进度 (%)", Value = SelectedProject.Progress.ToString() }
            };

            var results = ShowMultiFieldDialogRequested?.Invoke(
                $"编辑 {SelectedProject.Name} - 进度", fields);

            if (results == null) return;

            bool changed = false;
            var changes = new System.Collections.Generic.List<string>();

            if (results.TryGetValue("Stage", out var stage) && stage != SelectedProject.Stage)
            {
                SelectedProject.Stage = stage; changed = true; changes.Add($"阶段→{stage}");
            }
            if (results.TryGetValue("Progress", out var progStr) && int.TryParse(progStr, out var prog))
            {
                prog = Math.Max(0, Math.Min(100, prog));
                if (prog != SelectedProject.Progress)
                {
                    SelectedProject.Progress = prog; changed = true; changes.Add($"进度→{prog}%");
                }
            }

            if (changed)
            {
                _dataService.SaveProjects(Projects.ToList());
                AddActivity(SelectedProject.Name, $"更新了进度：{string.Join("，", changes)}");
                RefreshUIRequested?.Invoke();
            }
        }

        private void EditProjectAll()
        {
            if (SelectedProject == null) return;

            var dialog = ShowProjectEditDialogRequested?.Invoke(SelectedProject);
            if (dialog == null) return;

            if (dialog.ShowDialog() == true)
            {
                var editedProject = dialog.EditedProject;
                var changes = new System.Collections.Generic.List<string>();

                // 检查并记录所有变更
                if (editedProject.ProjectType != SelectedProject.ProjectType)
                    changes.Add($"项目类型→{editedProject.ProjectType}");
                if (editedProject.StartDate != SelectedProject.StartDate)
                    changes.Add($"开始日期→{editedProject.StartDate}");
                if (editedProject.PlanEndDate != SelectedProject.PlanEndDate)
                    changes.Add($"计划完成→{editedProject.PlanEndDate}");
                if (editedProject.Priority != SelectedProject.Priority)
                    changes.Add($"优先级→{editedProject.Priority}");
                if (editedProject.Description != SelectedProject.Description)
                    changes.Add("描述已更新");
                if (editedProject.CurrentTask != SelectedProject.CurrentTask)
                    changes.Add($"当前任务→{editedProject.CurrentTask}");
                if (editedProject.NextPlan != SelectedProject.NextPlan)
                    changes.Add($"下一步计划→{editedProject.NextPlan}");
                if (editedProject.Stage != SelectedProject.Stage)
                    changes.Add($"阶段→{editedProject.Stage}");
                if (editedProject.Progress != SelectedProject.Progress)
                    changes.Add($"进度→{editedProject.Progress}%");
                if (editedProject.Subtitle != SelectedProject.Subtitle)
                    changes.Add("副标题已更新");

                // 检查问题变更
                var oldIssueIds = SelectedProject.Issues?.Select(i => i.Id).ToList() ?? new System.Collections.Generic.List<string>();
                var newIssueIds = editedProject.Issues?.Select(i => i.Id).ToList() ?? new System.Collections.Generic.List<string>();
                var addedIssues = newIssueIds.Except(oldIssueIds).ToList();
                var removedIssues = oldIssueIds.Except(newIssueIds).ToList();

                if (addedIssues.Count > 0)
                    changes.Add($"新增{addedIssues.Count}个问题");
                if (removedIssues.Count > 0)
                    changes.Add($"删除{removedIssues.Count}个问题");

                // 应用变更
                SelectedProject.ProjectType = editedProject.ProjectType;
                SelectedProject.StartDate = editedProject.StartDate;
                SelectedProject.PlanEndDate = editedProject.PlanEndDate;
                SelectedProject.Priority = editedProject.Priority;
                SelectedProject.Description = editedProject.Description;
                SelectedProject.CurrentTask = editedProject.CurrentTask;
                SelectedProject.NextPlan = editedProject.NextPlan;
                SelectedProject.Stage = editedProject.Stage;
                SelectedProject.Progress = editedProject.Progress;
                SelectedProject.Subtitle = editedProject.Subtitle;
                SelectedProject.Issues = editedProject.Issues;

                // 保存项目数据
                _dataService.SaveProjects(Projects.ToList());

                // 记录到项目日志
                if (changes.Count > 0)
                {
                    var logContent = $"编辑了项目：{string.Join("，", changes)}";
                    
                    // 构建问题变更详情
                    var issueDetails = new System.Collections.Generic.List<string>();
                    if (addedIssues.Count > 0)
                    {
                        var addedIssueTitles = addedIssues.Select(id => 
                            editedProject.Issues.FirstOrDefault(i => i.Id == id)?.Title ?? "未知问题").ToList();
                        issueDetails.Add($"新增问题：{string.Join("、", addedIssueTitles)}");
                    }
                    if (removedIssues.Count > 0)
                    {
                        var removedIssueTitles = removedIssues.Select(id => 
                            SelectedProject.Issues.FirstOrDefault(i => i.Id == id)?.Title ?? "未知问题").ToList();
                        issueDetails.Add($"删除问题：{string.Join("、", removedIssueTitles)}");
                    }
                    var detail = issueDetails.Count > 0 ? string.Join("\n", issueDetails) : null;
                    
                    AddProjectLog(SelectedProject, logContent);
                    AddActivity(SelectedProject.Name, logContent, detail);
                }

                RefreshUIRequested?.Invoke();
            }
        }

        private void AddProjectLog(ProjectModel project, string content)
        {
            var log = new ProjectLog
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Content = content,
                Author = "系统"
            };

            if (project.Logs == null)
                project.Logs = new System.Collections.Generic.List<ProjectLog>();

            project.Logs.Insert(0, log);
        }

        #endregion

        #region 辅助方法

        private void UpdateStatistics()
        {
            TotalProjects = Projects.Count;
            InProgressCount = Projects.Count(p => p.Stage == "开发中" || p.Stage == "测试中");
            CompletedCount = Projects.Count(p => p.Stage == "已完成");
            PendingCount = Projects.Count(p => p.Stage == "需求确认");
            
            // 待办数量
            TodoCount = Todos.Count(t => !t.IsCompleted);
            
            // 延期项目数量（计划完成日期已过但未完成的项目）
            OverdueCount = Projects.Count(p =>
            {
                if (p.Stage == "已完成") return false;
                if (string.IsNullOrEmpty(p.PlanEndDate)) return false;
                if (DateTime.TryParse(p.PlanEndDate, out DateTime endDate))
                {
                    return endDate < DateTime.Now;
                }
                return false;
            });
        }

        private void AddActivity(string projectName, string content, string detail = null)
        {
            var activity = new ActivityItem
            {
                Time = DateTime.Now.ToString("HH:mm"),
                ProjectName = projectName,
                Content = content,
                Detail = detail
            };
            Activities.Insert(0, activity);
            if (Activities.Count > 20)
            {
                while (Activities.Count > 20)
                    Activities.RemoveAt(Activities.Count - 1);
            }
            _dataService.SaveActivities(Activities.ToList());
        }

        /// <summary>
        /// 保存所有数据
        /// </summary>
        public void SaveAll()
        {
            _dataService.SaveProjects(Projects.ToList());
            _dataService.SaveTodos(Todos.ToList());
            _dataService.SaveActivities(Activities.ToList());
        }

        #endregion
    }
}
