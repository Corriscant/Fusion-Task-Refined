using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using FusionTask.Infrastructure;

namespace FusionTask.Tests.Editor
{
    /// <summary>
    /// Tests for MaterialApplier.
    /// </summary>
    public class MaterialApplierTests
    {
        private class FakePlayerMaterialProvider : IPlayerMaterialProvider
        {
            public Material Material;

            public Task<Material> GetMaterialAsync(int index)
            {
                return Task.FromResult(Material);
            }

            public Material GetMaterial(int index)
            {
                return Material;
            }

            public void Release()
            {
            }
        }

        private FakePlayerMaterialProvider _provider;
        private MaterialApplier _materialApplier;
        private GameObject _gameObject;
        private MeshRenderer _renderer;

        [SetUp]
        public void SetUp()
        {
            _provider = new FakePlayerMaterialProvider();
            _materialApplier = new MaterialApplier(_provider);
            _gameObject = new GameObject();
            _renderer = _gameObject.AddComponent<MeshRenderer>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [Test]
        public void ApplyMaterial_ReturnsFalse_WhenRendererIsNull()
        {
            var result = _materialApplier.ApplyMaterial(null, 0, "Entity");
            Assert.IsFalse(result);
        }

        [Test]
        public async Task ApplyMaterialAsync_ReturnsFalse_WhenRendererIsNull()
        {
            var result = await _materialApplier.ApplyMaterialAsync(null, 0, "Entity");
            Assert.IsFalse(result);
        }

        [Test]
        public void ApplyMaterial_ReturnsFalse_WhenProviderReturnsNull()
        {
            _provider.Material = null;
            var result = _materialApplier.ApplyMaterial(_renderer, 0, "Entity");
            Assert.IsFalse(result);
        }

        [Test]
        public async Task ApplyMaterialAsync_ReturnsFalse_WhenProviderReturnsNull()
        {
            _provider.Material = null;
            var result = await _materialApplier.ApplyMaterialAsync(_renderer, 0, "Entity");
            Assert.IsFalse(result);
        }

        [Test]
        public void ApplyMaterial_SetsSharedMaterial_AndPropertyBlock()
        {
            var material = new Material(Shader.Find("Standard"));
            _provider.Material = material;
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetFloat("_Glossiness", 0.25f);

            var result = _materialApplier.ApplyMaterial(_renderer, 0, "Entity", propertyBlock);

            Assert.IsTrue(result);
            Assert.AreEqual(material, _renderer.sharedMaterial);
            var retrievedBlock = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(retrievedBlock);
            Assert.AreEqual(0.25f, retrievedBlock.GetFloat("_Glossiness"), 0.001f);
        }

        [Test]
        public async Task ApplyMaterialAsync_SetsSharedMaterial_AndPropertyBlock()
        {
            var material = new Material(Shader.Find("Standard"));
            _provider.Material = material;
            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetFloat("_Glossiness", 0.25f);

            var result = await _materialApplier.ApplyMaterialAsync(_renderer, 0, "Entity", propertyBlock);

            Assert.IsTrue(result);
            Assert.AreEqual(material, _renderer.sharedMaterial);
            var retrievedBlock = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(retrievedBlock);
            Assert.AreEqual(0.25f, retrievedBlock.GetFloat("_Glossiness"), 0.001f);
        }
    }
}
