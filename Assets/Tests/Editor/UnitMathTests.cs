using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using FusionTask.Gameplay;

namespace FusionTask.Tests.Editor
{
    public class UnitMathTests
    {
        private GameObject _gameObject;
        private Unit _unit;
        private MethodInfo _hasReachedTargetMethod;

        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject();
            _gameObject.AddComponent<Selectable>();
            _unit = _gameObject.AddComponent<Unit>();
            _hasReachedTargetMethod = typeof(Unit).GetMethod("HasReachedTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(_hasReachedTargetMethod);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void HasReachedTarget_ReturnsTrueWithinOneUnitRadiusXZ()
        {
            Vector3 target = new Vector3(5f, 0f, 5f);
            _gameObject.transform.position = new Vector3(5.5f, 10f, 4.7f);

            bool reached = (bool)_hasReachedTargetMethod.Invoke(_unit, new object[] { target });

            Assert.IsTrue(reached);
        }

        [Test]
        public void HasReachedTarget_ReturnsFalseBeyondOneUnitRadiusXZ()
        {
            Vector3 target = new Vector3(5f, 0f, 5f);
            _gameObject.transform.position = new Vector3(7f, 0f, 5f); // Distance on X axis is 2 units

            bool reached = (bool)_hasReachedTargetMethod.Invoke(_unit, new object[] { target });

            Assert.IsFalse(reached);
        }

        [Test]
        public void HasReachedTarget_ReturnsTrueWhenFarAwayOnYAxis()
        {
            Vector3 target = new Vector3(5f, 0f, 5f);
            _gameObject.transform.position = new Vector3(5.8f, 100f, 5.6f); // Far on Y but within 1 unit on XZ

            bool reached = (bool)_hasReachedTargetMethod.Invoke(_unit, new object[] { target });

            Assert.IsTrue(reached);
        }
    }
}
