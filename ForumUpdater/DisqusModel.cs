using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ForumUpdater
{
    public struct DisqusModel<T>
    {
        public DisqusCursor Cursor;
        //public IEnumerable<DisqusComment> Response;
        public T Response;
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
        public String Prev;
        public bool? HasNext;
        public String Next;
        public bool? HasPrev;
        public String Total;
        public String Id;
        public bool? More;
    }

    public struct DisqusFeed
    {
        public string Link;
        public string Forum;
    }

    
    public struct InterestingForums
    {
        public List<DisqusItem> Items;
        public Object Objects;
    }

    public struct DisqusItem
    {
        public string Reason;
        public string Id;
    }
}
