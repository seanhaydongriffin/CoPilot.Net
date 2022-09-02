

using System;
using System.Collections.Generic;

namespace SharedProject.Models
{
    [Serializable]
    public class ExecutionGroupScript
    {
        public int? id { get; set; }
        public string host_name { get; set; }
        public int script_id { get; set; }
        public string script_name { get; set; }
        public string selector { get; set; }
        public string post_run_delay { get; set; }
        public string state { get; set; }
        public bool? excluded { get; set; }
        public System.DateTime? start_date_time { get; set; }
        public System.DateTime? end_date_time { get; set; }
        public string em_comment { get; set; }
        public string shared_folder_1 { get; set; }
        public bool? elevated { get; set; }
        public int order_id { get; set; }
        public bool? in_progress { get; set; }



    }

    public class ExecutionGroupScriptEqualityComparer : IEqualityComparer<ExecutionGroupScript>
    {
        public int GetHashCode(ExecutionGroupScript co)
        {
            if (co == null)
            {
                return 0;
            }
            return co.id.GetHashCode();
        }

        public bool Equals(ExecutionGroupScript x1, ExecutionGroupScript x2)
        {
            if (object.ReferenceEquals(x1, x2))
            {
                return true;
            }
            if (object.ReferenceEquals(x1, null) ||
                object.ReferenceEquals(x2, null))
            {
                return false;
            }
            return x1.id == x2.id && x1.state == x2.state && x1.start_date_time == x2.start_date_time && x1.end_date_time == x2.end_date_time;
        }
    }



}
