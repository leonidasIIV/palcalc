﻿using GraphSharp;
using PalCalc.Solver;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalCalc.UI.Model
{
    public class BreedingEdge : TypedEdge<BreedingTreeNodeViewModel>
    {
        public BreedingEdge(BreedingTreeNodeViewModel parent, BreedingTreeNodeViewModel child) : base(parent, child, EdgeTypes.Hierarchical) { }

        public override string ToString() => "";
    }

    public class BreedingGraph : HierarchicalGraph<BreedingTreeNodeViewModel, BreedingEdge>
    {
        private BreedingGraph(BreedingTree tree)
        {
            Tree = tree;
            Nodes = tree.AllNodes.Select(p => new BreedingTreeNodeViewModel(p.Item1)).ToList();
        }

        public BreedingTree Tree { get; private set; }
        public List<BreedingTreeNodeViewModel> Nodes { get; }

        public BreedingTreeNodeViewModel NodeFor(IBreedingTreeNode pref) => Nodes.Single(n => n.Value == pref);

        public static BreedingGraph FromPalReference(IPalReference palRef)
        {
            var tree = new BreedingTree(palRef);
            var result = new BreedingGraph(tree);

            result.AddVertexRange(result.Nodes);

            // breeding tree is upside down relative to breeding direction
            foreach (var (child, _) in tree.AllNodes)
                foreach (var parent in child.Children)
                    result.AddEdge(new BreedingEdge(parent: result.NodeFor(parent), child: result.NodeFor(child)));

            return result;
            
        }
    }
}
