﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Kusto.Language.Symbols;

namespace BabyKusto.Core.InternalRepresentation
{
    internal class IRCastExpressionNode : IRExpressionNode
    {
        public IRCastExpressionNode(IRExpressionNode expression, TypeSymbol resultType)
            : base(resultType, expression.ResultKind)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public IRExpressionNode Expression { get; }

        public override int ChildCount => 1;
        public override IRNode GetChild(int index) =>
            index switch
            {
                0 => Expression,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        public override TResult? Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
            where TResult : class
        {
            return visitor.VisitCastExpression(this, context);
        }

        public override string ToString()
        {
            return $"CastExpression: {SchemaDisplay.GetText(ResultType)}";
        }
    }
}
