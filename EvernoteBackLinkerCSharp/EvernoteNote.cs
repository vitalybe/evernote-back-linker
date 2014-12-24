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
    struct NoteLink
    {
        private string _url;
        public string Title { get; set; }

        public string Url
        {
            get { return _url; }
            set
            {
                string internalUrl;
                if (value.StartsWith(Consts.ExternalNoteUrlPrefix))
                {
                    var guid = value.Replace(Consts.ExternalNoteUrlPrefix, "");
                    internalUrl = String.Format("{0}{1}/{1}", Consts.ExternalNoteUrlPrefix, guid);
                }
                else
                {
                    internalUrl = value;
                }

                _url = internalUrl;
            }
        }
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
            var backlinks = from potentialBacklink in doc.DocumentNode.SelectNodes("/en-note/div[a and starts-with(.,'[[[Backlink')]")
                            let childNodes = potentialBacklink.ChildNodes
                            where childNodes.Count() == 3
                            let prefix = childNodes[0]
                            let a = childNodes[1]
                            let sufix = childNodes[2]
                            where prefix.InnerText == Consts.BacklinkPrefix && sufix.InnerText == Consts.BacklinkSufix
                            where a.Name == "a" && prefix.Name == "#text" && sufix.Name == "#text"
                            select new NoteLink { Title = a.InnerText, Url = a.Attributes["href"].Value };

            return backlinks;
        }

        public IEnumerable<NoteLink> FindNoteLinks()
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(Content);
            
            var noteLinksXpath = string.Format("//a[starts-with(@href, '{0}') or starts-with(@href, '{1}')]", 
                Consts.ExternalNoteUrlPrefix, Consts.InternalNoteUrlPrefix);
            var noteLinks = from potentialLinks in doc.DocumentNode.SelectNodes(noteLinksXpath)
                            select new NoteLink  { Title = potentialLinks.InnerText, Url = potentialLinks.Attributes["href"].Value };

            var backlinks = FindBacklinks();
            return noteLinks.Except(backlinks, (link, banklink) => link.Url == banklink.Url).ToList();
        }


        private string AppendToContent(string addedContent)
        {
            Regex enNoteRegex = new Regex("(<en-note .+?>)");
            var replacement = "$1" + addedContent.Replace("$", "$$");

            return enNoteRegex.Replace(_note.Content, replacement);
        }

        public void AddBacklink(string linkTitle, string linkUrl)
        {
            var backlinkHtml = string.Format("<div>{0}<a href='{1}' style='color:#69aa35'>{2}</a>{3}</div>", 
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
                string shard = GetShardFromToken(Consts.EvernoteDevToken);
                return string.Format("https://www.evernote.com/shard/{0}/edit/notebook/{1}", shard, _note.Guid);
            }
        }

        /// <summary>
        /// Gets the shard from token.
        /// E.g from S=s6:U=b28fa:E=14526ceas8f:C=13dcf1d7f91:P=185:A=vitalyb-4494:V=2:H=bd19cea547f7adc32e3466f96b923dd6
        /// Returns s6
        /// </summary>
        /// <param name="evernoteToken">The evernote token.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException">Thrown if fails to extract the shard from the token</exception>
        private static string GetShardFromToken(string evernoteToken)
        {
            var match = Regex.Match(evernoteToken, "^S=(s[0-9]+):");
            if (match.Success == false)
            {
                throw new Exception("Failed to extract shard from the token");
            }

            return match.Groups[1].Value;
        }

        public void Open()
        {
            Process.Start(Url);
        }
    }
}