using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using FusionTask.Infrastructure;

namespace FusionTask.Tests.Editor
{
    /// <summary>
    /// Tests for the AsyncObjectPool functionality.
    /// </summary>
    public class AsyncObjectPoolTests
    {
        private class TestComponent : MonoBehaviour
        {
        }

        [Test]
        public async Task Get_ReturnsActiveObject_WhenPoolIsEmpty()
        {
            async Task<TestComponent> Factory()
            {
                await Task.Yield();
                var go = new GameObject();
                return go.AddComponent<TestComponent>();
            }

            var pool = new AsyncObjectPool<TestComponent>(Factory);

            var instance = await pool.Get();

            Assert.IsTrue(instance.gameObject.activeSelf);
            Assert.IsFalse(instance.IsNullOrDestroyed());

            Object.DestroyImmediate(instance.gameObject);
        }

        [Test]
        public async Task Release_DisablesObject_And_GetReturnsSameInstance()
        {
            async Task<TestComponent> Factory()
            {
                await Task.Yield();
                var go = new GameObject();
                return go.AddComponent<TestComponent>();
            }

            var pool = new AsyncObjectPool<TestComponent>(Factory);

            var first = await pool.Get();
            pool.Release(first);

            Assert.IsFalse(first.gameObject.activeSelf);

            var second = await pool.Get();

            Assert.AreSame(first, second);
            Assert.IsTrue(second.gameObject.activeSelf);

            Object.DestroyImmediate(second.gameObject);
        }

        [Test]
        public async Task Warmup_CreatesAndStoresSpecifiedNumberOfObjects()
        {
            int counter = 0;

            async Task<TestComponent> Factory()
            {
                counter++;
                await Task.Yield();
                var go = new GameObject();
                return go.AddComponent<TestComponent>();
            }

            var pool = new AsyncObjectPool<TestComponent>(Factory);

            const int count = 3;
            await pool.Warmup(count);

            Assert.AreEqual(count, counter);

            for (int i = 0; i < count; i++)
            {
                var instance = await pool.Get();
                Assert.IsTrue(instance.gameObject.activeSelf);
                Object.DestroyImmediate(instance.gameObject);
            }

            Assert.AreEqual(count, counter);
        }
    }
}
