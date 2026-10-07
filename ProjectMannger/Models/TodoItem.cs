using System;
using System.Runtime.Serialization;

namespace ProjectMannger.Models
{
    /// <summary>
    /// 待办事项模型
    /// </summary>
    [DataContract]
    public class TodoItem
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Title { get; set; }
        [DataMember] public string Priority { get; set; } = "中";
        [DataMember] public string DueTime { get; set; }
        [DataMember] public bool IsCompleted { get; set; }
        [DataMember] public string RelatedProject { get; set; }
        [DataMember] public DateTime CreatedDate { get; set; }

        public TodoItem()
        {
            Id = Guid.NewGuid().ToString("N");
            CreatedDate = DateTime.Now;
        }
    }
}
