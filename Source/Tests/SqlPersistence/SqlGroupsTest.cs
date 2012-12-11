﻿using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Terminals.Data;
using Favorite = Terminals.Data.DB.Favorite;
using Group = Terminals.Data.DB.Group;

namespace Tests
{
    /// <summary>
    ///This is a test class for database implementation of Groups
    ///</summary>
    [TestClass]
    public class SqlGroupsTest : SqlTestsLab
    {
        private int addedCount;
        private int updatedCount;
        private int deletedCount;

        [TestInitialize]
        public void TestInitialize()
        {
            this.InitializeTestLab();
            this.PrimaryPersistence.Dispatcher.GroupsChanged += new GroupsChangedEventHandler(this.DispatcherGroupsChanged);
        }

        [TestCleanup]
        public void TestClose()
        {
            this.ClearTestLab();
        }

        private void DispatcherGroupsChanged(GroupsChangedArgs args)
        {
            this.addedCount += args.Added.Count;
            this.updatedCount += args.Updated.Count;
            this.deletedCount = args.Removed.Count;
        }

        [TestMethod]
        public void AddGroupTest()
        {
            Group childGroup = this.CreateTestGroup("TestGroupA");
            Group parentGroup = this.CreateTestGroup("TestGroupB");
            childGroup.Parent = parentGroup; // don't use entities here, we are testing intern logic
            IGroup testParent = childGroup.Parent; // dummy test to resolve parent

            Assert.AreEqual(testParent, parentGroup, "Parent group wasn't set properly");
            ObjectSet<Group> checkedGroups = this.CheckDatabase.Groups;
            Group checkedChild = checkedGroups.FirstOrDefault(group => group.Id == childGroup.Id);
            Group checkedParent = checkedGroups.FirstOrDefault(group => group.Id == parentGroup.Id);
            Assert.IsNotNull(checkedChild, "Group wasn't added to the database");
            Assert.IsNotNull(checkedParent, "Group wasn't added to the database");
            Assert.AreEqual(1, checkedParent.ChildGroups.Count, "Group wasn't added as child");
            // only one, because Added event is send once for each group
            Assert.AreEqual(2, this.addedCount, "Add event wasn't received"); 
        }

        [TestMethod]
        public void LoadGroupFavoritesTest()
        {
            IGroup group = this.CreateTestGroupA();
            Favorite favorite = this.AddFavoriteToPrimaryPersistence();
            group.AddFavorite(favorite);
            List<IFavorite> favorites = group.Favorites;
            Assert.AreEqual(1, favorites.Count, "Group favorites count doesn't match.");
            Assert.AreEqual(1, this.updatedCount, "Group update event didn't reach properly.");
        }

        [TestMethod]
        public void DeleteGroupTest()
        {
            var testGroup = this.CreateTestGroupA();
            int storedBefore = this.CheckDatabase.Groups.Count();
            this.PrimaryPersistence.Groups.Delete(testGroup);
            int storedAfter = this.CheckDatabase.Groups.Count();

            this.AssertGroupDeleted(storedAfter, storedBefore);
        }

        [TestMethod]
        public void RebuildGroupsTest()
        {
            this.CreateTestGroupA();
            int storedBefore = this.CheckDatabase.Groups.Count();
            this.PrimaryPersistence.Groups.Rebuild();
            int storedAfter = this.CheckDatabase.Groups.Count();

            this.AssertGroupDeleted(storedAfter, storedBefore);
        }

        private Group CreateTestGroupA()
        {
            return this.CreateTestGroup("TestGroupA");
        }

        private Group CreateTestGroup(string newGroupName)
        {
            IFactory factory = this.PrimaryPersistence.Factory;
            Group testGroup = factory.CreateGroup(newGroupName) as Group;
            this.PrimaryPersistence.Groups.Add(testGroup);
            return testGroup;
        }

        private void AssertGroupDeleted(int storedAfter, int storedBefore)
        {
            Assert.AreEqual(1, storedBefore, "group wasn't created");
            Assert.AreEqual(0, storedAfter, "group wasn't deleted");
            Assert.AreEqual(1, this.deletedCount, "Deleted event wasn't received");
        }
    }
}