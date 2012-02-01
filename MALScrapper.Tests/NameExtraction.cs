using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using MALContent;

namespace MALScrapper.Tests
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
            string name = Extract.NameFromClublist(site);
            Assert.AreEqual("EinLawliet", name);
        }

        [Test]
        public void ExtractClubsEmpty()
        {
            string site = LoadFile(@"sites\clubs\DeusOmega.html");
            string name = Extract.NameFromClublist(site);
            Assert.AreEqual("DeusOmega", name);
        }

        [Test]
        public void ExtractClubsIvalid()
        {
            string site = LoadFile(@"sites\clubs\Invalid id.html");
            string name = Extract.NameFromClublist(site);
            Assert.AreEqual(null, name);
        }
    }
}
