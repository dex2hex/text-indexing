/////////////////////////////////////////////////////////////////////
//      File Name          : SuffixTree.cs
//      Created            : 11 9 2013   21:20
//      Author             : Costin S
//
/////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace TextIndexing
{
    using Dictionary = Dictionary<char, ISuffixNode>;
    using SortedList = SortedList<char, ISuffixNode>;
    using SortedDictionary = SortedDictionary<char, ISuffixNode>;

    public interface IEdgeLabel
    {
        int Start 
        { get; }

        int End 
        { get; }
    }

    public interface ISuffixTree
    {
        #region Properties

        string Text 
        { get; }

        ISuffixNode Root 
        { get; }

        #endregion

        #region Methods

        bool Contains(string text);
        IEnumerable<int> Search(string text);

        #endregion
    }

    public interface ISuffixNode
    {
        #region Properties

        ISuffixNode Link
        { get; }

        ISuffixNode Parent
        { get; }

        IDictionary<char, ISuffixNode> Children
        { get; }

        IEdgeLabel Edge
        { get; }

        bool IsLeaf
        { get; }

        int LeafNumber
        { get; }

        #endregion
    }

    /// <summary>
    /// Ukkonen's O(n) online suffix tree construction.
    /// </summary>
    public sealed class SuffixTree : ISuffixTree
    {
        #region C'tor

        private SuffixTree()
        {
        }

        #endregion

        #region Properties

        public ISuffixNode Root { get; private set; }
        public string Text { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a suffix tree for the given text.
        /// Make sure the last character in 'text' is unique; im using $..
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">The text.</param>
        /// <returns>if the argument 'text' is empty or null string, function returns null !!</returns>
        public static ISuffixTree Create<T>(string text)
            where T : IDictionary<char, ISuffixNode>, new()
        {
            var suffixImpl = SuffixTreeImpl<T>.Create(text);
            return suffixImpl == null ? null : new SuffixTree() { Root = suffixImpl.Root, Text = suffixImpl.Text };
        }

        /// <summary>
        /// Creates a suffix tree for the given text. 
        /// Make sure the last character in 'text' is unique; im using $..
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>if the argument 'text' is empty or null string, function returns null !!</returns>
        public static ISuffixTree Create(string text)
        {
            return SuffixTree.Create<Dictionary>(text);
        }

        /// <summary>
        /// Determines whether [contains] [the specified text].
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>
        ///   <c>true</c> if [contains] [the specified text]; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var matchResult = new TextMatcher(this).Match(text);
                return (matchResult != null && matchResult.MatchedCount == text.Length);
            }
            else return false;
        }

        /// <summary>
        /// Searches for specified text within the tree and returns the list of matching starting positions.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="matchList">The match list.</param>
        /// <returns></returns>
        public IEnumerable<int> Search(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var matchResult = new TextMatcher(this).Match(text);

                if (matchResult != null && matchResult.MatchingSuffixes != null && matchResult.MatchedCount == text.Length) {
                    foreach (var suffixNode in matchResult.MatchingSuffixes)
                    {
                        yield return suffixNode.LeafNumber;
                    }
                }
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Algorithm implementation, parameterized on the collection used to store nodes' children.. 
        /// Options other than the Dictionary, SortedDictionary and SortedList considered here do exist!
        /// </summary>
        /// <typeparam name="ChildrenCollectionType">The children collection type.</typeparam>
        private class SuffixTreeImpl<ChildrenCollectionType>
            where ChildrenCollectionType : IDictionary<char, ISuffixNode>, new()
        {
            #region Fields

            private SuffixNode theRoot;

            #endregion

            #region C'tor

            /// <summary>
            /// C'tor
            /// </summary>
            /// <param name="text"></param>
            private SuffixTreeImpl(string text)
            {
                this.theRoot = new SuffixNode() { Parent = null };
                this.Text = text;
            }

            #endregion

            #region Properties

            public string Text
            { get; private set; }            

            public ISuffixNode Root
            {
                get
                {
                    return this.theRoot;
                }
            }

            private int CurrentPhase
            { get; set; }

            #endregion            

            #region Public Methods

            public static SuffixTreeImpl<ChildrenCollectionType> Create(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return null;
                }

                var tree = new SuffixTreeImpl<ChildrenCollectionType>(text);
                var root = tree.theRoot;                
                var deep = root.AddEdge(tree.Text, 0, -1);
                deep.SetLeaf(0);

                SuffixNode prevExtEnd = deep;
                int lastCreatedLeaf = -1;
                int m = text.Length;

                for (int i = 1; i < m; i++)
                {
                    tree.CurrentPhase = i;

                    bool skipRemaining = false;
                    SuffixNode internCreatedPrevExt = null;

                    int j = (lastCreatedLeaf == -1) ? 1 : lastCreatedLeaf;
                    for (; j < i && !skipRemaining; j++)
                    {
                        IEdgeLabel[] edges = null;
                        SuffixNode found = null;
                        int edgecursor = -1;
                        bool matchEndedAtNode;

                        if (j == lastCreatedLeaf && j > 1)
                        {
                            if (!prevExtEnd.IsLeaf)
                            {
                                edges = new IEdgeLabel[] { EdgeLabel.Create(i - 1, i - 1) };
                                matchEndedAtNode = tree.Match(prevExtEnd, edges, out found, out edgecursor);
                            }
                            else
                            {
                                found = prevExtEnd;
                                matchEndedAtNode = true;
                            }
                        }

                        if (found == null)
                        {
                            SuffixNode target = null;
                            if (tree.PreMatch(prevExtEnd, out target, out edges))
                            {
                                edges = new IEdgeLabel[] { EdgeLabel.Create(j, i - 1) };
                                matchEndedAtNode = tree.Match(tree.theRoot, edges, out found, out edgecursor);
                            }
                            else
                            {
                                if (edges != null)
                                {
                                    matchEndedAtNode = tree.Match(target, edges, out found, out edgecursor);
                                }
                                else
                                {
                                    found = target;
                                    matchEndedAtNode = true;
                                }
                            }
                        }
                        else
                        {
                            matchEndedAtNode = true;
                        }

                        if (!matchEndedAtNode)
                        {
                            if (tree.Text[edgecursor] == tree.Text[i])
                            {
                                skipRemaining = true;
                                break;
                            }
                            else
                            {
                                var foundParent = found.Parent;

                                // there's no node here .. better create one..
                                foundParent.RemoveEdge(tree.GetCharFromIndex(found.Edge.Start));

                                // create new node..
                                var internalNode = foundParent.AddEdge(tree.Text, found.Edge.Start, edgecursor - 1);

                                // massage old node and add it back..
                                found.Parent = internalNode;
                                found.Edge.Start = edgecursor;
                                internalNode.AddEdge(tree.Text, found);

                                // fix up links if we need to ..
                                if (internCreatedPrevExt != null)
                                {
                                    internCreatedPrevExt.Link = internalNode;
                                }

                                internCreatedPrevExt = internalNode;

                                // create a new leaf and hang it here.. 
                                var newLeaf = internalNode.AddEdge(tree.Text, i, -1);
                                newLeaf.SetLeaf(j);
                                lastCreatedLeaf = j;

                                prevExtEnd = internalNode;
                            }
                        }
                        else
                        {
                            if (found.IsLeaf)
                            {
                                prevExtEnd = found;
                            }
                            else
                            {
                                if (internCreatedPrevExt != null)
                                {
                                    internCreatedPrevExt.Link = found;
                                }

                                internCreatedPrevExt = found.Link == null ? found : null;

                                if (found.GetEdge(tree.GetCharFromIndex(i)) == null)
                                {
                                    var newLeaf = found.AddEdge(tree.Text, i, -1);
                                    newLeaf.SetLeaf(j);
                                    lastCreatedLeaf = j;

                                    prevExtEnd = found;
                                }
                                else
                                {
                                    skipRemaining = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (!skipRemaining)
                    {
                        var parent = root;
                        var child = parent.GetEdge(tree.GetCharFromIndex(i));
                        if (child == null)
                        {
                            var newLeaf = parent.AddEdge(tree.Text, i, -1);
                            newLeaf.SetLeaf(i);

                            lastCreatedLeaf = i;
                            prevExtEnd = newLeaf;
                        }

                        if (internCreatedPrevExt != null)
                        {
                            internCreatedPrevExt.Link = parent;
                            internCreatedPrevExt = null;
                        }
                    }
                }

                tree.FixupLeaves(tree.theRoot, tree.Text.Length - 1);
                return tree;
            }            

            #endregion

            #region Private Methods

            private int GetEdgeLength(IEdgeLabel e)
            {
                return (e.End != -1 ? e.End : this.CurrentPhase - 1) - e.Start + 1;
            }

            private char GetCharFromIndex(int index)
            {
                if (0 <= index && index < this.Text.Length)
                {
                    return this.Text[index];
                }
                else throw new ArgumentOutOfRangeException("index");
            }

            /// <summary>
            /// pre-match routine
            /// </summary>
            /// <param name="p">node to start pre-matching from</param>
            /// <param name="target">if method returns true, this value will be ignored; otherwise, upon return, it contains the node to start the next matching from</param>
            /// <param name="edges">if method returns true, this will be ignored; otherwise, upon return, it contains the set of edges to be matched next, 
            /// or null (if no further matching should be performed next</param>
            /// <returns>true if the next match should be started from root node, false otherwise</returns>
            private bool PreMatch(SuffixNode p, out SuffixNode target, out IEdgeLabel[] edges)
            {
                Debug.Assert(p != this.theRoot);

                target = null;
                edges = null;

                bool goDownFromRoot = true;
                var v = p.Parent;

                if (p.Link != null)
                {
                    target = p.Link;
                    edges = null;
                    goDownFromRoot = false;
                }
                else if (v != this.theRoot)
                {
                    if (v.Link == null)
                    {
                        var w = v.Parent;

                        if (w != this.theRoot)
                        {
                            Debug.Assert(w.Link != null);

                            edges = new EdgeLabel[2] {
                                EdgeLabel.Create(v.Edge.Start, v.Edge.End == -1 ? (this.CurrentPhase - 1) : v.Edge.End),
                                EdgeLabel.Create(p.Edge.Start, p.Edge.End == -1 ? (this.CurrentPhase - 1) : p.Edge.End)
                            };

                            target = w.Link;
                            goDownFromRoot = false;
                        }
                    }
                    else
                    {
                        Debug.Assert(v.Link != null);

                        edges = new EdgeLabel[1]{
                            EdgeLabel.Create(p.Edge.Start, p.Edge.End == -1 ? (this.CurrentPhase - 1) : p.Edge.End)
                        };

                        target = v.Link;
                        goDownFromRoot = false;
                    }
                }

                return goDownFromRoot;
            }

            /// <summary>
            /// match routine
            /// </summary>
            /// <param name="node">node to start matching from</param>
            /// <param name="edges">set of edges to be matched tree edges against</param>
            /// <param name="childNode">on return should contain the node reached by the matching routing</param>
            /// <param name="unmatchedEdgeCursor">if matching didn't finish at an internal node, it contains the first unmatched char index</param>
            /// <returns>true if matching ended at node; false if matching finished inside an edge</returns>
            private bool Match(SuffixNode node, IEdgeLabel[] edges, out SuffixNode childNode, out int unmatchedEdgeCursor)
            {
                if (node == null)
                {
                    throw new ArgumentNullException("parent");
                }

                if (edges == null || edges.Length <= 0)
                {
                    throw new ArgumentException("invalid argument", "edges");
                }

                unmatchedEdgeCursor = -1;
                var matchingEdgeIndex = 0;
                var matchingEdgeCursor = edges[matchingEdgeIndex].Start;
                var matchingEdgeEnd = edges[matchingEdgeIndex].End;

                childNode = this.TraverseEdge(node, matchingEdgeCursor);
                var treeEdgeLength = this.GetEdgeLength(childNode.Edge);

                do
                {
                    var diff = matchingEdgeEnd - matchingEdgeCursor + 1 - treeEdgeLength;

                    if (diff > 0)
                    {
                        matchingEdgeCursor += treeEdgeLength;
                    }
                    else
                    {
                        if (++matchingEdgeIndex < edges.Length)
                        {
                            matchingEdgeCursor = edges[matchingEdgeIndex].Start;
                            matchingEdgeEnd = edges[matchingEdgeIndex].End;
                        }
                        else
                        {
                            // nothing more to match .. 
                            if (diff < 0) //unmatched = -diff
                            {
                                unmatchedEdgeCursor = (childNode.Edge.End == -1 ? (this.CurrentPhase - 1) : childNode.Edge.End) + diff + 1;
                            }
                            break;
                        }
                    }

                    if (diff >= 0)
                    {
                        node = childNode;
                        childNode = this.TraverseEdge(node, matchingEdgeCursor);
                        treeEdgeLength = this.GetEdgeLength(childNode.Edge);
                    }
                } while (true);

                return unmatchedEdgeCursor == -1;
            }

            private SuffixNode TraverseEdge(SuffixNode parent, int matchingStart)
            {
                SuffixNode child = parent.GetEdge(this.GetCharFromIndex(matchingStart));
                if (child != null)
                {
                    return child;
                }
                else throw new ArgumentException("invalid argument", "matchingStart");
            }

            private void FixupLeaves(SuffixNode p, int endIndex)
            {
                if (p.IsLeaf)
                {
                    p.Edge.End = endIndex;                    
                }
                else foreach (var child in p.Children)
                {
                    this.FixupLeaves(child.Value as SuffixNode, endIndex);
                }                
            }

            #endregion

            #region Nested Classes

            private class SuffixNode : ISuffixNode
            {
                #region Fields

                private ChildrenCollectionType _children;

                #endregion

                #region Properties

                public EdgeLabel Edge
                { get; set; }

                public SuffixNode Link
                { get; set; }

                public SuffixNode Parent
                { get; set; }

                public bool IsLeaf
                { get; set; }

                public int LeafNumber
                { get; set; }

                public IDictionary<char, ISuffixNode> Children
                {
                    get
                    {
                        if (this._children == null)
                        {
                            this._children = new ChildrenCollectionType();
                        }

                        return this._children;
                    }
                }

                #endregion

                #region Public Methods

                public SuffixNode GetEdge(char startChar)
                {
                    ISuffixNode child = null;
                    if (this.Children.TryGetValue(startChar, out child))
                    {
                        return (SuffixNode)child;
                    }
                    else return null;                    
                }

                public SuffixNode AddEdge(string text, int startCharIndex, int endCharIndex)
                {
                    Debug.Assert(endCharIndex == -1 || (0 <= startCharIndex && startCharIndex <= endCharIndex && endCharIndex < text.Length));
                    Debug.Assert(!this.Children.ContainsKey(text[startCharIndex]));

                    var newChild = new SuffixNode()
                    {
                        Edge = EdgeLabel.Create(startCharIndex, endCharIndex),
                        Parent = this,
                        Link = null,
                        IsLeaf = false
                    };

                    this.Children.Add(text[startCharIndex], newChild);
                    return newChild;
                }

                public void AddEdge(string text, SuffixNode newChild)
                {
                    this.Children.Add(text[newChild.Edge.Start], newChild);
                }

                public SuffixNode RemoveEdge(char startChar)
                {
                    var removedNode = this.GetEdge(startChar);
                    if (removedNode != null)
                    {
                        this.Children.Remove(startChar);
                    }

                    return removedNode;
                }

                public void SetLeaf(int leafNumber)
                {
                    this.IsLeaf = true;
                    this.LeafNumber = leafNumber;
                }

                #endregion

                #region ISuffixNode implementation

                ISuffixNode ISuffixNode.Link
                {
                    get { return this.Link; }
                }

                ISuffixNode ISuffixNode.Parent
                {
                    get { return this.Parent; }
                }

                IEdgeLabel ISuffixNode.Edge
                {
                    get { return this.Edge; }
                }

                #endregion
            }

            /// <summary>
            /// represents a text fragment (i.e. consecutive sequence of characters)
            /// </summary>
            private class EdgeLabel : IEdgeLabel
            {
                public int Start 
                { get; set; }

                public int End 
                { get; set; }

                public static EdgeLabel Create(int s, int e)
                {
                    if (s < 0 || e < -1 || (e != -1 && s > e))
                    {
                        throw new ArgumentOutOfRangeException("s");
                    }

                    return new EdgeLabel() { Start = s, End = e };
                }
            }

            #endregion
        }

        private class TextMatcher
        {            
            #region C'tor
            
            public TextMatcher(ISuffixTree suffixTree)
            {
                this.Tree = suffixTree;
            }

            #endregion

            #region Properties

            private ISuffixTree Tree { get; set; } 

            #endregion

            #region Public Methods

            /// <summary>
            /// Searches for the specified text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="matchingList">The matching list.</param>
            /// <param name="child">The child.</param>
            /// <param name="unmatchedEdgeCursor">First index of the unmached edge.</param>
            /// <returns>number of characters successfully matched</returns>
            public MatchResult Match(string text)
            {
                if (string.IsNullOrEmpty(text)){
                    return null;
                }

                var matchResult = new MatchResult(){
                    MatchedCount = 0,
                    InternNode = null,
                    UnmatchedEdgeCursor = -1,
                    MatchingSuffixes = null,
                };
                
                var parent = this.Tree.Root;
                int length = text.Length;

                int k = 0, edgeCursor = -1;
                ISuffixNode child = null;

                while (parent.Children.TryGetValue(text[k], out child))
                {
                    int edgeEnd = child.Edge.End;
                    edgeCursor = child.Edge.Start;

                    while (k < length && edgeCursor <= edgeEnd && text[k++] == this.Tree.Text[edgeCursor++]) { }

                    if (k < length && edgeCursor > edgeEnd){                        
                        parent = child;
                    }
                    else break;
                }

                if (k > 0)
                {
                    matchResult.InternNode = child;

                    var agreedOnLastChar = text[k - 1] == this.Tree.Text[edgeCursor - 1];
                    var stack = new Stack<ISuffixNode>();
                    stack.Push(child);

                    while (stack.Count > 0)
                    {
                        var p = stack.Pop();

                        if (p.IsLeaf)
                        {
                            if (matchResult.MatchingSuffixes == null){
                                matchResult.MatchingSuffixes = new List<ISuffixNode>();
                            }
                            
                            matchResult.MatchingSuffixes.Add(p);                            
                        }
                        else foreach (var c in p.Children)
                        {
                            stack.Push(c.Value);
                        }
                    }

                    if (!agreedOnLastChar)
                    {
                        matchResult.UnmatchedEdgeCursor = edgeCursor - 1;
                        matchResult.MatchedCount = k - 1;
                    }
                    else
                    {
                        matchResult.MatchedCount = k;
                    }
                }

                return matchResult;
            }

            #endregion

            #region Nested Classes

            public class MatchResult
            {
                public int MatchedCount { get; set; }
                public List<ISuffixNode> MatchingSuffixes { get; set; }
                public ISuffixNode InternNode { get; set; }
                public int UnmatchedEdgeCursor { get; set; }
            }

            #endregion
        }

        #endregion
    }    

    public static class SuffixTreeExtensions
    {
        public static string GetNodeSuffix(this ISuffixTree tree, ISuffixNode p)
        {
            var sb = new StringBuilder();
            GetNodeSuffixImpl(tree, p, sb);
            return sb.ToString();
        }

        private static void GetNodeSuffixImpl(ISuffixTree t, ISuffixNode p, StringBuilder sb)
        {
            if (p.Parent != null)
            {
                GetNodeSuffixImpl(t, p.Parent, sb);

                int length = (p.IsLeaf ? t.Text.Length - 1 : p.Edge.End) - p.Edge.Start + 1;
                sb.Append(t.Text.Substring(p.Edge.Start, length));
            }
        }
    }
}
