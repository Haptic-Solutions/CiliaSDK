using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace CiliaSDK
{
    class CiliaPipeReceiver
    {
        /*Member Variables*/
        private static CiliaManager mCiliaManager;
        private static bool mShutdown = false;
        /**
         * Sets mShutdown to true so that infinite loops can close.
         */
        public static void ShutDown()
        {
            mShutdown = true;
        }
        /**
         * Sets static variable mCiliaManager.
         * @param aCiliaManager to set mCiliaManager.
         */
        public static void SetCiliaManager(CiliaManager aCiliaManager)
        {
            mCiliaManager = aCiliaManager;
        }
        /**
         * Used to handle fake when new fake Cilias startup and shut down.
         * Attaches and Removes these fake Cilias.
         */
        public static void FakeCiliaEventHandler()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSid, null), PipeAccessRights.FullControl, AccessControlType.Allow));
            
            while (!mShutdown)
            {
                using (NamedPipeServerStream ciliaPipeServer =
                new NamedPipeServerStream("ciliapipe", PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.None, 1, 1, pipeSecurity))
                {

                    ciliaPipeServer.WaitForConnection();
                    using (StreamReader streamReader = new StreamReader(ciliaPipeServer))
                    {
                        string ciliaMessage;
                        while ((ciliaMessage = streamReader.ReadLine()) != null)
                        {
                            string[] ciliaMessages = ciliaMessage.Split(',');
                            if (ciliaMessages[1].Equals("Attach"))
                            {
                                mCiliaManager.CiliaAttach(ciliaMessages[0]);
                            }
                            else if (ciliaMessages[1].Equals("Detach"))
                            {
                                mCiliaManager.CiliaDetach(ciliaMessages[0]);
                            }
                        }
                    }
                    ciliaPipeServer.Close();
                }
            }
        }
    }
}
