/////////////////////////////////////////////////////////////////////
//      File Name          : SuffixTree.Test.cs
//      Created            : 11 9 2013   21:45
//      Author             : Costin S
//
/////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TextIndexing;
using System.IO;

namespace TextIndexing.Test
{
    using Dictionary = Dictionary<char, ISuffixNode>;
    using SortedList = SortedList<char, ISuffixNode>;
    using SortedDictionary = SortedDictionary<char, ISuffixNode>;
    
    public sealed class SuffixTree_Test
    {
        #region Enums

        public enum TraceLevel
        {
            BuildTime = 0x1,
            Lite = 0x2,
            Verbose = 0x4,
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// creates the suffix tree for the specified text, verifies suffix tree is correct, writes the output to the console..
        /// </summary>
        /// <param name="text"></param>
        public static void Run_Console(string text, TraceLevel traceLevel)
        {
            if (string.IsNullOrEmpty(text) || text[text.Length - 1] != '$') 
                throw new ArgumentException("argument must not be null or empty; argument must end with terminal char '$'..");

            Debug.AutoFlush = true;            
            Debug.Listeners.Add(new TextWriterTraceListener(System.Console.Out));

            CreateSuffixTree(text, traceLevel);
            Debug.Listeners.Clear();
        }

        public static void Run_File(string text, string outputFile, TraceLevel traceLevel)
        {
            if (string.IsNullOrEmpty(text) || text[text.Length - 1] != '$')
                throw new ArgumentException("argument must not be null or empty; argument must end with terminal char '$'..");

            if (File.Exists(outputFile)){
                File.Delete(outputFile);
            }

            Debug.AutoFlush = true;            
            Debug.Listeners.Add(new TextWriterTraceListener(File.CreateText(outputFile)));

            CreateSuffixTree(text, traceLevel);
            Debug.Listeners.Clear();
        }

        /// <summary>
        /// generates random strings and builds associated suffix trees. verifies suffix trees are correct. writes any output to the file given as argument..
        /// NOTE: file is deleted if already exists!!
        /// </summary>
        public static void Run_Random(string outputFile, TraceLevel traceLevel)
        {
            if (File.Exists(outputFile)){
                File.Delete(outputFile);
            }

            Debug.AutoFlush = true;                        
            Debug.Listeners.Add(new TextWriterTraceListener(File.CreateText(outputFile)));

            // create 100 strings, containing a random sequence of {a, b} characters, of random length <= 2500
            CreateSuffixTrees(GenerateRandomStrings(20, new Tuple<int, int>(97, 98), 0, 2500), traceLevel);

            // create 100 strings, containing a random sequence of {a, b, c, d} characters, of random length <= 2500   
            CreateSuffixTrees(GenerateRandomStrings(20, new Tuple<int, int>(97, 100), 0, 2500), traceLevel);

            // create 500 strings, containing a random sequence of {a, b, c, d, e, f} characters, of length = 10000
            CreateSuffixTrees(GenerateRandomStrings(3, new Tuple<int, int>(97, 102), 10000, 10000), traceLevel);

            Debug.Listeners.Clear();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// generates <stringCount> number of strings, 
        /// each string has a random length greater between <minStringLength> and <maxStringLength>, 
        /// each string contains a random sequence of printable ascii characters in the range <charRange>, 
        /// each string ends with the unique char '$'
        /// </summary>
        /// <param name="stringCount">number of strings and suffix tree to generate and test</param>
        /// <param name="charRange">valid ascii char range is [32, 127]</param>
        /// <param name="minStringLength">minimum number for all generated strings</param>
        /// <param name="maxStringLength">maximum length for all generated strings</param>
        private static IEnumerable<string> GenerateRandomStrings(int stringCount, Tuple<int, int> charRange, int minStringLength, int maxStringLength)
        {
            if (charRange.Item1 < 32 || charRange.Item2 > 127)
                throw new ArgumentOutOfRangeException("charRange", "valid printable ascii char range is [32, 127]");

            if (minStringLength < 0 || maxStringLength <= 0 || maxStringLength < minStringLength)
                throw new ArgumentOutOfRangeException("max/min__StringLength", "minStringLength and maxStringLength should be positive, maxStringLength must be non-zero and greater or equal to minStringLength");

            var random = new Random();
            for (int k = 0; k < stringCount; k++)
            {
                int length = random.Next(minStringLength, maxStringLength);

                var builder = new StringBuilder(length+1);
                for (int i = 0; i < length; i++)
                {
                    char newChar;
                    while ((newChar = (char)random.Next(charRange.Item1, charRange.Item2 + 1)) == '$') ;

                    builder.Append(newChar);
                }
                builder.Append('$');

                yield return builder.ToString();
            }
        }

        private static void CreateSuffixTree(string text, TraceLevel traceLevel)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= 1)
                throw new ArgumentException("text cannot be null, empty, or have a length less than 1");

            Stopwatch stopWatch = new Stopwatch();
            Console.Write(".");

            if(traceLevel >= TraceLevel.Lite){
                Debug.WriteLine(""); Debug.WriteLine(""); Debug.WriteLine("");
                Debug.WriteLine("================================================================================================= ");
                Debug.WriteLine(text); Debug.WriteLine(""); Debug.WriteLine("text length: {0}", text.Length);
            } else if(traceLevel >= TraceLevel.BuildTime){
                Debug.WriteLine("================================================================================================= ");
                Debug.WriteLine(""); Debug.WriteLine("text length: {0}", text.Length);
            }                        

            stopWatch.Start();
            var suffixTree = SuffixTree.Create<SortedList>(text);
            stopWatch.Stop();

            if(traceLevel >= TraceLevel.BuildTime){
                Debug.WriteLine("total milliseconds: {0}", stopWatch.ElapsedMilliseconds); Debug.WriteLine("");
            }

            // diagnose..
            var diag = SuffixTreeDiagnostic.create(suffixTree);
            diag.display(traceLevel);
        }

        private static void CreateSuffixTrees(IEnumerable<string> texts, TraceLevel traceLevel)
        {
            if (texts != null){
                foreach (var text in texts)
                    CreateSuffixTree(text, traceLevel);
            }
        }        

        #endregion

        #region Nested Classes

        class SuffixTreeDiagnostic
        {
            #region Fields

            private SortedDictionary<int, ISuffixNode> suffixNodes = new SortedDictionary<int, ISuffixNode>();
            private List<ISuffixNode> internalNodes = new List<ISuffixNode>();

            #endregion

            #region Enums

            [Flags]
            public enum Info
            {
                Content = 0x1,
                SuffixLinks = 0x2,
                Suffixes = 0x4
            }

            #endregion

            #region C'tor

            private SuffixTreeDiagnostic() { }

            #endregion

            #region Properties

            private ISuffixTree Tree { get; set; }

            #endregion

            #region Public Methods

            public static SuffixTreeDiagnostic create(ISuffixTree suffixTree)
            {
                if (suffixTree != null)
                {
                    var diag = new SuffixTreeDiagnostic() { Tree = suffixTree };
                    diag.build(suffixTree.Root);

                    return diag;
                }
                else throw new ArgumentNullException("tree");               
            }

            public void display(TraceLevel traceLevel)
            {
                if(traceLevel >= TraceLevel.Lite){
                    Debug.WriteLine("");
                    Debug.WriteLine("suffix links count: {0} ", internalNodes.Count);
                    Debug.WriteLine("----------------------- ");
                }

                if(traceLevel >= TraceLevel.Verbose){
                    display(Info.Content | Info.SuffixLinks);
                }

                if(traceLevel >= TraceLevel.Lite){
                    Debug.WriteLine("");
                    Debug.WriteLine("suffix count (leaves): {0}", suffixNodes.Count);
                    Debug.WriteLine("-------------------------- ");
                }

                if(traceLevel >= TraceLevel.Verbose){
                    display(Info.Content | Info.Suffixes);
                }
            }

            #endregion

            #region Private Methods

            private void verify(ISuffixNode suffixNode)
            {
                if (suffixNode != null && !suffixNode.IsLeaf && suffixNode != this.Tree.Root)
                {
                    Debug.Assert(suffixNode.Link != null);

                    var thisSuffix = this.Tree.GetNodeSuffix(suffixNode);
                    var linkSuffix = this.Tree.GetNodeSuffix(suffixNode.Link);

                    Debug.Assert(thisSuffix.Length - 1 == linkSuffix.Length);
                    Debug.Assert(thisSuffix.Substring(1).Equals(linkSuffix));
                }
            }

            private void build(ISuffixNode suffixNode)
            {
                if (suffixNode != null)
                {
                    if (suffixNode.IsLeaf)
                    {
                        suffixNodes.Add(suffixNode.LeafNumber, suffixNode);
                    }
                    else
                    {
                        if (suffixNode.Parent != null)
                        {
                            internalNodes.Add(suffixNode);
                            verify(suffixNode);
                        }

                        foreach (var kvp in suffixNode.Children)
                        {
                            build(kvp.Value);
                        }
                    }
                }
            }

            private void display(Info info)
            {
                if (info.HasFlag(Info.SuffixLinks))
                {
                    foreach (var internalNode in internalNodes)
                    {
                        if (internalNode.Parent != null)
                        {
                            var thisSuffix = this.Tree.GetNodeSuffix(internalNode);
                            var linkSuffix = this.Tree.GetNodeSuffix(internalNode.Link);

                            if (info.HasFlag(Info.Content))
                            {
                                Debug.Write(thisSuffix); Debug.Write(" | "); Debug.WriteLine(linkSuffix);
                            }
                            else
                            {
                                Debug.Write(thisSuffix.Length.ToString()); Debug.Write(" | "); Debug.WriteLine(linkSuffix.Length.ToString());
                            }
                        }
                    }
                }

                if (info.HasFlag(Info.Suffixes))
                {
                    foreach (var leafNode in suffixNodes.Values)
                    {
                        var suffix = this.Tree.GetNodeSuffix(leafNode);
                        string format = string.Format("leaf node {{0, 3}} -- Sx={{1, {0}}}", this.Tree.Text.Length);
                        Debug.WriteLine(string.Format(format, leafNode.LeafNumber, info.HasFlag(Info.Content) ? suffix : suffix.Length.ToString()));
                    }
                }
            }

            #endregion
        }

        #endregion
    }    
}
