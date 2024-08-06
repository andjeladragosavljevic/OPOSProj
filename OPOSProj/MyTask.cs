using Nito.AsyncEx;

namespace OposScheduler
{
    /// <summary>
    /// Class that represents my task, and inherits the task class.
    /// </summary>
    public class MyTask : Task
    {
        public int Priority { get; set; }
        public int Duration { get; set; }
        public int Deadline { get; set; }
        public int NumberOfCores { get; set; }
        public Action Action { get; set; }
        public object? Resource { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public CancellationTokenSource CancellationTokenSource;
        public ManualResetEvent? ManualResetEvent { get; set; }

        public MyTask(Action action, int priority, int duraion, int deadline, CancellationTokenSource cts) : base(action)
        {
          
            Action = action;
            Priority = priority;
            Duration = duraion;
            Deadline = deadline;
            CancellationTokenSource = cts;
            CancellationToken = cts.Token;

        }
        public MyTask(Action action, int duration, int deadline, CancellationTokenSource cts) : base(action)
        {

            Action = action;
            Duration = duration;
            Deadline = deadline;
            CancellationTokenSource = cts;
            CancellationToken = cts.Token;

        }
        public MyTask(Action action, int priority, int duraion, int deadline, CancellationTokenSource cts, object resources) : base(action)
        {
            Action = action;
            Priority = priority;
            Duration = duraion;
            Deadline = deadline;
            CancellationTokenSource = cts;
            CancellationToken = cts.Token;
            Resource = resources;

        }
    }
}
