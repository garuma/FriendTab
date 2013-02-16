using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace FriendTab
{
	public class SerialScheduler : TaskScheduler
	{
		ConcurrentQueue<Task> taskQueue = new ConcurrentQueue<Task> ();
		int running;
		WaitCallback callback;

		public static readonly SerialScheduler Instance;
		public static readonly TaskFactory Factory;

		static SerialScheduler ()
		{
			Instance = new SerialScheduler ();
			Factory = new TaskFactory (Instance);
		}

		public SerialScheduler ()
		{
			callback = ProcesserAction;
		}

		void ProcesserAction (object state)
		{
			var task = state as Task;
			if (task != null)
				TryExecuteTask (task);

			while (taskQueue.TryDequeue (out task))
				TryExecuteTask (task);

			Interlocked.Exchange (ref running, 0);
		}

		protected override IEnumerable<Task> GetScheduledTasks ()
		{
			throw new System.NotImplementedException ();
		}

		protected override bool TryExecuteTaskInline (Task task, bool taskWasPreviouslyQueued)
		{
			return TryExecuteTask (task);
		}

		protected override void QueueTask (Task task)
		{
			if (Interlocked.Exchange (ref running, 1) == 0)
				ThreadPool.QueueUserWorkItem (callback, task);
			else {
				taskQueue.Enqueue (task);
				if (Interlocked.Exchange (ref running, 1) == 0)
					ThreadPool.QueueUserWorkItem (callback, task);
			}
		}
	}
}

