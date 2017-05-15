using System;

namespace Scheduling
{
    /// <summary>
    /// Represents a task for processing
    /// </summary>
    public class Task
    {
        #region Properties

        /// <summary>
        /// Identifier
        /// </summary>
        public int Id { get; }
        
        /// <summary>
        /// Duration
        /// </summary>
        public double Duration { get; }

        /// <summary>
        /// Deadline
        /// </summary>
        public DateTime Deadline { get; }

        /// <summary>
        /// Extreme start time
        /// </summary>
        public DateTime ExtremeTime { get; }

        #endregion

        public Task(int id, double duration, DateTime deadline)
        {
            Id = id;
            Duration = duration;
            Deadline = deadline;
            ExtremeTime = Deadline.AddMinutes(-Duration);
        }
    }
}