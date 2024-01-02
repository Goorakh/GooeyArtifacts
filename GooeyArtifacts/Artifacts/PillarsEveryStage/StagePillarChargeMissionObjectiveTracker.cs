using RoR2;
using RoR2.UI;

namespace GooeyArtifacts.Artifacts.PillarsEveryStage
{
    public class StagePillarChargeMissionObjectiveTracker : ObjectivePanelController.ObjectiveTracker
    {
        int _chargedPillars, _requiredPillars;

        public override string GenerateString()
        {
            StagePillarChargeMissionController missionController = sourceDescriptor.source as StagePillarChargeMissionController;

            _requiredPillars = missionController.RequiredPillarCount;
            _chargedPillars = missionController.ChargedPillarCount;

            return Language.GetStringFormatted("OBJECTIVE_MOON_BATTERY_MISSION", _chargedPillars, _requiredPillars);
        }

        public override bool IsDirty()
        {
            if (base.IsDirty())
                return true;

            StagePillarChargeMissionController missionController = sourceDescriptor.source as StagePillarChargeMissionController;
            return missionController.RequiredPillarCount != _requiredPillars || missionController.ChargedPillarCount != _chargedPillars;
        }
    }
}
