using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jib.WPF.Controls.DataGrid
{
    public class FilterOperationItem
    {
        public Enums.FilterOperation FilterOption { get; set; }
        public string ImagePath { get; set; }
        public string Description { get; set; }
        public bool NeedsFilterValue { get; set; }

        public FilterOperationItem(Enums.FilterOperation operation, string description, string imagePath, bool needsFilterValue = true)
        {
            FilterOption = operation;
            Description = description;
            ImagePath = imagePath;
            NeedsFilterValue = needsFilterValue;
        }
        public override string ToString()
        {
            return Description;
        }
    }
}
