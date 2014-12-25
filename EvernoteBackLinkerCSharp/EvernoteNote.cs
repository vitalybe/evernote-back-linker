using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Type;
using Evernote.EDAM.UserStore;
using EvernoteBackLinkerCSharp.Misc;
using HtmlAgilityPack;

namespace EvernoteBackLinkerCSharp
{
    class EvernoteNote
    {
        private readonly Evernote _evernote;
        private readonly Note _note;

        private string ExternalNoteUrlPrefix
        {
            get { return String.Format("https://www.evernote.com/shard/{0}/nl/{1}/", 
                _evernote.EvernoteUser.ShardId, _evernote.EvernoteUser.Id); }
        }

        private string InternalNoteUrlPrefix
        {
            get { return String.Format("evernote:///view/{1}/{0}/", _evernote.EvernoteUser.ShardId, _evernote.EvernoteUser.Id); }
        }


        public EvernoteNote(Evernote evernote, Note note)
        {
            _evernote = evernote;
            _note = note;
        }

        public string Content
        {
            get { return _note.Content; }
            set
            {
                _note.Content = value;
                _evernote.NoteStore.updateNote(_evernote.DevToken, _note);
            }
        }

        public string Title
        {
            get { return _note.Title; }
            set
            {
                _note.Title = value;
                _evernote.NoteStore.updateNote(_evernote.DevToken, _note);
            }
        }

        public DateTime Updated
        {
            get
            {
                return UnixTimeStampToDateTime(_note.Updated);
            }
        }

        private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public IEnumerable<NoteLink> FindBacklinks()
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(Content);
            
            var backlinkNodes = doc.DocumentNode.SelectNodes("/en-note//div[a and starts-with(.,'[[[Backlink')]");
            backlinkNodes = backlinkNodes ?? new HtmlNodeCollection(null);

            var backlinks = from potentialBacklink in backlinkNodes
                            let childNodes = potentialBacklink.ChildNodes
                            where childNodes.Count() == 3
                            let prefix = childNodes[0]
                            let a = childNodes[1]
                            let sufix = childNodes[2]
                            where prefix.InnerText == Consts.BacklinkPrefix && sufix.InnerText == Consts.BacklinkSufix
                            where a.Name == "a" && prefix.Name == "#text" && sufix.Name == "#text"
                            select new NoteLink (a.InnerText, a.Attributes["href"].Value, this);

            return UniqueNoteLinks(backlinks);
        }

        private IEnumerable<NoteLink> UniqueNoteLinks(IEnumerable<NoteLink> backlinks)
        {
            return from backlink in backlinks
                group backlink by backlink.Url into groupedLinks
                select new NoteLink(groupedLinks.First().Text, groupedLinks.Key, this);
        }

        public IEnumerable<NoteLink> FindNoteLinks()
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(Content);
            
            var noteLinksXpath = String.Format("//a[starts-with(@href, '{0}') or starts-with(@href, '{1}')]", 
                ExternalNoteUrlPrefix, InternalNoteUrlPrefix);

            var potentialLinksNodes = doc.DocumentNode.SelectNodes(noteLinksXpath);
            potentialLinksNodes = potentialLinksNodes ?? new HtmlNodeCollection(null);

            var noteLinks = from potentialLinks in potentialLinksNodes
                            select new NoteLink(potentialLinks.InnerText, potentialLinks.Attributes["href"].Value, this);
            
            noteLinks = UniqueNoteLinks(noteLinks);
            
            var backlinks = FindBacklinks();
            return noteLinks.Except(backlinks, (link, banklink) => link.Url == banklink.Url);
        }


        private string AppendToContent(string addedContent)
        {
            Regex enNoteRegex = new Regex("(<en-note.+?>)");
            var replacement = "$1" + addedContent.Replace("$", "$$");

            return enNoteRegex.Replace(_note.Content, replacement);
        }

        public void AddBacklink(string linkTitle, string linkUrl)
        {
            var backlinkHtml = String.Format("<div>{0}<a href='{1}' style='color:#69aa35'>{2}</a>{3}</div><br/>", 
                Consts.BacklinkPrefix, linkUrl, linkTitle, Consts.BacklinkSufix);
            this.Content = AppendToContent(backlinkHtml);
        }


        /// <summary>
        /// Gets the note URL in the following format: 
        /// https://www.evernote.com/shard/s6/edit/notebook/ecbe4a3d-b5bb-4a09-977a-7ec125d2836d#st=p&n=ecbe4a3d-b5bb-4a09-977a-7ec125d2836d
        /// </summary>
        /// <param name="note"></param>
        /// <returns></returns>
        public string Url
        {
            get
            {
                return NoteGuidToUrl(_note.Guid);
            }
        }

        public string NoteGuidToUrl(string guid)
        {
            return String.Format("{0}{1}/{1}/", InternalNoteUrlPrefix, guid);
        }
    }
}