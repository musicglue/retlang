using System;
using System.Collections.Concurrent;
using System.Threading;
using Retlang.Core;

namespace Retlang.Fibers
{
	/// <summary>
	/// Similar to PoolFiber, but doesn't allocate batches of messages to ThreadPool workers, 
	/// instead it tries to allocate a single message to a single worker, potentially increasing 
	/// the rate at which messages are processed.
	/// </summary>
	public class ParallelPoolFiber : IFiber
	{
		readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
		readonly Subscriptions _subscriptions = new Subscriptions();

		readonly IExecutor _executor;
		readonly IThreadPool _pool;
		readonly Scheduler _timer;

		ExecutionState _started = ExecutionState.Created;

		/// <summary>
		/// Construct new instance.
		/// </summary>
		public ParallelPoolFiber() : this(new DefaultThreadPool(), new DefaultExecutor())
		{
		}

		/// <summary>
		/// Construct new instance.
		/// </summary>
		public ParallelPoolFiber(IThreadPool pool, IExecutor executor)
		{
			_pool = pool;
			_executor = executor;
			_timer = new Scheduler(this);
		}

		/// <summary>
		/// Stops the fiber.
		/// </summary>
		public void Dispose()
		{
			Stop();
		}

		/// <summary>
		/// Enqueue a single action.
		/// </summary>
		/// <param name="action"></param>
		public void Enqueue(Action action)
		{
			if (_started == ExecutionState.Stopped) return;

			for(var i = 0; i < _queue.Count; i++)
				_pool.Queue(ExecuteNextAction);

			_queue.Enqueue(action);

			if (_started == ExecutionState.Created) return;

			_pool.Queue(ExecuteNextAction);
		}

		void ExecuteNextAction(object state)
		{
			Action action;

			if (!_queue.TryDequeue(out action)) return;

			_executor.Execute(action);
		}
		
		/// <summary>
		/// Gets the queue size
		/// </summary>
		public int QueueSize
		{
			get { return _queue.Count; }
		}

		/// <summary>
		/// Start consuming actions.
		/// </summary>
		public void Start()
		{
			if (_started == ExecutionState.Running)
			{
				throw new ThreadStateException("Already Started");
			}

			_started = ExecutionState.Running;
		}

		/// <summary>
		/// Stop consuming actions.
		/// </summary>
		public void Stop()
		{
			_timer.Dispose();
			_started = ExecutionState.Stopped;
			_subscriptions.Dispose();
		}

		///<summary>
		/// Register subscription to be unsubcribed from when the fiber is disposed.
		///</summary>
		///<param name="toAdd"></param>
		public void RegisterSubscription(IDisposable toAdd)
		{
			_subscriptions.Add(toAdd);
		}

		///<summary>
		/// Deregister a subscription.
		///</summary>
		///<param name="toRemove"></param>
		///<returns></returns>
		public bool DeregisterSubscription(IDisposable toRemove)
		{
			return _subscriptions.Remove(toRemove);
		}

		/// <summary>
		/// <see cref="IScheduler.Schedule(Action,long)"/>
		/// </summary>
		/// <param name="action"></param>
		/// <param name="firstInMs"></param>
		/// <returns></returns>
		public IDisposable Schedule(Action action, long firstInMs)
		{
			return _timer.Schedule(action, firstInMs);
		}

		/// <summary>
		/// <see cref="IScheduler.ScheduleOnInterval(Action,long,long)"/>
		/// </summary>
		/// <param name="action"></param>
		/// <param name="firstInMs"></param>
		/// <param name="regularInMs"></param>
		/// <returns></returns>
		public IDisposable ScheduleOnInterval(Action action, long firstInMs, long regularInMs)
		{
			return _timer.ScheduleOnInterval(action, firstInMs, regularInMs);
		}
	}
}
