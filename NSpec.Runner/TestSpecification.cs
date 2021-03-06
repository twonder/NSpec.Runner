﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using NSpec.Domain.Formatters;
using PostSharp.Aspects;

namespace NSpec.Runner
{
    /// <summary>
    /// The aspect that gets added as an attribute to a test function.  
    /// It intercepts the function call an builds the nspec contexts for 
    /// the function and then invokes the function.
    /// </summary>
    [Serializable]
    public class TestSpecification : MethodInterceptionAspect
    {
        [XmlArrayItem(Type = typeof(MethodBase))]
        [XmlArray]
        private static List<MethodBase> methodsThatHaveBeenPrepared = new List<MethodBase>();

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            // if the method has been prepared, we need to actually execute the function
            if (methodsThatHaveBeenPrepared.Contains(args.Method))
            {
                base.OnInvoke(args);
                return;
            }

            // mark that the method has been prepared
            methodsThatHaveBeenPrepared.Add(args.Method);

            // prepare the npsec content of the function
            var finder = new SingleClassGetter(args.Instance.GetType());
            var builder = new MethodContextBuilder(finder, new DefaultConventions());
            var runner = new MethodContextRunner(builder, new ConsoleFormatter(), false);

            // set up the contexts for the method
            var methodInfo = args.Method as MethodInfo;
            var contexts = builder.MethodContext(methodInfo);

            // run the contexts
            var builtContexts = contexts.Build();
            var results = runner.Run(builtContexts);

            // if there were any failures, raise the exception so that the test framework has an error
            if (results.Failures().Any())
            {
                throw new TestFailedException();
            }

            // tests all passed
        }
    }
}
