# Deprecated

I rewrote this in python here and it works better too: https://github.com/vitalybe/evernote-back-linker-2

# ~~Evernote back-linker~~

## ~~Overview~~

~~Evernote has an ability to create a link from one note to another. It is great for creating "Tables of contents", main vs sub pages and in general for any note organizaiton. However, when you open a note that might be linked from somewhere else, you will have no idea where it is linked from.~~

~~This application scans all your notes and creates a backlink at the top of the notes that link to it.~~

~~For example, if you create a note **All about my car**, add some text and then create a link to another note **Car history** and then run the app, it will add a backlink, e.g:~~

> ~~**This is my Car history**~~
  
> ~~[[[Backlink: All about my car]]]~~

> ~~This is a long note about my car history~~

## ~~Running~~

~~To use, perform the following:~~

1. ~~Compile in Visual Studio~~
2. ~~If you don't have a developer token, create one here: https://www.evernote.com/api/DeveloperToken.action~~
3. ~~Run the resulting exe with the token, e.g: `EvernoteBackLinkerCSharp.exe S=s6:U=b...`~~
4. ~~For best results, add a Windows Scheduler task to run it every hour (when idle).~~

~~**Note:** The first run will take a while if you have a lot of notes due to Evernote rate-limits. The subsequent runs should be a lot faster, however, since it will only scan the newly modified notes.~~
