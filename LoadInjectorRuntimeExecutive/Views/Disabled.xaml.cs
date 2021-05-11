using System.Windows.Controls;
using System.Xml;

namespace LoadInjector.RunTime {

    public partial class Disabled : UserControl {
        private readonly XmlNode node;

        public Disabled(XmlNode node) {
            this.node = node;
            InitializeComponent();
            DataContext = this;
        }

        public string LineName => node.Attributes["name"]?.Value;
    }
}