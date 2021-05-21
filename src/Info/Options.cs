﻿using System.Collections.Generic;
using CommandLine;

namespace scalus.Info
{
    [Verb("info", HelpText = "Show information about the current scalus configuration")]
    public class Options : IVerb
    {
        [Option('p', "property", Required = false, HelpText = "Show description of DTO property")]
        public string Property { get; set; }

        [Option('d', "dto", Required = false, HelpText = "Show information about the DTO")]
        public bool Dto { get; set; }


    }
}