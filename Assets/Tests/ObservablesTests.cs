using NUnit.Framework;
using Observables;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Collections;

#if UNITY_INCLUDE_TESTS
using UnityEngine;
using UnityEngine.TestTools;
#endif

namespace Observables.Tests 
{
    public class ObservablesTests 
    {
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

            observable.StopObserving(this, action);

            Observable<float>.InvokeMessage(observable, 2F);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TestDestructorObservable() 
        {
            Observable<float> observable = new Observable<float>();
            List<WeakReference<object>> keys = GetValue<List<WeakReference<object>>>("_keys", observable);

            new Action(() =>
            {
                DestructorMock observer = new DestructorMock();
                observable.Observe(observer, value => { });
                observer = null;
            }).Invoke();

            Assert.AreEqual(1, keys.Count);

            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.WaitForPendingFinalizers();

            Assert.AreEqual(0, keys.Count);
        }

        [Test]
        public void TestWeakReferenceGC()
        {
            DestructorMock instance = new DestructorMock();
            WeakReference<DestructorMock> weakReference = new WeakReference<DestructorMock>(instance);
            instance = null;

            for (int index = 0; index < 5; index++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
            }
            
            Assert.IsFalse(weakReference.TryGetTarget(out _));
        }

        private TResult GetValue<TResult>(string name, object target)
            where TResult : class
        {
            const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type type = target.GetType();
            FieldInfo fieldInfo = type.GetField(name, bindingFlags);
            return (TResult)fieldInfo.GetValue(target);
        }
    }

    class DestructorMock : ADestructorObserver { }
}