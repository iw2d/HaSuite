﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace HaSharedLibrary.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class HaResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal HaResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("HaSharedLibrary.Properties.HaResources", typeof(HaResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
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
        ///   Looks up a localized string similar to Auto-Detect.
        /// </summary>
        internal static string EncTypeAuto {
            get {
                return ResourceManager.GetString("EncTypeAuto", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Bruteforce AES key.
        /// </summary>
        internal static string EncTypeBruteforce {
            get {
                return ResourceManager.GetString("EncTypeBruteforce", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Use custom encryption key.
        /// </summary>
        internal static string EncTypeCustom {
            get {
                return ResourceManager.GetString("EncTypeCustom", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to GMS (old).
        /// </summary>
        internal static string EncTypeGMS {
            get {
                return ResourceManager.GetString("EncTypeGMS", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MSEA / EMS / Taiwan(old).
        /// </summary>
        internal static string EncTypeMSEA {
            get {
                return ResourceManager.GetString("EncTypeMSEA", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BMS / GMS / MapleSEA / メイプルストーリー / 메이플스토리.
        /// </summary>
        internal static string EncTypeNone {
            get {
                return ResourceManager.GetString("EncTypeNone", resourceCulture);
            }
        }
    }
}