using NUnit.Framework;

namespace GridWorld.Tests {
    public class TickClockTests {
        const float Tick = TickClock.TickSeconds;

        [Test]
        public void Advance_LessThanATick_RunsNoneAndBlendsPartWay() {
            var clock = new TickClock();

            Assert.AreEqual(0, clock.Advance(Tick * 0.5f));
            Assert.AreEqual(0.5f, clock.alpha, 1e-4f);
        }

        [Test]
        public void Advance_TimeAddsUpAcrossFrames_RunsATick() {
            var clock = new TickClock();

            clock.Advance(Tick * 0.5f);

            Assert.AreEqual(1, clock.Advance(Tick * 0.5f));
            Assert.AreEqual(0f, clock.alpha, 1e-4f);
        }

        [Test]
        public void Advance_SeveralTicksOfTime_RunsEachAndKeepsTheFraction() {
            var clock = new TickClock();

            Assert.AreEqual(2, clock.Advance(Tick * 2.25f));
            Assert.AreEqual(0.25f, clock.alpha, 1e-3f);
        }

        [Test]
        public void Advance_LongFrame_RunsAtMostFourAndDropsTheRest() {
            var clock = new TickClock();

            Assert.AreEqual(TickClock.MaxTicksPerFrame, clock.Advance(Tick * 10.5f));
            Assert.Less(clock.alpha, 1f);
            Assert.AreEqual(0, clock.Advance(0f));
        }
    }
}
