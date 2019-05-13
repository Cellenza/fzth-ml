using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using LinqToTwitter;
using Newtonsoft.Json;

namespace FZTHDevopsML.TweetSearch
{
    class Program
    {
        const int RetryCount = 15;
        private const int MaxTweetCount = 10000;
        static void Main(string[] args)
        {
            string consumerKey = null;
            string consumerSecret = null;
            string hashTagEntry = null;

            if (args.Length >= 1)
            {
                consumerKey = args[0];
            }

            if (args.Length >= 2)
            {
                consumerSecret = args[1];
            }

            if (args.Length >= 3)
            {
                consumerKey = args[2];
            }

            if (string.IsNullOrEmpty(consumerKey))
            {
                Console.Write("Consumer Key:    ");
                consumerKey = Console.ReadLine()?.Trim();
            }

            if (string.IsNullOrEmpty(consumerSecret))
            {
                Console.Write("Consumer Secret: ");
                consumerSecret = Console.ReadLine()?.Trim();
            }

            if (string.IsNullOrWhiteSpace(hashTagEntry))
            {
                Console.Write("Hashtags:         ");
                hashTagEntry = Console.ReadLine()?.Trim();
            }
          
            if (hashTagEntry == null)
            {
                return;
            }
            var hastTagList = hashTagEntry.Trim().Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var hashTag in hastTagList)
            {
                ExtractHashtag(hashTag, consumerKey, consumerSecret);
            }
            Console.WriteLine("Press ENTER to quit");
            Console.ReadLine();
        }

        private static void ExtractHashtag(string hastTag, string consumerKey, string consumerSecret)
        {
            var fileName = $"tweets-{hastTag}.json.gz";
            Console.WriteLine();
            Console.Write("#{0}: ",hastTag);
            try
            {
                var auth = new SingleUserAuthorizer
                {
                    CredentialStore = new InMemoryCredentialStore()
                    {
                        ConsumerKey = consumerKey,
                        ConsumerSecret = consumerSecret,
                    }
                };

                var twitterCtx = new TwitterContext(auth);

                var combinedSearchResults = new List<Status>();
                var end = DateTime.Now.Date;
                var start = end.AddDays(-8);
                var date = end;
                var maxId = 0uL;
                var currentDay = start.Day + 1;
                while (date >= start)
                {
                    if (date.Day != currentDay)
                    {
                        Console.Write($"{date.Day:00}/{date.Month:00}.");
                        currentDay = date.Day;
                    }
                    else
                    {
                        Console.Write(".");
                    }

                    Search searchResponse = null;

                    int retry = RetryCount;

                    while (retry > 0)
                    {
                        try
                        {
                            searchResponse = GenerateSearch(twitterCtx, hastTag, maxId);
                            break;
                        }
                        catch (Exception)
                        {
                            retry--;
                            Console.Write("*");
                            Thread.Sleep(TimeSpan.FromMinutes(1));
                            searchResponse = null;
                        }
                    }

                    if (retry == 0)
                    {
                        Console.WriteLine(" timeout :(");
                    }


                    if (searchResponse == null)
                    {
                        break;
                    }

                    if (searchResponse.Statuses.Count > 0)
                    {
                        combinedSearchResults.AddRange(searchResponse.Statuses);
                        var lastStatus = searchResponse.Statuses.OrderBy(st => st.CreatedAt).First();
                        date = lastStatus.CreatedAt;
                        maxId = lastStatus.StatusID;
                        if (combinedSearchResults.Count > MaxTweetCount)
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                Console.WriteLine($" {combinedSearchResults.Count} tweets found");
                var dump = JsonConvert.SerializeObject(combinedSearchResults);
                using (var dumpStream = new MemoryStream())
                {
                    using (var writer = new StreamWriter(dumpStream))
                    {
                        writer.Write(dump);
                        writer.Flush();

                        using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            using (var gZipStream = new GZipStream(fileStream, CompressionMode.Compress, true))
                            {
                                dumpStream.Position = 0;
                                dumpStream.CopyTo(gZipStream);


                                gZipStream.Close();
                                fileStream.Flush();
                            }
                        }
                    }
                }


                Console.WriteLine($"'{fileName}' file generated.");
            }
            catch (AggregateException aggregateException)
            {
                foreach (var ex in aggregateException.InnerExceptions)
                {
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        private static Search GenerateSearch(TwitterContext twitterCtx, string hastTag, ulong maxId)
        {
            Search searchResponse = (
                (from search in twitterCtx.Search
                    where search.Type == SearchType.Search &&
                          search.Query == $"#{hastTag}"
                          && search.MaxID == maxId
                          && search.SearchLanguage == "fr"
                    select search)
                .SingleOrDefault());
            return searchResponse;
        }
    }
}
