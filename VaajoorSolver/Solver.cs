using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace VaajoorSolver
{
    internal class Solver
    {
        /// <summary>
        /// Vaajoor API
        /// </summary>
        private const string api = "https://www.vaajoor.ir/api/check";

        /// <summary>
        /// It seems that the GameId value is an incremental counter starting from 2022-01-08
        /// </summary>
        private static DateTime _FIRST_GAME_ID_DATE = DateTime.Parse("2022-01-08");

        /// <summary>
        /// A regular Persian word regex pattern, containing all Persian chars
        /// </summary>
        private const string _wordPattern = "[آابپتثجچحخدذرزژسشصضطظعغفقکگلمنوهیئ]";

        /// <summary>
        /// After each request, we update the regex pattern based on api response and keep it here
        /// </summary>
        private static List<string> _updatedPattern = new();

        internal string PersianWordsFileName { get; }
        internal int WordCharsCount { get; }
        internal int GameId { get; }

        internal Solver(string persianWordsFileName, int wordCharsCount)
        {
            PersianWordsFileName = persianWordsFileName;
            WordCharsCount = wordCharsCount;

            // Current GameID calculation
            GameId = Convert.ToInt32((DateTime.Now - _FIRST_GAME_ID_DATE).TotalDays) - 1;
        }

        internal bool Solve(out string answer)
        {
            answer = null;

            LogProvider.Info("");
            LogProvider.Info("***************************************");
            LogProvider.Info($"**** New Game. Game ID = {GameId} ****");

            // initial pattern 
            for (int i = 0; i < WordCharsCount; i++)
                _updatedPattern.Add(_wordPattern);

            // log purpose
            var theClock = Stopwatch.StartNew();

            // fetching words from the datasource
            var rawWords = File.ReadAllLines(PersianWordsFileName)
                                .Where(x => x.Length == WordCharsCount);

            // prepare the words
            var words = rawWords.ToDictionary(item => item
                                            , item => false /* false ~> ok to check, true ~> skip the Word */ );

            // log purpose
            int guessCounter = 0;
            bool isSolvedResult = false;

            foreach (var theWord in words.ToList())
            {
                if (words[theWord.Key]) // if the word has been marked as 'skippable', just skip it
                    continue;

                // log
                guessCounter++;
                var cnt = words.Count(x => !x.Value);   // count the non-skippable words
                LogProvider.Info($"matching '{theWord.Key}' in {cnt} words. Guess counter : {guessCounter}. Elapsed: {theClock.Elapsed.TotalHours:00}:{theClock.Elapsed.Minutes:00}:{theClock.Elapsed.Seconds:00} seconds...");

                // fetch the result from the remote api
                var response = getApiResponse(generateParams(theWord.Key, GameId));

                // parsing the response
                CheckResult result = Newtonsoft.Json.JsonConvert.DeserializeObject<CheckResult>(response);
                if (result.DictionaryError)
                {
                    LogProvider.Info($"The word is not in his dictionary! skipping to the next one...");
                    continue;
                }

                isSolvedResult = isSolved(result);
                if (isSolvedResult)
                {
                    answer = theWord.Key;
                    LogProvider.Info($"Eureka!! '{theWord.Key}'. Found it by {guessCounter} guess[es] in {theClock.Elapsed.TotalHours:00}:{theClock.Elapsed.Minutes:00}:{theClock.Elapsed.Seconds:00} seconds");
                    break;
                }

                // update the pattern based on a new response
                _updatedPattern = updatePattern(theWord.Key, result);
                string flattenPattern = flattenWordsPattern(_updatedPattern);

                // filter words by new pattern
                filterWordsByPattern(words, flattenPattern);
            }

            // 
            theClock.Stop();
            if (!isSolvedResult)
            {
                LogProvider.Info($"Not found by {guessCounter} guesses in {theClock.Elapsed.TotalHours:00}:{theClock.Elapsed.Minutes:00}:{theClock.Elapsed.Seconds:00} seconds. :-?");
            }
                
            return isSolvedResult;
        }

        private static bool isSolved(CheckResult result)
        {
            return result.Match.All(x => x == "g");
        }

        private static void filterWordsByPattern(Dictionary<string, bool> words, string pattern)
        {
            var theRegex = new Regex(pattern);
            foreach (var theWord in words.ToList())
            {
                if (theWord.Value)
                    continue;

                if (!theRegex.IsMatch(theWord.Key))
                    words[theWord.Key] = true;  // mark the word as Skippable
            }
        }

        private static string flattenWordsPattern(List<string> pattern)
        {
            return string.Join("", pattern);
        }

        private static List<string> updatePattern(string word, CheckResult result)
        {
            List<string> updatedPattern = new();

            // e.g : match[] = rgrrg
            //                  red, green, red, red, green
            for (int i = 0; i < result.Match.Length; i++)
            {
                switch (result.Match[i])
                {
                    case "g":
                        updatedPattern.Add(word[i].ToString());
                        break;

                    default:
                        updatedPattern.Add(_wordPattern);
                        break;
                }
            }

            return updatedPattern;
        }

        private static string generateParams(string word, int gameId)
        {
            return $"{api}?word={word}&g={gameId}";
        }

        private static string getApiResponse(string uri)
        {
            var request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
