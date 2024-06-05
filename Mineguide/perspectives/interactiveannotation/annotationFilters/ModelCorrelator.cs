using pm4h.tpa.ipi;
using pm4h.tpa;
using pm4h.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pm4h.data;

namespace Mineguide.perspectives.interactiveannotation.annotationFilters
{
    public  class ModelCorrelator
    {

        static Dictionary<Guid, NodeAlignment> ComputeNodeCorrelations (TPATemplate tpa0, TPATemplate tpa1)
        {
            Dictionary<Guid, NodeAlignment> nodes = new Dictionary<Guid, NodeAlignment>();
             
            foreach (var n in tpa0.Nodes)
            {
                var al = new NodeAlignment()
                {
                    Name = n.Name,
                    tpa1 = tpa0,
                    tpa2 = tpa1,
                    Nodes1 = tpa0.Nodes.Where(t => PMLogHelper.IsEquivalent(t, n)).ToList(),
                    Nodes2 = tpa1.Nodes.Where(t => PMLogHelper.IsEquivalent(t, n)).ToList(),

                };

                nodes[n.Id] = al;

            }
            return nodes;
        }

        public static Dictionary<TPATemplate.Node, TPATemplate.Node> ComputeNodeCorrelations(iTPAModel m0, iTPAModel m1)
        {
            Dictionary<TPATemplate.Node, TPATemplate.Node> res = new Dictionary<TPATemplate.Node, TPATemplate.Node>();
            var nc = ComputeNodeCorrelations(m0.getTPATemplate(),m1.getTPATemplate());
            foreach (var i in nc)
            {
                foreach(var al in i.Value.getAlignments())
                {
                    res[al.Key] = al.Value;
                }

                
            }
            return res;

        }

        public static iTPAModel CoordinateNodeIds(iTPAModel pattern,iTPAModel model)
        {
            var corr = ComputeNodeCorrelations(pattern,model);

            foreach (var i in corr)
            {
                ReMapNodeId(model, i.Value.Id, i.Key.Id);
            }


            return model;
        }

        static void ReMapNodeId(iTPAModel m, Guid oldId, Guid newid)
        {
            if (m is MemoryTPA mtpa)
            {
                var n = m.getTPATemplate().FindNodebyId(oldId);
                n.Id = newid;
                mtpa.NodeReferences[newid] = mtpa.NodeReferences[oldId];
                mtpa.Posiciones[newid] = mtpa.Posiciones[oldId];

                foreach(var nt in mtpa.getTPATemplate().NodeTransitions)
                {
                    if (nt.SourceNodes.Contains(oldId))
                    {
                        nt.SourceNodes.Remove(oldId);
                        nt.SourceNodes.Add(newid);
                    }
                    if (nt.EndNodes.Contains(oldId))
                    {
                        nt.EndNodes.Remove(oldId);
                        nt.EndNodes.Add(newid);
                    }
                }

                foreach (var nsl in m.getNodeStatsLayers().IterateLayers().ToArray())
                {
                    nsl[newid] = nsl[oldId];
                }
                foreach (var nsl in m.getNodeMapsLayers().IterateLayers().ToArray())
                {
                    nsl[newid] = nsl[oldId];
                }
            }
        }

        static TPATemplate.Node[] GetPosibleCorrelations(TPATemplate.Node n0, IEnumerable<TPATemplate.Node> m1)
        {
            var res = m1.Where(n1 => PMLogHelper.IsEquivalent(n0, n1)).ToArray();
            return res;
        }


        public static TPATemplate.Node GetAssociatedNode(PMEvent e, iTPAModel m)
        {
            var eqnodes = m.getTPATemplate().Nodes.Where(n => PMLogHelper.IsEquivalent(n, e)).ToArray();
            if (eqnodes.Length == 0) return null;
            if (eqnodes.Length == 1) return eqnodes.First();

            return null;
        }

    }
}
