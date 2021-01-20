using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;


namespace StowayNet
{
    public static class SelectExtension
    {
        public static IQueryable<T> SelectProperties<T>(
            this IQueryable<T> source,
            IEnumerable<string> selectedProperties) where T : class
        {
            IDictionary<string, PropertyInfo> sourceProperties =
                GetTypeProperties<T>(selectedProperties);

            Type runtimeType = RuntimeTypeBuilder.GetRuntimeType(sourceProperties);
            Type sourceType = typeof(T);

            ParameterExpression sourceParameter = Expression.Parameter(sourceType, "t");

            FieldInfo[] runtimeTypeFields = runtimeType.GetFields();

            IEnumerable<MemberBinding> bindingsToRuntimeType = runtimeTypeFields
                .Select(field => Expression.Bind(
                    field,
                    Expression.Property(
                        sourceParameter,
                        sourceProperties[field.Name]
                    )
                ));

            IQueryable<object> runtimeTypeSelectExpressionQuery
                = GetTypeSelectExpressionQuery<object>(
                    sourceType,
                    runtimeType,
                    bindingsToRuntimeType,
                    source,
                    sourceParameter
            );

            List<object> listOfObjects = runtimeTypeSelectExpressionQuery.ToList();

            MethodInfo castMethod = typeof(Queryable)
                .GetMethod("Cast", BindingFlags.Public | BindingFlags.Static)
                .MakeGenericMethod(runtimeType);

            IQueryable castedSource = castMethod.Invoke(
                null,
                new Object[] { listOfObjects.AsQueryable() }
            ) as IQueryable;

            ParameterExpression runtimeParameter = Expression.Parameter(runtimeType, "p");

            IDictionary<string, FieldInfo> dynamicTypeFieldsDict =
                runtimeTypeFields.ToDictionary(f => f.Name, f => f);

            IEnumerable<MemberBinding> bindingsToTargetType = sourceProperties.Values
                .Select(property => Expression.Bind(
                    property,
                    Expression.Field(
                        runtimeParameter,
                        dynamicTypeFieldsDict[property.Name]
                    )
                ));

            IQueryable<T> targetTypeSelectExpressionQuery
                = GetTypeSelectExpressionQuery<T>(
                    runtimeType,
                    sourceType,
                    bindingsToTargetType,
                    castedSource,
                    runtimeParameter
            );

            return targetTypeSelectExpressionQuery;
        }

        private static IQueryable<TT> GetTypeSelectExpressionQuery<TT>(
            Type sourceType,
            Type targetType,
            IEnumerable<MemberBinding> binding,
            IQueryable source,
            ParameterExpression sourceParameter)
        {
            LambdaExpression typeSelector =
                Expression.Lambda(
                    Expression.MemberInit(
                        Expression.New(
                            targetType.GetConstructor(Type.EmptyTypes)
                        ),
                        binding
                    ),
                    sourceParameter
                );

            MethodCallExpression typeSelectExpression =
                Expression.Call(
                    typeof(Queryable),
                    "Select",
                    new[] { sourceType, targetType },
                    Expression.Constant(source),
                    typeSelector
                );

            return Expression.Lambda(typeSelectExpression)
                .Compile()
                .DynamicInvoke() as IQueryable<TT>;
        }

        private static IDictionary<string, PropertyInfo> GetTypeProperties<T>(
            IEnumerable<string> selectedProperties) where T : class
        {

            var sourceProperties = typeof(T).GetProperties();

            var properties = new Dictionary<string, PropertyInfo>();
            foreach (var name in selectedProperties)
            {
                properties[name] = sourceProperties.FirstOrDefault(p => p.Name == name);
            }

            return properties;
            //var existedProperties = properties.ToDictionary(p=>p.Name);

            //return selectedProperties
            //    .Where(existedProperties.ContainsKey)
            //    .ToDictionary(p => p, p => existedProperties[p]);
        }
    }

}
