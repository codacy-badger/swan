﻿namespace Unosquare.Swan.Test
{
    using NUnit.Framework;
    using Unosquare.Swan.Components;

    [TestFixture]
    public class DelayProviderTest
    {
        [Test]
        [TestCase(DelayProvider.DelayStrategy.ThreadSleep)]
        [TestCase(DelayProvider.DelayStrategy.TaskDelay)]
        [TestCase(DelayProvider.DelayStrategy.ThreadPool)]
        public void WaitOne_TakesCertainTime(DelayProvider.DelayStrategy strategy)
        {
            using (var delay = new DelayProvider(strategy))
            {
                var time = delay.WaitOne();
                var mil = time.Milliseconds;
                Assert.GreaterOrEqual(mil, 1);
            }               
        }
    }
}
