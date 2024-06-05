using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mineguide.perspectives.interactiveannotation.annotationFilters;

namespace Mineguide.perspectives.interactiveannotation.modeltransformations
{
    public static class RestrictionsExtensions
    {
        public static bool IsConnectedComponent(this TransformationRegion info)
        {
            //------------------------------------------------------------------------------------------------------------------
            // RESTRICTIONS:
            // It is considered a CONNECTED COMPONENT when it meets:
            // a) Number of nodes greater than 1            
            // b) There is only one entry node
            // c) There is only one exit node (We add an exception when all exit transitions go to the same destination node)
            // d) All nodes have at least one entry transition and one exit transition           
            //------------------------------------------------------------------------------------------------------------------

            // a) Number of nodes greater than 1
            if (info.Nodes.Length < 2) return false; // not fulfilled a)   // if there are less than two nodes, it is not a sequence

            var template = info.TPA;
            var seqInputs = new HashSet<Guid>(); // input nodes to the sequence
            var seqOutputs = new HashSet<Guid>(); // output nodes to the sequence
            var seqOutputsDestinations = new HashSet<Guid>(); // output nodes destinations to the sequence
            foreach (var n in info.Nodes)
            {
                int numInTransitions = 0;
                int numOutTransitions = 0;
                // search inputs to sequence selected
                foreach (var t in n.getInTransitions(template, false))
                {
                    numInTransitions++;
                    foreach (var sourceNodeId in t.SourceNodes)
                    {
                        if (!info.Nodes.Any(n3 => n3.Id == sourceNodeId))
                        {
                            seqInputs.Add(n.Id); // add input node to sequence inputs                                                   
                        }
                    }
                }
                // search outputs to sequence selected
                foreach (var t in n.getOutTransitions(template, false))
                {
                    numOutTransitions++;
                    foreach (var endNodeId in t.EndNodes)
                    {
                        if (!info.Nodes.Any(n3 => n3.Id == endNodeId))
                        {
                            seqOutputs.Add(n.Id); // add node to sequence outputs
                            seqOutputsDestinations.Add(endNodeId); // add node to sequence outputs destinations

                            //// e) If the output node is a decision, all destination nodes of the decision must be included
                            //if (n.IsDecision()) // if the output node is a decision node
                            //{
                            //    // not fulfilled e)
                            //    return false; // not all output nodes of the decision are included
                            //}
                        }
                    }
                }

                // d) All nodes have at least one entry transition and one exit transition
                if (numInTransitions < 1 || numOutTransitions < 1) // if there is a node without input or output transitions
                {
                    return false; // not fulfilled d)
                }
            }

            // b) There is only one entry node
            if (seqInputs.Count != 1) // if there is more than one input to the sequence
            {
                return false; // NO SE CUMPLE b)
            }

            // c) There is only one exit node (We add an exception when all exit transitions go to the same destination node)
            if (seqOutputs.Count != 1) // if there is more than one output to the sequence            
            {
                // -- exception if all outputs are to same node --
                if (seqOutputsDestinations.Count > 1) // if there is more than one node destination c) is not fulfilled             
                {
                    return false; // is not fulfilled c)
                }

                // EXCEPTION ALLOWED There is only one node destination (all outputs are to the same node)                                                          
            }

            return true;

        }       
    }
}
