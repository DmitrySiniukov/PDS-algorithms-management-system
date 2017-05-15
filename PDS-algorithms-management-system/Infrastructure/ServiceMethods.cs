using Microsoft.CSharp;
using OptimalSchedulingLogic;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Enterprise.Models;

namespace Enterprise.Infrastructure
{
    public class ServiceMethods
    {
        public static MethodInfo GetAlgorithmMethod(string code)
        {
            var entireCode = string.Format(@"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;
            namespace TempAssembly
            {{
	            class TempProgram
	            {{
                    public static IEnumerable<IEnumerable<int>> BuildSchedule(int m, IEnumerable<double> lengths, IEnumerable<DateTime> deadlines)
		            {{
                        {0}
                    }}
		            private static List<Task> convertTasks(IEnumerable<double> lengths, IEnumerable<DateTime> deadlines)
		            {{
			            var result = new List<Task>();
			            var dArray = deadlines.ToArray();
			            var i = 0;
			            foreach (var l in lengths)
			            {{
				            result.Add(new Task(i, l, dArray[i]));
				            i++;
			            }}
			            return result;
		            }}
		            class Task
		            {{
			            public int Id {{ get; private set; }}
			            public double Duration {{ get; private set; }}
			            public DateTime Deadline {{ get; private set; }}
			            public DateTime ExtremeTime {{ get; private set; }}
			            public Task(int id, double duration, DateTime deadline)
			            {{
				            Id = id;
				            Duration = duration;
				            Deadline = deadline;
				            ExtremeTime = Deadline.AddMinutes(-Duration);
			            }}
		            }}
		            class Machine
		            {{
			            public int Id {{ get; private set; }}
			            public Machine(int id) {{ Id = id; }}
		            }}
		            class MachineSchedule : ICloneable, IComparable<MachineSchedule>
		            {{
			            public Machine Machine {{ get; private set; }}
			            public DateTime StartTime {{ get; set; }}
			            public LinkedList<Task> Tasks {{ get; private set; }}
			            public MachineSchedule(Machine machine, DateTime startTime, LinkedList<Task> tasks)
			            {{
				            Machine = machine;
				            StartTime = startTime;
				            Tasks = tasks;
			            }}
			            public MachineSchedule(Machine machine) : this(machine, DateTime.MaxValue, new LinkedList<Task>()) {{ }}
			            public MachineSchedule() : this(null, DateTime.MaxValue, new LinkedList<Task>()) {{ }}
			            public object Clone() {{ return new MachineSchedule(Machine, StartTime, new LinkedList<Task>(Tasks)); }}
			            public int CompareTo(MachineSchedule other)
			            {{
				            if (other == null) {{ return 1; }}
				            return StartTime == other.StartTime ? Machine.Id.CompareTo(other.Machine.Id) : StartTime.CompareTo(other.StartTime);
			            }}
		            }}
		            class MachineScheduleWrapper : IComparable<MachineScheduleWrapper>, ICloneable
		            {{
			            public MachineSchedule Schedule {{ get; private set; }}
			            public DateTime EndTime {{ get; set; }}
			            public MachineScheduleWrapper(MachineSchedule schedule) : this(schedule, DateTime.MinValue) {{ }}
			            public MachineScheduleWrapper(MachineSchedule schedule, DateTime endTime)
			            {{
				            Schedule = schedule;
				            EndTime = endTime;
			            }}
			            public int CompareTo(MachineScheduleWrapper other)
			            {{
				            var endTimeCompResult = EndTime.CompareTo(other.EndTime);
				            return endTimeCompResult == 0 ? Schedule.Machine.Id.CompareTo(other.Schedule.Machine.Id) : endTimeCompResult;
			            }}
			            public object Clone() {{ return new MachineScheduleWrapper((MachineSchedule)Schedule.Clone(), EndTime); }}
		            }}
		            class SuspectedTask : IComparable<SuspectedTask>
		            {{
			            public Task Task {{ get; private set; }}
			            public List<int> PossibleMachines {{ get; private set; }}
			            public SuspectedTask(Task task, List<int> possibleMachines)
			            {{
				            Task = task;
				            PossibleMachines = possibleMachines;
			            }}
			            public int CompareTo(SuspectedTask other)
			            {{
				            var dealineCompResult = Task.Deadline.CompareTo(other.Task.Deadline);
				            return dealineCompResult == 0 ? Task.Id.CompareTo(other.Task.Id) : dealineCompResult;
			            }}
		            }}
	            }}
            }}", code);

            var provider = new CSharpCodeProvider();
            var parameters = new CompilerParameters();
            parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("System.Data.dll");
            parameters.ReferencedAssemblies.Add("System.Data.DataSetExtensions.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.Linq.dll");
            var results = provider.CompileAssemblyFromSource(parameters, entireCode);
            if (results.Errors.HasErrors)
            {
                var sb = new StringBuilder();
                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(string.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }
                throw new InvalidOperationException(sb.ToString());
            }

            var assembly = results.CompiledAssembly;
            var program = assembly.GetType("TempAssembly.TempProgram");
            var result = program.GetMethod("BuildSchedule");
            return result;
        }

        public static IEnumerable<IEnumerable<int>> BuildSchedule(MethodInfo algorithmMethod, int m,
            IEnumerable<Task> tasks)
        {
            var lengths = new List<double>();
            var deadlines = new List<DateTime>();
            foreach (var t in tasks)
            {
                lengths.Add(t.Duration);
                deadlines.Add(t.Deadline);
            }
            return BuildSchedule(algorithmMethod, m, lengths, deadlines);
        }

        public static IEnumerable<IEnumerable<int>> BuildSchedule(MethodInfo algorithmMethod, int m,
            IEnumerable<double> lengths, IEnumerable<DateTime> deadlines)
        {
            return algorithmMethod.Invoke(null, new object[] {m, lengths, deadlines}) as IEnumerable<IEnumerable<int>>;
        }

        public static Schedule ConvertSchedule(IEnumerable<IEnumerable<int>> algorithmOutput, List<Task> tasks)
        {
            var machineSchedules = new List<MachineSchedule>();
            var machineId = 0;
            foreach (var machineInfo in algorithmOutput)
            {
                var machineSchedule = new MachineSchedule(new Machine(++machineId, ""));
                foreach (var taskIndex in machineInfo)
                {
                    machineSchedule.Tasks.AddLast(tasks[taskIndex]);
                }
                machineSchedule.CalculateStartTime();
                machineSchedules.Add(machineSchedule);
            }
            return new Schedule(machineSchedules);
        }

        public static MethodInfo GetAnalyticFunction(string code)
        {
            var entireCode = string.Format(@"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;
            using Enterprise.Infrastructure;
            namespace TempAssembly
            {{
	            class TempProgram
	            {{
                    public static double Function(Func<int, IEnumerable<double>, IEnumerable<DateTime>, IEnumerable<IEnumerable<int>>> algorithm, AlgorithmInput input)
                    {{
                        {0}
                    }}		            
	            }}
            }}", code);

            var provider = new CSharpCodeProvider();
            var parameters = new CompilerParameters();
            parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("System.Data.dll");
            parameters.ReferencedAssemblies.Add("System.Data.DataSetExtensions.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.dll");
            parameters.ReferencedAssemblies.Add("System.Xml.Linq.dll");
            parameters.ReferencedAssemblies.Add((typeof (ServiceMethods)).Assembly.CodeBase);
            var results = provider.CompileAssemblyFromSource(parameters, entireCode);
            if (results.Errors.HasErrors)
            {
                var sb = new StringBuilder();
                foreach (CompilerError error in results.Errors)
                {
                    sb.AppendLine(string.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                }
                throw new InvalidOperationException(sb.ToString());
            }

            var assembly = results.CompiledAssembly;
            var program = assembly.GetType("TempAssembly.TempProgram");
            var result = program.GetMethod("Function");
            return result;
        }

        public static double CalculateFunctionValue(MethodInfo function, MethodInfo algorithmMethod,
            AlgorithmInput input)
        {
            Func<int, IEnumerable<double>, IEnumerable<DateTime>, IEnumerable<IEnumerable<int>>> algorithm =
                (m, l, d) => BuildSchedule(algorithmMethod, m, l, d);
            return (double) function.Invoke(null, new object[] {algorithm, input});
        }

        public static List<GraphPoint> BuildGraph(Analytic analytic, MethodInfo algorithmMethod,
            List<AlgorithmInput> inputs)
        {
            var analFunction = GetAnalyticFunction(analytic.FunctionCode);
            var inputsCopy = new List<AlgorithmInput>(inputs);
            inputsCopy.Sort((x, y) => x.Characteristic.CompareTo(y.Characteristic));
            var xCurrent = inputsCopy[0].Characteristic;
            var ind = 0;

            var result = new List<GraphPoint>();
            var exit = false;
            while (!exit)
            {
                var xNext = xCurrent + analytic.Step;
                var ySum = 0d;
                var indBefore = ind;
                while (inputsCopy[ind].Characteristic < xNext)
                {
                    ySum += CalculateFunctionValue(analFunction, algorithmMethod, inputs[ind]);
                    ind++;
                    if (ind == inputsCopy.Count)
                    {
                        exit = true;
                        break;
                    }
                }

                var y = (ind == indBefore ? 0d : ySum/(ind - indBefore));
                result.Add(new GraphPoint {X = xCurrent, Y = y});
                xCurrent = xNext;
            }
            
            return result;
        }


        public class GraphPoint
        {
            public double X { get; set; }

            public double Y { get; set; }
        }
    }
}