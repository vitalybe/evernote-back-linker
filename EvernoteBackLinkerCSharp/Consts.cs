namespace EvernoteBackLinkerCSharp
{
    class Consts
    {
        private const string Shard = "s6";
        private const string UserId = "731386";

        public const string EvernoteDevToken = "***REMOVED***";

        public static readonly string ExternalNoteUrlPrefix = string.Format("https://www.evernote.com/shard/{0}/nl/{1}/", Shard, UserId);
        public static readonly string InternalNoteUrlPrefix = string.Format("evernote:///view/{1}/{0}/", Shard, UserId);

        public const string BacklinkPrefix = "[[[Backlink:&nbsp;";
        public const string BacklinkSufix = "]]]";
    }
}