using pm4h.data;
using pm4h.tpa;
using pm4h.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mineguide.perspectives.interactiveannotation.annotationFilters
{
    [Serializable]
    public class NodeReference
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string IdKeys { get; set; } = "";
        public bool SelfCycle { get; set; } = false;  
        public NodeReference[] Prefix { get; set; } = new NodeReference[] { };
        public bool IsEquivalent(PMEvent e)
        {
            if (e== null || e.ActivityName != Name) return false;
            return PMLogHelper.IsEquivalent(e.GetIdKeys(), PMDataHelper.GetIdKeys(IdKeys));
        }

        public bool IsEquivalent(TPATemplate.Node e)
        {
            if (e == null || e.Name != Name) return false;
            return PMLogHelper.IsEquivalent(PMDataHelper.GetIdKeys(e.IdKeys), PMDataHelper.GetIdKeys(IdKeys));
        }

        public bool IsEquivalent(NodeReference e)
        {
            if (e == null || e.Name != Name) return false;
            return PMLogHelper.IsEquivalent(PMDataHelper.GetIdKeys(e.IdKeys), PMDataHelper.GetIdKeys(IdKeys));
        }

        public bool IsEquivalent(PMEvent e, PMEvent[] eprefix)
        {
            if (IsEquivalent(e) )
            {
                if (Prefix.Length == 0) return true;
                if (eprefix.All(p => Prefix.Any(x => x.IsEquivalent(p))))
                {
                    return true;
                }
            }
            return false;


        }

      
        public bool IsAmbiguous(TPATemplate tpa)
        {
            int eq = 0;
            foreach(var n in tpa.Nodes)
            {
                if (IsEquivalent(n)) eq++;
            }
            return eq>1;
        }

        public static NodeReference[] CreateHistory(TPATemplate.Node n, TPATemplate tpa)
        {
            List<NodeReference> res = new List<NodeReference>();
            res.Add(CreateNodeReference(n,tpa));
            foreach (var x in n.GetBackWardNodes(tpa))
            {
                foreach(var y in x)
                {
                    res.Add(CreateNodeReference(y, tpa));
                }
            }
            return res.ToArray();
        }

        public static NodeReference CreateNodeReference(TPATemplate.Node n, TPATemplate tpa)
        {
            NodeReference id = new NodeReference();
            id.Id = n.Id;
            id.Name = n.Name;
            id.IdKeys = n.IdKeys;
            id.SelfCycle = n.IsSelfCycle(tpa);
            return id;
        }

        public static NodeReference FromNode(TPATemplate.Node n, TPATemplate tpa)
        {
            NodeReference id = CreateNodeReference(n, tpa);
            int index = 0;
            if (id.IsAmbiguous(tpa))
            {
                id.Prefix = CreateHistory(n, tpa);
            }
            return id;
        }

        public Dictionary<string, string> GetIdKeys()
        {
            return PMDataHelper.GetIdKeys(this.IdKeys);
        }

        public string GetIdKey(string s)
        {
            Dictionary<string, string> idKeys = GetIdKeys();
            if (idKeys.ContainsKey(s))
            {
                return idKeys[s];
            }

            return null;
        }
    }



    [Serializable]
    public class NodePattern
    {
        public NodeReference[] Pattern { get; set; }

        

    }
}
