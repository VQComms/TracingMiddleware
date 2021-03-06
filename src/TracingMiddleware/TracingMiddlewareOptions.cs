﻿namespace TracingMiddleware
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Reflection;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;

    public delegate void Trace(string requestId, string message);

    public delegate string MessageFormat(string key, string value);

    public delegate string TypeFormat(object value);

    public class TracingMiddlewareOptions
    {
        public static readonly MessageFormat DefaultMessageFormat =
            (key, value) => string.Format("{0} : {1}", key, value);

        public static readonly TypeFormat DefaultTypeFormat = value => value.ToString();
        public static readonly Trace DefaultTrace = (requestId, message) => Console.WriteLine(requestId + " : " + message);
        public static readonly TracingMiddlewareOptions Default;
        public static readonly IEnumerable<Func<HttpContext, bool>> DefaultTracingFilters = Enumerable.Empty<Func<HttpContext, bool>>();
        private readonly TypeFormat defaultTypeFormat;
        private readonly List<Predicate<string>> ignoreKeyPredicates = new List<Predicate<string>>();
        private readonly HashSet<string> ignoreKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Type> ignoreTypes = new HashSet<Type>();
        private readonly List<Predicate<string>> includeKeyPredicates = new List<Predicate<string>>();
        private readonly HashSet<string> includeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<Type> includeTypes = new HashSet<Type>();
        private readonly Func<bool> isEnabled;
        private readonly Dictionary<string, Trace> keyTracers = new Dictionary<string, Trace>();

        private readonly List<Func<HttpContext, bool>> filters = new List<Func<HttpContext, bool>>();


        private readonly ConcurrentDictionary<string, bool> shouldIgnoreKeyCache =
            new ConcurrentDictionary<string, bool>();

        private readonly ConcurrentDictionary<Type, bool> shouldIgnoreTypeCache =
            new ConcurrentDictionary<Type, bool>();

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
                .ForType<IHeaderDictionary>(
                    headers => string.Join(",",
                        headers.Select(header => string.Format("[{0}:{1}]", header.Key, string.Join(",", header.Value)))))
                .ForType<ClaimsPrincipal>(user => string.Join(",", user.Claims.Select(x => x.Type + ":" + x.Value)));

        }

        public TracingMiddlewareOptions(Func<bool> isEnabled = null)
            : this(DefaultTrace, DefaultMessageFormat, DefaultTypeFormat, DefaultTracingFilters, isEnabled)
        {
        }

        public TracingMiddlewareOptions(Trace trace, Func<bool> isEnabled = null)
            : this(trace, DefaultMessageFormat, DefaultTypeFormat, DefaultTracingFilters, isEnabled)
        {
        }

        public TracingMiddlewareOptions(Trace trace, MessageFormat messageFormat, Func<bool> isEnabled = null)
            : this(trace, messageFormat, DefaultTypeFormat, DefaultTracingFilters, isEnabled)
        {
        }

        public TracingMiddlewareOptions(Trace trace, TypeFormat defaultTypeformat, Func<bool> isEnabled = null)
            : this(trace, DefaultMessageFormat, defaultTypeformat, DefaultTracingFilters, isEnabled)
        {
        }

        public TracingMiddlewareOptions(Trace trace, MessageFormat messageFormat, TypeFormat defaultTypeFormat, IEnumerable<Func<HttpContext, bool>> filters, Func<bool> isEnabled = null)
        {
            if (trace == null) throw new ArgumentNullException("trace");
            if (messageFormat == null) throw new ArgumentNullException("messageFormat");
            if (defaultTypeFormat == null) throw new ArgumentNullException("defaultTypeFormat");

            this.Trace = trace;
            this.MessageFormat = messageFormat;
            this.defaultTypeFormat = defaultTypeFormat;
            this.isEnabled = isEnabled ?? (() => true);
            this.filters.AddRange(filters);
        }

        public Trace Trace { get; }

        public bool IsEnabled => isEnabled();

        public MessageFormat MessageFormat { get; }

        public IEnumerable<Func<HttpContext, bool>> Filters => filters;

        public TracingMiddlewareOptions ForType<T>(Func<T, string> format)
        {
            typeFormatters.Add(typeof(T), value => format((T)value));
            return this;
        }

        public TracingMiddlewareOptions ForType<T>(IFormatProvider formatProvider, string format)
        {
            typeFormatters.Add(typeof(T), value => string.Format(formatProvider, format, value));
            return this;
        }

        public TracingMiddlewareOptions ForKey(string key, Action<string, string> traceAction)
        {
            keyTracers.Add(key, (requestId, value) => traceAction(requestId, value));
            return this;
        }

        public TracingMiddlewareOptions Ignore<T>()
        {
            ignoreTypes.Add(typeof(T));
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
            includeTypes.Add(typeof(T));
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

        public TracingMiddlewareOptions AddFilter(Func<HttpContext, bool> filter)
        {
            this.filters.Add(filter);
            return this;
        }

        public TypeFormat GetFormatter(string key, Type type)
        {
            if (ignoreKeys.Contains(key) && !includeKeys.Contains(key))
            {
                return null;
            }
            if (shouldIgnoreTypeCache.GetOrAdd(type, t =>
                ignoreTypes.Any(ignoreType => ignoreType.GetTypeInfo().IsAssignableFrom(t)) &&
                !includeTypes.Any(includeType => type.GetTypeInfo().IsAssignableFrom(t))))
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
                        typeFormatters.FirstOrDefault(kvp => kvp.Key.GetTypeInfo().IsAssignableFrom(type));
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