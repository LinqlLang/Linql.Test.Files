﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linql.Client.Json
{
    public class LinqlType 
    {
        public string TypeName { get; set; }

        public List<LinqlType> GenericParameters { get; set; }

        public LinqlType(string TypeName, List<LinqlType> GenericParameters) 
        {
            this.TypeName = TypeName;
            this.GenericParameters = GenericParameters;
        }

        public LinqlType(Type Type)
        {
            List<LinqlType> genericArgs = Type.IsConstructedGenericType ? Type.GetGenericArguments().Select(r => new LinqlType(r)).ToList() : null;

            if (typeof(LinqlSearch).IsAssignableFrom(Type))
            {
                this.TypeName = nameof(LinqlSearch);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(Type))
            {
                this.TypeName = "List";
            }
            else
            {
                this.TypeName = Type.Name;
            }
        }
    }
}
