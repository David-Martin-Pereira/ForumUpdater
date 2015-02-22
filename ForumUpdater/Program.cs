using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Newtonsoft.Json;

namespace ForumUpdater
{
    public static class Program
    {
        private const string FileCursors = "../../Cursors.txt";
        private const string FileForums = "../../Forums.txt";
        private static string _url = "https://disqus.com/api/3.0/threads/list.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100";

        private static StreamReader _result;
        private static string _jsonString;
        private static DisqusModel _results;
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
                Console.WriteLine("Load complete. Press any key to continue");
                Console.ReadLine();
                UpdateForum();
            }
            else
            {
                Console.WriteLine("Load failed. Press any key to exit");
                Console.ReadLine();
            }
        }

        public static string FixSemiColon(string urlSeveralSemicolon)
        {
            return urlSeveralSemicolon.Replace(";", "/");
        }

        private static bool PrepareDictionary()
        {
            var result = true;

            var lastCursor = File.ReadAllLines(FileCursors).Last();

            _url += "&cursor=" + lastCursor;

            // ReSharper disable once AssignNullToNotNullAttribute
            _result = new StreamReader(WebRequest.Create(_url).GetResponse().GetResponseStream());

            _jsonString = _result.ReadToEnd();

            _results = JsonConvert.DeserializeObject<DisqusModel>(_jsonString);

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
            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
            while (_results.Cursor.HasNext)
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

                File.AppendAllLines(FileCursors, new[] { _results.Cursor.Id });


                _url = _url.Substring(0, _url.IndexOf("&cursor=", StringComparison.Ordinal));

                _url += "&cursor=" + _results.Cursor.Next;

                // ReSharper disable once AssignNullToNotNullAttribute
                
                _result = new StreamReader(WebRequest.Create(_url).GetResponse().GetResponseStream());
                _results = JsonConvert.DeserializeObject<DisqusModel>(_result.ReadToEnd());
            }
        }
        
    }
}
