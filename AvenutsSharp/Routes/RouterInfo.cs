﻿using AventusSharp.Routes.Request;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AventusSharp.Routes
{
    public class RouterInfo
    {
        public Regex pattern;
        public MethodType method;
        public MethodInfo action;
        public IRouter router;
        public int nbParamsFunction;
        public Dictionary<string, RouterParameterInfo> parameters = new Dictionary<string, RouterParameterInfo>();
        public string UniqueKey
        {
            get => pattern.ToString() + "||" + method.ToString();
        }

        public RouterInfo(Regex pattern, MethodType method, MethodInfo action, IRouter router, int nbParamsFunction)
        {
            this.pattern = pattern;
            this.method = method;
            this.action = action;
            this.router = router;
            this.nbParamsFunction = nbParamsFunction;
        }

        public override string ToString()
        {
            return pattern.ToString() + " " + method.ToString();
        }
    }

    public class RouterParameterInfo
    {
        public string name;
        public Type type;
        public int positionCSharp = -1;
        public int positionUrl = -1;

        public RouterParameterInfo(string name, Type type)
        {
            this.name = name;
            this.type = type;
        }
    }

}
