using System;

namespace EvernoteBackLinkerCSharp
{
    class NoteLink
    {
        private readonly EvernoteNote _sourceNote;

        public NoteLink(string text, string url, EvernoteNote sourceNote)
        {
            _sourceNote = sourceNote;
            // The / must exist in the end of the URL. I'm removing it just to find the guid:
            // evernote:///view/731386/s6/af0d84a6-9260-45df-8ebc-99c6bcdfc3fa/af0d84a6-9260-45df-8ebc-99c6bcdfc3fa/
            url = url.Trim('/');
            Guid = url.Substring(url.LastIndexOf("/", StringComparison.Ordinal) + 1);
            Url = _sourceNote.NoteGuidToUrl(Guid);
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
}