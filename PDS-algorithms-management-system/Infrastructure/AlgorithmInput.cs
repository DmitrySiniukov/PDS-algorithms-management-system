using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Enterprise.Models;
using OptimalSchedulingLogic;

namespace Enterprise.Infrastructure
{
    public class AlgorithmInput
    {
        private Input _model;

        private DateTime _zeroDate = new DateTime(2016, 12, 28, 12, 0, 0);

        public int Id { get { return _model.Id; } }

        public double Characteristic { get { return _model.Characteristic; } }

        public int AnalyticId { get { return _model.AnalyticId; } }

        public int MachineNumber { get { return _model.MachineNumber; } }

        private List<Task> _tasks;

        public List<Task> Tasks
        {
            get
            {
                if (_tasks == null)
                {
                    _tasks = new List<Task>();
                    var taskStrs = _model.Tasks.Split(';');
                    var taskId = 0;
                    foreach (var taskStr in taskStrs)
                    {
                        var ld = taskStr.Split(',');
                        _tasks.Add(new Task(++taskId, "", double.Parse(ld[0]), _zeroDate.AddMinutes(double.Parse(ld[1]))));
                    }
                }
                return _tasks;
            }
        }

        public Schedule _solution;

        public Schedule Solution
        {
            get
            {
                if (_solution == null)
                {
                    var machineSchedules = new List<MachineSchedule>();
                    var machineStrs = _model.Solution.Split(';');
                    var machineId = 0;
                    foreach (var machineStr in machineStrs)
                    {
                        var machineInfo = machineStr.Split(',');
                        var machineSchedule = new MachineSchedule(new Machine(++machineId, ""),
                            _zeroDate.AddMinutes(double.Parse(machineInfo[0])), new LinkedList<Task>());
                        var tasks = Tasks;
                        for (var i = 1; i < machineInfo.Length; i++)
                        {
                            machineSchedule.Tasks.AddLast(tasks[int.Parse(machineInfo[i])]);
                        }
                        machineSchedules.Add(machineSchedule);
                    }
                    // Add missing (empty) machine schedules
                    while (machineId < MachineNumber)
                    {
                        machineSchedules.Add(new MachineSchedule(new Machine(++machineId, "")));
                    }
                    _solution = new Schedule(machineSchedules);
                }
                return _solution;
            }
        }

        public AlgorithmInput(Input model)
        {
            _model = model;
        }
    }
}