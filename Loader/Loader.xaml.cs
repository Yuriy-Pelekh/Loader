using System;
using System.Collections.Generic;
using System.Linq;

namespace Loader
{
    /// <summary>
    /// This class is the main loader UI and implements the ILoader interface
    /// </summary>
    public partial class Loader : ILoader
    {
        public Loader()
        {
            InitializeComponent();
        }

        #region ILoader Members

        // called when download of all packages starts(called once)
        public void InitCallback(List<Uri> packageSourceList)
        {
            // * note we only take the size here because if they are streaming sources the url will change later. 
            // so we get the real url's later in downloadStartCallback.
            this.packageSourceList = new Dictionary<Uri, ProgressCtrl>(packageSourceList.Count);
            packageDownloadCount = packageSourceList.Count;
        }

        // called when download of each package/file starts
        public void DownloadStartCallback(Uri packageSource)
        {
            // dynamically generate a progress control for each file we download and add it to UI
            var progressCtrl = new ProgressCtrl();
            var packageName = packageSource.ToString();
            if (packageName.Length > 50)
                packageName = ".." + packageName.Substring(packageName.Length - 50);
            progressCtrl.Name = packageName;
            progressCtrl.Blink.AutoReverse = true;
            progressCtrl.Blink.Begin();
            DownloadListStackCtrl.Children.Add(progressCtrl);
            packageSourceList.Add(packageSource, progressCtrl);
        }

        // called on download progress of each package/file
        public void DownloadProgressCallback(Uri packageSource, DownloadProgressEventArgs eventArgs)
        {
            float percentageDownloadedAcurate = ((float)eventArgs.BytesReceived / eventArgs.TotalBytesToReceive) * 100;
            var progressCtrl = packageSourceList[packageSource];
            //var packageName = progressCtrl.Name;
            progressCtrl.LoadingTextCtrl.Text = "Loading " + percentageDownloadedAcurate.ToString("0.0") + "%";
            progressCtrl.ProgressBarCtrl.Value = eventArgs.ProgressPercentage;
        }

        // called on download complete of each package/file
        public void DownloadCompleteCallback(Uri packageSource, DownloadCompleteEventArgs e)
        {
            packageDownloadCount--;
            // if download is complete set source to a package of our choice
            if (packageDownloadCount <= 0)
            {
                // ! note that for the demo's sake we are just setting the active xap to be the first xap on the list.
                //   you should probably modify this if you have more then one xap on the list !
                foreach (var source in packageSourceList.Keys.Where(source => source.ToString().EndsWith(".xap")))
                {
                    // this will unload the the loader from the page and cause the package source to become the active xap file on page
                    // ! this should be the last loader operation after that it will start the unload process !
                    XapUtil.SetCurrentXapFile(source);
                    break;
                }
            }
        }

        #endregion

        // data
        private Dictionary<Uri, ProgressCtrl> packageSourceList;
        private int packageDownloadCount;
    }
}
