using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using System.IO;
using Vosen.MAL.Content;

namespace Vosen.MAL.Tests
{
    [TestFixture]
    public class AnimeNamesExtraction
    {
        [Test]
        public void TestEmpty()
        {
            AnimeResult result = Extract.AnimeNamesFromSite("");
            Assert.AreEqual(AnimeResponse.Unknown, result.Response);
        }

        [Test]
        public void TestSimple()
        {
            AnimeResult result = Extract.AnimeNamesFromSite(Helper.LoadFile(@"sites\anime\1.html"));
            Assert.AreEqual(AnimeResponse.Successs, result.Response);
            Assert.IsNotNull(result.Synonyms);
            Assert.AreEqual("Cowboy Bebop", result.RomajiName);
        }

        [Test]
        public void TestInvalid()
        {
            AnimeResult result = Extract.AnimeNamesFromSite(Helper.LoadFile(@"sites\anime\2.html"));
            Assert.AreEqual(AnimeResponse.InvalidId, result.Response);
        }

        [Test]
        public void TestComplex1()
        {
            AnimeResult result = Extract.AnimeNamesFromSite(Helper.LoadFile(@"sites\anime\100.html"));
            Assert.AreEqual(AnimeResponse.Successs, result.Response);
            Assert.IsNotNull(result.Synonyms);
            Assert.AreEqual("Prétear", result.RomajiName);
            Assert.AreEqual("Pretear", result.EnglishName);
            Assert.Contains("Shin Shirayuki Hime Densetsu Pretear", result.Synonyms.ToList());
        }

        [Test]
        public void TestComplex2()
        {
            AnimeResult result = Extract.AnimeNamesFromSite(Helper.LoadFile(@"sites\anime\112.html"));
            Assert.AreEqual(AnimeResponse.Successs, result.Response);
            Assert.IsNotNull(result.Synonyms);
            Assert.AreEqual("Cosprayers", result.RomajiName);
            Assert.AreEqual("The Cosmopolitan Prayers", result.EnglishName);
            Assert.AreEqual(3, result.Synonyms.Count);
            var synonyms = result.Synonyms.ToList();
            Assert.Contains("Chou Henshin Cos ? Prayer", synonyms);
            Assert.Contains("Super Transforming Cos ? Prayer", synonyms);
            Assert.Contains("Cho Henshin Cosprayers", synonyms);
        }

        [Test]
        public void TestEmptySynonyms()
        {
            AnimeResult result = Extract.AnimeNamesFromSite(Helper.LoadFile(@"sites\anime\985.html"));
            Assert.AreEqual(AnimeResponse.Successs, result.Response);
            Assert.IsNotNull(result.Synonyms);
            Assert.AreEqual("Dragon Ball Z Special 2: The History of Trunks", result.RomajiName);
            Assert.AreEqual("Dragon Ball Z Special 2: The History of Trunks", result.EnglishName);
            Assert.AreEqual(2, result.Synonyms.Count);
            var synonyms = result.Synonyms.ToList();
            Assert.Contains("Dragon Ball Z: Zetsubou e no Hankou!! Nokosareta Chousenshi - Gohan to Trunks", synonyms);
            Assert.Contains("Dragon Ball Z: Resist Despair!! The Surviving Fighters - Gohan and Trunks", synonyms);
        }
    }
}
