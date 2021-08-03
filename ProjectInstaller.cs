using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace CiliaSDK
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        /**
         * Constructor that calls InitializeComponent in Project Installer Designer
         */
        public ProjectInstaller()
        {
            InitializeComponent();
        }
        /**
         * Starts Cilia SDK Service after install.
         */
        private void CiliaSDKServiceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            ServiceController ciliaService = new ServiceController("Cilia SDK Service");
            ciliaService.Start();
        }

        private void CiliaSDKServiceProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
