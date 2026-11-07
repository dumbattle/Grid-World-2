using NUnit.Framework;

namespace GridWorld.Tests {
    public class WorldInstanceTests {
        class FirstContext : WorldFunctionContext { }
        class SecondContext : WorldFunctionContext { }

        // Reaches data through a generic constraint, as function extension methods do, rather than a cast.
        static WorldInstance ReadData<T>(T context) where T : IWorldFunctionContext {
            return context.data;
        }

        [Test]
        public void GetContext_FirstCall_BindsContextToWorld() {
            var world = new WorldInstance();

            var context = world.GetContext<FirstContext>();

            Assert.AreSame(world, ReadData(context));
        }

        [Test]
        public void GetContext_SameType_ReturnsCachedContext() {
            var world = new WorldInstance();

            Assert.AreSame(world.GetContext<FirstContext>(), world.GetContext<FirstContext>());
        }

        [Test]
        public void GetContext_DifferentTypes_ReturnsSeparateContexts() {
            var world = new WorldInstance();

            Assert.IsInstanceOf<FirstContext>(world.GetContext<FirstContext>());
            Assert.IsInstanceOf<SecondContext>(world.GetContext<SecondContext>());
        }

        [Test]
        public void GetContext_DifferentWorlds_BindsEachToItsOwnWorld() {
            var first = new WorldInstance();
            var second = new WorldInstance();

            Assert.AreSame(first, ReadData(first.GetContext<FirstContext>()));
            Assert.AreSame(second, ReadData(second.GetContext<FirstContext>()));
        }
    }
}
