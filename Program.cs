//#define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace CiliaSDK
{
    /**
     * Program has main function for launching our Cilia Server
     */
    static class Program
    {
        /**
        * Luanches Cilia SDK Service.
        * Uncomment DEBUG defenition to run without installing.
        */
        static void Main()
        {
#if DEBUG
            CiliaSDKService CiliaService = new CiliaSDKService();
            
            CiliaService.DebugStart();
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#else
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new CiliaSDKService()
            };
            ServiceBase.Run(ServicesToRun);
            System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
#endif
        }
    }
}
//