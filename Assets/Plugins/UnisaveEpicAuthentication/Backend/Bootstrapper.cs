using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Unisave.Foundation
{
    /// <summary>
    /// TODO: this is a dummy implementation of a bootstrapping system
    /// that needs to be added to Unisave Framework
    /// </summary>
    public abstract class Bootstrapper
    {
        /// <summary>
        /// This is like the "Program.Main()" method in console applications,
        /// it is executed when the bootstrapper is meant to run.
        /// </summary>
        public abstract void Main();
        
        // -----------------------------------------------

        private static bool hasRun = false;
        
        public static void AssertRan()
        {
            if (hasRun)
                return;

            Type bootstrapperType = typeof(Bootstrapper);
            List<Type> bootstrapperTypes = new List<Type>();
            
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                foreach (Type type in asm.GetTypes())
                    if (!type.IsAbstract && bootstrapperType.IsAssignableFrom(type))
                        bootstrapperTypes.Add(type);

            List<Bootstrapper> bootstrappers = bootstrapperTypes.Select(
                t => (Bootstrapper) Activator.CreateInstance(t)
            ).ToList();
            
            // TODO: order them by declared dependencies
            
            // run them
            foreach (Bootstrapper b in bootstrappers)
                b.Main();
            
            hasRun = true;
        }
    }
}