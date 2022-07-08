using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskSchedulerExample
{
  public class TaskWithPriority : Task
  {
    public bool LowPriority { get; set; }
    internal System.Threading.Tasks.TaskScheduler _taskSheduler;

    public TaskWithPriority(Action action, bool lowPriority) : base(action)
    {
      this.LowPriority = lowPriority;
      _taskSheduler = TaskSchedulerWithPriority.Scheduler;
    }

    public static TaskWithPriority RunWithPriority(Action action, bool lowPriority)
    {
      TaskWithPriority task = new TaskWithPriority(action, lowPriority);
      task.StartWithPriority(lowPriority);
      return task;
    }

    public void StartWithPriority(bool lowPriority = false)
    {
      this.LowPriority = lowPriority;
      this.Start(_taskSheduler);
    }
  }
}
