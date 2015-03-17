using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ForumUpdater
{
    public static class Program
    {
        private const string FileCursors = "../../Cursors - 3.txt";
        private const string FileCursorsMedium = "../../Cursors - 2.txt";
        private const string FileCursorsOld = "../../Cursors.txt";
        private const string FileForums = "../../Forums.txt";
        private static string _url = "https://disqus.com/api/3.0/threads/list.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100&cursor=";

        private const string UrlInteresting = "https://disqus.com/api/3.0/forums/interestingForums.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100";

        private static int _requestCounter;
        private static string _firstCursor;
        private static string _lastCursor;
        private static StreamReader _streamResult;
        private static string _jsonString;
        private static DisqusModel<IEnumerable<DisqusFeed>> _results;
        private static Dictionary<string, string> _forumDictionary;
        private static String[] _forumFileLines;

        public static void Main(string[] args)
        {
            var mainThread= new Thread(MainThread);

            mainThread.Start();
        }

        public static void MainThread()
        {
            Console.WriteLine("Loading dictionary...");

            if (PrepareDictionary())
            {
                Console.WriteLine("Load complete. Press enter key to continue");
                Console.ReadLine();
                UpdateForum();
            }
            else
            {
                Console.WriteLine("Load failed. Press enter key to exit");
                Console.ReadLine();
            }
        }

        public static string FixSemiColon(string urlSeveralSemicolon)
        {
            return urlSeveralSemicolon.Replace(";", "/");
        }

        private static bool PrepareDictionary()
        {
            _requestCounter = 1;

            var result = true;

            var cursors = File.ReadAllLines(FileCursors);

            _firstCursor = cursors.First();
            _lastCursor = cursors.Last();

            _url = _url.Substring(0, _url.IndexOf("&cursor=", StringComparison.Ordinal));
            _url += "&cursor=" + _firstCursor;
            
            try
            {
                _streamResult = new StreamReader(WebRequest.Create(_url).GetResponse().GetResponseStream());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                Environment.Exit(1);
            }
            
            Console.WriteLine("Requests --->"+_requestCounter++);

            _jsonString = _streamResult.ReadToEnd();

            _results = JsonConvert.DeserializeObject<DisqusModel<IEnumerable<DisqusFeed>>>(_jsonString);

            _forumDictionary = new Dictionary<string, string>();

            _forumFileLines = File.ReadAllLines(FileForums);

            foreach (var line in _forumFileLines)
            {
                var parts = line.Split(';');
                try
                {
                    _forumDictionary.Add(parts[1], parts[0]);
                }
                catch (Exception)
                {
                    Console.WriteLine("Forums name repeated");
                    Console.WriteLine(parts[1]);
                    result = false;
                }

            }

            return result;
        }

        private static void UpdateForum()
        {
            while (_requestCounter < 1000)
            {
                if (_requestCounter < 999)
                {

                    //actualización de los 100 foros más interesantes de esta semana (por número de posts)

                    StreamReader streamResultInteresting = null;

                    try
                    {
                        streamResultInteresting =
                            new StreamReader(WebRequest.Create(UrlInteresting).GetResponse().GetResponseStream());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.ReadLine();
                        Environment.Exit(1);
                    }

                    Console.WriteLine("Requests --->" + _requestCounter++);

                    var jsonStringInteresting = streamResultInteresting.ReadToEnd();

                    var resultsInteresting =
                        JsonConvert.DeserializeObject<DisqusModel<InterestingForums>>(jsonStringInteresting);

                    var innerJson = resultsInteresting.Response.Objects;

                    var jo = JObject.Parse(innerJson.ToString());

                    var children = jo.Children();

                    foreach (var child in children)
                    {
                        foreach (var properties in child)
                        {
                            var forum = properties.SelectToken("id").ToString();
                            var link = properties.SelectToken("url").ToString();

                            if (!_forumDictionary.ContainsKey(forum) &&!UrlContainsDisqusDotCom(link))
                            {
                                _forumDictionary.Add(forum, link);

                                Console.WriteLine(link + " " + forum);

                                File.AppendAllLines(FileForums, new[] {FixSemiColon(link) + ";" + forum});
                            }
                        }
                    }

                    
                    //actualización de los cursores hacia atrás
                    while (_results.Cursor.HasPrev != null && (bool) _results.Cursor.HasPrev)
                    {
                        if (_requestCounter > 997) break;

                        foreach (var feed in _results.Response)
                        {
                            if (!_forumDictionary.ContainsKey(feed.Forum) && !UrlContainsDisqusDotCom(feed.Link))
                            {
                                _forumDictionary.Add(feed.Forum, feed.Link);

                                Console.WriteLine(feed.Link + " " + feed.Forum);

                                File.AppendAllLines(FileForums, new[] {FixSemiColon(feed.Link) + ";" + feed.Forum});
                            }

                        }

                        //reescritura del nuevo cursor hacia arriba (es previo)
                        if (!_firstCursor.Contains(_results.Cursor.Prev.Substring(0, _results.Cursor.Prev.Length - 1)))
                            File.WriteAllText(FileCursors,
                                _results.Cursor.Prev.Substring(0, _results.Cursor.Prev.Length - 1) + "0" + "\n" +
                                File.ReadAllText(FileCursors));

                        //reemplazo del parámetro "cursor"
                        _url = _url.Substring(0, _url.IndexOf("&cursor=", StringComparison.Ordinal));

                        _url += "&cursor=" + _results.Cursor.Prev;

                        try
                        {
                            _streamResult = new StreamReader(WebRequest.Create(_url).GetResponse().GetResponseStream());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.ReadLine();
                            Environment.Exit(1);
                        }

                        _results =
                            JsonConvert.DeserializeObject<DisqusModel<IEnumerable<DisqusFeed>>>(
                                _streamResult.ReadToEnd());

                        Console.WriteLine("Requests --->" + _requestCounter++);
                        Console.WriteLine("Updating previous cursors");
                    }

                    if ((bool) !_results.Cursor.HasPrev)
                    {

                        //reemplazo del parámetro "cursor"
                        _url = _url.Substring(0, _url.IndexOf("&cursor=", StringComparison.Ordinal));

                        _url += "&cursor=" + _lastCursor;


                        Console.WriteLine("Present time reached. Updating next cursors");

                        try
                        {
                            _streamResult = new StreamReader(WebRequest.Create(_url).GetResponse().GetResponseStream());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            Console.ReadLine();
                            Environment.Exit(1);
                        }

                        _results =
                            JsonConvert.DeserializeObject<DisqusModel<IEnumerable<DisqusFeed>>>(
                                _streamResult.ReadToEnd());



                        //actualización de los cursores hacia delante (del archivo Cursors - 2.txt, cuando alcance al primero del siguiente pararemos)
                        while (_results.Cursor.HasNext != null && (bool) _results.Cursor.HasNext)
                        {
                            if (_requestCounter > 997) break;

                            if (_results.Cursor.Next.Equals("1424976259673728:0:0"))
                            {
                                Console.WriteLine("Old Cursors file reached. Old and new will be merged");
                                var textNewFileCursors = File.ReadAllText(FileCursors);
                                File.WriteAllText(FileCursorsMedium,
                                    textNewFileCursors.Substring(0, textNewFileCursors.Length - 1) +
                                    File.ReadAllText(FileCursorsMedium));
                                Console.WriteLine("Files merged");
                                Console.ReadLine();
                                Environment.Exit(0);
                            }

                            foreach (var feed in _results.Response)
                            {
                                if (!_forumDictionary.ContainsKey(feed.Forum) && !UrlContainsDisqusDotCom(feed.Link))
                                {
                                    _forumDictionary.Add(feed.Forum, feed.Link);

                                    Console.WriteLine(feed.Link + " " + feed.Forum);

                                    File.AppendAllLines(FileForums, new[] {FixSemiColon(feed.Link) + ";" + feed.Forum});
                                }

                            }
                            //añadir nuevo cursor al final (es el siguiente)
                            File.AppendAllLines(FileCursors, new[] {_results.Cursor.Id});

                            //reemplazo del parámetro "cursor"
                            _url = _url.Substring(0, _url.IndexOf("&cursor=", StringComparison.Ordinal));

                            _url += "&cursor=" + _results.Cursor.Next;

                            try
                            {
                                _streamResult =
                                    new StreamReader(WebRequest.Create(_url).GetResponse().GetResponseStream());
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                Console.ReadLine();
                                Environment.Exit(1);
                            }


                            _results =
                                JsonConvert.DeserializeObject<DisqusModel<IEnumerable<DisqusFeed>>>(
                                    _streamResult.ReadToEnd());

                            Console.WriteLine("Requests --->" + _requestCounter++);
                            Console.WriteLine("Updating next cursors");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Request limit per hour reached, waiting to proceed again");
                    Thread.Sleep(3600001);
                    Console.WriteLine("Loading dictionary...");

                    if (PrepareDictionary())
                    {
                    }
                    else
                    {
                        Console.WriteLine("Load failed. Press enter key to exit");
                        Console.ReadLine();
                    }
                }
            }

        }

        public static bool UrlContainsDisqusDotCom(string url)
        {
            const string pattern = ".*disqus\\.com.*";

            return Regex.IsMatch(url, pattern);
        }
    }
}
