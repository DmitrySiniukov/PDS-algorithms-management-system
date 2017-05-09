using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Enterprise.Infrastructure;
using Enterprise.Models;
using Newtonsoft.Json;

namespace Enterprise.Models
{
	/// <summary>
	/// Represents a schedule
	/// </summary>
	public class Schedule<T> : List<T> where T : MachineSchedule, new()
	{
		/// <summary>
		/// Default constructor
		/// </summary>
		public Schedule()
		{
		}

		/// <summary>
		/// Constructor based on machines
		/// </summary>
		/// <param name="machines"></param>
		public Schedule(IEnumerable<Machine> machines)
		{
			var instance = new T();
			foreach (var machine in machines)
			{
				Add(instance.Create(machine) as T);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="machineSchedules"></param>
		public Schedule(IEnumerable<T> machineSchedules)
		{
			AddRange(machineSchedules);
		}


		public string GetJson()
		{
			var data = new List<object>();
			var links = new List<object>();
			var currentId = 1;
            var linkId = 10;
            foreach (var machine in this.OrderBy(x => x.Machine.DepartmentId).ThenBy(x => x.Machine.Id))
			{
				if (machine.Tasks.Count != 0)
				{
					var parentId = currentId++;
					var startTime = machine.StartTime;
					var previousId = 0;
					var duration = 0d;
					foreach (var task in machine.Tasks)
					{
						data.Add(new
						{
							id = currentId,
							text = task.TaskName,
							start_date = dateString(startTime),
							duration = string.Format("{0}", task.Duration/60),
							parent = parentId.ToString(),
							progress = 0,
							open = true
						});
						startTime = startTime.AddSeconds(task.Duration);

						if (previousId != 0)
						{
							links.Add(new
							{
								id = linkId.ToString(),
								source = previousId.ToString(),
								target = currentId.ToString(),
								type = "0"
							});
							++linkId;
						}
						previousId = currentId;
						duration += task.Duration;
						currentId++;
					}

					data.Add(new
					{
						id = parentId,
						text = string.Format("{0} (дільниця \"{1}\")", machine.Machine.Name, machine.Machine.DepartmentName),
						start_date = dateString(machine.StartTime),
						duration = string.Format("{0}", duration / 60),
						progress = 1,
						open = true
					});
				}
			}
			
			return new JavaScriptSerializer().Serialize(new {data = data, links = links});
		}

		private static string dateString(DateTime date)
		{
			return string.Format("{0:D2}-{1:D2}-{2} {3:D2}:{4:D2}", date.Day, date.Month, date.Year, date.Hour, date.Minute);
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="tasks"></param>
		/// <param name="machines"></param>
		/// <returns></returns>
		public static Schedule<MachineSchedule> BuildSchedule(IEnumerable<ScheduleTask> tasks, IEnumerable<Machine> machines)
		{
			#region Validate arguments

			if (tasks == null)
			{
				throw new ArgumentNullException();
			}

			if (machines == null)
			{
				throw new ArgumentNullException();
			}

			var tasksList = (tasks as List<ScheduleTask>) ?? tasks.ToList();
			var machinesList = (machines as List<Machine>) ?? machines.ToList();

			if (tasksList.Count == 0 || machinesList.Count == 0)
			{
				return new Schedule<MachineSchedule>(machinesList);
			}

			#endregion

			tasksList.Sort((x, y) =>
			{
				var result = x.ExtremeTime.CompareTo(y.ExtremeTime);
				return result == 0 ? x.Duration.CompareTo(y.Duration) : result;
			});

			var initSchedule = initialSchedule(tasksList, machinesList);

			// Initial schedule has been found
			if (initSchedule != null)
			{
				if (initSchedule.AppointedTasks.Count == tasksList.Count)
				{
					return initSchedule.Convert();
				}

				// Algorithm A1.1
				var success = true;
				var sortedSet = new List<InitialMachineSchedule>(initSchedule);
				foreach (var currentTask in tasksList)
				{
					if (initSchedule.AppointedTasks.Contains(currentTask.TechnologyId))
					{
						continue;
					}

					InitialMachineSchedule min = null;
					foreach (var machine in sortedSet)
					{
						if (currentTask.CompatibleDepartments.Contains(machine.Machine.DepartmentId) &&
                            (min == null || machine.EndTime < min.EndTime))
						{
							min = machine;
						}
					}

					if (min == null)
					{
						success = false;
						break;
					}

					if (min.Tasks.Count == 0)
					{
						min.StartTime = currentTask.ExtremeTime;
						min.EndTime = currentTask.Deadline;
						min.Tasks.AddFirst(currentTask);
						continue;
					}

					var newEndTime = min.EndTime.AddSeconds(currentTask.Duration);
					if (newEndTime > currentTask.Deadline)
					{
						success = false;
						break;
					}
					
					min.Tasks.AddLast(currentTask);
					min.EndTime = min.EndTime.AddSeconds(currentTask.Duration);
				}

				if (success)
				{
					return (new InitialSchedule(sortedSet).Convert());
				}
			}

			// Algorithm A2.1

			// Sorg by (d, l)
			tasksList.Sort((x, y) =>
			{
				var result = x.Deadline.CompareTo(y.Deadline);
				return result == 0 ? x.Duration.CompareTo(y.Duration) : result;
			});

			var machineSchedules = new Schedule<MachineSchedule>(machinesList);
			var n = tasksList.Count;

			// Building schedule
			for (var i = n - 1; i >= 0; --i)
			{
				// Find unallowable with minimal start time
				var currentTask = tasksList[i];
				MachineSchedule targetMachine = null;
			    MachineSchedule lastMachine = null;
                foreach (var schedule in machineSchedules)
				{
				    if (currentTask.CompatibleDepartments.Contains(schedule.Machine.DepartmentId))
				    {
				        if (lastMachine == null || lastMachine.StartTime < schedule.StartTime)
				        {
				            lastMachine = schedule;
				        }

				        if (!(currentTask.Deadline > schedule.StartTime) &&
				            (targetMachine == null || schedule.StartTime < targetMachine.StartTime))
				        {
				            targetMachine = schedule;
				        }
				    }
				}

			    if (lastMachine == null)
			    {
                    continue;
			    }

			    // If founded
				if (targetMachine != null)
				{
					targetMachine.Tasks.AddFirst(currentTask);
					targetMachine.StartTime = currentTask.ExtremeTime;
					continue;
				}

				// Else take machine with max start time, find allowable task with max duration
				ScheduleTask longestTask = null;
				var index = i;
				for (var j = i; j >= 0; --j)
				{
					if (tasksList[j].CompatibleDepartments.Contains(lastMachine.Machine.DepartmentId) &&
						!(tasksList[j].Deadline < lastMachine.StartTime) &&
						(longestTask == null || tasksList[j].Duration > longestTask.Duration))
					{
						longestTask = tasksList[j];
						index = j;
					}
				}
                
				lastMachine.Tasks.AddFirst(longestTask);
				lastMachine.StartTime = lastMachine.StartTime.AddSeconds(-longestTask.Duration);

				// Remove appointed task
				tasksList.RemoveAt(index);
			}

			return new Schedule<MachineSchedule>(machineSchedules);
		}

		/// <summary>
		/// Initial schedule
		/// </summary>
		/// <param name="tasks"></param>
		/// <param name="machines"></param>
		/// <returns></returns>
		private static InitialSchedule initialSchedule(List<ScheduleTask> tasks, List<Machine> machines)
		{
			var schedule = new InitialSchedule(machines);
			
			var engaged = new List<int>();
			foreach (var currentTask in tasks)
			{
				int? target = null;
				foreach (var id in engaged)
				{
					if (!currentTask.CompatibleDepartments.Contains(schedule[id].Machine.DepartmentId) || schedule[id].EndTime > currentTask.ExtremeTime)
					{
						continue;
					}

					if (target != null)
					{
						return null;
					}

					target = id;
				}

				if (target != null)
				{
					var t = (int)target;
					schedule[t].Tasks.AddLast(currentTask);
					schedule[t].EndTime = schedule[t].EndTime.AddSeconds(currentTask.Duration);
					schedule.AppointedTasks.Add(currentTask.TechnologyId);
					continue;
				}

				if (engaged.Count == machines.Count)
				{
					return schedule;
				}

				// engage next processor
			    int? index = null;
			    for (var i = 0; i < schedule.Count; i++)
			    {
			        if (!engaged.Contains(i) && currentTask.CompatibleDepartments.Contains(schedule[i].Machine.DepartmentId))
			        {
			            index = i;
			            break;
			        }
			    }
				if (index == null)
				{
					continue;
				}
			    var forEngage = schedule[(int)index];
				forEngage.StartTime = currentTask.ExtremeTime;
				forEngage.Tasks.AddLast(currentTask);
				forEngage.EndTime = currentTask.Deadline;
				schedule.AppointedTasks.Add(currentTask.TechnologyId);
			    engaged.Add((int)index);
			}

			return schedule;
		}


		/// <summary>
		/// Initial machine schedule
		/// </summary>
		private class InitialMachineSchedule : MachineSchedule
		{
			public DateTime EndTime { get; set; }


			public InitialMachineSchedule()
			{
				EndTime = DateTime.MinValue;
			}

			private InitialMachineSchedule(Machine machine) : base(machine)
			{
				EndTime = DateTime.MinValue;
			}


			public override MachineSchedule Create(Machine machine)
			{
				return new InitialMachineSchedule(machine);
			}
		}

		/// <summary>
		/// Initial schedule
		/// </summary>
		private class InitialSchedule : Schedule<InitialMachineSchedule>
		{
			/// <summary>
			/// 
			/// </summary>
			public List<int> AppointedTasks { get; set; }


			/// <summary>
			/// Constructor
			/// </summary>
			/// <param name="machines"></param>
			public InitialSchedule(IEnumerable<Machine> machines) : base(machines)
			{
				AppointedTasks = new List<int>();
			}

			/// <summary>
			/// 
			/// </summary>
			/// <param name="machineSchedules"></param>
			public InitialSchedule(IEnumerable<Schedule<T>.InitialMachineSchedule> machineSchedules) : base(machineSchedules)
			{
				AppointedTasks = new List<int>();
			}


			/// <summary>
			/// 
			/// </summary>
			/// <returns></returns>
			public Schedule<MachineSchedule> Convert()
			{
				var result = new Schedule<MachineSchedule>();
				result.AddRange(this.Cast<MachineSchedule>());
				return result;
			}
		}

		private class EndTimeComparer : IComparer<InitialMachineSchedule>
		{
			public int Compare(InitialMachineSchedule x, InitialMachineSchedule y)
			{
				return x.EndTime == y.EndTime ? x.Machine.Id.CompareTo(y.Machine.Id) : x.EndTime.CompareTo(y.EndTime);
			}
		}

		private class StartTimeComparer : IComparer<MachineSchedule>
		{
			public int Compare(MachineSchedule x, MachineSchedule y)
			{
				return x.StartTime == y.StartTime ? x.Machine.Id.CompareTo(y.Machine.Id) : x.StartTime.CompareTo(y.StartTime);
			}
		}
	}
}