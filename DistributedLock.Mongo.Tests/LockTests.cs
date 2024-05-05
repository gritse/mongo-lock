using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedLock.Mongo.Tests
{
    [TestClass]
    public class LockTests
    {
        private const string ConnectionString = "mongodb://localhost:27017/";
        private const string TestDb = "sample-db";

        private IMongoCollection<LockAcquire<string>> _locks;
        private IMongoCollection<ReleaseSignal> _signals;

        [TestInitialize]
        public async Task Initialize()
        {
            var client = new MongoClient(ConnectionString);
            var database = client.GetDatabase(TestDb);

            _locks = database.GetCollection<LockAcquire<string>>("locks");
            _signals = database.GetCollection<ReleaseSignal>("signals");
        }

        [TestMethod]
        public async Task AcquireLock()
        {
            MongoLock<string> mongoLock = new MongoLock<string>(_locks, _signals, Guid.NewGuid().ToString());

            IAcquire acq = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0));
            Assert.IsTrue(acq.Acquired);
        }

        [TestMethod]
        public async Task Acquire_And_Block()
        {
            MongoLock<string> mongoLock = new MongoLock<string>(_locks, _signals, Guid.NewGuid().ToString());

            IAcquire acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
            Assert.IsTrue(acq1.Acquired);

            IAcquire acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
            Assert.IsFalse(acq2.Acquired);
        }

        [TestMethod]
        public async Task Acquire_Block_Release_And_Acquire()
        {
            MongoLock<string> mongoLock = new MongoLock<string>(_locks, _signals, Guid.NewGuid().ToString());

            await using (IAcquire acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0)))
            {
                Assert.IsTrue(acq1.Acquired);

                IAcquire acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
                Assert.IsFalse(acq2.Acquired);
            }

            // await mongoLock.ReleaseAsync(acq1);

            IAcquire acq3 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
            Assert.IsTrue(acq3.Acquired);
        }

        [TestMethod]
        public async Task Acquire_BlockFor5Seconds_Release_Acquire()
        {
            MongoLock<string> mongoLock = new MongoLock<string>(_locks, _signals, Guid.NewGuid().ToString());

            IAcquire acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
            Assert.IsTrue(acq1.Acquired);

            IAcquire acq2 = await InTimeRange(() => mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)), 4000, 6000);
            Assert.IsFalse(acq2.Acquired);

            await mongoLock.ReleaseAsync(acq1);

            IAcquire acq3 = await InTimeRange(() => mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)), 0, 1500);
            Assert.IsTrue(acq3.Acquired);
        }

        [TestMethod]
        public async Task Acquire_WaitUntilExpire_Acquire()
        {
            MongoLock<string> mongoLock = new MongoLock<string>(_locks, _signals, Guid.NewGuid().ToString());

            IAcquire acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0));
            Assert.IsTrue(acq1.Acquired);

            IAcquire acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0));
            Assert.IsFalse(acq2.Acquired);

            await Task.Delay(TimeSpan.FromSeconds(10));

            IAcquire acq3 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0));
            Assert.IsTrue(acq3.Acquired);
        }

        [TestMethod]
        public void Synchronize_CriticalSection_For_4_Threads()
        {
            MongoLock<string> mongoLock = new MongoLock<string>(_locks, _signals, Guid.NewGuid().ToString());

            List<Task> tasks = new List<Task>();
            List<int> bucket = new List<int>() { 0 };
            Random random = new Random(DateTime.UtcNow.GetHashCode());

            for (int i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(async delegate
                {
                    for (int j = 0; j < 100; j++)
                    {
                        await using IAcquire acq = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10));

                        int count = bucket.Count;
                        Thread.Sleep(random.Next(0, 10));

                        int value = bucket[count - 1];
                        bucket.Add(value + 1);
                    }
                }));
            }


            Task.WaitAll(tasks.ToArray());
            Assert.IsTrue(bucket.SequenceEqual(Enumerable.Range(0, 401)));
        }

        private static async Task<T> InTimeRange<T>(Func<Task<T>> action, double from, double to)
        {
            DateTime started = DateTime.UtcNow;

            T result = await action.Invoke();

            double elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
            Assert.IsTrue(elapsed <= to && elapsed >= from);

            return result;
        }
    }
}
