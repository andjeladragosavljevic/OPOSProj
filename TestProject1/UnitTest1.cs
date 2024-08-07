using OposFS;
using OposScheduler;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace TestProject1
{
    [SupportedOSPlatform("windows")]
    public class UnitTest1
    {

        private const int DefaultDuration = 1000;
        private const int DefaultDeadline = 5000;

        [Fact]
        public void MyScheduler_Constructor()
        {
            int expectedMaxDegree = 3;
            int expectedNumOfCores = 5;

            MyScheduler scheduler = new MyScheduler(3, 5);
            int actualMaxDegree = scheduler.MaxDegreeOfParallelism;
            int actualNumOfCores = scheduler.NumberOfCores;

            Assert.Equal(expectedMaxDegree, actualMaxDegree);
            Assert.Equal(expectedNumOfCores, actualNumOfCores);

        }



        [Fact]
        public void MyTask_Constructor1()
        {
            MyTask myTask = new MyTask(testFunction, DefaultDuration, DefaultDeadline, new CancellationTokenSource());
            int actualDuration = myTask.Duration;
            int actualDeadline = myTask.Deadline;

            Assert.Equal(DefaultDuration, actualDuration);
            Assert.Equal(DefaultDeadline, actualDeadline);

        }
        [Fact]
        public void MyTask_Constructor2()
        {
            int expectedPriority = 5;

            MyTask myTask = new MyTask(testFunction, expectedPriority, DefaultDuration, DefaultDeadline, new CancellationTokenSource());
            int actualPriority = myTask.Priority;

            Assert.Equal(expectedPriority, actualPriority);


        }
        [Fact]
        public void MyTask_Constructor3()
        {
            string expectedResource = "resource";
            int expectedPriority = 5;

            MyTask myTask = new MyTask(testFunction, expectedPriority, DefaultDuration, DefaultDeadline, new CancellationTokenSource(), "resource");
            Assert.Equal(expectedResource, myTask.Resource);

        }


        [Fact]
        public void MyScheduler_PriorityScheduling()
        {
            int expectedPriority = 4;

            MyTask myTask1 = new MyTask(testFunction, 2, DefaultDuration, 1000, new CancellationTokenSource());
            MyTask myTask2 = new MyTask(testFunction, 4, DefaultDuration, 1000, new CancellationTokenSource());

            MyScheduler scheduler = new MyScheduler(1, 5);
            MyScheduler.PriorityScheduling = true;

            myTask1.Start(scheduler);
            myTask2.Start(scheduler);

            Task.Delay(100).Wait();

            var task = scheduler.GetTasks()[0] as MyTask;
            if (task != null)
            {
                int actualPriority = task.Priority;
                Assert.Equal(expectedPriority, actualPriority);
            }
            else
            {

                Assert.True(false, "The task is not of type MyTask.");
            }

        }

        [Fact]
        public void MyScheduler_PreemptiveScheduling()
        {
            bool expected = false;

            MyTask myTask1 = new MyTask(testFunction, 2, DefaultDuration, 1000, new CancellationTokenSource());
            MyTask myTask2 = new MyTask(testFunction, 4, DefaultDuration, 1000, new CancellationTokenSource());

            MyScheduler scheduler = new MyScheduler(1, 5);
            MyScheduler.PreemptiveScheduling = true;

            myTask1.Start(scheduler);
            Task.Delay(100).Wait();
            myTask2.Start(scheduler);

            Task.Delay(100).Wait();
            bool actual = myTask1.CancellationToken.IsCancellationRequested;
            Assert.Equal(expected, actual);

        }

        [Fact]
        public void MyScheduler_PIP()
        {
            int expected = 5;
            bool expectedCancellation = false;

            MyTask myTask1 = new MyTask(testFunction, 2, DefaultDuration, 1000, new CancellationTokenSource(), "resource");
            MyTask myTask2 = new MyTask(testFunction, 4, DefaultDuration, 1000, new CancellationTokenSource(), "resource");

            MyScheduler scheduler = new MyScheduler(1, 5);
            MyScheduler.PreemptiveScheduling = true;

            myTask1.Start(scheduler);
            Task.Delay(100).Wait();
            myTask2.Start(scheduler);

            Task.Delay(100).Wait();
            int actual = myTask1.Priority;
            bool actualCancellation = myTask1.CancellationToken.IsCancellationRequested;

            Assert.Equal(expected, actual);
            Assert.Equal(expectedCancellation, actualCancellation);

        }


        [Fact]
        public void MyScheduler_Deadlock()
        {
            string file = "deadlock.txt";
            bool expectedStatus1 = true;
            bool expectedStatus2 = true;


            MyTask myTask1 = new MyTask(delegate { TaskWithResources(file); }, 2, DefaultDuration, 1000, new CancellationTokenSource(), "resource");
            MyTask myTask2 = new MyTask(delegate { TaskWithResources2(file); }, 4, DefaultDuration, 1000, new CancellationTokenSource(), "resource");

            MyScheduler scheduler = new MyScheduler(2, 5);
            MyScheduler.PreemptiveScheduling = true;

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            myTask1.Start(scheduler);
            Task.Delay(10000).Wait();
            myTask2.Start(scheduler);

            Task.Delay(1000).Wait();
            bool actualStatus1 = !myTask1.IsCompleted;
            bool actualStatus2 = !myTask2.IsCompleted;


            Assert.Equal(expectedStatus1, actualStatus1);
            Assert.Equal(expectedStatus2, actualStatus2);

            manualResetEvent.Set();

        }
        [Fact]
        public void Sharpening()
        {
            string file1 = "pexels-photo-922611.jpeg";
            string file2 = "pexels-photo-922611 - Copy.jpeg";
            MyScheduler scheduler = new MyScheduler(2, 6);
            TaskFactory taskFactory = new TaskFactory(scheduler);

            Sharpen sharpen1 = new Sharpen();
            Sharpen sharpen2 = new Sharpen();
            Sharpen.Parallelism = 2;
            Sharpen.Test = true;

            Task task1 = sharpen1.SharpeningAsync(file1, taskFactory);
            Task task2 = sharpen2.Sharpening(file2, taskFactory);

            Task.WaitAll(task1, task2);
            Sharpen.Test = false;

            Assert.True(task1.IsCompletedSuccessfully);
            Assert.True(task2.IsCompletedSuccessfully);
            Assert.True(sharpen1.Time < sharpen2.Time);

        }


        private void testFunction()
        {
            for (int i = 0; i < 10; i++)
            {

            }
        }

        object locker = new object();
        object locker1 = new object();
        TimeSpan timeout = TimeSpan.FromMilliseconds(500);
        void TaskWithResources(string file)
        {
            using FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            bool lockWasTaken = false;
            bool lockWasTaken1 = false;
            try
            {
                Monitor.TryEnter(locker, timeout, ref lockWasTaken);
                {
                    Thread.Sleep(1000);
                    try
                    {
                        Monitor.TryEnter(locker1, timeout, ref lockWasTaken1);
                        {
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine("opos1\n");
                            sw.Close();
                        }
                    }
                    finally { if (lockWasTaken1) { Monitor.Exit(locker1); } }
                }
            }
            finally { if (lockWasTaken) Monitor.Exit(locker); }


        }
        void TaskWithResources2(string file)
        {

            using FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            bool lockWasTaken = false;
            bool lockWasTaken1 = false;
            try
            {
                Monitor.TryEnter(locker1, timeout, ref lockWasTaken1);
                {
                    Thread.Sleep(1000);
                    try
                    {
                        Monitor.TryEnter(locker, timeout, ref lockWasTaken);
                        {
                            StreamWriter sw = new StreamWriter(fs);
                            sw.WriteLine("opos2\n");
                            sw.Close();
                        }
                    }
                    finally { if (lockWasTaken) { Monitor.Exit(locker1); } }
                }
            }
            finally { if (lockWasTaken1) Monitor.Exit(locker); }
        }


    }
}