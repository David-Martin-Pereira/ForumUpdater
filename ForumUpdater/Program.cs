using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ForumUpdater
{
    public static class Program
    {
        private const string FileCursors = "../../Cursors.txt";
        private const string FileForums = "../../Forums.txt";
        private static string _url = "https://disqus.com/api/3.0/threads/list.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100";

        private static string _urlInteresting =
            "https://disqus.com/api/3.0/forums/interestingForums.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100";

        private static int requestCounter;
        private static string firstCursor;
        private static string lastCursor;
        private static StreamReader _streamResult;
        private static string _jsonString;
        private static DisqusModel<IEnumerable<DisqusFeed>> _results;
        private static DisqusModel<InterestingForums> _resultsInteresting; 
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
            requestCounter = 1;

            var result = true;

            var cursors = File.ReadAllLines(FileCursors);

            firstCursor = cursors.First();
            lastCursor = cursors.Last();

            _url += "&cursor=" + firstCursor;
            
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
            
            Console.WriteLine("Requests --->"+requestCounter++);

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
            //actualización de los 100 foros más interesantes de esta semana (por número de posts)

            StreamReader _streamResultInteresting=null;

            try
            {
                _streamResultInteresting = new StreamReader(WebRequest.Create(_urlInteresting).GetResponse().GetResponseStream());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                Environment.Exit(1);
            }
            
            Console.WriteLine("Requests --->" + requestCounter++);

            var jsonStringInteresting = _streamResultInteresting.ReadToEnd();

            var _resultsInteresting = JsonConvert.DeserializeObject<DisqusModel<InterestingForums>>(jsonStringInteresting);

            var innerJson = _resultsInteresting.Response.Objects;

            var jo = JObject.Parse(innerJson.ToString());

            var children = jo.Children();

            foreach (var child in children)
            {
                foreach (var properties in child)
                {
                    var forum = properties.SelectToken("id").ToString();
                    var link = properties.SelectToken("url").ToString();

                    if (!_forumDictionary.ContainsKey(forum))
                    {
                        _forumDictionary.Add(forum,link);

                        Console.WriteLine(link+" "+forum);

                        File.AppendAllLines(FileForums,new []{FixSemiColon(link)+";"+forum});
                    }
                }
            }



            //actualización de los cursores hacia atrás
            while ((bool)_results.Cursor.HasPrev)
            {
                foreach (var feed in _results.Response)
                {
                    if (!_forumDictionary.ContainsKey(feed.Forum))
                    {
                        _forumDictionary.Add(feed.Forum, feed.Link);

                        Console.WriteLine(feed.Link + " " + feed.Forum);

                        File.AppendAllLines(FileForums, new[] { FixSemiColon(feed.Link) + ";" + feed.Forum });
                    }

                }

                //reescritura del nuevo cursor hacia arriba (es previo)
                File.WriteAllText(FileCursors,_results.Cursor.Id+"\n"+File.ReadAllText(FileCursors));

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
                
                _results = JsonConvert.DeserializeObject<DisqusModel<IEnumerable<DisqusFeed>>>(_streamResult.ReadToEnd());

                Console.WriteLine("Requests --->" + requestCounter++);
                Console.WriteLine("Updating previous cursors");
            }

            //reemplazo del parámetro "cursor"
            _url = _url.Substring(0, _url.IndexOf("&cursor=", StringComparison.Ordinal));

            _url += "&cursor=" + lastCursor;

            Console.WriteLine("Present time reached. Updating next cursors");
            //actualización de los cursores hacia delante
            while ((bool)_results.Cursor.HasNext)
            {

                foreach (var feed in _results.Response)
                {
                    if (!_forumDictionary.ContainsKey(feed.Forum))
                    {
                        _forumDictionary.Add(feed.Forum, feed.Link);

                        Console.WriteLine(feed.Link + " " + feed.Forum);

                        File.AppendAllLines(FileForums, new[] { FixSemiColon(feed.Link) + ";" + feed.Forum });
                    }

                }
                //añadir nuevo cursor al final (es el siguiente)
                File.AppendAllLines(FileCursors, new[] { _results.Cursor.Id });

                //reemplazo del parámetro "cursor"
                _url = _url.Substring(0, _url.IndexOf("&cursor=", StringComparison.Ordinal));

                _url += "&cursor=" + _results.Cursor.Next;

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

                
                _results = JsonConvert.DeserializeObject<DisqusModel<IEnumerable<DisqusFeed>>>(_streamResult.ReadToEnd());

                Console.WriteLine("Requests --->" + requestCounter++);
                Console.WriteLine("Updating next cursors");
            }
        }
        
    }
}
