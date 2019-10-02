using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Jib.WPF.Controls.DataGrid
{
    public delegate void FilterChangedEvent(object sender, FilterChangedEventArgs e);
    public delegate void CancelableFilterChangedEvent(object sender, CancelableFilterChangedEventArgs e);

    /// <summary>
    /// Interaction logic for JibGrid.xaml
    /// </summary>
    public partial class JibGrid : System.Windows.Controls.DataGrid, INotifyPropertyChanged
    {
        public event CancelableFilterChangedEvent BeforeFilterChanged;
        public event FilterChangedEvent AfterFilterChanged;

        private List<ColumnOptionControl> _optionControls = new List<ColumnOptionControl>();
        private PropertyChangedEventHandler _filterHandler;

        protected bool IsResetting { get; set; }

        public List<ColumnFilterControl> Filters { get; set; }
        public Type FilterType { get; set; }

        public bool IsFilterLoaded { get; set; }

        public int LastX { get; set; }

        protected ICollectionView CollectionView
        {
            get { return this.ItemsSource as ICollectionView; }
        }
        #region FilteredItemsSource DependencyProperty
        public static readonly DependencyProperty FilteredItemsSourceProperty =
                                                                DependencyProperty.Register("FilteredItemsSource", typeof(IEnumerable), typeof(JibGrid),
                                                                new PropertyMetadata(null, new PropertyChangedCallback(OnFilteredItemsSourceChanged)));

        public IEnumerable FilteredItemsSource
        {
            get { return (IEnumerable)GetValue(FilteredItemsSourceProperty); }
            set { SetValue(FilteredItemsSourceProperty, value); }
        }

        public static void OnFilteredItemsSourceChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            JibGrid g = sender as JibGrid;
            if (g != null)
            {
                var list = (IEnumerable)e.NewValue;
                var view = new CollectionViewSource();
                view.Source = list;
                Type srcT = e.NewValue.GetType().GetInterfaces().First(i => i.Name.StartsWith("IEnumerable"));
                g.FilterType = srcT.GetGenericArguments().First();
                g.ItemsSource = CollectionViewSource.GetDefaultView(list);
                if (g.Filters != null)
                    foreach (var filter in g.Filters)
                        filter.ResetControl();

            }
        }
#endregion

        #region Grouping Properties

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("False")]
        private bool _collapseLastGroup = false;
        public bool CollapseLastGroup
        {
            get { return _collapseLastGroup; }
            set
            {
                if (_collapseLastGroup != value)
                {
                    _collapseLastGroup = value;
                    OnPropertyChanged("CollapseLastGroup");
                }
            }
        }

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("False")]
        private bool _canUserGroup = false;
        public bool CanUserGroup
        {
            get { return _canUserGroup; }
            set
            {
                if (_canUserGroup != value)
                {
                    _canUserGroup = value;
                    OnPropertyChanged("CanUserGroup");
                    foreach (var optionControl in Filters)
                        optionControl.CanUserGroup = _canUserGroup;
                }
            }
        }

        #endregion Grouping Properties

        #region Freezing Properties

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("False")]
        private bool _canUserFreeze = false;
        public bool CanUserFreeze
        {
            get { return _canUserFreeze; }
            set
            {
                if (_canUserFreeze != value)
                {
                    _canUserFreeze = value;
                    OnPropertyChanged("CanUserFreeze");
                    foreach (var optionControl in Filters)
                        optionControl.CanUserFreeze = _canUserFreeze;
                }
            }
        }

        #endregion Freezing Properties

        #region Filter Properties

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("False")]
        private bool _canUserSelectDistinct = false;
        public bool CanUserSelectDistinct
        {
            get { return _canUserSelectDistinct; }
            set
            {
                if (_canUserSelectDistinct != value)
                {
                    _canUserSelectDistinct = value;
                    OnPropertyChanged("CanUserSelectDistinct");
                    foreach (var optionControl in Filters)
                        optionControl.CanUserSelectDistinct = _canUserSelectDistinct;
                }
            }
        }

        [Bindable(false)]
        [Category("Appearance")]
        [DefaultValue("True")]
        private bool _canUserFilter = true;
        public bool CanUserFilter
        {
            get { return _canUserFilter; }
            set
            {
                if (_canUserFilter != value)
                {
                    _canUserFilter = value;
                    OnPropertyChanged("CanUserFilter");
                    foreach (var optionControl in Filters)
                        optionControl.CanUserFilter = _canUserFilter;
                }
            }
        }
        #endregion Filter Properties
        public JibGrid()
        {
            Filters = new List<ColumnFilterControl>();
            _filterHandler = new PropertyChangedEventHandler(filter_PropertyChanged);
            InitializeComponent();
            Style = GetStyle("DataGridStyle");
            CellStyle = GetStyle("DataGridCellStyle");

            //in App.xaml in your application, you need to update the DataGridStyle and DataGridCellStyle styles
            //Jib.WPF.Testbed shows an example that conforms to the MahApps Teal light theme
        }

        static public Style GetStyle(string keyName)
        {
            object resource = Application.Current.TryFindResource(keyName);
            if (resource != null && resource.GetType() == typeof(Style))
                return (Style)resource;
            else
                return null;
        }


        /// <summary>
        /// Whenever any registered OptionControl raises the FilterChanged property changed event, we need to rebuild
        /// the new predicate used to filter the CollectionView.  Since Multiple Columns can have predicate we need to
        /// iterate over all registered OptionControls and get each predicate.
        /// </summary>
        /// <param name="sender">The object which has risen the event</param>
        /// <param name="e">The property which has been changed</param>
        void filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FilterChanged")
            {
                Predicate<object> predicate = null;
                foreach (var filter in Filters)
                    if (filter.HasPredicate)
                        if (predicate == null)
                            predicate = filter.GeneratePredicate();
                        else
                            predicate = predicate.And(filter.GeneratePredicate());
                bool canContinue = true;
                var args = new CancelableFilterChangedEventArgs(predicate);
                if (BeforeFilterChanged != null && !IsResetting)
                {
                    BeforeFilterChanged(this, args);
                    canContinue = !args.Cancel;
                }
                if (canContinue)
                {
                    ListCollectionView view = CollectionViewSource.GetDefaultView(this.ItemsSource) as ListCollectionView;
                    if (view != null && view.IsEditingItem)
                        view.CommitEdit();
                    if (view != null && view.IsAddingNew)
                        view.CommitNew();
                    if (CollectionView != null)
                        CollectionView.Filter = predicate;
                    if (AfterFilterChanged != null)
                        AfterFilterChanged(this, new FilterChangedEventArgs(predicate));
                }
                else
                {
                    IsResetting = true;
                    var ctrl = sender as ColumnFilterControl;
                    ctrl.ResetControl();
                    IsResetting = false;
                }
            }
        }
        
        internal void RegisterOptionControl(ColumnFilterControl ctrl)
        {
            if (!Filters.Contains(ctrl))
            {
                ctrl.PropertyChanged += _filterHandler;
                Filters.Add(ctrl);
            }
        }

        public void FirePredicationGeneration()
        {
            {
                Predicate<object> predicate = null;
                foreach (var filter in Filters)
                    if (filter.HasPredicate)
                        if (predicate == null)
                            predicate = filter.GeneratePredicate();
                        else
                            predicate = predicate.And(filter.GeneratePredicate());
                bool canContinue = true;
                var args = new CancelableFilterChangedEventArgs(predicate);
                if (BeforeFilterChanged != null && !IsResetting)
                {
                    BeforeFilterChanged(this, args);
                    canContinue = !args.Cancel;
                }
                if (canContinue)
                {
                    ListCollectionView view = CollectionViewSource.GetDefaultView(this.ItemsSource) as ListCollectionView;
                    if (view != null && view.IsEditingItem)
                        view.CommitEdit();
                    if (view != null && view.IsAddingNew)
                        view.CommitNew();
                    if (CollectionView != null)
                        CollectionView.Filter = predicate;
                    if (AfterFilterChanged != null)
                        AfterFilterChanged(this, new FilterChangedEventArgs(predicate));
                }
                else
                {
                    IsResetting = true;
                    IsResetting = false;
                }
            }
        }

        #region Grouping

        public void AddGroup(string boundPropertyName)
        {
            if (!string.IsNullOrWhiteSpace(boundPropertyName) && CollectionView != null && CollectionView.GroupDescriptions != null)
            {
                foreach (var groupedCol in CollectionView.GroupDescriptions)
                {
                    var propertyGroup = groupedCol as PropertyGroupDescription;

                    if (propertyGroup != null && propertyGroup.PropertyName == boundPropertyName)
                        return;
                }

                CollectionView.GroupDescriptions.Add(new PropertyGroupDescription(boundPropertyName));
            }
        }

        public bool IsGrouped(string boundPropertyName)
        {
            if (CollectionView != null && CollectionView.Groups != null)
            {
                foreach (var g in CollectionView.GroupDescriptions)
                {
                    var pgd = g as PropertyGroupDescription;

                    if (pgd != null)
                        if (pgd.PropertyName == boundPropertyName)
                            return true;
                }
            }

            return false;
        }

        public void RemoveGroup(string boundPropertyName)
        {
            if (!string.IsNullOrWhiteSpace(boundPropertyName) && CollectionView != null && CollectionView.GroupDescriptions != null)
            {
                PropertyGroupDescription selectedGroup = null;

                foreach (var groupedCol in CollectionView.GroupDescriptions)
                {
                    var propertyGroup = groupedCol as PropertyGroupDescription;

                    if (propertyGroup != null && propertyGroup.PropertyName == boundPropertyName)
                    {
                        selectedGroup = propertyGroup;
                    }
                }

                if (selectedGroup != null)
                    CollectionView.GroupDescriptions.Remove(selectedGroup);

                //if (CollapseLastGroup && CollectionView.Groups != null)
                    //foreach (CollectionViewGroup group in CollectionView.Groups)
                    //    RecursiveCollapse(group);
            }
        }

        public void ClearGroups()
        {
            if (CollectionView != null && CollectionView.GroupDescriptions != null)
                CollectionView.GroupDescriptions.Clear();
        }
        #endregion Grouping
        public List<T> GetVisualChildCollection<T>(object parent) where T : Visual
        {
            List<T> visualCollection = new List<T>();
            GetVisualChildCollection(parent as DependencyObject, visualCollection);
            return visualCollection;
        }

        #region Freezing

        public void FreezeColumn(DataGridColumn column)
        {
            if (this.Columns != null && this.Columns.Contains(column))
            {
                column.DisplayIndex = this.FrozenColumnCount;
                this.FrozenColumnCount++;
            }
        }
        public bool IsFrozenColumn(DataGridColumn column)
        {
            if (this.Columns != null && this.Columns.Contains(column))
            {
                return column.DisplayIndex < this.FrozenColumnCount;
            }
            else
            {
                return false;
            }
        }
        public void UnFreezeColumn(DataGridColumn column)
        {
            if (this.FrozenColumnCount > 0 && column.IsFrozen && this.Columns != null && this.Columns.Contains(column))
            {
                this.FrozenColumnCount--;
                column.DisplayIndex = this.FrozenColumnCount;
            }
        }

        public void UnFreezeAllColumns()
        {
            for (int i = Columns.Count - 1; i >= 0; i--)
                UnFreezeColumn(Columns[i]);
        }

        #endregion Freezing

        public void ShowFilter(DataGridColumn column, Visibility visibility)
        {
            var ctrl = Filters.Where(i => i.FilterColumnInfo.Column == column).FirstOrDefault();
            if (ctrl != null)
                ctrl.FilterVisibility = visibility;
        }

        public void ConfigureFilter(DataGridColumn column, bool canUserSelectDistinct, bool canUserGroup, bool canUserFreeze, bool canUserFilter)
        {
            column.SetValue(ColumnConfiguration.CanUserFilterProperty, canUserFilter);
            column.SetValue(ColumnConfiguration.CanUserFreezeProperty, canUserFreeze);
            column.SetValue(ColumnConfiguration.CanUserGroupProperty, canUserGroup);
            column.SetValue(ColumnConfiguration.CanUserSelectDistinctProperty, canUserSelectDistinct);
         
            var ctrl = Filters.Where(i => i.FilterColumnInfo.Column == column).FirstOrDefault();
            if (ctrl != null)
            {
                ctrl.CanUserSelectDistinct = canUserSelectDistinct;
                ctrl.CanUserGroup = canUserGroup;
                ctrl.CanUserFreeze = canUserFreeze;
                ctrl.CanUserFilter = canUserFilter;
            }
        }

        public void ResetDistinctLists()
        {
            foreach (var optionControl in Filters)
                optionControl.ResetDistinctList();
        }

        internal void RegisterColumnOptionControl(ColumnOptionControl columnOptionControl)
        {
            _optionControls.Add(columnOptionControl);
        }
        internal void UpdateColumnOptionControl(ColumnFilterControl columnFilterControl)
        {
            //Since visibility for column contrls is set off the ColumnFilterControl by the base grid, we need to 
            //update the ColumnOptionControl since it is a seperate object.
            var ctrl = _optionControls.Where(c => c.FilterColumnInfo != null && columnFilterControl.FilterColumnInfo != null && c.FilterColumnInfo.Column == columnFilterControl.FilterColumnInfo.Column).FirstOrDefault();
            if (ctrl != null)
                ctrl.ResetVisibility();
        }
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion



        private int GetColumnHeaderIndexFromColumn(DataGridColumn column)
        {
            List<DataGridColumnHeader> columnHeaders = GetVisualChildCollection<DataGridColumnHeader>(MainGrid).Where(c => c.Visibility == Visibility.Visible).ToList();
            int counter = 0;

            foreach (DataGridColumnHeader columnHeader in columnHeaders)
            {
                if (columnHeader.Column == column)
                {
                    return counter;
                }

                if (columnHeader.Column != null)
                {
                    counter++;
                }
            }
            return counter;
        }

        private void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection) where T : Visual
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T)
                {
                    visualCollection.Add(child as T);
                }
                else if (child != null)
                {
                    GetVisualChildCollection(child, visualCollection);
                }
            }
        }

        private void MenuHide_Click(object sender, RoutedEventArgs e)
        {
            var menuName = (MenuItem)e.Source;
            DataGridColumn column = Columns.Where(c => c.Header.ToString() == menuName.DataContext.ToString()).FirstOrDefault();

            int index = GetColumnHeaderIndexFromColumn(column);

            var visibleColumns = Columns.Where(c => c.Visibility == Visibility.Visible).ToList();
            for (int i = 0; i < visibleColumns.Count; i++)
            {
                if (index == i)
                {
                    visibleColumns[index].Visibility = Visibility.Hidden;
                    break;
                }
            }

            SaveReadingsDataGrid();
        }

        private void MenuRename_Click(object sender, RoutedEventArgs e)
        {
            var menuOption = (MenuItem)e.Source;

            UserEntryDialog dialog = new UserEntryDialog();
            dialog.Title = "Rename";
            dialog.Width = 300;
            dialog.ResponseTextBox.Text = menuOption.DataContext.ToString();
            dialog.ResponseTextBox.SelectAll();
            ColumnFilterControl.SetWindowPosition(dialog);

            if (dialog.ShowDialog() == true)
            {
                DataGridColumn column = Columns.Where(c => c.Header.ToString() == menuOption.DataContext.ToString()).FirstOrDefault();
                int index = GetColumnHeaderIndexFromColumn(column);

                var visibleColumns = Columns.Where(c => c.Visibility == Visibility.Visible).ToList();
                for (int i = 0; i < visibleColumns.Count; i++)
                {
                    if (index == i)
                    {
                        visibleColumns[index].Header = dialog.ResponseTextBox.Text;
                        break;
                    }
                }

                SaveReadingsDataGrid();
            }
            else
            {
                return;
            }
        }

        private void MenuShowAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Columns)
            {
                item.Visibility = Visibility.Visible;
            }

            SaveReadingsDataGrid();
        }

        private void SaveReadingsDataGrid()
        {
            //TODO: Implement saving of column detail in upcoming issue

            //var state = new GridColumnState();

            //var index = 0;
            //foreach (var dataGridColumn in MainGrid.Columns.OrderBy(c => c.DisplayIndex))
            //{
            //    var column = (DataGridBoundColumn)dataGridColumn;
            //    var binding = (Binding)column.Binding;
            //    var name = binding.Path.Path;
            //    var col = new GridColumn
            //    {
            //        Name = name,
            //        Header = column.Header.ToString(),
            //        Index = index++,
            //        Visibility = column.Visibility == Visibility.Visible
            //    };

            //    state.Add(col);
            //}
        }
    }
}
