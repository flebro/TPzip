using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TPzip.Core
{
    static class ConfigUtil
    {
        private static Logger LOGGER = Logging.Logger;

        #region Constantes

        #region Etiquettes configuration

        private const string INPUT_PATH_KEY         = "inputPath";
        private const string OUTPUT_PATH_KEY        = "outputPath";
        private const string APPEND_KEY             = "append";
        private const string PERIODE_ATTENTE_KEY    = "periodeAttenteSecondes";
        private const string MODE_NOMMAGE_KEY       = "modeNommage";

        #endregion

        #region Valeurs par défaut

        private const string DEFAULT_INPUT_PATH                 = @"%temp%\AutoZip\input";
        private const string DEFAULT_OUTPUT_PATH                = @"%temp%\AutoZip\output";
        private const int DEFAULT_PERIODE_ATTENTE               = 30;
        private const bool DEFAULT_APPEND                       = false;
        private const ModeNommageEnum DEFAULT_MODE_NOMMAGE      = ModeNommageEnum.DATE;

        #endregion
        
        #endregion

        public static string ReadInputPath()
        {
            string inputPath = ReadParam(INPUT_PATH_KEY);
            return inputPath != null ? inputPath : DEFAULT_INPUT_PATH;
        }

        public static string ReadOutputPath()
        {
            string inputPath = ReadParam(OUTPUT_PATH_KEY);
            return inputPath != null ? inputPath : DEFAULT_OUTPUT_PATH;
        }

        public static int ReadPeriodeAttente()
        {
            int result;
            string perStr = ReadParam(PERIODE_ATTENTE_KEY);
            return int.TryParse(perStr, out result) ? result : DEFAULT_PERIODE_ATTENTE;
        }

        public static ModeNommageEnum ReadModeNommage()
        {
            ModeNommageEnum result;
            string modeNommageStr = ReadParam(MODE_NOMMAGE_KEY);
            return Enum.TryParse(modeNommageStr, out result) ? result : DEFAULT_MODE_NOMMAGE;
        }

        public static bool ReadAppend()
        {
            bool result;
            string perStr = ReadParam(APPEND_KEY);
            return bool.TryParse(perStr, out result) ? result : DEFAULT_APPEND;
        }

        private static string ReadParam(string paramKey)
        {
            string param = ConfigurationManager.AppSettings[paramKey];
            if (param == null)
            {
                LOGGER.Warn($"Paramètre {paramKey} absent. Récupération de la valeur par défaut.");
            }
            return param;
        }

    }
}
