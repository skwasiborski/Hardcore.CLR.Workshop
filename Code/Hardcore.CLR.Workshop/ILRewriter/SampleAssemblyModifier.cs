using System;
using System.Linq;
using System.Reflection;
using ILRewriter.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILRewriter
{
    public interface IInterceptor
    {
        object Intercept(object target, object[] args, MethodBase method);
    }

    public class SampleInterceptor : IInterceptor
    {
        public object Intercept(object target, object[] args, MethodBase method)
        {
            Console.WriteLine($"method: {method}");
            Console.WriteLine($"target: {target}");
            Console.WriteLine(string.Join(", ", args.Select((v, i) => $"arg{i}: <{v?.ToString()}>")));
            return 1;
        }
    }

    public interface ILogger
    {
        void WriteLine(string text);

        void WriteLine(string text, string arg);
    }

    public class SampleLogger : ILogger
    {
        public void WriteLine(string text)
        {
            Console.WriteLine($"From Logger: {text}");
        }

        public void WriteLine(string text, string arg)
        {
            Console.WriteLine($"From Logger: {text}", arg);
        }
    }

    public static class LoggerRegistry
    {
        public static ILogger GetLogger() => new SampleLogger();
    }

    public static class InterceptorRegistry
    {
        public static IInterceptor GetInterceptor() => new SampleInterceptor();
    }

    public class SampleAssemblyModifier : IAssemblyModifier
    {
        public void Modify(AssemblyDefinition assembly)
        {
            var mainModule = assembly.MainModule;

            var getInterceptorMethod = mainModule.Import(typeof(InterceptorRegistry).GetMethod(nameof(InterceptorRegistry.GetInterceptor)));
            var interceptMethod = mainModule.Import(typeof(IInterceptor).GetMethod(nameof(IInterceptor.Intercept)));
            var objectReference = mainModule.Import(typeof(object));
            var getMethodFromHandle =
                mainModule.Import(typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle),
                    new Type[] {typeof(RuntimeMethodHandle), typeof(RuntimeTypeHandle)}));

            var targetType = mainModule.Types.FirstOrDefault(t => t.Name == "SampleClassWithInstanceMethod");
            if (targetType == null)
                return;

            var targetMethod = targetType.Methods.First(m => m.Name == "DoSomething");
            var methodBody = targetMethod.Body;

            var IL = methodBody.GetILProcessor();

            var instructions = methodBody.Instructions.ToArray();

            methodBody.Instructions.Clear();

            var argsArray = targetMethod.AddLocal<object[]>();

            foreach (var instruction in instructions)
            {
                if (instruction.OpCode != OpCodes.Call)
                {
                    IL.Append(instruction);
                    continue;
                }

                var methodCall = ((MethodReference) instruction.Operand);
                var parameterCount = methodCall.Parameters.Count;
                var localParameters = new VariableDefinition[parameterCount];

                var thisParameter = targetMethod.AddLocal<object>();

                for (int i = 0 ; i < parameterCount; i++)
                {
                    localParameters[i] = targetMethod.AddLocal<object>();
                    if (methodCall.Parameters[i].ParameterType.IsValueType)
                    {
                        IL.Emit(OpCodes.Box, methodCall.Parameters[i].ParameterType);
                    }

                    IL.Emit(OpCodes.Stloc, localParameters[i]);
                }

                if (!methodCall.Resolve().IsStatic)
                {
                    IL.Emit(OpCodes.Stloc, thisParameter);
                }
                else
                {
                    IL.Emit(OpCodes.Ldnull);
                    IL.Emit(OpCodes.Stloc, thisParameter);
                }

                IL.Emit(OpCodes.Call, getInterceptorMethod);
                IL.Emit(OpCodes.Ldc_I4, parameterCount);
                IL.Emit(OpCodes.Newarr, objectReference);
                IL.Emit(OpCodes.Stloc, argsArray);

                int j = 0;
                foreach (var variableDefinition in localParameters.Reverse())
                {
                    IL.Emit(OpCodes.Ldloc, argsArray);

                    IL.Emit(OpCodes.Ldc_I4, j++);
                    IL.Emit(OpCodes.Ldloc, variableDefinition);

                    IL.Emit(OpCodes.Stelem_Ref);
                }

                IL.Emit(OpCodes.Ldloc, thisParameter);
                IL.Emit(OpCodes.Ldloc, argsArray);
                IL.Emit(OpCodes.Ldtoken, methodCall);
                IL.Emit(OpCodes.Ldtoken, methodCall.DeclaringType);
                IL.Emit(OpCodes.Call, getMethodFromHandle);

                IL.Emit(OpCodes.Callvirt, interceptMethod);

                if (methodCall.ReturnType.MetadataType == MetadataType.Void)
                {
                    IL.Emit(OpCodes.Pop);
                }
                else
                {
                    IL.Emit(OpCodes.Unbox_Any, methodCall.ReturnType);
                }
            }
        }
    }
}