using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace Shortcut_Copy_Tool.ViewModel
{
    public class FilesForCopy
    {
        public string phyPath { get; set; }
        public string tree { get; set; }
        public string display { get; set; }
    }

    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region Attributes
        public const string IncDirTreeName = "IncDirTree";
        private bool _IncDirTree = false;
        public bool IncDirTree
        {
            get { return _IncDirTree; }
            set
            {
                if (_IncDirTree = value) return;
                _IncDirTree = value;
                RaisePropertyChanged(IncDirTreeName);
            }
        }
        
        public const string FolderSourceName = "FolderSource";
        private string _FolderSource = string.Empty;
        public string FolderSource
        {
            get { return _FolderSource; }
            set
            {
                if (_FolderSource == value) return;
                _FolderSource = value;
                RaisePropertyChanged(FolderSourceName);
            }
        }

        public const string FolderDestinationName = "FolderDestination";
        private string _FolderDestination = string.Empty;
        public string FolderDestination
        {
            get { return _FolderDestination; }
            set
            {
                if (_FolderDestination == value) return;
                _FolderDestination = value;
                RaisePropertyChanged(FolderDestinationName);
            }
        }

        public const string ScanEnabledName = "ScanEnabled";
        private bool _ScanEnabled;
        public bool ScanEnabled
        {
            get { return _ScanEnabled; }
            set
            {
                if (_ScanEnabled == value) return;
                _ScanEnabled = value;
                RaisePropertyChanged(ScanEnabledName);
            }
        }

        public const string CopyEnabledName = "CopyEnabled";
        private bool _CopyEnabled;
        public bool CopyEnabled
        {
            get { return _CopyEnabled; }
            set
            {
                if (_CopyEnabled == value) return;
                _CopyEnabled = value;
                RaisePropertyChanged(CopyEnabledName);
            }
        }

        public const string FilesListName = "FilesList";
        private ConcurrentBag<FilesForCopy> _FilesList = new ConcurrentBag<FilesForCopy>();
        public ConcurrentBag<FilesForCopy> FilesList
        {
            get { return _FilesList; }
            set
            {
                if (_FilesList == value) return;
                _FilesList = value;
                RaisePropertyChanged(FilesListName);
            }
        }

        public const string FilesCountName = "FilesCount";
        private long _FilesCount = 0;
        public long FilesCount
        {
            get { return _FilesCount; }
            set
            {
                if (_FilesCount == value) return;
                _FilesCount = value;
                RaisePropertyChanged(FilesCountName);
            }
        }
        #endregion

        private long size;

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            BrowseSourceCommand = new RelayCommand(BrowseSourceExecute, () => true);
            BrowseDestinationCommand = new RelayCommand(BrowseDestinationExecute, () => true);
            ScanCommand = new RelayCommand(ScanExecute, () => true);
            CopyCommand = new RelayCommand(CopyExecute, () => true);

            this.PropertyChanged += MainViewModel_PropertyChanged;

            ScanEnabled = false;
            if (Properties.Settings.Default.SourceDirectory != string.Empty) FolderSource = Properties.Settings.Default.SourceDirectory;
            if (Properties.Settings.Default.CopyDirectory != string.Empty) FolderDestination = Properties.Settings.Default.CopyDirectory;
            IncDirTree = Properties.Settings.Default.IncDirTree;

        }

        public void MainViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Check for changed properties
            switch (e.PropertyName)
            {
                case FolderSourceName:
                    //Check for folder, save last used if it exists and execute scan
                    if (System.IO.Directory.Exists(FolderSource))
                    {
                        Properties.Settings.Default.SourceDirectory = FolderSource;
                        Properties.Settings.Default.Save();
                        ScanEnabled = true;
                    }
                    else ScanEnabled = false;
                    break;

                case FolderDestinationName:
                    //Check destination exists and we have thigns to copy
                    CheckDestFolder();
                    break;
                
                case FilesListName:
                    //Has the files list changed
                    FilesCount = FilesList.Count;
                    if (Directory.Exists(FolderDestination) && FilesCount > 0 && FolderDestination != FolderSource) CopyEnabled = true;
                    else CopyEnabled = false;
                    break;

                case IncDirTreeName:
                    Properties.Settings.Default.IncDirTree = IncDirTree;
                    Properties.Settings.Default.Save();
                    break;
            }
        }

        /// <summary>
        /// Check if the destination folder exists
        /// </summary>
        public void CheckDestFolder()
        {
            bool testEnable = false;
            if (Directory.Exists(FolderDestination))
            {
                testEnable = true;
            }
            else
            {
                //We dont have a folder that exists, should we create it?
                using (Ookii.Dialogs.Wpf.TaskDialog td = new Ookii.Dialogs.Wpf.TaskDialog())
                {
                    td.WindowTitle = "Folder Does not Exist";
                    td.MainInstruction = "Folder Does not Exist";
                    td.Content = "Do you wish to create this folder?";
                    td.MainIcon = Ookii.Dialogs.Wpf.TaskDialogIcon.Warning;
                    Ookii.Dialogs.Wpf.TaskDialogButton butOk = new Ookii.Dialogs.Wpf.TaskDialogButton(Ookii.Dialogs.Wpf.ButtonType.Ok);
                    Ookii.Dialogs.Wpf.TaskDialogButton butCan = new Ookii.Dialogs.Wpf.TaskDialogButton(Ookii.Dialogs.Wpf.ButtonType.Cancel);
                    td.Buttons.Add(butOk);
                    td.Buttons.Add(butCan);
                    Ookii.Dialogs.Wpf.TaskDialogButton selection = td.ShowDialog();
                    if (selection == butOk)
                    {
                        //Create directory
                        Directory.CreateDirectory(FolderDestination);
                        testEnable = true;
                    }
                    else
                    {
                        CopyEnabled = false;
                    }
                }
            }
            //We are testing if we enable copy (and save settings)
            if (testEnable)
            {
                Properties.Settings.Default.CopyDirectory = FolderDestination;
                Properties.Settings.Default.Save();
                if (FilesList.Count > 0 && FolderDestination != FolderSource) CopyEnabled = true;
                else CopyEnabled = false;
            }
        }

        #region Commands
        public RelayCommand BrowseSourceCommand { get; private set; }
        private void BrowseSourceExecute()
        {
            FolderSource = GetDirectory(FolderSource);
        }

        public RelayCommand BrowseDestinationCommand { get; private set; }
        private void BrowseDestinationExecute()
        {
            FolderDestination = GetDirectory(FolderDestination);
        }

        #region Copy
        Ookii.Dialogs.Wpf.ProgressDialog progress;
        public RelayCommand CopyCommand { get; private set; }
        private void CopyExecute()
        {
            if (Directory.Exists(FolderDestination))
            {
                using (progress = new Ookii.Dialogs.Wpf.ProgressDialog())
                {
                    progress.WindowTitle = "Copying Files";
                    progress.Text = "Copying " + FilesList.Count + " Items (" + BytesToString(size) + ")";
                    progress.ShowTimeRemaining = true;
                    progress.DoWork += new System.ComponentModel.DoWorkEventHandler(progress_DoWork);
                    progress.Show();
                }
            }
        }

        /// <summary>
        /// Do the copies, 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void progress_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            CopyEnabled = false;
            bool fail;
            int fileCounter;
            long count = 0;
            int percent = 0;
            string outname;
            string dest = FolderDestination + System.IO.Path.DirectorySeparatorChar;
            string finalDest = string.Empty;
            //Go through files in list and copy them
            foreach (FilesForCopy f in FilesList)
            {
                if (progress.CancellationPending) break;
                //Update Progress Bar
                percent = (int)System.Math.Round((((double)count / (double)FilesList.Count) * 100));
                progress.ReportProgress(percent, null, f.phyPath.Replace(FolderSource, ""));

                //Are we including the directory tree
                if (IncDirTree)
                {
                    //Create path
                    string dirPath = Path.Combine(dest, f.tree);
                    //Check path exists
                    if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);
                    finalDest = Path.Combine(dirPath, Path.GetFileName(f.phyPath));
                }
                else finalDest = dest;

                //If the destination exists then inc counter to append to the name so as to not overwrite
                if (File.Exists(finalDest + Path.GetFileName(f.phyPath)))
                {
                    fileCounter = 0;
                    fail = true;
                    while (fail)
                    {
                        if (!File.Exists(finalDest + Path.GetFileNameWithoutExtension(f.phyPath) + fileCounter + Path.GetExtension(f.phyPath))) break;
                        fileCounter++;
                    }
                    //Save the new output name
                    outname = finalDest + Path.GetFileNameWithoutExtension(f.phyPath) + fileCounter + Path.GetExtension(f.phyPath);
                }
                else outname = finalDest + Path.GetFileName(f.phyPath);  //If the destination does not exists then fire away!
                //Copy the File
                CopyFile(f.phyPath, outname, true);
                count++;
            }
            CopyEnabled = true;
        }
        #endregion

        #region Scan Command
        private ConcurrentBag<FilesForCopy> tmpFiles;
        public RelayCommand ScanCommand { get; private set; }
        public void ScanExecute()
        {
            //Get source files
            if (Directory.Exists(FolderSource))
            {
                tmpFiles = new ConcurrentBag<FilesForCopy>();
                size = 0;
                ScanPath(FolderSource, FolderSource, tmpFiles);
                FilesList = tmpFiles;
            }
        }

        private void ScanPath(string path, string lnkPath, ConcurrentBag<FilesForCopy> fileList)
        {
            //file or folder
            try
            {
                FileAttributes attr = File.GetAttributes(path);
                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    //Directory - scan each file and directory in that directory
                    //foreach (string currentFile in Directory.GetFiles(path))
                    Parallel.ForEach<string>(Directory.GetFiles(path), currentFile =>
                        {
                            ScanPath(currentFile, lnkPath, fileList);
                        });
                    //foreach (string currentDirectory in Directory.GetDirectories(path))
                    Parallel.ForEach<string>(Directory.GetDirectories(path), currentDirectory =>
                        {
                            string dirname = Path.GetFileName(currentDirectory);
                            ScanPath(currentDirectory, System.IO.Path.Combine(lnkPath, dirname), fileList);
                        });
                }
                else
                {
                    //File
                    if (Path.GetExtension(path) == ".lnk")   //Is the file a shortcut
                    {
                        //If its a shortcut take the target and test *that*
                        IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                        IWshRuntimeLibrary.IWshShortcut link;
                        link = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(path);
                        
                        FileAttributes lnkattr = File.GetAttributes(link.TargetPath);
                        if ((lnkattr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            ScanPath(link.TargetPath, System.IO.Path.Combine(lnkPath, Path.GetFileNameWithoutExtension(link.TargetPath)), fileList);
                        }
                        else
                        {
                            FileInfo fi = new FileInfo(link.TargetPath);
                            FilesForCopy f = new FilesForCopy();
                            f.phyPath = link.TargetPath;
                            f.tree = lnkPath.Replace(FolderSource + System.IO.Path.DirectorySeparatorChar, string.Empty);
                            f.display = Path.Combine(f.tree, Path.GetFileName(f.phyPath));
                            tmpFiles.Add(f);
                            Interlocked.Add(ref size, fi.Length);
                        }
                    }
                    else //if we have a file add it ot the list
                    {
                        FileInfo fi = new FileInfo(path);
                        FilesForCopy f = new FilesForCopy();
                        f.tree = lnkPath.Replace(FolderSource + System.IO.Path.DirectorySeparatorChar, string.Empty);
                        f.phyPath = path;
                        f.display = Path.Combine(f.tree, Path.GetFileName(f.phyPath));
                        tmpFiles.Add(f);
                        Interlocked.Add(ref size, fi.Length);
                    }

                }
            }
            catch { }
        }
        #endregion
        #endregion

        /// <summary>
        /// Get Directory Selection Off User
        /// </summary>
        /// <returns></returns>
        private string GetDirectory(string startPath)
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog fd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            fd.Description = "Select Folder with Links";
            if (startPath != null && Directory.Exists(startPath)) fd.SelectedPath = startPath;
            if (fd.ShowDialog() == true)
            {
                return fd.SelectedPath;
            }
            else return startPath;
        }

        static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }

        const int CopyBufferSize = 512 * 1024;
        static void CopyFile(string src, string dest, bool overwrite)
        {
            if (overwrite)
            {
                if (File.Exists(dest)) File.Delete(dest);
            }
            using (var outputFile = File.Create(dest))
            {
                using (var inputFile = File.OpenRead(src))
                {
                    // we need two buffers so we can ping-pong
                    var buffer1 = new byte[CopyBufferSize];
                    var buffer2 = new byte[CopyBufferSize];
                    var inputBuffer = buffer1;
                    int bytesRead;
                    IAsyncResult writeResult = null;
                    while ((bytesRead = inputFile.Read(inputBuffer, 0, CopyBufferSize)) != 0)
                    {
                        // Wait for pending write
                        if (writeResult != null)
                        {
                            writeResult.AsyncWaitHandle.WaitOne();
                            outputFile.EndWrite(writeResult);
                            writeResult = null;
                        }
                        // Assign the output buffer
                        var outputBuffer = inputBuffer;
                        // and swap input buffers
                        inputBuffer = (inputBuffer == buffer1) ? buffer2 : buffer1;
                        // begin asynchronous write
                        writeResult = outputFile.BeginWrite(outputBuffer, 0, bytesRead, null, null);
                    }
                    if (writeResult != null)
                    {
                        writeResult.AsyncWaitHandle.WaitOne();
                        outputFile.EndWrite(writeResult);
                    }
                }
            }
        }
    }
}