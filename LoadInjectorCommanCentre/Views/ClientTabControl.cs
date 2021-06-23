using LoadInjectorBase.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;

namespace LoadInjectorCommandCentre.Views {

    public class ClientTabControl : INotifyPropertyChanged {
        private ExecutionRecords tabRecords = new ExecutionRecords();
        private Canvas panel;
        private double startArrrowX;
        private double startArrrowY;
        private double stopArrrowX;
        private double stopArrrowY;
        private int maxOffset = Int32.MinValue;
        private int minOffset = Int32.MaxValue;
        private const int arrowHeadLength = 12;
        private const int arrowHeadWidth = 5;
        private const double chainIndent = 15.0;
        private const double lineMargin = 165.0;

        public ExecutionRecords TabExecutionRecords {
            get {
                return tabRecords;
            }
            set {
                tabRecords = value;
                OnPropertyChanged("TabExecutionRecords");
            }
        }

        private string header;

        public string Header {
            get { return header; }
            set {
                header = value;
                OnPropertyChanged("Header");
            }
        }

        public MainCommandCenterController MainController { get; private set; }

        private string ip;

        public string IP {
            get {
                return $"Client IP: {ip}";
            }
            set {
                ip = value;
                OnPropertyChanged("IP");
            }
        }

        private string processID;

        public Canvas FlowCanvas {
            get { return this.DrawConfig(DataModel); }
        }

        public string ProcessID {
            get {
                return $"Process ID: {processID}";
            }
            set {
                processID = value;
                OnPropertyChanged("ProcessID");
            }
        }

        private string executionID;

        public string ExecutionNodeID {
            get {
                return executionID;
            }
            set {
                executionID = value;
                OnPropertyChanged("ExecutionNodeID");
            }
        }

        private string osversion;

        public string OSVersion {
            get {
                return $"{osversion}";
            }
            set {
                osversion = value;
                OnPropertyChanged("OSVersion");
            }
        }

        private string statusText = ClientState.UnAssigned.Value;

        public string StatusText {
            get {
                return $"{statusText}";
            }
            set {
                statusText = value;
                OnPropertyChanged("StatusText");
            }
        }

        public Visibility DetailVisibility {
            get {
                if (IsSummary) {
                    return Visibility.Collapsed;
                } else {
                    return Visibility.Visible;
                }
            }
        }

        public string Title {
            get {
                if (IsSummary) {
                    return "All Connected Nodes";
                } else {
                    return $"Execution Node: {IP}, {ProcessID}.  Work Package:  {WorkPackage}";
                }
            }
        }

        public bool IsSummary { get; set; }

        public string ConnectionID { get; set; }

        private string xml;

        public string XML {
            get {
                return xml;
            }
            set {
                xml = Utils.FormatXML(value);
                DataModel = new XmlDocument();
                DataModel.LoadXml(xml);
                OnPropertyChanged("XML");
                OnPropertyChanged("FlowCanvas");
            }
        }

        private XmlDocument DataModel;

        private string consoleText;

        public string ConsoleText {
            get { return consoleText; }
            set {
                consoleText = value;
                OnPropertyChanged("ConsoleText");
            }
        }

        private string workPackage;

        public string WorkPackage {
            get {
                return workPackage;
            }
            set {
                workPackage = value;
                OnPropertyChanged("WorkPackage");
            }
        }

        private CompletionReport completionReport;

        public CompletionReport CompletionReport {
            get { return completionReport; }
            set {
                completionReport = value;
                OnPropertyChanged("CompletionReport");
                OnPropertyChanged("CompletionReportString");
            }
        }

        public string CompletionReportString {
            get {
                if (CompletionReport != null) {
                    return CompletionReport.ToString();
                } else {
                    return "-Pending-";
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged(string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ClientTabControl(string tabTitle, MainCommandCenterController mainCommandCenterController) {
            this.Header = tabTitle;
            this.MainController = mainCommandCenterController;
        }

        public void AddUpdateExecutionRecord(ExecutionRecordClass rec) {
            try {
                Application.Current.Dispatcher.Invoke(delegate {
                    ExecutionRecordClass r = TabExecutionRecords.FirstOrDefault<ExecutionRecordClass>(record => record.ExecutionLineID == rec.ExecutionLineID);

                    if (r != null) {
                        r.MM = rec.MM;
                        r.Sent = rec.Sent;
                        r.Name = rec.Name;
                        r.Type = rec.Type;
                        OnPropertyChanged("TabExecutionRecords");
                    } else {
                        TabExecutionRecords.Add(rec);
                        OnPropertyChanged("TabExecutionRecords");
                    }
                });
            } catch (Exception ex) {
                Console.WriteLine("Unmanaged error.   " + ex.Message);
            }
        }

        internal void TabSelected() {
            if (MainController?.View.RecordsCollection != null) {
                MainController?.View?.RecordsCollection.Clear();
                foreach (ExecutionRecordClass rec in this.TabExecutionRecords) {
                    MainController?.View?.RecordsCollection.Add(rec);
                }
                MainController?.View?.OnPropertyChanged("ExecutionRecords");
            }
            DrawConfig(DataModel);
        }

        internal void SetCompletionReportText(CompletionReport report) {
            Application.Current.Dispatcher.Invoke((Action)delegate {
                CompletionReport = report;
            });
        }

        public Canvas DrawConfig(XmlDocument xmlDoc) {
            if (xmlDoc == null) {
                return null;
            }
            maxOffset = Int32.MinValue;
            minOffset = Int32.MaxValue;

            panel = new Canvas();
            panel.Height = 1200;
            panel.Width = 800;

            panel.Children.Clear();

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

            MaxFlightOffsets();

            bool hasSource = (maxOffset != Int32.MinValue && minOffset != Int32.MaxValue);
            double ptr = topMargin;

            if (hasSource) {
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

                System.Windows.Shapes.Path arrowpath = GetResourceCopy<System.Windows.Shapes.Path>("arrow");

                Tuple<Polygon, System.Windows.Shapes.Path> p = DrawLineArrow(new Point(startArrrowX, startArrrowY), new Point(stopArrrowX, stopArrrowY), arrowpath);
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
                mpmlabel.SetValue(Control.FontWeightProperty, FontWeights.Bold);
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
                mpmlabel.SetValue(Control.FontWeightProperty, FontWeights.Bold);
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
                mpmlabel.SetValue(Control.FontWeightProperty, FontWeights.Bold);

                panel.Children.Add(mpmlabel);

                foreach (XmlNode node in amsdirectlines) {
                    // The Descriptive Panel
                    Border amsDirect = GetResourceCopy<Border>("amsDirect");

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
                    Tuple<Polygon, System.Windows.Shapes.Path> p = DrawLineArrow(new Point(startArrrowX, startArrrowY), new Point(stopArrrowX, stopArrrowY), GetResourceCopy<System.Windows.Shapes.Path>("arrow"));
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
                mpmlabel.SetValue(Control.FontWeightProperty, FontWeights.Bold);

                panel.Children.Add(mpmlabel);

                foreach (XmlNode node in destinationlines) {
                    Border dest = GetResourceCopy<Border>("dest");

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

                    Tuple<Polygon, System.Windows.Shapes.Path> p = DrawLineArrow(new Point(startArrrowX, startArrrowY), new Point(stopArrrowX, stopArrrowY), GetResourceCopy<System.Windows.Shapes.Path>("arrow"));
                    panel.Children.Add(p.Item1);
                    panel.Children.Add(p.Item2);

                    ptr += 35;
                }
            }

            return panel;
        }

        private double AddDataDriven(string basename, string lineName, string linevars, XmlNodeList nodes, double ptr, Brush grayBrush) {
            foreach (XmlNode node in nodes) {
                // The Descriptive Panel
                Border border = GetResourceCopy<Border>(basename);

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
                Tuple<Polygon, System.Windows.Shapes.Path> p = DrawLineArrow(new Point(stopArrrowX, stopArrrowY), new Point(startArrrowX, startArrrowY), GetResourceCopy<System.Windows.Shapes.Path>("arrow"));
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

            Tuple<Polygon, System.Windows.Shapes.Path> p = DrawLineArrow(new Point(stopArrrowX, stopArrrowY), new Point(startArrrowX, startArrrowY), GetResourceCopy<System.Windows.Shapes.Path>("arrow"));
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

        private double AddChainedLine(XmlNode node, double ptr, int depth) {
            SolidColorBrush grayBrush = new SolidColorBrush {
                Color = Colors.Gray
            };

            Border chainDriven = GetResourceCopy<Border>("chainDriven");

            FindChild<Label>(chainDriven, "chainDrivenLineName").Content = node.Attributes["name"].Value;
            FindChild<StackPanel>(chainDriven, "chainDrivenStack").SetValue(Control.WidthProperty, 346 - depth * chainIndent);
            FindChild<Label>(chainDriven, "chainDrivenLineDelay").Content = $"  Delay = {node.Attributes["delay"].Value} sec.";

            if (CheckDisabled(node)) {
                FindChild<Label>(chainDriven, "chainDrivenLineName").FontStyle = FontStyles.Italic;
                FindChild<Label>(chainDriven, "chainDrivenLineName").Foreground = grayBrush;
            }

            chainDriven.SetValue(Canvas.LeftProperty, lineMargin + depth * chainIndent);
            chainDriven.SetValue(Control.WidthProperty, 350 - depth * chainIndent);
            chainDriven.SetValue(Canvas.TopProperty, ptr);
            panel.Children.Add(chainDriven);

            // The Arrow pointing betweeen LoadInjector and the panel

            double stopArrrowY = ptr + 15.0;
            double startArrrowY = stopArrrowY;

            double startArrrowX = 60.0;
            double stopArrrowX = lineMargin + depth * chainIndent;

            Tuple<Polygon, System.Windows.Shapes.Path> p = DrawLineArrow(new Point(stopArrrowX, stopArrrowY), new Point(startArrrowX, startArrrowY), GetResourceCopy<System.Windows.Shapes.Path>("arrow"), true);
            panel.Children.Add(p.Item1);
            panel.Children.Add(p.Item2);

            ptr += 35;

            XmlNodeList chainlines = node.SelectNodes("./chained");

            foreach (XmlNode chain in chainlines) {
                ptr = AddChainedLine(chain, ptr, depth + 1);
            }

            return ptr;
        }

        private Tuple<Polygon, System.Windows.Shapes.Path> DrawLineArrow(Point startPoint, Point endPoint, System.Windows.Shapes.Path arrowpath, bool isChained = false) {
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

            return new Tuple<Polygon, System.Windows.Shapes.Path>(p, arrowpath);
        }

        private T GetResourceCopy<T>(string key) {
            T model = (T)MainController.View.FindResource(key);
            return ElementClone<T>(model);
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

        public void MaxFlightOffsets() {
            foreach (XmlNode node in DataModel.SelectNodes("//eventdrivensources/*")) {
                try {
                    if (Int32.TryParse(node.Attributes["flightSetFrom"]?.Value, out int fltSetFrom))
                        minOffset = Math.Min(fltSetFrom, minOffset);
                    if (Int32.TryParse(node.Attributes["flightSetTo"]?.Value, out int fltSetTo))
                        maxOffset = Math.Max(fltSetTo, maxOffset);
                } catch (Exception) {
                    // NO-OP
                }
            }

            XmlNodeList ratelines = DataModel.SelectNodes("//ratedriven");
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
    }

   

}