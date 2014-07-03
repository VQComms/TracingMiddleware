namespace TracingMiddleware
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;

    public delegate void Trace(string message);

    public delegate string MessageFormat(string key, string value);

    public delegate string TypeFormat(object value);

    public class TracingMiddlewareOptions
    {
        public static readonly MessageFormat DefaultMessageFormat =
            (key, value) => string.Format("{0} : {1}", key, value);

        public static readonly TypeFormat DefaultTypeFormat = value => value.ToString();
        public static readonly Trace ConsoleTrace = Console.WriteLine;
        public static readonly TracingMiddlewareOptions Default;

        private readonly TypeFormat defaultTypeFormat;
        private readonly List<Predicate<string>> ignoreKeyPredicates = new List<Predicate<string>>();
        private readonly HashSet<string> ignoreKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Type> ignoreTypes = new HashSet<Type>();
        private readonly List<Predicate<string>> includeKeyPredicates = new List<Predicate<string>>();
        private readonly HashSet<string> includeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Type> includeTypes = new HashSet<Type>();
        private readonly Func<bool> isEnabled;
        private readonly Dictionary<string, Trace> keyTracers = new Dictionary<string, Trace>();
        private readonly MessageFormat messageFormat;

        private readonly ConcurrentDictionary<string, bool> shouldIgnoreKeyCache =
            new ConcurrentDictionary<string, bool>();

        private readonly ConcurrentDictionary<Type, bool> shouldIgnoreTypeCache =
            new ConcurrentDictionary<Type, bool>();

        private readonly Trace trace;
        private readonly Dictionary<Type, TypeFormat> typeFormatters = new Dictionary<Type, TypeFormat>();

        private readonly ConcurrentDictionary<Type, TypeFormat> typeFormattersCache =
            new ConcurrentDictionary<Type, TypeFormat>();

        static TracingMiddlewareOptions()
        {
            Default = new TracingMiddlewareOptions()
                .ForType<long>(v => v.ToString(CultureInfo.InvariantCulture))
                .ForType<int>(v => v.ToString(CultureInfo.InvariantCulture))
                .ForType<CancellationToken>(
                    v => "CancellationRequested=" + v.IsCancellationRequested + ";CanBeCanceled=" + v.CanBeCanceled)
                .ForType<Stream>(v =>
                {
                    if (!v.CanRead)
                    {
                        return "Stream Unreadable";
                    }
                    using (var reader = new StreamReader(v))
                    {
                        return reader.ReadToEnd();
                    }
                })
                .ForType<IDictionary<string, string[]>>(
                    headers => string.Join(",",
                        headers.Select(header => string.Format("[{0}:{1}]", header.Key, string.Join(",", header.Value)))));
        }

        public TracingMiddlewareOptions(Func<bool> isEnabled = null)
            : this(ConsoleTrace, DefaultMessageFormat, DefaultTypeFormat, isEnabled)
        {
        }

        public TracingMiddlewareOptions(Trace trace, Func<bool> isEnabled = null)
            : this(trace, DefaultMessageFormat, DefaultTypeFormat, isEnabled)
        {
        }

        public TracingMiddlewareOptions(Trace trace, MessageFormat messageFormat, Func<bool> isEnabled = null)
            : this(trace, messageFormat, DefaultTypeFormat, isEnabled)
        {
        }

        public TracingMiddlewareOptions(Trace trace, TypeFormat defaultTypeformat, Func<bool> isEnabled = null)
            : this(trace, DefaultMessageFormat, defaultTypeformat, isEnabled)
        {
        }

        public TracingMiddlewareOptions(Trace trace, MessageFormat messageFormat, TypeFormat defaultTypeFormat,
            Func<bool> isEnabled = null)
        {
            if (trace == null) throw new ArgumentNullException("trace");
            if (messageFormat == null) throw new ArgumentNullException("messageFormat");
            if (defaultTypeFormat == null) throw new ArgumentNullException("defaultTypeFormat");

            this.trace = trace;
            this.messageFormat = messageFormat;
            this.defaultTypeFormat = defaultTypeFormat;
            this.isEnabled = isEnabled ?? (() => true);
        }

        public Trace Trace
        {
            get { return trace; }
        }

        public bool IsEnabled
        {
            get { return isEnabled(); }
        }

        public MessageFormat MessageFormat
        {
            get { return messageFormat; }
        }

        public TracingMiddlewareOptions ForType<T>(Func<T, string> format)
        {
            typeFormatters.Add(typeof (T), value => format((T) value));
            return this;
        }

        public TracingMiddlewareOptions ForType<T>(IFormatProvider formatProvider, string format)
        {
            typeFormatters.Add(typeof (T), value => string.Format(formatProvider, format, value));
            return this;
        }

        public TracingMiddlewareOptions ForKey(string key, Action<string> traceAction)
        {
            keyTracers.Add(key, value => traceAction(value));
            return this;
        }

        public TracingMiddlewareOptions Ignore<T>()
        {
            ignoreTypes.Add(typeof (T));
            return this;
        }

        public TracingMiddlewareOptions Ignore(string key)
        {
            ignoreKeys.Add(key);
            return this;
        }

        public TracingMiddlewareOptions Ignore(Predicate<string> keyPredicate)
        {
            ignoreKeyPredicates.Add(keyPredicate);
            return this;
        }

        public TracingMiddlewareOptions Include<T>()
        {
            includeTypes.Add(typeof (T));
            return this;
        }

        public TracingMiddlewareOptions Include(string key)
        {
            includeKeys.Add(key);
            return this;
        }

        public TracingMiddlewareOptions Include(Predicate<string> keyPredicate)
        {
            includeKeyPredicates.Add(keyPredicate);
            return this;
        }

        public TypeFormat GetFormatter(string key, Type type)
        {
            if (ignoreKeys.Contains(key) && !includeKeys.Contains(key))
            {
                return null;
            }
            if (shouldIgnoreTypeCache.GetOrAdd(type, t =>
                ignoreTypes.Any(ignoreType => ignoreType.IsAssignableFrom(t)) &&
                !includeTypes.Any(includeType => type.IsAssignableFrom(t))))
            {
                return null;
            }
            if (shouldIgnoreKeyCache.GetOrAdd(key, k =>
                ignoreKeyPredicates.Any(predicate => predicate(k)) &&
                !includeKeyPredicates.Any(predicate => predicate(k))))
            {
                return null;
            }

            return typeFormattersCache.GetOrAdd(type, t =>
            {
                TypeFormat typeFormat;
                if (!typeFormatters.TryGetValue(type, out typeFormat))
                {
                    KeyValuePair<Type, TypeFormat> typeFormatKvp =
                        typeFormatters.FirstOrDefault(kvp => kvp.Key.IsAssignableFrom(type));
                    typeFormat = default(KeyValuePair<Type, TypeFormat>).Equals(typeFormatKvp)
                        ? defaultTypeFormat
                        : typeFormatKvp.Value;
                }
                return value => value == null ? "{null}" : typeFormat(value);
            });
        }

        public Trace GetTrace(string key)
        {
            if (!keyTracers.ContainsKey(key))
            {
                return Trace;
            }

            return keyTracers[key];
        }
    }
}