using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ForumUpdater
{
    public struct DisqusModel
    {
        public DisqusCursor Cursor;
        //public IEnumerable<DisqusComment> Response;
        public IEnumerable<DisqusFeed> Response;
        //public string Response;
        public int Code;//error code
    }

    public struct DisqusComment
    {
        public string Parent;
        public int Likes;
        public int Dislikes;
        public string Forum;
        public long Thread;
        public DisqusAuthor Author;
        public string Raw_message;
        public DateTime CreatedAt;
        public long Id;

    }

    public struct DisqusAuthor
    {
        public string Name;
        public string Url;
        public string ProfileUrl;
        public double Reputation;
        public long Id;
    }

    public struct DisqusCursor
    {
        public string Prev;
        public bool HasNext;
        public string Next;
        public bool HasPrev;
        public string Total;
        public string Id;
        public bool More;
    }

    public struct DisqusFeed
    {
        public string Link;
        public string Forum;
    }
}
