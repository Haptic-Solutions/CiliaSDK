using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CiliaSDK
{
    public partial class CiliaSDKService : ServiceBase
    {
        Thread CiliaSDKThread;
        CiliaSDK mCiliaServer;

        /**
         * Runs InitializeComponent in SDK Service Designer.
         */
        public CiliaSDKService()
        {
            InitializeComponent();
        }
        /**
         * Runs OnStart with no parameters.
         */
        public void DebugStart()
        {
            OnStart(null);
        }
        /**
         * On Start create a new thread running StartSDK function.
         */
        protected override void OnStart(string[] args)
        {
            try
            {
                mCiliaServer = new CiliaSDK();
                CiliaSDKThread = new Thread(mCiliaServer.StartSDK);
                CiliaSDKThread.Start();
            }
            catch
            {

            }
        }
        /**
         * When the service stops join the SDK thread.
         */
        protected override void OnStop()
        {
            try
            {
                mCiliaServer.ShutDown();
                CiliaSDKThread.Join();
            }
            catch
            {

            }
        }


    }
}
