using System.Linq.Expressions;
using Franz.Common.MultiTenancy;

namespace Franz.Common.EntityFramework.Repositories;

/// <summary>Rejects bulk update expressions that attempt to reassign tenant ownership.</summary>
internal static class BulkUpdateExpressionValidator
{
    public static void Validate<TEntity>(Expression expression)
    {
        if (!typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity)))
            return;

        var visitor = new TenantOwnershipAssignmentVisitor();
        visitor.Visit(expression);

        if (visitor.AttemptsTenantOwnershipChange)
            throw new TenantIsolationException(
                "Bulk updates cannot change TenantId. Tenant ownership is immutable.");
    }

    private sealed class TenantOwnershipAssignmentVisitor : ExpressionVisitor
    {
        public bool AttemptsTenantOwnershipChange { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // SetProperty(selector, value) stores the property selector as its first argument.
            if (node.Method.Name == "SetProperty" && node.Arguments.Count > 0)
            {
                var selector = StripQuote(node.Arguments[0]);
                if (selector is LambdaExpression lambda &&
                    ReferencesTenantId(lambda.Body, lambda.Parameters.FirstOrDefault()))
                    AttemptsTenantOwnershipChange = true;
            }

            return base.VisitMethodCall(node);
        }

        private static Expression StripQuote(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
                expression = ((UnaryExpression)expression).Operand;
            return expression;
        }

        private static bool ReferencesTenantId(Expression expression, ParameterExpression? entityParameter)
        {
            if (entityParameter is null)
                return false;

            var visitor = new TenantMemberVisitor(entityParameter);
            visitor.Visit(expression);
            return visitor.Found;
        }
    }

    private sealed class TenantMemberVisitor(ParameterExpression entityParameter) : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.Name == nameof(ITenantEntity.TenantId) &&
                node.Expression == entityParameter)
                Found = true;

            return base.VisitMember(node);
        }
    }
}
