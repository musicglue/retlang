using System.Threading;
using NUnit.Framework;
using Retlang;

namespace RetlangTests
{
    public abstract class SubstitutabilityBaseTest
    {
        private IProcessContextFactory _contextFactory;
        public abstract IProcessBus CreateBus(IProcessContextFactory factory);


        private IProcessBus _bus;

        [SetUp]
        public void Setup()
        {
            _contextFactory = new ProcessContextFactory();
            _contextFactory.Start();
            _bus = CreateBus(_contextFactory);
        }

        [TearDown]
        public void TearDown()
        {
            if (_bus != null)
            {
                _bus.Stop();
            }
            _contextFactory.Stop();
        }


        [Test]
        public void ScheduleBeforeStart()
        {
            ManualResetEvent reset = new ManualResetEvent(false);

            Command onReset = delegate { reset.Set(); };
            _bus.Schedule(onReset, 1);
            _bus.Start();

            Assert.IsTrue(reset.WaitOne(5000, false));
        }

        [Test]
        public void DoubleStartResultsInException()
        {
            _bus.Start();
            try
            {
                _bus.Start();
                Assert.Fail("Should not Start");
            }catch(ThreadStateException failed)
            {
                
            }
        }

        [Test]
        public void AsyncRequestTimeout()
        {
            ManualResetEvent reset = new ManualResetEvent(false);
            Command onTimeout = delegate
                                    {
                                        reset.Set();
                                    };
            _bus.Start();
            OnMessage<string> reply = delegate { Assert.Fail("Should not be called"); };
            _bus.SendAsyncRequest(new object(), "msg", reply, onTimeout, 1);
            Assert.IsTrue(reset.WaitOne(5000, false));
        }

        [Test]
        public void AsyncRequestWithReply()
        {
            IProcessBus replyBus = CreateBus(_contextFactory);
            replyBus.Start();
            string requestTopic = "request";
            OnMessage<string> onMsg = delegate(IMessageHeader header, string msg)
                                          {
                                              replyBus.Publish(header.ReplyTo, msg);
                                          };
            replyBus.Subscribe(new TopicEquals(requestTopic), onMsg);
            Command onTimeout = delegate
                                    {
                                        Assert.Fail("Should not timeout");
                                    };
            _bus.Start();
            ManualResetEvent reset = new ManualResetEvent(false);
            OnMessage<string> reply = delegate { reset.Set(); };
            _bus.SendAsyncRequest("request", "msg", reply, onTimeout, 100);
            Assert.IsTrue(reset.WaitOne(5000, false));
            replyBus.Stop();
        }
    }


    [TestFixture]
    public class ThreadedContextTests : SubstitutabilityBaseTest
    {
        public override IProcessBus CreateBus(IProcessContextFactory factory)
        {
            return factory.Create();
        }
    }

    [TestFixture]
    public class ThreadPoolContextTests : SubstitutabilityBaseTest
    {
        public override IProcessBus CreateBus(IProcessContextFactory factory)
        {
            return factory.CreatePooled();
        }
    }
}