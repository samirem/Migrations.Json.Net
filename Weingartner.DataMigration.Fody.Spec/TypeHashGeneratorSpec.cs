﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using FluentAssertions;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Xunit;

namespace Weingartner.DataMigration.Fody.Spec
{
    public class TypeHashGeneratorSpec
    {
        [Fact]
        public void ShouldGenerateCorrectHashForSimpleType()
        {
            var sut = new TypeHashGenerator();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(Address)));

            hash.Should().Be(GetExpectedHashForAddress());
        }

        [Fact]
        public void ShouldGenerateCorrectHashForNestedTypes()
        {
            var sut = new TypeHashGenerator();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(Person)));

            hash.Should().Be(GetExpectedHashForPerson());
        }

        [Fact]
        public void ShouldGenerateCorrectHashForNestedEnumerableTypes()
        {
            var sut = new TypeHashGenerator();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(Club)));

            hash.Should().Be(GetExpectedHashForClub());
        }

        [Fact]
        public void ShouldGenerateCorrectHashForNestedTypesWithGenericArguments()
        {
            var sut = new TypeHashGenerator();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(ClubEntry)));

            hash.Should().Be(GetExpectedHashForClubEntry());
        }

        [Fact]
        public void ShouldWorkWithCircularTypeDefinitions()
        {
            var sut = new TypeHashGenerator();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(LinkedPersonEntry)));

            hash.Should().Be(GetExpectedHashForLinkedPersonEntry());
        }

        [Fact]
        public void ShouldGenerateSameHashWhenPropertyPositionsSwitched()
        {
            var sut = new TypeHashGenerator();
            var hash1 = sut.GenerateHashBase(GetTypeDefinition(typeof(Address)));
            var hash2 = sut.GenerateHashBase(GetTypeDefinition(typeof(Address2))).Replace("/Address2", "/Address");

            hash1.Should().Be(hash2);
        }

        [Fact]
        public void ShouldIgnoreVersionProperty()
        {
            var sut = new TypeHashGenerator();
            var hash = sut.GenerateHashBase(GetTypeDefinition(typeof(VersionedData)));

            hash.Should().Be(GetExpectedHashForVersionedData());
        }

        // TODO support type hierarchies

        private static TypeDefinition GetTypeDefinition(Type type)
        {
            var module = AssemblyDefinition
                .ReadAssembly(Assembly.GetExecutingAssembly().Location)
                .Modules
                .Single();
            var typeDef = module.Import(type).Resolve();
            return module
                .GetAllTypes()
                .Single(t => t.IsProbablyEqualTo(typeDef));
        }

        private static readonly string BaseName = typeof (TypeHashGeneratorSpec).FullName;

        private static string GetExpectedHashForAddress()
        {
            return "System.String-City|System.String-Street";
        }

        private static string GetExpectedHashForAddressWithType()
        {
            return string.Format("{0}/Address({1})", BaseName, GetExpectedHashForAddress());
        }

        private static string GetExpectedHashForPerson()
        {
            return "System.String-Name|" + GetExpectedHashForAddressWithType() + "-Address";
        }

        private static string GetExpectedHashForPersonWithType()
        {
            return string.Format("{0}/Person({1})", BaseName, GetExpectedHashForPerson());
        }

        private static string GetExpectedHashForClubEntry()
        {
            return "System.Tuple`2(System.Int32-Item1|" + GetExpectedHashForPersonWithType() + "-Item2)-Member";
        }

        private static string GetExpectedHashForClub()
        {
            return "System.Collections.Generic.IDictionary`2(System.Int32|" + GetExpectedHashForPersonWithType() + ")-Members";
        }

        private static string GetExpectedHashForLinkedPersonEntry()
        {
            return string.Format("{0}/LinkedPersonEntry-Next|{1}-Current", BaseName, GetExpectedHashForPersonWithType());
        }

        private static string GetExpectedHashForVersionedData()
        {
            return string.Empty;
        }

        private class LinkedPersonEntry
        {
            [DataMember]
            public Person Current { get; private set; }

            [DataMember]
            public LinkedPersonEntry Next { get; private set; }
        }

        private class Club
        {
            [DataMember]
            public IDictionary<int, Person> Members { get; private set; }
        }

        private class ClubEntry
        {
            [DataMember]
            public Tuple<int, Person> Member { get; private set; }
        }

        private class Employee : Person
        {
            [DataMember]
            public string EmployeeId { get; private set; }
        }

        private class Person
        {
            [DataMember]
            public string Name { get; private set; }

            [DataMember]
            public Address Address { get; private set; }
        }

        private class Address
        {
            [DataMember]
            public string City { get; private set; }

            [DataMember]
            public string Street { get; private set; }
        }

        private class Address2
        {
            [DataMember]
            public string Street { get; private set; }

            [DataMember]
            public string City { get; private set; }
        }

        private class VersionedData
        {
            [DataMember]
            public string Version { get; private set; }
        }
    }
}
