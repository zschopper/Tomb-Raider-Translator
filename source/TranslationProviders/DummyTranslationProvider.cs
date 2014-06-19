﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TRTR
{
    class DummyTranslationProvider : TranslationProvider
    {
        internal override void Open() { }
        internal override void Close() { }
        internal override void Clear() { }

        protected override bool getUseContext() { return false; }

        internal override string GetTranslation(string text, IFileEntry entry, Dictionary<string, string> context)
        {
            return text;
        }

    }
}
