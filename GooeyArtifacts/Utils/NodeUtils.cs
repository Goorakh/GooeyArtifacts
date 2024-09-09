using HG;
using RoR2;
using RoR2.Navigation;
using System;

namespace GooeyArtifacts.Utils
{
    public static class NodeUtils
    {
        public static void SetNodeOccupied(NodeGraph nodeGraph, NodeGraph.NodeIndex nodeIndex, bool occupied)
        {
            if (!nodeGraph)
                throw new ArgumentNullException(nameof(nodeGraph));

            DirectorCore directorCore = DirectorCore.instance;
            if (!directorCore)
                return;

            ref DirectorCore.NodeReference[] occupiedNodes = ref directorCore.occupiedNodes;

            DirectorCore.NodeReference nodeReference = new DirectorCore.NodeReference(nodeGraph, nodeIndex);
            int nodeReferenceIndex = Array.IndexOf(occupiedNodes, nodeReference);
            bool isOccupied = nodeReferenceIndex != -1;
            if (isOccupied == occupied)
                return;

            if (occupied)
            {
                directorCore.AddOccupiedNode(nodeGraph, nodeIndex);
            }
            else
            {
                ArrayUtils.ArrayRemoveAtAndResize(ref occupiedNodes, nodeReferenceIndex);
            }
        }
    }
}
