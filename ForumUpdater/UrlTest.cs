using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ForumUpdater
{
    [TestFixture]
    public class UrlTest
    {
        [Test]
        public void UrlConCursorCorrecto()
        {
            var url = "https://disqus.com/api/3.0/threads/list.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100";

            const string fileCursors = "../../Cursors.txt";

            var lastCursor = File.ReadAllLines(fileCursors).Last();

            url += "&cursor=" + lastCursor;

            Assert.AreEqual("https://disqus.com/api/3.0/threads/list.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100&cursor=1423495865138525:0:0", url);
        }

        [Test]
        public void UrlConCursorNuevoCorrecto()
        {
            string url = "https://disqus.com/api/3.0/threads/list.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100";

            const string fileCursors = "../../Cursors.txt";

            var lastCursor = File.ReadAllLines(fileCursors).Last();

            url += "&cursor=" + lastCursor;

            url = url.Substring(0, url.IndexOf("&cursor=", StringComparison.Ordinal));

            url += "&cursor=1423495867133776:0:0";

            Assert.AreEqual("https://disqus.com/api/3.0/threads/list.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100&cursor=1423495867133776:0:0", url);
        }


        [Test]
        public void UrlConVariosSemicolon_FixSemiColon_UrlConUnSemiColon()
        {
            const string urlEjemplo = "http://ejemplo.com/esto;tiene;varios;semiColon;Y;No;quiero;ninguno";

            var finalUrl = Program.FixSemiColon(urlEjemplo);

            Assert.IsFalse(finalUrl.Contains(";"));

        }

        [Test]
        public void DisqusErrorJson()
        {
            const string url =
                "https://disqus.com/api/3.0/threads/listPosts.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&forum=aubi&limit=50&thread=link:http://www.autobild.de/artikel/audi-r8-erlkoenig-2015-video-5036062.htm";

            WebRequest webRequest = null;
            WebResponse webResponse = null;
            StreamReader streamReader = null;
            Stream responseStream = null;
            // ReSharper disable once AssignNullToNotNullAttribute
            try
            {
                webRequest = WebRequest.Create(url);
            }
            catch (Exception)
            {
                Console.WriteLine("Error en el web request");
            }

            try
            {
                webResponse = webRequest.GetResponse();
            }
            catch (Exception)
            {
                Console.WriteLine("Error en el webResponse");
            }

            try
            {
                responseStream = webResponse.GetResponseStream();
            }
            catch (Exception)
            {
                Console.WriteLine("Error en el response Stream");
            }

            try
            {
                streamReader = new StreamReader(responseStream);
            }
            catch (Exception)
            {
                Console.WriteLine("Error en el stream reader");
            }

            var jsonString = streamReader.ReadToEnd();
            var results = JsonConvert.DeserializeObject<DisqusModel>(jsonString);

            Console.WriteLine(jsonString);

            //Assert.AreEqual("Invalid argument, 'thread': Unable to find thread 'link:http://www.autobild.de/artikel/audi-r8-erlkoenig-2015-video-5036062.htm'",results.Response.First());

            //Assert.AreEqual(2, results.Code);

        }
    }
}
