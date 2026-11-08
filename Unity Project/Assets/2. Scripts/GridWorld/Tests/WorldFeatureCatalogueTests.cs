using System;
using NUnit.Framework;

namespace GridWorld.Tests {
    public class WorldFeatureCatalogueTests {
        class MarkerData : WorldFeatureData { }

        class MarkerType : WorldFeatureType<MarkerData> {
            protected override void Register(WorldSetupContext ctx, AreaDataSO area, MarkerData data) { }
        }

        class OtherMarkerType : WorldFeatureType<MarkerData> {
            protected override void Register(WorldSetupContext ctx, AreaDataSO area, MarkerData data) { }
        }

        [Test]
        public void Register_NewDataType_IsContained() {
            var catalogue = new WorldFeatureCatalogue();

            catalogue.Register(new MarkerType());

            Assert.IsTrue(catalogue.Contains(typeof(MarkerData)));
        }

        [Test]
        public void Register_DataTypeAlreadyRegistered_ThrowsNamingTypes() {
            var catalogue = new WorldFeatureCatalogue();
            catalogue.Register(new MarkerType());

            var error = Assert.Throws<InvalidOperationException>(() => {
                catalogue.Register(new OtherMarkerType());
            });

            StringAssert.Contains(nameof(MarkerData), error.Message);
            StringAssert.Contains(nameof(MarkerType), error.Message);
            StringAssert.Contains(nameof(OtherMarkerType), error.Message);
        }

        [Test]
        public void Register_Null_Throws() {
            Assert.Throws<ArgumentNullException>(() => {
                new WorldFeatureCatalogue().Register<MarkerData>(null);
            });
        }
    }
}
