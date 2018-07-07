using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace Web.Utilities
{
	/// <summary>
	/// RA: the main way to generate URLs in a type-safe way is w/ UrlHelper, but that requires passing an instance of it around through all methods
	/// because it needs to come w/ HttpContext from a Controller. This class provides static alternatives to URL creation using reflection.
	/// </summary>
	public static class AppUrl
	{
		public static readonly Lazy<string> AppBasePath = new Lazy<string>(() => string.Equals(Environment.MachineName, "RYAN-PC", StringComparison.OrdinalIgnoreCase) ? "http://localhost:62568" : "http://ryanallen.io");

		/// <summary>
		/// Returns the relative URL of a <see cref="TController"/>'s action method (HTTP endpoint) using the <see cref="RouteAttribute"/> info.
		///
		/// e.g. [HttpGet, Route("method/{arg1}")] => "/app/method/arg1Value"
		/// </summary>
		public static string GetRelativeUrl<TController>(Expression<Action<TController>> action, object queryParams = null) where TController : Controller
		{
			var method = GetMethod(action);
			var customAttributes = method.CustomAttributes.ToList();
			var routeInfo = customAttributes.SingleOrDefault(a => a.AttributeType == typeof(RouteAttribute));
			var httpInfo = customAttributes.SingleOrDefault(a => a.AttributeType == typeof(HttpGetAttribute) || a.AttributeType == typeof(HttpPostAttribute));
			if (httpInfo == null)
			{
				throw new ArgumentException($@"Cannot get URL for {typeof(TController).Name} method {method.Name}. Requires {typeof(ActionMethodSelectorAttribute)} (e.g. {typeof(HttpGetAttribute).Name}) and {typeof(RouteAttribute).Name}.");
			}

			// get URL and arguments/linq
			int temp;
			var urlWithoutParameters = routeInfo?.ConstructorArguments.FirstOrDefault(s => s.ArgumentType == typeof(string)).Value.ToString() ?? method.Name;
			var argumentInfo = method
				.GetParameters()
				// the method's parameters has the method's parameter names; the action's arguments has the actual values; so we zip them up
				.Zip(((MethodCallExpression)action.Body).Arguments, (p, a) => new { name = p.Name, value = Expression.Lambda(a).Compile().DynamicInvoke() })
				.Where(a => a.value != null && (a.value is string | a.value is bool || a.value is int || a.value is double || Int32.TryParse(a.value.ToString(), out temp)))
				.ToList();

			// add query params
			if (queryParams != null)
			{
				var additionalArguments = queryParams.GetType()
					.GetProperties()
					.Select(q => new { name = q.Name, value = q.GetValue(queryParams, null) })
					.Where(a => a.value != null && (a.value is string | a.value is bool || a.value is int || a.value is double || Int32.TryParse(a.value.ToString(), out temp)))
					.ToList();
				argumentInfo.AddRange(additionalArguments);
			}

			// fill in parameters directly to the URL if applicable
			var urlWithParameters = urlWithoutParameters.Substitute(argumentInfo.ToDictionary(a => a.name, a => a.value));

			// set any parameters present in the URL as query parameters
			var queryParameters = argumentInfo
				.Where(a => !urlWithoutParameters.Contains($"{{{a.name}}}"))
				.Select(a => $"{a.name}={a.value}")
				.ToDelimitedString("&");

			// return relative URL
			return $"/{urlWithParameters}{(String.IsNullOrWhiteSpace(queryParameters) ? String.Empty : $"?{queryParameters}")}";
		}

		/// <summary>
		/// Returns the full URL of a <see cref="TController"/>'s action method (HTTP endpoint) using the <see cref="RouteAttribute"/> info.
		///
		/// e.g. [HttpGet, Route("app/method/{arg1}")] => "http://localhost:10400/app/method/arg1Value"
		/// </summary>
		public static Uri GetFullUrl<TController>(Expression<Action<TController>> action, object queryParams = null) where TController : Controller
		{
			return new Uri($"{AppBasePath.Value}{GetRelativeUrl(action, queryParams)}");
		}

		private static MethodInfo GetMethod<T>(Expression<Action<T>> methodCallExpression)
		{
			return GetMethodFromLambda(methodCallExpression);
		}

		public static MethodInfo GetMethod(Expression<Action> methodCallExpression)
		{
			return GetMethodFromLambda(methodCallExpression);
		}

		private static MethodInfo GetMethodFromLambda(LambdaExpression lambda)
		{
			var methodCall = (MethodCallExpression)lambda.Body;
			return methodCall.Method;
		}

		private static string Substitute(this string @this, Dictionary<string, object> substitutions)
		{
			foreach (var kvp in substitutions)
			{
				@this = @this.Replace($"{{{kvp.Key}}}", kvp.Value.ToString());
			}
			return @this;
		}
	}
}