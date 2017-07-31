using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeekAndArchive
{
    class Program
    {
        static List<FileInfo> FoundFiles;
        static List<FileSystemWatcher> watchers;
        static List<DirectoryInfo> archiveDirs;

        static void Main(string[] args)
        {
            watchers = new List<FileSystemWatcher>();
            string fileName = args[0];
            string directoryName = args[1];
            FoundFiles = new List<FileInfo>();

            //examine if the given directory exists at all 
            DirectoryInfo rootDir = new DirectoryInfo(directoryName);
            if (!rootDir.Exists)
            {
                Console.WriteLine("The specified directory does not exist.");
                return;
            }

            //search recursively for the mathing files
            RecursiveSearch(FoundFiles, fileName, rootDir);
            //list the found files
            Console.WriteLine("Found {0} files.", FoundFiles.Count);
            foreach (FileInfo fil in FoundFiles)
            {
                Console.WriteLine("{0}", fil.FullName);
            }
            Console.ReadKey();

            foreach (FileInfo fil in FoundFiles)
            {
                FileSystemWatcher newWatcher = new FileSystemWatcher(fil.DirectoryName, fil.Name);
                newWatcher.Changed += new FileSystemEventHandler(WatcherChanged);
                newWatcher.EnableRaisingEvents = true;
                watchers.Add(newWatcher);
            }

            archiveDirs = new List<DirectoryInfo>();
            //create archive directories
            for (int i = 0; i < FoundFiles.Count; i++)
            {
                archiveDirs.Add(Directory.CreateDirectory("archive" + i.ToString()));
            }

            IsolatedStorageFile userStore = IsolatedStorageFile.GetUserStoreForAssembly();
            IsolatedStorageFileStream userStream = new IsolatedStorageFileStream("UserSettings.set", FileMode.Create, userStore);

            StreamWriter userWriter = new StreamWriter(userStream);
            userWriter.WriteLine("User Prefs");
            userWriter.Close();

            string[] files = userStore.GetFileNames("UserSettings.set");
            if (files.Length == 1)
            {
                userStream = new IsolatedStorageFileStream("UserSettings.set", FileMode.Open, userStore);
                StreamReader userReader = new StreamReader(userStream);
                string contents = userReader.ReadToEnd();
            }
        }

        static void ArchiveFile(DirectoryInfo archiveDir, FileInfo fileToArchive)
        {
            FileStream input = fileToArchive.OpenRead();
            FileStream output = File.Create(archiveDir.FullName + @"\" + fileToArchive.Name + ".gz");
            GZipStream Compressor = new GZipStream(output, CompressionMode.Compress);
            int b = input.ReadByte();
            while (b != -1)
            {
                Compressor.WriteByte((byte)b);

                b = input.ReadByte();
            }
            Compressor.Close();
            input.Close();
            output.Close();
        }

        static void WatcherChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("{0} has been changed!", e.FullPath);
            //find the the index of the changed file 
            FileSystemWatcher senderWatcher = (FileSystemWatcher)sender;
            int index = watchers.IndexOf(senderWatcher, 0);
            //now that we have the index, we can archive the file 
            ArchiveFile(archiveDirs[index], FoundFiles[index]);
        }

        static void RecursiveSearch(List<FileInfo> foundFiles, string fileName, DirectoryInfo currentDirectory)
        {
            foreach (FileInfo fil in currentDirectory.GetFiles())
            {
                if (fil.Name == fileName)
                    foundFiles.Add(fil);
            }

            foreach (DirectoryInfo dir in currentDirectory.GetDirectories())
            {
                RecursiveSearch(foundFiles, fileName, dir);
            }
        }

    }
}
