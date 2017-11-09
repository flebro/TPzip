using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using TPzip.Core;

namespace TPzip.Service
{
    partial class AutoZipService : ServiceBase
    {
        #region Fields

        private AutoZip _AutoZip;

        #endregion

        #region Constructor

        public AutoZipService()
        {
            InitializeComponent();
            _AutoZip = new AutoZip();
        }

        #endregion

        #region Methods

        protected override void OnContinue() => _AutoZip.Resume();

        protected override void OnPause() => _AutoZip.Pause();

        protected override void OnStart(string[] args) => _AutoZip.Start();

        protected override void OnStop() => _AutoZip.Stop();
        
        #endregion

    }
}
