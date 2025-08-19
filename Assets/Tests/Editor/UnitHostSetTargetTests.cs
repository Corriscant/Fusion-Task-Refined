using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Fusion;
using FusionTask.Gameplay;

// Create a specific alias for NUnit's Assert class to resolve ambiguity.
using Assert = NUnit.Framework.Assert;

namespace FusionTask.Tests.Editor
{
    /// <summary>
    /// Tests for Unit.HostSetTarget.
    /// </summary>
    public class UnitHostSetTargetTests
    {
        private static void SetAuthority(NetworkObject networkObject, bool hasAuthority)
        {
            if (networkObject == null)
            {
                throw new ArgumentNullException(nameof(networkObject));
            }

            var flagsField = typeof(NetworkObject).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(f => f.FieldType.IsEnum && f.Name.ToLower().Contains("flags"));
            var enumType = flagsField.FieldType;
            var stateAuthFlag = Enum.GetValues(enumType)
                .Cast<object>()
                .First(v => v.ToString().Contains("StateAuthority"));
            var current = Convert.ToInt32(flagsField.GetValue(networkObject));
            var flagValue = Convert.ToInt32(stateAuthFlag);
            var newValue = hasAuthority ? (current | flagValue) : (current & ~flagValue);
            flagsField.SetValue(networkObject, Enum.ToObject(enumType, newValue));
        }

        private static void SetPrivateField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(obj, value);
        }

        private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
        {
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            prop.SetValue(obj, value, null);
        }

        private static T GetPrivateProperty<T>(object obj, string propertyName)
        {
            var prop = obj.GetType().GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)prop.GetValue(obj, null);
        }

        private static T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)field.GetValue(obj);
        }

        [Test]
        public void HostSetTarget_NoAuthority_DoesNotChangeFields()
        {
            var go = new GameObject();
            var networkObject = go.AddComponent<NetworkObject>();
            var unit = go.AddComponent<Unit>();

            SetAuthority(networkObject, false);
            SetPrivateField(unit, "lastCommandServerTick", 5f);
            SetPrivateProperty(unit, "TargetPosition", Vector3.one);
            SetPrivateProperty(unit, "HasTarget", false);

            unit.HostSetTarget(new Vector3(7f, 8f, 9f), 10f);

            var target = GetPrivateProperty<Vector3>(unit, "TargetPosition");
            Assert.AreEqual(1f, target.x, 0.001f);
            Assert.AreEqual(1f, target.y, 0.001f);
            Assert.AreEqual(1f, target.z, 0.001f);
            Assert.IsFalse(GetPrivateProperty<bool>(unit, "HasTarget"));
            Assert.AreEqual(5f, GetPrivateField<float>(unit, "lastCommandServerTick"), 0.001f);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HostSetTarget_OldTick_DoesNotChangeFields()
        {
            var go = new GameObject();
            var networkObject = go.AddComponent<NetworkObject>();
            var unit = go.AddComponent<Unit>();

            SetAuthority(networkObject, true);
            SetPrivateField(unit, "lastCommandServerTick", 5f);
            SetPrivateProperty(unit, "TargetPosition", Vector3.one);
            SetPrivateProperty(unit, "HasTarget", false);

            unit.HostSetTarget(new Vector3(7f, 8f, 9f), 5f);

            var target = GetPrivateProperty<Vector3>(unit, "TargetPosition");
            Assert.AreEqual(1f, target.x, 0.001f);
            Assert.AreEqual(1f, target.y, 0.001f);
            Assert.AreEqual(1f, target.z, 0.001f);
            Assert.IsFalse(GetPrivateProperty<bool>(unit, "HasTarget"));
            Assert.AreEqual(5f, GetPrivateField<float>(unit, "lastCommandServerTick"), 0.001f);

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void HostSetTarget_NewTick_UpdatesFields()
        {
            var go = new GameObject();
            var networkObject = go.AddComponent<NetworkObject>();
            var unit = go.AddComponent<Unit>();

            SetAuthority(networkObject, true);
            SetPrivateField(unit, "lastCommandServerTick", 5f);
            SetPrivateProperty(unit, "TargetPosition", Vector3.one);
            SetPrivateProperty(unit, "HasTarget", false);

            var newTarget = new Vector3(7f, 8f, 9f);
            unit.HostSetTarget(newTarget, 6f);

            var target = GetPrivateProperty<Vector3>(unit, "TargetPosition");
            Assert.AreEqual(newTarget.x, target.x, 0.001f);
            Assert.AreEqual(newTarget.y, target.y, 0.001f);
            Assert.AreEqual(newTarget.z, target.z, 0.001f);
            Assert.IsTrue(GetPrivateProperty<bool>(unit, "HasTarget"));
            Assert.AreEqual(6f, GetPrivateField<float>(unit, "lastCommandServerTick"), 0.001f);

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
