using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading.Tasks;

namespace MALContent
{
    public static class Extract
    {
        private static Regex trimWhitespace = new Regex(@"\s+", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex captureRating = new Regex(@"http://myanimelist\.net/anime/(?<id>[0-9]+?)/", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public static ExtractionResult DownloadRatedAnime(string name)
        {
            string site;
            using (var client = new ScrappingWebClient())
            {
                site = client.DownloadString("http://myanimelist.net/animelist/" + name + "&status=7");
            }
            var ratingTuple = ExtractionCore(site);
            if (ratingTuple.Item1 == ExtractionResultType.TooLarge)
                return DownloadRatedAnimeFromSublists(name);
            return RatedAnimeCore(ratingTuple);
        }

        private static ExtractionResult DownloadRatedAnimeFromSublists(string name)
        {
            ScrappingWebClient watchingList = new ScrappingWebClient();
            Task<string> watchingTask = watchingList.DownloadStringTask("http://myanimelist.net/animelist/" + name + "&status=1");
            ScrappingWebClient completedList = new ScrappingWebClient();
            Task<string> completedTask = completedList.DownloadStringTask("http://myanimelist.net/animelist/" + name + "&status=2");
            ScrappingWebClient onHoldList = new ScrappingWebClient();
            Task<string> onHoldTask = onHoldList.DownloadStringTask("http://myanimelist.net/animelist/" + name + "&status=3");
            ScrappingWebClient droppedList = new ScrappingWebClient();
            Task<string> droppedTask = droppedList.DownloadStringTask("http://myanimelist.net/animelist/" + name + "&status=4");
            Task<string>[] tasks = new Task<string>[] { watchingTask, completedTask, onHoldTask, droppedTask };
            Task.WaitAll(tasks);
            watchingList.Dispose();
            completedList.Dispose();
            onHoldList.Dispose();
            droppedList.Dispose();
            var allRatedAnime = tasks.Select(task => RatedAnime(task.Result))
                                     .Where(result => result.Response == ExtractionResultType.Successs)
                                     .SelectMany(i => i.Ratings);
            return new ExtractionResult() { Response = ExtractionResultType.Successs, Ratings = allRatedAnime.ToList() };
        }

        public static ExtractionResult RatedAnime(string site)
        {
            Tuple<ExtractionResultType, IEnumerable<AnimeRating>> parseResult = ExtractionCore(site);
            return RatedAnimeCore(parseResult);
        }

        private static ExtractionResult RatedAnimeCore(Tuple<ExtractionResultType, IEnumerable<AnimeRating>> parseResult)
        {
            if (parseResult.Item1 == ExtractionResultType.Successs)
                return new ExtractionResult() { Response = parseResult.Item1, Ratings = parseResult.Item2.Where(anime => anime.rating > 0).ToList() };
            return new ExtractionResult() { Response = parseResult.Item1 };
        }

        public static ExtractionResult DownloadAlldAnime(string name)
        {
            string site;
            using (var client = new ScrappingWebClient())
            {
                site = client.DownloadString("http://myanimelist.net/animelist/" + name + "&status=7");
            }
            var ratingTuple = ExtractionCore(site);
            if (ratingTuple.Item1 == ExtractionResultType.TooLarge)
                return DownloadAllAnimeFromSublists(name);
            return AllAnimeCore(ratingTuple);
        }

        private static ExtractionResult DownloadAllAnimeFromSublists(string name)
        {
            ScrappingWebClient watchingList = new ScrappingWebClient();
            Task<string> watchingTask = watchingList.DownloadStringTask("http://myanimelist.net/animelist/" + name + "&status=1");
            ScrappingWebClient completedList = new ScrappingWebClient();
            Task<string> completedTask = completedList.DownloadStringTask("http://myanimelist.net/animelist/" + name + "&status=2");
            ScrappingWebClient onHoldList = new ScrappingWebClient();
            Task<string> onHoldTask = onHoldList.DownloadStringTask("http://myanimelist.net/animelist/" + name + "&status=3");
            ScrappingWebClient droppedList = new ScrappingWebClient();
            Task<string> droppedTask = droppedList.DownloadStringTask("http://myanimelist.net/animelist/" + name + "&status=4");
            ScrappingWebClient plannedList = new ScrappingWebClient();
            Task<string> plannedTask = plannedList.DownloadStringTask("http://myanimelist.net/animelist/" + name + "&status=6");
            Task<string>[] tasks = new Task<string>[] { watchingTask, completedTask, onHoldTask, droppedTask, plannedTask };
            Task.WaitAll(tasks);
            watchingList.Dispose();
            completedList.Dispose();
            onHoldList.Dispose();
            droppedList.Dispose();
            var allAnime = tasks.Select(task => AllAnime(task.Result))
                                .Where(result => result.Response == ExtractionResultType.Successs)
                                .SelectMany(i => i.Ratings);
            return new ExtractionResult() { Response = ExtractionResultType.Successs, Ratings = allAnime.ToList() };
        }

        public static ExtractionResult AllAnime(string site)
        {
            Tuple<ExtractionResultType, IEnumerable<AnimeRating>> parseResult = ExtractionCore(site);
            return AllAnimeCore(parseResult);
        }

        private static ExtractionResult AllAnimeCore(Tuple<ExtractionResultType, IEnumerable<AnimeRating>> parseResult)
        {
            if (parseResult.Item1 == ExtractionResultType.Successs)
                return new ExtractionResult() { Response = parseResult.Item1, Ratings = parseResult.Item2.ToList() };
            return new ExtractionResult() { Response = parseResult.Item1 };
        }

        private static Tuple<ExtractionResultType, IEnumerable<AnimeRating>> ExtractionCore(string site)
        {
            if (site.Contains("There was a MySQL Error."))
            {
                return new Tuple<ExtractionResultType, IEnumerable<AnimeRating>>(ExtractionResultType.MySQLError, null);
            }

            if (site.Contains("Invalid Username Supplied"))
            {
                return new Tuple<ExtractionResultType, IEnumerable<AnimeRating>>(ExtractionResultType.InvalidUsername, null);
            }

            if (site.Contains("This list has been made private by the owner."))
            {
                return new Tuple<ExtractionResultType, IEnumerable<AnimeRating>>(ExtractionResultType.ListIsPrivate, null);
            }

            if (site.Contains("\"All Anime\" is disabled for lists with greater than 1500 anime entries."))
            {
                return new Tuple<ExtractionResultType, IEnumerable<AnimeRating>>(ExtractionResultType.TooLarge, null);
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(site);
            HtmlNode tableNode = doc.GetElementbyId("list_surround");
            if (tableNode == null)
                return new Tuple<ExtractionResultType, IEnumerable<AnimeRating>>(ExtractionResultType.Unknown, null);
            var mainIndices = FindTitleRatingIndices(tableNode);
            // check for people who don't put ratings on their profiles
            if (mainIndices == null)
            {
                return new Tuple<ExtractionResultType, IEnumerable<AnimeRating>>(ExtractionResultType.Successs, new AnimeRating[0]);
            }
            // collect ratings
            var ratings = tableNode.ChildNodes
                .Where(n => n.Name == "table" && !n.Attributes.Contains("class"))
                .Select(n => ExtractPayload(n, mainIndices.Item1, mainIndices.Item2))
                .Where(t => t != null)
                .Select(t => ParseRatings(t.Item1, t.Item2));
            // return our findings
            return new Tuple<ExtractionResultType, IEnumerable<AnimeRating>>(ExtractionResultType.Successs, ratings);
        }

        private static Tuple<int, int> FindTitleRatingIndices(HtmlNode outerNode)
        {
            var headNodes = outerNode.ChildNodes.Select(ExtractHeadCells).FirstOrDefault(e => e != null);
            if (headNodes == null)
                return null;
            int title = -1;
            int rating = -1;
            // we've got <td> nodes containing titles
            for (int i = 0; i < headNodes.Count; i++)
            {
                // strip whitespace
                string innerText = trimWhitespace.Replace(headNodes[i].InnerText, " ");
                if (innerText == "Anime Title")
                {
                    title = i;
                }
                else
                {
                    // look for a <strong> child
                    var strongNode = headNodes[i].ChildNodes.FirstOrDefault(n => n.Name == "strong");
                    if (strongNode != null && strongNode.InnerText == "Score")
                        rating = i;
                }
                if (title != -1 && rating != -1)
                    return Tuple.Create(title, rating);
            }
            return null;
        }

        private static IList<HtmlNode> ExtractHeadCells(HtmlNode node)
        {
            if (node.Name != "table")
                return null;
            var row = node.ChildNodes.FirstOrDefault(n => n.Name == "tr");
            if (row == null)
                return null;
            var headNodes = row.ChildNodes.Where(IsHeadCell).ToList();
            if (headNodes.Count == 0)
                return null;
            return headNodes;
        }

        private static bool IsHeadCell(HtmlNode node)
        {
            if (node.Name != "td")
                return false;
            var headerAttrib = node.Attributes["class"];
            if (headerAttrib == null)
                return false;
            return headerAttrib.Value == "table_header";
        }

        private static Tuple<HtmlNode, HtmlNode> ExtractPayload(HtmlNode tableNode, int titleIndex, int ratingIndex)
        {
            var row = tableNode.Element("tr");
            if (row == null)
                return null;
            var cells = row.Elements("td").ToList();
            if (cells.Count < 2)
                return null;
            var linkNode = cells[titleIndex].ChildNodes.FirstOrDefault(n => n.Name == "a");
            if (linkNode == null)
                return null;
            var linkNodeClass = linkNode.Attributes["class"];
            if (linkNodeClass == null || linkNodeClass.Value != "animetitle")
                return null;
            return Tuple.Create(linkNode, cells[ratingIndex]);
        }

        private static AnimeRating ParseRatings(HtmlNode animeLink, HtmlNode ratingCell)
        {
            int id = Int32.Parse(captureRating.Match(animeLink.Attributes["href"].Value).Groups["id"].Captures[0].Value);
            byte rating;
            if (ratingCell.InnerText != null && Byte.TryParse(ratingCell.InnerText, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out rating))
            {
                return new AnimeRating(id, rating);
            }
            return new AnimeRating(id, 0);
        }
    }
}
