/*
Copyright (c) 2011 Walter Tetzner

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be included
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace org.bovinegenius.maybe {
  public class Maybe {
    public static T Do<T>(Expression<Func<T>> expr) {
      return Compile(expr).Invoke();
    }

    public static Func<T> Compile<T>(Expression<Func<T>> expr) {
      return (Func<T>)ConvertExpr(expr.Body).Compile();
    }

    private static LambdaExpression ConvertExpr(Expression expr) {
      if (expr.NodeType == ExpressionType.MemberAccess
          || expr.NodeType == ExpressionType.Call) {
            var chain = MemberChain(expr);
            var first = (Expression)chain.First().Value;
            var body = first.NodeType == ExpressionType.MemberAccess ? ((MemberExpression)first).Expression :
                                                                       ((MethodCallExpression)first).Object;
        var lambda = chain
          .Select((x) => { if (x.Value.NodeType == ExpressionType.MemberAccess) {
            return (Expression)Expression.MakeMemberAccess(Expression.Parameter(x.Key, "x"), ((MemberExpression)x.Value).Member);
          } else {
            return Expression.Call(Expression.Parameter(x.Key, "x"), ((MethodCallExpression)x.Value).Method,
                                   ((MethodCallExpression)x.Value).Arguments);
          }})
          .Reverse()
          .Aggregate((soFar, next) => {
            LambdaExpression func = null;
            if (soFar.NodeType == ExpressionType.Lambda) {
              func = (LambdaExpression)soFar;
            } else if (soFar.NodeType == ExpressionType.Call) {
              var val = (MethodCallExpression)soFar;
              var param = (ParameterExpression)val.Object;
              var lambdaBody = Expression.Condition(Expression.Equal(param, Expression.Constant(null)),
                                                    Expression.Convert(Expression.Constant(null), val.Type),
                                                    val);
              func = Expression.Lambda(lambdaBody, new ParameterExpression[] { param });
            } else if (soFar.NodeType == ExpressionType.MemberAccess) {
              var val = (MemberExpression)soFar;
              var param = (ParameterExpression)val.Expression;
              var lambdaBody = Expression.Condition(Expression.Equal(param, Expression.Constant(null)),
                                                    Expression.Convert(Expression.Constant(null), val.Type),
                                                    val);
              func = Expression.Lambda(lambdaBody, new ParameterExpression[] { param });
            }
            var invoke = Expression.Invoke(func, new Expression[] { next });
            var prm = (ParameterExpression)(next.NodeType == ExpressionType.MemberAccess ?
              ((MemberExpression)next).Expression :
              ((MethodCallExpression)next).Object);
            var exprBody = Expression.Condition(Expression.Equal(prm, Expression.Constant(null)),
                                                Expression.Convert(Expression.Constant(null), invoke.Type),
                                                invoke);
            return Expression.Lambda(exprBody, new ParameterExpression[] { prm });
          });

        var invocation = Expression.Invoke(lambda, new Expression[] { body });
        return Expression.Lambda(invocation, new ParameterExpression[] { });
      } else {
        return Expression.Lambda(expr, new ParameterExpression[] {});
      }
    }

    private static IEnumerable<KeyValuePair<Type, Expression>> MemberChain(Expression expr) {
      if (expr.NodeType == ExpressionType.MemberAccess) {
        var exp = (MemberExpression)expr;
        return MemberChain(exp.Expression).Concat(new KeyValuePair<Type, Expression>[]
        { new KeyValuePair<Type, Expression>(exp.Member.DeclaringType, exp) });
      } else if (expr.NodeType == ExpressionType.Call) {
        var exp = (MethodCallExpression)expr;
        return MemberChain(exp.Object).Concat(new KeyValuePair<Type, Expression>[]
        { new KeyValuePair<Type, Expression>(exp.Method.DeclaringType, exp) });
      } else {
        return new KeyValuePair<Type, Expression>[0];
      }
    }
  }
}
