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

using System.IO;
using NUnit.Framework;

namespace Net.Asn1.Forms.TreeView.Tests
{
    /// <summary>
    ///     Basic test of BerReader functionality.
    /// </summary>
    [TestFixture]
    [Category("Basic")]
    public class BasicTests
    {
        /// <summary>
        ///     Test if BerReade will not parse given data when instantiated.
        /// </summary>
        [Test]
        [TestCase("asn1_github_certificate_chain.asn1")]
        [TestCase("github.cer")]
        public void ReadCertificateChain(string filename)
        {
            using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var userCtrl = new Asn1TreeView();
                userCtrl.LoadContent(fs);

                Assert.IsTrue(userCtrl.Nodes.Count != 0);

                var node = userCtrl.Nodes[0] as Asn1TreeNode;
                Assert.IsNotNull(node);
                Assert.IsTrue(node.Text != "Invalid ASN.1 structure.");
            }
        }
    }
}