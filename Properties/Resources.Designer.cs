﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Il codice è stato generato da uno strumento.
//     Versione runtime:4.0.30319.42000
//
//     Le modifiche apportate a questo file possono provocare un comportamento non corretto e andranno perse se
//     il codice viene rigenerato.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PickAnyVoice.Properties {
    using System;
    
    
    /// <summary>
    ///   Classe di risorse fortemente tipizzata per la ricerca di stringhe localizzate e così via.
    /// </summary>
    // Questa classe è stata generata automaticamente dalla classe StronglyTypedResourceBuilder.
    // tramite uno strumento quale ResGen o Visual Studio.
    // Per aggiungere o rimuovere un membro, modificare il file con estensione ResX ed eseguire nuovamente ResGen
    // con l'opzione /str oppure ricompilare il progetto VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Restituisce l'istanza di ResourceManager nella cache utilizzata da questa classe.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("PickAnyVoice.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Esegue l'override della proprietà CurrentUICulture del thread corrente per tutte le
        ///   ricerche di risorse eseguite utilizzando questa classe di risorse fortemente tipizzata.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Cerca una risorsa localizzata di tipo System.Byte[].
        /// </summary>
        internal static byte[] infer_edge {
            get {
                object obj = ResourceManager.GetObject("infer_edge", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Cerca una risorsa localizzata di tipo System.Byte[].
        /// </summary>
        internal static byte[] infer_google {
            get {
                object obj = ResourceManager.GetObject("infer_google", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Cerca una risorsa localizzata di tipo System.Byte[].
        /// </summary>
        internal static byte[] pythoninfer {
            get {
                object obj = ResourceManager.GetObject("pythoninfer", resourceCulture);
                return ((byte[])(obj));
            }
        }
        
        /// <summary>
        ///   Cerca una risorsa localizzata di tipo System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap Tab1 {
            get {
                object obj = ResourceManager.GetObject("Tab1", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Cerca una risorsa localizzata di tipo System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap Tab2 {
            get {
                object obj = ResourceManager.GetObject("Tab2", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a runtime\python.exe gui_v2.py --pycmd runtime\python.exe
        ///pause
        ///.
        /// </summary>
        internal static string V2BAT {
            get {
                return ResourceManager.GetString("V2BAT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una risorsa localizzata di tipo System.Byte[].
        /// </summary>
        internal static byte[] V2PYTHON {
            get {
                object obj = ResourceManager.GetObject("V2PYTHON", resourceCulture);
                return ((byte[])(obj));
            }
        }
    }
}
