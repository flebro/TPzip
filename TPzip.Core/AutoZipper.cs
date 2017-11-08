using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace TPzip.Core
{
    public class AutoZipper : IDisposable
    {
        private static Logger LOGGER = Logging.Logger;

        #region Constantes

        private const string DEFAULT_INPUT_PATH = "%temp%/AutoZip/Input";
        private const string DEFAULT_OUTPUT_PATH = "%temp%/AutoZip/Output";

        #endregion

        #region Attributs

        private FileSystemWatcher fileSystemWatcher;
        private string inputPath;
        private string outputPath;
        private Task zipTask;
        private DateTime lastUpdate;
        private CancellationTokenSource cancellationTokenSource;

        #endregion

        #region Propriétés

        public int Periode { get; set; } = 30;
        public bool Append { get; set; } = false;
        public ModeNommageEnum ModeNommage { get; set; } = ModeNommageEnum.DATE;

        #endregion

        #region Constructor

        public AutoZipper()
        {
            cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Méthodes

        public void Start(string inputPath, string outputPath)
        {
            this.inputPath = Environment.ExpandEnvironmentVariables(inputPath);
            this.outputPath = Environment.ExpandEnvironmentVariables(outputPath);
            
            fileSystemWatcher = new FileSystemWatcher(inputPath);
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Pause()
        {
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Resume()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }

        public void Dispose()
        {
            // On arrête l'écoute
            fileSystemWatcher?.Dispose();
            fileSystemWatcher = null;
            // On annule l'éventuelle tache en cours
            cancellationTokenSource.Cancel();
            // On attend que l'éventuelle tâche en cours se termine
            zipTask?.Wait();
        }

        public void doWork(CancellationToken cancellationToken)
        {
            // Déjà, il faut au moins attendre 1 fois la période d'attente
            
            // Ensuite, tant que l'on a pas atteint la fin de la période ou qu'on a pas annulé
        }

        #endregion
        
        #region Even Handlers

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Avant toute chose on attend la fin de la/les copie(s)

            // On met à jour la date de dernière maj
            lastUpdate = DateTime.Now;

            // S'il n'y a pas de Task en cours, on en lance une
            if (zipTask == null || zipTask.IsCompleted)
            {
                zipTask = Task.Run(() => doWork(), cancellationTokenSource.Token);
            }
        }

        #endregion

    }
}
