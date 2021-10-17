using NUnit.Framework;
using System;
using System.Collections;

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

            Action<float> action = value =>
            {
                callCount += 1;
                Assert.AreEqual(1F, value);
            };

            observable.Observe(this, action);

            Observable<float>.InvokeMessage(observable, 1F);

            Assert.AreEqual(1, callCount);

            observable.RemoveObserver(this, action);

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
                observable.Observe(observer, observer.ObserverAction);
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
                observable.Observe<float>(observer, observer.ObserverWithPayloadAction);
                observer = null;

            }).Invoke();

            Observable<int>.InvokeMessage(observable, 1, 1F);

            Assert.AreEqual(1, DestructorMock.actionWithPayloadCallCount);

            Observable<int>.InvokeMessage(observable, 2, 2F);
            Observable<int>.InvokeMessage(observable, 2, 2F);

            Assert.AreEqual(5, DestructorMock.actionWithPayloadCallCount);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            Observable<int>.InvokeMessage(observable, 10, 1F);

            Assert.AreEqual(5, DestructorMock.actionWithPayloadCallCount);
        }

#if UNITY_2019_1_OR_NEWER
        [UnityTest]
        public IEnumerator TestDestroyingObservableObjects()
        {
            Observable<int> observable = new Observable<int>();

            UnityDestroyableMock mock = new GameObject().AddComponent<UnityDestroyableMock>();
            observable.Observe(mock, mock.ObserverAction);
            observable.Observe<float>(mock, mock.ObservverWithPayloadAction);

            Observable<int>.InvokeMessage(observable, 2);
            Observable<int>.InvokeMessage(observable, 2, 0F);

            Assert.AreEqual(2, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(2, UnityDestroyableMock.actionWithPayloadCallCount);

            Observable<int>.InvokeMessage(observable, 20);
            Observable<int>.InvokeMessage(observable, 10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);

            Object.DestroyImmediate(mock.gameObject);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, false);
            GC.WaitForPendingFinalizers();

            yield return null;

            Observable<int>.InvokeMessage(observable, 20);
            Observable<int>.InvokeMessage(observable, 10, 0F);

            Assert.AreEqual(22, UnityDestroyableMock.actionCallCount);
            Assert.AreEqual(12, UnityDestroyableMock.actionWithPayloadCallCount);
        }
#endif
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