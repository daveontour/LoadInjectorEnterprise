using LoadInjector.Common;
using LoadInjector.GridDefinitions;
using LoadInjector.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;
using Xceed.Wpf.Toolkit.PropertyGrid;
using Path = System.Windows.Shapes.Path;

namespace LoadInjector.Views {

    public partial class TreeEditorView : UserControl, INotifyPropertyChanged, IView {
        private readonly Dictionary<FrameworkElement, XmlNode> canvasToNode = new Dictionary<FrameworkElement, XmlNode>();
        private readonly Dictionary<XmlNode, FrameworkElement> nodeToCanvas = new Dictionary<XmlNode, FrameworkElement>();

        public Dictionary<TextBlock, XmlNode> tbToNode = new Dictionary<TextBlock, XmlNode>();
        private TreeEditorViewModel viewModel;

        private const int arrowHeadLength = 12;
        private const int arrowHeadWidth = 5;
        private const double chainIndent = 15.0;
        private const double lineMargin = 165.0;
        private readonly ContextMenuProvider contextMenuProvider;
        private int maxOffset = Int32.MinValue;
        private int minOffset = Int32.MaxValue;
        private Canvas panel;
        private double startArrrowX;
        private double startArrrowY;
        private double stopArrrowX;
        private double stopArrrowY;
        private TaskCompletionSource<bool> tcs;
        private FileSystemWatcher watcher;
        public XmlNode CurrentDestinationNode { get; private set; }

        public TabControl GetBottomTabControl => bottomTabControl;

        public XmlNode GetCurrentDestination => CurrentDestinationNode;

        public TabControl GetDestinationTabControl => destinationTabControl;

        public TreeEditorViewModel ViewModel {
            get => viewModel;
            set {
                viewModel = value;
                Dispatcher.BeginInvoke((Action)delegate {
                    Cursor = Cursors.Wait;
                    BindUIElementToViewModel();
                    Cursor = Cursors.Arrow;
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        public TreeEditorView() {
            InitializeComponent();
            DataContextChanged += new DependencyPropertyChangedEventHandler(TreeEditorView_DataContextChanged);
            contextMenuProvider = new ContextMenuProvider();
            xmlTreeView.ContextMenu = new ContextMenu();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static T ElementClone<T>(T element) {
            MemoryStream memStream = ElementToStream(element);
            T clone = ElementFromStream<T>(memStream);
            return clone;
        }

        public static T ElementFromStream<T>(MemoryStream elementAsStream) {
            object reconstructedElement = null;

            if (elementAsStream.CanRead) {
                elementAsStream.Seek(0, SeekOrigin.Begin);
                reconstructedElement = XamlReader.Load(elementAsStream);
                elementAsStream.Close();
            }

            return (T)reconstructedElement;
        }

        public static MemoryStream ElementToStream(object element) {
            MemoryStream memStream = new MemoryStream();
            XamlWriter.Save(element, memStream);
            return memStream;
        }

        public static T FindChild<T>(DependencyObject parent, string childName)
   where T : DependencyObject {
            // Confirm parent and childName are valid.
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++) {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null) {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child.
                    if (foundChild != null) break;
                } else if (!string.IsNullOrEmpty(childName)) {
                    FrameworkElement frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName) {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                } else {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject {
            if (depObj != null) {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T) {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child)) {
                        yield return childOfChild;
                    }
                }
            }
        }

        public bool CanChangeElementType(string value) {
            return viewModel.CanChangeElementType();
        }

        public void ChangeElementType(string value) {
            viewModel.ChangeElementType(value);
        }

        public void ChangeFilterType(string value) {
            viewModel.ChangeFilterType(value);
        }

        public void CheckSaveTemplate() {
            MessageBoxResult res = MessageBox.Show("Save Template File Changes?", "Save Changes", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes) {
                string fn = CurrentDestinationNode.Attributes["templateFile"]?.Value;
                if (fn == null) {
                    SaveFileDialog dialog = new SaveFileDialog {
                        Filter = "All Files (*.*)|*.*"
                    };
                    if (dialog.ShowDialog() == true) {
                        TextRange text = new TextRange(TemplateText.Document.ContentStart, TemplateText.Document.ContentEnd);
                        File.WriteAllText(dialog.FileName, text.Text);

                        if (CurrentDestinationNode.Attributes["templateFile"] == null) {
                            XmlAttribute newAttribute = viewModel.SelectedElement.NearestDestination.OwnerDocument.CreateAttribute("templateFile");
                            newAttribute.Value = dialog.FileName;
                            CurrentDestinationNode.Attributes.Append(newAttribute);
                        } else {
                            CurrentDestinationNode.Attributes["templateFile"].Value = dialog.FileName;
                        }
                    }
                } else {
                    TextRange text = new TextRange(TemplateText.Document.ContentStart, TemplateText.Document.ContentEnd);
                    File.WriteAllText(fn, text.Text);
                }
            }
        }

        public void DrawConfig(XmlDocument xmlDoc) {
            maxOffset = Int32.MinValue;
            minOffset = Int32.MaxValue;

            panel = new Canvas();
            panel.Height = 1200;
            panel.Width = 800;

            panel.Children.Clear();
            canvasToNode.Clear();
            nodeToCanvas.Clear();

            SolidColorBrush grayBrush = new SolidColorBrush {
                Color = Colors.Gray
            };

            XmlNodeList destinationlines = xmlDoc.SelectNodes("//destination");
            XmlNodeList amsdatalines = xmlDoc.SelectNodes("//amsdatadriven");
            XmlNodeList csvdatalines = xmlDoc.SelectNodes("//csvdatadriven");
            XmlNodeList exceldatalines = xmlDoc.SelectNodes("//exceldatadriven");
            XmlNodeList xmldatalines = xmlDoc.SelectNodes("//xmldatadriven");
            XmlNodeList jsondatalines = xmlDoc.SelectNodes("//jsondatadriven");
            XmlNodeList databasedatalines = xmlDoc.SelectNodes("//databasedatadriven");
            XmlNodeList amsdirectlines = xmlDoc.SelectNodes("//amsdirect");
            XmlNodeList ratelines = xmlDoc.SelectNodes("//ratedriven");
            XmlNodeList chained = xmlDoc.SelectNodes("//chained");

            double topMargin = 10.0;
            int cellHeight = 30;

            int tot = destinationlines.Count + amsdirectlines.Count + amsdatalines.Count + csvdatalines.Count + exceldatalines.Count + xmldatalines.Count + jsondatalines.Count + databasedatalines.Count
                + ratelines.Count + chained.Count;
            int x = Math.Max(400, (tot + 3) * (cellHeight + 5) + 100);
            panel.Height = x + 50;

            Border loadInjectorLabel = GetResourceCopy<Border>("qxLabel");
            loadInjectorLabel.Width = x;
            loadInjectorLabel.SetValue(Canvas.TopProperty, topMargin);
            panel.Children.Add(loadInjectorLabel);

            nodeToCanvas.Add(xmlDoc.SelectSingleNode("//settings"), loadInjectorLabel);
            canvasToNode.Add(loadInjectorLabel, xmlDoc.SelectSingleNode("//settings"));
            loadInjectorLabel.PreviewMouseLeftButtonDown += LeftMouseButtonDown;
            loadInjectorLabel.PreviewMouseRightButtonDown += RightMouseButtonDown;

            MaxFlightOffsets();

            bool hasSource = (maxOffset != Int32.MinValue && minOffset != Int32.MaxValue);
            double ptr = topMargin;

            if (hasSource && Parameters.SITAAMS) {
                Border fltSource = GetResourceCopy<Border>("fltSource");

                FindChild<Label>(fltSource, "minOff").Content = $"Min OffSet = {minOffset}";
                FindChild<Label>(fltSource, "maxOff").Content = $"Max OffSet = {maxOffset}";

                fltSource.SetValue(Canvas.LeftProperty, lineMargin);
                fltSource.SetValue(Canvas.TopProperty, ptr);
                panel.Children.Add(fltSource);

                stopArrrowY = ptr + 15.0;
                startArrrowY = stopArrrowY;
                startArrrowX = lineMargin;
                stopArrrowX = 60.0;

                Path arrowpath = GetResourceCopy<Path>("arrow");

                Tuple<Polygon, Path> p = DrawLineArrow(new Point(startArrrowX, startArrrowY), new Point(stopArrrowX, stopArrrowY), arrowpath);
                panel.Children.Add(p.Item1);
                panel.Children.Add(p.Item2);

                ptr += 35;
            }

            if ((amsdatalines.Count + csvdatalines.Count + exceldatalines.Count + xmldatalines.Count + jsondatalines.Count + databasedatalines.Count) > 0) {
                ptr += 15;

                Label mpmlabel = new Label() {
                    Content = "Data Driven Sources"
                };
                mpmlabel.SetValue(Canvas.LeftProperty, 80.0);
                mpmlabel.SetValue(Canvas.TopProperty, ptr - 22);
                mpmlabel.SetValue(FontWeightProperty, FontWeights.Bold);
                panel.Children.Add(mpmlabel);
            }

            if (amsdatalines.Count > 0) {
                ptr = AddDataDriven("amsDataDriven", "amsDataDrivenLineName", "amsDataDrivenLineVars", amsdatalines, ptr, grayBrush);
            }
            if (csvdatalines.Count > 0) {
                ptr = AddDataDriven("csvDataDriven", "csvDataDrivenLineName", "csvDataDrivenLineVars", csvdatalines, ptr, grayBrush);
            }
            if (exceldatalines.Count > 0) {
                ptr = AddDataDriven("excelDataDriven", "excelDataDrivenLineName", "excelDataDrivenLineVars", exceldatalines, ptr, grayBrush);
            }
            if (xmldatalines.Count > 0) {
                ptr = AddDataDriven("xmlDataDriven", "xmlDataDrivenLineName", "xmlDataDrivenLineVars", xmldatalines, ptr, grayBrush);
            }
            if (jsondatalines.Count > 0) {
                ptr = AddDataDriven("jsonDataDriven", "jsonDataDrivenLineName", "jsonDataDrivenLineVars", jsondatalines, ptr, grayBrush);
            }
            if (databasedatalines.Count > 0) {
                ptr = AddDataDriven("dbDataDriven", "dbDataDrivenLineName", "dbDataDrivenLineVars", databasedatalines, ptr, grayBrush);
            }

            ptr += 15;

            if (ratelines.Count > 0) {
                Label mpmlabel = new Label() {
                    Content = "Rate Driven Sources"
                };
                mpmlabel.SetValue(Canvas.LeftProperty, 80.0);
                mpmlabel.SetValue(Canvas.TopProperty, ptr - 22);
                mpmlabel.SetValue(FontWeightProperty, FontWeights.Bold);
                panel.Children.Add(mpmlabel);

                foreach (XmlNode node in ratelines) {
                    ptr = AddRateLine(node, ptr);
                }
            }

            ptr += 15;
            if (amsdirectlines.Count > 0) {
                Label mpmlabel = new Label() {
                    Content = "AMS Direct Destinations"
                };
                mpmlabel.SetValue(Canvas.LeftProperty, 80.0);
                mpmlabel.SetValue(Canvas.TopProperty, ptr - 22);
                mpmlabel.SetValue(FontWeightProperty, FontWeights.Bold);

                panel.Children.Add(mpmlabel);

                foreach (XmlNode node in amsdirectlines) {
                    // The Descriptive Panel
                    Border amsDirect = GetResourceCopy<Border>("amsDirect");

                    nodeToCanvas.Add(node, amsDirect);
                    canvasToNode.Add(amsDirect, node);

                    amsDirect.PreviewMouseDown += LeftMouseButtonDown;
                    amsDirect.PreviewMouseRightButtonDown += NodeRightMouseDown;

                    int numVars = node.SelectNodes("./variable").Count;

                    FindChild<Label>(amsDirect, "amsLineName").Content = node.Attributes["name"].Value;
                    FindChild<Label>(amsDirect, "amsLineVars").Content = $"{numVars} vars.";

                    if (CheckDisabled(node)) {
                        FindChild<Label>(amsDirect, "amsLineName").FontStyle = FontStyles.Italic;
                        FindChild<Label>(amsDirect, "amsLineName").Foreground = grayBrush;

                        FindChild<Label>(amsDirect, "amsLineVars").FontStyle = FontStyles.Italic;
                        FindChild<Label>(amsDirect, "amsLineVars").Foreground = grayBrush;
                    }

                    amsDirect.SetValue(Canvas.LeftProperty, lineMargin);
                    amsDirect.SetValue(Canvas.TopProperty, ptr);
                    panel.Children.Add(amsDirect);

                    // The Arrow pointing betweeen LoadInjector and the panel
                    stopArrrowY = ptr + 15.0;
                    startArrrowY = stopArrrowY;

                    startArrrowX = 60.0;
                    stopArrrowX = lineMargin;

                    // The Arrow pointing betweeen LoadInjector and the panel
                    Tuple<Polygon, Path> p = DrawLineArrow(new Point(startArrrowX, startArrrowY), new Point(stopArrrowX, stopArrrowY), GetResourceCopy<Path>("arrow"));
                    panel.Children.Add(p.Item1);
                    panel.Children.Add(p.Item2);

                    ptr += 35;
                }
                ptr += 15;
            }

            if (destinationlines.Count > 0) {
                Label mpmlabel = new Label() {
                    Content = "Destinations"
                };
                mpmlabel.SetValue(Canvas.LeftProperty, 80.0);
                mpmlabel.SetValue(Canvas.TopProperty, ptr - 22);
                mpmlabel.SetValue(FontWeightProperty, FontWeights.Bold);

                panel.Children.Add(mpmlabel);

                foreach (XmlNode node in destinationlines) {
                    Border dest = GetResourceCopy<Border>("dest");

                    nodeToCanvas.Add(node, dest);
                    canvasToNode.Add(dest, node);

                    dest.PreviewMouseDown += LeftMouseButtonDown;
                    dest.PreviewMouseRightButtonDown += NodeRightMouseDown;
                    FindChild<Label>(dest, "directLineName").Content = node.Attributes["name"].Value;
                    FindChild<Label>(dest, "directLineProtocol").Content = node.Attributes["protocol"].Value + ",";

                    int numVars = node.SelectNodes("./variable").Count;
                    FindChild<Label>(dest, "directLineVars").Content = $"{numVars} vars.";

                    if (CheckDisabled(node)) {
                        FindChild<Label>(dest, "directLineName").FontStyle = FontStyles.Italic;
                        FindChild<Label>(dest, "directLineName").Foreground = grayBrush;

                        FindChild<Label>(dest, "directLineVars").FontStyle = FontStyles.Italic;
                        FindChild<Label>(dest, "directLineVars").Foreground = grayBrush;

                        FindChild<Label>(dest, "directLineProtocol").FontStyle = FontStyles.Italic;
                        FindChild<Label>(dest, "directLineProtocol").Foreground = grayBrush;
                    }

                    dest.SetValue(Canvas.LeftProperty, lineMargin);
                    dest.SetValue(Canvas.TopProperty, ptr);
                    panel.Children.Add(dest);

                    startArrrowY = ptr + 15;
                    stopArrrowY = startArrrowY;

                    startArrrowX = 60.0;
                    stopArrrowX = lineMargin;

                    Tuple<Polygon, Path> p = DrawLineArrow(new Point(startArrrowX, startArrrowY), new Point(stopArrrowX, stopArrrowY), GetResourceCopy<Path>("arrow"));
                    panel.Children.Add(p.Item1);
                    panel.Children.Add(p.Item2);

                    ptr += 35;
                }
            }

            configViewBox.Content = panel;
        }

        public void DrawConfig() {
            DrawConfig(viewModel.DataModel);
        }

        public PropertyGrid GetPropertyGrid() {
            return _propertyGrid;
        }

        public SelectedElementViewModel GetSelectedElement() {
            return viewModel.SelectedElement;
        }

        public XmlDocument GetViewModel() {
            return viewModel.DataModel;
        }

        public void HighlightCanvas(XmlNode node) {
            SolidColorBrush redBrush = new SolidColorBrush {
                Color = Colors.Red
            };
            SolidColorBrush midBlueBrush = new SolidColorBrush {
                Color = Colors.MidnightBlue
            };

            try {
                FrameworkElement target = nodeToCanvas[node];
                Border border = (Border)target;

                foreach (Border tb in FindVisualChildren<Border>(panel)) {
                    Brush backgroundColor = tb.Background;

                    if (backgroundColor is SolidColorBrush bc) {
                        string colorValue = bc.Color.ToString();
                        if (colorValue == "#FFFF0000") {
                            tb.Background = midBlueBrush;
                        }
                    }
                }

                border.Background = redBrush;
            } catch (Exception) {
                foreach (Border tb in FindVisualChildren<Border>(panel)) {
                    Brush backgroundColor = tb.Background;

                    if (backgroundColor is SolidColorBrush bc) {
                        string colorValue = bc.Color.ToString();
                        if (colorValue == "#FFFF0000") {
                            tb.Background = midBlueBrush;
                        }
                    }
                }
            }
        }

        public void HighlightNode(XmlNode xmlNode) {
            TreeViewItem tvItem = null;
            try {
                tvItem = xmlTreeView.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
            } catch {
                // NO-OP
            }
            if (xmlNode == null) {
                SelectTreeViewItem(ref tvItem, "");
            } else {
                SelectTreeViewItem(ref tvItem, xmlNode);
            }
        }

        public void MaxFlightOffsets() {
            foreach (XmlNode node in viewModel.DataModel.SelectNodes("//eventdrivensources/*")) {
                try {
                    if (Int32.TryParse(node.Attributes["flightSetFrom"]?.Value, out int fltSetFrom))
                        minOffset = Math.Min(fltSetFrom, minOffset);
                    if (Int32.TryParse(node.Attributes["flightSetTo"]?.Value, out int fltSetTo))
                        maxOffset = Math.Max(fltSetTo, maxOffset);
                } catch (Exception) {
                    // NO-OP
                }
            }

            XmlNodeList ratelines = viewModel.DataModel.SelectNodes("//ratedriven");
            foreach (XmlNode node in ratelines) {
                try {
                    if (Int32.TryParse(node.Attributes["flightSetFrom"]?.Value, out int fltSetFrom))
                        minOffset = Math.Min(fltSetFrom, minOffset);
                    if (Int32.TryParse(node.Attributes["flightSetTo"]?.Value, out int fltSetTo))
                        maxOffset = Math.Max(fltSetTo, maxOffset);
                } catch (Exception) {
                    // NO-OP
                }
            }
        }

        public void RefreshDraw() {
            DrawConfig(viewModel.DataModel);
        }

        public void ResetCurrentDestination() {
            CurrentDestinationNode = null;
        }

        public void SetProtocolGrid(string value) {
            viewModel.SetProtocolGrid(value);
        }

        public void UpdateDiagram() {
            DrawConfig();
        }

        public void UpdateParamBindings(string param) {
            viewModel.OnPropertyChanged(param);
        }

        public void UpdateTemplateBox() {
            XmlNode destNode = viewModel.SelectedElement.NearestDestination;

            if (destNode == CurrentDestinationNode) {
                TextRange text = new TextRange(TemplateText.Document.ContentStart, TemplateText.Document.ContentEnd);
                FlowDocument doc = new FlowDocument();
                doc.Blocks.Add(GetPara(text.Text));
                TemplateText.Document = doc;
                return;
            }

            CurrentDestinationNode = destNode;
            Dispatcher.BeginInvoke((Action)(() => bottomTabControl.SelectedIndex = 2));
            string templateFilename = destNode.Attributes["templateFile"]?.Value;
            if (templateFilename == null) {
                TemplateText.Document = new FlowDocument();
                ViewModel.EnableTemplateSave = false;
                return;
            }

            FileInfo fileInfo = new FileInfo(templateFilename);
            if (!fileInfo.Exists) {
                FlowDocument errorDoc = new FlowDocument();
                Paragraph para = new Paragraph();
                para.Inlines.Add(new Italic(new Bold(new Run("Template File Not Defined or File Not Found")) { Foreground = new SolidColorBrush { Color = Colors.Gray } }));
                errorDoc.Blocks.Add(para);
                TemplateText.Document = errorDoc;
                return;
            }

            string readText = File.ReadAllText(fileInfo.FullName);

            FlowDocument mcFlowDoc = new FlowDocument();
            mcFlowDoc.Blocks.Add(GetPara(readText));

            // Set contents
            TemplateText.Document = mcFlowDoc;

            if (watcher != null) {
                watcher.EnableRaisingEvents = false;
            }

            tcs?.SetResult(true);
            ViewModel.EnableTemplateSave = false;
            _ = Watch(fileInfo);
        }

        internal void ControlPropertyChange() {
            viewModel.OnPropertyChanged("XMLText");
            DrawConfig(viewModel.DataModel);
        }

        protected void OnPropertyChanged(string propName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        private void Add_Header(object sender, RoutedEventArgs e) {
            ViewModel.AddHeader();
        }

        private void Add_Trigger(object sender, RoutedEventArgs e) {
            ViewModel.AddTrigger();
        }

        private void Add_Value(object sender, RoutedEventArgs e) {
            ViewModel.AddValue();
        }

        private double AddChainedLine(XmlNode node, double ptr, int depth) {
            SolidColorBrush grayBrush = new SolidColorBrush {
                Color = Colors.Gray
            };

            Border chainDriven = GetResourceCopy<Border>("chainDriven");

            nodeToCanvas.Add(node, chainDriven);
            canvasToNode.Add(chainDriven, node);

            chainDriven.PreviewMouseDown += LeftMouseButtonDown;
            chainDriven.PreviewMouseRightButtonDown += NodeRightMouseDown;

            FindChild<Label>(chainDriven, "chainDrivenLineName").Content = node.Attributes["name"].Value;
            FindChild<StackPanel>(chainDriven, "chainDrivenStack").SetValue(WidthProperty, 346 - depth * chainIndent);
            FindChild<Label>(chainDriven, "chainDrivenLineDelay").Content = $"  Delay = {node.Attributes["delay"].Value} sec.";

            if (CheckDisabled(node)) {
                FindChild<Label>(chainDriven, "chainDrivenLineName").FontStyle = FontStyles.Italic;
                FindChild<Label>(chainDriven, "chainDrivenLineName").Foreground = grayBrush;
            }

            chainDriven.SetValue(Canvas.LeftProperty, lineMargin + depth * chainIndent);
            chainDriven.SetValue(WidthProperty, 350 - depth * chainIndent);
            chainDriven.SetValue(Canvas.TopProperty, ptr);
            panel.Children.Add(chainDriven);

            // The Arrow pointing betweeen LoadInjector and the panel

            double stopArrrowY = ptr + 15.0;
            double startArrrowY = stopArrrowY;

            double startArrrowX = 60.0;
            double stopArrrowX = lineMargin + depth * chainIndent;

            Tuple<Polygon, Path> p = DrawLineArrow(new Point(stopArrrowX, stopArrrowY), new Point(startArrrowX, startArrrowY), GetResourceCopy<Path>("arrow"), true);
            panel.Children.Add(p.Item1);
            panel.Children.Add(p.Item2);

            ptr += 35;

            XmlNodeList chainlines = node.SelectNodes("./chained");

            foreach (XmlNode chain in chainlines) {
                ptr = AddChainedLine(chain, ptr, depth + 1);
            }

            return ptr;
        }

        private double AddDataDriven(string basename, string lineName, string linevars, XmlNodeList nodes, double ptr, Brush grayBrush) {
            foreach (XmlNode node in nodes) {
                // The Descriptive Panel
                Border border = GetResourceCopy<Border>(basename);

                nodeToCanvas.Add(node, border);
                canvasToNode.Add(border, node);

                border.PreviewMouseDown += LeftMouseButtonDown;
                border.PreviewMouseRightButtonDown += NodeRightMouseDown;

                FindChild<Label>(border, lineName).Content = node.Attributes["name"].Value;
                FindChild<Label>(border, linevars).Content = $"{node.ChildNodes.Count} triggers.";

                if (CheckDisabled(node)) {
                    FindChild<Label>(border, lineName).FontStyle = FontStyles.Italic;
                    FindChild<Label>(border, lineName).Foreground = grayBrush;

                    FindChild<Label>(border, linevars).FontStyle = FontStyles.Italic;
                    FindChild<Label>(border, linevars).Foreground = grayBrush;
                }
                border.SetValue(Canvas.LeftProperty, lineMargin);
                border.SetValue(Canvas.TopProperty, ptr);
                panel.Children.Add(border);

                // The Arrow pointing betweeen LoadInjector and the panel

                stopArrrowY = ptr + 15.0;
                startArrrowY = stopArrrowY;

                startArrrowX = 60.0;
                stopArrrowX = lineMargin;

                // The Arrow pointing betweeen LoadInjector and the panel
                Tuple<Polygon, Path> p = DrawLineArrow(new Point(stopArrrowX, stopArrrowY), new Point(startArrrowX, startArrrowY), GetResourceCopy<Path>("arrow"));
                panel.Children.Add(p.Item1);
                panel.Children.Add(p.Item2);

                XmlNodeList chainlines = node.SelectNodes("./chained");
                if (chainlines.Count > 0) {
                    ptr += 35;
                }
                foreach (XmlNode chain in chainlines) {
                    ptr = AddChainedLine(chain, ptr, 1);
                }
                if (chainlines.Count > 0) {
                    ptr -= 35;
                }

                ptr += 35;
            }

            return ptr;
        }

        private double AddRateLine(XmlNode node, double ptr) {
            SolidColorBrush grayBrush = new SolidColorBrush {
                Color = Colors.Gray
            };

            Border rateDriven = GetResourceCopy<Border>("rateDriven");
            if (node.Name == "chained") {
                rateDriven = GetResourceCopy<Border>("chainDriven");
            }

            nodeToCanvas.Add(node, rateDriven);
            canvasToNode.Add(rateDriven, node);

            rateDriven.PreviewMouseDown += LeftMouseButtonDown;
            rateDriven.PreviewMouseRightButtonDown += NodeRightMouseDown;

            FindChild<Label>(rateDriven, "rateDrivenLineName").Content = node.Attributes["name"].Value;

            if (CheckDisabled(node)) {
                FindChild<Label>(rateDriven, "rateDrivenLineName").FontStyle = FontStyles.Italic;
                FindChild<Label>(rateDriven, "rateDrivenLineName").Foreground = grayBrush;
            }

            rateDriven.SetValue(Canvas.LeftProperty, lineMargin);
            rateDriven.SetValue(Canvas.TopProperty, ptr);
            panel.Children.Add(rateDriven);

            // The Arrow pointing betweeen LoadInjector and the panel

            double stopArrrowY = ptr + 15.0;
            double startArrrowY = stopArrrowY;

            double startArrrowX = 60.0;
            double stopArrrowX = lineMargin;

            Tuple<Polygon, Path> p = DrawLineArrow(new Point(stopArrrowX, stopArrrowY), new Point(startArrrowX, startArrrowY), GetResourceCopy<Path>("arrow"));
            panel.Children.Add(p.Item1);
            panel.Children.Add(p.Item2);

            XmlNodeList chainlines = node.SelectNodes("./chained");
            if (chainlines.Count > 0) {
                ptr += 35;
            }
            foreach (XmlNode chain in chainlines) {
                ptr = AddChainedLine(chain, ptr, 1);
            }
            if (chainlines.Count > 0) {
                ptr -= 35;
            }

            ptr += 35;

            return ptr;
        }

        private void BindUIElementToViewModel() {
            try {
                if (viewModel == null) {
                    return;
                } else {
                    viewModel.View = this as IView;
                }
            } catch {
                return;
            }

            XmlDataProvider dataProvider = FindResource("xmlDataProvider") as XmlDataProvider;
            dataProvider.Document = viewModel.DataModel;
            dataProvider.Refresh();

            xmlTreeView.ContextMenu.Items.Clear();

            contextMenuProvider.contextMenus[ContextMenuType.AddAMSDataDriven].Command = ViewModel.AddAMSDataDrivenCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddCSVDataDriven].Command = ViewModel.AddCSVDataDrivenCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddExcelDataDriven].Command = ViewModel.AddExcelDataDrivenCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddXMLDataDriven].Command = ViewModel.AddXMLDataDrivenCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddJSONDataDriven].Command = ViewModel.AddJSONDataDrivenCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddDataBaseDataDriven].Command = ViewModel.AddDataBaseDataDrivenCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddRateDriven].Command = ViewModel.AddRateDrivenCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddDestination].Command = ViewModel.AddDestinationCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddAMSDirect].Command = ViewModel.AddAMSDirectCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddVariable].Command = ViewModel.AddVariableCommand;
            contextMenuProvider.contextMenus[ContextMenuType.Clone].Command = ViewModel.CloneElementCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddChained].Command = ViewModel.AddChainedCommand;
            contextMenuProvider.contextMenus[ContextMenuType.Delete].Command = ViewModel.DeleteElementCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddFilter].Command = ViewModel.AddFilterCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddDataFilter].Command = ViewModel.AddDataFilterCommand;
            contextMenuProvider.contextMenus[ContextMenuType.AddExpression].Command = ViewModel.AddExpressionCommand;

            ViewModel.HighlightNodeInUI = HighlightNode;

            ((TabItem)GetDestinationTabControl.Items.GetItemAt(0)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(1)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(2)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(3)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(4)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(5)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(6)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(7)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(8)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(9)).Visibility = Visibility.Collapsed;
            ((TabItem)GetDestinationTabControl.Items.GetItemAt(10)).Visibility = Visibility.Collapsed;

            TabItem tab10 = (TabItem)GetDestinationTabControl.Items.GetItemAt(10);
            tab10.Visibility = Visibility.Visible;
            GetDestinationTabControl.SelectedIndex = 10;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            // NO-OP
        }

        private bool CheckDisabled(XmlNode node) {
            bool disabled = false;
            if (node.Attributes["disabled"] != null) {
                try {
                    disabled = bool.Parse(node.Attributes["disabled"].Value);
                } catch (Exception) {
                    disabled = false;
                }
            }
            return disabled;
        }

        private void Delete_Header(object sender, RoutedEventArgs e) {
            ViewModel.DeleteHeader(headerDataGrid);
        }

        private void Delete_Header0(object sender, RoutedEventArgs e) {
            ViewModel.DeleteHeader(headerDataGrid0);
        }

        private void Delete_HeaderProtocol(object sender, RoutedEventArgs e) {
            ViewModel.DeleteHeader(headerDataGridProtocol);
        }

        private void Delete_Trigger(object sender, RoutedEventArgs e) {
            TriggerModelEntry trig = (TriggerModelEntry)triggerDataGrid2.SelectedItem;
            if (trig == null) {
                return;
            }
            MessageBoxResult res = MessageBox.Show($"Delete Trigger: {trig.Name} ?", "Delete Trigger", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes) {
                ViewModel.DeleteTrigger(triggerDataGrid2);
            }
        }

        private void Delete_Value(object sender, RoutedEventArgs e) {
            ViewModel.DeleteValue(valueGrid);
        }

        private Tuple<Polygon, Path> DrawLineArrow(Point startPoint, Point endPoint, Path arrowpath, bool isChained = false) {
            GeometryGroup geometryGroup = new GeometryGroup();
            //line
            LineGeometry line = new LineGeometry {
                StartPoint = startPoint
            };
            double length = Math.Sqrt(Math.Abs(startPoint.X - endPoint.X) * Math.Abs(startPoint.X - endPoint.X) +
                Math.Abs(startPoint.Y - endPoint.Y) * Math.Abs(startPoint.Y - endPoint.Y));
            Point EndPoint = new Point(startPoint.X + length, startPoint.Y);
            line.EndPoint = new Point(EndPoint.X - arrowHeadLength, EndPoint.Y);

            geometryGroup.Children.Add(line);
            arrowpath.Data = geometryGroup;
            arrowpath.StrokeThickness = 1;
            arrowpath.Fill = Brushes.SteelBlue;
            if (isChained) {
                arrowpath.StrokeDashArray = new DoubleCollection() { 2 };
            }

            //rotate
            RotateTransform form = new RotateTransform {
                CenterX = startPoint.X,
                CenterY = startPoint.Y
            };
            //calculate the angle
            double angle = Math.Asin(Math.Abs(startPoint.Y - endPoint.Y) / length);
            double angle2 = 180 / Math.PI * angle;
            //orientation
            if (endPoint.Y > startPoint.Y) {
                angle2 = (endPoint.X > startPoint.X) ? angle2 : (180 - angle2);
            } else {
                angle2 = (endPoint.X > startPoint.X) ? -angle2 : -(180 - angle2);
            }
            form.Angle = angle2;
            arrowpath.RenderTransform = form;

            Point p1P = new Point(EndPoint.X, EndPoint.Y);
            Point p2P = new Point(EndPoint.X - arrowHeadLength, EndPoint.Y - arrowHeadWidth);
            Point p3P = new Point(EndPoint.X - arrowHeadLength, EndPoint.Y + arrowHeadWidth);

            Point p1 = form.Transform(p1P);
            Point p2 = form.Transform(p2P);
            Point p3 = form.Transform(p3P);

            Polygon p = GetResourceCopy<Polygon>("arrowHead");
            p.Points = new PointCollection() { p1, p2, p3 };

            return new Tuple<Polygon, Path>(p, arrowpath);
        }

        private Paragraph GetPara(string template) {
            List<string> tokenList = new List<string>();
            XmlNode destNode = viewModel.SelectedElement.NearestDestination;
            XmlNode selectedNode = viewModel.SelectedElement.DataModel;

            string selectedToken = null;
            if (selectedNode.Name == "variable") {
                selectedToken = selectedNode.Attributes["token"]?.Value;
            }

            XmlNodeList variables = destNode.SelectNodes(".//variable");
            foreach (XmlNode vari in variables) {
                var a = vari.Attributes["token"]?.Value;
                if (a != null) {
                    tokenList.Add(vari.Attributes["token"]?.Value);
                }
            }

            SolidColorBrush blueBrush = new SolidColorBrush {
                Color = Colors.Blue
            };
            SolidColorBrush redBrush = new SolidColorBrush {
                Color = Colors.Red
            };

            Paragraph para = new Paragraph();

            try {
                foreach (string token in tokenList) {
                    template = template.Replace(token, $"@@##>>{token}@@##>>");
                }
                string[] runs = template.Split(new[] { "@@##>>" }, StringSplitOptions.None);

                string last = runs[runs.Length - 1];
                if (last.EndsWith("\r\n\r\n")) {
                    last = last.Substring(0, last.Length - 2);
                    runs[runs.Length - 1] = last;
                }

                foreach (string run in runs) {
                    if (tokenList.Contains(run)) {
                        if (selectedToken == run) {
                            para.Inlines.Add(new Italic(new Bold(new Run(run)) { Foreground = redBrush }));
                        } else {
                            para.Inlines.Add(new Italic(new Bold(new Run(run)) { Foreground = blueBrush }));
                        }
                    } else {
                        para.Inlines.Add(new Run(run));
                    }
                }
            } catch (Exception) {
                return new Paragraph();
            }

            return para;
        }

        private T GetResourceCopy<T>(string key) {
            T model = (T)FindResource(key);
            return ElementClone<T>(model);
        }

        private void LeftMouseButtonDown(object sender, MouseButtonEventArgs e) {
            //Clear the tree selection
            SelectionUtils.ClearTreeViewSelection(xmlTreeView);

            if (sender == null) {
                e.Handled = false;
                return;
            }

            FrameworkElement target;

            try {
                target = (FrameworkElement)sender;
            } catch (Exception) {
                target = null;
            }

            if (target == null) {
                e.Handled = false;
                return;
            }

            XmlNode node;

            try {
                node = canvasToNode[target];
                HighlightNode(node);
            } catch (Exception) {
                node = null;
            }

            if (node == null) {
                e.Handled = false;
                return;
            }

            ViewModel.ViewAttributesCommand.Execute(node);

            Border border = (Border)target;
            SolidColorBrush redBrush = new SolidColorBrush {
                Color = Colors.Red
            };
            SolidColorBrush midBlueBrush = new SolidColorBrush {
                Color = Colors.MidnightBlue
            };

            foreach (Border tb in FindVisualChildren<Border>(panel)) {
                Brush backgroundColor = tb.Background;

                if (backgroundColor is SolidColorBrush) {
                    string colorValue = ((SolidColorBrush)backgroundColor).Color.ToString();
                    if (colorValue == "#FFFF0000") {
                        tb.Background = midBlueBrush;
                    }
                }
            }

            border.Background = redBrush;

            e.Handled = true;
        }

        private void NodeRightMouseDown(object sender, MouseButtonEventArgs e) {
            try {
                viewModel.SelectedElement = new SelectedElementViewModel(canvasToNode[sender as FrameworkElement]);
            } catch (Exception) {
                viewModel.SelectedElement = new SelectedElementViewModel(tbToNode[sender as TextBlock]);
            }

            ContextMenuProvider menuProvider = new ContextMenuProvider();
            ContextMenu cm = new ContextMenu();

            menuProvider.contextMenus[ContextMenuType.AddChained].Command = ViewModel.AddChainedCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddChained]);

            cm.Items.Add(new Separator());

            menuProvider.contextMenus[ContextMenuType.Clone].Command = ViewModel.CloneElementCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.Clone]);

            cm.Items.Add(new Separator());

            menuProvider.contextMenus[ContextMenuType.Delete].Command = ViewModel.DeleteElementCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.Delete]);

            try {
                cm.PlacementTarget = sender as Rectangle;
            } catch (Exception) {
                cm.PlacementTarget = sender as TextBlock;
            }
            cm.IsOpen = true;
        }

        private void OnTempleteFileChangedAsync(object sender, FileSystemEventArgs e) {
            ViewModel.EnableTemplateSave = false;
            _ = Task.Run(() => {
                var fs = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using (var sr = new StreamReader(fs)) {
                    string text = sr.ReadToEnd();

                    Application.Current.Dispatcher.Invoke(delegate {
                        FlowDocument mcFlowDoc = new FlowDocument();
                        mcFlowDoc.Blocks.Add(GetPara(text));
                        TemplateText.Document = mcFlowDoc;
                    });
                }
            });
        }

        private void RightMouseButtonDown(object sender, MouseButtonEventArgs e) {
            // Right Click Menu for the "Load Injector" block in the diagram.

            viewModel.SelectedElement = new SelectedElementViewModel(viewModel.DataModel.SelectSingleNode("//lines"));

            ContextMenuProvider menuProvider = new ContextMenuProvider();
            ContextMenu cm = new ContextMenu();

            if (Parameters.SITAAMS) {
                menuProvider.contextMenus[ContextMenuType.AddAMSDataDriven].Command = ViewModel.AddAMSDataDrivenCommand;
                cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddAMSDataDriven]);
            }
            menuProvider.contextMenus[ContextMenuType.AddCSVDataDriven].Command = ViewModel.AddCSVDataDrivenCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddCSVDataDriven]);

            menuProvider.contextMenus[ContextMenuType.AddExcelDataDriven].Command = ViewModel.AddExcelDataDrivenCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddExcelDataDriven]);

            menuProvider.contextMenus[ContextMenuType.AddXMLDataDriven].Command = ViewModel.AddXMLDataDrivenCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddXMLDataDriven]);

            menuProvider.contextMenus[ContextMenuType.AddJSONDataDriven].Command = ViewModel.AddJSONDataDrivenCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddJSONDataDriven]);

            menuProvider.contextMenus[ContextMenuType.AddDataBaseDataDriven].Command = ViewModel.AddDataBaseDataDrivenCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddDataBaseDataDriven]);

            cm.Items.Add(new Separator());

            menuProvider.contextMenus[ContextMenuType.AddRateDriven].Command = ViewModel.AddRateDrivenCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddRateDriven]);

            cm.Items.Add(new Separator());

            if (Parameters.SITAAMS) {
                menuProvider.contextMenus[ContextMenuType.AddAMSDirect].Command = ViewModel.AddAMSDirectCommand;
                cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddAMSDirect]);
            }

            menuProvider.contextMenus[ContextMenuType.AddDestination].Command = ViewModel.AddDestinationCommand;
            cm.Items.Add(menuProvider.contextMenus[ContextMenuType.AddDestination]);

            try {
                cm.PlacementTarget = sender as Rectangle;
            } catch (Exception) {
                cm.PlacementTarget = sender as TextBlock;
            }
            cm.IsOpen = true;
        }

        private void SaveAsCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e) {
            viewModel.SaveDocumentCommand.Execute(null);//Implementation of saveAs
        }

        private bool SelectTreeViewItem(ref TreeViewItem tvItem, XmlNode toBeSelectedNode) {
            bool isSelected = false;
            if (tvItem == null)
                return isSelected;

            if (!tvItem.IsExpanded) {
                tvItem.Focus();
                tvItem.IsExpanded = true;
            }
            if (!(tvItem.Header is XmlNode tempNode)) {
                return isSelected;
            }
            if (ReferenceEquals(tempNode, toBeSelectedNode)) {
                tvItem.IsSelected = true;
                tvItem.IsExpanded = true;
                isSelected = true;
                return isSelected;
            } else {
                for (int i = 0; i < tvItem.Items.Count; i++) {
                    TreeViewItem childItem = tvItem.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;

                    isSelected = SelectTreeViewItem(ref childItem, toBeSelectedNode);
                    if (isSelected) {
                        break;
                    }
                }
                return isSelected;
            }
        }

        private bool SelectTreeViewItem(ref TreeViewItem rootNode, string elementName) {
            bool isSelected = false;
            if (rootNode == null)
                return isSelected;

            if (!rootNode.IsExpanded) {
                rootNode.Focus();
                rootNode.IsExpanded = true;
            }
            if (!(rootNode.Header is XmlNode tempNode)) {
                return isSelected;
            }
            if (String.Compare(tempNode.Name, elementName, StringComparison.OrdinalIgnoreCase) == 0) {
                rootNode.IsSelected = true;
                rootNode.IsExpanded = true;
                isSelected = true;
                return isSelected;
            } else {
                for (int i = 0; i < rootNode.Items.Count; i++) {
                    TreeViewItem childItem = rootNode.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;

                    isSelected = SelectTreeViewItem(ref childItem, elementName);
                    if (isSelected) {
                        break;
                    }
                }
                return isSelected;
            }
        }

        private void TemplateSaveButton_Click(object sender, RoutedEventArgs e) {
            XmlNode destNode = viewModel.SelectedElement.NearestDestination;

            string templateFilename = destNode.Attributes["templateFile"]?.Value;

            viewModel.EnableTemplateSave = false;

            if (templateFilename == null) {
                SaveFileDialog dialog = new SaveFileDialog {
                    Filter = "All Files (*.*)|*.*"
                };
                if (dialog.ShowDialog() == true) {
                    TextRange text = new TextRange(TemplateText.Document.ContentStart, TemplateText.Document.ContentEnd);
                    File.WriteAllText(dialog.FileName, text.Text);

                    if (destNode.Attributes["templateFile"] == null) {
                        XmlAttribute newAttribute = destNode.OwnerDocument.CreateAttribute("templateFile");
                        newAttribute.Value = dialog.FileName;
                        destNode.Attributes.Append(newAttribute);
                    } else {
                        destNode.Attributes["templateFile"].Value = dialog.FileName;
                    }

                    DestinationPropertyGrid myGrid = (DestinationPropertyGrid)ViewModel.MyGrid;
                    myGrid.Template = dialog.FileName;

                    try {
                        ViewModel.UpdatePropertiesPanel(ViewModel.SelectedElement.DataModel);
                    } catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                    }

                    UpdateTemplateBox();

                    return;
                } else {
                    // Nothing
                }

                return;
            }

            FileInfo fileInfo = new FileInfo(templateFilename);
            if (!fileInfo.Exists) {
                try {
                    fileInfo.Create();
                } catch (Exception) {
                    MessageBox.Show($"Could not create file {fileInfo.FullName}", "Save Template File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            TextRange textRange = new TextRange(TemplateText.Document.ContentStart, TemplateText.Document.ContentEnd);
            try {
                File.WriteAllText(fileInfo.FullName, textRange.Text);
            } catch (Exception) {
                MessageBox.Show($"Could not write to file {fileInfo.FullName}", "Save Template File Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TemplateText_KeyDown(object sender, KeyEventArgs e) {
            XmlNode destNode = viewModel.SelectedElement.NearestDestination;
            if (destNode == null || destNode.Name != "destination") {
                ViewModel.EnableTemplateSave = false;
            } else {
                ViewModel.EnableTemplateSave = true;
            }
        }

        private void TreeEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
            ViewModel = e.NewValue as TreeEditorViewModel;
            if (ViewModel != null) {
                DrawConfig(viewModel.DataModel);
            }
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseEventArgs e) {
            // Context menu for Tree structure

            xmlTreeView.ContextMenu.Items.Clear();

            if (sender is TreeViewItem selectedItem) {
                selectedItem.IsSelected = true;
            }

            if (viewModel.SelectedElement.DataModel.Name == "eventdrivensources") {
                if (Parameters.SITAAMS) {
                    xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddAMSDataDriven]);
                }
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddCSVDataDriven]);
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddExcelDataDriven]);
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddXMLDataDriven]);
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddJSONDataDriven]);
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddDataBaseDataDriven]);
            }
            if (viewModel.SelectedElement.DataModel.Name == "ratedrivensources") {
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddRateDriven]);
            }

            if (viewModel.SelectedElement.DataModel.Name == "lines") {
                if (Parameters.SITAAMS) {
                    xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddAMSDirect]);
                }
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddDestination]);
            }

            string flttype = viewModel.SelectedElement.DataModel.Attributes["flttype"]?.Value;
            if ((viewModel.SelectedElement.DataModel.Name == "amsdatadriven" || viewModel.SelectedElement.DataModel.Name == "xmldatadriven")
                || (viewModel.SelectedElement.DataModel.Name.EndsWith("driven") && flttype != null && flttype != "none")
                ) {
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddFilter]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
            }
            if (viewModel.SelectedElement.DataModel.Name == "filter") {
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddExpression]);
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddDataFilter]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Clone]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Delete]);
            }
            if (viewModel.SelectedElement.DataModel.Name == "and"
                || viewModel.SelectedElement.DataModel.Name == "or"
                || viewModel.SelectedElement.DataModel.Name == "xor"
                || viewModel.SelectedElement.DataModel.Name == "not"
                ) {
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddExpression]);
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddDataFilter]);

                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Delete]);
            }

            if (viewModel.SelectedElement.DataModel.Name.EndsWith("driven")) {
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddChained]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Clone]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Delete]);
            }
            if (viewModel.SelectedElement.DataModel.Name == "chained") {
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddChained]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Clone]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Delete]);
            }

            if (viewModel.SelectedElement.DataModel.Name == "destination"
                 || viewModel.SelectedElement.DataModel.Name == "amsdirect"
            ) {
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Clone]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Delete]);
            }
            if (viewModel.SelectedElement.DataModel.Name == "destination" || viewModel.SelectedElement.DataModel.Name == "amsdirect") {
                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.AddVariable]);
            }

            if (viewModel.SelectedElement.DataModel.Name == "variable" || viewModel.SelectedElement.DataModel.Name == "settings") {
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Clone]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Delete]);
                xmlTreeView.ContextMenu.Items.Add(new Separator());
            }

            if (
                 viewModel.SelectedElement.DataModel.Name == "contains"
                || viewModel.SelectedElement.DataModel.Name == "matches"
                || viewModel.SelectedElement.DataModel.Name == "length"
                || viewModel.SelectedElement.DataModel.Name == "xpexists"
                || viewModel.SelectedElement.DataModel.Name == "xpmatches"
                || viewModel.SelectedElement.DataModel.Name == "xpequal"
                || viewModel.SelectedElement.DataModel.Name == "dateRange"

                ) {
                xmlTreeView.ContextMenu.Items.Add(contextMenuProvider.contextMenus[ContextMenuType.Delete]);
            }
        }

        private async Task<bool> Watch(FileInfo fileInfo) {
            tcs = new TaskCompletionSource<bool>();

            try {
                watcher = new FileSystemWatcher {
                    Path = fileInfo.DirectoryName,
                    NotifyFilter = NotifyFilters.LastWrite,
                    Filter = fileInfo.Name
                };
                watcher.Changed += new FileSystemEventHandler(OnTempleteFileChangedAsync);
                watcher.EnableRaisingEvents = true;

                return await tcs.Task;
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        private void XmlTreeView_Selected(object sender, RoutedEventArgs e) {
            XmlNode selectedItem = xmlTreeView.SelectedItem as XmlNode;
            if (selectedItem.Name == "config") {
                return;
            }
            ViewModel.ViewAttributesCommand.Execute(selectedItem);
        }
    }
}