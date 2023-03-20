using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonReader
{
    public class University
    {
        public static readonly byte[] StateProvince_UTF8  = Encoding.UTF8.GetBytes("state-province");
        public static readonly byte[] Domains_UTF8 = Encoding.UTF8.GetBytes("domains");
        public static readonly byte[] Country_UTF8 = Encoding.UTF8.GetBytes("country");
        public static readonly byte[] WebPages_UTF8 = Encoding.UTF8.GetBytes("web_pages");
        public static readonly byte[] Name_UTF8 = Encoding.UTF8.GetBytes("name");
        public static readonly byte[] AlphaTwoCode_UTF8 = Encoding.UTF8.GetBytes("alpha_two_code");
        public static readonly byte[] Sports_UTF8 = Encoding.UTF8.GetBytes("sports");

        public string StateProvince { get; set; }
        public List<string> Domains { get; set; }
        public string Country { get; set; }
        public List<string> WebPages { get; set; }
        public string Name { get; set; }
        public string AlphaTwoCode { get; set; }

        public List<Sport> Sports { get; set; }
    }

    public class Sport {
        public static readonly byte[] Type_UTF8 = Encoding.UTF8.GetBytes("type");
        public static readonly byte[] Location_UTF8 = Encoding.UTF8.GetBytes("location");

        public string Type { get; set; }
        public string Location { get; set; }
    }

}
