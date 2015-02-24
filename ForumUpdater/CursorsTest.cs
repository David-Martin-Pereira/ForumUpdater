using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace ForumUpdater
{
    [TestFixture]
    class CursorsTest
    {
        private const string url= "https://disqus.com/api/3.0/threads/list.json?api_key=ytwDh9f4ndTA027jIPzMAFC0hXdeyEEwOsKjujADneiRl2SUdjUpQ40bEXDMhupa&limit=100";
        private const string dummyCursor = "../../DummyCursor.txt";

        [Test]
        public void PrecedingToFirstCursorCorrectlyAddedFirst()
        {

            var urlLocal = url;

            var streamResult = new StreamReader(WebRequest.Create(urlLocal).GetResponse().GetResponseStream());
        }

        [Test]
        public void FollowingToLastCursorCorrectyAddedLast()
        {
            var urlLocal = url;

            var streamResult = new StreamReader(WebRequest.Create(urlLocal).GetResponse().GetResponseStream());
        }

        [Test]
        public void CorrectReplaceOfAFile()
        {
            var testText = "asdifjasdof\nsdjfopisjgojid\nofaisjdpofjasodf\n";

            File.WriteAllText(dummyCursor,testText);

            var previousText = "42345235";

            File.WriteAllText(dummyCursor,previousText+"\n"+ File.ReadAllText(dummyCursor));

            //File.AppendAllLines(dummyCursor,new []{previousText});

            var lastLine = File.ReadAllLines(dummyCursor).Last();
            var firstLine = File.ReadAllLines(dummyCursor).First();

            Assert.AreEqual("42345235", firstLine);
            Assert.AreEqual("ofaisjdpofjasodf", lastLine);

        }
    }
}
