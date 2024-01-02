using RoR2;
using RoR2.UI;
using System.Collections.Generic;

namespace GooeyArtifacts.Patches
{
    static class ObjectiveSourceModifierHook
    {
        public delegate void OverrideObjectiveSourceDelegate(ref ObjectivePanelController.ObjectiveSourceDescriptor descriptor, ref bool visible);
        public static event OverrideObjectiveSourceDelegate OverrideObjectiveSource;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.ObjectivePanelController.GetObjectiveSources += ObjectivePanelController_GetObjectiveSources;
        }

        static void ObjectivePanelController_GetObjectiveSources(On.RoR2.UI.ObjectivePanelController.orig_GetObjectiveSources orig, ObjectivePanelController self, CharacterMaster master, List<ObjectivePanelController.ObjectiveSourceDescriptor> output)
        {
            orig(self, master, output);

            if (OverrideObjectiveSource != null)
            {
                for (int i = output.Count - 1; i >= 0; i--)
                {
                    ObjectivePanelController.ObjectiveSourceDescriptor objectiveDescriptor = output[i];
                    bool visible = true;

                    OverrideObjectiveSource(ref objectiveDescriptor, ref visible);

                    if (visible)
                    {
                        output[i] = objectiveDescriptor;
                    }
                    else
                    {
                        output.RemoveAt(i);
                    }
                }
            }
        }
    }
}
