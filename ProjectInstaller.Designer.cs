namespace CiliaSDK
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ciliaSDKServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.ciliaSDKServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // SDKServiceProcessInstaller
            // 
            this.ciliaSDKServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.ciliaSDKServiceProcessInstaller.Password = null;
            this.ciliaSDKServiceProcessInstaller.Username = null;
            this.ciliaSDKServiceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.CiliaSDKServiceProcessInstaller_AfterInstall);
            // 
            // SDKServiceInstaller
            // 
            this.ciliaSDKServiceInstaller.ServiceName = "Cilia SDK Service";
            this.ciliaSDKServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.ciliaSDKServiceInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.CiliaSDKServiceInstaller_AfterInstall);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ciliaSDKServiceProcessInstaller,
            this.ciliaSDKServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller ciliaSDKServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller ciliaSDKServiceInstaller;
    }
}