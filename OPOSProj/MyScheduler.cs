using System.Collections.Concurrent;

namespace OposScheduler
{
    public class MyScheduler : TaskScheduler
    {
        private static bool _currentThreadIsProcessingItems;
        /// <summary>
        /// A collection of tasks that arrived for scheduling.
        /// </summary>
        private readonly BlockingCollection<Task> _readyTasks = new();
        /// <summary>
        /// A collection of currently running tasks.
        /// </summary>
        private readonly List<Task> _running = new();
        public List<Task> GetTasks()
        {
            return _running;
        }
        /// <summary>
        /// Maximum number of tasks that can be executed in parallel.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }
        /// <summary>
        ///  Number of processor cores.
        /// </summary>
        public int NumberOfCores { get; private set; }
        /// <summary>
        /// Indicates whether it is priority scheduling.
        /// </summary>
        public static bool PriorityScheduling { get; set; }
        /// <summary>
        /// Indicates whether it is preemptive priority scheduling.
        /// </summary>
        public static bool PreemptiveScheduling { get; set; }

        private int _delegatesQueuedOrRunning = 0;

       
        public MyScheduler(int maxDegreeOfParallelism, int numOfCores)
        {
            ThreadPool.SetMinThreads(1, 1);
            if (!ThreadPool.SetMaxThreads(numOfCores, 2))
            {
                throw new ArgumentOutOfRangeException(nameof(numOfCores));
            }
            NumberOfCores = numOfCores;
        
            if (maxDegreeOfParallelism < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
            }
            MaxDegreeOfParallelism = maxDegreeOfParallelism;

        }
        /// <summary>
        /// Remove complited tasks.
        /// </summary>
        private void RemoveComplitedTask()
        {
            for (int i = _running.Count - 1; i >= 0; i--)
            {
                var item = _running[i];
                if (item is MyTask task1)
                {
                    if (task1.CancellationToken.IsCancellationRequested)
                    {
                        _running.RemoveAt(i);

                    }
                }
            }

        }
        /// <summary>
        /// Queues a task to the scheduler.
        /// </summary>
        /// <param name="task"></param>
        protected sealed override void QueueTask(Task task)
        {
           RemoveComplitedTask();
           
            _readyTasks.Add(task);
      
            MyTask? myTask = task as MyTask;

            if(myTask != null)
                System.Diagnostics.Debug.WriteLine($"A task with priority {myTask.Priority} has arrived.");

            //Priority scheduling
            if (PriorityScheduling)
            {
                if (myTask != null)
                {
                    
                    List<Task> sorted = _readyTasks.OrderByDescending(x =>
                    {
                        if (x is MyTask task)
                        {
                            return task.Priority;
                        }
                        return 0;
                    }).ToList();
                    while (_readyTasks.TryTake(out _)) { } // Clear collection
                    sorted.ForEach(x => _readyTasks.Add(x));
                }

            }
            if (_delegatesQueuedOrRunning < MaxDegreeOfParallelism)
            {
                ++_delegatesQueuedOrRunning;
                NotifyThreadPoolOfPendingWork();
            }
            else //Preemptive scheduling
            {
                
                if (PreemptiveScheduling && myTask != null)
                {
                   
                    bool resource = false;
                    if (_running != null)
                    {
                        foreach (var task1 in _running)
                        {
                            if (task1 is MyTask task2)
                            {
                                try
                                {
                                    if (task2.Resource != null && myTask.Resource != null)
                                    {
                                        if (task2.Resource.Equals(myTask.Resource))
                                        {
                                            resource = true;

                                            //PIP
                                            if (task2.Priority < myTask.Priority)
                                            {
                                                task2.Priority = ++myTask.Priority;
                                                break;
                                            }
                                        }
                                    }
                                    if (!resource)
                                    {
                                        if (task1 is MyTask task3)
                                        {
                                            resource = false;
                                            if (task3.Priority < myTask.Priority)
                                            {
                                                task3.CancellationTokenSource.Cancel();
                                                _running.Remove(task1);
                                                break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        ///  Inform the ThreadPool that there's work to be executed for this scheduler.
        /// </summary>
        private void NotifyThreadPoolOfPendingWork()
        {
            _ = ThreadPool.UnsafeQueueUserWorkItem(_ =>
              {
                  _currentThreadIsProcessingItems = true;
                  try
                  {

                      while (true)
                      {
                          Task item;

                          if (_readyTasks.Count == 0)
                          {
                              --_delegatesQueuedOrRunning;
                              break;
                          }

                          item = _readyTasks.Take();
                          _running.Add(item);

                          if (item is MyTask myTask)
                          {
                              System.Diagnostics.Trace.WriteLine($"A task with priority {myTask.Priority} has started.");
                
                          }

                          base.TryExecuteTask(item);
                      }
                  }
                  finally { _currentThreadIsProcessingItems = false; }
              }, null);
        }


        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
           
            if (!_currentThreadIsProcessingItems) 
                return false;

            if (taskWasPreviouslyQueued)
                if (TryDequeue(task))
                    return base.TryExecuteTask(task);
                else
                    return false;
            else
                return base.TryExecuteTask(task);
        }

       
        public sealed override int MaximumConcurrencyLevel { get { return MaxDegreeOfParallelism; } }


        protected sealed override IEnumerable<Task> GetScheduledTasks() => _readyTasks;
    }
}