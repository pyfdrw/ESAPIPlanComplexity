using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using MoreLinq;

namespace PlanComplexity.Script
{
    public class CustomDataGrid : DataGrid
    {
        public event ExecutedRoutedEventHandler ExecutePasteEvent;
        public event CanExecuteRoutedEventHandler CanExecutePasteEvent;

        // ******************************************************************
        static CustomDataGrid()
        {
            CommandManager.RegisterClassCommandBinding(
                typeof(CustomDataGrid),
                new CommandBinding(ApplicationCommands.Paste,
                    new ExecutedRoutedEventHandler(OnExecutedPasteInternal),
                    new CanExecuteRoutedEventHandler(OnCanExecutePasteInternal)));
        }

        // ******************************************************************
        #region Clipboard Paste

        // ******************************************************************
        private static void OnCanExecutePasteInternal(object target, CanExecuteRoutedEventArgs args)
        {
            ((CustomDataGrid)target).OnCanExecutePaste(target, args);
        }

        // ******************************************************************
        /// <summary>
        /// This virtual method is called when ApplicationCommands.Paste command query its state.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCanExecutePaste(object target, CanExecuteRoutedEventArgs args)
        {
            if (CanExecutePasteEvent != null)
            {
                CanExecutePasteEvent(target, args);
                if (args.Handled)
                {
                    return;
                }
            }

            args.CanExecute = CurrentCell != null;
            args.Handled = true;
        }

        // ******************************************************************
        private static void OnExecutedPasteInternal(object target, ExecutedRoutedEventArgs args)
        {
            ((CustomDataGrid)target).OnExecutedPaste(target, args);
        }

        // ******************************************************************
        /// <summary>
        /// This virtual method is called when ApplicationCommands.Paste command is executed.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="args"></param>
        protected virtual void OnExecutedPaste(object target, ExecutedRoutedEventArgs args)
        {
            if (ExecutePasteEvent != null)
            {
                ExecutePasteEvent(target, args);
                if (args.Handled)
                {
                    return;
                }
            }

            // parse the clipboard data            [row][column]
            List<string[]> clipboardData = ClipboardHelper2.ParseClipboardData();

            bool hasAddedNewRow = false;

            Debug.Print(">>> DataGrid Paste: >>>");
#if DEBUG
            StringBuilder sb = new StringBuilder();
#endif
            int minRowIndex = Items.IndexOf(CurrentItem);
            int maxRowIndex = Items.Count - 1;
            int startIndexOfDisplayCol = (SelectionUnit != DataGridSelectionUnit.FullRow) ? CurrentColumn.DisplayIndex : 0;
            int clipboardRowIndex = 0;
            for (int i = minRowIndex; i <= maxRowIndex && clipboardRowIndex < clipboardData.Count; i++, clipboardRowIndex++)
            {
                if (i < this.Items.Count)
                {
                    CurrentItem = Items[i];

                    BeginEditCommand.Execute(null, this);

                    int clipboardColumnIndex = 0;
                    for (int j = startIndexOfDisplayCol; clipboardColumnIndex < clipboardData[clipboardRowIndex].Length; j++, clipboardColumnIndex++)
                    {
                        // DataGridColumn column = ColumnFromDisplayIndex(j);
                        DataGridColumn column = null;
                        foreach (DataGridColumn columnIter in this.Columns)
                        {
                            if (columnIter.DisplayIndex == j)
                            {
                                column = columnIter;
                                break;
                            }
                        }

                        column?.OnPastingCellClipboardContent(Items[i], clipboardData[clipboardRowIndex][clipboardColumnIndex]);

#if DEBUG
                        sb.AppendFormat("{0,-10}", clipboardData[clipboardRowIndex][clipboardColumnIndex]);
                        sb.Append(" - ");
#endif
                    }

                    CommitEditCommand.Execute(this, this);
                    if (i == maxRowIndex)
                    {
                        maxRowIndex++;
                        hasAddedNewRow = true;
                    }
                }


#if DEBUG
                Debug.Print(sb.ToString());
                sb.Clear();
#endif
            }

            // update selection
            if (hasAddedNewRow)
            {
                UnselectAll();
                UnselectAllCells();

                CurrentItem = Items[minRowIndex];

                if (SelectionUnit == DataGridSelectionUnit.FullRow)
                {
                    SelectedItem = Items[minRowIndex];
                }
                else if (SelectionUnit == DataGridSelectionUnit.CellOrRowHeader ||
                         SelectionUnit == DataGridSelectionUnit.Cell)
                {
                    SelectedCells.Add(new DataGridCellInfo(Items[minRowIndex], Columns[startIndexOfDisplayCol]));

                }
            }
        }

        // ******************************************************************
        /// <summary>
        ///     Whether the end-user can add new rows to the ItemsSource.
        /// </summary>
        public bool CanUserPasteToNewRows
        {
            get { return (bool)GetValue(CanUserPasteToNewRowsProperty); }
            set { SetValue(CanUserPasteToNewRowsProperty, value); }
        }

        // ******************************************************************
        /// <summary>
        ///     DependencyProperty for CanUserAddRows.
        /// </summary>
        public static readonly DependencyProperty CanUserPasteToNewRowsProperty =
            DependencyProperty.Register("CanUserPasteToNewRows",
                                        typeof(bool), typeof(CustomDataGrid),
                                        new FrameworkPropertyMetadata(true, null, null));

        // ******************************************************************
        #endregion Clipboard Paste

        private void SetGridToSupportManyEditEitherWhenValidationErrorExists()
        {
            this.Items.CurrentChanged += Items_CurrentChanged;


            //Type DatagridType = this.GetType().BaseType;
            //PropertyInfo HasCellValidationProperty = DatagridType.GetProperty("HasCellValidationError", BindingFlags.NonPublic | BindingFlags.Instance);
            //HasCellValidationProperty.
        }

        void Items_CurrentChanged(object sender, EventArgs e)
        {
            //this.Items[0].
            //throw new NotImplementedException();
        }

        // ******************************************************************
        private void SetGridWritable()
        {
            Type DatagridType = this.GetType().BaseType;
            PropertyInfo HasCellValidationProperty = DatagridType.GetProperty("HasCellValidationError", BindingFlags.NonPublic | BindingFlags.Instance);
            if (HasCellValidationProperty != null)
            {
                HasCellValidationProperty.SetValue(this, false, null);
            }
        }

        // ******************************************************************
        public void SetGridWritableEx()
        {
            BindingFlags bindingFlags = BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo cellErrorInfo = this.GetType().BaseType.GetProperty("HasCellValidationError", bindingFlags);
            PropertyInfo rowErrorInfo = this.GetType().BaseType.GetProperty("HasRowValidationError", bindingFlags);
            cellErrorInfo.SetValue(this, false, null);
            rowErrorInfo.SetValue(this, false, null);
        }

        // ******************************************************************
    }

    public static class ClipboardHelper2
    {
        public delegate string[] ParseFormat(string value);

        public static List<string[]> ParseClipboardData()
        {
            List<string[]> clipboardData = new List<string[]>();

            // get the data and set the parsing method based on the format
            // currently works with CSV and Text DataFormats            
            IDataObject dataObj = System.Windows.Clipboard.GetDataObject();

            if (dataObj != null)
            {
                string[] formats = dataObj.GetFormats();
                if (formats.Contains(DataFormats.CommaSeparatedValue))
                {
                    string clipboardString = (string)dataObj.GetData(DataFormats.CommaSeparatedValue);
                    {
                        // EO: Subject to error when a CRLF is included as part of the data but it work for the moment and I will let it like it is
                        // WARNING ! Subject to errors
                        string[] lines = clipboardString.Split(new string[] { "\r\n" }, StringSplitOptions.None);

                        string[] lineValues;
                        foreach (string line in lines)
                        {
                            lineValues = CsvHelper.ParseLineCommaSeparated(line);
                            if (lineValues != null)
                            {
                                clipboardData.Add(lineValues);
                            }
                        }
                    }
                }
                else if (formats.Contains(DataFormats.Text))
                {
                    string clipboardString = (string)dataObj.GetData(DataFormats.Text);
                    clipboardData = CsvHelper.ParseText(clipboardString);
                }
            }

            return clipboardData;
        }
    }

    public class CsvHelper
    {
        public static Dictionary<LineSeparator, Func<string, string[]>> DictionaryOfLineSeparatorAndItsFunc = new Dictionary<LineSeparator, Func<string, string[]>>();

        static CsvHelper()
        {
            DictionaryOfLineSeparatorAndItsFunc[LineSeparator.Unknown] = ParseLineNotSeparated;
            DictionaryOfLineSeparatorAndItsFunc[LineSeparator.Tab] = ParseLineTabSeparated;
            DictionaryOfLineSeparatorAndItsFunc[LineSeparator.Semicolon] = ParseLineSemicolonSeparated;
            DictionaryOfLineSeparatorAndItsFunc[LineSeparator.Comma] = ParseLineCommaSeparated;
        }

        // ******************************************************************
        public enum LineSeparator
        {
            Unknown = 0,
            Tab,
            Semicolon,
            Comma
        }

        // ******************************************************************
        public static LineSeparator GuessCsvSeparator(string oneLine)
        {
            List<Tuple<LineSeparator, int>> listOfLineSeparatorAndThereFirstLineSeparatedValueCount = new List<Tuple<LineSeparator, int>>();

            listOfLineSeparatorAndThereFirstLineSeparatedValueCount.Add(new Tuple<LineSeparator, int>(LineSeparator.Tab, CsvHelper.ParseLineTabSeparated(oneLine).Count()));
            listOfLineSeparatorAndThereFirstLineSeparatedValueCount.Add(new Tuple<LineSeparator, int>(LineSeparator.Semicolon, CsvHelper.ParseLineSemicolonSeparated(oneLine).Count()));
            listOfLineSeparatorAndThereFirstLineSeparatedValueCount.Add(new Tuple<LineSeparator, int>(LineSeparator.Comma, CsvHelper.ParseLineCommaSeparated(oneLine).Count()));

            Tuple<LineSeparator, int> bestBet = listOfLineSeparatorAndThereFirstLineSeparatedValueCount.MaxBy((n) => n.Item2).FirstOrDefault();

            if (bestBet != null && bestBet.Item2 > 1)
            {
                return bestBet.Item1;
            }

            return LineSeparator.Unknown;
        }

        // ******************************************************************
        public static string[] ParseLineCommaSeparated(string line)
        {
            // CSV line parsing : From "jgr4" in http://www.kimgentes.com/worshiptech-web-tools-page/2008/10/14/regex-pattern-for-parsing-csv-files-with-embedded-commas-dou.html
            var matches = Regex.Matches(line, @"\s?((?<x>(?=[,]+))|""(?<x>([^""]|"""")+)""|""(?<x>)""|(?<x>[^,]+)),?",
                                        RegexOptions.ExplicitCapture);

            string[] values = (from Match m in matches
                               select m.Groups["x"].Value.Trim().Replace("\"\"", "\"")).ToArray();

            return values;
        }

        // ******************************************************************
        public static string[] ParseLineTabSeparated(string line)
        {
            var matchesTab = Regex.Matches(line, @"\s?((?<x>(?=[\t]+))|""(?<x>([^""]|"""")+)""|""(?<x>)""|(?<x>[^\t]+))\t?",
                            RegexOptions.ExplicitCapture);

            string[] values = (from Match m in matchesTab
                               select m.Groups["x"].Value.Trim().Replace("\"\"", "\"")).ToArray();

            return values;
        }

        // ******************************************************************
        public static string[] ParseLineSemicolonSeparated(string line)
        {
            // CSV line parsing : From "jgr4" in http://www.kimgentes.com/worshiptech-web-tools-page/2008/10/14/regex-pattern-for-parsing-csv-files-with-embedded-commas-dou.html
            var matches = Regex.Matches(line, @"\s?((?<x>(?=[;]+))|""(?<x>([^""]|"""")+)""|""(?<x>)""|(?<x>[^;]+));?",
                                        RegexOptions.ExplicitCapture);

            string[] values = (from Match m in matches
                               select m.Groups["x"].Value.Trim().Replace("\"\"", "\"")).ToArray();

            return values;
        }

        // ******************************************************************
        public static string[] ParseLineNotSeparated(string line)
        {
            string[] lineValues = new string[1];
            lineValues[0] = line;
            return lineValues;
        }

        // ******************************************************************
        public static List<string[]> ParseText(string text)
        {
            string[] lines = text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            return ParseString(lines);
        }

        // ******************************************************************
        public static List<string[]> ParseString(string[] lines)
        {
            List<string[]> result = new List<string[]>();

            LineSeparator lineSeparator = LineSeparator.Unknown;
            if (lines.Any())
            {
                lineSeparator = GuessCsvSeparator(lines[0]);
            }

            Func<string, string[]> funcParse = DictionaryOfLineSeparatorAndItsFunc[lineSeparator];

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                result.Add(funcParse(line));
            }

            return result;
        }

        // ******************************************************************
    }
}
