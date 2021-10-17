using NUnit.Framework;

namespace Observables.Tests
{
    public class ObserverPayloadInfoTests
    {
        [Test]
        public void TestInvokation()
        {
            int intValue = default;
            float floatValue = default;

            void TestAction(int a, float b)
            {
                intValue = a;
                floatValue = b;
            }

            ObserverPayloadInfo<int, float> payload = new ObserverPayloadInfo<int, float>(TestAction);

            payload.Invoke(10, 20F);

            Assert.AreEqual(10, intValue);
            Assert.AreEqual(20F, floatValue);
        }

        [Test]
        public void TestRegisterUnregister()
        {
            int callCount = 0;

            void TestAction(int a, float b) => callCount++;

            ObserverPayloadInfo<int, float> payload = new ObserverPayloadInfo<int, float>(TestAction);

            payload.Invoke(1, 1F);
            payload.Invoke(1, 1F);

            Assert.AreEqual(2, callCount);

            callCount = 0;

            payload.Register(TestAction);

            payload.Invoke(1, 1F);

            Assert.AreEqual(1, callCount);

            callCount = 0;

            payload.Unregister(TestAction);

            payload.Invoke(0, 0.0f);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void TestUnregisterByTarget()
        {
            int callCount = 0;

            void TestAction(int a, float b) => callCount++;

            ObserverPayloadInfo<int, float> payload = new ObserverPayloadInfo<int, float>(TestAction);

            OtherClass other = new OtherClass();

            payload.Register(other.TestAction);

            payload.Invoke(0, 0F);
            payload.Invoke(0, 0F);

            Assert.AreEqual(2, callCount);
            Assert.AreEqual(2, other.callCount);

            callCount = 0;
            other.callCount = 0;

            payload.Unregister(TestAction);

            payload.Invoke(0, 0F);
            payload.Invoke(0, 0F);

            Assert.AreEqual(0, callCount);
            Assert.AreEqual(2, other.callCount);

            callCount = 0;
            other.callCount = 0;

            payload.Unregister(other.TestAction);

            payload.Invoke(0, 0F);
            payload.Invoke(0, 0F);

            Assert.AreEqual(0, callCount);
            Assert.AreEqual(0, other.callCount);
        }

        private class OtherClass
        {
            public int callCount = 0;

            public void TestAction(int a, float b) => callCount++;
        }
    }
}