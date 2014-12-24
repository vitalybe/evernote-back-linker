using System;
using System.Collections.Generic;
using Evernote.EDAM.Error;
using Evernote.EDAM.NoteStore;
using Evernote.EDAM.UserStore;
using Thrift.Protocol;
using Thrift.Transport;
using EvernoteSDK = Evernote;

namespace EvernoteBackLinkerCSharp
{
    class Evernote
    {
        // Can be be initialized after authentication is complete.
        private NoteStore.Client _noteStore;

        public NoteStore.Client NoteStore
        {
            get
            {
                if (_noteStore == null)
                {
                    Console.WriteLine("Connecting to Evernote...");

                    //String evernoteHost = "sandbox.evernote.com";
                    const string EVERNOTE_HOST = "www.evernote.com";
                    Uri userStoreUrl = new Uri("https://" + EVERNOTE_HOST + "/edam/user");
                    TTransport userStoreTransport = new THttpClient(userStoreUrl);
                    TProtocol userStoreProtocol = new TBinaryProtocol(userStoreTransport);
                    UserStore.Client userStore = new UserStore.Client(userStoreProtocol);

                    bool versionOk =
                        userStore.checkVersion("Evernote EDAMTest (C#)",
                           Constants.EDAM_VERSION_MAJOR,
                           Constants.EDAM_VERSION_MINOR);
                    if (!versionOk)
                    {
                        throw new Exception("EDAM SDK version is not recent enough. Please update via nuget");
                    }

                    // Get the URL used to interact with the contents of the user's account
                    // When your application authenticates using OAuth, the NoteStore URL will
                    // be returned along with the auth token in the final OAuth request.
                    // In that case, you don't need to make this call.
                    String noteStoreUrl = userStore.getNoteStoreUrl(Consts.EvernoteDevToken);

                    TTransport noteStoreTransport = new THttpClient(new Uri(noteStoreUrl));
                    TProtocol noteStoreProtocol = new TBinaryProtocol(noteStoreTransport);
                    _noteStore = new NoteStore.Client(noteStoreProtocol);
                }

                return _noteStore;
            }
        }

        /// <summary>
        /// Finds an Evernote note by ID.
        /// </summary>
        /// <param name="noteGuid">The note GUID.</param>
        /// <returns>The note if one is found. Null otherwise.</returns>
        public EvernoteNote FindById(string noteGuid)
        {
            EvernoteNote note;
            try
            {
                var externalNote = NoteStore.getNote(Consts.EvernoteDevToken, noteGuid, true, false, false, false);
                note = new EvernoteNote(_noteStore, externalNote);
            }
            catch (EDAMNotFoundException)
            {
                note = null;
            }

            return note;

        }

        /// <summary>
        /// Finds the note by URL.
        /// The url we can handle is: evernote:///view/731386/s6/79873b78-8570-4227-91e4-952a724ba445/79873b78-8570-4227-91e4-952a724ba445/
        /// </summary>
        /// <returns>The external note, if the given URL is valid and points to a note. Null oterhwise</returns>
        /// <exception cref="System.ArgumentNullException">If externalTaskUriField is null</exception>
        public EvernoteNote FindNoteByUrl(string externalTaskUriField)
        {
            if (externalTaskUriField == null) throw new ArgumentNullException("externalTaskUriField");

            EvernoteNote externalNote = null;

            if (Uri.IsWellFormedUriString(externalTaskUriField, UriKind.Absolute))
            {
                Uri evernoteNoteUri = new Uri(externalTaskUriField);

                // We'd like to go from that: evernote:///view/731386/s6/79873b78-8570-4227-91e4-952a724ba445/79873b78-8570-4227-91e4-952a724ba445/
                // To: 79873b78-8570-4227-91e4-952a724ba445
                int segmentsCount = evernoteNoteUri.Segments.Length;
                string lastSegment = evernoteNoteUri.Segments[segmentsCount - 1];
                lastSegment = lastSegment.Trim('/');

                Guid noteGuid;
                bool parsed = Guid.TryParse(lastSegment, out noteGuid);

                if (parsed)
                {
                    // If the last segment is a guid, we assume it is a note. We send it as it is to avoid confusing the external application.
                    externalNote = FindById(lastSegment);
                }
            }

            return externalNote;
        }

        public IEnumerable<EvernoteNote> GetRecentlyChangedNotes(TimeSpan lastModifiedTimeSpan)
        {
            NoteFilter filter = new NoteFilter();
            filter.Words  = "updated:day-" + Math.Round(lastModifiedTimeSpan.TotalDays);
            NotesMetadataResultSpec spec = new NotesMetadataResultSpec { IncludeTitle = true };

            int offset = 0;
            int pageSize = 10;
            NotesMetadataList notes = null;

            do
            {
                notes = NoteStore.findNotesMetadata(Consts.EvernoteDevToken, filter, offset, pageSize, spec);
                foreach (NoteMetadata note in notes.Notes)
                {
                    yield return FindById(note.Guid);
                }
                offset = offset + notes.Notes.Count;
            } while (notes.TotalNotes > offset);
        }
    }
}