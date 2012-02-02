using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using Vosen.MAL.Content;

namespace Vosen.MAL.Tests
{
    [TestFixture]
    public class NameExtraction
    {
        private static string LoadFile(string path)
        {
            using (var stream = new StreamReader(path))
            {
                return stream.ReadToEnd();
            }
        }

        [Test]
        public void ExtractClubsPlain()
        {
            string site = LoadFile(@"sites\clubs\EinLawliet.html");
            var nameResult = Extract.NameFromClublist(site);
            Assert.AreEqual(NameResponse.Success, nameResult.Response);
            Assert.AreEqual("EinLawliet", nameResult.Name);
        }

        [Test]
        public void ExtractClubsEmpty()
        {
            string site = LoadFile(@"sites\clubs\DeusOmega.html");
            var nameResult = Extract.NameFromClublist(site);
            Assert.AreEqual(NameResponse.Success, nameResult.Response);
            Assert.AreEqual("DeusOmega", nameResult.Name);
        }

        [Test]
        public void ExtractClubsIvalid()
        {
            string site = LoadFile(@"sites\clubs\Invalid id.html");
            var nameResult = Extract.NameFromClublist(site);
            Assert.AreEqual(NameResponse.InvalidId, nameResult.Response);
        }

        [Test]
        public void ExtractClubsMalformed()
        {
            var nameResult = Extract.NameFromClublist("");
            Assert.AreEqual(NameResponse.Unknown, nameResult.Response);
        }
    }
}
