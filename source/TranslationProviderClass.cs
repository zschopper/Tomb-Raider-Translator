using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TRTR
{
    internal static class TranslationDict_removed // singleton dictionary
    {

        internal static TranslationProvider Provider { get; set; }

        static TranslationDict_removed()
        {
            TranslationDict_removed.Provider = null;
        }

        internal static void LoadTranslations()
        {
            if (Provider == null)
                throw new Exception("Translation provider isn't initialized.");
            Provider.LoadTranslations();
        }

        internal static string GetTranslation(string text, FileEntry entry, string[] context = null)
        {
            if (Provider == null)
                throw new Exception("Translation provider isn't initialized.");
            return Provider.GetTranslation(text, entry, context);
        }

        internal static bool UseContext
        {
            get
            {
                if (Provider == null)
                    throw new Exception("Translation provider isn't initialized.");
                return Provider.UseContext;
            }
        }

        internal static void Clear()
        {
            if (Provider == null)
                throw new Exception("Translation provider isn't initialized.");
            Provider.Clear();
        }
    }

    internal abstract class TranslationProvider
    {
        internal bool UseContext { get { return getUseContext(); } }

        internal abstract void Open();
        internal abstract void Clear();
        internal abstract void LoadTranslations();
        internal abstract string GetTranslation(string text, FileEntry entry, string[] context = null);
        internal abstract void Close();

        protected abstract bool getUseContext();
    }
}
