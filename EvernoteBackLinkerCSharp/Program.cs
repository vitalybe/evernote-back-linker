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
            EvernoteNoteSource evernote = new EvernoteNoteSource();
            var note = evernote.FindById("0e2cc3f9-8695-44c9-be56-dfb8b120ecd4");
            var backlinks = note.FindBacklinks();

            //note.AddBacklink("From C#!", "https://www.evernote.com/shard/s6/nl/731386/d87d2d13-1982-4279-b630-dee24a176f10");
        }
    }
}
