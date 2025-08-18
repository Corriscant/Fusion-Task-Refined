using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using FusionTask.Gameplay;
using FusionTask.Infrastructure;

namespace FusionTask.Tests.Editor
{
    /// <summary>
    /// Tests for ListExtensions.GetCenter.
    /// </summary>
    public class ListExtensionsTests
    {
        private class MockPositionable : IPositionable
        {
            public Vector3 Position { get; set; }
        }

        [Test]
        public void GetCenter_ReturnsZero_ForNullList()
        {
            List<IPositionable> items = null;
            var center = items.GetCenter();
            Assert.AreEqual(0f, center.x, 0.001f);
            Assert.AreEqual(0f, center.y, 0.001f);
            Assert.AreEqual(0f, center.z, 0.001f);
        }

        [Test]
        public void GetCenter_ReturnsZero_ForEmptyList()
        {
            var items = new List<IPositionable>();
            var center = items.GetCenter();
            Assert.AreEqual(0f, center.x, 0.001f);
            Assert.AreEqual(0f, center.y, 0.001f);
            Assert.AreEqual(0f, center.z, 0.001f);
        }

        [Test]
        public void GetCenter_ComputesCenter_ForMultipleItems()
        {
            var items = new List<IPositionable>
            {
                new MockPositionable { Position = Vector3.zero },
                new MockPositionable { Position = new Vector3(2f, 0f, 0f) }
            };

            var center = items.GetCenter();

            Assert.AreEqual(1f, center.x, 0.001f);
            Assert.AreEqual(0f, center.y, 0.001f);
            Assert.AreEqual(0f, center.z, 0.001f);
        }

        [Test]
        public void GetCenter_IgnoresNullEntries()
        {
            var items = new List<IPositionable>
            {
                new MockPositionable { Position = Vector3.zero },
                null,
                new MockPositionable { Position = new Vector3(2f, 0f, 0f) }
            };

            var center = items.GetCenter();

            Assert.AreEqual(1f, center.x, 0.001f);
            Assert.AreEqual(0f, center.y, 0.001f);
            Assert.AreEqual(0f, center.z, 0.001f);
        }
    }
}
