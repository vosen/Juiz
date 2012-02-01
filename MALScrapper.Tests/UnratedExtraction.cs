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
    public class UnratedExtraction
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
            var result = Extract.AllAnime("");
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Unknown, result.Response);
        }

        [Test]
        public void MySQLError()
        {
            string site = LoadFile(@"sites\animelist\JamesBennitDiver.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.MySQLError, result.Response);
        }

        [Test]
        public void InvalidUser()
        {
            string site = LoadFile(@"sites\animelist\gesla_jazn.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.InvalidUsername, result.Response);
        }

        [Test]
        public void PrivateList()
        {
            string site = LoadFile(@"sites\animelist\SoiFong.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.ListIsPrivate, result.Response);
        }

        [Test]
        public void ListTooLarge()
        {
            string site = LoadFile(@"sites\animelist\DarkLaila.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.TooLarge, result.Response);
        }

        [Test]
        public void EmptyList()
        {
            string site = LoadFile(@"sites\animelist\aaroncaberte.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(0, result.Ratings.Count);
        }

        [Test]
        public void UnscoredList()
        {
            string site = LoadFile(@"sites\animelist\htiek359.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(4, result.Ratings.Count);
        }

        [Test]
        public void ShortList()
        {
            string site = LoadFile(@"sites\animelist\Ningx.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(42, result.Ratings.Count);
        }

        [Test]
        public void CurrentlyWatchingList()
        {
            string site = LoadFile(@"sites\animelist\Aokaado&status=1.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(29, result.Ratings.Count);
        }

        [Test]
        public void CompletedList()
        {
            string site = LoadFile(@"sites\animelist\Aokaado&status=2.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(126, result.Ratings.Count);
        }

        [Test]
        public void OnHoldList()
        {
            string site = LoadFile(@"sites\animelist\Aokaado&status=3.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(49, result.Ratings.Count);
        }

        [Test]
        public void DroppedList()
        {
            string site = LoadFile(@"sites\animelist\Aokaado&status=4.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(44, result.Ratings.Count);
        }

        [Test]
        public void PlanToWatchList()
        {
            string site = LoadFile(@"sites\animelist\Aokaado&status=6.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(34, result.Ratings.Count);
        }

        [Test]
        public void LongList()
        {
            string site = LoadFile(@"sites\animelist\Aokaado.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(282, result.Ratings.Count);
        }

        [Test]
        public void ComplexList()
        {
            string site = LoadFile(@"sites\animelist\Alfyan.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(ExtractionResultType.Successs, result.Response);
            Assert.AreEqual(330, result.Ratings.Count);
        }
    }
}
