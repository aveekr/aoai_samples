// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins;

namespace Plugins;

internal sealed class ClassificationPlugin
{
    [KernelFunction, Description("Based on Merchent names and expense description, categorize them")]
    public string ClassifyExpense(
        [Description("The name of the Merchent")] string merchant_name,
        [Description("The description of the expense")] string expense_description) =>

        $"Sent email to: {merchant_name}. Body: {expense_description}";

   
}
