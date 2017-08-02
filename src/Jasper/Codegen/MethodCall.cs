﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Baseline;
using Baseline.Reflection;
using Jasper.Codegen.Compilation;

namespace Jasper.Codegen
{
    public class MethodCall : Frame
    {
        public Dictionary<Type, Type> Aliases { get; } = new Dictionary<Type, Type>();

        public Type HandlerType { get; }
        public MethodInfo Method { get; }
        public Variable ReturnVariable { get; private set; }

        public static MethodCall For<T>(Expression<Action<T>> expression)
        {
            var method = ReflectionHelper.GetMethod(expression);

            return new MethodCall(typeof(T), method);
        }


        public MethodCall(Type handlerType, MethodInfo method) : base(method.IsAsync())
        {
            HandlerType = handlerType;
            Method = method;

            if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
            {
                var variableType = method.ReturnType.CanBeCastTo<Task>()
                    ? method.ReturnType.GetGenericArguments().First()
                    : method.ReturnType;

                var name = variableType.IsSimple() || variableType == typeof(object) || variableType == typeof(object[])
                    ? "result_of_" + method.Name
                    : Variable.DefaultArgName(variableType);

                ReturnVariable = new Variable(variableType, name, this);
            }

            Variables = new Variable[method.GetParameters().Length];
        }

        /// <summary>
        /// Call a method on the current object
        /// </summary>
        public bool IsLocal { get; set; }

        public Variable Target { get; set; }


        private Variable findVariable(ParameterInfo param, GeneratedMethod chain)
        {
            var type = param.ParameterType;

            if (Aliases.ContainsKey(type))
            {
                var actualType = Aliases[type];
                var inner = chain.FindVariable(actualType);
                return new CastVariable(inner, type);
            }

            Variable variable;
            return chain.TryFindVariableByName(type, param.Name, out variable) ? variable : chain.FindVariable(type);
        }

        public Variable[] Variables { get; }

        public bool TrySetParameter(Variable variable)
        {
            var parameters = Method.GetParameters().Select(x => x.ParameterType).ToArray();
            if (parameters.Count(x => variable.VariableType.CanBeCastTo(x)) == 1)
            {
                var index = Array.IndexOf(parameters, variable.VariableType);
                Variables[index] = variable;

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool TrySetParameter(string parameterName, Variable variable)
        {
            var parameters = Method.GetParameters().ToArray();
            var matching = parameters.FirstOrDefault(x =>
                variable.VariableType.CanBeCastTo(x.ParameterType) && x.Name == parameterName);

            if (matching == null) return false;

            var index = Array.IndexOf(parameters, matching);
            Variables[index] = variable;

            return true;
        }

        protected override IEnumerable<Variable> resolveVariables(GeneratedMethod chain)
        {
            var parameters = Method.GetParameters().ToArray();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (Variables[i] != null)
                {
                    continue;
                }

                var param = parameters[i];
                Variables[i] = findVariable(param, chain);
            }

            foreach (var variable in Variables)
            {
                yield return variable;
            }

            if (!Method.IsStatic && !IsLocal)
            {
                if (Target == null)
                {
                    Target = chain.FindVariable(HandlerType);
                }

                yield return Target;
            }
        }




        public override void GenerateCode(GeneratedMethod method, ISourceWriter writer)
        {
            var methodName = Method.Name;
            if (Method.IsGenericMethod)
            {
                methodName += $"<{Method.GetGenericArguments().Select(x => x.FullName).Join(", ")}>";
            }

            var callingCode = $"{methodName}({Variables.Select(x => x.Usage).Join(", ")})";
            var target = determineTarget();

            var returnValue = "";
            var suffix = "";

            if (IsAsync)
            {
                if (method.AsyncMode == AsyncMode.ReturnFromLastNode)
                {
                    returnValue = "return ";
                }
                else
                {
                    returnValue = "await ";
                    suffix = ".ConfigureAwait(false)";
                }
            }

            if (ReturnVariable != null)
            {
                returnValue = $"var {ReturnVariable.Usage} = {returnValue}";
            }

            // TODO -- will need to see if it's IDisposable too

            writer.Write($"{returnValue}{target}{callingCode}{suffix};");

            Next?.GenerateCode(method, writer);
        }

        private string determineTarget()
        {
            if (IsLocal) return string.Empty;

            var target = Method.IsStatic
                ? HandlerType.NameInCode()
                : Target.Usage;

            return target + ".";
        }


        public override bool CanReturnTask()
        {
            return IsAsync;
        }

        public override string ToString()
        {
            return $"{nameof(HandlerType)}: {HandlerType}, {nameof(Method)}: {Method}";
        }
    }
}
