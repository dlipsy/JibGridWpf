﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Controls.Primitives;

namespace Jib.WPF.Controls.DataGrid
{
    /// <summary>
    /// Interaction logic for ColumnFilterControl.xaml
    /// </summary>
    public partial class ColumnFilterControl : UserControl, INotifyPropertyChanged
    {
        private Func<object, object> _boundColumnPropertyAccessor = null;

        #region Properties

        public ObservableCollection<FilterOperationItem> FilterOperations { get; set; }

        public ObservableCollection<CheckboxComboItem> DistinctPropertyValues { get; set; }

        public bool HasPredicate { get { return FilterText.Length > 0 || DistinctPropertyValues.Where(d => d.IsChecked).Count() > 0; } }

        public OptionColumnInfo FilterColumnInfo { get; set; }

        public JibGrid Grid { get; set; }

        private bool _CanUserFreeze = true;
        public bool CanUserFreeze
        {
            get
            {
                return _CanUserFreeze;
            }
            set
            {
                _CanUserFreeze = value;
                Grid.UpdateColumnOptionControl(this);
                OnPropertyChanged("CanUserFreeze");
            }
        }

        private bool _CanUserGroup;
        public bool CanUserGroup
        {
            get
            {
                return _CanUserGroup;
            }
            set
            {
                _CanUserGroup = value;
                Grid.UpdateColumnOptionControl(this);
                OnPropertyChanged("CanUserGroup");
            }
        }

        private bool _CanUserFilter = true;
        public bool CanUserFilter
        {
            get
            {
                return _CanUserFilter;
            }
            set
            {
                _CanUserFilter = value;
                CalcControlVisibility();
            }
        }

        private bool _CanUserSelectDistinct = false;
        public bool CanUserSelectDistinct
        {
            get
            {
                return _CanUserSelectDistinct;
            }
            set
            {
                _CanUserSelectDistinct = value;
                CalcControlVisibility();
            }
        }

        public Visibility FilterVisibility
        {
            get
            {
                return this.Visibility;
            }
            set
            {
                this.Visibility = value;
            }
        }

        public bool FilterReadOnly
        {
            get { return DistinctPropertyValues.Where(i => i.IsChecked).Count() > 0; }
        }

        public bool FilterOperationsEnabled
        {
            get { return DistinctPropertyValues.Where(i => i.IsChecked).Count() == 0; }
        }


        public Brush FilterBackGround
        {
            get
            {
                if (DistinctPropertyValues.Where(i => i.IsChecked).Count() > 0)
                    return SystemColors.ControlBrush;
                else
                    return Brushes.White;
            }
        }
        private string _FilterText = string.Empty;
        public string FilterText
        {
            get { return _FilterText; }
            set
            {
                if (value != _FilterText)
                {
                    _FilterText = value;
                    OnPropertyChanged("FilterText");
                    OnPropertyChanged("FilterChanged");
                }
            }
        }

        private int FilterPeriod;


        private FilterOperationItem _SelectedFilterOperation;
        public FilterOperationItem SelectedFilterOperation
        {
            get
            {
                if (DistinctPropertyValues.Where(i => i.IsChecked).Count() > 0)
                    return FilterOperations.Where(f => f.FilterOption == Enums.FilterOperation.Equals).FirstOrDefault();
                return _SelectedFilterOperation;
            }
            set
            {
                if (value != _SelectedFilterOperation)
                {
                    _SelectedFilterOperation = value;
                    OnPropertyChanged("SelectedFilterOperation");
                    OnPropertyChanged("FilterChanged");
                }
            }
        }
        #endregion

        public ColumnFilterControl()
        {
            DistinctPropertyValues = new ObservableCollection<CheckboxComboItem>();
            FilterOperations = new ObservableCollection<FilterOperationItem>();
            InitializeComponent();



            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = this;
                this.Loaded += new RoutedEventHandler(ColumnFilterControl_Loaded);
            }
        }


        void ColumnFilterControl_Loaded(object sender, RoutedEventArgs e)
        {
            DataGridColumn column = null;
            DataGridColumnHeader colHeader = null;

            UIElement parent = (UIElement)VisualTreeHelper.GetParent(this);
            while (parent != null)
            {
                parent = (UIElement)VisualTreeHelper.GetParent(parent);
                if (colHeader == null)
                    colHeader = parent as DataGridColumnHeader;

                if (Grid == null)
                    Grid = parent as JibGrid;
            }

            if (colHeader != null)
                column =  colHeader.Column;
        
            CanUserFilter = Grid.CanUserFilter;
            CanUserFreeze = Grid.CanUserFreeze;
            CanUserGroup = Grid.CanUserGroup;
            CanUserSelectDistinct = Grid.CanUserSelectDistinct;


            if (column != null)
            {
                object oCanUserFilter = column.GetValue(ColumnConfiguration.CanUserFilterProperty);
                if (oCanUserFilter != null)
                    CanUserFilter = (bool)oCanUserFilter;

                object oCanUserFreeze = column.GetValue(ColumnConfiguration.CanUserFreezeProperty);
                if (oCanUserFreeze != null)
                    CanUserFreeze = (bool)oCanUserFreeze;

                object oCanUserGroup = column.GetValue(ColumnConfiguration.CanUserGroupProperty);
                if (oCanUserGroup != null)
                    CanUserGroup = (bool)oCanUserGroup;

                object oCanUserSelectDistinct = column.GetValue(ColumnConfiguration.CanUserSelectDistinctProperty);
                if (oCanUserSelectDistinct != null)
                    CanUserSelectDistinct = (bool)oCanUserSelectDistinct;
            }


            if (Grid.FilterType == null)
                return;

            FilterColumnInfo = new OptionColumnInfo(column, Grid.FilterType);

            Grid.RegisterOptionControl(this);

            FilterOperations.Clear();
            if (FilterColumnInfo.PropertyType != null)
            {
                if (TypeHelper.IsStringType(FilterColumnInfo.PropertyType))
                {
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.Contains, "Contains", "/Jib.WPF.Controls;component/Images/Contains.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.StartsWith, "Starts With", "/Jib.WPF.Controls;component/Images/StartsWith.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.EndsWith, "Ends With", "/Jib.WPF.Controls;component/Images/EndsWith.png"));
                }
                FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.Equals, "Equals", "/Jib.WPF.Controls;component/Images/Equal.png"));
                if (TypeHelper.IsNumbericType(FilterColumnInfo.PropertyType))
                {
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.GreaterThan, "Greater Than", "/Jib.WPF.Controls;component/Images/GreaterThan.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.GreaterThanEqual, "Greater Than or Equal", "/Jib.WPF.Controls;component/Images/GreaterThanEqual.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.LessThan, "Less Than", "/Jib.WPF.Controls;component/Images/LessThan.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.LessThanEqual, "Less Than or Equal", "/Jib.WPF.Controls;component/Images/LessThanEqual.png"));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.NotEquals, "Not Equal", "/Jib.WPF.Controls;component/Images/NotEqual.png"));
                }
                if (TypeHelper.IsDateTimeType(FilterColumnInfo.PropertyType))
                {
                    CanUserSelectDistinct = false;
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.Today, "Today", "/Jib.WPF.Controls;component/Images/Today.png", false));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.Yesterday, "Yesterday", "/Jib.WPF.Controls;component/Images/Yesterday.png", false));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.LastXDays, "Last X Days", "/Jib.WPF.Controls;component/Images/LastX.png", false));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.LastXWeeks, "Last X Weeks", "/Jib.WPF.Controls;component/Images/LastX.png", false));
                    FilterOperations.Add(new FilterOperationItem(Enums.FilterOperation.LastXMonths, "Last X Months", "/Jib.WPF.Controls;component/Images/LastX.png", false));
                }
                SelectedFilterOperation = FilterOperations[0];
            }

            if (FilterColumnInfo != null && FilterColumnInfo.IsValid)
            {
                foreach (var i in DistinctPropertyValues.Where(i => i.IsChecked))
                    i.IsChecked = false;
                DistinctPropertyValues.Clear();
                FilterText = string.Empty;
                _boundColumnPropertyAccessor = null;

                if (!string.IsNullOrWhiteSpace(FilterColumnInfo.PropertyPath))
                {
                    if (FilterColumnInfo.PropertyPath.Contains('.'))
                        throw new ArgumentException(string.Format("This version of the grid does not support a nested property path such as '{0}'.  Please make a first-level property for filtering and bind to that.", FilterColumnInfo.PropertyPath));

                    this.Visibility = System.Windows.Visibility.Visible;
                    ParameterExpression arg = System.Linq.Expressions.Expression.Parameter(typeof(object), "x");
                    System.Linq.Expressions.Expression expr = System.Linq.Expressions.Expression.Convert(arg, Grid.FilterType);
                    expr = System.Linq.Expressions.Expression.Property(expr, Grid.FilterType, FilterColumnInfo.PropertyPath);
                    System.Linq.Expressions.Expression conversion = System.Linq.Expressions.Expression.Convert(expr, typeof(object));
                    _boundColumnPropertyAccessor = System.Linq.Expressions.Expression.Lambda<Func<object, object>>(conversion, arg).Compile();
                }
                else
                {
                    this.Visibility = System.Windows.Visibility.Collapsed;
                }
                object oDefaultFilter = column.GetValue(ColumnConfiguration.DefaultFilterProperty);
                if (oDefaultFilter != null)
                    FilterText = (string)oDefaultFilter;
            }

            CalcControlVisibility();

          
        }
        
        private void ExecutePredicateGeneration(string value)
        {
            Grid.FirePredicationGeneration();
            ResetControl();
        }

        private void txtFilter_Loaded(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).DataContext = this;
        }

        private void txtFilter_KeyUp(object sender, KeyEventArgs e)
        {
            FilterText = ((TextBox)sender).Text;
        }

        public Predicate<object> GeneratePredicate()
        {
            Predicate<object> predicate = null;
            if (DistinctPropertyValues.Where(i => i.IsChecked).Count() > 0)
            {
                foreach (var item in DistinctPropertyValues.Where(i => i.IsChecked))
                {
                    if (predicate == null)
                        predicate = GenerateFilterPredicate(FilterColumnInfo.PropertyPath, item.Tag.ToString(), Grid.FilterType, FilterColumnInfo.PropertyType, SelectedFilterOperation);
                    else
                        predicate = predicate.Or(GenerateFilterPredicate(FilterColumnInfo.PropertyPath, item.Tag.ToString(), Grid.FilterType, FilterColumnInfo.PropertyType.UnderlyingSystemType, SelectedFilterOperation));
                }
            }
            else
            {
                predicate = GenerateFilterPredicate(FilterColumnInfo.PropertyPath, FilterText, Grid.FilterType, FilterColumnInfo.PropertyType.UnderlyingSystemType, SelectedFilterOperation);
            }
            return predicate;
        }

        protected Predicate<object> GenerateFilterPredicate(string propertyName, string filterValue, Type objType, Type propType, FilterOperationItem filterItem)
        {
            ParameterExpression objParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "x");
            UnaryExpression param = System.Linq.Expressions.Expression.TypeAs(objParam, objType);
            var prop = System.Linq.Expressions.Expression.Property(param, propertyName);
            var val = System.Linq.Expressions.Expression.Constant(filterValue);

            switch (filterItem.FilterOption)
            {
                case Enums.FilterOperation.Contains:
                    return ExpressionHelper.GenerateGeneric(prop, val, propType, objParam, "Contains");
                case Enums.FilterOperation.EndsWith:
                    return ExpressionHelper.GenerateGeneric(prop, val, propType, objParam, "EndsWith");
                case Enums.FilterOperation.StartsWith:
                    return ExpressionHelper.GenerateGeneric(prop, val, propType, objParam, "StartsWith");
                case Enums.FilterOperation.Equals:
                    return ExpressionHelper.GenerateEquals(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.NotEquals:
                    return ExpressionHelper.GenerateNotEquals(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.GreaterThanEqual:
                    return ExpressionHelper.GenerateGreaterThanEqual(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.LessThanEqual:
                    return ExpressionHelper.GenerateLessThanEqual(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.GreaterThan:
                    return ExpressionHelper.GenerateGreaterThan(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.LessThan:
                    return ExpressionHelper.GenerateLessThan(prop, filterValue, propType, objParam);
                case Enums.FilterOperation.Today:
                    return ExpressionHelper.GenerateBetweenValues(prop, DateTime.Today.ToString(), DateTime.Today.AddDays(1.0).ToString(), propType, objParam);

                case Enums.FilterOperation.Yesterday:
                    return ExpressionHelper.GenerateBetweenValues(prop, DateTime.Today.AddDays(-1.0).ToString(), DateTime.Today.ToString(), propType, objParam);

                case Enums.FilterOperation.LastXDays:
                    if (FilterPeriod == 0)
                        return ExpressionHelper.GenerateBetweenValues(prop, DateTime.Today.AddDays(-1.0).ToString(), DateTime.Today.ToString(), propType, objParam);
                    else
                        return ExpressionHelper.GenerateBetweenValues(prop, DateTime.Today.AddDays(-1 * FilterPeriod).ToString(), DateTime.Today.ToString(), propType, objParam);

                case Enums.FilterOperation.LastXWeeks:
                    if (FilterPeriod == 0)
                        return ExpressionHelper.GenerateBetweenValues(prop, DateTime.Today.AddDays(-7.0).ToString(), DateTime.Today.ToString(), propType, objParam);
                    else
                        return ExpressionHelper.GenerateBetweenValues(prop, DateTime.Today.AddDays(-7 * FilterPeriod).ToString(), DateTime.Today.ToString(), propType, objParam);

                case Enums.FilterOperation.LastXMonths:
                    if (FilterPeriod == 0)
                        return ExpressionHelper.GenerateBetweenValues(prop, DateTime.Today.AddMonths(-1).ToString(), DateTime.Today.ToString(), propType, objParam);
                    else
                        return ExpressionHelper.GenerateBetweenValues(prop, DateTime.Today.AddMonths(-1 * FilterPeriod).ToString(), DateTime.Today.ToString(), propType, objParam);

                default:
                    throw new ArgumentException("Could not decode Search Mode.  Did you add a new value to the enum, or send in Unknown?");
            }

        }

        public void ResetControl()
        {
            foreach (var i in DistinctPropertyValues)
                i.IsChecked = false;
            FilterText = string.Empty;

            DistinctPropertyValues.Clear();
        }
        public void ResetDistinctList()
        {
            DistinctPropertyValues.Clear();
        }
        private void CalcControlVisibility()
        {
            if (CanUserFilter)
            {
                cbOperation.Visibility = System.Windows.Visibility.Visible;
                if (CanUserSelectDistinct)
                {
                    cbDistinctProperties.Visibility = System.Windows.Visibility.Visible;
                    txtFilter.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    cbDistinctProperties.Visibility = System.Windows.Visibility.Collapsed;
                    txtFilter.Visibility = System.Windows.Visibility.Visible;
                }
            }
            else
            {
                cbOperation.Visibility = System.Windows.Visibility.Collapsed;
                cbDistinctProperties.Visibility = System.Windows.Visibility.Collapsed;
                txtFilter.Visibility = System.Windows.Visibility.Collapsed;
            }
        }



        private void cbDistinctProperties_DropDownOpened(object sender, EventArgs e)
        {
            if (_boundColumnPropertyAccessor != null)
            {
                if (DistinctPropertyValues.Count == 0)
                {
                    List<object> result = new List<object>();
                    foreach (var i in Grid.FilteredItemsSource)
                    {
                        object value = _boundColumnPropertyAccessor(i);
                        if (value != null)
                            if (result.Where(o => o.ToString() == value.ToString()).Count() == 0)
                                result.Add(value);
                    }
                    try
                    {
                        result.Sort();
                    }
                    catch
                    {
                        if (System.Diagnostics.Debugger.IsLogging())
                            System.Diagnostics.Debugger.Log(0, "Warning", "There is no default compare set for the object type");
                    }
                    foreach (var obj in result)
                    {
                        var item = new CheckboxComboItem()
                        {
                            Description = GetFormattedValue(obj),
                            Tag = obj,
                            IsChecked = false
                        };
                        item.PropertyChanged += new PropertyChangedEventHandler(filter_PropertyChanged);
                        DistinctPropertyValues.Add(item);
                    }
                }
            }
        }

        private string GetFormattedValue(object obj)
        {
            if (FilterColumnInfo.Converter != null)
                return FilterColumnInfo.Converter.Convert(obj, typeof(string), FilterColumnInfo.ConverterParameter, FilterColumnInfo.ConverterCultureInfo).ToString();
            else
                return obj.ToString();
        }

        void filter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var list = DistinctPropertyValues.Where(i => i.IsChecked).ToList();
            if (list.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var i in DistinctPropertyValues.Where(i => i.IsChecked))
                    sb.AppendFormat("{0}{1}", sb.Length > 0 ? "," : "", i);
                FilterText = sb.ToString();
            }
            else
            {
                FilterText = string.Empty;
            }
            OnPropertyChanged("FilterReadOnly");
            OnPropertyChanged("FilterBackGround");
            OnPropertyChanged("FilterOperationsEnabled");
            OnPropertyChanged("SelectedFilterOperation");
        }

        #region IPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        #endregion

        private void CbOperation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var filterOperationItem = e.AddedItems[0] as FilterOperationItem;

                switch (filterOperationItem.Description)
                {                  
                    case "Last X Days":
                        if (Grid.IsFilterLoaded)
                        {
                            FilterPeriod = Grid.LastX;
                        }                       

                        ExecutePredicateGeneration(FilterPeriod.ToString());

                        break;

                    case "Last X Months":
                        if (Grid.IsFilterLoaded)
                        {
                            FilterPeriod = Grid.LastX;
                        }

                        ExecutePredicateGeneration(FilterPeriod.ToString());

                        break;

                    case "Last X Weeks":
                        if (Grid.IsFilterLoaded)
                        {
                            FilterPeriod = Grid.LastX;
                        }

                        ExecutePredicateGeneration(FilterPeriod.ToString());

                        break;
                }

                if (filterOperationItem != null && !filterOperationItem.NeedsFilterValue)
                {
                    if (DoesFilterTextNeedToBeEmpty(filterOperationItem))
                    {
                        FilterText = " ";
                    }
                }
            }
        }
        private bool DoesFilterTextNeedToBeEmpty(FilterOperationItem filterOperationItem)
        {
            if ((filterOperationItem.FilterOption == Enums.FilterOperation.LastXDays || filterOperationItem.FilterOption == Enums.FilterOperation.LastXWeeks || filterOperationItem.FilterOption == Enums.FilterOperation.LastXMonths) && FilterPeriod == 0)
            {
                return false;
            }

            return true;
        }

    }
}
