#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Globalization;
using System.Reflection;

namespace Newtonsoft.Json.Schema.Infrastructure
{
    internal abstract class ReflectionDelegateFactory
    {
        private static bool? _dynamicCodeGeneration;

        public static bool DynamicCodeGeneration
        {
#if HAVE_SECURITY_SAFE_CRITICAL_ATTRIBUTE
            [SecuritySafeCritical]
#endif
            get
            {
                if (_dynamicCodeGeneration == null)
                {
#if HAVE_CAS
                    try
                    {
                        new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
                        new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess).Demand();
                        new SecurityPermission(SecurityPermissionFlag.SkipVerification).Demand();
                        new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                        new SecurityPermission(PermissionState.Unrestricted).Demand();
                        _dynamicCodeGeneration = true;
                    }
                    catch (Exception)
                    {
                        _dynamicCodeGeneration = false;
                    }
#else
                    _dynamicCodeGeneration = false;
#endif
                }

                return _dynamicCodeGeneration.GetValueOrDefault();
            }
        }

        public static ReflectionDelegateFactory Instance
        {
            get
            {
#if !NET35
                if (DynamicCodeGeneration)
                {
                    return ExpressionReflectionDelegateFactory.Instance;
                }
#endif

                return LateBoundReflectionDelegateFactory.Instance;
            }
        }

        public Func<T, object> CreateGet<T>(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo propertyInfo)
            {
                // https://github.com/dotnet/corefx/issues/26053
                if (propertyInfo.PropertyType.IsByRef)
                {
                    throw new InvalidOperationException("Could not create getter for {0}. ByRef return values are not supported.".FormatWith(CultureInfo.InvariantCulture, propertyInfo));
                }

                return CreateGet<T>(propertyInfo);
            }

            if (memberInfo is FieldInfo fieldInfo)
            {
                return CreateGet<T>(fieldInfo);
            }

            throw new Exception("Could not create getter for {0}.".FormatWith(CultureInfo.InvariantCulture, memberInfo));
        }

        public Action<T, object> CreateSet<T>(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo propertyInfo)
            {
                return CreateSet<T>(propertyInfo);
            }

            if (memberInfo is FieldInfo fieldInfo)
            {
                return CreateSet<T>(fieldInfo);
            }

            throw new Exception("Could not create setter for {0}.".FormatWith(CultureInfo.InvariantCulture, memberInfo));
        }

        public abstract MethodCall<T, object> CreateMethodCall<T>(MethodBase method);
        public abstract ObjectConstructor<object> CreateParameterizedConstructor(MethodBase method);
        public abstract Func<T> CreateDefaultConstructor<T>(Type type);
        public abstract Func<T, object> CreateGet<T>(PropertyInfo propertyInfo);
        public abstract Func<T, object> CreateGet<T>(FieldInfo fieldInfo);
        public abstract Action<T, object> CreateSet<T>(FieldInfo fieldInfo);
        public abstract Action<T, object> CreateSet<T>(PropertyInfo propertyInfo);
    }
}