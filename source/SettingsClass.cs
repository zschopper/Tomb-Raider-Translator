using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TRTR
{
    static class Settings
    {
        #region private declarations
        static private string fileName = ".\\TRTR.xml";
        static private string transRootDir = string.Empty;
        static private Point formLocation = Screen.PrimaryScreen.WorkingArea.Location;
        static private string lastLocale = Thread.CurrentThread.CurrentCulture.Name;
        static private string lastGame = string.Empty;
        static private Dictionary<string, CultureInfo> cultures = new Dictionary<string, CultureInfo>();
        static private string[] args = null;
        #endregion

        static internal string TransRootDir { get { return transRootDir; } set { transRootDir = value; } }
        static internal Point FormLocation { get { return formLocation; } set { formLocation = value; } }
        static internal string LastLocale { get { return lastLocale; } set { lastLocale = value; } }
        static internal string LastGame { get { return lastGame; } set { lastGame = value; } }
        static internal Dictionary<string, CultureInfo> Cultures { get { return cultures; } }
        static internal string[] Args { get { return args; } set { args = value; } }
        static internal Version version;

        static internal void Load()
        {
            version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            Regex rx = new Regex(@"lang\-([a-z]+\-[A-Z]+)\.xml");
            // string fileName = string.Format("lang-{0}.xml", Application.CurrentCulture.Name);
//            string fileNameEN = "lang-en-GB.xml";
            cultures = new Dictionary<string, CultureInfo>();
            Assembly thisExe = Assembly.GetExecutingAssembly();
            string[] resources = thisExe.GetManifestResourceNames();

            for (int i = 0; i < resources.Length; i++)
            {
                string res = resources[i];
                Match mtch = rx.Match(res);
                if (mtch.Success)
                {
                    CultureInfo clt = new CultureInfo(mtch.Groups[1].Value);
                    cultures.Add(clt.TwoLetterISOLanguageName.ToUpper(), clt);
                }
            }
            
            XmlDocument doc = new XmlDocument();
            if (File.Exists(fileName))
            {
                doc.Load(fileName);
                XmlNode rootNode = doc.SelectSingleNode("settings");
                XmlNode transDirNode = rootNode.SelectSingleNode("transdir/@value");
                if (transDirNode != null)
                    transRootDir = transDirNode.Value;
                XmlNode lastLocaleNode = rootNode.SelectSingleNode("lastlocale/@value");
                if (lastLocaleNode != null)
                    lastLocale = lastLocaleNode.Value;
                XmlNode lastGameNode = rootNode.SelectSingleNode("lastgame/@value");
                if (lastGameNode != null)
                    lastGame = lastGameNode.Value;
                XmlNode formLocationNode = rootNode.SelectSingleNode("formlocation/@value");
                if (formLocationNode != null)
                {
                    string formLocationStr = formLocationNode.Value;
                    string[] elements = formLocationStr.Split(';');
                    if (elements.Length == 2)
                        formLocation = new System.Drawing.Point(Convert.ToInt32(elements[0].Trim()), 
                                Convert.ToInt32(elements[1].Trim()));
                }
            }
        }

        static internal void Save()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement element = doc.CreateElement("settings");
            XmlNode rootNode = doc.AppendChild(element);
            XmlNode node;
            if (transRootDir.Length > 0)
            {
                element = doc.CreateElement("transdir");
                element.SetAttribute("value", transRootDir);
                node = rootNode.AppendChild(element);
            }

            element = doc.CreateElement("lastlocale");
            element.SetAttribute("value", lastLocale);
            node = rootNode.AppendChild(element);

            element = doc.CreateElement("lastgame");
            element.SetAttribute("value", lastGame);
            node = rootNode.AppendChild(element);
            
            element = doc.CreateElement("formlocation");
            element.SetAttribute("value", String.Format("{0}; {1}", formLocation.X, formLocation.Y));
            node = rootNode.AppendChild(element);

            doc.Save(fileName);
        }
    }
}
