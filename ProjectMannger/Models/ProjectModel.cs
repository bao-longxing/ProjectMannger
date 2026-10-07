using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ProjectMannger.Models
{
    /// <summary>
    /// 项目模型
    /// </summary>
    [DataContract]
    public class ProjectModel
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Name { get; set; }
        [DataMember] public string Subtitle { get; set; }
        [DataMember] public string IconColor { get; set; } = "#2563EB";
        [DataMember] public string IconKey { get; set; } = "IconMonitor";
        [DataMember] public string ProjectType { get; set; } = "软件开发";
        [DataMember] public string StartDate { get; set; }
        [DataMember] public string PlanEndDate { get; set; }
        [DataMember] public string Priority { get; set; } = "中";
        [DataMember] public string Description { get; set; }
        [DataMember] public string Stage { get; set; } = "需求确认";
        [DataMember] public int Progress { get; set; }
        [DataMember] public string CurrentTask { get; set; }
        [DataMember] public string NextPlan { get; set; }
        [DataMember] public int TotalTasks { get; set; }
        [DataMember] public int CompletedTasks { get; set; }
        [DataMember] public List<ProjectLog> Logs { get; set; } = new List<ProjectLog>();
        [DataMember] public List<ProjectIssue> Issues { get; set; } = new List<ProjectIssue>();

        public ProjectModel()
        {
            Id = Guid.NewGuid().ToString("N");
        }
    }

    /// <summary>
    /// 项目日志
    /// </summary>
    [DataContract]
    public class ProjectLog
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Date { get; set; }
        [DataMember] public string Content { get; set; }
        [DataMember] public string Author { get; set; }

        public ProjectLog()
        {
            Id = Guid.NewGuid().ToString("N");
            Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }
    }

    /// <summary>
    /// 项目问题
    /// </summary>
    [DataContract]
    public class ProjectIssue
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Title { get; set; }
        [DataMember] public string Description { get; set; }
        [DataMember] public string Status { get; set; } = "待处理";
        [DataMember] public string Priority { get; set; } = "中";
        [DataMember] public string CreatedDate { get; set; }
        [DataMember] public string Assignee { get; set; }

        public ProjectIssue()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }
    }
}
