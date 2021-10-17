using NUnit.Framework;
using System;

#if UNITY_INCLUDE_TESTS
using UnityEngine;
using UnityEngine.TestTools;
#endif

namespace Observables.Tests 
{
    public class ObservablesTests 
    {
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
}