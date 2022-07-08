using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskSchedulerExample;

class Example
{
  static void Main()
  {

    List<TaskWithPriority> tasks = new List<TaskWithPriority>();
    for (int i = 0; i < 250; i++)
    {
      var t = new TaskWithPriority(() => Thread.Sleep(500), lowPriority: true);
      t.StartWithPriority(true);
      tasks.Add(t);
    }

    var t2 = new TaskWithPriority(() => { var t = TaskSchedulerWithPriority.Scheduler; Environment.Exit(0); }, lowPriority: false);
    t2.StartWithPriority(false);
    tasks.Add(t2);
    Task.WaitAll(tasks.ToArray());

    Console.ReadLine();
  }
  static void AltMain()
  {
    // Create a scheduler that uses two threads.
    LimitedConcurrencyLevelTaskScheduler lcts = new LimitedConcurrencyLevelTaskScheduler(3);
    List<Task> tasks = new List<Task>();

    // Create a TaskFactory and pass it our custom scheduler.
    TaskFactory factory = new TaskFactory(lcts);
    CancellationTokenSource cts = new CancellationTokenSource();

    // Use our factory to run a set of tasks.
    Object lockObj = new Object();
    int outputItem = 0;

    for (int tCtr = 0; tCtr <= 4; tCtr++)
    {
      int iteration = tCtr;
      Task t = factory.StartNew(() => {
        var rt = Task.CurrentId;
        for (int i = 0; i < 1000; i++)
        {
          lock (lockObj)
          {
            Console.Write("{0} in task t-{1} on thread {2}   ",
                          i.ToString(), iteration, Thread.CurrentThread.ManagedThreadId);
            outputItem++;
            if (outputItem % 3 == 0)
              Console.WriteLine();
          }
        }
      }, cts.Token);
      tasks.Add(t);
    }
    // Use it to run a second set of tasks.
    for (int tCtr = 0; tCtr <= 4; tCtr++)
    {
      int iteration = tCtr;
      Task t1 = factory.StartNew(() => {
        for (int outer = 0; outer <= 10; outer++)
        {
          for (int i = 0x21; i <= 0x7E; i++)
          {
            lock (lockObj)
            {
              Console.Write("'{0}' in task t1-{1} on thread {2}   ",
                            i, iteration, Thread.CurrentThread.ManagedThreadId);
              outputItem++;
              if (outputItem % 3 == 0)
                Console.WriteLine();
            }
          }
        }
      }, cts.Token);
      tasks.Add(t1);
    }

    // Wait for the tasks to complete before displaying a completion message.
    Task.WaitAll(tasks.ToArray());
    cts.Dispose();
    Console.WriteLine("\n\nSuccessful completion.");
    Console.ReadLine();
  }
}

// Provides a task scheduler that ensures a maximum concurrency level while
// running on top of the thread pool.
/// <summary>
/// Test Task Sheduler
/// </summary>
public class LimitedConcurrencyLevelTaskScheduler : System.Threading.Tasks.TaskScheduler
{
  // Indicates whether the current thread is processing work items.
  [ThreadStatic]
  private static bool _currentThreadIsProcessingItems;
  // The list of tasks to be executed
  private readonly LinkedList<Task> _tasks = new LinkedList<Task>(); // protected by lock(_tasks)
  // The maximum concurrency level allowed by this scheduler.
  private readonly int _maxDegreeOfParallelism;
  // Indicates whether the scheduler is currently processing work items.
  private int _delegatesQueuedOrRunning = 0;

  // Creates a new instance with the specified degree of parallelism.
  public LimitedConcurrencyLevelTaskScheduler(int maxDegreeOfParallelism)
  {
    if (maxDegreeOfParallelism < 1) throw new ArgumentOutOfRangeException("maxDegreeOfParallelism");
    _maxDegreeOfParallelism = maxDegreeOfParallelism;
  }

  // Queues a task to the scheduler.
  protected sealed override void QueueTask(Task task)
  {
    // Add the task to the list of tasks to be processed.  If there aren't enough
    // delegates currently queued or running to process tasks, schedule another.
    lock (_tasks)
    {
      var t = task as TaskWithPriority;

      _tasks.AddLast(task);
      if (_delegatesQueuedOrRunning < _maxDegreeOfParallelism)
      {
        ++_delegatesQueuedOrRunning;
        NotifyThreadPoolOfPendingWork();
      }
    }
  }

  // Inform the ThreadPool that there's work to be executed for this scheduler.
  private void NotifyThreadPoolOfPendingWork()
  {
    ThreadPool.UnsafeQueueUserWorkItem(_ =>
    {
      // Note that the current thread is now processing work items.
      // This is necessary to enable inlining of tasks into this thread.
      _currentThreadIsProcessingItems = true;
      try
      {
        // Process all available items in the queue.
        while (true)
        {
          Task item;
          lock (_tasks)
          {
            // When there are no more items to be processed,
            // note that we're done processing, and get out.
            if (_tasks.Count == 0)
            {
              --_delegatesQueuedOrRunning;
              break;
            }

            // Get the next item from the queue
            item = _tasks.First.Value;
            _tasks.RemoveFirst();
          }

          // Execute the task we pulled out of the queue
          base.TryExecuteTask(item);
        }
      }
      // We're done processing items on the current thread
      finally { _currentThreadIsProcessingItems = false; }
    }, null);
  }

  // Attempts to execute the specified task on the current thread.
  protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
  {
    // If this thread isn't already processing a task, we don't support inlining
    if (!_currentThreadIsProcessingItems) return false;

    // If the task was previously queued, remove it from the queue
    if (taskWasPreviouslyQueued)
      // Try to run the task.
      if (TryDequeue(task))
        return base.TryExecuteTask(task);
      else
        return false;
    else
      return base.TryExecuteTask(task);
  }

  // Attempt to remove a previously scheduled task from the scheduler.
  protected sealed override bool TryDequeue(Task task)
  {
    lock (_tasks) return _tasks.Remove(task);
  }

  // Gets the maximum concurrency level supported by this scheduler.
  public sealed override int MaximumConcurrencyLevel { get { return _maxDegreeOfParallelism; } }

  // Gets an enumerable of the tasks currently scheduled on this scheduler.
  protected sealed override IEnumerable<Task> GetScheduledTasks()
  {
    bool lockTaken = false;
    try
    {
      Monitor.TryEnter(_tasks, ref lockTaken);
      if (lockTaken) return _tasks;
      else throw new NotSupportedException();
    }
    finally
    {
      if (lockTaken) Monitor.Exit(_tasks);
    }
  }
}