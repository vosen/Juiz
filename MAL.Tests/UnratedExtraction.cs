using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vosen.MAL.Content;
using System.IO;

namespace Vosen.MAL.Tests
{
    [TestFixture]
    public class UnratedExtraction
    {
        [Test]
        public void EmptySite()
        {
            var result = Extract.AllAnime("");
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Unknown, result.Response);
        }

        [Test]
        public void MySQLError()
        {
            string site = Helper.LoadFile(@"sites\animelist\JamesBennitDiver.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.MySQLError, result.Response);
        }

        [Test]
        public void InvalidUser()
        {
            string site = Helper.LoadFile(@"sites\animelist\gesla_jazn.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.InvalidUsername, result.Response);
        }

        [Test]
        public void PrivateList()
        {
            string site = Helper.LoadFile(@"sites\animelist\SoiFong.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.ListIsPrivate, result.Response);
        }

        [Test]
        public void ListTooLarge()
        {
            string site = Helper.LoadFile(@"sites\animelist\DarkLaila.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.TooLarge, result.Response);
        }

        [Test]
        public void EmptyList()
        {
            string site = Helper.LoadFile(@"sites\animelist\aaroncaberte.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(0, result.Ratings.Count);
        }

        [Test]
        public void UnscoredList()
        {
            string site = Helper.LoadFile(@"sites\animelist\htiek359.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(4, result.Ratings.Count);
        }

        [Test]
        public void ShortList()
        {
            string site = Helper.LoadFile(@"sites\animelist\Ningx.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(37, result.Ratings.Count);
        }

        [Test]
        public void CurrentlyWatchingList()
        {
            string site = Helper.LoadFile(@"sites\animelist\Aokaado&status=1.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(29, result.Ratings.Count);
        }

        [Test]
        public void CompletedList()
        {
            string site = Helper.LoadFile(@"sites\animelist\Aokaado&status=2.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(126, result.Ratings.Count);
        }

        [Test]
        public void OnHoldList()
        {
            string site = Helper.LoadFile(@"sites\animelist\Aokaado&status=3.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(49, result.Ratings.Count);
        }

        [Test]
        public void DroppedList()
        {
            string site = Helper.LoadFile(@"sites\animelist\Aokaado&status=4.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(44, result.Ratings.Count);
        }

        [Test]
        public void PlanToWatchList()
        {
            string site = Helper.LoadFile(@"sites\animelist\Aokaado&status=6.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(0, result.Ratings.Count);
        }

        [Test]
        public void LongList()
        {
            string site = Helper.LoadFile(@"sites\animelist\Aokaado.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(248, result.Ratings.Count);
        }

        [Test]
        public void ComplexList()
        {
            string site = Helper.LoadFile(@"sites\animelist\Alfyan.html");
            var result = Extract.AllAnime(site);
            Assert.IsNotNull(result);
            Assert.AreEqual(AnimelistResponse.Successs, result.Response);
            Assert.AreEqual(195, result.Ratings.Count);
        }
    }
}
