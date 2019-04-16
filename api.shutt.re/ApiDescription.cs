using System;
using System.Collections.Generic;

namespace api.shutt.re
{
    public class ApiDescription
    {
        public static string EmptyPayload => "This API ignores the payload, if any.";
        public string Url { get; set; }
        public IEnumerable<ApiDescriptionArgument> Arguments { get; set; }
        public string PayloadDescription { get; set; }
        public string Comment { get; set; }

    }
    public class ApiDescriptionArgument
    {
        public string Argument { get; }
        public string Description { get; }
        public static List<ApiDescriptionArgument> Empty => new List<ApiDescriptionArgument>();

        public ApiDescriptionArgument(string argument, string description)
        {
            Argument = argument;
            Description = description;
        }
    }

}