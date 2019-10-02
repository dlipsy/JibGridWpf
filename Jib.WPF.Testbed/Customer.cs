using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jib.WPF.Testbed
{
    public class Customer
    {
        public string City { get; set; }
        public string Address { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string ContactTitle { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string PostalCode { get; set; }
        public string Region { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }

        public DateTime LastModified { get; set; } 
            = DateTime.Now.Subtract(
                new TimeSpan(
                    Helpers.GetNextRandomValueBetween(0, 35), //num days 
                    Helpers.GetNextRandomValueBetween(0, 23), //num hrs
                    Helpers.GetNextRandomValueBetween(0, 59), //num min 
                    Helpers.GetNextRandomValueBetween(0, 59))); //num seconds 
        public Customer() { IsActive = true; }
    }
}
