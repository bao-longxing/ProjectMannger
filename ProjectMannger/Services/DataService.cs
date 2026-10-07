using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using ProjectMannger.Models;

namespace ProjectMannger.Services
{
    /// <summary>
    /// 数据服务 - 使用 JSON 文件进行数据持久化
    /// </summary>
    public class DataService
    {
        private static readonly string AppConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ProjectMannger");

        private static readonly string ConfigFile = Path.Combine(AppConfigDir, "config.json");
        
        private string _dataDir;
        private string _projectsFile;
        private string _todosFile;
        private string _activitiesFile;

        private static DataService _instance;
        public static DataService Instance => _instance ?? (_instance = new DataService());

        private DataService()
        {
            // 确保配置目录存在
            if (!Directory.Exists(AppConfigDir))
                Directory.CreateDirectory(AppConfigDir);

            // 加载配置
            var config = LoadConfig();
            _dataDir = config.DataPath ?? Path.Combine(AppConfigDir, "Data");

            // 确保数据目录存在
            if (!Directory.Exists(_dataDir))
                Directory.CreateDirectory(_dataDir);

            UpdateFilePaths();
        }

        private void UpdateFilePaths()
        {
            _projectsFile = Path.Combine(_dataDir, "projects.json");
            _todosFile = Path.Combine(_dataDir, "todos.json");
            _activitiesFile = Path.Combine(_dataDir, "activities.json");
        }

        /// <summary>
        /// 获取当前数据目录路径
        /// </summary>
        public string DataPath => _dataDir;

        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        public string ConfigPath => AppConfigDir;

        /// <summary>
        /// 更新数据目录路径
        /// </summary>
        public void UpdateDataPath(string newPath)
        {
            if (string.IsNullOrWhiteSpace(newPath))
                return;

            // 确保新目录存在
            if (!Directory.Exists(newPath))
                Directory.CreateDirectory(newPath);

            _dataDir = newPath;
            UpdateFilePaths();

            // 保存配置
            var config = LoadConfig();
            config.DataPath = newPath;
            SaveConfig(config);
        }

        #region 配置管理

        public AppConfig LoadConfig()
        {
            return LoadFromFile<AppConfig>(ConfigFile) ?? new AppConfig();
        }

        public void SaveConfig(AppConfig config)
        {
            SaveToFile(ConfigFile, config);
        }

        #endregion

        #region 项目管理

        public List<ProjectModel> LoadProjects()
        {
            var projects = LoadFromFile<List<ProjectModel>>(_projectsFile);
            if (projects == null)
            {
                // 首次使用（无持久化数据）：创建一个示例项目
                projects = new List<ProjectModel>
                {
                    new ProjectModel
                    {
                        Name = "示例项目",
                        Subtitle = "欢迎使用项目管理器，这里是示例数据",
                        IconColor = "#2563EB",
                        IconKey = "IconMonitor",
                        ProjectType = "软件开发",
                        StartDate = DateTime.Now.ToString("yyyy-MM-dd"),
                        PlanEndDate = DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd"),
                        Priority = "中",
                        Description = "这是一个示例项目，用于帮助你快速了解项目管理器的功能。你可以在左侧点击该项目查看详情，也可以编辑或删除它。",
                        Stage = "需求确认",
                        Progress = 20,
                        CurrentTask = "了解项目管理器的各项功能",
                        NextPlan = "开始创建你的第一个真实项目",
                        TotalTasks = 5,
                        CompletedTasks = 1
                    }
                };
                SaveProjects(projects);
            }
            return projects;
        }

        public void SaveProjects(List<ProjectModel> projects)
        {
            SaveToFile(_projectsFile, projects);
        }

        #endregion

        #region 待办事项

        public List<TodoItem> LoadTodos()
        {
            var todos = LoadFromFile<List<TodoItem>>(_todosFile);
            if (todos == null)
            {
                // 首次使用（无持久化数据）：创建一个示例待办
                todos = new List<TodoItem>
                {
                    new TodoItem
                    {
                        Title = "欢迎！点击这里添加你的第一条待办",
                        Priority = "中",
                        DueTime = DateTime.Now.ToString("HH:mm"),
                        RelatedProject = "示例项目"
                    }
                };
                SaveTodos(todos);
            }
            return todos;
        }

        public void SaveTodos(List<TodoItem> todos)
        {
            SaveToFile(_todosFile, todos);
        }

        #endregion

        #region 最近活动

        public List<ActivityItem> LoadActivities()
        {
            var activities = LoadFromFile<List<ActivityItem>>(_activitiesFile);
            if (activities == null)
            {
                // 首次使用（无持久化数据）：添加欢迎活动
                activities = new List<ActivityItem>
                {
                    new ActivityItem
                    {
                        Time = DateTime.Now.ToString("HH:mm"),
                        ProjectName = "欢迎使用",
                        Content = "欢迎使用项目管理器，祝你使用愉快！",
                        Detail = "首次使用已为你准备了示例项目与示例待办"
                    }
                };
                SaveActivities(activities);
            }
            return activities;
        }

        public void SaveActivities(List<ActivityItem> activities)
        {
            SaveToFile(_activitiesFile, activities);
        }

        #endregion

        #region JSON 序列化辅助

        private static T LoadFromFile<T>(string filePath) where T : class
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                string json = File.ReadAllText(filePath, Encoding.UTF8);
                return DeserializeJson<T>(json);
            }
            catch
            {
                return null;
            }
        }

        private static void SaveToFile<T>(string filePath, T data)
        {
            try
            {
                string json = SerializeToJson(data);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch { }
        }

        /// <summary>
        /// 简单 JSON 序列化（不依赖外部库）
        /// </summary>
        private static string SerializeToJson<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                serializer.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        private static T DeserializeJson<T>(string json) where T : class
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                return serializer.ReadObject(ms) as T;
            }
        }

        #endregion
    }

    /// <summary>
    /// 应用配置
    /// </summary>
    [DataContract]
    public class AppConfig
    {
        [DataMember]
        public string DataPath { get; set; }
    }
}
