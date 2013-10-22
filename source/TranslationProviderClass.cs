using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TRTR
{
    internal abstract class TranslationProvider
    {
        internal bool UseContext { get { return getUseContext(); } }

        internal abstract void Open();
        internal abstract void Clear();
        internal abstract string GetTranslation(string text, FileEntry entry, string[] context = null);
        internal abstract void Close();

        protected abstract bool getUseContext();
    }
}
