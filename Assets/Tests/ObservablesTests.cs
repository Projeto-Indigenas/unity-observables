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

            Observable<float> observable = new Observable<float>();

            void TestAction(float value)
            {
                callCount += 1;
                Assert.AreEqual(1F, value);
            }

            observable.Observe(TestAction);

            Observable<float>.InvokeMessage(observable, 1F);

            Assert.AreEqual(1, callCount);

            observable.RemoveObserver(TestAction);

            Observable<float>.InvokeMessage(observable, 2F);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TestObservablesOperatorOverloading()
        {
            int callCount = 0;

            Observable<float> observable = new Observable<float>();

            void TestAction(float value)
            {
                callCount += 1;
                Assert.AreEqual(1F, value);
            }

            observable += TestAction;

            Observable<float>.InvokeMessage(observable, 1F);

            Assert.AreEqual(1, callCount);

            observable -= TestAction;

            Observable<float>.InvokeMessage(observable, 2F);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TestDestructorObservable() 
        {
            Observable<int> observable = new Observable<int>();

            new Action(() =>
            {
                DestructorMock observer = new DestructorMock();
                observable.Observe(observer.ObserverAction);
                observer = null;

            }).Invoke();

            Observable<int>.InvokeMessage(observable, 1);

            Assert.AreEqual(1, DestructorMock.actionCallCount);

            Observable<int>.InvokeMessage(observable, 10);

            Assert.AreEqual(11, DestructorMock.actionCallCount);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            Observable<int>.InvokeMessage(observable, 1);

            Assert.AreEqual(11, DestructorMock.actionCallCount);
        }

        [Test]
        public void TestDestructorObservableOperatorOverloading()
        {
            Observable<int> observable = new Observable<int>();

            new Action(() =>
            {
                DestructorMock observer = new DestructorMock();
                observable += observer.ObserverAction;
                observer = null;

            }).Invoke();

            Observable<int>.InvokeMessage(observable, 1);

            Assert.AreEqual(1, DestructorMock.actionCallCount);

            Observable<int>.InvokeMessage(observable, 10);

            Assert.AreEqual(11, DestructorMock.actionCallCount);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            Observable<int>.InvokeMessage(observable, 1);

            Assert.AreEqual(11, DestructorMock.actionCallCount);
        }

        [Test]
        public void TestDestructorObservableWithPayload()
        {
            Observable<int> observable = new Observable<int>();

            new Action(() =>
            {
                DestructorMock observer = new DestructorMock();
                observable.With<float>().Observe(observer.ObserverWithPayloadAction);
                observer = null;

            }).Invoke();

            Observable<int, float>.InvokeMessage(observable.With<float>(), 1, 1F);

            Assert.AreEqual(1, DestructorMock.actionWithPayloadCallCount);

            Observable<int, float>.InvokeMessage(observable.With<float>(), 2, 2F);
            Observable<int, float>.InvokeMessage(observable.With<float>(), 2, 2F);

            Assert.AreEqual(5, DestructorMock.actionWithPayloadCallCount);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            Observable<int, float>.InvokeMessage(observable.With<float>(), 10, 1F);

            Assert.AreEqual(5, DestructorMock.actionWithPayloadCallCount);
        }

#if UNITY_2019_1_OR_NEWER
        [UnityTest]
        public IEnumerator TestDestroyingObservableObjects()
        {
            Observable<int> observable = new Observable<int>();

            UnityDestroyableMock mock = new GameObject().AddComponent<UnityDestroyableMock>();
            observable.Observe(mock.ObserverAction);
            observable.With<float>().Observe(mock.ObservverWithPayloadAction);

            Observable<int>.InvokeMessage(observable, 2);
            Observable<int, float>.InvokeMessage(observable.With<float>(), 2, 0F);

            Assert.AreEqual(2, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(2, UnityDestroyableMock.actionWithPayloadCallCount);

            Observable<int>.InvokeMessage(observable, 20);
            Observable<int, float>.InvokeMessage(observable.With<float>(), 10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);

            Object.DestroyImmediate(mock.gameObject);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            yield return null;

            Observable<int>.InvokeMessage(observable, 20);
            Observable<int, float>.InvokeMessage(observable.With<float>(), 10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);
        }

        [UnityTest]
        public IEnumerator TestDestroyingObservableObjectsOperatorOverloading()
        {
            Observable<int> observable = new Observable<int>();

            UnityDestroyableMock mock = new GameObject().AddComponent<UnityDestroyableMock>();
            observable += mock.ObserverAction;
            observable.With<float>().observable += mock.ObservverWithPayloadAction;

            Observable<int>.InvokeMessage(observable, 2);
            Observable<int, float>.InvokeMessage(observable.With<float>(), 2, 0F);

            Assert.AreEqual(2, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(2, UnityDestroyableMock.actionWithPayloadCallCount);

            Observable<int>.InvokeMessage(observable, 20);
            Observable<int, float>.InvokeMessage(observable.With<float>(), 10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);

            Object.DestroyImmediate(mock.gameObject);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            yield return null;

            Observable<int>.InvokeMessage(observable, 20);
            Observable<int, float>.InvokeMessage(observable.With<float>(), 10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);
        }
#endif

        [Test]
        public void TestObservableOperatorOverloading()
        {
            Observable<int> observable = new Observable<int>();

            DestructorMock mock = new DestructorMock();
            observable += mock.ObserverAction;

            Observable<int>.InvokeMessage(observable, 2);

            Assert.AreEqual(2, DestructorMock.actionCallCount);

            Observable<int>.InvokeMessage(observable, 20);

            Assert.AreEqual(22, DestructorMock.actionCallCount);

            mock = null;

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            Observable<int>.InvokeMessage(observable, 20);

            Assert.AreEqual(22, DestructorMock.actionCallCount);
        }

        [Test]
        public void MeasureInvokeTimes()
        {
            Stopwatch stopwatch = new Stopwatch();

            Observable<int> observable = new Observable<int>();

            UnityDestroyableMock goMock = new GameObject().AddComponent<UnityDestroyableMock>();
            DestructorMock mock = new DestructorMock();
            observable.Observe(goMock.ObserverAction);
            observable.With<float>().Observe(goMock.ObservverWithPayloadAction);

            observable.Observe(mock.ObserverAction);
            observable.With<float>().Observe(mock.ObserverWithPayloadAction);

            stopwatch.Start();
            Observable<int>.InvokeMessage(observable, 0);
            stopwatch.Stop();

            TestContext.Out.WriteLine($"Invoke message with 2 observers took {stopwatch.Elapsed.TotalMilliseconds} ms");

            stopwatch.Restart();
            Observable<int, float>.InvokeMessage(observable.With<float>(), 0, 0F);
            stopwatch.Stop();

            TestContext.Out.WriteLine($"Invoke message with payload with 2 observers took {stopwatch.Elapsed.TotalMilliseconds} ms");
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