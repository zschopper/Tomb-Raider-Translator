using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Xml;
using System.Diagnostics;
using System.ComponentModel;


namespace TRTR
{
    class VDFNodeList : List<VDFNode> { }

    class VDFNode
    {
        private string name;
        private string value;
        private VDFNode parent;
        private SteamVDFDoc doc;
        private List<VDFNode> childItems = new List<VDFNode>();
        public string Name { get { return this.name; } set { this.name = value; } }
        public string Value { get { return this.value; } set { this.value = value; } }
        public VDFNode Parent { get { return parent; } set { parent = value; } }
        public SteamVDFDoc Doc { get { return doc; } set { doc = value; } }
        public List<VDFNode> ChildItems { get { return childItems; } }

        public VDFNode ChildItemByPath(string path) { 
            string[]elements = path.Split('\\');
            VDFNode ret = this;
            foreach(string element in elements)
            {
                ret = ret.ChildItemByName(element);
                if (ret == null)
                    return null;
            }
            return ret;
        }

        public VDFNode ChildItemByName(string name, int idx = 0)
        {
            foreach (VDFNode item in childItems)
            {
                int nth = idx;
                if (item.name == name)
                {
                    if (nth == 0)
                        return item;
                    nth--;
                }
            }
            return null;
        }

        public VDFNode(string name = "", string value = "")
        {
            this.name = name;
            this.value = value;
        }
    }

    class SteamVDFDoc
    {
        VDFNode root = null;

        public bool parseLine(string line, out string name, out string value)
        {
            name = string.Empty;
            value = string.Empty;
            bool inQuote = false;
            bool inEscape = false;
            bool inSep = false;
            string collect = string.Empty;
            int strIndex = 0;

            for (int j = 0; j < line.Length; j++)
            {
                char lineJ = line[j];
                switch (lineJ)
                {
                    case '\\':
                        if (inEscape)
                        {
                            collect += line[j];
                        }
                        inEscape = !inEscape;
                        break;
                    case '"':
                        if (inEscape)
                        {
                            collect += line[j];
                        }
                        else
                            inQuote = !inQuote;

                        inEscape = false;

                        break;
                    case ' ':
                    case '\t':
                        inEscape = false;
                        if (inQuote)
                        {
                            collect += line[j];
                        }
                        else
                        {
                            if (!inSep)
                            {
                                if (strIndex == 0)
                                    name = collect;
                                else
                                    value = collect;
                                collect = string.Empty;
                                inSep = true;
                                strIndex++;
                            }
                            
                        }
                        break;
                    default:
                        if (inEscape)
                        {

                        }
                        else
                        {
                            collect += line[j];
                        }
                        inEscape = false;
                        break;
                }
            }

            bool ret = !inEscape && !inQuote;

            if (ret)
            {
                if (collect.Length > 0)
                {
                    if (strIndex == 0)
                        name = collect;
                    else
                        value = collect;
                    collect = string.Empty;
                }
            }
            return ret;


        }

        public SteamVDFDoc(string file)
        {
            int lvl = 0;
            Regex rxValue = new Regex(@"^""([^""]+)""\s+""([^""]+)""$");
            VDFNode node = null;
            List<string> lines = new List<string>(File.ReadAllLines(file, Encoding.ASCII));
            for (int i = 0; i < lines.Count - 1; i++)
            {
                string line = lines[i].Trim();
                if (line.Length > 0)
                {
                    switch (line[0])
                    {
                        case '{':
                            lvl++;
                            if (node != null)
                                node = node.ChildItems[node.ChildItems.Count - 1];
                            else
                                node = root;
                            break;
                        case '}':
                            node = node.Parent;
                            lvl--;
                            break;
                        default:
                            string name = string.Empty;
                            string value = string.Empty;
                            parseLine(line, out name, out value);
                            addNode(node, name, value);
                            break;
                    }
                }
            }
        }

        public VDFNode addNode(VDFNode parent, VDFNode node)
        {

            if (parent == null)
                root = node;
            else
                parent.ChildItems.Add(node);
            node.Parent = parent;
            node.Doc = this;
            return node;
        }

        public VDFNode addNode(VDFNode parent, string name, string value = "")
        {
            return addNode(parent, new VDFNode(name, value));
        }

        public VDFNode ItemByPath(string path) 
        {
            string[] elements = path.Split('\\');
            VDFNode ret = null;
            foreach (string element in elements)
            {
                if (ret == null)
                {
                    if (element == root.Name)
                        ret = root;
                    else
                        return null;
                }
                else
                {
                    ret = ret.ChildItemByName(element);
                }
                if (ret == null)
                    return null;
            }
            return ret;
        }
    }
}
