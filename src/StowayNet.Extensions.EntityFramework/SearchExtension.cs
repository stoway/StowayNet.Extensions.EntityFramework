using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StowayNet
{
    public static class SearchExtension
    {
        private static Dictionary<ConditionOperator, Func<Expression, Expression, Expression>> _binaryOpFactory = null;
        static Dictionary<ConditionOperator, Func<Expression, Expression, Expression>> binaryOpFactory
        {
            get
            {
                if (_binaryOpFactory == null)
                {
                    _binaryOpFactory = new Dictionary<ConditionOperator, Func<Expression, Expression, Expression>>();
                    binaryOpFactory.Add(ConditionOperator.Equal, Expression.Equal);
                    binaryOpFactory.Add(ConditionOperator.GreaterThan, Expression.GreaterThan);
                    binaryOpFactory.Add(ConditionOperator.LessThan, Expression.LessThan);
                    binaryOpFactory.Add(ConditionOperator.GreaterThanOrEqual, Expression.GreaterThanOrEqual);
                    binaryOpFactory.Add(ConditionOperator.LessThanOrEqual, Expression.LessThanOrEqual);
                    binaryOpFactory.Add(ConditionOperator.NotEqual, Expression.NotEqual);
                    //binaryOpFactory.Add(ConditionOperator.And, Expression.And);
                    //binaryOpFactory.Add(ConditionOperator.Or, Expression.Or);
                    //MethodInfo contains = typeof(string).GetMethod("Contains");
                }
                return _binaryOpFactory;
            }
        }

        public static IQueryable<TEntity> Query<TEntity>(this IQueryable<TEntity> source, IList<ConditionEntity> conditions)
        {
            var parameter = Expression.Parameter(source.ElementType);
            Expression expr = Expression.Constant(true);
            var properties = typeof(TEntity).GetProperties();
            if (conditions != null && conditions.Count > 0)
            {
                foreach (var c in conditions)
                {
                    if (c.Value == null || string.IsNullOrWhiteSpace(c.Value.ToString()))
                        continue;

                    if (properties.Count(p => p.Name.ToLower() == c.FieldName.ToLower()) == 0)
                    {
                        continue;
                    }

                    Expression left = Expression.Property(parameter, c.FieldName);

                    if (c.Operator == ConditionOperator.Between)
                    {
                        Expression right = Expression.Constant(ChangeType(c.Value, left.Type), left.Type);

                        Expression right2 = Expression.Constant(ChangeType(c.Value2, left.Type), left.Type);

                        c.Operator = ConditionOperator.GreaterThanOrEqual;
                        expr = Expression.And(expr, GetExpression(parameter, c, left, right));

                        c.Operator = ConditionOperator.LessThanOrEqual;
                        expr = Expression.And(expr, GetExpression(parameter, c, left, right2));

                    }
                    else if (c.Operator == ConditionOperator.In)
                    {
                        var cnd_value = c.Value as IEnumerable<object>;
                        IEnumerable<Expression> equals = cnd_value.Select(value =>
                             (Expression)Expression.Equal(left,
                                  Expression.Constant(value, left.Type)));

                        Expression filter = equals.Aggregate((accumulate, equal) =>
                            Expression.Or(accumulate, equal));
                        expr = Expression.And(expr, filter);
                    }
                    else
                    {
                        Expression right = Expression.Constant(ChangeType(c.Value, left.Type), left.Type);

                        Expression filter = GetExpression(parameter, c, left, right);
                        expr = Expression.And(expr, filter);
                    }

                }
            }

            var lambda = Expression.Lambda<Func<TEntity, bool>>(expr, parameter);

            return source.Where(lambda);
        }
        public static IQueryable Where(this IQueryable source, IList<ConditionEntity> conditions)
        {
            var parameter = Expression.Parameter(source.ElementType);
            Expression expr = Expression.Constant(true);
            foreach (var c in conditions)
            {
                if (c.Value == null || string.IsNullOrWhiteSpace(c.Value.ToString()))
                    continue;

                if (source.ElementType.GetProperty(c.FieldName) == null)
                    continue;

                Expression left = Expression.Property(parameter, c.FieldName);

                Expression right = Expression.Constant(ChangeType(c.Value, left.Type), left.Type);

                if (c.Operator == ConditionOperator.Between)
                {
                    Expression right2 = Expression.Constant(ChangeType(c.Value2, left.Type), left.Type);

                    c.Operator = ConditionOperator.GreaterThanOrEqual;
                    expr = Expression.And(expr, GetExpression(parameter, c, left, right));

                    c.Operator = ConditionOperator.LessThanOrEqual;
                    expr = Expression.And(expr, GetExpression(parameter, c, left, right2));

                }
                else if (c.Operator == ConditionOperator.In)
                {
                    var cnd_value = c.Value as IEnumerable<object>;
                    IEnumerable<Expression> equals = cnd_value.Select(value =>
                         (Expression)Expression.Equal(left,
                              Expression.Constant(value, left.Type)));

                    Expression filter = equals.Aggregate((accumulate, equal) =>
                        Expression.Or(accumulate, equal));
                    expr = Expression.And(expr, filter);
                }
                else
                {
                    Expression filter = GetExpression(parameter, c, left, right);
                    expr = Expression.And(expr, filter);
                }

            }
            return source.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable), "Where",
                    new Type[] { source.ElementType },
                    source.Expression, Expression.Quote(Expression.Lambda(expr, parameter))));
        }

        public static Expression GetExpression(ParameterExpression parameter, ConditionEntity condition, Expression left, Expression right)
        {
            Expression expression = null;
            if (condition.Operator == ConditionOperator.Contains)
            {
                expression = BuildLikeExpression(parameter, condition);
            }
            else
            {
                expression = binaryOpFactory[condition.Operator](left, right);
            }

            return expression;
        }


        public static Expression BuildLikeExpression(
            ParameterExpression parameter,
            ConditionEntity condition,
            char wildcard = '%')
        {

            var method = GetLikeMethod(condition.Value.ToString(), wildcard);

            condition.Value = condition.Value.ToString().Trim(wildcard);
            Expression left = Expression.Property(parameter, condition.FieldName);
            return Expression.Call(left, method, Expression.Constant(condition.Value));

        }

        private static MethodInfo GetLikeMethod(string value, char wildcard)
        {
            var methodName = "Contains";

            var textLength = value.Length;
            value = value.TrimEnd(wildcard);
            if (textLength > value.Length)
            {
                methodName = "StartsWith";
                textLength = value.Length;
            }

            value = value.TrimStart(wildcard);
            if (textLength > value.Length)
            {
                methodName = (methodName == "StartsWith") ? "Contains" : "EndsWith";
                textLength = value.Length;
            }

            var stringType = typeof(string);
            return stringType.GetMethod(methodName, new Type[] { stringType });
        }

        private static object ChangeType(object value, Type conversionType)
        {
            if (value == null) return null;

            if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                NullableConverter nullableConverter = new NullableConverter(conversionType);
                conversionType = nullableConverter.UnderlyingType;

                if (conversionType.IsEnum)
                {
                    return Enum.Parse(conversionType, value.ToString());
                }
            }
            else if (conversionType.IsGenericType && conversionType.GenericTypeArguments.Length == 1 && conversionType.GenericTypeArguments[0].IsEnum)
            {
                return Enum.Parse(conversionType.GenericTypeArguments[0], value.ToString());
            }
            else if (conversionType.IsEnum)
            {
                return Enum.Parse(conversionType, value.ToString());
            }

            return Convert.ChangeType(value, conversionType);
        }
    }

}
