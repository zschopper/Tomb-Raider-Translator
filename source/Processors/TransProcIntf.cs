using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TRTR.Processors {

    interface ITransProc {
        // properties
        // methods
        string Name { get; }
        void Initialize();
        string[] GetFileList();
        void Extract(IFileEntryList entryList); // extracts translatable data
        void CreateTranslation(IFileEntryList entryList, XmlNode node, string dir); // creates translation xml
        void CreateRestoration(IFileEntryList entryList, XmlNode node); // creates restoration xml
        void Translate(IFileEntryList entryList, XmlNode node, bool simulated); // translates game
        void Restore(IFileEntryList entryList, XmlNode node, bool simulated); // restores game
    }

    class SampleTransProc : ITransProc {

        string ITransProc.Name { get { return "sample"; } }
        void ITransProc.Initialize() { }
        string[] ITransProc.GetFileList() { return new string[0]; }
        void ITransProc.Extract(IFileEntryList entryList) { } // extracts translatable data
        void ITransProc.CreateTranslation(IFileEntryList entryList, XmlNode node, string dir) { } // creates translation xml
        void ITransProc.CreateRestoration(IFileEntryList entryList, XmlNode node) { } // creates restoration xml
        void ITransProc.Translate(IFileEntryList entryList, XmlNode node, bool simulated) { } // translates game
        void ITransProc.Restore(IFileEntryList entryList, XmlNode node, bool simulated) { } // restores game
    }
}
