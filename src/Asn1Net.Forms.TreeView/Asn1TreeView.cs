/*
 *  Copyright 2012-2016 The Asn1Net Project
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */
/*
 *  Written for the Asn1Net project by:
 *  Peter Polacko <peter.polacko+asn1net@gmail.com>
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Net.Asn1.Reader;

namespace Net.Asn1.Forms.TreeView
{
    /// <summary>
    /// User control for visualizing ASN.1 objects.
    /// </summary>
    public class Asn1TreeView : System.Windows.Forms.TreeView
    {
        private bool _EncapsulatedDataParsing = true;
        /// <summary>
        /// Flag indicating if encapsulated data from value of a Primitive ASN.1 node should be parsed.
        /// </summary>
        [Description("Set to True to parse encapsulated data located in value of Primitive ASN.1 node.")]
        [Category("ASN1 Behavior")]
        [Bindable(BindableSupport.Yes)]
        [DefaultValue(true)]
        public bool EncapsulatedDataParsing
        {
            get { return _EncapsulatedDataParsing; }
            set { _EncapsulatedDataParsing = value; }
        }

        private bool _ReadContent = true;
        /// <summary>
        /// Flag indicating if value of Primitive ASN.1 node should be read.
        /// </summary>
        [Description("Set to True to read content of ASN.1 node when parsing structure of ASN.1 object.")]
        [Category("ASN1 Behavior")]
        [Bindable(BindableSupport.Yes)]
        [DefaultValue(true)]
        public bool ReadContent
        {
            get { return _ReadContent; }
            set { _ReadContent = value; }
        }

        /// <summary>
        /// Load ASN.1 object in form of a stream, parse it and display its structure.
        /// </summary>
        /// <param name="asn1Content">ASN.1 object to parse.</param>
        public void LoadContent(Stream asn1Content)
        {
            // clear any existing nodes
            Nodes.Clear();

            // use parser to get the structure
            using (var reader = new BerReader(asn1Content))
            {
                var rootNode = new InternalNode();
                try
                {
                    // read whole object. This may fail if there is no valid ASN.1 object in the stream.
                    rootNode = reader.ReadToEnd(ReadContent);
                }
                catch (Exception ex)
                {
                    // something went wrong when reading file. Possibly it was not an ASN.1 object. 
                    Trace.WriteLine(String.Format("Parsing exception: {0}", ex));

                    var safetyNode = new Asn1TreeNode(null, "Invalid ASN.1 structure.");
                    Nodes.Add(safetyNode);
                }

                // reader does not parse encapsulated data. Do it manually.
                foreach (InternalNode internalNode in rootNode.ChildNodes)
                {
                    //  This will enrich list of nodes with additional nodes parsed out of values of Primitive ASN.1 nodes
                    if (EncapsulatedDataParsing)
                        ParseEncapsulatedData(internalNode);

                    // build tree from list of ASN.1 nodes
                    var rootTreeNode = new Asn1TreeNode(internalNode);
                    MakeTreeNode(rootTreeNode, internalNode, 1, EncapsulatedDataParsing);
                    Nodes.Add(rootTreeNode);
                }
            }

            // expand tree
            ExpandAll();
            SelectedNode = Nodes[0];
        }

        /// <summary>
        /// Recursively go through internal node and make corresponding structure of Asn1TreeNodes.
        /// </summary>
        /// <param name="treeNode">Node whose children will be filled by this method.</param>
        /// <param name="node">Internal node that will be processed to get tree of Asn1TreeNodes.</param>
        /// <param name="depth">Current depth of processing.</param>
        /// <param name="parseEncapsulatedData">Flag if encapsulated data should be parsed as well.</param>
        internal static void MakeTreeNode(TreeNode treeNode, InternalNode node, int depth, bool parseEncapsulatedData)
        {
            // count new depth if node has children
            int innerdepth = (node.ChildNodes.Count > 0) ? depth + 1 : depth;

            // recursively go through children and print structure of them
            foreach (InternalNode innerNode in node.ChildNodes)
            {
                if (parseEncapsulatedData)
                    ParseEncapsulatedData(innerNode);

                try
                {
                    var innerTreeNode = new Asn1TreeNode(innerNode);
                    treeNode.Nodes.Add(innerTreeNode);
                    MakeTreeNode(innerTreeNode, innerNode, innerdepth, parseEncapsulatedData);
                }
                catch (Exception)
                {
                    ;
                }
            }
        }

        /// <summary>
        /// Method will parse value of given node for existing ASN.1 objects.
        /// </summary>
        /// <param name="innerNode">Node to parse.</param>
        internal static void ParseEncapsulatedData(InternalNode innerNode)
        {
            if (innerNode.NodeType != Asn1NodeType.Primitive) return;
            if (innerNode.RawValue == null) return; // nothing to parse

            // for now, only BIT STRING or OCTET STRING may contain encapsulated data
            if (innerNode.Identifier.Tag != Asn1Type.BitString && innerNode.Identifier.Tag != Asn1Type.OctetString)
                return;


            byte[] dataToParse = innerNode.RawValue;
            if (innerNode.Identifier.Tag == Asn1Type.BitString)
                dataToParse = innerNode.ReadContentAsBitString();

            // Reader will close the stream when it is done parsing
            var memStream = new MemoryStream(dataToParse);

            using (var innerReader = new BerReader(memStream))
            {
                try
                {
                    InternalNode innerRootNode = innerReader.ReadToEnd(true);
                    innerNode.ChildNodes.AddRange(innerRootNode.ChildNodes);
                }
                catch (Exception)
                {
                    // nothing to do. Value of primitive node did not contain valid ASN.1 structure.
                }
            }
        }
    }
}