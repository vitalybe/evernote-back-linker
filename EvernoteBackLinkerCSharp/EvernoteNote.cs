using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Evernote.EDAM.NoteStore;
using Evernote.EDAM.Type;
using EvernoteBackLinkerCSharp.Misc;
using HtmlAgilityPack;

namespace EvernoteBackLinkerCSharp
{
    class NoteLink
    {
        public NoteLink(string text, string url)
        {
            // The / must exist in the end of the URL. I'm removing it just to find the guid:
            // evernote:///view/731386/s6/af0d84a6-9260-45df-8ebc-99c6bcdfc3fa/af0d84a6-9260-45df-8ebc-99c6bcdfc3fa/
            url = url.Trim('/');
            Guid = url.Substring(url.LastIndexOf("/", StringComparison.Ordinal) + 1);
            Url = EvernoteNote.NoteGuidToUrl(Guid);
            Text = text;
            if (string.IsNullOrWhiteSpace(Guid))
            {
                throw new Exception("Guid can not be empty.");
            }
        }
        
        public string Text { get; private set; }
        public string Url { get; private set; }
        public string Guid { get; private set; }
    }


    class EvernoteNote
    {
        private readonly NoteStore.Client _noteStore;
        private readonly Note _note;

        public EvernoteNote(NoteStore.Client noteStore, Note note)
        {
            _noteStore = noteStore;
            _note = note;
        }

        public string Content
        {
            get { return _note.Content; }
            set
            {
                _note.Content = value;
                _noteStore.updateNote(Consts.EvernoteDevToken, _note);
            }
        }

        public string Title
        {
            get { return _note.Title; }
            set
            {
                _note.Title = value;
                _noteStore.updateNote(Consts.EvernoteDevToken, _note);
            }
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
                            select new NoteLink (text: a.InnerText, url: a.Attributes["href"].Value);

            return UniqueNoteLinks(backlinks);
        }

        private static IEnumerable<NoteLink> UniqueNoteLinks(IEnumerable<NoteLink> backlinks)
        {
            return from backlink in backlinks
                group backlink by backlink.Url into groupedLinks
                select new NoteLink(groupedLinks.First().Text, groupedLinks.Key);
        }

        public IEnumerable<NoteLink> FindNoteLinks()
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(Content);
            
            var noteLinksXpath = string.Format("//a[starts-with(@href, '{0}') or starts-with(@href, '{1}')]", 
                Consts.ExternalNoteUrlPrefix, Consts.InternalNoteUrlPrefix);

            var potentialLinksNodes = doc.DocumentNode.SelectNodes(noteLinksXpath);
            potentialLinksNodes = potentialLinksNodes ?? new HtmlNodeCollection(null);

            var noteLinks = from potentialLinks in potentialLinksNodes
                            select new NoteLink (text: potentialLinks.InnerText, url: potentialLinks.Attributes["href"].Value );
            
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
            var backlinkHtml = string.Format("<div>{0}<a href='{1}' style='color:#69aa35'>{2}</a>{3}</div><br/>", 
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

        public static string NoteGuidToUrl(string guid)
        {
            return String.Format("{0}{1}/{1}/", Consts.InternalNoteUrlPrefix, guid);
        }
    }
}