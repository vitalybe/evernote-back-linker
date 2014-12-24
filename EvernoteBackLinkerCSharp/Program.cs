using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Evernote.EDAM.Error;

namespace EvernoteBackLinkerCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            const string LAST_PROCESSED_UPDATE_DATE_FILE = "LastUpdateTime.txt";


            // For note debugging:
            //ProcessNote(evernote.FindById("f8b526ec-211a-42a4-9852-38b3a8afcae1"), evernote);
            //return;

            bool processingDone = false;
            while (processingDone == false)
            {
                try
                {
                    Evernote evernote = new Evernote();
                    var noteProcessingSpan = GetNoteProcessingSpan(LAST_PROCESSED_UPDATE_DATE_FILE);

                    ProcessNotes(LAST_PROCESSED_UPDATE_DATE_FILE, noteProcessingSpan, evernote);
                    processingDone = true;
                }
                catch (EDAMSystemException e)
                {
                    if (e.ErrorCode == (EDAMErrorCode)19)
                    {
                        Console.WriteLine("ERROR - Evernote quota reached. Wil tray again after a while: " + DateTime.Now.ToShortTimeString());
                        Thread.Sleep(TimeSpan.FromMinutes(15));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private static TimeSpan GetNoteProcessingSpan(string lastProcessedUpdateDateFile)
        {
            var noteSpan = TimeSpan.FromDays(365*10);
            if (File.Exists(lastProcessedUpdateDateFile))
            {
                var lastProcessedUpdateDate = File.ReadAllText(lastProcessedUpdateDateFile);
                var updateDateString = DateTime.Parse(lastProcessedUpdateDate);
                noteSpan = DateTime.Now - updateDateString;
            }
            return noteSpan;
        }

        private static void ProcessNotes(string lastProcessedUpdateDateFile, TimeSpan noteSpan, Evernote evernote)
        {
            foreach (var note in evernote.GetRecentlyChangedNotes(noteSpan))
            {
                bool noteProcessSuccess = false;
                while (noteProcessSuccess == false)
                {
                    try
                    {
                        ProcessNote(note, evernote);
                        File.WriteAllText(lastProcessedUpdateDateFile, note.Updated.ToShortDateString());
                        noteProcessSuccess = true;
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("ERROR - Failed to write LastUpdate file. Will tray again in a few: " + e.Message);
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                    }
                }
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
