﻿using System;
using System.Linq;
using System.Collections.Generic;
using Lynicon.Collation;
using Lynicon.Repositories;
using Lynicon.Test.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lynicon.Extensibility;
using Lynicon.Base.Modules;
using Lynicon.Base.Models;
using Lynicon.Relations;

// Initialise database with test data
//  use ef directly, use appropriate schema for modules in use
// Attach event handlers to run at end of others, handlers store data for checking in class local vars
// 

namespace Lynicon.AutoTests
{
    [TestClass]
    public class ReferencesTest
    {
        [ClassInitialize]
        public static void Init(TestContext ctx)
        {

        }

        [TestMethod]
        public void FollowRefs()
        {
            var rt1 = Collator.Instance.GetNew<RefTargetContent>(new Address(typeof(RefTargetContent), "1"));
            var rt2 = Collator.Instance.GetNew<RefTargetContent>(new Address(typeof(RefTargetContent), "2"));
            rt1.Title = "RT1";
            rt2.Title = "RT2";
            rt1.RTString = "xyz";
            Collator.Instance.Set(rt1);
            Collator.Instance.Set(rt2);

            var rc1 = Collator.Instance.GetNew<RefContent>(new Address(typeof(RefContent), "1"));
            var rc2 = Collator.Instance.GetNew<RefContent>(new Address(typeof(RefContent), "2"));
            rc1.Title = "RC1";
            rc2.Title = "RC2";
            rc1.RefTarget = new Reference<RefTargetContent>(rt1.ItemId);
            rc1.RefTargetOther = new Reference<RefTargetContent>(rt2.ItemId);
            rc2.RefTarget = new Reference<RefTargetContent>(rt1.ItemId);
            Collator.Instance.Set(rc1);
            Collator.Instance.Set(rc2);

            var backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt1.OriginalRecord), "RefTarget").ToList();
            Assert.AreEqual(2, backRefs.Count, "Get references to item with 2 refs");
            backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt2.OriginalRecord), "RefTarget").ToList();
            Assert.AreEqual(0, backRefs.Count, "Get references to item with 0 refs");
            backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt2.OriginalRecord), "RefTargetOther").ToList();
            Assert.AreEqual(1, backRefs.Count, "Get references to item with 1 refs");

            rc2.RefTarget = new Reference<RefTargetContent>(rt2.ItemId);
            Collator.Instance.Set(rc2);
            backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt1.OriginalRecord), "RefTarget").ToList();
            Assert.AreEqual(1, backRefs.Count, "Get references after update");

            Collator.Instance.Delete(rc1);
            backRefs = Reference.GetReferencesFrom<RefContent>(new ItemVersionedId(rt1.OriginalRecord), "RefTarget").ToList();
            Assert.AreEqual(0, backRefs.Count, "Get references after delete");
        }
    }
}
