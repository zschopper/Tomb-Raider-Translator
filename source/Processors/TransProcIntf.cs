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
        void Extract(FileEntryList entryList); // extracts translatable data
        void CreateTranslation(FileEntryList entryList, XmlNode node, string dir); // creates translation xml
        void CreateRestoration(FileEntryList entryList, XmlNode node); // creates restoration xml
        void Translate(FileEntryList entryList, XmlNode node, bool simulated); // translates game
        void Restore(FileEntryList entryList, XmlNode node, bool simulated); // restores game
    }

    class SampleTransProc : ITransProc {

        string ITransProc.Name { get { return "sample"; } }
        void ITransProc.Initialize() { }
        string[] ITransProc.GetFileList() { return new string[0]; }
        void ITransProc.Extract(FileEntryList entryList) { } // extracts translatable data
        void ITransProc.CreateTranslation(FileEntryList entryList, XmlNode node, string dir) { } // creates translation xml
        void ITransProc.CreateRestoration(FileEntryList entryList, XmlNode node) { } // creates restoration xml
        void ITransProc.Translate(FileEntryList entryList, XmlNode node, bool simulated) { } // translates game
        void ITransProc.Restore(FileEntryList entryList, XmlNode node, bool simulated) { } // restores game
    }
}
