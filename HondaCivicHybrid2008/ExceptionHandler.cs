using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Logger = CaledosLab.Portable.Logging.Logger;

namespace HondaCivicHybrid2008
{
    public static class ExceptionHandler
    {
        public static void LogException(Exception ex)
        {
            Logger.WriteLine(App.streamWriter, "Unhandled Exception");
            Logger.WriteLine(App.streamWriter, ex);
        }

        /// <summary>
        /// Handles failure for application exception on UI thread (or initiated from UI thread via async void handler)
        /// </summary>
        public static void HandleException(Exception ex)
        {
            if (ex.Message == "Object reference not set to an instance of an object")
            {
                Logger.WriteLine(App.streamWriter, ex.Message);
                App.connectionManager.SendCommand("ATZ");
            }
            else
            {
                LogException(ex);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(ex.Message);
                });
            }
        }

        /// <summary>
        /// Gets the error message to display from an exception
        /// </summary>
        public static string GetDisplayMessage(Exception ex)
        {
            string errorMessage;
#if DEBUG
                errorMessage = (ex.Message + " " + ex.StackTrace);
#else
            errorMessage = ex.Message;
#endif
            return errorMessage;
        }
    }
}
