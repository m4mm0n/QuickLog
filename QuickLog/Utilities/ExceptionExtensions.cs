using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace QuickLog.Utilities
{
    /// <summary>
    /// Extension methods that produce readable exception text similar to Ben.Demystifier's ToStringDemystified,
    /// but implemented internally with zero external dependencies.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Returns a readable exception + stack trace string with async/iterator frames remapped
        /// to their original user methods and with cleaner method/type formatting.
        /// </summary>
        public static string ToStringDemystified(this Exception ex, bool filterInfrastructureFrames = true)
        {
            if (ex is null) return string.Empty;

            var sb = new StringBuilder(256);
            WriteExceptionTree(sb, ex, 0, filterInfrastructureFrames);
            return sb.ToString();
        }

        private static void WriteExceptionTree(StringBuilder sb, Exception ex, int depth, bool filter)
        {
            // Header for this exception
            if (depth == 0)
            {
                sb.Append(ex.GetType().FullName);
            }
            else
            {
                sb.Append(" ---> ").Append(ex.GetType().FullName);
            }

            if (!string.IsNullOrWhiteSpace(ex.Message))
            {
                sb.Append(": ").Append(ex.Message);
            }
            sb.AppendLine();

            // Stack trace (demystified)
            var trace = new StackTrace(ex, true);
            var frames = trace.GetFrames() ?? Array.Empty<StackFrame>();
            foreach (var frame in frames.Select(f => FrameFormatter.Create(f)).Where(f => f != null)!)
            {
                if (filter && FrameFormatter.IsInfrastructure(frame!)) continue;
                sb.Append("   at ").Append(FrameFormatter.Format(frame!)).AppendLine();
            }

            // Inner exceptions
            if (ex is AggregateException agg && agg.InnerExceptions?.Count > 0)
            {
                foreach (var inner in agg.InnerExceptions)
                    WriteExceptionTree(sb, inner, depth + 1, filter);
            }
            else if (ex.InnerException != null)
            {
                WriteExceptionTree(sb, ex.InnerException, depth + 1, filter);
            }

            // Footer for inner exception chains
            if (depth > 0)
            {
                sb.Append("   --- End of inner exception stack trace ---").AppendLine();
            }
        }

        /// <summary>Lightweight model of a stack frame after remapping/demystifying.</summary>
        private sealed record DemystifiedFrame(
            MethodBase Method,
            string? File,
            int Line,
            int Column
        );

        private static class FrameFormatter
        {
            private static readonly HashSet<string> _infraNamespaces = new(StringComparer.Ordinal)
            {
                "System.Runtime.CompilerServices",
                "System.Runtime.ExceptionServices",
                "System.Threading.Tasks",
                "System.Threading",
                "System.Runtime.InteropServices",
                "System.Reflection",
            };

            public static bool IsInfrastructure(DemystifiedFrame f)
            {
                var dt = f.Method.DeclaringType;
                if (dt == null) return false;

                // Trim obvious await/task/lambda plumbing to reduce noise.
                var ns = dt.Namespace ?? string.Empty;
                if (_infraNamespaces.Any(ns.StartsWith)) return true;

                // Compiler generated state machines / display classes
                if (IsCompilerGenerated(dt)) return true;

                // Task/Thread pool invocations
                if (dt == typeof(System.Threading.ExecutionContext)) return true;
                if (dt == typeof(System.Threading.ThreadPool)) return true;

                return false;
            }

            public static DemystifiedFrame? Create(StackFrame frame)
            {
                var method = frame.GetMethod();
                if (method == null) return null;

                method = RemapStateMachineMethod(method) ?? method;

                return new DemystifiedFrame(
                    method,
                    frame.GetFileName(),
                    frame.GetFileLineNumber(),
                    frame.GetFileColumnNumber()
                );
            }

            public static string Format(DemystifiedFrame f)
            {
                var methodText = FormatMethod(f.Method);
                if (!string.IsNullOrEmpty(f.File) && f.Line > 0)
                {
                    // Same style Exception.ToString uses when PDB is present
                    return $"{methodText} in {TrimPath(f.File)}:line {f.Line}";
                }
                return methodText;
            }

            private static string TrimPath(string path)
            {
                // Keep it short but predictable: filename + a bit of tail if it helps
                try
                {
                    return System.IO.Path.GetFileName(path) ?? path;
                }
                catch
                {
                    return path;
                }
            }

            private static bool IsCompilerGenerated(MemberInfo mi) =>
                mi.GetCustomAttribute<CompilerGeneratedAttribute>() != null ||
                (mi.DeclaringType != null && mi.DeclaringType.GetCustomAttribute<CompilerGeneratedAttribute>() != null);

            /// <summary>
            /// If the frame points to a state machine's MoveNext, remap it back to the original async/iterator method.
            /// </summary>
            private static MethodBase? RemapStateMachineMethod(MethodBase method)
            {
                if (method.Name != "MoveNext" || method.DeclaringType == null) return null;

                var stateMachineType = method.DeclaringType;
                if (!IsCompilerGenerated(stateMachineType)) return null;

                // Find any method in the parent that has AsyncStateMachine or IteratorStateMachine equal to this generated type.
                var parent = stateMachineType.DeclaringType;
                if (parent == null) return null;

                foreach (var candidate in parent.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var asyncAttr = candidate.GetCustomAttribute<AsyncStateMachineAttribute>();
                    if (asyncAttr?.StateMachineType == stateMachineType) return candidate;

                    var iterAttr = candidate.GetCustomAttribute<IteratorStateMachineAttribute>();
                    if (iterAttr?.StateMachineType == stateMachineType) return candidate;
                }

                return null;
            }

            private static readonly Dictionary<Type, string> _keywordTypes = new()
            {
                [typeof(void)] = "void",
                [typeof(bool)] = "bool",
                [typeof(byte)] = "byte",
                [typeof(sbyte)] = "sbyte",
                [typeof(char)] = "char",
                [typeof(decimal)] = "decimal",
                [typeof(double)] = "double",
                [typeof(float)] = "float",
                [typeof(int)] = "int",
                [typeof(uint)] = "uint",
                [typeof(nint)] = "nint",
                [typeof(nuint)] = "nuint",
                [typeof(long)] = "long",
                [typeof(ulong)] = "ulong",
                [typeof(short)] = "short",
                [typeof(ushort)] = "ushort",
                [typeof(object)] = "object",
                [typeof(string)] = "string",
            };

            private static string FormatMethod(MethodBase method)
            {
                var sb = new StringBuilder();

                // Type/namespace
                if (method.DeclaringType != null)
                {
                    sb.Append(FormatTypeName(method.DeclaringType));
                    sb.Append('.');
                }

                // Method name / operators / lambdas
                sb.Append(PrettifyMethodName(method.Name));

                // Generic arguments on method
                if (method.IsGenericMethod)
                {
                    var args = method.GetGenericArguments();
                    sb.Append('<').Append(string.Join(", ", args.Select(FormatTypeName))).Append('>');
                }

                // Parameter list
                sb.Append('(');
                var parameters = method.GetParameters();
                if (parameters.Length > 0)
                {
                    var rendered = parameters.Select(p =>
                    {
                        var prefix =
                            p.IsOut ? "out " :
                            p.ParameterType.IsByRef && !p.IsOut ? "ref " : string.Empty;

                        var t = p.ParameterType;
                        if (t.IsByRef) t = t.GetElementType()!;

                        return $"{prefix}{FormatTypeName(t)} {p.Name}";
                    });
                    sb.Append(string.Join(", ", rendered));
                }
                sb.Append(')');

                return sb.ToString();
            }

            private static string PrettifyMethodName(string name)
            {
                // Normalize compiler generated names:
                //   <MyMethod>b__12_0  => MyMethod::<lambda>
                //   MoveNext          => already handled by state machine remap
                if (name.StartsWith("<", StringComparison.Ordinal))
                {
                    var close = name.IndexOf('>');
                    if (close > 1)
                    {
                        var owner = name.Substring(1, close - 1);
                        // Detect lambda vs local function
                        if (name.Contains("b__"))
                            return owner + "::lambda";
                        return owner; // local function / iterator helper
                    }
                }
                return name;
            }

            private static string FormatTypeName(Type t)
            {
                if (_keywordTypes.TryGetValue(t, out var keyword))
                    return keyword;

                if (t.IsGenericType)
                {
                    // Nullable<T> -> T?
                    if (t.GetGenericTypeDefinition() == typeof(Nullable<>))
                        return FormatTypeName(t.GetGenericArguments()[0]) + "?";

                    // ValueTuple<T1,...> -> (T1, T2, ...)
                    if (IsValueTuple(t))
                    {
                        var parts = t.GetGenericArguments().Select(FormatTypeName);
                        return "(" + string.Join(", ", parts) + ")";
                    }

                    var name = t.Name;
                    var tick = name.IndexOf('`');
                    if (tick >= 0) name = name.Substring(0, tick);

                    var args = t.GetGenericArguments().Select(FormatTypeName);
                    var typePrefix = t.Namespace is { Length: > 0 } ns ? ns + "." : string.Empty;

                    // Nested types: format Outer.Inner
                    if (t.IsNested && t.DeclaringType != null)
                        typePrefix = FormatTypeName(t.DeclaringType) + ".";

                    return $"{typePrefix}{name}<{string.Join(", ", args)}>";
                }

                if (t.IsArray)
                {
                    var rank = t.GetArrayRank();
                    return FormatTypeName(t.GetElementType()!) + "[" + new string(',', rank - 1) + "]";
                }

                // Nested type?
                if (t.IsNested && t.DeclaringType != null)
                    return FormatTypeName(t.DeclaringType) + "." + t.Name;

                // Full name, but avoid + for nested and use dots
                if (!string.IsNullOrEmpty(t.FullName))
                    return t.FullName!.Replace('+', '.');

                return t.Name;
            }

            private static bool IsValueTuple(Type t)
            {
                if (!t.IsGenericType) return false;
                var def = t.GetGenericTypeDefinition();
                return def == typeof(ValueTuple<>) ||
                       def == typeof(ValueTuple<,>) ||
                       def == typeof(ValueTuple<,,>) ||
                       def == typeof(ValueTuple<,,,>) ||
                       def == typeof(ValueTuple<,,,,>) ||
                       def == typeof(ValueTuple<,,,,,>) ||
                       def == typeof(ValueTuple<,,,,,,>) ||
                       def == typeof(ValueTuple<,,,,,,,>);
            }
        }
    }
}
