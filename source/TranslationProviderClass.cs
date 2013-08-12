using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TRTR
{
    internal static class TranslationDict // singleton dictionary
    {

        internal static TranslationProvider Provider { get; set; }

        static TranslationDict()
        {
            TranslationDict.Provider = null;
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

        internal static void Clear()
        {
            if (Provider == null)
                throw new Exception("Translation provider isn't initialized.");
            Provider.Clear();
        }
    }

    internal abstract class TranslationProvider
    {
        internal abstract void LoadTranslations();

        internal abstract string GetTranslation(string text, FileEntry entry, string[] context = null);

        internal abstract void Clear();
    }
}
