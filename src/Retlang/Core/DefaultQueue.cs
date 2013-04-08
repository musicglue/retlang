using System;
using System.Collections.Generic;
using System.Threading;

namespace Retlang.Core
{
    /// <summary>
    /// Default implementation.
    /// </summary>
    public class DefaultQueue : IQueue
    {
        private readonly object _lock = new object();
        private readonly IExecutor _executor;

        private bool _running = true;

        private Queue<Action> _actions = new Queue<Action>();
        private Queue<Action> _toPass = new Queue<Action>();

        ///<summary>
        /// Default queue with custom executor
        ///</summary>
        ///<param name="executor"></param>
        public DefaultQueue(IExecutor executor)
        {
            _executor = executor;
        }

        ///<summary>
        /// Default queue with default executor
        ///</summary>
        public DefaultQueue() 
            : this(new DefaultExecutor())
        {
        }

        /// <summary>
        /// Enqueue action.
        /// </summary>
        /// <param name="action"></param>
        public void Enqueue(Action action)
        {
            lock (_lock)
            {
                _actions.Enqueue(action);
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// Execute actions until stopped.
        /// </summary>
        public void Run()
        {
            while (ExecuteNextBatch()) { }
        }

        /// <summary>
        /// Stop consuming actions.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _running = false;
                Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        /// Number of actions in the queue. 
        /// </summary>
        public int Size
        {
            get
            {
                lock (_lock)
                {
                    return _actions.Count + _toPass.Count;
                }
            }
        }

        private Queue<Action> DequeueAll()
        {
            lock (_lock)
            {
                if (ReadyToDequeue())
                {
                    Queues.Swap(ref _actions, ref _toPass);
                    _actions.Clear();
                    return _toPass;
                }
                return null;
            }
        }

        private bool ReadyToDequeue()
        {
            while (_actions.Count == 0 && _running)
            {
                Monitor.Wait(_lock);
            }
            return _running;
        }

        /// <summary>
        /// Remove all actions and execute.
        /// </summary>
        /// <returns></returns>
        public bool ExecuteNextBatch()
        {
            var toExecute = DequeueAll();
            if (toExecute == null)
            {
                return false;
            }
            _executor.Execute(toExecute);
            return true;
        }
    }
}