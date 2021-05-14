using System;
using System.Xml;

namespace LoadInjector.Common {

    public class DocumentLoadedEventArgs : EventArgs {
        public XmlDocument Document { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }

        public string ArchiveRoot { get; set; }
    }

    public class SearchRequestedEventArgs : EventArgs {
        public string XPath { get; set; }
    }

    public class SaveAsEventArgs : EventArgs {
        public string Path { get; set; }
        public string FileName { get; set; }
    }
}