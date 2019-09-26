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
        public Customer() { IsActive = true; }
    }
}
