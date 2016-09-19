using System;
using System.Collections.Generic;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.Mount;
using Microsoft.TemplateEngine.Core.Contracts;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros;
using Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Macros.Config;
using Microsoft.TemplateEngine.Utils;
using Newtonsoft.Json.Linq;

namespace Microsoft.TemplateEngine.Orchestrator.RunnableProjects.Config
{
    public class MacrosOperationConfig : IOperationConfig
    {
        private static IReadOnlyDictionary<string, IMacro> _macros;

        private static IReadOnlyDictionary<String, IMacroConfig> _macroConfigs;

        public Guid Id => new Guid("B03E4760-455F-48B1-9FF2-79ADB1E91519");

        public string Key => "macros";

        public int Order => -10000;

        public IEnumerable<IOperationProvider> Setup(IComponentManager componentManager, IReadOnlyList<IMacroConfig> macroConfigs, IVariableCollection variables, IParameterSet parameters)
        {
            EnsureMacros(componentManager);

            ParameterSetter setter = (p, value) =>
            {
                ((RunnableProjectGenerator.ParameterSet)parameters).AddParameter(p);
                parameters.ResolvedValues[p] = value;
            };

            IList<IMacroConfig> allMacroConfigs = new List<IMacroConfig>(macroConfigs);

            // TODO: finish this, along with ResolveDeferredMacroConfig()
            foreach (IMacroConfig config in macroConfigs)
            {
                GeneratedSymbolDeferredMacroConfig deferredConfig = config as GeneratedSymbolDeferredMacroConfig;
                if (deferredConfig == null)
                {
                    continue;
                }

                // setup the actual macro config, add it to the all MacroConfigs
                IMacroConfig macroConfigObject;
                if (_macroConfigs.TryGetValue(deferredConfig.Type, out macroConfigObject))
                {
                    IMacroConfig actualConfig = macroConfigObject.ConfigFromDeferredConfig(deferredConfig);
                    allMacroConfigs.Add(actualConfig);
                }
            }

            foreach (IMacroConfig config in allMacroConfigs)
            {
                IMacro macroObject;
                if (_macros.TryGetValue(config.Type, out macroObject))
                {
                    macroObject.EvaluateConfig(variables, config, parameters, setter);
                }
            }

            return Empty<IOperationProvider>.List.Value;
        }

        public IEnumerable<IOperationProvider> Process(IComponentManager componentManager, JObject rawConfiguration, IDirectory templateRoot, IVariableCollection variables, IParameterSet parameters)
        {
            EnsureMacros(componentManager);

            ParameterSetter setter = (p, value) =>
            {
                ((RunnableProjectGenerator.ParameterSet) parameters).AddParameter(p);
                parameters.ResolvedValues[p] = value;
            };

            foreach (JProperty property in rawConfiguration.Properties())
            {
                string variableName = property.Name;
                JObject def = (JObject)property.Value;
                string macroType = def["type"].ToString();

                IMacro macroObject;
                if (_macros.TryGetValue(macroType, out macroObject))
                {
                    macroObject.Evaluate(variableName, variables, def, parameters, setter);
                }
            }

            return Empty<IOperationProvider>.List.Value;
        }

        private static void EnsureMacros(IComponentManager componentManager)
        {
            if (_macros == null)
            {
                Dictionary<string, IMacro> macros = new Dictionary<string, IMacro>();

                foreach (IMacro macro in componentManager.OfType<IMacro>())
                {
                    macros[macro.Type] = macro;
                }

                _macros = macros;
            }

            if (_macroConfigs == null)
            {
                Dictionary<string, IMacroConfig> macroConfigs = new Dictionary<string, IMacroConfig>();

                foreach (IMacroConfig config in componentManager.OfType<IMacroConfig>())
                {
                    macroConfigs[config.Type] = config;
                }

                _macroConfigs = macroConfigs;
            }
        }
    }
}