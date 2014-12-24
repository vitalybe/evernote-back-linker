using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvernoteBackLinkerCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Evernote evernote = new Evernote();
            
            // For note debugging:
            //ProcessNote(evernote.FindById("f8b526ec-211a-42a4-9852-38b3a8afcae1"), evernote);
            //return;

            foreach (var note in evernote.GetRecentlyChangedNotes(TimeSpan.FromDays(365*10)))
            {
                ProcessNote(note, evernote);
            }
        }

        private static void ProcessNote(EvernoteNote note, Evernote evernote)
        {
            Console.WriteLine("Prcessing note: " + note.Title);

            var noteLinks = note.FindNoteLinks();
            foreach (var noteLink in noteLinks)
            {
                Console.Write("\tFound note link to: {0}... ", noteLink.Text);
                var linkedNote = evernote.FindById(noteLink.Guid);
                var backlinks = linkedNote.FindBacklinks();
                if (backlinks.Any(backlink => backlink.Url == note.Url) == false)
                {
                    Console.WriteLine("Adding backlink");
                    linkedNote.AddBacklink(note.Title, note.Url);
                }
                else
                {
                    Console.WriteLine("Backlink already exists");
                }
            }
        }
    }
}
