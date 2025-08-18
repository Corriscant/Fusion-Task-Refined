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
    }
}
