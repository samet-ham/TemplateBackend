using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Interceptors;
using Core.Utilities.IoC;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Aspects.Autofac.Caching
{
    public class CacheAspect : MethodInterception
    {
        private readonly int _duration;
		private readonly ICacheManager _cacheManager;
		public CacheAspect(int duration = 60)
		{
			_duration = duration;
			_cacheManager = ServiceTool.ServiceProvider.GetService<ICacheManager>();
		}

        public override void Intercept(IInvocation invocation)
        {
            var methodName = string.Format($"{invocation.Method.ReflectedType.FullName}.{invocation.Method.Name}");
            var arguments = invocation.Arguments.ToList();

            var sb = new StringBuilder();
            foreach (var item in arguments)
            {
                var paramValues = item.GetType().GetProperties().Select(p => p.GetValue(item)?.ToString() ?? string.Empty);
                sb.Append(string.Join('_', paramValues));
            }

            var key = $"{methodName}({string.Join(",", arguments.Select(x => x?.ToString() ?? "<Null>"))})";
            var newKey = key + sb;
            if (_cacheManager.IsAdd(newKey))
            {
                invocation.ReturnValue = _cacheManager.Get(newKey);
                return;
            }
            invocation.Proceed();
            _cacheManager.Add(newKey, invocation.ReturnValue, _duration);
        }

        //public override void Intercept(IInvocation invocation)
        //{
        //	var methodName = string.Format($"{invocation.Arguments[0]}.{invocation.Method.Name}");
        //	var arguments = invocation.Arguments;
        //	var key = $"{methodName}({BuildKey(arguments)})";
        //	if (_cacheManager.IsAdd(key))
        //	{
        //		invocation.ReturnValue = _cacheManager.Get(key);
        //		return;
        //	}
        //	invocation.Proceed();
        //	_cacheManager.Add(key, invocation.ReturnValue, _duration);
        //}


        string BuildKey(object[] args)
        {
            var sb = new StringBuilder();
            foreach (var arg in args)
            {
                var paramValues = arg.GetType().GetProperties().Select(p => p.GetValue(arg)?.ToString() ?? string.Empty);
                sb.Append(string.Join('_', paramValues));

            }
            return sb.ToString();
        }

    }
}
