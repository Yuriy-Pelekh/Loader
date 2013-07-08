using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Browser;

namespace Loader
{
    public partial class App
    {
        public App()
        {
            Startup += ApplicationStartup;
            Exit += ApplicationExit;
            UnhandledException += ApplicationUnhandledException;

            InitializeComponent();
        }

        private void ApplicationStartup(object sender, StartupEventArgs e)
        {
            // create a loader page
            var loader = new Loader();

            // set the visual root to loader page
            RootVisual = loader;

            // create package download manager and start the download process using html supplied InitParams
            // note the last param sets a 50KB max download speed for debugging and simulation mode!
            
            // Example for debugging
            //e.InitParams.Add("LoaderSourceList", "Shell.xap;MockupBuilder.xap");
            //e.InitParams.Add("Template", "myTemplate");

            TemplateParameter.GetTemaplate(e.InitParams);
            
            var pdm = new PackageDownloadManager(loader, e.InitParams, 0);
            // another option is to use a hand coded list ->
            //List<Uri> myDownloadList = new List<Uri>();
            //myDownloadList.Add(new Uri("ClientBin/Beegger.xap", UriKind.RelativeOrAbsolute));
            //ParamUtil.fixRelativeLinks(ref myDownloadList);
            //PackageDownloadManager pdm = new PackageDownloadManager(loader, myDownloadList, 50);
        }

        private void ApplicationExit(object sender, EventArgs e)
        {

        }
        
        private void ApplicationUnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if (!Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
                Deployment.Current.Dispatcher.BeginInvoke(() => ReportErrorToDOM(e));
            }
        }
        
        private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                var errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

                HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight 2 Application " + errorMsg + "\");");
            }
            catch
            {

            }
        }
    }
}
