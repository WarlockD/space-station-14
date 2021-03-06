﻿using System.Collections.Generic;
using Content.Server.Health.BodySystem.BodyTemplate;
using Content.Shared.Health.BodySystem;
using NUnit.Framework;
using Robust.UnitTesting;

namespace Content.Tests.Shared
{
    [TestFixture, Parallelizable, TestOf(typeof(BodyTemplate))]
    public class BodyTemplateTest : RobustUnitTest
    {
        [Test]
        public void CheckBodyTemplateHashingWorks()
        {

            BodyTemplate a = new BodyTemplate();
            BodyTemplate b = new BodyTemplate();
            BodyTemplate c = new BodyTemplate();
            BodyTemplate d = new BodyTemplate();
            BodyTemplate e = new BodyTemplate();

            a.Slots.Add("torso", BodyPartType.Torso);
            a.Slots.Add("left arm", BodyPartType.Arm);
            a.Connections.Add("torso", new List<string>() { "left arm" });
            a.CenterSlot = "torso";

            b.Slots.Add("left arm", BodyPartType.Arm);
            b.Slots.Add("torso", BodyPartType.Torso);
            b.Connections.Add("left arm", new List<string>() { "torso" });
            b.CenterSlot = "torso";

            c.Slots.Add("torso", BodyPartType.Head);
            c.Slots.Add("left arm", BodyPartType.Arm);
            c.Connections.Add("torso", new List<string>() { "left arm" });
            a.CenterSlot = "torso";

            d.Slots.Add("torso", BodyPartType.Torso);
            d.Slots.Add("left arm", BodyPartType.Arm);
            d.Slots.Add("left hand", BodyPartType.Hand);
            d.Connections.Add("left arm", new List<string>() { "left hand" });
            d.CenterSlot = "torso";

            e.Slots.Add("torso", BodyPartType.Torso);
            e.Slots.Add("left arm", BodyPartType.Arm);
            e.Slots.Add("left hand", BodyPartType.Hand);
            e.Connections.Add("left arm", new List<string>() { "torso" });
            e.CenterSlot = "left hand";

            Assert.That(a.Equals(b) && a.GetHashCode() != 0 && b.GetHashCode() != 0);
            Assert.That(!a.Equals(c) && a.GetHashCode() != 0 && c.GetHashCode() != 0);
            Assert.That(!d.Equals(e) && d.GetHashCode() != 0 && e.GetHashCode() != 0);
        }
    }
}
