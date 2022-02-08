using NUnit.Framework;
using Observables.Destructors;
using System;
using System.Collections;
using System.Diagnostics;

#if UNITY_INCLUDE_TESTS
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#endif

namespace Observables.Tests 
{
    public class ObservablesTests 
    {
        [SetUp]
        public void SetUp()
        {
            DestructorMock.actionCallCount = 0;
            DestructorMock.actionWithPayloadCallCount = 0;
            UnityDestroyableMock.actionCallCount = 0;
            UnityDestroyableMock.actionWithPayloadCallCount = 0;
        }

        [Test]
        public void TestWeakReferenceGC()
        {
            WeakReference<DestructorMock> weakReference = new WeakReference<DestructorMock>(null);

            new Action(() =>
            {
                DestructorMock instance = new DestructorMock();
                weakReference.SetTarget(instance);
                instance = null;

            }).Invoke();

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            Assert.IsFalse(weakReference.TryGetTarget(out _));
        }

        [Test]
        public void TestObservables() 
        {
            int callCount = 0;

            Observable observable = new Observable();

            void TestAction(float value)
            {
                callCount += 1;
                Assert.AreEqual(1F, value);
            }

            observable.Observe<float>(TestAction);

            observable.InvokeMessage(1F);

            Assert.AreEqual(1, callCount);

            observable.RemoveObserver<float>(TestAction);

            observable.InvokeMessage(2F);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TestObservablesOperatorOverloading()
        {
            int callCount = 0;

            Observable observable = new Observable();

            void TestAction(float value)
            {
                callCount += 1;
                Assert.AreEqual(1F, value);
            }

            observable.Observe<float>(TestAction);

            observable.InvokeMessage(1F);

            Assert.AreEqual(1, callCount);

            observable.RemoveObserver<float>(TestAction);

            observable.InvokeMessage(2F);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TestDestructorObservable() 
        {
            Observable observable = new Observable();

            new Action(() =>
            {
                DestructorMock observer = new DestructorMock();
                Action<int> action = observer.ObserverAction;
                observable.Observe(action);
                observer = null;

            }).Invoke();

            observable.InvokeMessage(1);

            Assert.AreEqual(1, DestructorMock.actionCallCount);

            observable.InvokeMessage(10);

            Assert.AreEqual(11, DestructorMock.actionCallCount);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            observable.InvokeMessage(1);

            Assert.AreEqual(11, DestructorMock.actionCallCount);
        }

        [Test]
        public void TestDestructorObservableOperatorOverloading()
        {
            Observable observable = new Observable();

            new Action(() =>
            {
                DestructorMock observer = new DestructorMock();
                observable.Observe<int>(observer.ObserverAction);
                observer = null;

            }).Invoke();

            observable.InvokeMessage(1);

            Assert.AreEqual(1, DestructorMock.actionCallCount);

            observable.InvokeMessage(10);

            Assert.AreEqual(11, DestructorMock.actionCallCount);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            observable.InvokeMessage(1);

            Assert.AreEqual(11, DestructorMock.actionCallCount);
        }

        [Test]
        public void TestDestructorObservableWithPayload()
        {
            Observable observable = new Observable();

            new Action(() =>
            {
                DestructorMock observer = new DestructorMock();
                observable.Observe<int, float>(observer.ObserverWithPayloadAction);
                observer = null;

            }).Invoke();

            observable.InvokeMessage(1, 1F);

            Assert.AreEqual(1, DestructorMock.actionWithPayloadCallCount);

            observable.InvokeMessage(2, 2F);
            observable.InvokeMessage(2, 2F);

            Assert.AreEqual(5, DestructorMock.actionWithPayloadCallCount);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            observable.InvokeMessage(10, 1F);

            Assert.AreEqual(5, DestructorMock.actionWithPayloadCallCount);
        }

#if UNITY_2019_1_OR_NEWER
        [UnityTest]
        public IEnumerator TestDestroyingObservableObjects()
        {
            Observable observable = new Observable();

            UnityDestroyableMock mock = new GameObject().AddComponent<UnityDestroyableMock>();
            observable.Observe<int>(mock.ObserverAction);
            observable.Observe<int, float>(mock.ObservverWithPayloadAction);

            observable.InvokeMessage(2);
            observable.InvokeMessage(2, 0F);

            Assert.AreEqual(2, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(2, UnityDestroyableMock.actionWithPayloadCallCount);

            observable.InvokeMessage(20);
            observable.InvokeMessage(10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);

            Object.DestroyImmediate(mock.gameObject);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            yield return null;

            observable.InvokeMessage(20);
            observable.InvokeMessage(10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);
        }

        [UnityTest]
        public IEnumerator TestDestroyingObservableObjectsOperatorOverloading()
        {
            Observable observable = new Observable();

            UnityDestroyableMock mock = new GameObject().AddComponent<UnityDestroyableMock>();
            observable.Observe<int>(mock.ObserverAction);
            observable.Observe<int, float>(mock.ObservverWithPayloadAction);

            observable.InvokeMessage(2);
            observable.InvokeMessage(2, 0F);

            Assert.AreEqual(2, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(2, UnityDestroyableMock.actionWithPayloadCallCount);

            observable.InvokeMessage(20);
            observable.InvokeMessage(10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);

            Object.DestroyImmediate(mock.gameObject);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            yield return null;

            observable.InvokeMessage(20);
            observable.InvokeMessage(10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);
        }
#endif

        [Test]
        public void TestObservableOperatorOverloading()
        {
            Observable observable = new Observable();

            DestructorMock mock = new DestructorMock();
            observable.Observe<int>(mock.ObserverAction);

            observable.InvokeMessage(2);

            Assert.AreEqual(2, DestructorMock.actionCallCount);

            observable.InvokeMessage(20);

            Assert.AreEqual(22, DestructorMock.actionCallCount);

            mock = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            observable.InvokeMessage(20);

            Assert.AreEqual(22, DestructorMock.actionCallCount);
        }

        [Test]
        public void MeasureInvokeTimes()
        {
            Stopwatch stopwatch = new Stopwatch();

            Observable observable = new Observable();

            UnityDestroyableMock goMock = new GameObject().AddComponent<UnityDestroyableMock>();
            DestructorMock mock = new DestructorMock();
            observable.Observe<int>(goMock.ObserverAction);
            observable.Observe<int, float>(goMock.ObservverWithPayloadAction);

            observable.Observe<int>(mock.ObserverAction);
            observable.Observe<int, float>(mock.ObserverWithPayloadAction);

            stopwatch.Start();
            observable.InvokeMessage(0);
            stopwatch.Stop();

            TestContext.Out.WriteLine($"Invoke message with 2 observers took {stopwatch.Elapsed.TotalMilliseconds} ms");

            stopwatch.Restart();
            observable.InvokeMessage(0, 0F);
            stopwatch.Stop();

            TestContext.Out.WriteLine($"Invoke message with payload with 2 observers took {stopwatch.Elapsed.TotalMilliseconds} ms");
        }

        [Test]
        public void TestObservableKeysFindingObjects()
        {
            Observable observable1 = new Observable();
            Observable observable2 = new Observable();

            bool action1Called = false;
            bool action2Called = false;
            Action<int> action1 = a => action1Called = true;
            Action<int> action2 = a => action2Called = true;

            observable1.Observe(action1);
            observable2.Observe(action2);

            observable1.InvokeMessage(1);
            observable2.InvokeMessage(2);

            Assert.IsTrue(action1Called);
            Assert.IsTrue(action2Called);
        }
    }

    class DestructorMock : ADestructorObserver 
    {
        public static int actionCallCount = 0;
        public static int actionWithPayloadCallCount = 0;

        public void ObserverAction(int value)
        {
            actionCallCount += value;
        }

        public void ObserverWithPayloadAction(int value, float other)
        {
            actionWithPayloadCallCount += value;
        }
    }

    class UnityDestroyableMock : ADestroyableObserver
    {
        public static int actionCallCount = 0;
        public static int actionWithPayloadCallCount = 0;

        public void ObserverAction(int value)
        {
            actionCallCount += value;
        }

        public void ObservverWithPayloadAction(int value, float other)
        {
            actionWithPayloadCallCount += value;
        }
    }
}