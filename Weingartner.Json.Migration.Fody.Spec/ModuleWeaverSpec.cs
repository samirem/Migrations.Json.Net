﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using Mono.Cecil;
using Weingartner.Json.Migration.Common;
using Xunit;

namespace Weingartner.Json.Migration.Fody.Spec
{
    public class ModuleWeaverSpec
    {
        private readonly Assembly _Assembly;
        private readonly string _NewAssemblyPath;
        private readonly string _AssemblyPath;

        public ModuleWeaverSpec()
        {
            _AssemblyPath = Path.GetFullPath(@"..\..\..\Weingartner.Json.Migration.TestApplication\bin\Debug\Weingartner.Json.Migration.TestApplication.dll");

#if !DEBUG
            _AssemblyPath = _AssemblyPath.Replace(@"\Debug\", @"\Release\");
#endif

            _NewAssemblyPath = Path.ChangeExtension(_AssemblyPath, Guid.NewGuid() + ".dll");
            File.Copy(_AssemblyPath, _NewAssemblyPath, true);

            var moduleDefinition = ModuleDefinition.ReadModule(_NewAssemblyPath);
            var weavingTask = new ModuleWeaver
            {
                ModuleDefinition = moduleDefinition
            };

            weavingTask.Execute();
            moduleDefinition.Write(_NewAssemblyPath);

            _Assembly = Assembly.LoadFile(_NewAssemblyPath);
        }

        [Fact]
        public void ShouldInjectVersionProperty()
        {
            var type = _Assembly.GetType("Weingartner.Json.Migration.TestApplication.TestData");
            var instance = Activator.CreateInstance(type);

            var property = instance.GetType().GetProperty(VersionMemberName.Instance.VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            property.Should().NotBeNull();
        }

        [Fact]
        public void ShouldInjectCorrectVersionInTypeThatHasMigrationMethods()
        {
            var type = _Assembly.GetType("Weingartner.Json.Migration.TestApplication.TestData");
            var instance = Activator.CreateInstance(type);

            var property = instance.GetType().GetProperty(VersionMemberName.Instance.VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            property.GetValue(instance).Should().Be(1);
        }

        [Fact]
        public void ShouldInjectCorrectVersionInTypeThatHasNoMigrationMethods()
        {
            var type = _Assembly.GetType("Weingartner.Json.Migration.TestApplication.TestDataWithoutMigration");
            var instance = Activator.CreateInstance(type);

            var property = instance.GetType().GetProperty(VersionMemberName.Instance.VersionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            property.GetValue(instance).Should().Be(0);
        }

        [Fact]
        public void ShouldInjectDataMemberAttributeIfTypeHasDataContractAttribute()
        {
            var type = _Assembly.GetType("Weingartner.Json.Migration.TestApplication.TestDataContract");

            type.GetProperty(VersionMemberName.Instance.VersionPropertyName)
                .CustomAttributes
                .Select(attr => attr.AttributeType)
                .Should()
                .Contain(t => t == typeof(DataMemberAttribute));
        }

        [Fact]
        public void ShouldHaveVersion0WhenNoMigrationMethodExists()
        {
            var type = _Assembly.GetType("Weingartner.Json.Migration.TestApplication.TestDataWithoutMigration");

            // ReSharper disable once PossibleNullReferenceException
            ((int)type.GetField(VersionMemberName.Instance.VersionBackingFieldName, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).Should().Be(0);
        }

        [Fact]
        public void PeVerify()
        {
            Verifier.Verify(_AssemblyPath, _NewAssemblyPath);
        }
    }
}
