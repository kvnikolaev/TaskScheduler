using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskSchedulerExample;

public class TaskSchedulerWithPriority : System.Threading.Tasks.TaskScheduler
{
  private static readonly TaskScheduler _scheduler = new TaskSchedulerWithPriority();
  private readonly int _maximumConcurrencyLevel = Math.Max(2, Environment.ProcessorCount);
  private int _tasksQueued = 0;

  private Queue<TaskWithPriority> _normalPriorityTasks = new Queue<TaskWithPriority>();
  private Queue<TaskWithPriority> _lowPriorityTasks = new Queue<TaskWithPriority>();

  protected override IEnumerable<Task> GetScheduledTasks()
  {
    return _normalPriorityTasks.ToArray().Concat(_lowPriorityTasks.ToArray());
  }

  private readonly object locker = new object();
  protected override void QueueTask(Task task)
  {
    lock (locker)
    {
      var priorityTask = task as TaskWithPriority;
      if (priorityTask == null)
        throw new ArgumentException("Argument not TaskWithPriority");
      if (priorityTask.LowPriority)
        _lowPriorityTasks.Enqueue(priorityTask);
      else
        _normalPriorityTasks.Enqueue(priorityTask);

      if (_tasksQueued < _maximumConcurrencyLevel)
      {
        _tasksQueued++;
        NotifyThreadPoolOfPendingWork();
      }
    }
  }

  [ThreadStatic]
  private static bool _currentThreadProcessingTask; // blocking current thread while executing a task

  private void NotifyThreadPoolOfPendingWork()
  {
    ThreadPool.UnsafeQueueUserWorkItem((_) =>
    {
      _currentThreadProcessingTask = true;
      try
      {
        while (true)
        {
          TaskWithPriority item;
          lock (locker)
          {
            if (_normalPriorityTasks.Count != 0)
            {
              item = _normalPriorityTasks.Dequeue();
            }
            else if (_lowPriorityTasks.Count != 0)
            {
              item = _lowPriorityTasks.Dequeue();
            }
            else
            {
              _tasksQueued--;
              break;
            }
          }
          base.TryExecuteTask(item);
        }
      }
      finally { _currentThreadProcessingTask = false; }
    }, null);
  }

  protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
  {
    if (!_currentThreadProcessingTask) return false;

    if (!taskWasPreviouslyQueued) return false; // We're executing only queued tasks

    if (!(task is TaskWithPriority)) return false; // We're executing only our tasks with priority

#if DEBUG
    throw new Exception("Неожиданная ошибка в менеджере потоков");
#endif

    //TaskWithPriority item;
    //lock (locker)
    //{
    //  if (_normalPriorityTasks.Contains(task))
    //    return base.TryExecuteTask(task);

    //}
    //if (item != null)
    //  return base.TryExecuteTask(task);
    //else return false;

    return false;
  }

  public static TaskScheduler Scheduler => _scheduler;
}

