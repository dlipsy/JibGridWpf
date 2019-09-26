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
using System.Windows.Controls.Primitives;

namespace Jib.WPF.Controls.DataGrid
{
    public partial class ColumnOptionControl : UserControl, INotifyPropertyChanged
    {
        private FilterOperationItem _addPin = new FilterOperationItem(Enums.FilterOperation.Unknown, "Pin Column", "/Jib.WPF.Controls;component/Images/PinUp.png");
        private FilterOperationItem _addGroup = new FilterOperationItem(Enums.FilterOperation.Unknown, "Add Grouping", "/Jib.WPF.Controls;component/Images/GroupBy.png");
        private FilterOperationItem _removePin = new FilterOperationItem(Enums.FilterOperation.Unknown, "Unpin Column", "/Jib.WPF.Controls;component/Images/pinDown.png");
        private FilterOperationItem _removeGroup = new FilterOperationItem(Enums.FilterOperation.Unknown, "Remove Grouping", "/Jib.WPF.Controls;component/Images/RemoveGroupBy.png");

        public JibGrid Grid { get; set; }

        public OptionColumnInfo FilterColumnInfo { get; set; }

        public ObservableCollection<FilterOperationItem> ColumnOptions { get; private set; }

        private FilterOperationItem _SelectedColumnOptionItem;
        public FilterOperationItem SelectedColumnOptionItem
        {
            get { return _SelectedColumnOptionItem; }

            set
            {
                if (_SelectedColumnOptionItem != value)
                {
                    _SelectedColumnOptionItem = value;
                    OnPropertyChanged("SelectedColumnOptionItem");
                }
            }
        }

        private bool _CanUserFreeze;
        public bool CanUserFreeze
        {
            get { return _CanUserFreeze; }
            set
            {
                if (value != _CanUserFreeze)
                {
                    _CanUserFreeze = value;
                    OnPropertyChanged("CanUserFreeze");
                }
            }
        }

        private bool _CanUserGroup;
        public bool CanUserGroup
        {
            get { return _CanUserGroup; }
            set
            {
                if (value != _CanUserGroup)
                {
                    _CanUserGroup = value;
                    OnPropertyChanged("CanUserGroup");
                }
            }
        }


        public ColumnOptionControl()
        {

            ColumnOptions = new ObservableCollection<FilterOperationItem>();
            InitializeComponent();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = this;
                this.Loaded += new RoutedEventHandler(ColumnOptionControl_Loaded);
                cbOptions.DropDownOpened += new EventHandler(cbOptions_DropDownOpened);
            }
        }

        void cbOptions_DropDownOpened(object sender, EventArgs e)
        {
            ColumnOptions.Clear();
            if (CanUserFreeze)
            {
                if (Grid.IsFrozenColumn(FilterColumnInfo.Column))
                    ColumnOptions.Add(_removePin);
                else
                    ColumnOptions.Add(_addPin);
            }
            if (CanUserGroup)
            {
                if (Grid.IsGrouped(FilterColumnInfo.PropertyPath))
                    ColumnOptions.Add(_removeGroup);
                else
                    ColumnOptions.Add(_addGroup);
            }

        }

        void ColumnOptionControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Check the Grid for Building commmands and Visibility
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
                column = colHeader.Column;

            FilterColumnInfo = new OptionColumnInfo(column, Grid.FilterType);

            CanUserFreeze = Grid.CanUserFreeze;
            CanUserGroup = Grid.CanUserGroup;
            if (column != null)
            {
                object oCanUserFreeze = column.GetValue(ColumnConfiguration.CanUserFreezeProperty);
                if (oCanUserFreeze != null && (bool)oCanUserFreeze)
                    CanUserFreeze = (bool)oCanUserFreeze;

                object oCanUserGroup = column.GetValue(ColumnConfiguration.CanUserGroupProperty);
                if (oCanUserGroup != null && (bool)oCanUserGroup)
                    CanUserGroup = (bool)oCanUserGroup;
            }

            Grid.RegisterColumnOptionControl(this);
            ResetVisibility();

        }

        #region IPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        #endregion

        internal void ResetVisibility()
        {
            if ((!CanUserGroup && !CanUserFreeze) || string.IsNullOrWhiteSpace(FilterColumnInfo.PropertyPath))
                this.Visibility = System.Windows.Visibility.Collapsed;
            else
                this.Visibility = System.Windows.Visibility.Visible;
        }

        private void cbOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbOptions.IsDropDownOpen && SelectedColumnOptionItem != null)
            {
                switch (SelectedColumnOptionItem.Description)
                {
                    case "Pin Column":
                        Grid.FreezeColumn(FilterColumnInfo.Column);
                        break;
                    case "Add Grouping":
                        if (!string.IsNullOrWhiteSpace(FilterColumnInfo.PropertyPath))
                            Grid.AddGroup(FilterColumnInfo.PropertyPath);
                        break;
                    case "Unpin Column":
                        Grid.UnFreezeColumn(FilterColumnInfo.Column);
                        break;
                    case "Remove Grouping":
                        if (!string.IsNullOrWhiteSpace(FilterColumnInfo.PropertyPath))
                            Grid.RemoveGroup(FilterColumnInfo.PropertyPath);
                        break;
                }
                cbOptions.IsDropDownOpen = false;
            }
        }
    }
}
