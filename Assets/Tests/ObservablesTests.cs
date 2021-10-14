using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Observables;
using System;

namespace Observables.Tests 
{
    public class ObservablesTests 
    {
        [Test]
        public void TestObservables() 
        {
            int callCount = 0;

            Observable<float> observable = new Observable<float>();

            observable.Observe(this, value => 
            {
                callCount += 1;
                Assert.AreEqual(1F, value);
            });

            Observable<float>.InvokeMessage(observable, 1F);

            Assert.AreEqual(1, callCount);

            observable.StopObserving(this);

            Observable<float>.InvokeMessage(observable, 2F);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void TestDestructorObservable() 
        {
            DestructorMock destructorMock = new DestructorMock();

            Observable<float> observable = new Observable<float>();

            observable.Observe(destructorMock, destructorMock.Observer);

            destructorMock = null;

            GC.Collect();

            observable.
        }

        private TResult GetValue<TResult>(string name, object target)
        {
            target.GetType().GetField(name, BindingFlags)
        }
    }

    private class DestructorMock : ADestructorObservable
    {
        public void Observer(float value)
        {

        }
    }
}