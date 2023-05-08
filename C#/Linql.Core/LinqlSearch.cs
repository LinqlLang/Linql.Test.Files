﻿using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Linql.Core
{
    /// <summary>
    /// A LinqlSearch is the container for LinqlExpressions.  It defines the top-most entrypoint of the query.  
    /// </summary>
    public class LinqlSearch
    {
        /// <summary>
        /// The type of search.  This is the entry point of the query.
        /// </summary>
        public LinqlType Type { get; set; }

        /// <summary>
        /// The expressions of the search.
        /// </summary>
        public List<LinqlExpression> Expressions { get; set; } = null;

        /// <summary>
        /// Creates a new LinqlSearch
        /// </summary>
        /// <param name="Type">The entry point type</param>
        public LinqlSearch(LinqlType Type)
        {
            this.Type = Type;
        }

        /// <summary>
        /// Creates a new LinqlSearch
        /// </summary>
        /// <param name="Type">The entry point type</param>
        public LinqlSearch(Type Type) : this(new LinqlType(Type)) { }

        /// <summary>
        /// This constructor is required for Json serialization/deserialization.  Should probably not use this.
        /// </summary>
        public LinqlSearch()
        {

        }

        public override string ToString()
        {
            if(this.Type == null)
            {
                return "LinqlSearch";
            }
            return $"LinqlSearch<{this.Type.ToString()}>";
        }
    }
}
