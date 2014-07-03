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

        private readonly TypeFormat _defaultTypeFormat;
        private readonly HashSet<string> _ignoreKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _includeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Predicate<string>> _ignoreKeyPredicates = new List<Predicate<string>>();
        private readonly List<Predicate<string>> _includeKeyPredicates = new List<Predicate<string>>();
        private readonly HashSet<Type> _ignoreTypes = new HashSet<Type>();
        private readonly HashSet<Type> _includeTypes = new HashSet<Type>();
        private readonly Func<bool> _isEnabled;
        private readonly MessageFormat _messageFormat;
        private readonly Trace _trace;
        private readonly Dictionary<Type, TypeFormat> _typeFormatters = new Dictionary<Type, TypeFormat>();
        private readonly ConcurrentDictionary<Type, bool> _shouldIgnoreTypeCache = new ConcurrentDictionary<Type, bool>();
        private readonly ConcurrentDictionary<string, bool> _shouldIgnoreKeyCache = new ConcurrentDictionary<string, bool>();
        private readonly ConcurrentDictionary<Type, TypeFormat> _typeFormattersCache = new ConcurrentDictionary<Type, TypeFormat>(); 

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
        {}

        public TracingMiddlewareOptions(Trace trace, Func<bool> isEnabled = null)
            : this(trace, DefaultMessageFormat, DefaultTypeFormat, isEnabled)
        {}

        public TracingMiddlewareOptions(Trace trace, MessageFormat messageFormat, Func<bool> isEnabled = null)
            : this(trace, messageFormat, DefaultTypeFormat, isEnabled)
        {}

        public TracingMiddlewareOptions(Trace trace, TypeFormat defaultTypeformat, Func<bool> isEnabled = null)
            : this(trace, DefaultMessageFormat, defaultTypeformat, isEnabled)
        {}

        public TracingMiddlewareOptions(Trace trace, MessageFormat messageFormat, TypeFormat defaultTypeFormat, Func<bool> isEnabled = null)
        {
            if (trace == null) throw new ArgumentNullException("trace");
            if (messageFormat == null) throw new ArgumentNullException("messageFormat");
            if (defaultTypeFormat == null) throw new ArgumentNullException("defaultTypeFormat");

            _trace = trace;
            _messageFormat = messageFormat;
            _defaultTypeFormat = defaultTypeFormat;
            _isEnabled = isEnabled ?? (() => true);
        }

        public Trace Trace
        {
            get { return _trace; }
        }

        public bool IsEnabled
        {
            get { return _isEnabled(); }
        }

        public MessageFormat MessageFormat
        {
            get { return _messageFormat; }
        }

        public TracingMiddlewareOptions ForType<T>(Func<T, string> format)
        {
            _typeFormatters.Add(typeof(T), value => format((T) value));
            return this;
        }

        public TracingMiddlewareOptions ForType<T>(IFormatProvider formatProvider, string format)
        {
            _typeFormatters.Add(typeof(T), value => string.Format(formatProvider, format, value));
            return this;
        }

        public TracingMiddlewareOptions Ignore<T>()
        {
            _ignoreTypes.Add(typeof(T));
            return this;
        }

        public TracingMiddlewareOptions Ignore(string key)
        {
            _ignoreKeys.Add(key);
            return this;
        }

        public TracingMiddlewareOptions Ignore(Predicate<string> keyPredicate)
        {
            _ignoreKeyPredicates.Add(keyPredicate);
            return this;
        }

        public TracingMiddlewareOptions Include<T>()
        {
            _includeTypes.Add(typeof(T));
            return this;
        }

        public TracingMiddlewareOptions Include(string key)
        {
            _includeKeys.Add(key);
            return this;
        }

        public TracingMiddlewareOptions Include(Predicate<string> keyPredicate)
        {
            _includeKeyPredicates.Add(keyPredicate);
            return this;
        }

        public TypeFormat GetFormatter(string key, Type type)
        {
            if (_ignoreKeys.Contains(key) && !_includeKeys.Contains(key))
            {
                return null;
            }
            if(_shouldIgnoreTypeCache.GetOrAdd(type, t => 
                _ignoreTypes.Any(ignoreType => ignoreType.IsAssignableFrom(t)) && !_includeTypes.Any(includeType => type.IsAssignableFrom(t))))
            {
                return null;
            }
            if(_shouldIgnoreKeyCache.GetOrAdd(key, k => 
                _ignoreKeyPredicates.Any(predicate => predicate(k)) && !_includeKeyPredicates.Any(predicate => predicate(k))))
            {
                return null;
            }

            return _typeFormattersCache.GetOrAdd(type, t =>
            {
                TypeFormat typeFormat;
                if (!_typeFormatters.TryGetValue(type, out typeFormat))
                {
                    var typeFormatKvp = _typeFormatters.FirstOrDefault(kvp => kvp.Key.IsAssignableFrom(type));
                    typeFormat = default(KeyValuePair<Type, TypeFormat>).Equals(typeFormatKvp) ?_defaultTypeFormat : typeFormatKvp.Value;
                }
                return value => value == null ? "{null}" : typeFormat(value);
            });
        }
    }
}