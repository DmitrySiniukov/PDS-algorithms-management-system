using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Enterprise.Infrastructure
{
    /// <summary>
	/// Represents a task for processing
	/// </summary>
	public class ScheduleTask
    {
        /// <summary>
        /// Identifier
        /// </summary>
        public int TechnologyId { get; }

        /// <summary>
        /// Duration
        /// </summary>
        public double Duration { get; }

        /// <summary>
        /// Product id
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Product name
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Deadline
        /// </summary>
        public DateTime Deadline { get; }

        /// <summary>
        /// Description
        /// </summary>
	    public string Description { get; }

	    /// <summary>
        /// Extreme start time
        /// </summary>
        public DateTime ExtremeTime
        {
            get { return Deadline.AddSeconds(-Duration); }
        }

        /// <summary>
        /// Task id
        /// </summary>
	    public int TaskId { get; }

	    /// <summary>
        /// Task name
        /// </summary>
        public string TaskName { get; }

        /// <summary>
        /// Compatible departments
        /// </summary>
        public List<int> CompatibleDepartments { get; }


        public ScheduleTask(int technologyId, double duration, int productId, string productName, DateTime deadline, int taskId,
            string taskName, string description)
        {
	        TechnologyId = technologyId;
            Duration = duration;
            ProductId = productId;
            ProductName = productName;
            Deadline = deadline;
	        TaskId = taskId;
            TaskName = taskName;
            Description = description;
            CompatibleDepartments = new List<int>();
        }
    }
}