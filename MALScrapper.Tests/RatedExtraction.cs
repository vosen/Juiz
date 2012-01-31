using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MALContent;
using System.IO;

namespace MALScrapper.Tests
{
    [TestFixture]
    public class RatedExtraction
    {
        private static string LoadFile(string path)
        {
            using(var stream = new StreamReader(path))
            {
                return stream.ReadToEnd();
            }
        }

        [Test]
        public void EmptySite()
        {
            var result = Extract.RatedAnime("");
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Unknown, result.Response);
        }

        [Test]
        public void MySQLError()
        {
            string site = LoadFile(@"sites\JamesBennitDiver.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.MySQLError, result.Response);
        }

        [Test]
        public void InvalidUser()
        {
            string site = LoadFile(@"sites\gesla_jazn.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.InvalidUsername, result.Response);
        }

        [Test]
        public void PrivateList()
        {
            string site = LoadFile(@"sites\SoiFong.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.ListIsPrivate, result.Response);
        }

        [Test]
        public void ListTooLarge()
        {
            string site = LoadFile(@"sites\DarkLaila.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.TooLarge, result.Response);
        }

        [Test]
        public void EmptyList()
        {
            string site = LoadFile(@"sites\aaroncaberte.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(0, result.Ratings.Count);
        }

        [Test]
        public void UnscoredList()
        {
            string site = LoadFile(@"sites\htiek359.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(0, result.Ratings.Count);
        }

        [Test]
        public void ShortList()
        {
            string site = LoadFile(@"sites\Ningx.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(2, result.Ratings.Count);
        }

        [Test]
        public void CurrentlyWatchingList()
        {
            string site = LoadFile(@"sites\Aokaado&status=1.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(14, result.Ratings.Count);
        }

        [Test]
        public void CompletedList()
        {
            string site = LoadFile(@"sites\Aokaado&status=2.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(123, result.Ratings.Count);
        }

        [Test]
        public void OnHoldList()
        {
            string site = LoadFile(@"sites\Aokaado&status=3.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(18, result.Ratings.Count);
        }

        [Test]
        public void DroppedList()
        {
            string site = LoadFile(@"sites\Aokaado&status=4.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(14, result.Ratings.Count);
        }

        [Test]
        public void PlanToWatchList()
        {
            string site = LoadFile(@"sites\Aokaado&status=6.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(0, result.Ratings.Count);
        }

        [Test]
        public void LongList()
        {
            string site = LoadFile(@"sites\Aokaado.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(169, result.Ratings.Count);
        }

        [Test]
        public void ComplexList()
        {
            string site = LoadFile(@"sites\Alfyan.html");
            var result = Extract.RatedAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(133, result.Ratings.Count);
        }
    }
}
