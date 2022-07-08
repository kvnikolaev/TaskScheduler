using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskSchedulerExample;

namespace UnitTestProject
{
  [TestClass]
  public class UnitTest
  {
    [TestMethod]
    public void DefaulSchedulerTimeOut()
    {
      List<Task> tasks = new List<Task>();
      for (int i = 0; i < 250; i++)
      {
        tasks.Add( Task.Run(() => { Thread.Sleep(500); }));
      }

      tasks.Add(Task.Run(() => { Assert.IsTrue(true); Environment.Exit(0); }));

      Task.WaitAll(tasks.ToArray());
    }

    [TestMethod]
    public void PrioritySchedulerTimeOut_Run()
    {
      List<TaskWithPriority> tasks = new List<TaskWithPriority>();
      for (int i = 0; i < 250; i++)
      {
        tasks.Add(TaskWithPriority.RunWithPriority(() => Thread.Sleep(500), lowPriority: true));
      }

      tasks.Add(TaskWithPriority.RunWithPriority(() => { Assert.IsTrue(true); Environment.Exit(0); }, lowPriority: false));

      Task.WaitAll(tasks.ToArray());
    }

    [TestMethod]
    public void PrioritySchedulerTimeOut_Start()
    {
      List<TaskWithPriority> tasks = new List<TaskWithPriority>();
      for (int i = 0; i < 250; i++)
      {
        var t = new TaskWithPriority(() => Thread.Sleep(500), lowPriority: true);
        t.StartWithPriority(true);
        tasks.Add(t);
      }

      var t2 = new TaskWithPriority(() => { Assert.IsTrue(true); Environment.Exit(0); }, lowPriority: false);
      t2.StartWithPriority(false);
      tasks.Add(t2);
      Task.WaitAll(tasks.ToArray());
    }
  }
}
