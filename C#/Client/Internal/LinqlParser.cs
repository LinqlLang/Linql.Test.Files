﻿using Linql.Client.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Linql.Client.Internal
{
    public class LinqlParser : ExpressionVisitor
    {
        internal Stack<LinqlExpression> LinqlStack { get; set; } = new Stack<LinqlExpression>();

        internal Stack<Expression> ExpressionStack { get; set; } = new Stack<Expression>();

        public LinqlExpression Root { get; protected set; }

        protected void PushToStack(LinqlExpression LinqlExpression, Expression CSharpExpression)
        {
            this.LinqlStack.Push(LinqlExpression);
            this.ExpressionStack.Push(CSharpExpression);
        }

        protected void PopStack()
        {
            this.LinqlStack.Pop();
            this.ExpressionStack.Pop();
        }

        protected void AttachToExpression(LinqlExpression ExpressionToAttach)
        {
            LinqlExpression previousExpression = this.LinqlStack.FirstOrDefault();
            if (previousExpression is LinqlLambda lambda)
            {
                lambda.Body = ExpressionToAttach;
            }
            else if (previousExpression is LinqlBinary binary)
            {
                if (binary.Left == null)
                {
                    binary.Left = ExpressionToAttach;
                }
                else
                {
                    binary.Right = ExpressionToAttach;
                }
            }
            else if (previousExpression != null)
            {
                previousExpression.Next = ExpressionToAttach;
            }
            else
            {
                this.Root = ExpressionToAttach;
            }

        }

        protected void RemoveFromPrevious(LinqlExpression ExpressionToRemove)
        {
            this.PopStack();

            LinqlExpression previousExpression = this.LinqlStack.FirstOrDefault();

            if (previousExpression is LinqlFunction function)
            {
                if (function.Arguments != null && ExpressionToRemove != null)
                {
                    function.Arguments.Remove(ExpressionToRemove);
                }

            }

        }

        public LinqlParser(Expression LinqlExpression) : base()
        {
            this.Visit(LinqlExpression);
        }


        public override Expression Visit(Expression expression)
        {
            return base.Visit(expression);
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {


            Expression previous = this.ExpressionStack.FirstOrDefault();

            object value = c.Value;
            LinqlType Type = new LinqlType(c.Type);

            LinqlConstant constant = new LinqlConstant(Type, value);
            this.AttachToExpression(constant);
            this.PushToStack(constant, c);


            return base.VisitConstant(c);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            LinqlFunction function = new LinqlFunction(m.Method.Name);

            if (m.Object != null)
            {
                LinqlParser objectParser = new LinqlParser(m.Object);
                LinqlExpression parsedObject = objectParser.Root;
                function.Object = parsedObject;
            }
            function.Arguments = m.Arguments.Select(r =>
            {
                LinqlParser argParser = new LinqlParser(r);
                return argParser.Root;
            }).ToList();

            if(function.Arguments.Count == 0)
            {
                function.Arguments = null;
            }

            this.AttachToExpression(function);
            this.PushToStack(function, m);

            if (this.Root == function)
            {
                LinqlExpression firstArg = function.Arguments.FirstOrDefault();

                if (firstArg is LinqlConstant linqlConstant && linqlConstant.ConstantType.TypeName == nameof(LinqlSearch))
                {
                    function.Arguments = function.Arguments.Skip(1).ToList();
                }
            }

            return m;
        }


        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType != ExpressionType.Quote)
            {
                string unaryName = Enum.GetName(typeof(ExpressionType), node.NodeType);

                LinqlUnary unary = new LinqlUnary(unaryName);
                this.AttachToExpression(unary);
                this.PushToStack(unary, node);
            }
            base.VisitUnary(node);

            return node;
        }


        //protected override Expression VisitLambda<T>(LambdaExpression lambda)
        //{
        //    return base.VisitLambda<T>(lambda);
        //}

        protected override Expression VisitBinary(BinaryExpression binary)
        {
            string binaryName = Enum.GetName(typeof(ExpressionType), binary.NodeType);
            LinqlBinary linqlBinary = new LinqlBinary(binaryName);

            this.AttachToExpression(linqlBinary);
            this.PushToStack(linqlBinary, binary);

            Expression left = binary.Left;
            Expression right = binary.Right;

            LinqlParser leftParser = new LinqlParser(left);
            LinqlParser rightParser = new LinqlParser(right);

            linqlBinary.Left = leftParser.Root;
            linqlBinary.Right = rightParser.Root;

            return binary;
        }

        protected override Expression VisitLambda<T>(Expression<T> lambda)
        {
            LinqlLambda linqlLambda = new LinqlLambda();

            this.AttachToExpression(linqlLambda);
            this.PushToStack(linqlLambda, lambda);

            LinqlParser bodyParser = new LinqlParser(lambda.Body);

            linqlLambda.Body = bodyParser.Root;

            foreach (ParameterExpression parameter in lambda.Parameters)
            {
                LinqlParameter param = new LinqlParameter(parameter.Name);

                if (linqlLambda.Parameters == null)
                {
                    linqlLambda.Parameters = new List<LinqlExpression>();
                }
                linqlLambda.Parameters.Add(param);
            }

            return lambda;
        }


        protected override Expression VisitParameter(ParameterExpression parameter)
        {

            LinqlParameter param = new LinqlParameter(parameter.Name);
            this.AttachToExpression(param);
            this.PushToStack(param, parameter);
            Expression expression = base.VisitParameter(parameter);

            return parameter;
        }


        protected override Expression VisitMember(MemberExpression m)
        {

            LinqlProperty property = new LinqlProperty(m.Member.Name);

            if (m.Member.MemberType == System.Reflection.MemberTypes.Field)
            {
                ExpressionStack.Push(m);
            }

            base.VisitMember(m);
            LinqlExpression previous = this.LinqlStack.First();

            if (previous is LinqlConstant constant)
            {
                object value = constant.Value;

                FieldInfo field = m.Member.DeclaringType.GetField(m.Member.Name);

                if (field != null)
                {
                    value = field.GetValue(value);
                }
                else
                {
                    PropertyInfo propertyInfo = m.Member.DeclaringType.GetProperty(m.Member.Name);
                    value = propertyInfo.GetValue(value);
                }

                LinqlType Type = new LinqlType(value.GetType());

                LinqlConstant linqlConstant = new LinqlConstant(Type, value);
                LinqlExpression previousExpression = this.LinqlStack.FirstOrDefault();
                this.RemoveFromPrevious(previousExpression);
                this.AttachToExpression(linqlConstant);
                this.PushToStack(linqlConstant, m);
            }
            else
            {
                this.AttachToExpression(property);
                this.PushToStack(property, m);
            }


            return m;
        }
    }
}
