using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NLog;
using System.IO.Compression;
using System.Timers;
using System.Threading;

namespace TPzip.Core
{
    public class AutoZip : IDisposable
    {
        private static Logger LOGGER = Logging.Logger;

        #region Fields

        #region Internals

        // Sert à stocker le temps désiré pour la prochaine update
        private DateTime? _UpdateTime;
        // Sert à stocker le nom du fichier zip en cours si on est en mode append
        private string _CurrentZipPath;
        // Sert à stocker le nom du fichier/repertoire à traiter si on est en mode de nommage PREMIER_FICHIER
        private string _FirstFileName;

        private System.Timers.Timer _Timer;

        #endregion

        #region Accessibles

        // Les champs suivants sont accessibles (au moins en lecture) via une propriété
        private string _InputDirectoryPath;
        private string _OutputDirectoryPath;
        private FileSystemWatcher _FileSystemWatcher;
        private ModeNommageEnum _ModeNommage;
        private int _PeriodeAttenteSecondes;
        private bool _Append;

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// Obtient ou définit le répertoire à écouter.
        /// </summary>
        public string InputDirectoryPath
        {
            get { return _InputDirectoryPath; }
            private set { _InputDirectoryPath = value; }
        }

        /// <summary>
        ///     Obtient ou définit le répertoire de sortie.
        /// </summary>
        public string OutputDirectoryPath
        {
            get { return _OutputDirectoryPath; }
            private set { _OutputDirectoryPath = value; }
        }

        /// <summary>
        ///     Obtient ou définit le mode de nommage du fichier de sortie.
        /// </summary>
        public ModeNommageEnum ModeNommage
        {
            get => _ModeNommage;
            private set => _ModeNommage = value;
        }

        /// <summary>
        ///     Obtient ou définit la période d'attente avant le début de la compression.
        /// </summary>
        public int PeriodeAttenteSecondes
        {
            get => _PeriodeAttenteSecondes;
            private set => _PeriodeAttenteSecondes = value;
        }

        /// <summary>
        ///     Obtient ou définit la règle de création de fichier de sortie.
        /// </summary>
        public bool Append
        {
            get => _Append;
            private set => _Append = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="TPzip.Core.AutoZip"/> avec les paramètres définis dans le fichier de configuration.
        /// Si un paramètre est absent on récupère une valeur par défaut.
        /// </summary>
        public AutoZip() : this(ConfigUtil.ReadInputPath(),
                ConfigUtil.ReadOutputPath(),
                ConfigUtil.ReadPeriodeAttente(),
                ConfigUtil.ReadModeNommage(),
                ConfigUtil.ReadAppend())
        { }

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="TPzip.Core.AutoZip"/>.
        /// </summary>
        /// <param name="inputDirectoryPath">Répertoire à écouter.</param>
        /// <param name="outputDirectoryPath">Répertoire de sortie.</param>
        /// <param name="periodeAttente">Répertoire de sortie.</param>
        /// <param name="modeNommageEnum">Répertoire de sortie.</param>
        /// <param name="append">Répertoire de sortie.</param>
        public AutoZip(string inputDirectoryPath, string outputDirectoryPath, int periodeAttente, ModeNommageEnum modeNommageEnum, bool append)
        {
            //ExpandEnvironmentVariables permet de résoudre les chemins comme %temp%
            InputDirectoryPath = Environment.ExpandEnvironmentVariables(inputDirectoryPath);
            OutputDirectoryPath = Environment.ExpandEnvironmentVariables(outputDirectoryPath);
            PeriodeAttenteSecondes = periodeAttente;
            ModeNommage = modeNommageEnum;
            Append = append;

            LOGGER.Info($"Repertoire d'execution : {Environment.CurrentDirectory}");
            LOGGER.Info($"Repertoire d'entrée : {InputDirectoryPath}");
            LOGGER.Info($"Repertoire de sortie : {OutputDirectoryPath}");
            LOGGER.Info($"Periode d'attente : {PeriodeAttenteSecondes} secondes");
            LOGGER.Info($"Mode de nommage du zip : {ModeNommage}");
            LOGGER.Info($"Fichier unique avec ajout : {Append}");

            #region Arguments Testing

            if (string.IsNullOrWhiteSpace(InputDirectoryPath))
            {
                //throw new ArgumentException("Le paramètre " + nameof(inputDirectoryPath) 
                //    + " n'est pas défini ou vide."
                //    , nameof(inputDirectoryPath));

                throw new ArgumentException(
                    $"Le paramètre {nameof(inputDirectoryPath)} n'est pas défini ou vide."
                    , nameof(inputDirectoryPath));

                //$ devant une chaîne appel la méthode string.Format
                //string.Format("Le paramètre {0} n'est pas défini ou vide.", nameof(inputDirectoryPath));
            }

            if (string.IsNullOrWhiteSpace(OutputDirectoryPath))
            {
                throw new ArgumentException(
                $"Le paramètre {nameof(outputDirectoryPath)} n'est pas défini ou vide."
                , nameof(outputDirectoryPath));
            }

            #endregion

            #region Directories Check

            //Permet de vérifier si le dossier existe.
            if (!Directory.Exists(InputDirectoryPath))
            {
                //Crée tous les répertoires et sous-répertoires dans le chemin d'accès spécifié, sauf s'ils existent déjà.
                Directory.CreateDirectory(InputDirectoryPath);
            }

            //CreateDirectory ne créé pas le(s) dossier(s) s'il(s) existe(nt) déjà.
            Directory.CreateDirectory(OutputDirectoryPath);

            #endregion

            LOGGER.Info("AutoZip Init ended");
        }

        #endregion

        #region Methods

        #region Events handlers

        /// <summary>
        /// Cette event est levé à une changement dans le dossier écouté
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            // Avant toute chose on attend la fin de la/les copie(s)
            LOGGER.Trace("-------");
            LOGGER.Trace("CREATED");
            LOGGER.Trace("FullPath : " + e.FullPath);
            LOGGER.Trace("-------");

            WaitCopied(e.FullPath);
            LOGGER.Info("Copied");

            // On met à jour la date de dernière maj
            _UpdateTime = DateTime.Now.AddSeconds(PeriodeAttenteSecondes);
            LOGGER.Info($"Prochaine compression fixée pour {_UpdateTime?.ToString("dd/MM/yy HH:mm:ss")}");

            // On sauve le nom du fichier si c'est le premier au cas où on est en mode de nommage PREMIER_FICHIER
            if (_FirstFileName == null)
            {
                _FirstFileName = Path.GetFileNameWithoutExtension(e.FullPath);
            }

            // On lance un Timer en s'assurant n'en avoir qu'un d'actif et de ne pas tourner pour rien
            if (_Timer == null || !_Timer.Enabled)
            {
                LOGGER.Info("Schedule d'une tâche de compression.");
                _Timer = new System.Timers.Timer(PeriodeAttenteSecondes * 1000);
                _Timer.Elapsed += _Timer_Elapsed;
                _Timer.AutoReset = false;
                _Timer.Start();
            }
            else
            {
                LOGGER.Info("Il y a déjà une tâche en attente, on ne fait rien");
            }
        }

        /// <summary>
        /// Cet event est levé lorsque le Timer courrant est échu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // On vérifie que le travail soit toujours d'actualité
            if (_UpdateTime != null)
            {
                // On boucle tant que l'on a pas atteint la période d'attente désirée
                while (_UpdateTime > DateTime.Now)
                {
                    Thread.Sleep(1000);
                }

                // On définit le nom du fichier zip
                string targetPath = DefineFileName();

                // On lance l'archivage
                LOGGER.Info($"Début de l'archivage dans {targetPath}");
                using (ZipArchive archive = ZipFile.Open(targetPath, ZipArchiveMode.Update))
                {
                    Directory.EnumerateFileSystemEntries(InputDirectoryPath).ToList().ForEach(subEntry =>
                    {
                        CreateEntry(archive, subEntry, "");
                    });
                }
                LOGGER.Info("Archivage terminé");

                // On sauvegarde le nom du fichier au cas où l'on est en append
                _CurrentZipPath = targetPath;

                // On nettoie
                _FirstFileName = null;
                _UpdateTime = null;
                _Timer.Dispose();
                _Timer = null;

                LOGGER.Info("Tâche nettoyée");
            }
        }

        /// <summary>
        /// Méthode récursive pour créer toutes les entrées
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="entry"></param>
        /// <param name="rootNode"></param>
        private void CreateEntry(ZipArchive archive, string entry, string parentEntry)
        {
            if (File.Exists(entry))
            {
                archive.CreateEntryFromFile(entry, parentEntry + Path.GetFileName(entry));
                File.Delete(entry);
            }
            else if (Directory.Exists(entry))
            {
                parentEntry += Path.GetFileName(entry) + "\\";
                Directory.EnumerateFileSystemEntries(entry).ToList().ForEach(subEntry =>
                {
                    CreateEntry(archive, subEntry, parentEntry);
                });
                Directory.Delete(entry);
            }
        }

        #endregion

        #region Service Management

        public void Start()
        {
            if (_FileSystemWatcher == null)
            {
                _FileSystemWatcher = new FileSystemWatcher(InputDirectoryPath);
                _FileSystemWatcher.Created += _FileSystemWatcher_Created;
            }
            _FileSystemWatcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _Timer?.Dispose();
            _Timer = null;
            _FileSystemWatcher?.Dispose();
            _FileSystemWatcher = null;
        }

        public void Pause()
        {
            if (_FileSystemWatcher != null)
            {
                _FileSystemWatcher.EnableRaisingEvents = false;
            }
        }

        public void Resume() => Start();

        #endregion

        #region Util Methods

        /// <summary>
        /// Permet de construire le nom du fichier zip à produire
        /// </summary>
        /// <returns></returns>
        private string DefineFileName()
        {
            // Si on est en mode Append, on récupère le nom du fichier en cours s'il existe, sinon on recréé
            if (Append && _CurrentZipPath != null && File.Exists(_CurrentZipPath))
            {
                return _CurrentZipPath;
            }
            else
            {
                string fileName;
                switch (ModeNommage)
                {
                    case ModeNommageEnum.GUID:
                        fileName = Guid.NewGuid().ToString();
                        break;
                    case ModeNommageEnum.DATE:
                        fileName = DateTime.Now.ToString("dd_MM_yy_HHmmss");
                        break;
                    case ModeNommageEnum.PREMIER_FICHIER:
                    default:
                        fileName = _FirstFileName;
                        break;
                }
                return $"{OutputDirectoryPath}\\{fileName}.zip";
            }
        }

        private void WaitCopied(string path)
        {
            if (File.Exists(path))
            {
                LOGGER.Trace("Is a file");
                WaitFileCopied(path);
            }
            else if (Directory.Exists(path))
            {
                LOGGER.Trace("Is a directory");
                WaitDirectoryCopied(path);
            }
        }

        private void WaitDirectoryCopied(string directoryPath)
        {
            //Pour vérifier si un dossier est copié,
            //on vérifie que pour chaque sous-dossier le dernier accès en écriture est supérieur à 1 minutes.
            //si c'est le cas, on vérifie que chaque fichier est bien accessible en lecture/écriture avec un vérou exclusif.
            //A la moindre erreur, on recommence l'ensemble des vérifications.

            bool directoryCopied = false;
            bool subFilesCopied = false;
            bool subDirectoryCopied = false;

            do
            {
                //Si on a pas de sous-dossier, on passe subDirectoryCopied à true
                subDirectoryCopied = Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories).Any() == false;

                //Si on a des sous-dossiers, on fait la boucle de vérification des sous-dossiers
                if (!subDirectoryCopied)
                {
                    foreach (string subDirectoryPath in Directory.GetDirectories(directoryPath, "*", SearchOption.AllDirectories))
                    {
                        DateTime lastWriteTime = Directory.GetLastWriteTime(subDirectoryPath);

                        if (lastWriteTime.AddMinutes(1) > DateTime.Now)
                        {
                            subDirectoryCopied = false;
                            LOGGER.Trace("subDirectory not copied");
                            System.Threading.Thread.Sleep(500);
                            break;
                        }
                        else
                        {
                            subDirectoryCopied = true;
                            LOGGER.Trace("subDirectoryCopied");
                        }
                    }
                }

                //S'il n'existe pas de sous-dossier ou s'ils sont bien copiés, on fait la boucle de vérification des fichiers.
                if (subDirectoryCopied)
                {
                    //Si on a pas de fichiers dans le dossier, on passe subFilesCopied à true
                    subFilesCopied = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).Any() == false;

                    //Si on a des fichiers, on fait la boucle de vérification des sous-dossiers
                    if (!subFilesCopied)
                    {
                        foreach (string filePath in Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories))
                        {
                            if (!CheckFileCopied(filePath))
                            {
                                subFilesCopied = false;
                                LOGGER.Trace("subFiles not copied");
                                System.Threading.Thread.Sleep(500);
                                break;
                            }
                            else
                            {
                                subFilesCopied = true;
                                LOGGER.Trace("subFilesCopied");
                            }
                        }
                    }
                }

                directoryCopied = subDirectoryCopied && subFilesCopied;
            } while (!directoryCopied);
        }

        private void WaitFileCopied(string filePath)
        {
            while (!CheckFileCopied(filePath))
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        private bool CheckFileCopied(string filePath)
        {
            bool fileOpened = false;

            try
            {
                //IDisposable est une interface qui founie un mécanisme de supresion des objets non managés.
                //IDisposable définie une méthode Dispose qui doit être appelée pour nettoyer les ressources non managés.
                //Si la méthoe n'est pas appelé, il ya un risque de fuite mémoire.
                //L'instruction using permet de gérer convenablement les objets IDisposable
                //Dans l'instruction using, il est important de déclarer la référence et obligaoire d'instancier l'objet.
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    //Si on a pas d'exception, le fichier est bien ouvert et accessible.
                    fileOpened = true;
                }
            }
            catch (Exception)
            {
                //Le fichier n'est pas accessible
            }

            return fileOpened;
        }

        public void Dispose()
        {
            Stop();
        }

        #endregion

        #endregion
    }
}
