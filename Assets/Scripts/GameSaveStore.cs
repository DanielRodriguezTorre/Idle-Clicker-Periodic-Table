using System;
using System.IO;
using IdleClickerPeriodicTable.Core;
using UnityEngine;

namespace IdleClickerPeriodicTable
{
    /// <summary>La partida se lee y se guarda sin añadir ganancias por el tiempo que el juego estuvo cerrado.</summary>
    public static class GameSaveStore
    {
        /// <summary>La partida vive en este archivo, dentro de la carpeta de datos que proporciona Unity.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, "partida.json");

        /// <summary>Indica si hay un archivo guardado, que exista no significa que esté bien.</summary>
        public static bool Exists => File.Exists(FilePath);

        /// <summary>Se intenta recuperar la partida y se ponen sus valores en orden antes de usarla.</summary>
        public static bool TryLoad(out GameState state)
        {
            state = null;
            try
            {
                if (!Exists) 
                    return false;

                string json = File.ReadAllText(FilePath);

                // Una comprobación rápida para descartar archivos vacíos o que no parecen una partida.
                if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"money\"")) 
                    return false;

                state = JsonUtility.FromJson<GameState>(json);

                if (state == null) 
                    return false;

                state.Normalize();

                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                Debug.LogWarning("No se pudo leer la partida: " + GetErrorReason(exception) + " (código " + exception.HResult + ").");
                return false;
            }
        }

        /// <summary>Primero se escribe en un temporal, así la partida anterior no se pierde a medio guardar.</summary>
        public static bool TrySave(GameState state)
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                string temporaryPath = FilePath + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(state, true));

                // El guardado anterior solo se sustituye cuando el temporal ya está escrito entero.
                if (Exists) 
                    File.Replace(temporaryPath, FilePath, null);
                else 
                    File.Move(temporaryPath, FilePath);

                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("No se pudo guardar la partida: " + GetErrorReason(exception) + " (código " + exception.HResult + ").");
                return false;
            }
        }

        static string GetErrorReason(Exception exception)
        {
            // El mensaje de .NET depende del idioma del sistema, por eso el motivo se explica aquí en español.
            if (exception is UnauthorizedAccessException)
                return "no hay permisos para acceder al archivo";

            if (exception is ArgumentException)
                return "el archivo o su contenido no es válido";

            return "se ha producido un error de entrada o salida";
        }
    }
}
