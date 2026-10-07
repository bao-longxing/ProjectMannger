using System;
using System.Runtime.Serialization;

namespace ProjectMannger.Models
{
    /// <summary>
    /// 最近活动记录
    /// </summary>
    [DataContract]
    public class ActivityItem
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string Time { get; set; }
        [DataMember] public string ProjectName { get; set; }
        [DataMember] public string Content { get; set; }
        [DataMember] public string Detail { get; set; }
        [DataMember] public DateTime Timestamp { get; set; }

        public ActivityItem()
        {
            Id = Guid.NewGuid().ToString("N");
            Timestamp = DateTime.Now;
        }
    }
}
