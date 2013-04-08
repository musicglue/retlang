using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using Retlang.Core;
using Retlang.Fibers;
using Rhino.Mocks;
using Timer = System.Timers.Timer;

namespace RetlangTests
{
	[TestFixture]
	public class ParallelPoolFiberTests
	{
		public class AsyncBlock
		{
			
		}

		[Test]
		public void EnqueueSingleActionOnParallelPoolFiber()
		{
			var fiber = new ParallelPoolFiber();
			fiber.Start();

			var executed = false;
			var timeout = false;
			
			var timer = new Timer {AutoReset = false, Enabled = false, Interval = 100};
			timer.Elapsed += (sender, args) => timeout = true;
			timer.Start();
			
			var action = new Action(() => executed = true);
			fiber.Enqueue(action);

			while(!timeout) {}
			
			Assert.IsTrue(executed);
		}

		//[Test]
		//public void ActionsAreOnlyEnqueuedWhileTheFiberIsNotStopped()
		//{
		//    var repo = new MockRepository();
		//    var pool = repo.CreateMock<IThreadPool>();
		//    var timeout = new ManualResetEventSlim();

		//    var fiber = new ParallelPoolFiber(pool, new RetlangTests.StubExecutor());
			
		//    //var action1 = new Action(() => {});
		//    //var action2 = new Action(() => {});

		//    var action1Executed = false;
		//    var action2Executed = false;

		//    fiber.Enqueue(() => action1Executed = true);

		//    timeout.Wait(TimeSpan.FromMilliseconds(100));
		//}

		//public class StubExecutor : IExecutor
		//{
		//    public void Execute(Queue<Action> toExecute)
		//    {
		//    }

		//    public void Execute(Action toExecute)
		//    {
		//    }
		//}
	}
}
