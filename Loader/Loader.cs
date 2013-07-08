using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Windows.Browser;
using System.Windows.Threading;

namespace Loader
{
  /// <summary>
  /// Interface for loaders - every loader should inherit and implement this interface
  /// </summary>
  public interface ILoader
  {
    // init with list of packages to download
    void InitCallback(List<Uri> packageSourceList);
    // called when package download starts
    void DownloadStartCallback(Uri packageSource);
    // called when package download progresses
    void DownloadProgressCallback(Uri packageSource, DownloadProgressEventArgs eventArgs);
    // called when package download is complete
    void DownloadCompleteCallback(Uri packageSource, DownloadCompleteEventArgs eventArgs);
  }

  /// <summary>
  /// event classes
  /// </summary>
  public class DownloadProgressEventArgs : EventArgs
  {
    public DownloadProgressEventArgs()
    {

    }

    public DownloadProgressEventArgs(DownloadProgressChangedEventArgs e)
    {
      BytesReceived = e.BytesReceived;
      TotalBytesToReceive = e.TotalBytesToReceive;
    }
    // Summary:
    //     Gets the number of bytes received.
    //
    // Returns:
    //     An System.Int64 value that indicates the number of bytes received.
    public long BytesReceived { get; set; }
    // Summary:
    //     Gets the total number of bytes in a System.Net.WebClient data download operation.
    //
    // Returns:
    //     An System.Int64 value that indicates the number of bytes that will be received.
    public long TotalBytesToReceive { get; set; }
    // Summary:
    //     Gets the percentage of an asynchronous operation that has been completed.
    //
    // Returns:
    //     A percentage value that indicates the asynchronous operation progress.
    public int ProgressPercentage
    {
      get
      {
        return (int)Math.Round(((double)BytesReceived / TotalBytesToReceive) * 100);
      }
    }
  }

  public class DownloadCompleteEventArgs : EventArgs
  {
    public DownloadCompleteEventArgs()
    {

    }

    public DownloadCompleteEventArgs(OpenReadCompletedEventArgs e)
    {
      Cancelled = e.Cancelled;
      Error = e.Error;
      Result = e.Error == null ? e.Result : null;
    }
    // Summary:
    //     Gets a readable stream that contains the results of the System.Net.WebClient.OpenReadAsync(System.Uri,System.Object)
    //     method.
    //
    // Returns:
    //     A System.IO.Stream that contains the results of the System.Net.WebClient.OpenReadAsync(System.Uri,System.Object)
    //     method.
    //
    // Exceptions:
    //   System.InvalidOperationException:
    //     The asynchronous request was cancelled.
    public Stream Result { get; set; }
    // Summary:
    //     Gets a value that indicates whether an asynchronous operation has been canceled.
    //
    // Returns:
    //     true if the asynchronous operation has been canceled; otherwise, false. The
    //     default is false.
    public bool Cancelled { get; set; }
    // Summary:
    //     Gets a value that indicates which error occurred during an asynchronous operation.
    //
    // Returns:
    //     An System.Exception instance, if an error occurred during an asynchronous
    //     operation; otherwise null.
    public Exception Error { get; set; }
  }

  /// <summary>
  /// Package downloader class - takes care of the actual download of the packages
  /// </summary>
  public class PackageDownloader
  {
    /// <summary>
    /// download a package
    /// </summary>
    /// <param name="packageSource"></param>
    /// <param name="progressCallback"></param>
    public void Download(Uri packageSource, ILoader progressCallback)
    {
      Abort();
      progressCallbackInterface = progressCallback;
      this.packageSource = packageSource;
      progressCallbackInterface.DownloadStartCallback(packageSource);
      webClient = new WebClient();
      webClient.DownloadProgressChanged += OnDownloadProgressChanged;
      webClient.OpenReadCompleted += OnOpenReadCompleted;
      webClient.OpenReadAsync(packageSource);
    }

    public void Abort()
    {
      if (webClient != null)
      {
        webClient.DownloadProgressChanged -= OnDownloadProgressChanged;
        webClient.CancelAsync();
        webClient = null;
      }
    }

    protected void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
      progressCallbackInterface.DownloadProgressCallback(packageSource, new DownloadProgressEventArgs(e));
    }

    protected void OnOpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
    {
      progressCallbackInterface.DownloadCompleteCallback(packageSource, new DownloadCompleteEventArgs(e));
    }

    // data
    private WebClient webClient;
    private ILoader progressCallbackInterface;
    private Uri packageSource;
  }

  /// <summary>
  /// Package downloader simulator class - takes care of the simulated download of packages
  /// </summary>
  public class PackageDownloaderSimulator
  {

    public void Download(Uri packageSource, ILoader progressCallback, float maxTransferRateKb)
    {
      Abort();
      progressCallbackInterface = progressCallback;
      this.packageSource = packageSource;
      progressCallbackInterface.DownloadStartCallback(packageSource);
      webClient = new WebClient();
      webClient.DownloadProgressChanged += OnDownloadProgressChanged;
      webClient.OpenReadCompleted += OnOpenReadCompleted;
      webClient.OpenReadAsync(packageSource);
      // simulate misc
      InitTimer();
      maxTransferRateKbs = maxTransferRateKb;
    }

    private void InitTimer()
    {
      //_simulationStartTime = DateTime.Now;
      timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
      timer.Tick += OnTimerTick;
      timer.Start();
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
      // make sure we got a first call initializing the data -> total dl size etc.
      if (dlProgressEventArgs == null)
        return;
      //float currentTransferRate = m_dlProgressEvent.BytesReceived / DateTime.Now.Subtract(m_simulationStartTime).Seconds;
      dlProgressEventArgs.BytesReceived += (long)(maxTransferRateKbs * 1024);
      if (dlProgressEventArgs.BytesReceived >= dlProgressEventArgs.TotalBytesToReceive)
      {
        // set progress info max size
        dlProgressEventArgs.BytesReceived = dlProgressEventArgs.TotalBytesToReceive;
        // make sure we first got a download complete call initializing the data -> result
        if (dlCompleteEventArgs == null)
          return;
        // stop timer so we don't get any more calls after this point
        timer.Stop();
        // call both progress and complete to simulate current real behaviour...
        progressCallbackInterface.DownloadProgressCallback(packageSource, dlProgressEventArgs);
        progressCallbackInterface.DownloadCompleteCallback(packageSource, dlCompleteEventArgs);
      }
      else
        progressCallbackInterface.DownloadProgressCallback(packageSource, dlProgressEventArgs);
    }

    public void Abort()
    {
      if (timer != null)
      {
        timer.Tick -= OnTimerTick;
        timer.Stop();
        timer = null;
      }

      if (webClient != null)
      {
        webClient.DownloadProgressChanged -= OnDownloadProgressChanged;
        webClient.CancelAsync();
        webClient = null;
      }
    }

    protected void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
    {
      if (dlProgressEventArgs == null)
      {
        dlProgressEventArgs = new DownloadProgressEventArgs(e) { BytesReceived = 0 };
      }
    }

    protected void OnOpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
    {
      dlCompleteEventArgs = new DownloadCompleteEventArgs(e);
    }

    // data
    /////////////////////
    // simulate interval misc
    /////////////////////
    private float maxTransferRateKbs;
    private DownloadProgressEventArgs dlProgressEventArgs;
    private DownloadCompleteEventArgs dlCompleteEventArgs;
    //private DateTime _simulationStartTime;
    ////////////////////
    private WebClient webClient;
    private DispatcherTimer timer;
    private ILoader progressCallbackInterface;
    private Uri packageSource;
  }

  /// <summary>
  /// Manages the package list download process - takes a package list and loader interface as parameters and manage the download process
  /// </summary>
  public class PackageDownloadManager
  {
    public PackageDownloadManager(ILoader loader, IDictionary<string, string> initParams, float maxTransferRateKb)
    {
      // parse the package source list from init params
      var packageFileList = ParamUtil.InitParamsToUriList(initParams);

      if (packageFileList == null)
        return;

      ParamUtil.FixRelativeLinks(ref packageFileList);
      Init(loader, packageFileList, maxTransferRateKb);
    }

    public PackageDownloadManager(ILoader loader, List<Uri> packageSourceList, float maxTransferRateKb)
    {
      Init(loader, packageSourceList, maxTransferRateKb);
    }

    private void Init(ILoader loader, List<Uri> packageSourceList, float maxTransferRateKb)
    {
      // save transfer rate if any
      this.maxTransferRateKb = maxTransferRateKb;
      // save loader callback interface
      this.loader = loader;
      // call init callback with package list
      this.loader.InitCallback(packageSourceList);
      // iterate through list and start downloading the files
      foreach (var packageSourceFile in packageSourceList)
      {
        if (!IsStreamingUri(packageSourceFile))
        {
          if (maxTransferRateKb > 0)
            SimulateFileDownload(packageSourceFile);
          else
            DownloadFile(packageSourceFile);
        }
        else
        {
          // check if simulate mode is on
          var gmsCallback = maxTransferRateKb > 0
                                ? new SilverlightStreamingUtil.GetMediaStreamUriCallback(SimulateFileDownload)
                                : new SilverlightStreamingUtil.GetMediaStreamUriCallback(DownloadFile);
          // start the streaming media url process
          var streamUtil = new SilverlightStreamingUtil(gmsCallback);
          streamUtil.GetMediaStreamUri(packageSourceFile.ToString());
        }
      }
    }

    // the uri scheme for the streaming protocol -> note this should be changed in init params if changed here...
    private const string UriSchemeStreaming = "streaming:";

    // check if it is a streaming uri
    private bool IsStreamingUri(Uri packageSourceFile)
    {
      return packageSourceFile.ToString().StartsWith(UriSchemeStreaming);
    }

    // download file async
    public void DownloadFile(Uri source)
    {
      var downloader = new PackageDownloader();
      downloader.Download(source, loader);
    }

    // simulate file download for easier testing of the loader
    private void SimulateFileDownload(Uri source)
    {
      var downloader = new PackageDownloaderSimulator();
      //var totalDlTime = new TimeSpan(0, 0, 0, 10);
      downloader.Download(source, loader, maxTransferRateKb);
    }

    // data
    private ILoader loader;
    private float maxTransferRateKb;
  }

  /// <summary>
  /// Utility class for manipulating XAP files
  /// </summary>
  public class XapUtil
  {
    // set the source of the current silverlight plugin 
    // note that changing this will effectively change the XAP file that is currently in use!
    // the new XAP will be initialized and displayed in the browser replacing the old XAP source
    public static void SetCurrentXapFile(Uri packageSource)
    {
      var path = packageSource.ToString();

      if (TemplateParameter.TemplateName != null)
        HtmlPage.Plugin.SetProperty("initParams", TemplateParameter.TemplateKey + "=" + TemplateParameter.TemplateName);

      HtmlPage.Plugin.SetProperty("source", path);
    }

    // get the source of the current silverlight plugin 
    public static Uri GetCurrentXapFile()
    {
      var xapSource = (string)HtmlPage.Plugin.GetProperty("source");
      return new Uri(xapSource);
    }
  }
  /// <summary>
  /// Utility class for parsing parameter strings
  /// </summary>
  public class ParamUtil
  {
    // the key used in init params to tell us what the package download sources are
    private const string LoaderSourceKeyName = "LoaderSourceList";
    // delimiters for package source list -> file1;file2;...
    private static readonly string[] FileListDelimeters = { ";" };

    public static List<Uri> DelimitedStringListToUriList(string delimitedStringList)
    {
      var tempStrList = delimitedStringList.Split(FileListDelimeters, StringSplitOptions.RemoveEmptyEntries);
      var xapUriFileList = new List<Uri>(tempStrList.Length);
      #region Same as LINQ-expression
      //foreach (var xapFile in tempStrList)
      //    xapUriFileList.Add(new Uri(xapFile, UriKind.RelativeOrAbsolute));
      #endregion
      xapUriFileList.AddRange(tempStrList.Select(xapFile => new Uri(xapFile, UriKind.RelativeOrAbsolute)));
      return xapUriFileList;
    }

    public static List<Uri> InitParamsToUriList(IDictionary<string, string> initParams)
    {
      if (initParams.ContainsKey(LoaderSourceKeyName))
      {
        var delimitedStringList = initParams[LoaderSourceKeyName];
        return DelimitedStringListToUriList(delimitedStringList);
      }
      return null;
    }

    public static void FixRelativeLinks(ref List<Uri> xapUriFileList)
    {
      var basePath = HtmlPage.Document.DocumentUri.ToString();
      basePath = basePath.Remove(basePath.LastIndexOf('/'));
      // if its not terminated by a '/' add one, otherwise URI constructor does not build xap path correctly
      if (basePath[basePath.Length - 1] != ('/'))
        basePath += '/';
      // modify all relative links...
      // !!! note that stream: url's are absolute url's
      for (var i = 0; i < xapUriFileList.Count; i++)
      {
        if (!xapUriFileList[i].IsAbsoluteUri)
        {
          var xapFile = new Uri(new Uri(basePath), xapUriFileList[i]);
          xapUriFileList[i] = xapFile;
        }
      }
    }
  }

  /// <summary>
  /// Silverlight streaming service utility class - 
  /// helps manage downloading files hosted on the Microsoft silverlight streaming service (http://dev.live.com/silverlight/)
  /// </summary>
  public class SilverlightStreamingUtil
  {
    // declare a delegate type for getMediaStream result
    public delegate void GetMediaStreamUriCallback(Uri mediaUrl);

    // define the Microsoft Silverlight Streaming service base address
    private const string SLSMediaServiceRoot = "http://silverlight.services.live.com/";

    public SilverlightStreamingUtil(GetMediaStreamUriCallback getMediaStreamUriCallback)
    {
      this.getMediaStreamUriCallback = getMediaStreamUriCallback;
    }

    // get a media stream uri from a stream id
    // this is done by sending a specially crafted request to the streaming service
    // the streamId contains accountId, appName and a fileName if the request if for a non xap resource.
    // example: /57870/TestVideo/video.wmv
    // example: /57870/LoaderTestApp/
    public void GetMediaStreamUri(string streamId)
    {
      var infoMatch = Regex.Match(streamId, "/(.*)/(.*)/(.*)");
      var accountId = infoMatch.Groups[1].Value;
      var appName = infoMatch.Groups[2].Value;
      var fileName = infoMatch.Groups[3].Value;
      GetMediaStreamUri(accountId, appName, fileName);
    }

    // get a media stream uri from accountId, appName and a fileName if the request if for a non xap resource.
    public void GetMediaStreamUri(string accountId, string appName, string fileName)
    {
      var t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
      var timestamp = t.TotalMilliseconds;
      var milliseconds = timestamp.ToString().Split(new[] { '.' })[0];

      // check if we are requesting a media file or a xap application
      // because for some reason the format is different...
      var targetUri = fileName.Length != 0
                          ? new Uri(SLSMediaServiceRoot +
                                    string.Format("invoke/local/starth.js?id=bl2&u={0}&p0=/{1}/{2}/{3}",
                                                  milliseconds, accountId, appName, fileName))
                          : new Uri(SLSMediaServiceRoot +
                                    string.Format("invoke/{1}/{2}/starth.js?id=bl2&u={0}", milliseconds, accountId,
                                                  appName));

      var webClient = new WebClient();
      webClient.DownloadStringAsync(targetUri);
      webClient.DownloadStringCompleted += WebClientDownloadStringCompleted;
    }

    // json data struct returned by streaming svc
    // example: {"version": "2.0", "name": "LoadTest", "width": "800", "height": "600", "source": "LoadTest.xap"}
    public class SilverlightStreamData
    {
      public string Version { get; set; }
      public string Name { get; set; }
      public string Width { get; set; }
      public string Height { get; set; }
      public string Source { get; set; }
    }

    // download string completed callback
    private void WebClientDownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
    {
      if (e.Error == null)
      {
        var scriptResponse = e.Result;
        // get first part with base url or media url
        var mediaMatch = Regex.Match(scriptResponse, "http:.+.\"");
        var mediaUrl = mediaMatch.Value.Remove(mediaMatch.Value.Length - 1);

        // get the json part if any -> applies to xap packages
        var jsonPart = Regex.Match(scriptResponse, "{.+}").Value;
        // if we do have a xap package the get the source info so we can build the url
        if (jsonPart.Length > 0)
        {
          var ser = new DataContractJsonSerializer(typeof(SilverlightStreamData));
          var memStream = new MemoryStream(System.Text.Encoding.Unicode.GetBytes(jsonPart));
          var slsData = (SilverlightStreamData)ser.ReadObject(memStream);
          mediaUrl += "/" + slsData.Source;
        }
        // call callback with result
        getMediaStreamUriCallback(new Uri(mediaUrl));
      }
    }

    // data
    private readonly GetMediaStreamUriCallback getMediaStreamUriCallback;
  }
}
