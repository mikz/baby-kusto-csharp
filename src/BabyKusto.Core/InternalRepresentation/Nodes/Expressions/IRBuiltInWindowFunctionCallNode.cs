﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using BabyKusto.Core.Evaluation.BuiltIns;
using Kusto.Language.Symbols;

namespace BabyKusto.Core.InternalRepresentation
{
    internal class IRBuiltInWindowFunctionCallNode : IRExpressionNode
    {
        public IRBuiltInWindowFunctionCallNode(Signature signature, WindowOverloadInfo overloadInfo, IReadOnlyList<Parameter> parameters, IRListNode<IRExpressionNode> arguments, TypeSymbol resultType)
            : base(resultType, GetResultKind(arguments))
        {
            Signature = signature ?? throw new ArgumentNullException(nameof(signature));
            OverloadInfo = overloadInfo ?? throw new ArgumentNullException(nameof(overloadInfo));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public Signature Signature { get; }
        public WindowOverloadInfo OverloadInfo { get; }
        public IReadOnlyList<Parameter> Parameters { get; }
        public IRListNode<IRExpressionNode> Arguments { get; }

        public override int ChildCount => 1;
        public override IRNode GetChild(int index) =>
            index switch
            {
                0 => Arguments,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        public override TResult? Accept<TResult, TContext>(IRNodeVisitor<TResult, TContext> visitor, TContext context)
            where TResult : class
        {
            return visitor.VisitBuiltInWindowFunctionCall(this, context);
        }

        public override string ToString()
        {
            return $"BuiltInWindowFunctionCall({SchemaDisplay.GetText(Signature.Symbol)}): {SchemaDisplay.GetText(ResultType)}";
        }
    }
}
