/*
 *  Asn1Net.Forms.TreeView - User control for visualizing ASN.1 objects.
 *  Copyright (c) 2014-2015 Peter Polacko
 *  Author: Peter Polacko <peter.polacko+asn1net@gmail.com>
 *
 *  Licensing for open source projects:
 *  Asn1Net.Forms.TreeView is available under the terms of the GNU Affero General 
 *  Public License version 3 as published by the Free Software Foundation.
 *  Please see <http://www.gnu.org/licenses/agpl-3.0.html> for more details.
 *
 */

using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows.Forms;
using Net.Asn1.Reader;

namespace Net.Asn1.Forms.TreeView
{
    /// <summary>
    /// TreeNode class that holds an ASN.1 node information.
    /// </summary>
    [Serializable]
    public class Asn1TreeNode : TreeNode
    {
        /// <summary>
        /// ASN.1 node this class is representing
        /// </summary>
        private readonly InternalNode _node;

        /// <summary>
        /// Initializes a new instance of the Asn1TreeNode class using 
        /// the specified serialization information and context.
        /// </summary>
        /// <param name="info">The System.Runtime.Serialization.SerializationInfo that contains the data 
        /// to deserialize the class.</param>
        /// <param name="context">The System.Runtime.Serialization.StreamingContext that contains the source 
        /// and destination of the serialized stream.</param>
        protected Asn1TreeNode(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Asn1TreeNode class using 
        /// information from the specified internal ASN.1 node.
        /// </summary>
        /// <param name="node">ASN.1 node to visualize.</param>
        public Asn1TreeNode(InternalNode node)
            : base(CreateLabel(node), GetImageIndex(node), GetImageIndex(node))
        {
            _node = node;
        }

        /// <summary>
        /// Initializes a new instance of the Asn1TreeNode class using 
        /// information from the specified internal ASN.1 node and label text.
        /// </summary>
        /// <param name="node">ASN.1 node to visualize.</param>
        /// <param name="text">The label of the new tree node.</param>
        public Asn1TreeNode(InternalNode node, string text)
            : base(text)
        {
            _node = node;
        }

        /// <summary>
        /// Initializes a new instance of the Asn1TreeNode class with 
        /// the specified label text and images to display when the tree node is in a 
        /// selected and unselected state.
        /// </summary>
        /// <param name="node">ASN.1 node to visualize.</param>
        /// <param name="text">The label of the new tree node.</param>
        /// <param name="imageIndex">The index value of <see cref="System.Drawing.Image"/> to display when the tree node is unselected.</param>
        /// <param name="selectedImageIndex">The index value of <see cref="System.Drawing.Image"/> to display when the tree node is selected.</param>
        public Asn1TreeNode(InternalNode node, string text, int imageIndex, int selectedImageIndex)
            : base(text, imageIndex, selectedImageIndex)
        {
            _node = node;
        }

        /// <summary>
        /// Gets index value of <see cref="System.Drawing.Image"/> based on given ASN.1 node type.
        /// </summary>
        /// <param name="node">ASN.1 node for which an index of image should be found.</param>
        /// <returns>The index value of <see cref="System.Drawing.Image"/> to display.</returns>
        private static int GetImageIndex(InternalNode node)
        {
            if (node.Identifier.Class == Asn1Class.ContextSpecific)
                return 32;

            return (int) node.Identifier.Tag;
        }

        /// <summary>
        /// Makes label of the tree node based on given ASN.1 node.
        /// </summary>
        /// <param name="node">ASN.1 node from which label should be computed.</param>
        /// <returns>The label of the new tree node.</returns>
        private static string CreateLabel(InternalNode node)
        {
            string structure = PrintFriendlyText(node);
            if (node.RawValue != null && node.ChildNodes.Count == 0)
                // only value on the lowest level should be displayed
            {
                string data = PrintDataText(node);
                if (!string.IsNullOrEmpty(data))
                    structure = string.Format("{0} : {1}", structure, data);
            }

            return structure;
        }

        /// <summary>
        /// Prints data of selected ASN.1 node.
        /// </summary>
        /// <param name="node">ASN.1 node to read data from.</param>
        /// <returns>ASN.1 data as text.</returns>
        private static string PrintDataText(InternalNode node)
        {
            if (node.RawValue == null) throw new ArgumentNullException("node.RawValue");

            var stringValue = node.RawValue.ToHexString();

            // NOTE: treat as visible string
            var contextSpecificIA5StringTags = new[] {1, 2, 6};
            if (node.Identifier.Class == Asn1Class.ContextSpecific
                && contextSpecificIA5StringTags.Any(p => p == (int) node.Identifier.Tag))
                return node.ReadContentAsString();

            // append value of ASN.1 node
            if (node.NodeType == Asn1NodeType.Primitive && node.Identifier.Class != Asn1Class.ContextSpecific)
            {
                switch (node.Identifier.Tag)
                {
                    case Asn1Type.ObjectIdentifier:
                        stringValue = node.ReadContentAsObjectIdentifier();
                        break;
                    case Asn1Type.PrintableString:
                    case Asn1Type.Ia5String:
                    case Asn1Type.Utf8String:
                        stringValue = node.ReadContentAsString();
                        break;
                    case Asn1Type.GeneralizedTime:
                    case Asn1Type.UtcTime:
                        stringValue = node.ReadConventAsDateTimeOffset().ToString();
                        break;
                    case Asn1Type.Enumerated: // same as INTEGER
                    case Asn1Type.Integer:
                        stringValue = node.RawValue.ToHexString();
                        break;
                    case Asn1Type.Boolean:
                        stringValue = node.ReadContentAsBoolean().ToString();
                        break;
                    case Asn1Type.BitString:
                        stringValue = node.ReadContentAsBitString().ToHexString();
                        break;
                    case Asn1Type.Real:
                        stringValue = node.ReadContentAsReal().ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        stringValue = node.RawValue.ToHexString();
                        break;
                }
            }

            return stringValue;
        }

        /// <summary>
        /// Prints information about ASN.1 node in format: (offset,length) (tag)
        /// </summary>
        /// <param name="node">ASN.1 node to read from.</param>
        /// <returns>Information about ASN.1 node as text.</returns>
        private static string PrintFriendlyText(InternalNode node)
        {
            // print offset in source stream and length of ASN.1 node
            string offsetAndLength = String.Format("({0},{1})",
                node.StartPosition.ToString(CultureInfo.InvariantCulture),
                node.Length.ToString(CultureInfo.InvariantCulture));

            // append tag name
            string structure = String.Format("{0} {1}",
                offsetAndLength,
                (node.Identifier.Class == Asn1Class.ContextSpecific)
                    ? String.Format("{0} ({1})", node.Identifier.Class, (int) node.Identifier.Tag)
                    : node.Identifier.Tag.ToString());

            return structure;
        }
    }

    internal static class Extensions
    {
        /// <summary>
        /// Prints byte array as HEX string.
        /// </summary>
        /// <param name="ba">Byte array to process.</param>
        /// <returns>HEX string representation of byte array.</returns>
        internal static string ToHexString(this byte[] ba)
        {
            var hex = new StringBuilder(ba.Length*2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:X2}", b);
            return hex.ToString();
        }
    }
}