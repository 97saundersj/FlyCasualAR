namespace ExtraOptions
{
    namespace ExtraOptionsList
    {
        public class AugmentedRealityExtraOption : ExtraOption
        {
            public AugmentedRealityExtraOption()
            {
                Name = "Augmented Reality";
                Description = "Enables the experimental Augmented Reality mode.";
            }

            protected override void Activate()
            {
                DebugManager.AugmentedReality = true;
            }

            protected override void Deactivate()
            {
                DebugManager.AugmentedReality = false;
            }
        }
    }
}
