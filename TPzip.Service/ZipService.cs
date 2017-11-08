using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TPzip.Service
{
    partial class ZipService : ServiceBase
    {
        public ZipService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: ajoutez ici le code pour démarrer votre service.
        }

        protected override void OnStop()
        {
            // TODO: ajoutez ici le code pour effectuer les destructions nécessaires à l'arrêt de votre service.
        }
    }
}
