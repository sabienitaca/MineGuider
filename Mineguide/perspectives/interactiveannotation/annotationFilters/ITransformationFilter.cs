using Accord.Math;
using com.espertech.esper.compat.collections;
using pm4h.algorithm.i2palia;
using pm4h.algorithm.palia.Core.ipalia;
using pm4h.algorithm.palia.ipalia;
using pm4h.data;
using pm4h.filter;
using pm4h.tpa;
using pm4h.tpa.ipi;
using pm4h.utils;
using pm4h.windows.ui.fragments.tpaviewer.designer.tpacontrol.visualelements;
using Sabien.Core3.Utils.core2.Math.Combinatorics;
using Sabien.Utils.Combinatorics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;

namespace Mineguide.perspectives.interactiveannotation.annotationFilters
{
    
    public class TransformationRegion
    {
        public TransformationRegion(iTPAModel model, Estado[] states)
        {
            Model = model;
            States = states;
        }

        public iTPAModel Model { get; set; }
        public Estado[] States { get; set; }

        protected TPATemplate.Node[] _nodes;
        public TPATemplate.Node[] Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    if (Model == null || States == null) return new TPATemplate.Node[0];
                    _nodes = States.Select(s => TPA.FindNodebyId(s.ID)).ToArray();
                }
                return _nodes;
            }
        }

        public TPATemplate TPA
        {
            get
            {
                return Model?.getTPATemplate();
            }
        }

    }

    public interface ITransformationFilter:IPMLogFilter
    {
        public bool IsApplicable(TransformationRegion info);
    }

    public static class TransformationFilterHelper
    {

        public static bool IsPattern(this NodeReference[] nodes, PMEvent[] evts)
        {
            if (nodes.Length != evts.Length) { return false; }
            for (int x = 0; x < evts.Length; x++)
            {
                if (!nodes[x].IsEquivalent(evts[x])) return false;
            }
            return true;

        }


        public static PMEvent[] GetWindow(this NodeReference[] nodes, TraceMetadata md, PMEvent _ev)
        {

            List<PMEvent> events = new List<PMEvent>();
            for (int x = 1; x < nodes.Length; x++)
            {
                var ne = md.getRelativeNewEvent(-x);
                events.Add(ne);
            }
            events.Add(_ev);
            events = events.Where(e => e != null).OrderBy(e => e.Start).ToList();
            return events.ToArray();
        }

        public static bool IsInRegion(this TransformationRegion region, TPATemplate.Node n)
        {
            return region.Nodes.Contains(n);    
        }

        public static i2Node[] GetI2Nodes(i2TPA model, TPATemplate.Node[] nodes)
        {
            List<i2Node> result = new List<i2Node>();

            foreach(var node in nodes)
            {
                result.Add(model.Nodes[node.Id]);
            }

            return result.ToArray();
        }
        public static bool IsSelfCycle(this TPATemplate.Node nd, TPATemplate model)
        {
            return nd.GetNextNodes(model).Contains(nd);
        }

        public static bool IsSelfCycle(this TransformationRegion nd)
        {
            if (nd.Nodes.Count() != 1) return false;
            return nd.Nodes.First().IsSelfCycle(nd.TPA);
        }


        public static TPATemplate.Node[] GetTPANodes(TPATemplate model, i2Node[] nodes)
        {
            List<TPATemplate.Node> result = new List<TPATemplate.Node>();

            foreach (var node in nodes)
            {
                result.Add(model.FindNodebyId(node.Id));
            }

            return result.ToArray();
        }

        public static TPATemplate.Node[] GetNextNodes(this TPATemplate.Node[] n0, TPATemplate tpa)
        {
            HashSet<TPATemplate.Node> res = new HashSet<TPATemplate.Node>();
            foreach (var nx in n0)
            {
                foreach (var x in nx.GetNextNodes(tpa))
                {
                    res.Add(x);
                }
            }
            return res.ToArray();
        }
        public static TPATemplate.Node[] GetNextNodes(this TPATemplate.Node n0, TPATemplate tpa )
        {
            HashSet<TPATemplate.Node> res = new HashSet<TPATemplate.Node>();
            foreach (var t in n0.getOutTransitions(tpa))
            {
                foreach(var x in t.getEndNodes(tpa))
                {
                    res.Add(x);
                }
            }
            return res.ToArray();
        }

        public static TPATemplate.Node[] GetPreviousNodes(this TPATemplate.Node[] n0, TPATemplate tpa)
        {
            HashSet<TPATemplate.Node> res = new HashSet<TPATemplate.Node>();
            foreach (var nx in n0)
            {
                foreach (var x in nx.GetPreviousNodes(tpa))
                {
                    res.Add(x);
                }
            }
            return res.ToArray();
        }

        public static TPATemplate.Node[] GetPreviousNodes(this TPATemplate.Node n0, TPATemplate tpa)
        {
            HashSet<TPATemplate.Node> res = new HashSet<TPATemplate.Node>();
            foreach (var t in n0.getInTransitions(tpa))
            {
                foreach (var x in t.getSourceNodes(tpa))
                {
                    res.Add(x);
                }
            }
            return res.ToArray();
        }

        public static TPATemplate.Node[][] GetForwardNodes(this TPATemplate.Node n0, TPATemplate tpa)
        {
            HashSet<TPATemplate.Node> visited = new HashSet<TPATemplate.Node>();
            visited.Add(n0);
            List<TPATemplate.Node[]> res = new List<TPATemplate.Node[]>();
            var S = new TPATemplate.Node[] { n0 };
            while (S.Length > 0)
            {
                var nn = S.GetNextNodes(tpa).Except(visited).ToArray();
                if (nn.Length > 0)
                {
                    res.Add(nn);
                    foreach (var x in nn) visited.Add(x);
                }
                S = nn;
            }
            return res.ToArray();
        }

        public static TPATemplate.Node[][] GetBackWardNodes(this TPATemplate.Node n0, TPATemplate tpa)
        {
            HashSet<TPATemplate.Node> visited = new HashSet<TPATemplate.Node>();
            visited.Add(n0);
            List<TPATemplate.Node[]> res = new List<TPATemplate.Node[]>();
            var S = new TPATemplate.Node[] { n0 };
            while (S.Length > 0)
            {
                var nn = S.GetPreviousNodes(tpa).Except(visited).ToArray();
                if (nn.Length > 0)
                {
                    res.Add(nn);
                    foreach (var x in nn) visited.Add(x);
                }
                S = nn;
            }

            return res.ToArray();
        }

        public static TPATemplate.Node[] ToOneArray(this TPATemplate.Node[][] n)
        {
            HashSet<TPATemplate.Node> res = new HashSet<TPATemplate.Node>();
            foreach(var x in n) foreach( var x1 in x) res.Add(x1);
            return res.ToArray();
        }

        public static TPATemplate.Node[] GetRegionEntries(this TransformationRegion Region)
        {
            var tpa = Region.Model.getTPATemplate();
            List<TPATemplate.Node> res  = new List<TPATemplate.Node>();

            foreach (var n in Region.Nodes)
            {
                foreach (var t in n.getInTransitions(tpa))
                {
                    if (t.getSourceNodes(tpa).Any(x => !IsInRegion(Region, x)))
                    {
                        res.Add(n);
                        break;
                    }
                }
            }


            return res.ToArray();
        }
        public static TPATemplate.Node[] GetRegionOutputs(this TransformationRegion Region)
        {
            var tpa = Region.Model.getTPATemplate();
            List<TPATemplate.Node> res = new List<TPATemplate.Node>();

            foreach (var n in Region.Nodes)
            {
                foreach (var t in n.getOutTransitions(tpa))
                {
                    if (t.getEndNodes(tpa).Any(x => !IsInRegion(Region, x)))
                    {
                        res.Add(n);
                        break;
                    }
                }
            }


            return res.ToArray();
        }

        public static TPATemplate.Node[] GetSyncronizationNodes(this TransformationRegion Region)
        {
            var res = new HashSet<TPATemplate.Node> { };
            foreach(var n in Region.Nodes)
            {
                var bn = n.GetForwardNodes(Region.TPA);
                var nx = bn.FirstOrDefault(x => x.Any(y => !Region.Nodes.Contains(y)));
                nx.ForEach(x => res.Add(x));
            }
            return res.ToArray();  
        }

        public static TPATemplate.Node[] GetSplitNodes(this TransformationRegion Region)
        {
            var res = new HashSet<TPATemplate.Node> { };
            foreach (var n in Region.Nodes)
            {
                var bn = n.GetBackWardNodes(Region.TPA);
                var nx = bn.FirstOrDefault(x => x.Any(y => !Region.Nodes.Contains(y)));
                nx.ForEach(x=>res.Add(x));
            }
            return res.ToArray();
        }


        public static bool IsIsolated(this TransformationRegion Region, TPATemplate.Node n, IEnumerable<TPATemplate.Node> BorderNodes)
        {
            if (!Region.Nodes.Contains(n)) return false;

            var totalregion = Region.Nodes.Union(BorderNodes);

            return (n.GetNextNodes(Region.TPA).All(x => totalregion.Contains(x)) &&
                n.GetPreviousNodes(Region.TPA).All(x => totalregion.Contains(x)));
        }

        public static TPATemplate.Node[] GetSequence(this TransformationRegion Region)
        {


            var prm = new Permutations<Guid>(Region.Nodes.Select(x=>x.Id).ToList(), GenerateOption.WithoutRepetition);

            foreach (var px in prm)
            {
                var p = px as IEnumerable<Guid>;
                var sq = p.Select(x => Region.TPA.FindNodebyId(x)).ToArray();
                if (sq.IsSequence(Region.TPA)) return sq;
            }
            return null;
            
        }

        public static bool IsSequence(this TPATemplate.Node[] nodes, TPATemplate tpa)
        {
            for (int x = 1; x < nodes.Length; x++)
            {
                var nx = nodes[x-1].GetNextNodes(tpa);
                if (!nodes[x - 1].GetNextNodes(tpa).Contains(nodes[x])) return false;
            }

            return true;
        }

        public static IEnumerable<PMEvent> GetEquivalents(this NodeReference node, IEnumerable<PMEvent> events)
        {
            foreach (var e in events)
            {
               if (node.IsEquivalent(e))
                {
                    yield return e;
                }
            }
        }

        #region Patterns

        public static PMEvent[] Subtrace(this PMEvent[] trc, PMEvent point, bool include = true)
        {
            var res = trc.SkipWhile(x=>x!=point).ToArray();
            if (include) return res;
            return res.Skip(1).ToArray();
        }
        public static PMEvent[] NextPattern(this NodeReference nr, PMEvent[] trc)
        {
            if (nr.SelfCycle)
            {
                return trc.TakeWhile(x => nr.IsEquivalent(x)).ToArray();
            }
            if (nr.IsEquivalent(trc.FirstOrDefault()))
            {
                return new PMEvent[] { trc.First() };
            }
            return new PMEvent[]{};
        }
        public static PMEvent[] NextEventualPattern(this NodeReference nr, PMEvent[] trc)
        {
            var point =  nr.FindFirst(trc);
            var sb = trc.Subtrace(point);
            return nr.NextPattern(sb);
        }
        public static PMEvent FindFirst(this NodeReference nr,PMEvent[] trc)
        {
            for(int x=0; x < trc.Length; x++)
            {
                if (nr.IsEquivalent(trc[x],trc.Take(x).ToArray()))
                {
                    return trc[x];
                }
            }
            return null;
            return trc.FirstOrDefault(x => nr.IsEquivalent(x));
        }
        public static IEnumerable<PMEvent[]> Patterns(this NodeReference node, PMEvent[] trc)
        {
            List<PMEvent[]> res = new List<PMEvent[]>();
            var strace = trc;

            while (strace!=null && strace.Length > 0)
            {
                var s =  node.NextEventualPattern(strace);
                if (s!=null && s.Length>0)
                {
                    res.Add(s);
                   strace =  strace.Subtrace(s.Last(), false);
                }
                else
                {
                    strace = s;
                }

                
            }

            

            return res;

        }



        public static IEnumerable<PMEvent[]> Patterns(this NodeReference[] node, PMEvent[] trc)
        {

           
            List<PMEvent[]> res = new List<PMEvent[]>();
            if (node.Length == 0) return res;
            var startpoints = node.First().Patterns(trc);
            foreach (var p in startpoints)
            {
                var strc = trc.Subtrace(p.Last(), false);
                bool valid = true;
                List<PMEvent> resx = new List<PMEvent>();
                resx.AddRange(p);
                foreach (var nr in node.Skip(1))
                {
                    
                    if (nr.NextPattern(strc) is PMEvent[] e && e.Length > 0)
                    {
                        resx.AddRange(e);
                        strc = strc.Subtrace(e.Last(), false);
                    }
                    else
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                {
                    res.Add(resx.ToArray());
                    // siguientes patrones:
                    res.AddRange(node.Patterns(strc.Subtrace(resx.Last(), false)));
                    return res;
                }



            }
            return res;

        }



        #endregion

    }
}
